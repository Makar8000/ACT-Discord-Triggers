import { test } from 'node:test';
import { strict as assert } from 'node:assert';

import { PcmMixer } from '../src/pcm-mixer.js';

const CHUNK_BYTES = 3840; // 960 samples * 2 channels * 2 bytes

function constStereo(int16Value: number, frames: number): Buffer {
    const buf = Buffer.alloc(frames * 4);
    for (let i = 0; i < frames; i++) {
        buf.writeInt16LE(int16Value, i * 4);
        buf.writeInt16LE(int16Value, i * 4 + 2);
    }
    return buf;
}

function allSamplesEqual(buf: Buffer, expected: number): boolean {
    for (let i = 0; i < buf.length; i += 2) {
        if (buf.readInt16LE(i) !== expected) return false;
    }
    return true;
}

test('empty mixer emits one silence chunk', () => {
    const m = new PcmMixer();
    const chunk = m._mixOneChunk();
    assert.equal(chunk.length, CHUNK_BYTES);
    assert.ok(allSamplesEqual(chunk, 0));
});

test('single voice exactly one chunk long: output equals input', () => {
    const m = new PcmMixer();
    const voice = constStereo(1234, 960);
    m.addVoice(voice);
    const chunk = m._mixOneChunk();
    assert.equal(Buffer.compare(chunk, voice), 0);
    // Voice consumed; next chunk is silence.
    assert.ok(allSamplesEqual(m._mixOneChunk(), 0));
});

test('two voices sum sample-by-sample', () => {
    const m = new PcmMixer();
    m.addVoice(constStereo(100, 960));
    m.addVoice(constStereo(200, 960));
    const chunk = m._mixOneChunk();
    assert.ok(allSamplesEqual(chunk, 300));
});

test('addVoice with latency meta mixes identically (instrumentation is side-effect-only)', () => {
    const m = new PcmMixer();
    const voice = constStereo(4321, 960);
    // meta only drives the firstEmit log; it must not alter mixing output.
    m.addVoice(voice, { id: 7, enqueueT: 0 });
    const chunk = m._mixOneChunk();
    assert.equal(Buffer.compare(chunk, voice), 0);
    assert.ok(allSamplesEqual(m._mixOneChunk(), 0));
});

test('positive saturation clips to 32767', () => {
    const m = new PcmMixer();
    m.addVoice(constStereo(30000, 960));
    m.addVoice(constStereo(10000, 960));
    const chunk = m._mixOneChunk();
    assert.ok(allSamplesEqual(chunk, 32767));
});

test('negative saturation clips to -32768', () => {
    const m = new PcmMixer();
    m.addVoice(constStereo(-30000, 960));
    m.addVoice(constStereo(-10000, 960));
    const chunk = m._mixOneChunk();
    assert.ok(allSamplesEqual(chunk, -32768));
});

test('voice spanning 1.5 chunks: second chunk is half mixed, half silent', () => {
    const m = new PcmMixer();
    // 1440 frames = 1.5 chunks of stereo s16le (1440 * 4 = 5760 bytes).
    m.addVoice(constStereo(500, 1440));

    const c1 = m._mixOneChunk();
    assert.ok(allSamplesEqual(c1, 500));

    const c2 = m._mixOneChunk();
    // First 480 frames (1920 bytes) carry the voice's tail at 500.
    for (let i = 0; i < 1920; i += 2) {
        assert.equal(c2.readInt16LE(i), 500, `c2 first half at byte ${i}`);
    }
    // Remaining 480 frames are silence.
    for (let i = 1920; i < CHUNK_BYTES; i += 2) {
        assert.equal(c2.readInt16LE(i), 0, `c2 second half at byte ${i}`);
    }

    // Voice fully consumed; chunk 3 is pure silence.
    assert.ok(allSamplesEqual(m._mixOneChunk(), 0));
});

test('clear() drops in-flight voices', () => {
    const m = new PcmMixer();
    m.addVoice(constStereo(7777, 960 * 4)); // 4 chunks long
    const c1 = m._mixOneChunk();
    assert.ok(allSamplesEqual(c1, 7777));
    m.clear();
    assert.ok(allSamplesEqual(m._mixOneChunk(), 0));
});

test('independent per-voice cursors: longer voice survives shorter voice', () => {
    const m = new PcmMixer();
    m.addVoice(constStereo(100, 960 * 4)); // A: 4 chunks @ 100
    m.addVoice(constStereo(200, 960 * 2)); // B: 2 chunks @ 200

    // Chunks 1-2: both active → 300.
    assert.ok(allSamplesEqual(m._mixOneChunk(), 300));
    assert.ok(allSamplesEqual(m._mixOneChunk(), 300));

    // Chunks 3-4: only A remains → 100.
    assert.ok(allSamplesEqual(m._mixOneChunk(), 100));
    assert.ok(allSamplesEqual(m._mixOneChunk(), 100));

    // Both consumed.
    assert.ok(allSamplesEqual(m._mixOneChunk(), 0));
});

