# Fix HTTP 500.30

## Root cause

The main `DBADashWebView.dll` was loading **Trimble** at startup. If `Trimble.AgenticChat.Core.dll` was missing or mismatched on IIS, **the whole site died** (500.30 on `/api/health`).

The app is now split:

| DLL | Role |
|-----|------|
| `DBADashWebView.dll` | Dashboard — **no Trimble reference** |
| `DBADashWebView.AgenticChat.dll` | Chat plugin |
| `Trimble.AgenticChat.Core.dll` | Trimble (chat only) |

Dashboard starts even if chat DLLs are missing. Chat returns `disabled` instead of crashing IIS.

## Deploy (required)

On Mac:

```bash
./scripts/build-publish.sh
```

On Windows (elevated):

```powershell
Expand-Archive C:\deploy\dbadash-webview.zip -DestinationPath C:\deploy\dbadash-publish -Force
.\scripts\deploy-full-iis.ps1 -SourceDir C:\deploy\dbadash-publish\publish
```

**Must delete old DLLs before copy** — the script does this.

## Verify

```powershell
Invoke-RestMethod http://localhost:8080/api/health        # must work
Invoke-RestMethod http://localhost:8080/api/chat/health   # healthy or disabled JSON
```

## Emergency: dashboard only (no chat)

In `C:\inetpub\dbadash\appsettings.json` set:

```json
"AgenticChat": {
  "Enabled": false,
  ...
}
```

Recycle app pool. Dashboard works; chat off.

## "Unable to connect" (not 500.30)

Nothing is listening on port **8080** — IIS site or app pool is **stopped**, or the site was never created.

Run elevated:

```powershell
.\scripts\ensure-iis-running.ps1
```

Or manually:

```powershell
Import-Module WebAdministration
Start-WebAppPool -Name "DBADashWebView"
Start-Website -Name "DBADashWebView"
Get-NetTCPConnection -LocalPort 8080 -State Listen
```

## Still 500.30?

```powershell
cd C:\inetpub\dbadash
dotnet .\DBADashWebView.dll
```

Check `logs\stdout*.log`. Confirm ASP.NET Core 8 hosting bundle:

```powershell
dotnet --list-runtimes | findstr "AspNetCore.App 8"
```
