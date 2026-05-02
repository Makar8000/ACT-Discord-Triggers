import type { Host, Notifier, OpResult } from '../../src/pipe-server.js';
import type { Notification } from '../../src/protocol.js';

export interface RecordedCall {
    method: string;
    args: unknown[];
}

// Host stub for PipeServer dispatch tests. Records each method call and lets
// tests script return values via `next*` setters. `notify` captures the
// notifier passed to setNotifier so tests can fire push notifications and
// observe them on the socket.
export class FakeHost implements Host {
    public readonly calls: RecordedCall[] = [];
    public notify: Notifier | null = null;

    private _nextInit: OpResult = { ok: true, error: '' };
    private _nextJoinChannel: OpResult = { ok: true, error: '' };
    private _nextSpeakPcm: OpResult = { ok: true, error: '' };
    private _nextIsConnected = false;
    private _nextServers: string[] = [];
    private _nextChannels: string[] = [];

    private _initThrows: Error | null = null;
    private _joinThrows: Error | null = null;
    private _setGameThrows: Error | null = null;
    private _speakPcmThrows: Error | null = null;

    setNotifier(fn: Notifier): void {
        this.calls.push({ method: 'setNotifier', args: [] });
        this.notify = fn;
    }

    async init(token: string, status: string): Promise<OpResult> {
        this.calls.push({ method: 'init', args: [token, status] });
        if (this._initThrows) throw this._initThrows;
        return this._nextInit;
    }

    async deinit(): Promise<void> {
        this.calls.push({ method: 'deinit', args: [] });
    }

    isConnected(): boolean {
        this.calls.push({ method: 'isConnected', args: [] });
        return this._nextIsConnected;
    }

    getServers(): string[] {
        this.calls.push({ method: 'getServers', args: [] });
        return this._nextServers;
    }

    getChannels(serverName: string): string[] {
        this.calls.push({ method: 'getChannels', args: [serverName] });
        return this._nextChannels;
    }

    async setGame(text: string): Promise<void> {
        this.calls.push({ method: 'setGame', args: [text] });
        if (this._setGameThrows) throw this._setGameThrows;
    }

    async joinChannel(serverName: string, channelName: string): Promise<OpResult> {
        this.calls.push({ method: 'joinChannel', args: [serverName, channelName] });
        if (this._joinThrows) throw this._joinThrows;
        return this._nextJoinChannel;
    }

    async leaveChannel(): Promise<void> {
        this.calls.push({ method: 'leaveChannel', args: [] });
    }

    speakPcm(pcmBuffer: Buffer): OpResult {
        this.calls.push({ method: 'speakPcm', args: [pcmBuffer] });
        if (this._speakPcmThrows) throw this._speakPcmThrows;
        return this._nextSpeakPcm;
    }

    nextInit(r: OpResult): void { this._nextInit = r; }
    nextJoinChannel(r: OpResult): void { this._nextJoinChannel = r; }
    nextSpeakPcm(r: OpResult): void { this._nextSpeakPcm = r; }
    nextIsConnected(v: boolean): void { this._nextIsConnected = v; }
    nextServers(s: string[]): void { this._nextServers = s; }
    nextChannels(c: string[]): void { this._nextChannels = c; }

    initThrows(err: Error): void { this._initThrows = err; }
    joinChannelThrows(err: Error): void { this._joinThrows = err; }
    setGameThrows(err: Error): void { this._setGameThrows = err; }
    speakPcmThrows(err: Error): void { this._speakPcmThrows = err; }

    fireNotification(n: Notification): void {
        if (this.notify) this.notify(n);
    }
}
