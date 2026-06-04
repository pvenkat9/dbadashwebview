# Run on Windows after deploy (IIS or Kestrel). Verifies Agentic Chat + Trimble agents API.
param(
    [string]$BaseUrl = "http://localhost:8080",
    [string]$User = "admin",
    [string]$Password = "admin",
    [switch]$SkipAgentsProbe
)

$ErrorActionPreference = "Stop"

function Fail($msg) {
    Write-Host "FAIL: $msg" -ForegroundColor Red
    exit 1
}

Write-Host "=== DBA Dash Agentic Chat test ===" -ForegroundColor Cyan
Write-Host "Base URL: $BaseUrl`n"

try {
    $health = Invoke-RestMethod "$BaseUrl/api/health" -TimeoutSec 30
    if ($health.status -ne "healthy") { Fail "/api/health status not healthy" }
    Write-Host "OK /api/health" -ForegroundColor Green
} catch {
    Fail "/api/health — $($_.Exception.Message)"
}

try {
    $chatHealth = Invoke-RestMethod "$BaseUrl/api/chat/health" -TimeoutSec 30
    if (-not $chatHealth.enabled) {
        Fail "/api/chat/health — plugin disabled: $($chatHealth | ConvertTo-Json -Compress)"
    }
    Write-Host "OK /api/chat/health (plugin enabled)" -ForegroundColor Green
} catch {
    Fail "/api/chat/health — $($_.Exception.Message)"
}

try {
    $r = Invoke-WebRequest "$BaseUrl/agentic-chat-shell.html" -UseBasicParsing -TimeoutSec 30
    if ($r.StatusCode -ne 200) { Fail "agentic-chat-shell.html HTTP $($r.StatusCode)" }
    Write-Host "OK agentic-chat-shell.html" -ForegroundColor Green
} catch {
    Fail "agentic-chat-shell.html — $($_.Exception.Message)"
}

$loginBody = @{ username = $User; password = $Password } | ConvertTo-Json
try {
    $login = Invoke-RestMethod "$BaseUrl/api/auth/login" -Method Post -Body $loginBody -ContentType "application/json" -TimeoutSec 30
    $jwt = $login.token
    if (-not $jwt) { Fail "login returned no token" }
    Write-Host "OK login" -ForegroundColor Green
} catch {
    Fail "login — $($_.Exception.Message)"
}

$headers = @{ Authorization = "Bearer $jwt" }

try {
    $cfg = Invoke-RestMethod "$BaseUrl/api/chat/config" -Headers $headers -TimeoutSec 30
    if (-not $cfg.agentId) { Fail "/api/chat/config missing agentId" }
    Write-Host "OK /api/chat/config (agent $($cfg.agentId))" -ForegroundColor Green
} catch {
    Fail "/api/chat/config — $($_.Exception.Message)"
}

try {
    $diag = Invoke-RestMethod "$BaseUrl/api/chat/diagnostics" -Headers $headers -TimeoutSec 60
    $diag | ConvertTo-Json -Depth 5 | Write-Host

    if ($diag.PSObject.Properties.Name -contains "agentsApiOk") {
        if ($diag.agentsApiOk -eq $true) {
            Write-Host "`nPASS: agents.stage API authorized — embedded chat should work." -ForegroundColor Green
        } else {
            $detail = $diag.agentsApiDetail
            if (-not $detail) { $detail = ($diag.issues -join "; ") }
            Fail "agents.stage not OK — $detail. Set AgenticChat:TrimbleAccessToken or get Agents scope from Trimble."
        }
    } else {
        Write-Host "`nWARN: older build (no agentsApiOk). Redeploy latest dbadash-webview.zip." -ForegroundColor Yellow
        if ($diag.issues -and $diag.issues.Count -gt 0) {
            Fail ($diag.issues -join "; ")
        }
    }
} catch {
    Fail "/api/chat/diagnostics — $($_.Exception.Message)"
}

if (-not $SkipAgentsProbe) {
    Write-Host "`n=== Direct agents.stage probe (optional) ===" -ForegroundColor Cyan
    $appsettingsPath = Join-Path $PSScriptRoot "..\publish\appsettings.json"
    if (-not (Test-Path $appsettingsPath)) {
        $appsettingsPath = "C:\inetpub\dbadash\appsettings.json"
    }
    if (Test-Path $appsettingsPath) {
        $json = Get-Content $appsettingsPath -Raw | ConvertFrom-Json
        $ac = $json.AgenticChat
        if ($ac.TrimbleClientId -and $ac.TrimbleClientSecret) {
            $pair = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("$($ac.TrimbleClientId):$($ac.TrimbleClientSecret)"))
            $body = "grant_type=client_credentials&scope=$([uri]::EscapeDataString($ac.TrimbleChatScope))&audience=$([uri]::EscapeDataString($ac.TrimbleChatAudience))"
            try {
                $tok = Invoke-RestMethod $ac.TrimbleTokenUrl -Method Post -Headers @{
                    Authorization = "Basic $pair"
                    "Content-Type" = "application/x-www-form-urlencoded"
                } -Body $body -TimeoutSec 30
                $agentUrl = "$($ac.TrimbleAgentsApiBaseUrl)/v2/agents/$($ac.AgentId)"
                Invoke-RestMethod $agentUrl -Headers @{ Authorization = "Bearer $($tok.access_token)" } -TimeoutSec 30 | Out-Null
                Write-Host "PASS direct agents.stage probe" -ForegroundColor Green
            } catch {
                Write-Host "WARN direct agents probe failed (embed may 401): $($_.Exception.Message)" -ForegroundColor Yellow
            }
        }
    }
}

Write-Host "`nAll checks passed." -ForegroundColor Green
