import { test } from 'node:test';
import { strict as assert } from 'node:assert';
import { Op, PROTOCOL_VERSION, MAX_FRAME_BYTES } from '../src/protocol.js';

test('PROTOCOL_VERSION is a positive integer', () => {
    assert.equal(typeof PROTOCOL_VERSION, 'number');
    assert.ok(Number.isInteger(PROTOCOL_VERSION));
    assert.ok(PROTOCOL_VERSION > 0);
});

test('MAX_FRAME_BYTES is 64 MiB (matches C# Protocol.cs)', () => {
    assert.equal(MAX_FRAME_BYTES, 64 * 1024 * 1024);
});

test('all Op values are distinct strings', () => {
    const values = Object.values(Op);
    for (const v of values) assert.equal(typeof v, 'string');
    assert.equal(new Set(values).size, values.length);
});

test('every request op has a paired Result op (Shutdown excluded)', () => {
    // Shutdown is fire-and-forget — bridge process.exits before responding.
    const requestOps = [
        'Hello', 'Init', 'Deinit', 'IsConnected',
        'GetServers', 'GetChannels', 'SetGame',
        'JoinChannel', 'LeaveChannel',
    ] as const;
    const opValues = new Set<string>(Object.values(Op));
    for (const req of requestOps) {
        assert.ok(opValues.has(`${req}Result`), `missing pair: ${req}Result`);
    }
    // SpeakPcm is the one mismatched name — its result is "SpeakResult", not "SpeakPcmResult".
    // Documented in protocol.ts; assert it explicitly so a rename catches the mismatch.
    assert.equal(Op.SpeakPcm, 'SpeakPcm');
    assert.equal(Op.SpeakResult, 'SpeakResult');
    // Shutdown has no Result op by design.
    assert.ok(!opValues.has('ShutdownResult'));
});

test('notification ops have no Result suffix', () => {
    for (const notifOp of [Op.BotReady, Op.Log, Op.Disconnected]) {
        assert.ok(!notifOp.endsWith('Result'), `${notifOp} should not end with Result`);
    }
});

test('Op constants match their key names verbatim', () => {
    // Catches typos like Op.HelloResult = 'helloResult'. C# side uses PascalCase
    // string constants too, so case mismatches break the wire silently.
    for (const [key, value] of Object.entries(Op)) {
        assert.equal(value, key, `Op.${key} value drift: ${value}`);
    }
});
