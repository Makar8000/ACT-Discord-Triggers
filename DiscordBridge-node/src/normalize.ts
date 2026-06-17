// Auto-leveling (loudness normalization) for trigger playback. Some sounds come
// in mastered hot and others whisper-quiet; after a random effect the spread is
// worse still (echo/reverb tails drop RMS, distortion pushes it up). This brings
// each clip toward one target loudness so the user isn't reaching for the volume
// knob between triggers.
//
// It's per-clip and offline: the whole 48 kHz / 16-bit / stereo buffer is in hand
// before playback, so there's no streaming compressor to maintain — just measure
// the clip, pick one gain, apply it. RMS (average energy) is the loudness proxy
// because it tracks perceived volume far better than peak does; the peak is only
// used as a brick-wall ceiling so a boost can never clip.
//
// discord-host applies this in _enqueue, AFTER any random effect, so the effect's
// own level change is what gets corrected.

const FULL_SCALE = 32768; // |int16| range; 1.0 == 0 dBFS

// Leave a sliver of headroom under 0 dBFS. A gain that would push the loudest
// sample past this is clamped down to it — the limiter that makes boosting safe.
const PEAK_CEILING = 0.97; // ≈ -0.26 dBFS

// Cap how much we'll amplify. Without this, a near-silent clip (or the quiet tail
// of one) demands enormous gain and we'd just amplify hiss/noise into the mix.
const MAX_BOOST_DB = 12;
const MAX_BOOST_LINEAR = Math.pow(10, MAX_BOOST_DB / 20);

// Buffers quieter than this are treated as silence and left untouched (avoids a
// divide-by-tiny-RMS blowup and pointless work on empty/near-empty clips).
const MIN_RMS = 1 / FULL_SCALE; // one LSB

// Skip the rewrite when the gain is within this of unity — sub-perceptual, not
// worth allocating a new buffer for.
const UNITY_EPSILON = 0.01; // ≈ 0.086 dB

// Config defaults applied until the plugin pushes its own via SetNormalization.
// The C# DiscordClient mirrors these (see its NormalizeEnabled/NormalizeTargetDb);
// keep the two sides in step.
export const DEFAULT_NORMALIZE_ENABLED = true;
export const DEFAULT_NORMALIZE_TARGET_DB = -20;

export interface NormalizeResult {
    pcm: Buffer;
    gain: number;     // linear gain actually applied (1 when not applied)
    applied: boolean; // false → pcm is the input buffer, untouched
}

function dbToLinear(db: number): number {
    return Math.pow(10, db / 20);
}

// Normalize a 48k/16-bit/stereo PCM buffer toward `targetDbfs` (a negative dBFS
// value, e.g. -20). Measures RMS + peak across every sample (both channels
// together — we want a single coherent gain, not per-channel balance changes),
// then derives one gain bounded by the peak ceiling and the max-boost cap.
export function normalizePcm16(pcm: Buffer, targetDbfs: number): NormalizeResult {
    const sampleCount = pcm.length >>> 1; // int16 samples across L+R
    if (sampleCount === 0) return { pcm, gain: 1, applied: false };

    let sumSq = 0;
    let peak = 0;
    for (let i = 0; i < sampleCount; i++) {
        const s = pcm.readInt16LE(i * 2) / FULL_SCALE;
        sumSq += s * s;
        const a = s < 0 ? -s : s;
        if (a > peak) peak = a;
    }

    const rms = Math.sqrt(sumSq / sampleCount);
    if (rms < MIN_RMS || peak === 0) return { pcm, gain: 1, applied: false };

    // Gain that lands RMS on target, then clamp: never exceed the peak ceiling
    // (no clipping) and never boost beyond the cap (no amplifying silence).
    const peakLimit = PEAK_CEILING / peak;
    const gain = Math.min(dbToLinear(targetDbfs) / rms, peakLimit, MAX_BOOST_LINEAR);

    if (Math.abs(gain - 1) < UNITY_EPSILON) return { pcm, gain: 1, applied: false };

    const out = Buffer.allocUnsafe(sampleCount * 2);
    for (let i = 0; i < sampleCount; i++) {
        let v = Math.round(pcm.readInt16LE(i * 2) * gain);
        if (v > 32767) v = 32767;          // ceiling makes this rare, but a
        else if (v < -32768) v = -32768;   // rounded peak can still graze it
        out.writeInt16LE(v, i * 2);
    }
    return { pcm: out, gain, applied: true };
}
