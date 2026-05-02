import { test } from 'node:test';
import { strict as assert } from 'node:assert';
import { spawn, type ChildProcess } from 'node:child_process';
import * as net from 'node:net';
import * as path from 'node:path';
import * as readline from 'node:readline';
import { fileURLToPath } from 'node:url';

import { Op, PROTOCOL_VERSION } from '../src/protocol.js';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const pkgRoot = path.resolve(__dirname, '..');
const BRIDGE_ENTRY = path.join('src', 'bridge.ts');

const SKIP_NON_WINDOWS = process.platform !== 'win32';
const skipOpts = { skip: SKIP_NON_WINDOWS ? 'Windows-only (named pipes)' : false };

interface Bridge {
    proc: ChildProcess;
    pipeName: string;
    pipePath: string;
    stdout: string[];
    stderr: string[];
    exited: Promise<number | null>;
}

function makePipeName(suffix: string): string {
    return `bridge-test-${process.pid}-${Date.now()}-${suffix}`;
}

async function spawnBridge(suffix: string): Promise<Bridge> {
    const pipeName = makePipeName(suffix);
    const proc = spawn(
        process.execPath,
        ['--import', 'tsx', BRIDGE_ENTRY, pipeName],
        { cwd: pkgRoot, stdio: ['ignore', 'pipe', 'pipe'] },
    );
    const stdout: string[] = [];
    const stderr: string[] = [];
    if (proc.stderr) {
        const rl = readline.createInterface({ input: proc.stderr });
        rl.on('line', (line) => stderr.push(line));
    }
    const exited = new Promise<number | null>((resolve) => {
        proc.once('exit', (code) => resolve(code));
    });

    await new Promise<void>((resolve, reject) => {
        const timer = setTimeout(
            () => reject(new Error(`BRIDGE_READY timeout (10s). stderr=${stderr.join('\n')}`)),
            10_000,
        );
        if (!proc.stdout) {
            clearTimeout(timer);
            reject(new Error('proc.stdout missing'));
            return;
        }
        const rl = readline.createInterface({ input: proc.stdout });
        rl.on('line', (line) => {
            stdout.push(line);
            if (line.startsWith('BRIDGE_READY')) {
                clearTimeout(timer);
                resolve();
            }
        });
        proc.once('error', (err) => { clearTimeout(timer); reject(err); });
        proc.once('exit', (code) => {
            clearTimeout(timer);
            reject(new Error(`bridge exited (code=${code}) before BRIDGE_READY. stderr=${stderr.join('\n')}`));
        });
    });

    return {
        proc, pipeName,
        pipePath: `\\\\.\\pipe\\${pipeName}`,
        stdout, stderr, exited,
    };
}

async function killIfAlive(bridge: Bridge): Promise<void> {
    if (bridge.proc.exitCode === null && bridge.proc.signalCode === null) {
        try { bridge.proc.kill(); } catch { /* ignore */ }
        await Promise.race([
            bridge.exited,
            new Promise<void>((r) => setTimeout(r, 3000)),
        ]);
    }
}

class TestPipeClient {
    private buf = Buffer.alloc(0);
    private waiters = new Map<number, (msg: Record<string, unknown>) => void>();
    private nextReqId = 1;

    private constructor(public readonly socket: net.Socket) {
        socket.on('data', (chunk: Buffer) => this._onData(chunk));
    }

    static async connect(pipePath: string): Promise<TestPipeClient> {
        const socket = net.createConnection(pipePath);
        await new Promise<void>((resolve, reject) => {
            socket.once('connect', () => resolve());
            socket.once('error', reject);
        });
        return new TestPipeClient(socket);
    }

    private _onData(chunk: Buffer): void {
        this.buf = Buffer.concat([this.buf, chunk]);
        while (this.buf.length >= 4) {
            const len = this.buf.readUInt32LE(0);
            if (this.buf.length < 4 + len) break;
            const json = this.buf.subarray(4, 4 + len).toString('utf8');
            this.buf = this.buf.subarray(4 + len);
            let obj: Record<string, unknown>;
            try { obj = JSON.parse(json) as Record<string, unknown>; } catch { continue; }
            const reqId = obj['reqId'];
            if (typeof reqId === 'number') {
                const w = this.waiters.get(reqId);
                if (w) {
                    this.waiters.delete(reqId);
                    w(obj);
                }
            }
        }
    }

