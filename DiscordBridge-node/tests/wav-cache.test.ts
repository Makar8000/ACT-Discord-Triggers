import { test } from 'node:test';
import { strict as assert } from 'node:assert';

import { WavCache } from '../src/wav-cache.js';

test('miss returns null', () => {
    const c = new WavCache();
    assert.equal(c.get('any/path.wav', 1), null);
});

test('hit returns the cached buffer when mtime matches', () => {
    const c = new WavCache();
    const pcm = Buffer.from([1, 2, 3, 4]);
    c.set('a.wav', 100, pcm);
    const got = c.get('a.wav', 100);
    assert.ok(got);
    assert.equal(Buffer.compare(got, pcm), 0);
});

test('mtime mismatch invalidates the entry', () => {
    const c = new WavCache();
    c.set('a.wav', 100, Buffer.from([1, 2, 3]));
    assert.equal(c.get('a.wav', 101), null);
    // Subsequent get with the original mtime still misses (entry was evicted).
    assert.equal(c.get('a.wav', 100), null);
});

test('replacing same key updates mtime + buffer in place', () => {
    const c = new WavCache();
    c.set('a.wav', 100, Buffer.from([1]));
    c.set('a.wav', 200, Buffer.from([9, 9]));
    assert.equal(c.size, 1);
    const got = c.get('a.wav', 200);
    assert.ok(got);
    assert.equal(Buffer.compare(got, Buffer.from([9, 9])), 0);
});

test('mtime-mismatch eviction also drops the entry (caller is expected to re-set)', () => {
    // Realistic flow: speakFile stats the file, gets the current mtime, calls
    // get(path, currentMtime). If the cache had an entry with an older mtime,
    // it's stale → evicted. The caller then decodes and calls set(path, currentMtime, ...).
    const c = new WavCache();
    c.set('a.wav', 100, Buffer.from([1]));
    assert.equal(c.get('a.wav', 200), null); // simulates "file was modified"
    assert.equal(c.size, 0);
});

test('LRU evicts the oldest entry when capacity is exceeded', () => {
    const c = new WavCache(3);
    c.set('a.wav', 1, Buffer.from([1]));
    c.set('b.wav', 1, Buffer.from([2]));
    c.set('c.wav', 1, Buffer.from([3]));
    assert.equal(c.size, 3);
    c.set('d.wav', 1, Buffer.from([4]));
    assert.equal(c.size, 3);
    // 'a' was the oldest insertion → should be gone.
    assert.equal(c.get('a.wav', 1), null);
    assert.ok(c.get('b.wav', 1));
    assert.ok(c.get('c.wav', 1));
    assert.ok(c.get('d.wav', 1));
});

test('LRU touch on get: accessed entry survives next eviction', () => {
    const c = new WavCache(3);
    c.set('a.wav', 1, Buffer.from([1]));
    c.set('b.wav', 1, Buffer.from([2]));
    c.set('c.wav', 1, Buffer.from([3]));
    // Touch 'a' so it becomes most-recent.
    assert.ok(c.get('a.wav', 1));
    // Insert 'd' → 'b' (now oldest) should evict, not 'a'.
    c.set('d.wav', 1, Buffer.from([4]));
    assert.ok(c.get('a.wav', 1));
    assert.equal(c.get('b.wav', 1), null);
});

test('clear empties the cache', () => {
    const c = new WavCache();
    c.set('a.wav', 1, Buffer.from([1]));
    c.set('b.wav', 1, Buffer.from([2]));
    c.clear();
    assert.equal(c.size, 0);
    assert.equal(c.get('a.wav', 1), null);
});
