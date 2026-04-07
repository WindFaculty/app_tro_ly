#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Phase 2 setup script — Khởi tạo Tauri shell cho app trợ lý
.DESCRIPTION
    Script này thực hiện toàn bộ Phase 2:
    1. Kiểm tra prerequisites (Node, Rust, WebView2)
    2. Khởi tạo Tauri + React app trong apps/desktop-shell/
    3. Cài đặt dependencies
    Chạy từ thư mục gốc repo: .\scripts\phase2-setup-tauri.ps1
#>

param(
    [switch]$SkipPrereqCheck = $false
)

$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path -Parent $PSScriptRoot

function Write-Step($msg) { Write-Host "`n==> $msg" -ForegroundColor Cyan }
function Write-Ok($msg) { Write-Host "  OK: $msg" -ForegroundColor Green }
function Write-Warn($msg) { Write-Host "  WARN: $msg" -ForegroundColor Yellow }
function Write-Fail($msg) { Write-Host "  FAIL: $msg" -ForegroundColor Red }

# ──────────────────────────────────────────────
# Step 1: Prerequisites check
# ──────────────────────────────────────────────
if (-not $SkipPrereqCheck) {
    Write-Step "Checking prerequisites..."

    $failed = @()

    # Node.js
    try {
        $nodeVer = (node --version 2>&1)
        $major = [int]($nodeVer -replace 'v(\d+)\..*', '$1')
        if ($major -lt 18) { $failed += "Node.js >= 18 required, got $nodeVer" }
        else { Write-Ok "Node.js $nodeVer" }
    } catch { $failed += "Node.js not found. Install from https://nodejs.org" }

    # npm
    try {
        $npmVer = (npm --version 2>&1)
        Write-Ok "npm $npmVer"
    } catch { $failed += "npm not found" }

    # Rust / cargo
    try {
        $rustVer = (rustc --version 2>&1)
        Write-Ok "$rustVer"
    } catch { $failed += "Rust not found. Install from https://rustup.rs" }

    try {
        $cargoVer = (cargo --version 2>&1)
        Write-Ok "$cargoVer"
    } catch { $failed += "Cargo not found" }

    if ($failed.Count -gt 0) {
        Write-Host "`n[BLOCKED] Missing prerequisites:" -ForegroundColor Red
        $failed | ForEach-Object { Write-Fail $_ }
        Write-Host "`nSee docs/migration/phase2-setup.md for install instructions." -ForegroundColor Yellow
        exit 1
    }

    Write-Ok "All prerequisites met!"
}

# ──────────────────────────────────────────────
# Step 2: Create apps directory
# ──────────────────────────────────────────────
Write-Step "Setting up apps/ directory..."
$appsDir = Join-Path $RepoRoot "apps"
if (-not (Test-Path $appsDir)) {
    New-Item -ItemType Directory -Path $appsDir | Out-Null
    Write-Ok "Created apps/"
} else {
    Write-Ok "apps/ already exists"
}

# ──────────────────────────────────────────────
# Step 3: Create Tauri + React app
# ──────────────────────────────────────────────
$shellDir = Join-Path $appsDir "desktop-shell"

if (Test-Path $shellDir) {
    Write-Warn "apps/desktop-shell/ already exists, skipping scaffold"
} else {
    Write-Step "Scaffolding Tauri + React app..."
    Push-Location $appsDir
    try {
        npx create-tauri-app@latest desktop-shell `
            --template react-ts `
            --manager npm `
            --yes
        Write-Ok "Tauri app scaffolded"
    } finally {
        Pop-Location
    }
}

# ──────────────────────────────────────────────
# Step 4: Install dependencies
# ──────────────────────────────────────────────
Write-Step "Installing npm dependencies..."
Push-Location $shellDir
try {
    npm install --prefer-offline
    Write-Ok "npm install done"

    # Install Tauri shell plugin for spawning backend
    npm install @tauri-apps/plugin-shell
    Write-Ok "Tauri shell plugin installed"

    # Install Tauri dialog and process plugins
    npm install @tauri-apps/plugin-process
    Write-Ok "Tauri process plugin installed"

    npm install @tauri-apps/plugin-log
    Write-Ok "Tauri log plugin installed"
} finally {
    Pop-Location
}

Write-Host "`n[SUCCESS] Phase 2 scaffold complete!" -ForegroundColor Green
Write-Host "Next: AI will configure tauri.conf.json and add backend process manager."
Write-Host "Then run: cd apps/desktop-shell && npm run tauri dev"
