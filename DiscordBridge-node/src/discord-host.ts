import { createReadStream } from 'node:fs';
import { stat } from 'node:fs/promises';
import type { ReadStream } from 'node:fs';
import { Readable } from 'node:stream';
import {
    Client,
    GatewayIntentBits,
    ActivityType,
    ChannelType,
    type Guild,
} from 'discord.js';
import {
    joinVoiceChannel,
    createAudioPlayer,
    createAudioResource,
    StreamType,
    VoiceConnectionStatus,
    AudioPlayerStatus,
    getVoiceConnection,
    entersState,
    type VoiceConnection,
    type AudioPlayer,
    type AudioResource,
} from '@discordjs/voice';
import { Reader as WavReader, type WavFormat } from 'wav';

import * as log from './file-log.js';
import type { Host, Notifier, OpResult } from './pipe-server.js';
import type { LogLevel } from './protocol.js';
import { WavCache } from './wav-cache.js';

const TARGET_SAMPLE_RATE = 48000;
const TARGET_BITS = 16;
const WAV_FORMAT_PCM = 1;

function bufferAsStream(buf: Buffer): Readable {
    return new Readable({
        read() { this.push(buf); this.push(null); },
    });
}

// Mono → stereo by sample duplication (L = R = source). 16-bit signed LE.
// Exported for unit tests; not part of the public Host interface.
export function upmixMonoToStereo16(monoPcm: Buffer): Buffer {
    const sampleCount = monoPcm.length >>> 1;
    const out = Buffer.alloc(sampleCount * 4);
    for (let i = 0; i < sampleCount; i++) {
        const s = monoPcm.readInt16LE(i * 2);
        out.writeInt16LE(s, i * 4);
        out.writeInt16LE(s, i * 4 + 2);
    }
    return out;
}

// Linear-interpolation sample rate conversion for 16-bit signed LE stereo.
// Quality is fine for short trigger sounds going through Opus at 48k; do not
// reuse this for music/long-form audio without swapping to a polyphase filter.
// Exported for unit tests.
export function resampleStereo16(pcm: Buffer, srcRate: number, dstRate: number): Buffer {
    if (srcRate === dstRate) return pcm;
    const srcFrames = pcm.length >>> 2;
    const ratio = dstRate / srcRate;
    const dstFrames = Math.max(1, Math.floor(srcFrames * ratio));
    const out = Buffer.alloc(dstFrames * 4);
    const lastSrc = srcFrames - 1;
    for (let i = 0; i < dstFrames; i++) {
        const srcPos = i / ratio;
        const srcIdx = Math.floor(srcPos);
        const nextIdx = srcIdx < lastSrc ? srcIdx + 1 : lastSrc;
        const frac = srcPos - srcIdx;
        const l1 = pcm.readInt16LE(srcIdx * 4);
        const l2 = pcm.readInt16LE(nextIdx * 4);
        const r1 = pcm.readInt16LE(srcIdx * 4 + 2);
        const r2 = pcm.readInt16LE(nextIdx * 4 + 2);
        out.writeInt16LE(Math.round(l1 + (l2 - l1) * frac), i * 4);
        out.writeInt16LE(Math.round(r1 + (r2 - r1) * frac), i * 4 + 2);
    }
    return out;
}

type ResourceFactory = () => AudioResource;

export class DiscordHost implements Host {
    private client: Client | null = null;
    private statusMsg = '';
    private notify: Notifier | null = null;
    private connection: VoiceConnection | null = null;
    private player: AudioPlayer | null = null;
    private queue: ResourceFactory[] = [];
    private currentGuildId: string | null = null;
    private pingTimer: NodeJS.Timeout | null = null;
    private readonly wavCache = new WavCache();

    setNotifier(fn: Notifier): void { this.notify = fn; }

