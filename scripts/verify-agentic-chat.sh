#!/bin/zsh
# Verify Agentic Chat integration locally BEFORE deploying to Windows.
set -e
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

fail() { echo "FAIL: $1"; exit 1 }

echo "=== 1. Structure checks ==="
grep -q 'AgenticChatLoader' "$ROOT/backend/AgenticChatLoader.cs" || fail "missing AgenticChatLoader"
grep -q 'ReferenceOutputAssembly>false' "$ROOT/backend/DBADashWebView.csproj" || fail "main csproj must not reference Trimble at compile time"
grep -q 'Trimble.AgenticChat.Core' "$ROOT/backend/DBADashWebView.AgenticChat/DBADashWebView.AgenticChat.csproj" || fail "plugin must reference Trimble"
grep -q 'SerializeResponseMessage' "$ROOT/backend/DBADashWebView.AgenticChat/AgenticChatBootstrap.cs" || fail "Bootstrap must use SerializeResponseMessage"
! grep -q 'Trimble.AgenticChat' "$ROOT/backend/Program.cs" || fail "Program.cs must not import Trimble"

grep -q 'agentic-chat-shell.html' "$ROOT/frontend/src/components/AgenticChat.tsx" || fail "AgenticChat.tsx must load agentic-chat-shell.html"
grep -q '<iframe' "$ROOT/frontend/src/components/AgenticChat.tsx" || fail "AgenticChat.tsx must embed shell via iframe"
test -f "$ROOT/frontend/public/agentic-chat-shell.html" || fail "missing frontend/public/agentic-chat-shell.html"
grep -q 'embed.stage.trimble-ai.com' "$ROOT/frontend/public/agentic-chat-shell.html" || fail "shell must use embed.stage.trimble-ai.com"

echo "OK: plugin layout"

echo "=== 2. Build ==="
cd "$ROOT/backend"
dotnet build -c Release -v q
OUT="bin/Release/net8.0"
test -f "$OUT/DBADashWebView.AgenticChat.dll" || fail "plugin DLL not copied to output"
test -f "$OUT/Trimble.AgenticChat.Core.dll" || fail "Trimble DLL not copied to output"

echo "=== 3. Smoke test (main app starts without chat DLLs) ==="
# Pick a random high port to avoid 'address already in use' from earlier runs
PORT=$(( 52000 + RANDOM % 8000 ))
LOG="/tmp/dba-smoke-$$.log"

cleanup_smoke() {
  [[ -n "$SMOKE_PID" ]] && kill "$SMOKE_PID" 2>/dev/null || true
  wait "$SMOKE_PID" 2>/dev/null || true
  [[ -f "$OUT/Trimble.AgenticChat.Core.dll.bak" ]] && mv -f "$OUT/Trimble.AgenticChat.Core.dll.bak" "$OUT/Trimble.AgenticChat.Core.dll"
  [[ -f "$OUT/DBADashWebView.AgenticChat.dll.bak" ]] && mv -f "$OUT/DBADashWebView.AgenticChat.dll.bak" "$OUT/DBADashWebView.AgenticChat.dll"
  rm -f "$LOG"
}
trap cleanup_smoke EXIT

mv "$OUT/Trimble.AgenticChat.Core.dll" "$OUT/Trimble.AgenticChat.Core.dll.bak"
mv "$OUT/DBADashWebView.AgenticChat.dll" "$OUT/DBADashWebView.AgenticChat.dll.bak"

export ASPNETCORE_URLS="http://127.0.0.1:${PORT}"
dotnet "$OUT/DBADashWebView.dll" >"$LOG" 2>&1 &
SMOKE_PID=$!
sleep 4

if ! kill -0 "$SMOKE_PID" 2>/dev/null; then
  echo "--- dotnet exited early; log: ---"
  cat "$LOG"
  fail "main DLL exited before health check (see log above)"
fi

if curl -sf "http://127.0.0.1:${PORT}/api/health" | grep -q healthy; then
  echo "OK: /api/health works without Trimble DLLs (port $PORT)"
else
  echo "--- last log lines: ---"
  tail -20 "$LOG"
  fail "/api/health failed without Trimble DLLs"
fi

trap - EXIT
cleanup_smoke

echo ""
echo "All checks passed. Run: ./scripts/build-publish.sh"
