// LRU cache of decoded + resampled PCM buffers, keyed by absolute path.
// mtime is captured at insert time; reads check it against the current file
// stat so editing a sound effect in place invalidates the entry naturally.
//
// Sized for ACT trigger sounds: short clips (<2s typical, ~200 KB each at
// 48k/16/stereo). 32 entries × ~200 KB ≈ 6 MB worst case, still trivial.

export interface CachedWav {
    mtimeMs: number;
    pcm: Buffer;
}

export class WavCache {
    private readonly map = new Map<string, CachedWav>();
    private readonly maxEntries: number;

    constructor(maxEntries = 32) {
        this.maxEntries = maxEntries;
    }

    // Returns the cached PCM if (path, mtime) matches; null otherwise.
    // On hit, promotes the entry to most-recently-used.
    get(path: string, mtimeMs: number): Buffer | null {
        const entry = this.map.get(path);
        if (!entry) return null;
        if (entry.mtimeMs !== mtimeMs) {
            this.map.delete(path);
            return null;
        }
        // LRU touch: re-insert at end. Map preserves insertion order.
        this.map.delete(path);
        this.map.set(path, entry);
        return entry.pcm;
    }

    set(path: string, mtimeMs: number, pcm: Buffer): void {
        if (this.map.has(path)) this.map.delete(path);
        this.map.set(path, { mtimeMs, pcm });
        while (this.map.size > this.maxEntries) {
            const oldest = this.map.keys().next().value;
            if (oldest === undefined) break;
            this.map.delete(oldest);
        }
    }

    clear(): void { this.map.clear(); }

    get size(): number { return this.map.size; }
}
