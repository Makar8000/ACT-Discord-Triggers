import type { Socket } from 'node:net';

import * as log from './file-log.js';
import {
    Op,
    PROTOCOL_VERSION,
    MAX_FRAME_BYTES,
    type Notification,
    type OpName,
    type OutboundFrame,
    type ReqId,
} from './protocol.js';
import pkg from '../package.json' with { type: 'json' };

const BRIDGE_VERSION: string = pkg.version;

export { Op, PROTOCOL_VERSION };

export type Notifier = (n: Notification) => void;

export interface OpResult { ok: boolean; error: string }

// Minimal surface PipeServer needs from the host. discord-host.ts implements this.
export interface Host {
    setNotifier(fn: Notifier): void;
    init(token: string, status: string): Promise<OpResult>;
    deinit(): Promise<void>;
    isConnected(): boolean;
    getServers(): string[];
    getChannels(serverName: string): string[];
    setGame(text: string): Promise<void>;
    joinChannel(serverName: string, channelName: string): Promise<OpResult>;
    leaveChannel(): Promise<void>;
    speakPcm(pcmBuffer: Buffer): OpResult;
}

interface IncomingMessage {
    op: string;
    reqId?: unknown;
    [k: string]: unknown;
}

function isIncomingMessage(x: unknown): x is IncomingMessage {
    return typeof x === 'object' && x !== null && typeof (x as { op?: unknown }).op === 'string';
}

function asString(v: unknown, fallback = ''): string {
    return typeof v === 'string' ? v : fallback;
}

function asNumber(v: unknown): number | null {
    return typeof v === 'number' ? v : null;
}

export class PipeServer {
    private readonly socket: Socket;
    private readonly host: Host;
    private readBuf: Buffer;
    private writeQueue: Promise<void>;
    private closed: boolean;

    constructor(socket: Socket, host: Host) {
        this.socket = socket;
        this.host = host;
        this.readBuf = Buffer.alloc(0);
        this.writeQueue = Promise.resolve();
        this.closed = false;
    }

    run(): void {
        this.host.setNotifier((notif: Notification) => { void this._sendFrame(notif); });

        this.socket.on('data', (chunk: Buffer) => {
            this.readBuf = Buffer.concat([this.readBuf, chunk]);
            this._tryReadFrames();
        });
        this.socket.on('error', (err: Error) => {
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

    private _tryReadFrames(): void {
        while (this.readBuf.length >= 4) {
            const len = this.readBuf.readUInt32LE(0);
            if (len <= 0 || len > MAX_FRAME_BYTES) {
                log.error(`invalid frame length ${len}; closing pipe`);
                try { this.socket.destroy(); } catch { /* ignore */ }
                this.closed = true;
                return;
            }
            if (this.readBuf.length < 4 + len) return;
            const payload = this.readBuf.subarray(4, 4 + len);
            this.readBuf = this.readBuf.subarray(4 + len);
            const json = payload.toString('utf8');
            // Fire-and-forget so a slow handler doesn't block reads.
            this._handleFrame(json).catch((e: unknown) => log.error('handler crash', e));
        }
    }

    private async _handleFrame(json: string): Promise<void> {
        let op = '?';
        let reqId: ReqId = null;
        try {
            const parsed: unknown = JSON.parse(json);
            if (!isIncomingMessage(parsed)) {
                log.error('frame is not an object with op:string; dropping');
                return;
            }
            op = parsed.op;
            reqId = asNumber(parsed.reqId);
            log.info(`--> ${op} reqId=${reqId} bytes=${json.length}`);

            switch (op) {
                case Op.Hello: {
                    const protocolVersion = asNumber(parsed['protocolVersion']);
                    const ok = protocolVersion === PROTOCOL_VERSION;
                    await this._sendFrame({
                        op: Op.HelloResult, reqId,
                        ok,
                        bridgeVersion: BRIDGE_VERSION,
                        error: ok ? '' : `Protocol version mismatch: bridge=${PROTOCOL_VERSION}, plugin=${protocolVersion}`,
                    });
                    break;
                }
                case Op.Init: {
                    const r = await this.host.init(asString(parsed['token']), asString(parsed['status']));
                    await this._sendFrame({ op: Op.InitResult, reqId, ok: r.ok, error: r.error });
                    break;
                }
                case Op.Deinit: {
                    await this.host.deinit();
                    await this._sendFrame({ op: Op.DeinitResult, reqId, ok: true, error: '' });
                    break;
                }
                case Op.IsConnected: {
                    await this._sendFrame({ op: Op.IsConnectedResult, reqId, connected: this.host.isConnected() });
                    break;
                }
                case Op.GetServers: {
                    await this._sendFrame({ op: Op.GetServersResult, reqId, servers: this.host.getServers() });
                    break;
                }
                case Op.GetChannels: {
                    await this._sendFrame({
                        op: Op.GetChannelsResult, reqId,
                        channels: this.host.getChannels(asString(parsed['server'])),
                    });
                    break;
                }
                case Op.SetGame: {
                    await this.host.setGame(asString(parsed['text']));
                    await this._sendFrame({ op: Op.SetGameResult, reqId, ok: true, error: '' });
                    break;
                }
                case Op.JoinChannel: {
                    const r = await this.host.joinChannel(asString(parsed['server']), asString(parsed['channel']));
                    await this._sendFrame({ op: Op.JoinChannelResult, reqId, ok: r.ok, error: r.error });
                    break;
                }
                case Op.LeaveChannel: {
                    await this.host.leaveChannel();
                    await this._sendFrame({ op: Op.LeaveChannelResult, reqId, ok: true, error: '' });
                    break;
                }
                case Op.SpeakPcm: {
                    const pcm = Buffer.from(asString(parsed['pcm']), 'base64');
                    const r = this.host.speakPcm(pcm);
                    await this._sendFrame({ op: Op.SpeakResult, reqId, ok: r.ok, error: r.error });
                    break;
                }
                case Op.Shutdown: {
                    log.info('Shutdown requested');
                    try { await this.host.deinit(); } catch { /* ignore */ }
                    try { this.socket.end(); } catch { /* ignore */ }
                    setImmediate(() => process.exit(0));
                    break;
                }
                default: {
                    await this._sendFrame({ op: Op.Log, level: 'Error', message: `Unknown op: ${op}` });
                    break;
                }
            }
        } catch (e) {
            log.error(`handler '${op}' threw`, e);
            try {
                const message = e instanceof Error ? e.message : String(e);
                await this._sendFrame({ op: Op.Log, level: 'Error', message: `Handler '${op}' threw: ${message}` });
                if (reqId !== null) {
                    // Synthesize *Result op for the catch-all error response. The wire shape
                    // matches OkResponse on the C# side regardless of which op produced it.
                    await this._sendFrame({ op: (op + 'Result') as OpName, reqId, ok: false, error: message });
                }
            } catch { /* ignore */ }
        }
    }

    private _sendFrame(obj: OutboundFrame): Promise<void> {
        // Serialize writes so length+body can't interleave.
        this.writeQueue = this.writeQueue.then(() => new Promise<void>((resolve) => {
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
