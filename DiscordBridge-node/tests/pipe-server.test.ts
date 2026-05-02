import { test } from 'node:test';
import { strict as assert } from 'node:assert';
import type { Socket } from 'node:net';

import { PipeServer, Op } from '../src/pipe-server.js';
import { PROTOCOL_VERSION, MAX_FRAME_BYTES } from '../src/protocol.js';
import pkg from '../package.json' with { type: 'json' };
import { FakeSocket } from './helpers/fake-socket.js';
import { FakeHost } from './helpers/fake-host.js';
import { encodeFrame, decodeFrames, lenPrefix } from './helpers/frame.js';

interface Harness {
    sock: FakeSocket;
    host: FakeHost;
    ps: PipeServer;
}

function makeHarness(): Harness {
    const sock = new FakeSocket();
    const host = new FakeHost();
    const ps = new PipeServer(sock as unknown as Socket, host);
    ps.run();
    return { sock, host, ps };
}

// _handleFrame is async fire-and-forget and _sendFrame chains through a write
// queue with setImmediate callbacks. Poll the socket's accumulated writes until
// at least `n` complete frames have landed (or time out).
async function waitForFrames(
    sock: FakeSocket,
    n: number,
    timeoutMs = 1000,
): Promise<Array<Record<string, unknown>>> {
    const deadline = Date.now() + timeoutMs;
    while (Date.now() < deadline) {
        const { frames } = decodeFrames(sock.drainedWrites());
        if (frames.length >= n) return frames;
        await new Promise<void>((r) => setImmediate(r));
    }
    const { frames } = decodeFrames(sock.drainedWrites());
    throw new Error(`Timed out waiting for ${n} frames, got ${frames.length}`);
}

// Convenience: yield a few microtask ticks to let the dispatcher settle.
async function tick(n = 4): Promise<void> {
    for (let i = 0; i < n; i++) await new Promise<void>((r) => setImmediate(r));
}

// ----------------------------------------------------------------------------
// Frame parser
// ----------------------------------------------------------------------------

test('frame parser: single frame in one chunk dispatches', async () => {
    const { sock } = makeHarness();
    sock.emit('data', encodeFrame({ op: Op.Hello, reqId: 1, protocolVersion: PROTOCOL_VERSION }));
    const [frame] = await waitForFrames(sock, 1);
    assert.equal(frame!['op'], Op.HelloResult);
    assert.equal(frame!['reqId'], 1);
    assert.equal(frame!['ok'], true);
});

test('frame parser: frame split across two chunks reassembles', async () => {
    const { sock } = makeHarness();
    const buf = encodeFrame({ op: Op.Hello, reqId: 7, protocolVersion: PROTOCOL_VERSION });
    sock.emit('data', buf.subarray(0, 6));
    await tick(2);
    assert.equal(sock.writes.length, 0, 'no response before full frame arrives');
    sock.emit('data', buf.subarray(6));
    const [frame] = await waitForFrames(sock, 1);
    assert.equal(frame!['reqId'], 7);
});

test('frame parser: frame split byte-by-byte reassembles', async () => {
    const { sock } = makeHarness();
    const buf = encodeFrame({ op: Op.Hello, reqId: 3, protocolVersion: PROTOCOL_VERSION });
    for (const b of buf) sock.emit('data', Buffer.from([b]));
    const [frame] = await waitForFrames(sock, 1);
    assert.equal(frame!['reqId'], 3);
});

test('frame parser: two concatenated frames in one chunk dispatch in order', async () => {
    const { sock } = makeHarness();
    const a = encodeFrame({ op: Op.Hello, reqId: 1, protocolVersion: PROTOCOL_VERSION });
    const b = encodeFrame({ op: Op.IsConnected, reqId: 2 });
    sock.emit('data', Buffer.concat([a, b]));
    const frames = await waitForFrames(sock, 2);
    assert.equal(frames[0]!['op'], Op.HelloResult);
    assert.equal(frames[0]!['reqId'], 1);
    assert.equal(frames[1]!['op'], Op.IsConnectedResult);
    assert.equal(frames[1]!['reqId'], 2);
});

