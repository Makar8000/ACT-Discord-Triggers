import { test } from 'node:test';
import { strict as assert } from 'node:assert';

import { normalizePcm16 } from '../src/normalize.js';

// Build N stereo frames (16-bit LE) where every sample equals `value`. A constant
// amplitude makes RMS == |value| / 32768, so the expected gain is easy to reason
// about in the assertions below.
function constStereo(frames: number, value: number): Buffer {
    const buf = Buffer.alloc(frames * 4);
    for (let i = 0; i < frames; i++) {
        buf.writeInt16LE(value, i * 4);
        buf.writeInt16LE(value, i * 4 + 2);
    }
    return buf;
}

function maxAbs(pcm: Buffer): number {
    let m = 0;
    for (let i = 0; i < pcm.length; i += 2) {
        const a = Math.abs(pcm.readInt16LE(i));
        if (a > m) m = a;
    }
    return m;
}

function rmsNorm(pcm: Buffer): number {
    let sumSq = 0;
    const n = pcm.length >>> 1;
    for (let i = 0; i < n; i++) {
        const s = pcm.readInt16LE(i * 2) / 32768;
        sumSq += s * s;
    }
    return Math.sqrt(sumSq / n);
}

test('empty buffer is returned untouched', () => {
    const empty = Buffer.alloc(0);
    const r = normalizePcm16(empty, -20);
    assert.equal(r.applied, false);
    assert.equal(r.gain, 1);
    assert.equal(r.pcm, empty);
});

test('silence (all zeros) is left untouched, no divide-by-zero blowup', () => {
    const silence = constStereo(480, 0);
    const r = normalizePcm16(silence, -20);
    assert.equal(r.applied, false);
    assert.equal(r.gain, 1);
    assert.equal(r.pcm, silence);
});

test('loud clip is attenuated toward target (gain < 1)', () => {
    // Half-scale constant → RMS ≈ -6 dBFS, well above a -20 target.
    const loud = constStereo(480, 16384);
    const r = normalizePcm16(loud, -20);
    assert.equal(r.applied, true);
    assert.ok(r.gain < 1, `expected attenuation, got gain=${r.gain}`);
    // RMS of the result should land near the -20 dBFS target (10^(-20/20)=0.1).
    assert.ok(Math.abs(rmsNorm(r.pcm) - 0.1) < 0.01, `rms=${rmsNorm(r.pcm)}`);
    assert.ok(maxAbs(r.pcm) <= 32767);
});

test('quiet clip is boosted but capped at +12 dB max boost', () => {
    // Very quiet constant → target wants ~+34 dB; the cap should bind first.
    const quiet = constStereo(480, 64);
    const r = normalizePcm16(quiet, -20);
    assert.equal(r.applied, true);
    const maxBoost = Math.pow(10, 12 / 20); // ≈ 3.981
    assert.ok(r.gain <= maxBoost + 1e-6, `gain ${r.gain} exceeded max boost ${maxBoost}`);
    assert.ok(r.gain > 3.9, `expected gain near the cap, got ${r.gain}`);
});

test('peak ceiling binds before max boost on a low-RMS, high-peak clip', () => {
    // One full-ish peak frame in an otherwise-silent buffer: low RMS demands a big
    // boost, but the peak ceiling (0.97) clamps the gain below the +12 dB cap.
    const buf = constStereo(100, 0);
    buf.writeInt16LE(8192, 0);
    buf.writeInt16LE(8192, 2);
    const r = normalizePcm16(buf, -20);
    assert.equal(r.applied, true);
    const peakNorm = 8192 / 32768;
    const peakLimit = 0.97 / peakNorm;
    assert.ok(r.gain <= peakLimit + 1e-6, `gain ${r.gain} exceeded peak limit ${peakLimit}`);
    // No sample may clip after the boost.
    assert.ok(maxAbs(r.pcm) <= 32767);
});

test('near-target clip is left untouched (gain within unity epsilon)', () => {
    // Constant chosen so RMS ≈ -20 dBFS already (0.1 * 32768 ≈ 3277).
    const onTarget = constStereo(480, 3277);
    const r = normalizePcm16(onTarget, -20);
    assert.equal(r.applied, false);
    assert.equal(r.pcm, onTarget);
});

test('output length always matches input length', () => {
    const buf = constStereo(123, 9000);
    const r = normalizePcm16(buf, -16);
    assert.equal(r.pcm.length, buf.length);
});