    async init(token: string, status: string): Promise<OpResult> {
        if (this.client) {
            log.info('init: client already created, returning ok');
            return { ok: true, error: '' };
        }
        try {
            log.info('init: creating Client');
            this.statusMsg = status || '';
            this.client = new Client({
                intents: [GatewayIntentBits.Guilds, GatewayIntentBits.GuildVoiceStates],
            });

            this.client.on('error', (err: Error) => {
                log.error('client error', err);
                this._sendLog('Error', `client error: ${err.message}`);
            });

            this.client.on('warn', (msg: string) => {
                log.warn('client warn: ' + msg);
                this._sendLog('Warn', msg);
            });

            this.client.on('shardDisconnect', (event: { code?: number }, shardId: number) => {
                const reason = `shard ${shardId} disconnected (code ${event?.code ?? '?'})`;
                log.info(reason);
                if (this.notify) this.notify({ op: 'Disconnected', reason });
            });

            this.client.once('clientReady', async (client: Client<true>) => {
                try {
                    await this._applyStatus();
                } catch (e) {
                    log.error('clientReady setActivity failed', e);
                }
                if (this.notify) this.notify({ op: 'BotReady' });
                log.info(`clientReady: logged in as ${client.user.tag}`);
            });

            log.info('init: login starting');
            await this.client.login(token);
            log.info('init: login ok');
            return { ok: true, error: '' };
        } catch (e) {
            log.error('init failed', e);
            try { this.client?.destroy(); } catch { /* ignore */ }
            this.client = null;
            return { ok: false, error: log.errMsg(e) };
        }
    }

    async deinit(): Promise<void> {
        log.info('deinit');
        try { await this.leaveChannel(); } catch { /* ignore */ }
        try { await this.client?.destroy(); } catch { /* ignore */ }
        this.client = null;
    }

    isConnected(): boolean {
        try { return this.client?.isReady() ?? false; } catch { return false; }
    }

    getServers(): string[] {
        if (!this.client) return [];
        return [...this.client.guilds.cache.values()].map(g => g.name);
    }

    getChannels(serverName: string): string[] {
        if (!this.client) return [];
        const guild = this._findGuild(serverName);
        if (!guild) return [];
        return [...guild.channels.cache.values()]
            .filter(c => c.isVoiceBased() && c.type !== ChannelType.GuildStageVoice)
            .sort((a, b) => (a.position ?? 0) - (b.position ?? 0))
            .map(c => c.name);
    }

    async setGame(text: string): Promise<void> {
        this.statusMsg = (text && text.trim().length > 0) ? text.trim() : 'Playing with ACT Triggers';
        await this._applyStatus();
    }

    private async _applyStatus(): Promise<void> {
        if (!this.client?.user) return;
        try {
            this.client.user.setActivity(this.statusMsg, { type: ActivityType.Custom });
        } catch (e) {
            log.warn('setActivity failed: ' + log.errMsg(e));
        }
    }

    async joinChannel(serverName: string, channelName: string): Promise<OpResult> {
        log.info(`joinChannel: server='${serverName}' channel='${channelName}'`);
        const guild = this._findGuild(serverName);
        if (!guild) return { ok: false, error: `Server '${serverName}' not found.` };
        const channel = [...guild.channels.cache.values()].find(c =>
            c.isVoiceBased() && c.name === channelName);
        if (!channel) return { ok: false, error: `Voice channel '${channelName}' not found in server '${serverName}'.` };

        try {
            const existing = getVoiceConnection(guild.id);
            if (existing) {
                log.info('joinChannel: leaving existing connection first');
                await this.leaveChannel();
            }

            log.info('joinChannel: joinVoiceChannel + DAVE handshake');
            this.connection = joinVoiceChannel({
                channelId: channel.id,
                guildId: guild.id,
                adapterCreator: guild.voiceAdapterCreator,
                selfDeaf: true,
                selfMute: false,
            });
            this.currentGuildId = guild.id;

            this.connection.on('stateChange', (oldS, newS) => {
                log.info(`voice ${oldS.status} -> ${newS.status}`);
            });
            this.connection.on('error', (err: Error) => {
                log.error('voice connection error', err);
                this._sendLog('Error', `voice: ${err.message}`);
            });

            await entersState(this.connection, VoiceConnectionStatus.Ready, 30_000);
            log.info('joinChannel: voice Ready');
            this._startPingLog();

            this.player = createAudioPlayer();
            this.player.on('stateChange', (oldS, newS) => {
                log.info(`player ${oldS.status} -> ${newS.status}`);
                if (newS.status === AudioPlayerStatus.Idle) this._tryDrain();
            });
            this.player.on('error', (err: Error) => {
                log.error('player error', err);
                this._sendLog('Error', `player: ${err.message}`);
            });
            this.connection.subscribe(this.player);

            return { ok: true, error: '' };
        } catch (e) {
            log.error('joinChannel failed', e);
            return { ok: false, error: log.errMsg(e) };
        }
    }

