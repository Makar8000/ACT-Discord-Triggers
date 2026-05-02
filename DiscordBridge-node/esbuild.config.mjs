import esbuild from 'esbuild';
import { fileURLToPath } from 'node:url';
import path from 'node:path';

const __dirname = path.dirname(fileURLToPath(import.meta.url));

// Banner runs before any bundled code. Teaches Node to resolve `external`
// requires (the ones we left out of the bundle) from the node_modules folder
// we copy next to DiscordBridge.exe at build time. Without this, SEA mode
// treats the externals as built-in modules and throws ERR_UNKNOWN_BUILTIN_MODULE.
const banner = `
"use strict";
(() => {
    const path = require("node:path");
    const Module = require("node:module");
    const exeDir = path.dirname(process.execPath);
    const localModules = path.join(exeDir, "node_modules");
    process.env.NODE_PATH = process.env.NODE_PATH ? (localModules + path.delimiter + process.env.NODE_PATH) : localModules;
    Module.Module._initPaths();
})();
`;

await esbuild.build({
    entryPoints: [path.join(__dirname, 'src/bridge.js')],
    bundle: true,
    platform: 'node',
    target: 'node22',
    format: 'cjs',
    outfile: path.join(__dirname, 'dist/bundle.js'),
    banner: { js: banner },
    // Native modules and packages with __dirname / require.resolve runtime tricks
    // can't be statically bundled. Mark them external so SEA loads them from
    // node_modules at runtime via Node's normal require resolution.
    external: [
        '@snazzah/davey',          // native (.node)
        'opusscript',              // ships pure-JS but uses path tricks
        'libsodium-wrappers',
        'libsodium-wrappers-sumo',
        // prism-media optionally requires these; we don't use any of them at runtime
        // (we feed raw PCM, never go through ffmpeg or the alt opus/sodium libs),
        // but esbuild can't tell that without help.
        'ffmpeg-static',
        '@discordjs/opus',
        'node-opus',
        'mediaplex',
        'sodium-native',
        'sodium',
        'tweetnacl',
        // discord.js optional perf shims
        'bufferutil',
        'utf-8-validate',
        'erlpack',
        'zlib-sync',
    ],
    minify: false,
    sourcemap: false,
    logLevel: 'info',
});
