# Hot-update site files — REMOVES old DLLs first (mixed DLLs cause HTTP 500.30).
# Run elevated after copying chat-hot-deploy/ to the server.

param(
    [string]$SourceDir = "C:\deploy\chat-hot",
    [string]$SitePath = "C:\inetpub\dbadash",
    [string]$SiteName = "DBADashWebView",
    [string]$AppPoolName = "DBADashWebView"
)

$ErrorActionPreference = "Stop"
Import-Module WebAdministration

if (-not (Test-Path $SourceDir)) {
    Write-Error "Source not found: $SourceDir"
}

$mustHave = @(
    "DBADashWebView.dll",
    "DBADashWebView.deps.json",
    "DBADashWebView.runtimeconfig.json",
    "DBADashWebView.AgenticChat.dll",
    "Trimble.AgenticChat.Core.dll",
    "Newtonsoft.Json.dll"
)

foreach ($f in $mustHave) {
    if (-not (Test-Path (Join-Path $SourceDir $f))) {
        Write-Error "Missing in source: $f — run build-publish.sh + prepare-chat-hot-deploy.sh on Mac"
    }
}

Write-Host "Stopping app pool..." -ForegroundColor Cyan
Stop-WebAppPool -Name $AppPoolName -ErrorAction SilentlyContinue
Start-Sleep -Seconds 3

New-Item -ItemType Directory -Path $SitePath -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $SitePath "logs") -Force | Out-Null

# CRITICAL: delete old DLLs before copy — leftover DLLs + new deps.json = 500.30
Write-Host "Removing old DLLs from site..." -ForegroundColor Yellow
Get-ChildItem $SitePath -Filter "*.dll" -ErrorAction SilentlyContinue | Remove-Item -Force
if (Test-Path (Join-Path $SitePath "runtimes")) {
    Remove-Item (Join-Path $SitePath "runtimes") -Recurse -Force
}

Write-Host "Copying DLLs and config..." -ForegroundColor Cyan
Get-ChildItem $SourceDir -Filter "*.dll" | Copy-Item -Destination $SitePath -Force
Copy-Item (Join-Path $SourceDir "DBADashWebView.deps.json") $SitePath -Force
Copy-Item (Join-Path $SourceDir "DBADashWebView.runtimeconfig.json") $SitePath -Force

if (Test-Path (Join-Path $SourceDir "web.config")) {
    Copy-Item (Join-Path $SourceDir "web.config") $SitePath -Force
}

if (Test-Path (Join-Path $SourceDir "runtimes")) {
    Copy-Item (Join-Path $SourceDir "runtimes") (Join-Path $SitePath "runtimes") -Recurse -Force
}

if (Test-Path (Join-Path $SourceDir "wwwroot")) {
    $wwwroot = Join-Path $SitePath "wwwroot"
    if (Test-Path $wwwroot) {
        Write-Host "Removing old wwwroot before copy..." -ForegroundColor Yellow
        Remove-Item $wwwroot -Recurse -Force
    }
    Copy-Item (Join-Path $SourceDir "wwwroot") $wwwroot -Recurse -Force
}

$webConfig = Join-Path $SitePath "web.config"
if (Test-Path $webConfig) {
    (Get-Content $webConfig -Raw) -replace 'stdoutLogEnabled="false"', 'stdoutLogEnabled="true"' |
        Set-Content $webConfig -Encoding UTF8
}

Write-Host "Console smoke test..." -ForegroundColor Cyan
$env:ASPNETCORE_URLS = "http://127.0.0.1:5099"
Push-Location $SitePath
$p = Start-Process -FilePath "dotnet" -ArgumentList ".\DBADashWebView.dll" -PassThru -NoNewWindow
Start-Sleep -Seconds 5
Pop-Location
try {
    $t = Invoke-RestMethod "http://127.0.0.1:5099/api/health" -TimeoutSec 5
    Write-Host "Console OK: $($t | ConvertTo-Json -Compress)" -ForegroundColor Green
} catch {
    Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue
    Write-Host "Console startup FAILED — run diagnose-iis-startup.ps1" -ForegroundColor Red
    throw
}
Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue

Write-Host "Starting app pool and website..." -ForegroundColor Cyan
Start-WebAppPool -Name $AppPoolName
if (Get-Website -Name $SiteName -ErrorAction SilentlyContinue) {
    Start-Website -Name $SiteName
} else {
    Write-Host "WARN: IIS site '$SiteName' not found. Run ensure-iis-running.ps1" -ForegroundColor Yellow
}
Start-Sleep -Seconds 5

Write-Host "Testing IIS..." -ForegroundColor Cyan
$h = Invoke-RestMethod "http://localhost:8080/api/health" -TimeoutSec 20
Write-Host "/api/health OK: $($h | ConvertTo-Json -Compress)" -ForegroundColor Green
$c = Invoke-RestMethod "http://localhost:8080/api/chat/health" -TimeoutSec 20
Write-Host "/api/chat/health: $($c | ConvertTo-Json -Compress)" -ForegroundColor Green

Write-Host "Deploy complete." -ForegroundColor Green
