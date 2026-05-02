// integration_test.js — drive the built bridge via IPC against a real Discord bot.
//
// Spawns dist/DiscordBridge.exe, connects to its named pipe, runs through the
// full op set (Hello/Init/GetServers/GetChannels/JoinChannel/SpeakPcm/Leave/Shutdown),
// asserts each response shape is correct, and verifies the bot stays connected
// for the regression-test window (60+ seconds with audio firing).
//
// Required env: BOT_TOKEN, GUILD_NAME, CHANNEL_NAME (note: NAMES, not IDs —
// the IPC contract speaks in human-readable names like the plugin UI).
//
// Run from DiscordBridge-node\:
//   $env:BOT_TOKEN="..."; $env:GUILD_NAME="..."; $env:CHANNEL_NAME="..."; node test/integration_test.js

'use strict';

const { spawn } = require('node:child_process');
const net = require('node:net');
const path = require('node:path');
const readline = require('node:readline');

const TOKEN = process.env.BOT_TOKEN;
const GUILD = process.env.GUILD_NAME;
const CHANNEL = process.env.CHANNEL_NAME;
if (!TOKEN || !GUILD || !CHANNEL) {
    console.error('Set BOT_TOKEN, GUILD_NAME, CHANNEL_NAME env vars');
    process.exit(1);
}

const EXE = path.resolve(__dirname, '..', 'dist', 'DiscordBridge.exe');
const PIPE_NAME = 'integration-test-' + Math.floor(Math.random() * 1e9);
const PIPE_PATH = `\\\\.\\pipe\\${PIPE_NAME}`;

const ts = () => new Date().toISOString();
const log = (...a) => console.log(`[${ts()}]`, ...a);

function makeTone(seconds = 0.5) {
    const samples = 48000 * seconds;
    const buf = Buffer.alloc(samples * 4);
    for (let i = 0; i < samples; i++) {
        const v = Math.round(Math.sin(2 * Math.PI * 440 * i / 48000) * 8000);
        buf.writeInt16LE(v, i * 4);
        buf.writeInt16LE(v, i * 4 + 2);
    }
    return buf;
}

class PipeClient {
    constructor(socket) {
        this.socket = socket;
        this.readBuf = Buffer.alloc(0);
        this.pending = new Map();
        this.notifications = [];
        this.notifSubs = [];
        this.nextReqId = 1;

        socket.on('data', (chunk) => {
            this.readBuf = Buffer.concat([this.readBuf, chunk]);
            this._drain();
        });
        socket.on('error', (e) => log('pipe error:', e.message));
        socket.on('close', () => log('pipe closed'));
    }
    _drain() {
        while (this.readBuf.length >= 4) {
            const len = this.readBuf.readUInt32LE(0);
            if (this.readBuf.length < 4 + len) return;
            const json = this.readBuf.subarray(4, 4 + len).toString('utf8');
            this.readBuf = this.readBuf.subarray(4 + len);
            let msg;
            try { msg = JSON.parse(json); } catch (e) { log('bad json:', json); continue; }
            if (typeof msg.reqId === 'number' && this.pending.has(msg.reqId)) {
                const { resolve } = this.pending.get(msg.reqId);
                this.pending.delete(msg.reqId);
                resolve(msg);
            } else {
                this.notifications.push(msg);
                log('notif:', msg.op, msg.message ?? msg.reason ?? '');
                for (const cb of this.notifSubs) cb(msg);
            }
        }
    }
    send(req, expectResponse = true) {
        const reqId = this.nextReqId++;
        const obj = { ...req, reqId };
        const json = Buffer.from(JSON.stringify(obj), 'utf8');
        const len = Buffer.alloc(4);
        len.writeUInt32LE(json.length, 0);
        this.socket.write(len);
        this.socket.write(json);
        if (!expectResponse) return Promise.resolve(null);
        return new Promise((resolve, reject) => {
            const timer = setTimeout(() => {
                this.pending.delete(reqId);
                reject(new Error(`Timeout for ${req.op} reqId=${reqId}`));
            }, 60_000);
            this.pending.set(reqId, { resolve: (m) => { clearTimeout(timer); resolve(m); } });
        });
    }
    onNotification(cb) { this.notifSubs.push(cb); }
}

function assertEq(label, actual, expected) {
    if (actual !== expected) throw new Error(`${label}: expected ${JSON.stringify(expected)}, got ${JSON.stringify(actual)}`);
    log(`  PASS ${label}: ${JSON.stringify(actual)}`);
}