    async leaveChannel(): Promise<void> {
        log.info('leaveChannel');
        this._stopPingLog();
        this.queue.length = 0;
        try { this.player?.stop(true); } catch { /* ignore */ }
        this.player = null;
        try {
            if (this.currentGuildId) {
                const conn = getVoiceConnection(this.currentGuildId);
                conn?.destroy();
            } else if (this.connection) {
                this.connection.destroy();
            }
        } catch { /* ignore */ }
        this.connection = null;
        this.currentGuildId = null;
    }

    speakPcm(pcmBuffer: Buffer): OpResult {
        const guard = this._guardPlayback();
        if (!guard.ok) return guard;
        this.queue.push(() => createAudioResource(bufferAsStream(pcmBuffer), {
            inputType: StreamType.Raw,
        }));
        this._tryDrain();
        return { ok: true, error: '' };
    }

    async speakFile(path: string): Promise<OpResult> {
        const guard = this._guardPlayback();
        if (!guard.ok) return guard;

        // stat first so we can short-circuit on a cache hit and let mtime
        // invalidate stale entries when the user edits the file in place.
        let mtimeMs: number;
        try {
            const st = await stat(path);
            mtimeMs = st.mtimeMs;
        } catch (e) {
            return { ok: false, error: `Cannot read file: ${log.errMsg(e)}` };
        }

        const cachedPcm = this.wavCache.get(path, mtimeMs);
        if (cachedPcm) {
            log.info(`SpeakFile cache hit: ${path} (${cachedPcm.length} bytes)`);
            this.queue.push(() => createAudioResource(bufferAsStream(cachedPcm), {
                inputType: StreamType.Raw,
            }));
            this._tryDrain();
            return { ok: true, error: '' };
        }

        let fileStream: ReadStream;
        try {
            fileStream = createReadStream(path);
        } catch (e) {
            return { ok: false, error: `Cannot open file: ${log.errMsg(e)}` };
        }

        const reader = new WavReader();

        try {
            const decoded = await new Promise<{ format: WavFormat; pcm: Buffer }>((resolve, reject) => {
                const chunks: Buffer[] = [];
                let captured: WavFormat | null = null;
                reader.once('format', (fmt) => { captured = fmt; });
                reader.on('data', (c: Buffer) => { chunks.push(c); });
                reader.once('end', () => {
                    if (!captured) reject(new Error('WAV ended before fmt header was parsed'));
                    else resolve({ format: captured, pcm: Buffer.concat(chunks) });
                });
                reader.once('error', reject);
                fileStream.once('error', reject);
                fileStream.pipe(reader);
            });

            const { format, pcm } = decoded;

            // Hard rejects: things we can't convert without a heavier toolchain.
            // 16-bit signed PCM, mono or stereo, covers ~all real-world trigger WAVs
            // (CD audio, Audacity defaults, in-game effect exports). For 24/32-bit or
            // compressed WAV the user can re-export once.
            if (format.audioFormat !== WAV_FORMAT_PCM) {
                return { ok: false, error: `WAV must be uncompressed PCM (audioFormat=${format.audioFormat})` };
            }
            if (format.bitDepth !== TARGET_BITS) {
                return { ok: false, error: `WAV must be 16-bit (got ${format.bitDepth}-bit). Re-export from Audacity as "16-bit PCM".` };
            }
            if (format.channels !== 1 && format.channels !== 2) {
                return { ok: false, error: `WAV must be mono or stereo (got ${format.channels} channels)` };
            }

            // Channel + sample-rate conversion to the format the bridge feeds Discord.
            const stereoPcm = format.channels === 1 ? upmixMonoToStereo16(pcm) : pcm;
            const finalPcm = resampleStereo16(stereoPcm, format.sampleRate, TARGET_SAMPLE_RATE);

            this.wavCache.set(path, mtimeMs, finalPcm);

            this.queue.push(() => createAudioResource(bufferAsStream(finalPcm), {
                inputType: StreamType.Raw,
            }));
            this._tryDrain();
            return { ok: true, error: '' };
        } catch (e) {
            try { fileStream.destroy(); } catch { /* ignore */ }
            try { reader.destroy(); } catch { /* ignore */ }
            return { ok: false, error: `Failed to read WAV: ${log.errMsg(e)}` };
        }
    }

