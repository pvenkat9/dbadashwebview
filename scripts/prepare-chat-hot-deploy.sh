#!/bin/zsh
# Fast Windows deploy package: ALL runtime DLLs + full wwwroot (link-only chat UI).
set -e
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
OUT="$ROOT/chat-hot-deploy"

if [[ ! -f "$ROOT/publish/DBADashWebView.dll" ]]; then
  echo "Run ./scripts/build-publish.sh first"
  exit 1
fi

rm -rf "$OUT"
mkdir -p "$OUT"

echo "Copying all publish DLLs and config..."
cp "$ROOT/publish"/*.dll "$OUT/" 2>/dev/null || true
cp "$ROOT/publish/DBADashWebView.deps.json" "$OUT/"
cp "$ROOT/publish/DBADashWebView.runtimeconfig.json" "$OUT/"
cp "$ROOT/publish/web.config" "$OUT/" 2>/dev/null || true

if [[ -d "$ROOT/publish/runtimes" ]]; then
  cp -R "$ROOT/publish/runtimes" "$OUT/"
fi

if [[ -d "$ROOT/publish/wwwroot" ]]; then
  echo "Copying full wwwroot from publish..."
  cp -R "$ROOT/publish/wwwroot" "$OUT/"
fi

echo ""
echo "Prepared: $OUT"
echo "DLL count: $(ls -1 "$OUT"/*.dll 2>/dev/null | wc -l | tr -d ' ')"
test -f "$OUT/DBADashWebView.AgenticChat.dll" || { echo "ERROR: DBADashWebView.AgenticChat.dll missing"; exit 1; }
test -f "$OUT/Trimble.AgenticChat.Core.dll" || { echo "ERROR: Trimble.AgenticChat.Core.dll missing"; exit 1; }
test -f "$OUT/Newtonsoft.Json.dll" || { echo "ERROR: Newtonsoft.Json.dll missing"; exit 1; }
test -f "$OUT/wwwroot/index.html" || { echo "ERROR: wwwroot/index.html missing — run build-publish.sh"; exit 1; }
test -f "$OUT/wwwroot/agentic-chat-shell.html" || { echo "ERROR: agentic-chat-shell.html missing — run build-publish.sh"; exit 1; }
echo "OK: chat plugin + Trimble + Newtonsoft + wwwroot present"
