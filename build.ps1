# Top-level release build: produces ./release/ with everything an end-user
# drops into ACT's plugins directory.
#
# Steps:
#   1. msbuild plugin (net48, Release)
#   2. type-check + bundle bridge TS (esbuild)
#   3. copy node.exe + stage native/WASM externals
#   4. self-test bridge spawn (asserts BRIDGE_READY on stdout)
#   5. assemble ./release/ from plugin DLL + bridge dist
#
# For bridge-only iteration, use the npm scripts in DiscordBridge-node/
# (typecheck / bundle / test). Those stay independent of this script.

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false
Set-Location $PSScriptRoot

# --- 1. Plugin (net48 via dotnet build) ---
# net48 reference assemblies are auto-restored as a transitive dep of the SDK's
# net48 targeting; Costura.Fody weaves under the SDK's MSBuild correctly,
# producing a single merged DLL. So `dotnet build` is enough — no Visual
# Studio install required.
Write-Host "==> Building plugin (net48)"
dotnet build ACT_DiscordTriggers\ACT_DiscordTriggers.csproj -c Release -nologo -v:quiet
if ($LASTEXITCODE -ne 0) { Write-Error "Plugin build failed"; exit 1 }

# --- 2. Bridge: type-check, bundle, stage externals, self-test ---
Push-Location DiscordBridge-node
try {
    Write-Host "==> Cleaning DiscordBridge-node\dist\"
    if (Test-Path dist) { Remove-Item -Recurse -Force dist }
    New-Item -ItemType Directory dist | Out-Null

    Write-Host "==> Type-checking (tsc --noEmit)"
    # --no-install: fail loudly if `typescript` devDep is missing instead of
    # silently fetching from the registry mid-build.
    npx --no-install tsc --noEmit
    if ($LASTEXITCODE -ne 0) { Write-Error "Type check failed"; exit 1 }

    Write-Host "==> Bundling JS (esbuild)"
    node esbuild.config.mjs

    Write-Host "==> Copying node.exe"
    $nodeExe = (Get-Command node).Source
    Copy-Item $nodeExe -Destination dist\node.exe

    Write-Host "==> Staging external deps next to node.exe"
    # These packages are NOT bundled by esbuild (see esbuild.config.mjs `external`)
    # and must ship as files for Node's CJS resolver to find at runtime.
    #
    #   @snazzah/davey                — DAVE wrapper (JS, requires its native sibling below)
    #   @snazzah/davey-win32-x64-msvc — DAVE native (Rust NAPI .node, win-x64 only)
    #   opusscript                    — pure-JS Opus encoder (uses its own .wasm via __dirname)
    #   libsodium-wrappers            — pure-JS libsodium API (depends on libsodium)
    #   libsodium                     — WASM blob loaded by libsodium-wrappers
    #
    # If you bump deps and a require fails at startup, audit this list — npm
    # hoists transitive packages here and our esbuild externals list silently
    # misses them.
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
        $nestedNm = "node_modules\$pkg\node_modules"
        if (Test-Path $nestedNm) {
            Copy-Item -Recurse -Force $nestedNm "$stageDir\$pkg\node_modules"
        }
    }

    # Self-test: spawn the bridge and confirm BRIDGE_READY appears on stdout.
    # Catches packaging bugs (missing native module, broken require path) at
    # build time instead of at deploy time.
    Write-Host "==> Build self-test (spawn bridge, expect BRIDGE_READY)"
    $testPipe = "build-self-test-$(Get-Random)"
    $proc = Start-Process -FilePath dist\node.exe -ArgumentList "dist\bundle.js",$testPipe `
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
} finally {
    Pop-Location
}

# --- 3. Assemble ./release/ ---
Write-Host "==> Assembling release\"
if (Test-Path release) { Remove-Item -Recurse -Force release }
New-Item -ItemType Directory release | Out-Null
Copy-Item ACT_DiscordTriggers\bin\Release\net48\ACT_DiscordTriggers.dll release\
Copy-Item DiscordBridge-node\dist\node.exe release\
Copy-Item DiscordBridge-node\dist\bundle.js release\
Copy-Item -Recurse DiscordBridge-node\dist\node_modules release\node_modules

Write-Host ""
Write-Host "==> Done. Release contents in release\:"
$totalSize = (Get-ChildItem -Recurse release | Measure-Object -Property Length -Sum).Sum / 1MB
Write-Host ("    Total: {0:N1} MB" -f $totalSize)
Get-ChildItem release | ForEach-Object { Write-Host ("    {0,-32} {1}" -f $_.Name, $_.LastWriteTime) }
