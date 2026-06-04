# Full IIS deploy — replaces ALL binaries (fixes 500.30 from mixed old/new DLLs).
# Run elevated on Windows after copying dbadash-webview.zip to C:\deploy\
#
#   Expand-Archive C:\deploy\dbadash-webview.zip -DestinationPath C:\deploy\dbadash-publish -Force
#   .\scripts\deploy-full-iis.ps1 -SourceDir C:\deploy\dbadash-publish\publish

param(
    [string]$SourceDir = "C:\deploy\dbadash-publish\publish",
    [string]$SitePath = "C:\inetpub\dbadash",
    [string]$SiteName = "DBADashWebView",
    [string]$AppPoolName = "DBADashWebView",
    [int]$Port = 8080
)

$ErrorActionPreference = "Stop"
Import-Module WebAdministration

if (-not (Test-Path $SourceDir)) {
    Write-Error "Source not found: $SourceDir"
}

$coreFiles = @(
    "DBADashWebView.dll",
    "DBADashWebView.deps.json",
    "DBADashWebView.runtimeconfig.json",
    "web.config"
)

foreach ($f in $coreFiles) {
    if (-not (Test-Path (Join-Path $SourceDir $f))) {
        Write-Error "Source missing $f — extract dbadash-webview.zip from Mac build-publish"
    }
}

Write-Host "=== Full deploy to $SitePath ===" -ForegroundColor Cyan

# Backup production settings
$appsettingsBackup = $null
$appsettingsPath = Join-Path $SitePath "appsettings.json"
if (Test-Path $appsettingsPath) {
    $appsettingsBackup = Get-Content $appsettingsPath -Raw
    Write-Host "Backed up existing appsettings.json" -ForegroundColor Gray
}

$configBackup = $null
$configDir = Join-Path $SitePath "config"
if (Test-Path $configDir) {
    $configBackup = Join-Path $env:TEMP "dbadash-config-backup"
    if (Test-Path $configBackup) { Remove-Item $configBackup -Recurse -Force }
    Copy-Item $configDir $configBackup -Recurse
    Write-Host "Backed up config/ folder" -ForegroundColor Gray
}

Write-Host "Stopping app pool $AppPoolName..." -ForegroundColor Yellow
Stop-WebAppPool -Name $AppPoolName -ErrorAction SilentlyContinue
Start-Sleep -Seconds 3

# Remove stale DLLs and frontend assets (mixed versions cause HTTP 500.30 / old iframe bundles)
if (Test-Path $SitePath) {
    Write-Host "Removing old binaries from $SitePath..." -ForegroundColor Yellow
    Get-ChildItem $SitePath -Filter "*.dll" -ErrorAction SilentlyContinue | Remove-Item -Force
    Get-ChildItem $SitePath -Filter "*.deps.json" -ErrorAction SilentlyContinue | Remove-Item -Force
    Get-ChildItem $SitePath -Filter "*.runtimeconfig.json" -ErrorAction SilentlyContinue | Remove-Item -Force
    if (Test-Path (Join-Path $SitePath "runtimes")) {
        Remove-Item (Join-Path $SitePath "runtimes") -Recurse -Force
    }
    $wwwroot = Join-Path $SitePath "wwwroot"
    if (Test-Path $wwwroot) {
        Write-Host "Removing old wwwroot (stale index-*.js causes embedded chat errors)..." -ForegroundColor Yellow
        Remove-Item $wwwroot -Recurse -Force
    }
}

New-Item -ItemType Directory -Path $SitePath -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $SitePath "logs") -Force | Out-Null

Write-Host "Copying full publish output..." -ForegroundColor Yellow
Copy-Item (Join-Path $SourceDir "*") $SitePath -Recurse -Force

if ($appsettingsBackup) {
    Set-Content -Path $appsettingsPath -Value $appsettingsBackup -Encoding UTF8
    Write-Host "Restored production appsettings.json" -ForegroundColor Green
}

if ($configBackup) {
    Copy-Item "$configBackup\*" $configDir -Recurse -Force
    Write-Host "Restored config/ folder" -ForegroundColor Green
}

# Console smoke test (shows real startup error before IIS)
Write-Host "`n=== Console startup test ===" -ForegroundColor Cyan
$env:ASPNETCORE_URLS = "http://127.0.0.1:5099"
Push-Location $SitePath
$job = Start-Job -ScriptBlock {
    param($path)
    Set-Location $path
    & dotnet .\DBADashWebView.dll 2>&1
} -ArgumentList $SitePath
Start-Sleep -Seconds 5
$jobOutput = Receive-Job $job
Stop-Job $job -ErrorAction SilentlyContinue
Remove-Job $job -Force -ErrorAction SilentlyContinue
Pop-Location

if ($jobOutput) {
    $jobOutput | Select-Object -First 25 | ForEach-Object { Write-Host $_ }
}

try {
    $test = Invoke-RestMethod "http://127.0.0.1:5099/api/health" -TimeoutSec 5
    Write-Host "Console test OK: $($test | ConvertTo-Json -Compress)" -ForegroundColor Green
} catch {
    Write-Host "Console test FAILED — fix this before IIS will work:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host "Check: dotnet --list-runtimes | findstr AspNetCore 8" -ForegroundColor Yellow
    exit 1
}

# Stop console test on 5099
Get-NetTCPConnection -LocalPort 5099 -ErrorAction SilentlyContinue |
    ForEach-Object { Stop-Process -Id $_.OwningProcess -Force -ErrorAction SilentlyContinue }

# Ensure IIS site exists and is started (app pool alone is not enough)
if (-not (Get-Website -Name $SiteName -ErrorAction SilentlyContinue)) {
    Write-Host "Creating IIS site $SiteName on port $Port ..." -ForegroundColor Yellow
    if (-not (Test-Path "IIS:\AppPools\$AppPoolName")) {
        New-WebAppPool -Name $AppPoolName
        Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name "managedRuntimeVersion" -Value ""
    }
    New-Website -Name $SiteName -PhysicalPath $SitePath -ApplicationPool $AppPoolName -Port $Port -Force
}

Write-Host "`nStarting app pool and website..." -ForegroundColor Cyan
Start-WebAppPool -Name $AppPoolName
Start-Website -Name $SiteName
Start-Sleep -Seconds 5

$listening = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue
if (-not $listening) {
    Write-Host "ERROR: Nothing listening on port $Port. Run: .\scripts\ensure-iis-running.ps1" -ForegroundColor Red
    exit 1
}

$url = "http://localhost:$Port/api/health"
Write-Host "Testing $url ..." -ForegroundColor Cyan
$h = Invoke-RestMethod $url -TimeoutSec 20
Write-Host "IIS OK: $($h | ConvertTo-Json -Compress)" -ForegroundColor Green

$c = Invoke-RestMethod "http://localhost:$Port/api/chat/health" -TimeoutSec 20
Write-Host "Chat: $($c | ConvertTo-Json -Compress)" -ForegroundColor Green

Write-Host "`nDeploy complete." -ForegroundColor Green
