# Build script for DiscordBridge-node — produces a release directory containing:
#
#   DiscordBridge.exe   — tiny C#/AOT launcher that spawns node.exe with bundle.js
#   node.exe            — Node.js runtime (copied from current node install)
#   bundle.js           — bundled JS (esbuild-built, externals excluded)
#   node_modules\       — externals (native @snazzah/davey + pure-JS opus/sodium)
#
# Why a launcher instead of Node SEA: Node's SEA mode routes `require()` calls
# through embedderRequire, which only resolves built-in modules and the bundled
# main script. External requires like @snazzah/davey can't be loaded from a
# sibling node_modules folder under SEA. Plain `node.exe bundle.js` works
# correctly — so we ship that pattern with a launcher to satisfy the plugin's
# "spawn DiscordBridge.exe" expectation.

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false
Set-Location $PSScriptRoot

Write-Host "==> Cleaning dist\"
if (Test-Path dist) { Remove-Item -Recurse -Force dist }
New-Item -ItemType Directory dist | Out-Null

Write-Host "==> Bundling JS (esbuild)"
node esbuild.config.mjs

Write-Host "==> Building launcher (.NET 10 AOT)"
dotnet publish launcher\launcher.csproj -c Release -r win-x64 -o launcher\publish --nologo -v quiet
Copy-Item launcher\publish\DiscordBridge.exe -Destination dist\DiscordBridge.exe

Write-Host "==> Copying node.exe"
$nodeExe = (Get-Command node).Source
Copy-Item $nodeExe -Destination dist\node.exe

Write-Host "==> Staging external deps next to .exe"
# These packages are NOT bundled by esbuild (see esbuild.config.mjs `external`)
# and must ship as files for Node's CJS resolver to find at runtime.
#
#   @snazzah/davey           — DAVE wrapper (JS, requires its native sibling below)
#   @snazzah/davey-win32-x64-msvc — DAVE native (Rust NAPI .node, win-x64 only)
#   opusscript               — pure-JS Opus encoder (uses its own .wasm via __dirname)
#   libsodium-wrappers       — pure-JS libsodium API (depends on libsodium)
#   libsodium                — WASM blob loaded by libsodium-wrappers
#
# If you bump deps and a require fails at startup, audit this list — npm hoists
# transitive packages here and our esbuild externals list silently misses them.
$externals = @(
    '@snazzah/davey',
    '@snazzah/davey-win32-x64-msvc',
    'opusscript',
    'libsodium-wrappers',
    'libsodium'
)
$stageDir = 'dist\node_modules'
foreach ($pkg in $externals) {
    $src = "node_modules\$pkg"
    $dst = "$stageDir\$pkg"
    if (-not (Test-Path $src)) { Write-Warning "Missing dep: $src"; continue }
    New-Item -ItemType Directory -Path (Split-Path $dst) -Force -ErrorAction SilentlyContinue | Out-Null
    Copy-Item -Recurse -Force $src $dst
    # Also copy each external's own nested node_modules if any
    $nestedNm = "node_modules\$pkg\node_modules"
    if (Test-Path $nestedNm) {
        Copy-Item -Recurse -Force $nestedNm "$stageDir\$pkg\node_modules"
    }
}

# Self-test: spawn the bridge and confirm BRIDGE_READY appears on stdout.
# Catches packaging bugs (missing native module, broken require path) at build
# time instead of at deploy time.
Write-Host "==> Build self-test (spawn bridge, expect BRIDGE_READY)"
$testPipe = "build-self-test-$(Get-Random)"
$proc = Start-Process -FilePath dist\DiscordBridge.exe -ArgumentList $testPipe `
    -PassThru -NoNewWindow -RedirectStandardOutput dist\selftest.stdout -RedirectStandardError dist\selftest.stderr
$ok = $false
for ($i = 0; $i -lt 50; $i++) {
    Start-Sleep -Milliseconds 100
    if (Test-Path dist\selftest.stdout) {
        $line = Get-Content dist\selftest.stdout -ErrorAction SilentlyContinue -TotalCount 1
        if ($line -and $line.StartsWith('BRIDGE_READY')) { $ok = $true; break }
    }
    if ($proc.HasExited) { break }
}
if (-not $proc.HasExited) { try { Stop-Process -Id $proc.Id -Force } catch { } }
if (-not $ok) {
    Write-Error "Build self-test FAILED. stdout / stderr:"
    Get-Content dist\selftest.stdout -ErrorAction SilentlyContinue
    Get-Content dist\selftest.stderr -ErrorAction SilentlyContinue
    exit 1
}
Remove-Item dist\selftest.stdout, dist\selftest.stderr, dist\DiscordBridge.log -ErrorAction SilentlyContinue
Write-Host "    PASS"

Write-Host ""
Write-Host "==> Done."
$exeSize = (Get-Item dist\DiscordBridge.exe).Length / 1MB
$nodeSize = (Get-Item dist\node.exe).Length / 1MB
$bundleSize = (Get-Item dist\bundle.js).Length / 1MB
$nmSize = (Get-ChildItem -Recurse $stageDir | Measure-Object -Property Length -Sum).Sum / 1MB
Write-Host ("    DiscordBridge.exe   {0:N1} MB" -f $exeSize)
Write-Host ("    node.exe            {0:N1} MB" -f $nodeSize)
Write-Host ("    bundle.js           {0:N1} MB" -f $bundleSize)
Write-Host ("    node_modules\       {0:N1} MB" -f $nmSize)
Get-ChildItem dist | Format-Table Name,Length,LastWriteTime
