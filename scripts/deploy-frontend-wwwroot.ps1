# Replace IIS wwwroot only (link-only chat UI). Run after build-publish on Mac.
#   Expand-Archive C:\deploy\dbadash-webview.zip -DestinationPath C:\deploy\dbadash-publish -Force
#   .\scripts\deploy-frontend-wwwroot.ps1 -SourceDir C:\deploy\dbadash-publish\publish\wwwroot

param(
    [string]$SourceDir = "C:\deploy\dbadash-publish\publish\wwwroot",
    [string]$SitePath = "C:\inetpub\dbadash",
    [string]$AppPoolName = "DBADashWebView"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path (Join-Path $SourceDir "index.html"))) {
    Write-Error "Source wwwroot missing index.html — run build-publish.sh on Mac first"
}

$dest = Join-Path $SitePath "wwwroot"
Write-Host "=== Replace wwwroot at $dest ===" -ForegroundColor Cyan

Stop-WebAppPool -Name $AppPoolName -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2

if (Test-Path $dest) {
    Remove-Item $dest -Recurse -Force
}

Copy-Item $SourceDir $dest -Recurse -Force

if (-not (Test-Path (Join-Path $dest "agentic-chat-shell.html"))) {
    Write-Error "agentic-chat-shell.html missing — run build-publish.sh on Mac"
}

Start-WebAppPool -Name $AppPoolName
Write-Host "Done. Hard-refresh browser (Ctrl+Shift+R)." -ForegroundColor Green
