// Random sound-effects for trigger playback. When the plugin flags a SpeakPcm /
// SpeakFile request (user opted in, RNG rolled a hit), discord-host runs the
// fully-decoded 48 kHz / 16-bit / stereo PCM buffer through one randomly-chosen
// effect from EFFECT_NAMES before handing it to the mixer. Picking the effect
// AND its parameters here (not on the plugin side) keeps the whole DSP catalog
// in one place — the wire only carries a single "apply a random effect" bit.
//
// Everything is pure int16/float math: no native deps, trivial CPU next to the
// Opus encode that follows. Each effect randomizes its own parameters within a
// tasteful range so two hits of the same effect don't sound identical.
//
// The rng is injectable so the unit tests are deterministic; production passes
// Math.random.

const SR = 48000; // bridge is hard-wired to 48 kHz (see CLAUDE.md)
const TWO_PI = Math.PI * 2;

// () => float in [0, 1). Math.random-compatible.
export type Rng = () => number;

export const EFFECT_NAMES = [
    'echo', 'reverb', 'flanger', 'chorus',
    'tremolo', 'distortion', 'muffle', 'pitch',
] as const;
export type EffectName = typeof EFFECT_NAMES[number];

export interface EffectResult { pcm: Buffer; name: EffectName }

type Channels = readonly [Float32Array, Float32Array];
type EffectFn = (L: Float32Array, R: Float32Array, rng: Rng) => Channels;

// --- PCM <-> float helpers -------------------------------------------------

function decode(pcm: Buffer): Channels {
    const frames = pcm.length >>> 2; // 4 bytes per stereo frame
    const L = new Float32Array(frames);
    const R = new Float32Array(frames);
    for (let i = 0; i < frames; i++) {
        L[i] = pcm.readInt16LE(i * 4) / 32768;
        R[i] = pcm.readInt16LE(i * 4 + 2) / 32768;
    }
    return [L, R];
}

function encode(L: Float32Array, R: Float32Array): Buffer {
    const frames = Math.min(L.length, R.length);
    const out = Buffer.allocUnsafe(frames * 4);
    for (let i = 0; i < frames; i++) {
        out.writeInt16LE(clamp16(L[i]!), i * 4);
        out.writeInt16LE(clamp16(R[i]!), i * 4 + 2);
    }
    return out;
}

function clamp16(x: number): number {
    let s = Math.round(x * 32768);
    if (s > 32767) s = 32767;
    else if (s < -32768) s = -32768;
    return s;
}

function rangeOf(rng: Rng, min: number, max: number): number {
    return min + rng() * (max - min);
}

// --- Effects ---------------------------------------------------------------

// Feedback delay: dry signal plus a decaying train of echoes. Output is longer
// than the input by a tail long enough for the echoes to fade.
function echo(L: Float32Array, R: Float32Array, rng: Rng): Channels {
    const delay = Math.floor(rangeOf(rng, 0.09, 0.28) * SR); // 90–280 ms
    const fb = rangeOf(rng, 0.3, 0.55);
    const n = L.length + delay * 5;
    const oL = new Float32Array(n);
    const oR = new Float32Array(n);
    for (let i = 0; i < n; i++) {
        const dryL = i < L.length ? L[i]! : 0;
        const dryR = i < R.length ? R[i]! : 0;
        oL[i] = dryL + (i >= delay ? oL[i - delay]! * fb : 0);
        oR[i] = dryR + (i >= delay ? oR[i - delay]! * fb : 0);
    }
    return [oL, oR];
}

// Schroeder/Freeverb-lite: 4 parallel comb filters into 2 series allpass
// filters per channel, with the right channel's delays offset for stereo
// width. Output carries a reverb tail beyond the input.
function makeComb(size: number, fb: number, damp: number): (x: number) => number {
    const buf = new Float32Array(size);
    let idx = 0;
    let store = 0;
    return (x: number): number => {
        const y = buf[idx]!;
        store = y * (1 - damp) + store * damp;
        buf[idx] = x + store * fb;
        idx = idx + 1 < size ? idx + 1 : 0;
        return y;
    };
}

function makeAllpass(size: number, fb: number): (x: number) => number {
    const buf = new Float32Array(size);
    let idx = 0;
    return (x: number): number => {
        const bufout = buf[idx]!;
        const y = -x + bufout;
        buf[idx] = x + bufout * fb;
        idx = idx + 1 < size ? idx + 1 : 0;
        return y;
    };
}

