#!/bin/zsh
# Build and pack DBA Dash WebView for IIS deploy.
set -e
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

"$ROOT/scripts/verify-agentic-chat.sh"

echo "=== Frontend ==="
cd frontend && npm run build && cd "$ROOT"

echo "=== Backend publish ==="
cd backend
dotnet restore --configfile "$ROOT/nuget.config"
dotnet publish -c Release -o "$ROOT/publish" --configfile "$ROOT/nuget.config"
cd "$ROOT"

# Replace wwwroot entirely so old hashed JS bundles are not left beside new ones
rm -rf publish/wwwroot
mkdir -p publish/wwwroot
cp -r frontend/dist/* publish/wwwroot/
test -f publish/wwwroot/agentic-chat-shell.html || { echo "ERROR: agentic-chat-shell.html missing from wwwroot"; exit 1; }
if ! grep -rq 'agentic-chat-shell.html' publish/wwwroot/assets/*.js 2>/dev/null; then
  echo "ERROR: built JS must reference agentic-chat-shell.html"
  exit 1
fi

echo "=== Verify publish ==="
for f in DBADashWebView.dll DBADashWebView.AgenticChat.dll Trimble.AgenticChat.Core.dll Newtonsoft.Json.dll; do
  [[ -f "publish/$f" ]] || { echo "ERROR: missing publish/$f"; exit 1; }
done
# Main app must NOT require Trimble at process load (plugin is separate)
if strings publish/DBADashWebView.dll | grep -q "Trimble.AgenticChat"; then
  echo "WARN: main DLL still mentions Trimble (should only be in plugin)"
fi
echo "OK: all DLLs present"
ls -la publish/DBADashWebView.dll publish/DBADashWebView.AgenticChat.dll publish/Trimble.AgenticChat.Core.dll

rm -f dbadash-webview.zip
zip -r dbadash-webview.zip publish
echo "Done: $ROOT/dbadash-webview.zip"
