'use strict';

const fs = require('node:fs');
const path = require('node:path');
const os = require('node:os');

let logPath = null;
const writeQueue = [];
let writing = false;

const MAX_BYTES = 5 * 1024 * 1024;

function exeDir() {
    // SEA: process.execPath is the bundled .exe.
    // Dev: process.execPath is node.exe — fall back to cwd of main script.
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

function init() {
    try {
        logPath = path.join(exeDir(), 'DiscordBridge.log');
        try {
            const st = fs.statSync(logPath);
            if (st.size > MAX_BYTES) fs.unlinkSync(logPath);
        } catch { /* file may not exist */ }
        info(`==== Bridge starting (pid=${process.pid}, node=${process.version}, os=${os.platform()} ${os.release()}, exe=${process.execPath}) ====`);
    } catch { /* swallow */ }
}

function ts() {
    const d = new Date();
    const pad = (n, w = 2) => String(n).padStart(w, '0');
    return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())} ` +
           `${pad(d.getHours())}:${pad(d.getMinutes())}:${pad(d.getSeconds())}.${pad(d.getMilliseconds(), 3)}`;
}

function write(level, msg) {
    if (!logPath) return;
    const line = `${ts()} ${level} ${msg}${os.EOL}`;
    writeQueue.push(line);
    drain();
}

function drain() {
    if (writing || writeQueue.length === 0 || !logPath) return;
    writing = true;
    const batch = writeQueue.splice(0, writeQueue.length).join('');
    fs.appendFile(logPath, batch, { encoding: 'utf8' }, () => {
        writing = false;
        if (writeQueue.length > 0) drain();
    });
}

function info(msg)  { write('INFO',  msg); }
function warn(msg)  { write('WARN',  msg); }
function error(msg, err) {
    if (err) {
        const name = err.name || 'Error';
        const message = err.message || String(err);
        write('ERROR', `${msg} :: ${name}: ${message}`);
        if (err.stack) write('ERROR', err.stack);
    } else {
        write('ERROR', msg);
    }
}

module.exports = { init, info, warn, error };
