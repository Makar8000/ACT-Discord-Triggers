import { test } from 'node:test';
import { strict as assert } from 'node:assert';

import { upmixMonoToStereo16, resampleStereo16 } from '../src/discord-host.js';

function int16Buf(samples: number[]): Buffer {
    const b = Buffer.alloc(samples.length * 2);
    for (let i = 0; i < samples.length; i++) b.writeInt16LE(samples[i]!, i * 2);
    return b;
}

function readStereo(b: Buffer): Array<[number, number]> {
    const out: Array<[number, number]> = [];
    for (let i = 0; i < b.length; i += 4) {
        out.push([b.readInt16LE(i), b.readInt16LE(i + 2)]);
    }
    return out;
}

test('upmixMonoToStereo16: each mono sample becomes (s, s) stereo pair', () => {
    const mono = int16Buf([100, -200, 30000, -30000]);
    const stereo = upmixMonoToStereo16(mono);
    assert.deepEqual(readStereo(stereo), [[100, 100], [-200, -200], [30000, 30000], [-30000, -30000]]);
});

test('upmixMonoToStereo16: empty input → empty output', () => {
    assert.equal(upmixMonoToStereo16(Buffer.alloc(0)).length, 0);
});

test('resampleStereo16: srcRate == dstRate returns input verbatim', () => {
    const pcm = int16Buf([1, 2, 3, 4, 5, 6, 7, 8]); // 4 stereo frames
    const out = resampleStereo16(pcm, 48000, 48000);
    assert.equal(Buffer.compare(out, pcm), 0);
});

test('resampleStereo16: 44.1k → 48k stretches frame count by ratio', () => {
    // 441 input frames at 44.1k = 10 ms; expect ~480 frames at 48k.
    const frames = 441;
    const pcm = Buffer.alloc(frames * 4);
    for (let i = 0; i < frames; i++) {
        pcm.writeInt16LE(i, i * 4);
        pcm.writeInt16LE(-i, i * 4 + 2);
    }
    const out = resampleStereo16(pcm, 44100, 48000);
    const outFrames = out.length / 4;
    assert.equal(outFrames, 480);
});

test('resampleStereo16: monotonic ramp stays monotonic after resample (no obvious aliasing)', () => {
    // Linear ramp from 0..1000 across 1000 frames at 44.1k.
    const frames = 1000;
    const pcm = Buffer.alloc(frames * 4);
    for (let i = 0; i < frames; i++) {
        pcm.writeInt16LE(i, i * 4);
        pcm.writeInt16LE(i, i * 4 + 2);
    }
    const out = resampleStereo16(pcm, 44100, 48000);
    const outFrames = out.length / 4;
    let prev = -1;
    for (let i = 0; i < outFrames; i++) {
        const v = out.readInt16LE(i * 4);
        // Ramp must never decrease (allow equal: trailing samples clamp to last src).
        assert.ok(v >= prev, `non-monotonic at frame ${i}: ${prev} → ${v}`);
        prev = v;
    }
});

test('resampleStereo16: 48k → 24k halves frame count and preserves channel separation', () => {
    // 8 input frames; expect 4 output frames.
    const pcm = int16Buf([
        100, 200,
        110, 210,
        120, 220,
        130, 230,
        140, 240,
        150, 250,
        160, 260,
        170, 270,
    ]);
    const out = resampleStereo16(pcm, 48000, 24000);
    const stereo = readStereo(out);
    assert.equal(stereo.length, 4);
    // Left ramp stays in left channel, right ramp stays in right channel.
    for (const [l, r] of stereo) {
        assert.ok(l < r, `expected L < R, got L=${l} R=${r}`);
        assert.ok(r - l > 90 && r - l < 110, `expected ~100 channel separation, got ${r - l}`);
    }
});
