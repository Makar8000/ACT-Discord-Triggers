// Length-prefixed JSON frame helpers, matching the wire format implemented by
// PipeServer._tryReadFrames and _sendFrame: 4-byte LE length, then UTF-8 JSON.

export function encodeFrame(obj: unknown): Buffer {
    const json = Buffer.from(JSON.stringify(obj), 'utf8');
    const len = Buffer.alloc(4);
    len.writeUInt32LE(json.length, 0);
    return Buffer.concat([len, json]);
}

export interface DecodedFrames {
    frames: Array<Record<string, unknown>>;
    rest: Buffer;
}

export function decodeFrames(buf: Buffer): DecodedFrames {
    const frames: Array<Record<string, unknown>> = [];
    let offset = 0;
    while (buf.length - offset >= 4) {
        const len = buf.readUInt32LE(offset);
        if (buf.length - offset < 4 + len) break;
        const json = buf.subarray(offset + 4, offset + 4 + len).toString('utf8');
        frames.push(JSON.parse(json) as Record<string, unknown>);
        offset += 4 + len;
    }
    return { frames, rest: buf.subarray(offset) };
}

// Build a raw 4-byte LE length prefix for tests that want to inject malformed
// frames (e.g. length=0, length>MAX_FRAME_BYTES) without going through encodeFrame.
export function lenPrefix(len: number): Buffer {
    const b = Buffer.alloc(4);
    b.writeUInt32LE(len >>> 0, 0);
    return b;
}