function reverbChannel(
    x: Float32Array, n: number, spread: number, fb: number, damp: number,
): Float32Array {
    const k = SR / 44100; // Freeverb tunings are at 44.1 kHz
    const combs = [1116, 1188, 1277, 1356].map(t => makeComb(Math.round(t * k) + spread, fb, damp));
    const aps = [556, 441].map(t => makeAllpass(Math.round(t * k) + spread, 0.5));
    const out = new Float32Array(n);
    for (let i = 0; i < n; i++) {
        const inp = (i < x.length ? x[i]! : 0) * 0.5;
        let s = 0;
        for (const c of combs) s += c(inp);
        for (const a of aps) s = a(s);
        out[i] = s;
    }
    return out;
}

function reverb(L: Float32Array, R: Float32Array, rng: Rng): Channels {
    const fb = rangeOf(rng, 0.72, 0.88);   // room size
    const damp = rangeOf(rng, 0.1, 0.4);
    const wet = rangeOf(rng, 0.25, 0.45);
    const n = L.length + Math.floor(SR * 0.7); // ~0.7 s tail
    const wL = reverbChannel(L, n, 0, fb, damp);
    const wR = reverbChannel(R, n, 23, fb, damp); // Freeverb stereospread
    const oL = new Float32Array(n);
    const oR = new Float32Array(n);
    for (let i = 0; i < n; i++) {
        const dL = i < L.length ? L[i]! : 0;
        const dR = i < R.length ? R[i]! : 0;
        oL[i] = dL * (1 - wet) + wL[i]! * wet;
        oR[i] = dR * (1 - wet) + wR[i]! * wet;
    }
    return [oL, oR];
}

// One LFO-modulated fractional delay line with feedback. Flanger and chorus are
// the same structure at different delay/rate ranges.
function modDelayChannel(
    x: Float32Array, rate: number, baseS: number, depthS: number,
    fb: number, mix: number, phase: number,
): Float32Array {
    const n = x.length;
    const out = new Float32Array(n);
    const size = Math.ceil(baseS + depthS) + 4;
    const buf = new Float32Array(size);
    let widx = 0;
    const w = TWO_PI * rate / SR;
    for (let i = 0; i < n; i++) {
        const lfo = 0.5 - 0.5 * Math.cos(i * w + phase); // 0..1
        const delay = baseS + depthS * lfo;
        let rpos = widx - delay;
        while (rpos < 0) rpos += size;
        const r0 = Math.floor(rpos);
        const frac = rpos - r0;
        const a = buf[r0 % size]!;
        const b = buf[(r0 + 1) % size]!;
        const delayed = a + (b - a) * frac;
        const xin = x[i]!;
        buf[widx] = xin + delayed * fb;
        widx = widx + 1 < size ? widx + 1 : 0;
        out[i] = xin * (1 - mix) + delayed * mix;
    }
    return out;
}

function modDelay(
    L: Float32Array, R: Float32Array,
    rate: number, baseMs: number, depthMs: number, fb: number, mix: number,
): Channels {
    const baseS = baseMs / 1000 * SR;
    const depthS = depthMs / 1000 * SR;
    // Offset the right channel's LFO by a quarter cycle for stereo movement.
    const oL = modDelayChannel(L, rate, baseS, depthS, fb, mix, 0);
    const oR = modDelayChannel(R, rate, baseS, depthS, fb, mix, Math.PI / 2);
    return [oL, oR];
}

function flanger(L: Float32Array, R: Float32Array, rng: Rng): Channels {
    return modDelay(L, R,
        rangeOf(rng, 0.1, 0.6),   // rate Hz
        rangeOf(rng, 0.5, 2),     // base ms
        rangeOf(rng, 1.5, 3.5),   // depth ms
        rangeOf(rng, 0.3, 0.6),   // feedback
        0.5);
}

function chorus(L: Float32Array, R: Float32Array, rng: Rng): Channels {
    return modDelay(L, R,
        rangeOf(rng, 0.3, 1.2),   // rate Hz
        rangeOf(rng, 18, 28),     // base ms
        rangeOf(rng, 3, 7),       // depth ms
        rangeOf(rng, 0, 0.2),     // feedback
        0.45);
}

