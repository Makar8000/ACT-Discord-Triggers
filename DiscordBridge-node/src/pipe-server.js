'use strict';

const log = require('./file-log');

const PROTOCOL_VERSION = 1;
const MAX_FRAME_BYTES = 64 * 1024 * 1024;
const BRIDGE_VERSION = require('../package.json').version;

const Op = {
    Hello: 'Hello', HelloResult: 'HelloResult',
    Init: 'Init', InitResult: 'InitResult',
    Deinit: 'Deinit', DeinitResult: 'DeinitResult',
    IsConnected: 'IsConnected', IsConnectedResult: 'IsConnectedResult',
    GetServers: 'GetServers', GetServersResult: 'GetServersResult',
    GetChannels: 'GetChannels', GetChannelsResult: 'GetChannelsResult',
    SetGame: 'SetGame', SetGameResult: 'SetGameResult',
    JoinChannel: 'JoinChannel', JoinChannelResult: 'JoinChannelResult',
    LeaveChannel: 'LeaveChannel', LeaveChannelResult: 'LeaveChannelResult',
    SpeakPcm: 'SpeakPcm', SpeakResult: 'SpeakResult',
    Shutdown: 'Shutdown',
    BotReady: 'BotReady', Log: 'Log', Disconnected: 'Disconnected',
};

class PipeServer {
    constructor(socket, host) {
        this.socket = socket;
        this.host = host;
        this.readBuf = Buffer.alloc(0);
        this.writeQueue = Promise.resolve();
        this.closed = false;
    }

    run() {
        this.host.setNotifier((notif) => this._sendFrame(notif));

        this.socket.on('data', (chunk) => {
            this.readBuf = Buffer.concat([this.readBuf, chunk]);
            this._tryReadFrames();
        });
        this.socket.on('error', (err) => {
            log.error('pipe socket error', err);
            this.closed = true;
        });
        this.socket.on('close', () => {
            log.info('pipe closed by peer');
            this.closed = true;
        });
        this.socket.on('end', () => {
            log.info('pipe end (peer half-close)');
        });
    }

    _tryReadFrames() {
        while (this.readBuf.length >= 4) {
            const len = this.readBuf.readUInt32LE(0);
            if (len <= 0 || len > MAX_FRAME_BYTES) {
                log.error(`invalid frame length ${len}; closing pipe`);
                try { this.socket.destroy(); } catch { }
                this.closed = true;
                return;
            }
            if (this.readBuf.length < 4 + len) return;
            const payload = this.readBuf.subarray(4, 4 + len);
            this.readBuf = this.readBuf.subarray(4 + len);
            const json = payload.toString('utf8');
            // Fire-and-forget so a slow handler doesn't block reads.
            this._handleFrame(json).catch((e) => log.error('handler crash', e));
        }
    }

    async _handleFrame(json) {
        let op = '?';
        let reqId = null;
        try {
            const msg = JSON.parse(json);
            op = msg.op || '?';
            reqId = (typeof msg.reqId === 'number') ? msg.reqId : null;
            log.info(`--> ${op} reqId=${reqId} bytes=${json.length}`);

            switch (op) {
                case Op.Hello: {
                    const ok = msg.protocolVersion === PROTOCOL_VERSION;
                    this._sendFrame({
                        op: Op.HelloResult, reqId,
                        ok,
                        bridgeVersion: BRIDGE_VERSION,
                        error: ok ? '' : `Protocol version mismatch: bridge=${PROTOCOL_VERSION}, plugin=${msg.protocolVersion}`,
                    });
                    break;
                }
                case Op.Init: {
                    const r = await this.host.init(msg.token || '', msg.status || '');
                    this._sendFrame({ op: Op.InitResult, reqId, ok: r.ok, error: r.error });
                    break;
                }
                case Op.Deinit: {
                    await this.host.deinit();
                    this._sendFrame({ op: Op.DeinitResult, reqId, ok: true, error: '' });
                    break;
                }
                case Op.IsConnected: {
                    this._sendFrame({ op: Op.IsConnectedResult, reqId, connected: this.host.isConnected() });
                    break;
                }
                case Op.GetServers: {
                    this._sendFrame({ op: Op.GetServersResult, reqId, servers: this.host.getServers() });
                    break;
                }
                case Op.GetChannels: {
                    this._sendFrame({ op: Op.GetChannelsResult, reqId, channels: this.host.getChannels(msg.server || '') });
                    break;
                }
                case Op.SetGame: {
                    await this.host.setGame(msg.text || '');
                    this._sendFrame({ op: Op.SetGameResult, reqId, ok: true, error: '' });
                    break;
                }
                case Op.JoinChannel: {
                    const r = await this.host.joinChannel(msg.server || '', msg.channel || '');
                    this._sendFrame({ op: Op.JoinChannelResult, reqId, ok: r.ok, error: r.error });
                    break;
                }
                case Op.LeaveChannel: {
                    await this.host.leaveChannel();
                    this._sendFrame({ op: Op.LeaveChannelResult, reqId, ok: true, error: '' });
                    break;
                }
                case Op.SpeakPcm: {
                    const pcm = Buffer.from(msg.pcm || '', 'base64');
                    const r = this.host.speakPcm(pcm);
                    this._sendFrame({ op: Op.SpeakResult, reqId, ok: r.ok, error: r.error });
                    break;
                }
                case Op.Shutdown: {
                    log.info('Shutdown requested');
                    try { await this.host.deinit(); } catch { }
                    try { this.socket.end(); } catch { }
                    setImmediate(() => process.exit(0));
                    break;
                }
                default: {
                    this._sendFrame({ op: Op.Log, level: 'Error', message: `Unknown op: ${op}` });
                    break;
                }
            }
        } catch (e) {
            log.error(`handler '${op}' threw`, e);
            try {
                this._sendFrame({ op: Op.Log, level: 'Error', message: `Handler '${op}' threw: ${e.message}` });
                if (reqId !== null) {
                    this._sendFrame({ op: op + 'Result', reqId, ok: false, error: e.message });
                }
            } catch { }
        }
    }

    _sendFrame(obj) {
        // Serialize writes so length+body can't interleave.
        this.writeQueue = this.writeQueue.then(() => new Promise((resolve) => {
            if (this.closed || !this.socket.writable) { resolve(); return; }
            try {
                const json = Buffer.from(JSON.stringify(obj), 'utf8');
                const len = Buffer.alloc(4);
                len.writeUInt32LE(json.length, 0);
                this.socket.write(len, () => {
                    if (this.closed || !this.socket.writable) { resolve(); return; }
                    this.socket.write(json, () => resolve());
                });
            } catch (e) {
                log.error('sendFrame failed', e);
                resolve();
            }
        }));
        return this.writeQueue;
    }
}

module.exports = { PipeServer, Op, PROTOCOL_VERSION };
