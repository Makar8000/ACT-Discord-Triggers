import { test } from 'node:test';
import { strict as assert } from 'node:assert';
import { errMsg } from '../src/file-log.js';

test('errMsg(Error) returns the message', () => {
    assert.equal(errMsg(new Error('boom')), 'boom');
});

test('errMsg(Error) with empty message falls back to String(e)', () => {
    const e = new Error('');
    // Error('').toString() => 'Error', so the helper returns 'Error'.
    assert.equal(errMsg(e), 'Error');
});

test('errMsg(string) returns the string verbatim', () => {
    assert.equal(errMsg('plain string'), 'plain string');
});

test('errMsg(undefined) returns "undefined"', () => {
    assert.equal(errMsg(undefined), 'undefined');
});

test('errMsg(null) returns "null"', () => {
    assert.equal(errMsg(null), 'null');
});

test('errMsg(number) coerces via String()', () => {
    assert.equal(errMsg(42), '42');
});

test('errMsg(plain object) returns a non-empty string', () => {
    const out = errMsg({ foo: 1 });
    assert.equal(typeof out, 'string');
    assert.ok(out.length > 0);
});

test('errMsg(Error subclass) reads the message', () => {
    class MyError extends Error {
        constructor() { super('subclass msg'); this.name = 'MyError'; }
    }
    assert.equal(errMsg(new MyError()), 'subclass msg');
});