test('frame parser: length=0 destroys the socket', async () => {
    const { sock } = makeHarness();
    sock.emit('data', lenPrefix(0));
    await tick();
    assert.equal(sock.destroyed, true);
    assert.equal(sock.writes.length, 0);
});

test('frame parser: length > MAX_FRAME_BYTES destroys the socket', async () => {
    const { sock } = makeHarness();
    sock.emit('data', lenPrefix(MAX_FRAME_BYTES + 1));
    await tick();
    assert.equal(sock.destroyed, true);
});

test('frame parser: malformed JSON yields Log notification; subsequent frame still dispatches', async () => {
    // Malformed JSON falls into the catch path (op='?', reqId=null), which
    // emits a Log notification but no synthesized Result (reqId is null).
    // The next frame must still dispatch normally.
    const { sock } = makeHarness();
    const garbage = Buffer.from('{not json', 'utf8');
    const len = Buffer.alloc(4);
    len.writeUInt32LE(garbage.length, 0);
    const malformed = Buffer.concat([len, garbage]);
    const good = encodeFrame({ op: Op.Hello, reqId: 9, protocolVersion: PROTOCOL_VERSION });
    sock.emit('data', Buffer.concat([malformed, good]));
    const frames = await waitForFrames(sock, 2);
    assert.equal(frames[0]!['op'], Op.Log);
    assert.equal(frames[0]!['level'], 'Error');
    assert.match(String(frames[0]!['message']), /Handler '\?' threw/);
    assert.equal(frames[1]!['op'], Op.HelloResult);
    assert.equal(frames[1]!['reqId'], 9);
    assert.equal(sock.destroyed, false, 'malformed JSON should not tear down the pipe');
});

test('frame parser: object missing "op" field is dropped; next frame works', async () => {
    const { sock } = makeHarness();
    const noOp = encodeFrame({ reqId: 5, foo: 'bar' });
    const good = encodeFrame({ op: Op.Hello, reqId: 6, protocolVersion: PROTOCOL_VERSION });
    sock.emit('data', Buffer.concat([noOp, good]));
    const [frame] = await waitForFrames(sock, 1);
    assert.equal(frame!['reqId'], 6);
});

test('frame parser: extra unknown fields on a known op are tolerated', async () => {
    const { sock } = makeHarness();
    sock.emit('data', encodeFrame({
        op: Op.Hello, reqId: 11, protocolVersion: PROTOCOL_VERSION,
        extra: 'should be ignored', another: 42,
    }));
    const [frame] = await waitForFrames(sock, 1);
    assert.equal(frame!['ok'], true);
});

// ----------------------------------------------------------------------------
// Op dispatch
// ----------------------------------------------------------------------------

test('Hello: matching protocolVersion → ok=true, bridgeVersion = package.json version', async () => {
    const { sock } = makeHarness();
    sock.emit('data', encodeFrame({ op: Op.Hello, reqId: 1, protocolVersion: PROTOCOL_VERSION }));
    const [frame] = await waitForFrames(sock, 1);
    assert.equal(frame!['op'], Op.HelloResult);
    assert.equal(frame!['ok'], true);
    assert.equal(frame!['bridgeVersion'], pkg.version);
    assert.equal(frame!['error'], '');
});

test('Hello: mismatched protocolVersion → ok=false with informative error', async () => {
    const { sock } = makeHarness();
    sock.emit('data', encodeFrame({ op: Op.Hello, reqId: 2, protocolVersion: 999 }));
    const [frame] = await waitForFrames(sock, 1);
    assert.equal(frame!['ok'], false);
    const err = String(frame!['error']);
    assert.match(err, /Protocol version mismatch/);
    assert.match(err, /999/);
    assert.match(err, new RegExp(String(PROTOCOL_VERSION)));
});