(async () => {
    log('Spawning bridge:', EXE, PIPE_NAME);
    const proc = spawn(EXE, [PIPE_NAME], { stdio: ['ignore', 'pipe', 'pipe'] });

    const stderr = readline.createInterface({ input: proc.stderr });
    stderr.on('line', (line) => log('bridge stderr:', line));

    const stdout = readline.createInterface({ input: proc.stdout });
    let ready = false;
    stdout.on('line', (line) => {
        log('bridge stdout:', line);
        if (line.startsWith('BRIDGE_READY')) ready = true;
    });

    proc.on('exit', (code) => log(`bridge exited code=${code}`));

    // Wait up to 10s for handshake
    const t0 = Date.now();
    while (!ready && Date.now() - t0 < 10_000) await new Promise(r => setTimeout(r, 100));
    if (!ready) throw new Error('Bridge never sent BRIDGE_READY');

    log('Connecting to pipe:', PIPE_PATH);
    const sock = net.createConnection(PIPE_PATH);
    await new Promise((res, rej) => {
        sock.once('connect', res);
        sock.once('error', rej);
    });
    const client = new PipeClient(sock);
    log('Pipe connected');

    let botReady = false;
    let lastDisconnect = null;
    client.onNotification((m) => {
        if (m.op === 'BotReady') botReady = true;
        if (m.op === 'Disconnected') lastDisconnect = m.reason;
    });

    log('--- Hello ---');
    let r = await client.send({ op: 'Hello', protocolVersion: 1 });
    assertEq('hello.ok', r.ok, true);
    log('  bridgeVersion:', r.bridgeVersion);

    log('--- Init ---');
    r = await client.send({ op: 'Init', token: TOKEN, status: 'ACT integration test' });
    assertEq('init.ok', r.ok, true);

    log('  Waiting for BotReady notification...');
    const t1 = Date.now();
    while (!botReady && Date.now() - t1 < 30_000) await new Promise(r => setTimeout(r, 100));
    if (!botReady) throw new Error('No BotReady within 30s');
    log('  BotReady received');

    log('--- IsConnected ---');
    r = await client.send({ op: 'IsConnected' });
    assertEq('isConnected.connected', r.connected, true);

    log('--- GetServers ---');
    r = await client.send({ op: 'GetServers' });
    log('  servers:', r.servers);
    if (!r.servers.includes(GUILD)) throw new Error(`Guild '${GUILD}' not in server list`);

    log('--- GetChannels ---');
    r = await client.send({ op: 'GetChannels', server: GUILD });
    log('  channels:', r.channels);
    if (!r.channels.includes(CHANNEL)) throw new Error(`Channel '${CHANNEL}' not in channel list`);

    log('--- JoinChannel ---');
    r = await client.send({ op: 'JoinChannel', server: GUILD, channel: CHANNEL });
    assertEq('joinChannel.ok', r.ok, true);

    log('--- SpeakPcm (3 tones, 15s apart) ---');
    const tone = makeTone(0.5);
    const toneB64 = tone.toString('base64');
    for (let i = 0; i < 3; i++) {
        r = await client.send({ op: 'SpeakPcm', pcm: toneB64, sampleRate: 48000, bits: 16, channels: 2 });
        assertEq(`speakPcm[${i}].ok`, r.ok, true);
        if (i < 2) await new Promise(r => setTimeout(r, 15_000));
    }

    log('--- Sustain check (additional 30s) ---');
    await new Promise(r => setTimeout(r, 30_000));

    log('--- IsConnected (after sustain) ---');
    r = await client.send({ op: 'IsConnected' });
    assertEq('isConnected_after.connected', r.connected, true);
    if (lastDisconnect) throw new Error(`Got Disconnected during run: ${lastDisconnect}`);

    log('--- LeaveChannel ---');
    r = await client.send({ op: 'LeaveChannel' });
    assertEq('leaveChannel.ok', r.ok, true);

    log('--- Shutdown ---');
    client.send({ op: 'Shutdown' }, false);

    await new Promise(r => setTimeout(r, 2000));
    sock.end();
    log('=== ALL CHECKS PASSED ===');
    process.exit(0);
})().catch((e) => {
    log('TEST FAILED:', e.stack || e.message);
    process.exit(1);
});