// Amplitude LFO.
function tremolo(L: Float32Array, R: Float32Array, rng: Rng): Channels {
    const rate = rangeOf(rng, 3, 8);
    const depth = rangeOf(rng, 0.3, 0.8);
    const w = TWO_PI * rate / SR;
    const n = L.length;
    const oL = new Float32Array(n);
    const oR = new Float32Array(n);
    for (let i = 0; i < n; i++) {
        const g = 1 - depth * (0.5 - 0.5 * Math.cos(i * w));
        oL[i] = L[i]! * g;
        oR[i] = R[i]! * g;
    }
    return [oL, oR];
}

// tanh soft-clip waveshaper with makeup attenuation so it stays in range.
function distortion(L: Float32Array, R: Float32Array, rng: Rng): Channels {
    const drive = rangeOf(rng, 2, 6);
    const norm = Math.tanh(drive);
    const makeup = rangeOf(rng, 0.6, 0.85);
    const n = L.length;
    const oL = new Float32Array(n);
    const oR = new Float32Array(n);
    for (let i = 0; i < n; i++) {
        oL[i] = Math.tanh(L[i]! * drive) / norm * makeup;
        oR[i] = Math.tanh(R[i]! * drive) / norm * makeup;
    }
    return [oL, oR];
}

// One-pole lowpass — "muffled / behind a wall".
function muffle(L: Float32Array, R: Float32Array, rng: Rng): Channels {
    const cutoff = rangeOf(rng, 700, 2500);
    const dt = 1 / SR;
    const rc = 1 / (TWO_PI * cutoff);
    const alpha = dt / (rc + dt);
    const n = L.length;
    const oL = new Float32Array(n);
    const oR = new Float32Array(n);
    let yL = 0;
    let yR = 0;
    for (let i = 0; i < n; i++) {
        yL += alpha * (L[i]! - yL);
        yR += alpha * (R[i]! - yR);
        oL[i] = yL;
        oR[i] = yR;
    }
    return [oL, oR];
}

// Resample for a pitch shift (chipmunk if up, demon if down). Changes duration
// as well as pitch — fine here since the buffer is complete before playback.
function pitch(L: Float32Array, R: Float32Array, rng: Rng): Channels {
    const factor = rng() < 0.5 ? rangeOf(rng, 1.2, 1.6) : rangeOf(rng, 0.65, 0.85);
    const n = L.length;
    const outN = Math.max(1, Math.round(n / factor));
    const oL = new Float32Array(outN);
    const oR = new Float32Array(outN);
    const last = n - 1;
    for (let i = 0; i < outN; i++) {
        const pos = i * factor;
        const i0 = Math.floor(pos);
        const i1 = i0 < last ? i0 + 1 : last;
        const frac = pos - i0;
        oL[i] = L[i0]! + (L[i1]! - L[i0]!) * frac;
        oR[i] = R[i0]! + (R[i1]! - R[i0]!) * frac;
    }
    return [oL, oR];
}

const EFFECTS: Record<EffectName, EffectFn> = {
    echo, reverb, flanger, chorus, tremolo, distortion, muffle, pitch,
};

// Apply a named effect to a 48k/16/stereo PCM buffer. Buffers too short to hold
// a single stereo frame are returned untouched.
export function applyEffect(name: EffectName, pcm: Buffer, rng: Rng = Math.random): Buffer {
    if (pcm.length < 4) return pcm;
    const [L, R] = decode(pcm);
    const [oL, oR] = EFFECTS[name](L, R, rng);
    return encode(oL, oR);
}

// Pick one effect at random and apply it; returns the processed buffer plus the
// chosen effect name for logging/correlation.
export function applyRandomEffect(pcm: Buffer, rng: Rng = Math.random): EffectResult {
    const name = EFFECT_NAMES[Math.floor(rng() * EFFECT_NAMES.length)] ?? 'echo';
    return { pcm: applyEffect(name, pcm, rng), name };
}

// Deterministic PRNG for tests (mulberry32). Production uses Math.random.
export function mulberry32(seed: number): Rng {
    let a = seed >>> 0;
    return (): number => {
        a = (a + 0x6D2B79F5) | 0;
        let t = Math.imul(a ^ (a >>> 15), 1 | a);
        t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
        return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
    };
}
