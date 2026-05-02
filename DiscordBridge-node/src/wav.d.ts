// Minimal type surface for the `wav` npm package — we only use Reader.
// The package ships no types and we want to avoid an extra @types/* dep.
declare module 'wav' {
    import { Transform } from 'node:stream';

    export interface WavFormat {
        audioFormat: number;   // 1 = PCM
        endianness: 'LE' | 'BE';
        channels: number;
        sampleRate: number;
        byteRate: number;
        blockAlign: number;
        bitDepth: number;
        signed?: boolean;
    }

    // Reader is a Transform: write WAV bytes in, read decoded PCM bytes out.
    // Emits 'format' once the header is parsed, then 'data' chunks of PCM.
    // We override on/once to add the 'format' event; the standard Transform
    // events ('data', 'end', 'error', 'close') need explicit overloads here
    // because subclass method declarations shadow the base class overloads.
    export class Reader extends Transform {
        constructor();
        on(event: 'format', listener: (format: WavFormat) => void): this;
        on(event: 'data', listener: (chunk: Buffer) => void): this;
        on(event: 'end', listener: () => void): this;
        on(event: 'error', listener: (err: Error) => void): this;
        on(event: 'close', listener: () => void): this;
        on(event: string | symbol, listener: (...args: never[]) => void): this;
        once(event: 'format', listener: (format: WavFormat) => void): this;
        once(event: 'data', listener: (chunk: Buffer) => void): this;
        once(event: 'end', listener: () => void): this;
        once(event: 'error', listener: (err: Error) => void): this;
        once(event: 'close', listener: () => void): this;
        once(event: string | symbol, listener: (...args: never[]) => void): this;
    }
}