    send(op: string, fields: Record<string, unknown> = {}): Promise<Record<string, unknown>> {
        const reqId = this.nextReqId++;
        const msg = { op, reqId, ...fields };
        const json = Buffer.from(JSON.stringify(msg), 'utf8');
        const len = Buffer.alloc(4);
        len.writeUInt32LE(json.length, 0);
        return new Promise((resolve, reject) => {
            const timer = setTimeout(() => {
                this.waiters.delete(reqId);
                reject(new Error(`response timeout for ${op} reqId=${reqId}`));
            }, 5000);
            this.waiters.set(reqId, (m) => { clearTimeout(timer); resolve(m); });
            this.socket.write(len);
            this.socket.write(json);
        });
    }

    sendNoReply(op: string, fields: Record<string, unknown> = {}): void {
        const msg = { op, reqId: null, ...fields };
        const json = Buffer.from(JSON.stringify(msg), 'utf8');
        const len = Buffer.alloc(4);
        len.writeUInt32LE(json.length, 0);
        this.socket.write(len);
        this.socket.write(json);
    }

    close(): void {
        this.socket.destroy();
    }
}

// ----------------------------------------------------------------------------
// Tests
// ----------------------------------------------------------------------------

test('lifecycle: bridge prints BRIDGE_READY pipe=<name> on stdout', skipOpts, async () => {
    const bridge = await spawnBridge('ready');
    try {
        const readyLine = bridge.stdout.find((l) => l.startsWith('BRIDGE_READY'));
        assert.ok(readyLine);
        assert.equal(readyLine, `BRIDGE_READY pipe=${bridge.pipeName}`);
    } finally {
        await killIfAlive(bridge);
    }
});

test('lifecycle: Hello handshake with matching version succeeds', skipOpts, async () => {
    const bridge = await spawnBridge('hello-ok');
    try {
        const client = await TestPipeClient.connect(bridge.pipePath);
        const resp = await client.send(Op.Hello, { protocolVersion: PROTOCOL_VERSION });
        assert.equal(resp['op'], Op.HelloResult);
        assert.equal(resp['ok'], true);
        assert.equal(typeof resp['bridgeVersion'], 'string');
        assert.equal(resp['error'], '');
        client.close();
    } finally {
        await killIfAlive(bridge);
    }
});

test('lifecycle: Hello with wrong version returns ok=false; bridge stays responsive', skipOpts, async () => {
    const bridge = await spawnBridge('hello-bad');
    try {
        const client = await TestPipeClient.connect(bridge.pipePath);
        const bad = await client.send(Op.Hello, { protocolVersion: 999 });
        assert.equal(bad['ok'], false);
        assert.match(String(bad['error']), /Protocol version mismatch/);
        // Same connection: subsequent ops still work.
        const conn = await client.send(Op.IsConnected);
        assert.equal(conn['op'], Op.IsConnectedResult);
        assert.equal(conn['connected'], false);
        client.close();
    } finally {
        await killIfAlive(bridge);
    }
});

test('lifecycle: Shutdown op causes bridge to exit with code 0', skipOpts, async () => {
    const bridge = await spawnBridge('shutdown');
    try {
        const client = await TestPipeClient.connect(bridge.pipePath);
        await client.send(Op.Hello, { protocolVersion: PROTOCOL_VERSION });
        client.sendNoReply(Op.Shutdown);
        const code = await Promise.race([
            bridge.exited,
            new Promise<number | null>((_r, rej) => setTimeout(
                () => rej(new Error(`Shutdown timeout. stderr=${bridge.stderr.join('\n')}`)),
                5000,
            )),
        ]);
        assert.equal(code, 0);
    } finally {
        await killIfAlive(bridge);
    }
});

test('lifecycle: peer disconnect (without Shutdown) causes bridge to exit', skipOpts, async () => {
    const bridge = await spawnBridge('disconnect');
    try {
        const client = await TestPipeClient.connect(bridge.pipePath);
        await client.send(Op.Hello, { protocolVersion: PROTOCOL_VERSION });
        client.close();
        const code = await Promise.race([
            bridge.exited,
            new Promise<number | null>((_r, rej) => setTimeout(
                () => rej(new Error(`Disconnect-exit timeout. stderr=${bridge.stderr.join('\n')}`)),
                5000,
            )),
        ]);
        // Bridge exits cleanly (code 0); mirrors the .NET-side ReadFrameAsync→null behavior.
        assert.equal(code, 0);
    } finally {
        await killIfAlive(bridge);
    }
});

test('lifecycle: GetServers before Init returns empty array', skipOpts, async () => {
    const bridge = await spawnBridge('getservers');
    try {
        const client = await TestPipeClient.connect(bridge.pipePath);
        await client.send(Op.Hello, { protocolVersion: PROTOCOL_VERSION });
        const resp = await client.send(Op.GetServers);
        assert.equal(resp['op'], Op.GetServersResult);
        assert.deepEqual(resp['servers'], []);
        client.close();
    } finally {
        await killIfAlive(bridge);
    }
});