test('Init: forwards token + status to host; result echoes reqId', async () => {
    const { sock, host } = makeHarness();
    host.nextInit({ ok: true, error: '' });
    sock.emit('data', encodeFrame({ op: Op.Init, reqId: 50, token: 'tok-x', status: 'Online' }));
    const [frame] = await waitForFrames(sock, 1);
    assert.equal(frame!['op'], Op.InitResult);
    assert.equal(frame!['reqId'], 50);
    assert.equal(frame!['ok'], true);
    const initCall = host.calls.find((c) => c.method === 'init');
    assert.ok(initCall);
    assert.deepEqual(initCall.args, ['tok-x', 'Online']);
});

test('Init: host returns ok=false with error → result echoes verbatim', async () => {
    const { sock, host } = makeHarness();
    host.nextInit({ ok: false, error: 'invalid token' });
    sock.emit('data', encodeFrame({ op: Op.Init, reqId: 51, token: 'bad', status: '' }));
    const [frame] = await waitForFrames(sock, 1);
    assert.equal(frame!['ok'], false);
    assert.equal(frame!['error'], 'invalid token');
});

test('Deinit: invokes host.deinit and returns DeinitResult', async () => {
    const { sock, host } = makeHarness();
    sock.emit('data', encodeFrame({ op: Op.Deinit, reqId: 60 }));
    const [frame] = await waitForFrames(sock, 1);
    assert.equal(frame!['op'], Op.DeinitResult);
    assert.equal(frame!['reqId'], 60);
    assert.equal(frame!['ok'], true);
    assert.ok(host.calls.some((c) => c.method === 'deinit'));
});

test('IsConnected: result reflects host.isConnected()', async () => {
    const { sock, host } = makeHarness();
    host.nextIsConnected(true);
    sock.emit('data', encodeFrame({ op: Op.IsConnected, reqId: 70 }));
    const [frame] = await waitForFrames(sock, 1);
    assert.equal(frame!['op'], Op.IsConnectedResult);
    assert.equal(frame!['connected'], true);
});

test('GetServers: passes through host.getServers() array', async () => {
    const { sock, host } = makeHarness();
    host.nextServers(['Guild A', 'Guild B']);
    sock.emit('data', encodeFrame({ op: Op.GetServers, reqId: 80 }));
    const [frame] = await waitForFrames(sock, 1);
    assert.equal(frame!['op'], Op.GetServersResult);
    assert.deepEqual(frame!['servers'], ['Guild A', 'Guild B']);
});

test('GetServers: empty array serializes as []', async () => {
    const { sock, host } = makeHarness();
    host.nextServers([]);
    sock.emit('data', encodeFrame({ op: Op.GetServers, reqId: 81 }));
    const [frame] = await waitForFrames(sock, 1);
    assert.deepEqual(frame!['servers'], []);
});

test('GetChannels: server name passes through to host', async () => {
    const { sock, host } = makeHarness();
    host.nextChannels(['general', 'voice-1']);
    sock.emit('data', encodeFrame({ op: Op.GetChannels, reqId: 90, server: 'My Guild' }));
    const [frame] = await waitForFrames(sock, 1);
    assert.equal(frame!['op'], Op.GetChannelsResult);
    assert.deepEqual(frame!['channels'], ['general', 'voice-1']);
    const call = host.calls.find((c) => c.method === 'getChannels');
    assert.deepEqual(call?.args, ['My Guild']);
});

test('SetGame: text passes through; SetGameResult ok=true', async () => {
    const { sock, host } = makeHarness();
    sock.emit('data', encodeFrame({ op: Op.SetGame, reqId: 100, text: 'Playing FFXIV' }));
    const [frame] = await waitForFrames(sock, 1);
    assert.equal(frame!['op'], Op.SetGameResult);
    assert.equal(frame!['ok'], true);
    const call = host.calls.find((c) => c.method === 'setGame');
    assert.deepEqual(call?.args, ['Playing FFXIV']);
});

