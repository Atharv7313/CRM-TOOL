# Starts the services needed for the Reports page, each on its expected port,
# then launches the Angular frontend. Run from the repo root:  .\run-all.ps1
# Stops everything cleanly with Ctrl+C in this window (frontend), or close the
# spawned service windows.

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot

function Ensure-PortFree([int]$port, [string]$name) {
    $conn = Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue
    if ($conn) {
        $procId = ($conn.OwningProcess | Select-Object -First 1)
        $proc = Get-Process -Id $procId -ErrorAction SilentlyContinue
        Write-Host "Port $port ($name) already in use by PID $procId ($($proc.ProcessName)) — leaving it running." -ForegroundColor Yellow
        return $false   # already running, don't start another
    }
    return $true        # free, safe to start
}

function Start-Service-Window([string]$name, [string]$dir, [int]$port) {
    if (Ensure-PortFree $port $name) {
        Write-Host "Starting $name on http://localhost:$port ..." -ForegroundColor Cyan
        Start-Process powershell -ArgumentList @(
            '-NoExit', '-Command',
            "Set-Location '$dir'; dotnet run --no-build --no-launch-profile --urls 'http://localhost:$port'"
        )
    }
}

# 1) Pre-build all services so they start instantly (no compile delay at runtime)
$services = @(
    (Join-Path $root 'AuthService'),
    (Join-Path $root 'AuditLogService.API'),
    (Join-Path $root 'PatientService'),
    (Join-Path $root 'ProtocolService'),
    (Join-Path $root 'SampleService'),
    (Join-Path $root 'ReportingService'),
    (Join-Path $root 'NotificationService')
)
Write-Host "Building all services..." -ForegroundColor DarkGray
foreach ($svcDir in $services) {
    $name = Split-Path $svcDir -Leaf
    Write-Host "  dotnet build $name" -ForegroundColor DarkGray
    dotnet build $svcDir -c Debug --nologo -q
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build failed for $name — aborting." -ForegroundColor Red
        exit 1
    }
}

# 2) Launch each service in its own window (already built, starts immediately)
Start-Service-Window 'AuthService'         (Join-Path $root 'AuthService')         5015
Start-Service-Window 'AuditLogService'     (Join-Path $root 'AuditLogService.API') 5298
Start-Service-Window 'PatientService'      (Join-Path $root 'PatientService')      5276
Start-Service-Window 'ProtocolService'     (Join-Path $root 'ProtocolService')     5054
Start-Service-Window 'SampleService'       (Join-Path $root 'SampleService')       5025
Start-Service-Window 'ReportingService'    (Join-Path $root 'ReportingService')    5278
Start-Service-Window 'NotificationService' (Join-Path $root 'NotificationService') 5103

# 3) Give the services a moment to initialize (DB connections, DI, etc.)
Write-Host "Waiting 10s for services to initialize..." -ForegroundColor DarkGray
Start-Sleep -Seconds 10

# 3) Frontend (runs in THIS window; Ctrl+C stops it)
Write-Host "Starting Angular frontend on http://localhost:53719 ..." -ForegroundColor Cyan
Set-Location (Join-Path $root 'Frontend')
npm start
