import * as fs from 'node:fs';
import * as path from 'node:path';
import * as os from 'node:os';

type FileLogLevel = 'INFO' | 'WARN' | 'ERROR';

let logPath: string | null = null;
const writeQueue: string[] = [];
let writing = false;

const MAX_BYTES = 5 * 1024 * 1024;

function exeDir(): string {
    // Launcher: process.execPath is node.exe sitting next to bundle.js / launcher exe.
    // Dev (npm run): also node.exe — fall back to cwd of the main script in that case.
    try {
        const dir = path.dirname(process.execPath);
        if (process.execPath.toLowerCase().endsWith('node.exe')) {
            return process.cwd();
        }
        return dir;
    } catch {
        return process.cwd();
    }
}

export function init(): void {
    try {
        logPath = path.join(exeDir(), 'DiscordBridge.log');
        try {
            const st = fs.statSync(logPath);
            if (st.size > MAX_BYTES) fs.unlinkSync(logPath);
        } catch { /* file may not exist */ }
        info(`==== Bridge starting (pid=${process.pid}, node=${process.version}, os=${os.platform()} ${os.release()}, exe=${process.execPath}) ====`);
    } catch { /* swallow */ }
}

function ts(): string {
    const d = new Date();
    const pad = (n: number, w = 2) => String(n).padStart(w, '0');
    return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())} ` +
           `${pad(d.getHours())}:${pad(d.getMinutes())}:${pad(d.getSeconds())}.${pad(d.getMilliseconds(), 3)}`;
}

function write(level: FileLogLevel, msg: string): void {
    if (!logPath) return;
    const line = `${ts()} ${level} ${msg}${os.EOL}`;
    writeQueue.push(line);
    drain();
}

function drain(): void {
    if (writing || writeQueue.length === 0 || !logPath) return;
    writing = true;
    const batch = writeQueue.splice(0, writeQueue.length).join('');
    fs.appendFile(logPath, batch, { encoding: 'utf8' }, () => {
        writing = false;
        if (writeQueue.length > 0) drain();
    });
}

export function info(msg: string): void { write('INFO', msg); }
export function warn(msg: string): void { write('WARN', msg); }

export function error(msg: string, err?: unknown): void {
    if (err !== undefined) {
        if (err instanceof Error) {
            const name = err.name || 'Error';
            const message = err.message || String(err);
            write('ERROR', `${msg} :: ${name}: ${message}`);
            if (err.stack) write('ERROR', err.stack);
        } else {
            write('ERROR', `${msg} :: ${String(err)}`);
        }
    } else {
        write('ERROR', msg);
    }
}

export function errMsg(e: unknown): string {
    return e instanceof Error ? (e.message || String(e)) : String(e);
}
