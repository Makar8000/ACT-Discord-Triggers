import type { Socket } from 'node:net';

import * as log from './file-log.js';
import {
    Op,
    PROTOCOL_VERSION,
    MAX_FRAME_BYTES,
    FRAME_JSON_MARKER,
    FRAME_BINARY_SPEAK_PCM,
    BINARY_SPEAK_PCM_HEADER_BYTES,
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
    speakFile(path: string): Promise<OpResult>;
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
            const first = payload[0];
            // Fire-and-forget so a slow handler doesn't block reads.
            if (first === FRAME_JSON_MARKER) {
                this._handleJsonFrame(payload.toString('utf8'))
                    .catch((e: unknown) => log.error('json handler crash', e));
            } else if (first === FRAME_BINARY_SPEAK_PCM) {
                // Copy out of the read buffer slice so subsequent reads don't
                // overwrite the bytes the audio player is holding onto.
                const frame = Buffer.from(payload);
                this._handleBinarySpeakPcm(frame)
                    .catch((e: unknown) => log.error('binary handler crash', e));
            } else {
                log.error(`unknown frame marker 0x${(first ?? 0).toString(16)}; dropping`);
            }
        }
    }

    private async _handleBinarySpeakPcm(payload: Buffer): Promise<void> {
        if (payload.length < BINARY_SPEAK_PCM_HEADER_BYTES) {
            log.error(`binary SpeakPcm frame too short: ${payload.length} bytes`);
            return;
        }
        const reqId = payload.readUInt32LE(1);
        const sampleRate = payload.readUInt32LE(5);
        const bits = payload.readUInt8(9);
        const channels = payload.readUInt8(10);
        const pcm = payload.subarray(BINARY_SPEAK_PCM_HEADER_BYTES);
        log.info(`--> SpeakPcm reqId=${reqId} pcmBytes=${pcm.length} fmt=${sampleRate}/${bits}/${channels}`);
        // Bridge audio path is hard-wired to 48 kHz / 16-bit / stereo end-to-end
        // (see CLAUDE.md "Audio format constraint"). Reject mismatched payloads
        // up front rather than feeding the mixer something it would replay at
        // the wrong rate or with garbled framing.
        if (sampleRate !== 48000 || bits !== 16 || channels !== 2) {
            await this._sendFrame({
                op: Op.SpeakResult, reqId, ok: false,
                error: `Unsupported PCM format: ${sampleRate}/${bits}/${channels}; expected 48000/16/2`,
            });
            return;
        }
        try {
            const r = this.host.speakPcm(pcm);
            await this._sendFrame({ op: Op.SpeakResult, reqId, ok: r.ok, error: r.error });
        } catch (e) {
            const message = e instanceof Error ? e.message : String(e);
            log.error(`SpeakPcm handler threw: ${message}`);
            await this._sendFrame({ op: Op.Log, level: 'Error', message: `Handler 'SpeakPcm' threw: ${message}` });
            await this._sendFrame({ op: Op.SpeakResult, reqId, ok: false, error: message });
        }
    }

    private async _handleJsonFrame(json: string): Promise<void> {
        let op = '?';
        let reqId: ReqId = null;
        try {
            const parsed: unknown = JSON.parse(json);
            // Pull reqId opportunistically *before* shape validation so a
            // malformed-but-reqId-bearing frame can get a synthesized error
            // response via the catch path. C# correlates responses by reqId
            // alone (the op string is ignored at correlation time), so the
            // ?Result op below is an intentional placeholder.
            reqId = asNumber((parsed as { reqId?: unknown } | null)?.reqId);
            if (!isIncomingMessage(parsed)) {
                throw new Error('frame is not an object with op:string');
            }
            op = parsed.op;
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
                case Op.SpeakFile: {
                    const r = await this.host.speakFile(asString(parsed['path']));
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
                // Queue both error frames synchronously so a concurrently-
                // dispatched next handler can't interleave its response
                // between our Log and synthesized Result.
                const pending: Promise<void>[] = [
                    this._sendFrame({ op: Op.Log, level: 'Error', message: `Handler '${op}' threw: ${message}` }),
                ];
                if (reqId !== null) {
                    // SpeakFile's success op is `SpeakResult`, not `SpeakFileResult`. Keep the
                    // error frame symmetric with the success path so dispatchers that key on
                    // `op` (not just reqId) still match.
                    const resultOp: OpName = op === Op.SpeakFile ? Op.SpeakResult : ((op + 'Result') as OpName);
                    pending.push(this._sendFrame({ op: resultOp, reqId, ok: false, error: message }));
                }
                await Promise.all(pending);
            } catch { /* ignore */ }
        }
    }

    private _sendFrame(obj: OutboundFrame): Promise<void> {
        // Serialize writes so frames can't interleave. Length + body go in a
        // single socket.write so the kernel either flushes both or fails both —
        // a torn frame (header without body) is impossible with one syscall.
        this.writeQueue = this.writeQueue.then(() => new Promise<void>((resolve) => {
            if (this.closed || !this.socket.writable) { resolve(); return; }
            try {
                const json = Buffer.from(JSON.stringify(obj), 'utf8');
                const frame = Buffer.alloc(4 + json.length);
                frame.writeUInt32LE(json.length, 0);
                json.copy(frame, 4);
                this.socket.write(frame, () => resolve());
            } catch (e) {
                log.error('sendFrame failed', e);
                resolve();
            }
        }));
        return this.writeQueue;
    }
}