test('addVoice tolerates trailing odd byte by truncating', () => {
    const m = new PcmMixer();
    // 960 frames worth (3840 bytes) plus one stray byte.
    const stray = Buffer.concat([constStereo(1234, 960), Buffer.from([0xff])]);
    m.addVoice(stray);
    const chunk = m._mixOneChunk();
    // The aligned 3840 bytes mix as if the stray byte didn't exist.
    assert.ok(allSamplesEqual(chunk, 1234));
});

test('addVoice ignores buffer that has no aligned bytes', () => {
    const m = new PcmMixer();
    m.addVoice(Buffer.from([0xff])); // 1 byte → truncates to 0
    assert.ok(allSamplesEqual(m._mixOneChunk(), 0));
});

test('voice cap: 65th voice causes one FIFO drop, 64 survive', () => {
    const m = new PcmMixer();
    // 64 voices fit; the 65th evicts the oldest. Use distinct sample values
    // so we can prove order if we want to extend later.
    for (let i = 0; i < 64; i++) m.addVoice(constStereo(i + 1, 1));
    assert.equal(m.voiceCount, 64);
    const r = m.addVoice(constStereo(99, 1));
    assert.deepEqual(r, { dropped: 1 });
    assert.equal(m.voiceCount, 64);
});

test('voice cap: addVoice returns {dropped:0} below the cap', () => {
    const m = new PcmMixer();
    const r = m.addVoice(constStereo(1, 100));
    assert.deepEqual(r, { dropped: 0 });
});

test('voice cap: FIFO eviction — oldest dropped, newest survives', () => {
    const m = new PcmMixer();
    // Add 64 voices each carrying a 1-frame buffer of value 1.
    for (let i = 0; i < 64; i++) m.addVoice(constStereo(1, 1));
    // The 65th carries a distinct value 1000. After eviction the queue holds
    // 63 voices @ value=1 and 1 voice @ value=1000 → first chunk sample[0] = 63 + 1000.
    m.addVoice(constStereo(1000, 1));
    assert.equal(m.voiceCount, 64);
    const chunk = m._mixOneChunk();
    // Each 1-frame voice contributes only to sample[0] (1 stereo frame = 2 samples).
    assert.equal(chunk.readInt16LE(0), 63 + 1000);
});

test('byte cap: large queued bytes evict oldest until under MAX_QUEUED_BYTES', () => {
    const m = new PcmMixer();
    // 5 buffers of 8 MiB each = 40 MiB > 32 MiB cap. Adding the 5th should
    // evict the 1st. We use real stereo s16 buffers (constStereo) so the
    // cap math is on real consumed bytes.
    const big = constStereo(1, 8 * 1024 * 1024 / 4); // 8 MiB worth of stereo s16
    assert.equal(big.length, 8 * 1024 * 1024);
    for (let i = 0; i < 4; i++) {
        const r = m.addVoice(big);
        assert.equal(r.dropped, 0, `voice ${i + 1} should fit`);
    }
    const r5 = m.addVoice(big);
    assert.equal(r5.dropped, 1, 'fifth 8 MiB voice should evict the first');
    assert.ok(m.queuedBytes <= 32 * 1024 * 1024);
});

test('byte cap: a single oversized voice is preserved (never the only voice)', () => {
    const m = new PcmMixer();
    const huge = constStereo(1, (40 * 1024 * 1024) / 4); // 40 MiB > cap
    const r = m.addVoice(huge);
    assert.deepEqual(r, { dropped: 0 });
    assert.equal(m.voiceCount, 1);
});

test('byte cap: queuedBytes decrements as chunks are consumed', () => {
    const m = new PcmMixer();
    m.addVoice(constStereo(1, 960)); // exactly one chunk (3840 bytes)
    assert.equal(m.queuedBytes, 3840);
    m._mixOneChunk();
    assert.equal(m.queuedBytes, 0);
});

test('clear() resets queuedBytes', () => {
    const m = new PcmMixer();
    m.addVoice(constStereo(1, 1000));
    assert.ok(m.queuedBytes > 0);
    m.clear();
    assert.equal(m.queuedBytes, 0);
    assert.equal(m.voiceCount, 0);
});

test('Readable plumbing: _read pushes one chunk per call', async () => {
    const m = new PcmMixer();
    m.addVoice(constStereo(42, 960 * 3)); // 3 chunks long
    // Drive the public Readable API. Wait for 'readable' so the internal
    // buffer has been primed.
    await new Promise<void>((resolve) => m.once('readable', () => resolve()));
    const chunk = m.read(CHUNK_BYTES) as Buffer;
    assert.equal(chunk.length, CHUNK_BYTES);
    assert.ok(allSamplesEqual(chunk, 42));
});
