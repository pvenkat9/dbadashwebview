# Fix "Unable to connect to remote server" on localhost:8080
# Run elevated: .\scripts\ensure-iis-running.ps1

param(
    [string]$SitePath = "C:\inetpub\dbadash",
    [string]$SiteName = "DBADashWebView",
    [string]$AppPoolName = "DBADashWebView",
    [int]$Port = 8080
)

$ErrorActionPreference = "Continue"
Write-Host "=== DBA Dash IIS repair (port $Port) ===" -ForegroundColor Cyan

# Requires admin
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "WARN: Run PowerShell as Administrator for IIS changes." -ForegroundColor Yellow
}

Import-Module WebAdministration -ErrorAction SilentlyContinue
if (-not (Get-Module WebAdministration)) {
    Write-Host "FAIL: WebAdministration module not available. Install IIS management tools." -ForegroundColor Red
    exit 1
}

# 1. Is anything listening on 8080?
Write-Host "`n--- Port $Port ---" -ForegroundColor Yellow
$listeners = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue
if ($listeners) {
    Write-Host "OK: Something is listening on port $Port" -ForegroundColor Green
    $listeners | Format-Table LocalAddress, LocalPort, OwningProcess -AutoSize
} else {
    Write-Host "NOT listening on port $Port (this causes 'Unable to connect')" -ForegroundColor Red
}

# 2. Find site by path or name
Write-Host "`n--- IIS sites ---" -ForegroundColor Yellow
Get-Website | Format-Table Name, State, PhysicalPath, @{n='Bindings';e={$_.bindings.Collection.bindingInformation}} -AutoSize

$site = Get-Website -Name $SiteName -ErrorAction SilentlyContinue
if (-not $site) {
  $site = Get-Website | Where-Object { $_.physicalPath -like "*dbadash*" } | Select-Object -First 1
  if ($site) {
    $SiteName = $site.Name
    Write-Host "Using site: $SiteName" -ForegroundColor Gray
  }
}

# 3. Create site if missing
if (-not $site) {
    Write-Host "`nSite not found — creating $SiteName on port $Port ..." -ForegroundColor Yellow
    if (-not (Test-Path $SitePath)) {
        New-Item -ItemType Directory -Path $SitePath -Force | Out-Null
    }
    if (-not (Test-Path "IIS:\AppPools\$AppPoolName")) {
        New-WebAppPool -Name $AppPoolName
        Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name "managedRuntimeVersion" -Value ""
    }
    New-Website -Name $SiteName -PhysicalPath $SitePath -ApplicationPool $AppPoolName -Port $Port -Force
    $site = Get-Website -Name $SiteName
}

# 4. App pool
Write-Host "`n--- App pool $AppPoolName ---" -ForegroundColor Yellow
$pool = Get-WebAppPoolState -Name $AppPoolName -ErrorAction SilentlyContinue
if (-not $pool) {
    Write-Host "Creating app pool $AppPoolName ..." -ForegroundColor Yellow
    New-WebAppPool -Name $AppPoolName
    Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name "managedRuntimeVersion" -Value ""
}
Write-Host "Pool state: $(Get-WebAppPoolState -Name $AppPoolName).Value"
if ((Get-WebAppPoolState -Name $AppPoolName).Value -ne "Started") {
    Start-WebAppPool -Name $AppPoolName
    Start-Sleep -Seconds 2
}

# 5. Start website
Write-Host "`n--- Website $SiteName ---" -ForegroundColor Yellow
Write-Host "Site state: $(Get-Website -Name $SiteName).State"
if ((Get-Website -Name $SiteName).State -ne "Started") {
    Start-Website -Name $SiteName
    Start-Sleep -Seconds 2
}

# 6. Files
Write-Host "`n--- Site files $SitePath ---" -ForegroundColor Yellow
$need = @("DBADashWebView.dll", "web.config", "DBADashWebView.deps.json")
foreach ($f in $need) {
    $p = Join-Path $SitePath $f
    if (Test-Path $p) { Write-Host "  OK  $f" -ForegroundColor Green }
    else { Write-Host "  MISSING  $f" -ForegroundColor Red }
}

# 7. .NET 8 hosting
Write-Host "`n--- .NET runtimes ---" -ForegroundColor Yellow
dotnet --list-runtimes | Select-String "Microsoft.AspNetCore.App 8"
if (-not (dotnet --list-runtimes | Select-String "Microsoft.AspNetCore.App 8")) {
    Write-Host "FAIL: Install ASP.NET Core 8 Hosting Bundle:" -ForegroundColor Red
    Write-Host "  https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor Yellow
}

# 8. Console test (app itself)
Write-Host "`n--- Console test (bypasses IIS) ---" -ForegroundColor Yellow
if (Test-Path (Join-Path $SitePath "DBADashWebView.dll")) {
    $env:ASPNETCORE_URLS = "http://127.0.0.1:5099"
    Push-Location $SitePath
    $proc = Start-Process -FilePath "dotnet" -ArgumentList ".\DBADashWebView.dll" -PassThru -WindowStyle Hidden
    Start-Sleep -Seconds 5
    try {
        $t = Invoke-RestMethod "http://127.0.0.1:5099/api/health" -TimeoutSec 5
        Write-Host "App OK: $($t | ConvertTo-Json -Compress)" -ForegroundColor Green
    } catch {
        Write-Host "App FAILED to start — IIS cannot work until this passes:" -ForegroundColor Red
        Write-Host $_.Exception.Message
    }
    Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
    Pop-Location
}

# 9. Stdout log
$logDir = Join-Path $SitePath "logs"
if (Test-Path $logDir) {
    $latest = Get-ChildItem $logDir -Filter "stdout*.log" -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if ($latest) {
        Write-Host "`n--- Latest stdout log ($($latest.Name)) ---" -ForegroundColor Yellow
        Get-Content $latest.FullName -Tail 15
    }
}

# 10. HTTP test via IIS
Write-Host "`n--- IIS HTTP test ---" -ForegroundColor Yellow
Start-Sleep -Seconds 2
$listeners2 = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue
if (-not $listeners2) {
    Write-Host "Still nothing on port $Port. Try: iisreset" -ForegroundColor Red
    exit 1
}
try {
    $h = Invoke-RestMethod "http://localhost:$Port/api/health" -TimeoutSec 15
    Write-Host "SUCCESS: $($h | ConvertTo-Json -Compress)" -ForegroundColor Green
} catch {
    Write-Host "Port is open but HTTP failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Check logs in $logDir and run: iisreset" -ForegroundColor Yellow
    exit 1
}

Write-Host "`nDone. Site should be at http://localhost:$Port" -ForegroundColor Green
