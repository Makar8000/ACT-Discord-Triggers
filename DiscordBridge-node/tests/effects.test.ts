import { test } from 'node:test';
import { strict as assert } from 'node:assert';

import {
    EFFECT_NAMES,
    applyEffect,
    applyRandomEffect,
    mulberry32,
    type EffectName,
} from '../src/effects.js';

// A short stereo s16le buffer with a bit of signal in both channels.
function sineStereo(frames: number, freq = 220): Buffer {
    const buf = Buffer.alloc(frames * 4);
    for (let i = 0; i < frames; i++) {
        const s = Math.round(Math.sin((2 * Math.PI * freq * i) / 48000) * 12000);
        buf.writeInt16LE(s, i * 4);
        buf.writeInt16LE(s, i * 4 + 2);
    }
    return buf;
}

function allInt16Range(buf: Buffer): boolean {
    for (let i = 0; i < buf.length; i += 2) {
        const s = buf.readInt16LE(i);
        if (s < -32768 || s > 32767 || !Number.isFinite(s)) return false;
    }
    return true;
}

test('every effect produces frame-aligned, in-range, non-empty output', () => {
    const input = sineStereo(4800); // 100 ms
    for (const name of EFFECT_NAMES) {
        const out = applyEffect(name, input, mulberry32(1));
        assert.equal(out.length % 4, 0, `${name}: output not stereo-frame aligned`);
        assert.ok(out.length > 0, `${name}: output empty`);
        assert.ok(allInt16Range(out), `${name}: output out of int16 range`);
    }
});

test('effects are deterministic for a fixed rng seed', () => {
    const input = sineStereo(2400);
    for (const name of EFFECT_NAMES) {
        const a = applyEffect(name, input, mulberry32(42));
        const b = applyEffect(name, input, mulberry32(42));
        assert.equal(Buffer.compare(a, b), 0, `${name}: not deterministic under same seed`);
    }
});

test('echo and reverb extend the buffer with a tail', () => {
    const input = sineStereo(4800);
    assert.ok(applyEffect('echo', input, mulberry32(3)).length > input.length, 'echo should add a tail');
    assert.ok(applyEffect('reverb', input, mulberry32(3)).length > input.length, 'reverb should add a tail');
});

test('tremolo / distortion / muffle preserve length (sample-wise)', () => {
    const input = sineStereo(4800);
    for (const name of ['tremolo', 'distortion', 'muffle'] as EffectName[]) {
        assert.equal(applyEffect(name, input, mulberry32(5)).length, input.length, `${name} changed length`);
    }
});

test('applyRandomEffect returns a known name and a valid buffer', () => {
    const input = sineStereo(2400);
    const r = applyRandomEffect(input, mulberry32(99));
    assert.ok((EFFECT_NAMES as readonly string[]).includes(r.name), `unknown effect name ${r.name}`);
    assert.equal(r.pcm.length % 4, 0);
    assert.ok(allInt16Range(r.pcm));
});

test('applyRandomEffect picks deterministically for a fixed seed', () => {
    const input = sineStereo(1200);
    const a = applyRandomEffect(input, mulberry32(7));
    const b = applyRandomEffect(input, mulberry32(7));
    assert.equal(a.name, b.name);
    assert.equal(Buffer.compare(a.pcm, b.pcm), 0);
});

test('buffers too short to hold a stereo frame are returned untouched', () => {
    const tiny = Buffer.from([0x01, 0x02]); // 2 bytes < one stereo frame
    for (const name of EFFECT_NAMES) {
        assert.equal(Buffer.compare(applyEffect(name, tiny, mulberry32(1)), tiny), 0, `${name} mangled a tiny buffer`);
    }
});

test('silence in -> finite output (no NaN from feedback paths)', () => {
    const silence = Buffer.alloc(4800 * 4);
    for (const name of EFFECT_NAMES) {
        const out = applyEffect(name, silence, mulberry32(2));
        assert.ok(allInt16Range(out), `${name} produced non-finite output from silence`);
    }
});