test('JoinChannel: server + channel pass through; result echoed', async () => {
    const { sock, host } = makeHarness();
    host.nextJoinChannel({ ok: true, error: '' });
    sock.emit('data', encodeFrame({
        op: Op.JoinChannel, reqId: 110, server: 'Guild', channel: 'Voice',
    }));
    const [frame] = await waitForFrames(sock, 1);
    assert.equal(frame!['op'], Op.JoinChannelResult);
    assert.equal(frame!['ok'], true);
    const call = host.calls.find((c) => c.method === 'joinChannel');
    assert.deepEqual(call?.args, ['Guild', 'Voice']);
});

test('JoinChannel: error from host echoed', async () => {
    const { sock, host } = makeHarness();
    host.nextJoinChannel({ ok: false, error: 'channel not found' });
    sock.emit('data', encodeFrame({
        op: Op.JoinChannel, reqId: 111, server: 'X', channel: 'Y',
    }));
    const [frame] = await waitForFrames(sock, 1);
    assert.equal(frame!['ok'], false);
    assert.equal(frame!['error'], 'channel not found');
});

test('LeaveChannel: invokes host.leaveChannel; LeaveChannelResult ok=true', async () => {
    const { sock, host } = makeHarness();
    sock.emit('data', encodeFrame({ op: Op.LeaveChannel, reqId: 120 }));
    const [frame] = await waitForFrames(sock, 1);
    assert.equal(frame!['op'], Op.LeaveChannelResult);
    assert.equal(frame!['ok'], true);
    assert.ok(host.calls.some((c) => c.method === 'leaveChannel'));
});

test('SpeakPcm: base64 payload decodes to byte-equal Buffer at host', async () => {
    const { sock, host } = makeHarness();
    host.nextSpeakPcm({ ok: true, error: '' });
    const pcm = Buffer.from([0xde, 0xad, 0xbe, 0xef, 0x00, 0x01, 0x02, 0x03]);
    sock.emit('data', encodeFrame({
        op: Op.SpeakPcm, reqId: 130, pcm: pcm.toString('base64'),
    }));
    const [frame] = await waitForFrames(sock, 1);
    assert.equal(frame!['op'], Op.SpeakResult);
    assert.equal(frame!['ok'], true);
    const call = host.calls.find((c) => c.method === 'speakPcm');
    assert.ok(call);
    assert.ok(Buffer.isBuffer(call.args[0]));
    assert.equal(Buffer.compare(call.args[0] as Buffer, pcm), 0);
});

test('Unknown op: emits Log notification with no reqId, level=Error', async () => {
    const { sock } = makeHarness();
    sock.emit('data', encodeFrame({ op: 'Bogus', reqId: 999 }));
    const [frame] = await waitForFrames(sock, 1);
    assert.equal(frame!['op'], Op.Log);
    assert.equal(frame!['level'], 'Error');
    assert.match(String(frame!['message']), /Unknown op: Bogus/);
    // Log is a notification — must not carry reqId.
    assert.equal(frame!['reqId'], undefined);
});

// ----------------------------------------------------------------------------
// reqId correlation
// ----------------------------------------------------------------------------

test('reqId correlation: three concurrent requests get correctly tagged responses', async () => {
    const { sock, host } = makeHarness();
    host.nextIsConnected(true);
    host.nextServers(['Foo']);
    const a = encodeFrame({ op: Op.IsConnected, reqId: 10 });
    const b = encodeFrame({ op: Op.GetServers, reqId: 11 });
    const c = encodeFrame({ op: Op.IsConnected, reqId: 12 });
    sock.emit('data', Buffer.concat([a, b, c]));
    const frames = await waitForFrames(sock, 3);
    const ids = frames.map((f) => f['reqId']);
    assert.deepEqual(ids, [10, 11, 12]);
});

// ----------------------------------------------------------------------------
// Error path
// ----------------------------------------------------------------------------

