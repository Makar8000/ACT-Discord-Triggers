import { createReadStream } from 'node:fs';
import { stat } from 'node:fs/promises';
import { performance } from 'node:perf_hooks';
import type { ReadStream } from 'node:fs';
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
    getVoiceConnection,
    entersState,
    type VoiceConnection,
    type AudioPlayer,
} from '@discordjs/voice';
import { Reader as WavReader, type WavFormat } from 'wav';

import * as log from './file-log.js';
import { applyRandomEffect } from './effects.js';
import { PcmMixer } from './pcm-mixer.js';
import type { Host, Notifier, OpResult, SpeakMeta } from './pipe-server.js';
import type { LogLevel } from './protocol.js';
import { WavCache } from './wav-cache.js';

const TARGET_SAMPLE_RATE = 48000;
const TARGET_BITS = 16;
const WAV_FORMAT_PCM = 1;

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

export class DiscordHost implements Host {
    private client: Client | null = null;
    private statusMsg = '';
    private notify: Notifier | null = null;
    private connection: VoiceConnection | null = null;
    private player: AudioPlayer | null = null;
    private mixer: PcmMixer | null = null;
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

            this.mixer = new PcmMixer();
            // maxMissedFrames: with the mixer's pull-based _read producing
            // a chunk per call, the encoder should never see null. But a
            // GC pause that delays our _read by >100 ms (default tolerance)
            // would otherwise stop the player permanently. Disable the
            // missed-frame stop so transient delays just emit silence.
            this.player = createAudioPlayer({
                behaviors: { maxMissedFrames: Number.MAX_SAFE_INTEGER },
            });
            this.player.on('stateChange', (oldS, newS) => {
                log.info(`player ${oldS.status} -> ${newS.status}`);
            });
            this.player.on('error', (err: Error) => {
                log.error('player error', err);
                this._sendLog('Error', `player: ${err.message}`);
            });
            this.connection.subscribe(this.player);

            // One long-lived resource fed by the mixer. The mixer never
            // ends, so this single play() call drives all subsequent audio
            // (each speakPcm/speakFile just adds a voice into the mixer).
            const resource = createAudioResource(this.mixer, { inputType: StreamType.Raw });
            this.player.play(resource);

            return { ok: true, error: '' };
        } catch (e) {
            log.error('joinChannel failed', e);
            // entersState timeout (or any partial-init throw) leaves a live
            // VoiceConnection registered with @discordjs/voice. leaveChannel
            // is idempotent and tolerates partial state — it'll find the
            // orphan via getVoiceConnection(currentGuildId) and destroy it.
            try { await this.leaveChannel(); } catch { /* ignore */ }
            return { ok: false, error: log.errMsg(e) };
        }
    }

    async leaveChannel(): Promise<void> {
        log.info('leaveChannel');
        this._stopPingLog();
        this.mixer?.clear();
        this.mixer = null;
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

    speakPcm(pcmBuffer: Buffer, meta?: SpeakMeta): OpResult {
        const guard = this._guardPlayback();
        if (!guard.ok) return guard;
        return this._enqueue('SpeakPcm', pcmBuffer, meta);
    }

    // Enqueue a fully-prepared 48k/16/stereo buffer into the mixer and, when a
    // per-trigger meta is present, stamp the local pipeline: recv->enqueue ms
    // (this much was pure program time) plus a voice-RTT snapshot taken at the
    // exact moment of this trigger (#2). The mixer later logs enqueue->firstEmit
    // for the same reqId (#1), closing the gap between "queued" and "on the wire".
    private _enqueue(kind: string, pcm: Buffer, meta?: SpeakMeta): OpResult {
        const reqId = meta?.reqId ?? 0;
        // Optional random sound effect (user opted in; plugin rolled the dice).
        // Applied on the complete buffer here, before it enters the mixer, so the
        // recv->enqueue stamp below includes the DSP time as the program cost it is.
        let buf = pcm;
        if (meta?.fx) {
            try {
                const fx = applyRandomEffect(pcm);
                buf = fx.pcm;
                log.info(`fx reqId=${reqId} effect=${fx.name} ` +
                    `inMs=${this._pcmDurationMs(pcm.length)} outMs=${this._pcmDurationMs(buf.length)}`);
            } catch (e) {
                log.error('random effect failed; playing dry', e);
            }
        }
        const enqueueT = performance.now();
        const r = this.mixer!.addVoice(buf, { id: reqId, enqueueT });
        if (r.dropped > 0) this._sendLog('Warn', `Mixer overflow: dropped ${r.dropped} voice(s)`);
        if (meta) {
            const recvToEnqueue = (enqueueT - meta.recvT).toFixed(1);
            log.info(`${kind} reqId=${reqId} pcmMs=${this._pcmDurationMs(buf.length)} ` +
                `recv->enqueue=${recvToEnqueue}ms ${this._pingStr()}`);
        }
        return { ok: true, error: '' };
    }

    // Bytes of 48k/16-bit/stereo PCM -> clip duration in ms (192 bytes per ms).
    private _pcmDurationMs(bytes: number): number {
        return Math.round(bytes / (TARGET_SAMPLE_RATE * 2 * (TARGET_BITS / 8) / 1000));
    }

    // Voice RTT snapshot for the current connection. udp is the true media-path
    // RTT but is often undefined under DAVE; ws (voice gateway heartbeat) is the
    // fallback network-health signal. A late trigger with healthy rtt points at
    // program/buffering; a late trigger with a spiking rtt points at the bot
    // host's link to Discord. (Listener-side internet stays unobservable here.)
    private _pingStr(): string {
        try {
            const p = this.connection?.ping;
            if (!p) return 'rtt=n/a';
            const parts: string[] = [];
            if (typeof p.udp === 'number') parts.push(`udp=${p.udp}ms`);
            if (typeof p.ws === 'number') parts.push(`ws=${p.ws}ms`);
            return parts.length > 0 ? `rtt[${parts.join(' ')}]` : 'rtt=n/a';
        } catch { return 'rtt=n/a'; }
    }

    async speakFile(path: string, meta?: SpeakMeta): Promise<OpResult> {
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
            return this._enqueue('SpeakFile', cachedPcm, meta);
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
            if (!Number.isFinite(format.sampleRate) || format.sampleRate <= 0 || format.sampleRate > 192000) {
                return { ok: false, error: `WAV sample rate ${format.sampleRate} is out of supported range (1-192000 Hz)` };
            }

            // Channel + sample-rate conversion to the format the bridge feeds Discord.
            const stereoPcm = format.channels === 1 ? upmixMonoToStereo16(pcm) : pcm;
            const finalPcm = resampleStereo16(stereoPcm, format.sampleRate, TARGET_SAMPLE_RATE);

            this.wavCache.set(path, mtimeMs, finalPcm);
            return this._enqueue('SpeakFile', finalPcm, meta);
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
        if (!this.player || !this.mixer) {
            return { ok: false, error: 'Audio player not ready.' };
        }
        return { ok: true, error: '' };
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
