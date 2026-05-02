'use strict';

const { Readable } = require('node:stream');
const { Client, GatewayIntentBits, ActivityType, ChannelType } = require('discord.js');
const {
    joinVoiceChannel,
    createAudioPlayer,
    createAudioResource,
    StreamType,
    VoiceConnectionStatus,
    AudioPlayerStatus,
    getVoiceConnection,
    entersState,
} = require('@discordjs/voice');

const log = require('./file-log');

function bufferAsStream(buf) {
    return new Readable({
        read() { this.push(buf); this.push(null); }
    });
}

class DiscordHost {
    constructor() {
        this.client = null;
        this.statusMsg = '';
        this.notify = null;
        this.connection = null;
        this.player = null;
        this.queue = [];
        this.currentGuildId = null;
    }

    setNotifier(fn) { this.notify = fn; }

    async init(token, status) {
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

            this.client.on('error', (err) => {
                log.error('client error', err);
                this._sendLog('Error', `client error: ${err.message}`);
            });

            this.client.on('warn', (msg) => {
                log.warn('client warn: ' + msg);
                this._sendLog('Warn', msg);
            });

            this.client.on('shardDisconnect', (event, shardId) => {
                const reason = `shard ${shardId} disconnected (code ${event?.code ?? '?'})`;
                log.info(reason);
                if (this.notify) this.notify({ op: 'Disconnected', reason });
            });

            this.client.once('clientReady', async () => {
                try {
                    await this._applyStatus();
                } catch (e) {
                    log.error('clientReady setActivity failed', e);
                }
                if (this.notify) this.notify({ op: 'BotReady' });
                log.info(`clientReady: logged in as ${this.client.user?.tag}`);
            });

            log.info('init: login starting');
            await this.client.login(token);
            log.info('init: login ok');
            return { ok: true, error: '' };
        } catch (e) {
            log.error('init failed', e);
            try { this.client?.destroy(); } catch { }
            this.client = null;
            return { ok: false, error: e.message || String(e) };
        }
    }

    async deinit() {
        log.info('deinit');
        try { await this.leaveChannel(); } catch { }
        try { this.client?.destroy(); } catch { }
        this.client = null;
    }

    isConnected() {
        try { return this.client?.isReady() ?? false; } catch { return false; }
    }

    getServers() {
        if (!this.client) return [];
        return [...this.client.guilds.cache.values()].map(g => g.name);
    }

    getChannels(serverName) {
        if (!this.client) return [];
        const guild = this._findGuild(serverName);
        if (!guild) return [];
        const voice = [...guild.channels.cache.values()]
            .filter(c => c.isVoiceBased && c.isVoiceBased() && c.type !== ChannelType.GuildStageVoice)
            .sort((a, b) => (a.position ?? 0) - (b.position ?? 0));
        return voice.map(c => c.name);
    }

    async setGame(text) {
        this.statusMsg = (text && text.trim().length > 0) ? text.trim() : 'Playing with ACT Triggers';
        await this._applyStatus();
    }

    async _applyStatus() {
        if (!this.client?.user) return;
        try {
            this.client.user.setActivity(this.statusMsg, { type: ActivityType.Custom });
        } catch (e) {
            log.warn('setActivity failed: ' + e.message);
        }
    }

    async joinChannel(serverName, channelName) {
        log.info(`joinChannel: server='${serverName}' channel='${channelName}'`);
        const guild = this._findGuild(serverName);
        if (!guild) return { ok: false, error: `Server '${serverName}' not found.` };
        const channel = [...guild.channels.cache.values()].find(c =>
            c.isVoiceBased && c.isVoiceBased() && c.name === channelName);
        if (!channel) return { ok: false, error: `Voice channel '${channelName}' not found in server '${serverName}'.` };

        try {
            // If already connected to a different channel, leave first.
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
            this.connection.on('error', (err) => {
                log.error('voice connection error', err);
                this._sendLog('Error', `voice: ${err.message}`);
            });

            await entersState(this.connection, VoiceConnectionStatus.Ready, 30_000);
            log.info('joinChannel: voice Ready');

            this.player = createAudioPlayer();
            this.player.on('stateChange', (oldS, newS) => {
                log.info(`player ${oldS.status} -> ${newS.status}`);
                if (newS.status === AudioPlayerStatus.Idle) this._tryDrain();
            });
            this.player.on('error', (err) => {
                log.error('player error', err);
                this._sendLog('Error', `player: ${err.message}`);
            });
            this.connection.subscribe(this.player);

            return { ok: true, error: '' };
        } catch (e) {
            log.error('joinChannel failed', e);
            return { ok: false, error: e.message || String(e) };
        }
    }

    async leaveChannel() {
        log.info('leaveChannel');
        this.queue.length = 0;
        try { this.player?.stop(true); } catch { }
        this.player = null;
        try {
            if (this.currentGuildId) {
                const conn = getVoiceConnection(this.currentGuildId);
                conn?.destroy();
            } else if (this.connection) {
                this.connection.destroy();
            }
        } catch { }
        this.connection = null;
        this.currentGuildId = null;
    }

    speakPcm(pcmBuffer) {
        if (!this.connection || this.connection.state.status !== VoiceConnectionStatus.Ready) {
            return { ok: false, error: 'Not connected to a voice channel.' };
        }
        if (!this.player) {
            return { ok: false, error: 'Audio player not ready.' };
        }
        this.queue.push(pcmBuffer);
        this._tryDrain();
        return { ok: true, error: '' };
    }

    _tryDrain() {
        if (!this.player) return;
        if (this.player.state.status !== AudioPlayerStatus.Idle) return;
        // Loop, not recursion: a queue full of bad buffers would blow the stack.
        while (this.queue.length > 0) {
            const buf = this.queue.shift();
            try {
                const resource = createAudioResource(bufferAsStream(buf), {
                    inputType: StreamType.Raw,
                });
                this.player.play(resource);
                return;
            } catch (e) {
                log.error('play failed, dropping buffer', e);
            }
        }
    }

    _findGuild(name) {
        if (!this.client) return null;
        for (const g of this.client.guilds.cache.values()) {
            if (g.name === name) return g;
        }
        return null;
    }

    _sendLog(level, message) {
        if (this.notify) {
            try { this.notify({ op: 'Log', level, message }); } catch { }
        }
    }
}

module.exports = { DiscordHost };