test('Handler throws: emits Log + synthesized {Op}Result with ok=false', async () => {
    const { sock, host } = makeHarness();
    host.initThrows(new Error('init blew up'));
    sock.emit('data', encodeFrame({ op: Op.Init, reqId: 200, token: 't', status: 's' }));
    const frames = await waitForFrames(sock, 2);
    // Order per pipe-server.ts: Log first, then the synthesized result.
    assert.equal(frames[0]!['op'], Op.Log);
    assert.equal(frames[0]!['level'], 'Error');
    assert.match(String(frames[0]!['message']), /Handler 'Init' threw: init blew up/);
    assert.equal(frames[1]!['op'], Op.InitResult);
    assert.equal(frames[1]!['reqId'], 200);
    assert.equal(frames[1]!['ok'], false);
    assert.equal(frames[1]!['error'], 'init blew up');
});

test('Handler throws with reqId=null: only Log frame, no synthesized result', async () => {
    const { sock, host } = makeHarness();
    host.initThrows(new Error('boom'));
    sock.emit('data', encodeFrame({ op: Op.Init, reqId: null, token: 't', status: 's' }));
    // Wait long enough that any second frame would have arrived.
    const frames = await waitForFrames(sock, 1);
    await tick(8);
    const after = decodeFrames(sock.drainedWrites()).frames;
    assert.equal(after.length, 1, `expected exactly 1 frame, got ${after.length}`);
    assert.equal(frames[0]!['op'], Op.Log);
});

// ----------------------------------------------------------------------------
// Notifier wiring
// ----------------------------------------------------------------------------

test('setNotifier called exactly once on run()', () => {
    const { host } = makeHarness();
    const setNotifierCalls = host.calls.filter((c) => c.method === 'setNotifier');
    assert.equal(setNotifierCalls.length, 1);
    assert.ok(host.notify, 'notify callback should be captured');
});

test('Notifier: BotReady notification reaches the wire as a framed JSON', async () => {
    const { sock, host } = makeHarness();
    host.fireNotification({ op: Op.BotReady });
    const [frame] = await waitForFrames(sock, 1);
    assert.equal(frame!['op'], Op.BotReady);
    assert.equal(frame!['reqId'], undefined, 'notifications carry no reqId');
});

test('Notifier: Log notification carries level + message', async () => {
    const { sock, host } = makeHarness();
    host.fireNotification({ op: Op.Log, level: 'Warn', message: 'test message' });
    const [frame] = await waitForFrames(sock, 1);
    assert.equal(frame!['op'], Op.Log);
    assert.equal(frame!['level'], 'Warn');
    assert.equal(frame!['message'], 'test message');
});

test('Notifier: Disconnected carries reason', async () => {
    const { sock, host } = makeHarness();
    host.fireNotification({ op: Op.Disconnected, reason: 'gateway closed' });
    const [frame] = await waitForFrames(sock, 1);
    assert.equal(frame!['op'], Op.Disconnected);
    assert.equal(frame!['reason'], 'gateway closed');
});

// ----------------------------------------------------------------------------
// Write framing
// ----------------------------------------------------------------------------

test('write framing: response is preceded by 4-byte LE length prefix', async () => {
    const { sock } = makeHarness();
    sock.emit('data', encodeFrame({ op: Op.Hello, reqId: 1, protocolVersion: PROTOCOL_VERSION }));
    await waitForFrames(sock, 1);
    const buf = sock.drainedWrites();
    const declaredLen = buf.readUInt32LE(0);
    assert.equal(buf.length, 4 + declaredLen);
    const json = buf.subarray(4).toString('utf8');
    const parsed = JSON.parse(json) as Record<string, unknown>;
    assert.equal(parsed['op'], Op.HelloResult);
});

test('write framing: back-to-back notifications produce two non-interleaved frames', async () => {
    const { sock, host } = makeHarness();
    host.fireNotification({ op: Op.BotReady });
    host.fireNotification({ op: Op.Log, level: 'Info', message: 'second' });
    const frames = await waitForFrames(sock, 2);
    assert.equal(frames[0]!['op'], Op.BotReady);
    assert.equal(frames[1]!['op'], Op.Log);
    assert.equal(frames[1]!['message'], 'second');
});
