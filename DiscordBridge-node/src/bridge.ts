import * as net from 'node:net';
import * as log from './file-log.js';
import { DiscordHost } from './discord-host.js';
import { PipeServer } from './pipe-server.js';

async function main(): Promise<void> {
    log.init();
    log.info(`argv: ${process.argv.slice(2).join(' ')}`);

    const args = process.argv.slice(2);
    const [pipeName] = args;
    if (!pipeName) {
        process.stderr.write('Usage: DiscordBridge <pipe-name>\n');
        log.error('missing pipe-name argument');
        process.exit(1);
    }
    const pipePath = `\\\\.\\pipe\\${pipeName}`;
    log.info(`creating pipe server '${pipePath}'`);

    const host = new DiscordHost();

    const server = net.createServer({ allowHalfOpen: false });

    server.on('error', (err: Error) => {
        log.error('pipe server error', err);
        process.stderr.write(`BRIDGE_FATAL ${err.message}\n`);
        process.exit(2);
    });

    server.on('connection', (socket: net.Socket) => {
        log.info('client connected');
        // Stop accepting new clients (one plugin per bridge)
        server.close();
        const pipe = new PipeServer(socket, host);
        pipe.run();
        // When the plugin disconnects (pipe close or pipe error), the bridge has
        // no purpose. Tear down discord.js (closes the gateway WebSocket) and exit.
        // Mirrors the .NET bridge which exits when ReadFrameAsync returns null.
        const onPeerGone = async (): Promise<void> => {
            log.info('peer gone; deinit + exit');
            try { await host.deinit(); } catch { /* ignore */ }
            // Give file-log a tick to flush, then exit.
            setImmediate(() => process.exit(0));
        };
        socket.once('close', () => { void onPeerGone(); });
        socket.once('error', () => { void onPeerGone(); });
    });

    // Accept exactly one client.
    server.maxConnections = 1;

    await new Promise<void>((resolve, reject) => {
        server.once('error', reject);
        server.listen(pipePath, () => {
            server.removeListener('error', reject);
            resolve();
        });
    });

    // Plugin's BridgeProcess.cs scans stdout for line starting with "BRIDGE_READY".
    process.stdout.write(`BRIDGE_READY pipe=${pipeName}\n`);
    log.info('BRIDGE_READY printed; waiting for client');

    const shutdown = async (sig: string): Promise<void> => {
        log.info(`signal ${sig}; shutting down`);
        try { await host.deinit(); } catch { /* ignore */ }
        try { server.close(); } catch { /* ignore */ }
        process.exit(0);
    };
    process.on('SIGINT', () => { void shutdown('SIGINT'); });
    process.on('SIGTERM', () => { void shutdown('SIGTERM'); });
    process.on('SIGHUP', () => { void shutdown('SIGHUP'); });

    process.on('uncaughtException', (err: Error) => {
        // After an uncaughtException Node's state is undefined — limping along
        // produces incoherent IPC. Flush, write the fatal marker, and exit so
        // the plugin's BridgeProcess sees the OnExited event and tears down.
        log.error('uncaughtException', err);
        process.stderr.write(`BRIDGE_FATAL ${err.stack || err.message}\n`);
        process.exit(2);
    });
    process.on('unhandledRejection', (reason: unknown) => {
        const detail = reason instanceof Error ? (reason.stack || reason.message) : String(reason);
        log.error('unhandledRejection: ' + detail);
    });
}

main().catch((err: unknown) => {
    try { log.error('main crashed', err); } catch { /* ignore */ }
    const detail = err instanceof Error ? (err.stack || err.message) : String(err);
    process.stderr.write(`BRIDGE_FATAL ${detail}\n`);
    process.exit(2);
});