    private _startPingLog(): void {
        this._stopPingLog();
        const tick = (): void => {
            if (!this.connection) return;
            try {
                // VoiceConnection.ping is { ws, udp } in @discordjs/voice 0.18+.
                // ws = voice gateway heartbeat RTT. udp may be undefined for
                // DAVE-encrypted connections — omit it from the log when so.
                const p = this.connection.ping;
                const parts: string[] = [];
                if (typeof p.ws === 'number') parts.push(`ws=${p.ws}ms`);
                if (typeof p.udp === 'number') parts.push(`udp=${p.udp}ms`);
                if (parts.length > 0) log.info(`Discord voice RTT: ${parts.join(' ')}`);
            } catch (e) {
                log.warn('voice ping unavailable: ' + log.errMsg(e));
            }
        };
        // Wait 5s for the first WS heartbeat to populate before logging, then
        // sample every 60s. Both timers go through `pingTimer` so _stopPingLog
        // cancels whichever is currently scheduled.
        this.pingTimer = setTimeout(() => {
            tick();
            this.pingTimer = setInterval(tick, 60_000);
            if (this.pingTimer.unref) this.pingTimer.unref();
        }, 5_000);
        if (this.pingTimer.unref) this.pingTimer.unref();
    }

    private _stopPingLog(): void {
        if (this.pingTimer) {
            // Node's Timer object accepts either clearTimeout or clearInterval
            // regardless of which scheduled it, so calling both is safe.
            clearTimeout(this.pingTimer);
            clearInterval(this.pingTimer);
            this.pingTimer = null;
        }
    }

    private _guardPlayback(): OpResult {
        if (!this.connection || this.connection.state.status !== VoiceConnectionStatus.Ready) {
            return { ok: false, error: 'Not connected to a voice channel.' };
        }
        if (!this.player) {
            return { ok: false, error: 'Audio player not ready.' };
        }
        return { ok: true, error: '' };
    }

    private _tryDrain(): void {
        if (!this.player) return;
        if (this.player.state.status !== AudioPlayerStatus.Idle) return;
        // Loop, not recursion: a queue full of bad factories would blow the stack.
        while (this.queue.length > 0) {
            const factory = this.queue.shift();
            if (!factory) continue;
            try {
                const resource = factory();
                this.player.play(resource);
                return;
            } catch (e) {
                log.error('play failed, dropping resource', e);
            }
        }
    }

    private _findGuild(name: string): Guild | null {
        if (!this.client) return null;
        for (const g of this.client.guilds.cache.values()) {
            if (g.name === name) return g;
        }
        return null;
    }

    private _sendLog(level: LogLevel, message: string): void {
        if (this.notify) {
            try { this.notify({ op: 'Log', level, message }); } catch { /* ignore */ }
        }
    }
}
