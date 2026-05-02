'use strict';

const net = require('node:net');
const log = require('./file-log');
const { DiscordHost } = require('./discord-host');
const { PipeServer } = require('./pipe-server');

async function main() {
    log.init();
    log.info(`argv: ${process.argv.slice(2).join(' ')}`);

    const args = process.argv.slice(2);
    if (args.length < 1) {
        process.stderr.write('Usage: DiscordBridge <pipe-name>\n');
        log.error('missing pipe-name argument');
        process.exit(1);
    }
    const pipeName = args[0];
    const pipePath = `\\\\.\\pipe\\${pipeName}`;
    log.info(`creating pipe server '${pipePath}'`);

    const host = new DiscordHost();

    const server = net.createServer({ allowHalfOpen: false });

    server.on('error', (err) => {
        log.error('pipe server error', err);
        process.stderr.write(`BRIDGE_FATAL ${err.message}\n`);
        process.exit(2);
    });

    server.on('connection', (socket) => {
        log.info('client connected');
        // Stop accepting new clients (one plugin per bridge)
        server.close();
        const pipe = new PipeServer(socket, host);
        pipe.run();
        // When the plugin disconnects (pipe close or pipe error), the bridge has
        // no purpose. Tear down discord.js (closes the gateway WebSocket) and exit.
        // Mirrors the .NET bridge which exits when ReadFrameAsync returns null.
        const onPeerGone = async () => {
            log.info('peer gone; deinit + exit');
            try { await host.deinit(); } catch { }
            // Give file-log a tick to flush, then exit.
            setImmediate(() => process.exit(0));
        };
        socket.once('close', onPeerGone);
        socket.once('error', onPeerGone);
    });

    // Accept exactly one client.
    server.maxConnections = 1;

    await new Promise((resolve, reject) => {
        server.listen(pipePath, (err) => err ? reject(err) : resolve());
    });

    // Plugin's BridgeProcess.cs scans stdout for line starting with "BRIDGE_READY".
    process.stdout.write(`BRIDGE_READY pipe=${pipeName}\n`);
    log.info('BRIDGE_READY printed; waiting for client');

    // Keep process alive — net.Server keeps the loop running.
    // Graceful shutdown on signals.
    const shutdown = async (sig) => {
        log.info(`signal ${sig}; shutting down`);
        try { await host.deinit(); } catch { }
        try { server.close(); } catch { }
        process.exit(0);
    };
    process.on('SIGINT', () => shutdown('SIGINT'));
    process.on('SIGTERM', () => shutdown('SIGTERM'));
    process.on('SIGHUP', () => shutdown('SIGHUP'));

    process.on('uncaughtException', (err) => {
        // After an uncaughtException Node's state is undefined — limping along
        // produces incoherent IPC. Flush, write the fatal marker, and exit so
        // the plugin's BridgeProcess sees the OnExited event and tears down.
        log.error('uncaughtException', err);
        process.stderr.write(`BRIDGE_FATAL ${err.stack || err.message}\n`);
        process.exit(2);
    });
    process.on('unhandledRejection', (reason) => {
        log.error('unhandledRejection: ' + (reason?.stack || reason));
    });
}

main().catch((err) => {
    try { log.error('main crashed', err); } catch { }
    process.stderr.write(`BRIDGE_FATAL ${err.stack || err.message}\n`);
    process.exit(2);
});
