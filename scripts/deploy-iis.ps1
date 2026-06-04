# Deploy DBA Dash WebView to IIS on Windows (elevated PowerShell).
# Place dbadash-webview.zip at C:\deploy\ then:
#   .\deploy-iis.ps1
#   .\test-windows-agentic-chat.ps1 -BaseUrl http://localhost:8080

param(
    [string]$ZipPath = "C:\deploy\dbadash-webview.zip",
    [string]$SiteName = "DBADashWebView",
    [string]$AppPoolName = "DBADashWebView",
    [string]$PhysicalPath = "C:\inetpub\dbadash",
    [int]$Port = 8080,
    [switch]$FullReset
)

$ErrorActionPreference = "Stop"
$deployFull = Join-Path $PSScriptRoot "deploy-full-iis.ps1"
if (-not (Test-Path $deployFull)) {
    Write-Error "Missing deploy-full-iis.ps1"
}

if (-not (Test-Path $ZipPath)) {
    Write-Error "Zip not found: $ZipPath — copy dbadash-webview.zip from Mac build"
}

$extractRoot = "C:\deploy\dbadash-publish"
if (Test-Path $extractRoot) { Remove-Item $extractRoot -Recurse -Force }
Expand-Archive $ZipPath -DestinationPath $extractRoot -Force

$sourceDir = Join-Path $extractRoot "publish"
if (-not (Test-Path $sourceDir)) {
    $sourceDir = $extractRoot
}

if ($FullReset) {
    Import-Module WebAdministration
    Stop-Website -Name $SiteName -ErrorAction SilentlyContinue
    Remove-Website -Name $SiteName -ErrorAction SilentlyContinue
    Stop-WebAppPool -Name $AppPoolName -ErrorAction SilentlyContinue
    Remove-WebAppPool -Name $AppPoolName -ErrorAction SilentlyContinue
    if (Test-Path $PhysicalPath) { Remove-Item $PhysicalPath -Recurse -Force }
}

& $deployFull -SourceDir $sourceDir -SitePath $PhysicalPath -SiteName $SiteName -AppPoolName $AppPoolName -Port $Port

Write-Host "`nRun tests:" -ForegroundColor Cyan
Write-Host "  .\scripts\test-windows-agentic-chat.ps1 -BaseUrl http://localhost:$Port"
