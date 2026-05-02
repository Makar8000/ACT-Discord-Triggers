import { EventEmitter } from 'node:events';

// Minimal stub of net.Socket exposing only the surface PipeServer touches:
//   - 'data' / 'error' / 'close' / 'end' events
//   - write(buf, cb)
//   - destroy() / end()
//   - writable boolean
//
// Tests push bytes inbound via `sock.emit('data', chunk)` and read outbound
// frames from `sock.writes`.
export class FakeSocket extends EventEmitter {
    public writable = true;
    public destroyed = false;
    public ended = false;
    public readonly writes: Buffer[] = [];

    write(chunk: Buffer | string, cb?: (err?: Error | null) => void): boolean {
        if (!this.writable) {
            // Real net.Socket would emit 'error' here; tests don't exercise that path.
            if (cb) setImmediate(cb);
            return false;
        }
        const buf = typeof chunk === 'string' ? Buffer.from(chunk, 'utf8') : chunk;
        this.writes.push(buf);
        // Mimic Node's behavior: invoke the write callback on next tick.
        if (cb) setImmediate(cb);
        return true;
    }

    destroy(): void {
        this.destroyed = true;
        this.writable = false;
    }

    end(): void {
        this.ended = true;
        this.writable = false;
    }

    // Concatenated outbound bytes — convenience for decodeFrames callers.
    drainedWrites(): Buffer {
        return Buffer.concat(this.writes);
    }
}
