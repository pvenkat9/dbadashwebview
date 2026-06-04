# Diagnose ASP.NET Core 500.30 startup failure on IIS
# Run elevated: .\diagnose-iis-startup.ps1 -SitePath "C:\inetpub\dbadash"

param(
    [string]$SitePath = "C:\inetpub\dbadash"
)

$ErrorActionPreference = "Continue"
Write-Host "=== DBA Dash WebView startup diagnostics ===" -ForegroundColor Cyan
Write-Host "Site path: $SitePath`n"

if (-not (Test-Path $SitePath)) {
    Write-Host "FAIL: Site path does not exist." -ForegroundColor Red
    exit 1
}

$requiredCore = @(
    "DBADashWebView.dll",
    "DBADashWebView.deps.json",
    "DBADashWebView.runtimeconfig.json",
    "Microsoft.Data.SqlClient.dll",
    "web.config"
)

$requiredChat = @(
    "Trimble.AgenticChat.Core.dll",
    "Newtonsoft.Json.dll"
)

Write-Host "--- Core files (app will not start without these) ---" -ForegroundColor Yellow
$missingCore = @()
foreach ($f in $requiredCore) {
    $p = Join-Path $SitePath $f
    if (Test-Path $p) {
        $info = Get-Item $p
        Write-Host "  OK  $f  ($($info.Length) bytes)" -ForegroundColor Green
    } else {
        Write-Host "  MISSING  $f" -ForegroundColor Red
        $missingCore += $f
    }
}

Write-Host "`n--- Agentic Chat plugin (dashboard works without these; chat disabled) ---" -ForegroundColor Yellow
$missingChat = @()
foreach ($f in $requiredChat) {
    $p = Join-Path $SitePath $f
    if (Test-Path $p) {
        $info = Get-Item $p
        Write-Host "  OK  $f  ($($info.Length) bytes)" -ForegroundColor Green
    } else {
        Write-Host "  MISSING  $f" -ForegroundColor Yellow
        $missingChat += $f
    }
}

if ($missingCore.Count -gt 0) {
    Write-Host "`nMissing CORE files cause HTTP 500.30. Deploy the full publish folder (not only wwwroot or one DLL)." -ForegroundColor Red
}
if ($missingChat.Count -gt 0) {
    Write-Host "`nMissing chat DLLs: /api/health should still work; /api/chat/health returns disabled." -ForegroundColor Yellow
}

Write-Host "`n--- .NET runtimes (need Microsoft.AspNetCore.App 8.x) ---" -ForegroundColor Yellow
dotnet --list-runtimes | Select-String "Microsoft.AspNetCore.App 8"

Write-Host "`n--- Try starting app from console (shows real error) ---" -ForegroundColor Yellow
$env:ASPNETCORE_URLS = "http://127.0.0.1:5099"
Push-Location $SitePath
try {
    & dotnet .\DBADashWebView.dll 2>&1 | Select-Object -First 40
} catch {
    Write-Host $_.Exception.Message -ForegroundColor Red
} finally {
    Pop-Location
}

Write-Host "`n--- IIS stdout log (if enabled) ---" -ForegroundColor Yellow
$logDir = Join-Path $SitePath "logs"
if (Test-Path $logDir) {
    Get-ChildItem $logDir -Filter "stdout*.log" | Sort-Object LastWriteTime -Descending | Select-Object -First 1 | ForEach-Object {
        Write-Host "Latest: $($_.FullName)" -ForegroundColor Gray
        Get-Content $_.FullName -Tail 30
    }
} else {
    Write-Host "No logs folder. Enable stdoutLogEnabled=true in web.config and recycle app pool." -ForegroundColor Yellow
}

if ($missingCore.Count -gt 0) { exit 1 }
