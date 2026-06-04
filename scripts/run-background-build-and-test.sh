#!/bin/zsh
# Build publish zip, start local API, run Trimble + nonprod verification.
set -e
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
# Tests below may fail (Trimble 401, Mac plugin); build + server must succeed
LOG_DIR="${ROOT}/.test-logs"
mkdir -p "$LOG_DIR"
STAMP=$(date +%Y%m%d-%H%M%S)
BUILD_LOG="$LOG_DIR/build-$STAMP.log"
SERVER_LOG="$LOG_DIR/server-$STAMP.log"
PORT="${PORT:-5107}"
BASE_LOCAL="http://127.0.0.1:${PORT}"
BASE_NONPROD="${BASE_URL:-https://dbadash-nonprod.e-builder.net}"

log() { echo "[$(date +%H:%M:%S)] $*" | tee -a "$LOG_DIR/latest-summary.log"; }

log "=== 1. Build publish + zip ==="
cd "$ROOT"
if ! ./scripts/build-publish.sh >"$BUILD_LOG" 2>&1; then
  log "BUILD FAILED — see $BUILD_LOG"
  exit 1
fi
log "BUILD OK — dbadash-webview.zip ($(du -h "$ROOT/dbadash-webview.zip" | cut -f1))"

log "=== 2. Stop prior local servers ==="
pkill -f "DBADashWebView.dll" 2>/dev/null || true
sleep 1

log "=== 3. Start local publish API on $PORT ==="
cd "$ROOT/publish"
ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS="$BASE_LOCAL" \
  nohup dotnet DBADashWebView.dll >"$SERVER_LOG" 2>&1 &
SERVER_PID=$!
echo "$SERVER_PID" > "$LOG_DIR/server.pid"

for i in {1..30}; do
  if curl -sf "$BASE_LOCAL/api/health" >/dev/null 2>&1; then
    log "Local API up (pid $SERVER_PID)"
    break
  fi
  if ! kill -0 "$SERVER_PID" 2>/dev/null; then
    log "Server exited early — see $SERVER_LOG"
    tail -30 "$SERVER_LOG"
    exit 1
  fi
  sleep 1
done

curl -sf "$BASE_LOCAL/api/health" >/dev/null || { log "Local health timeout"; exit 1; }

log "=== 4. Local API tests ==="
{
  echo "--- /api/health ---"
  curl -sf "$BASE_LOCAL/api/health"
  echo ""
  echo "--- /api/chat/health ---"
  curl -sf "$BASE_LOCAL/api/chat/health"
  echo ""
  echo "--- shell ---"
  curl -sf -o /dev/null -w "agentic-chat-shell.html: %{http_code}\n" "$BASE_LOCAL/agentic-chat-shell.html"
  JWT=$(curl -sf -X POST "$BASE_LOCAL/api/auth/login" -H 'Content-Type: application/json' \
    -d '{"username":"admin","password":"admin"}' | python3 -c "import sys,json; print(json.load(sys.stdin).get('token',''))")
  if [[ -n "$JWT" ]]; then
    echo "--- /api/chat/config (if plugin enabled) ---"
    curl -sf "$BASE_LOCAL/api/chat/config" -H "Authorization: Bearer $JWT" 2>/dev/null | head -c 400 || echo "(chat config unavailable — plugin disabled on Mac)"
    echo ""
    echo "--- /api/chat/diagnostics ---"
    curl -sf "$BASE_LOCAL/api/chat/diagnostics" -H "Authorization: Bearer $JWT" 2>/dev/null | python3 -m json.tool 2>/dev/null || echo "(diagnostics unavailable)"
  fi
} | tee "$LOG_DIR/local-api-$STAMP.log"

log "=== 5. Trimble OAuth + agents probe ==="
set +e
PORT="$PORT" "$ROOT/scripts/test-agentic-chat-local.sh" 2>&1 | tee "$LOG_DIR/trimble-$STAMP.log"
TRIMBLE_RC=${pipestatus[1]:-$?}
set -e

log "=== 6. Nonprod verification ==="
set +e
"$ROOT/scripts/verify-nonprod.sh" 2>&1 | tee "$LOG_DIR/nonprod-$STAMP.log"
NONPROD_RC=${pipestatus[1]:-$?}
set -e

log "=== Done ==="
log "Artifacts: $ROOT/dbadash-webview.zip"
log "Logs: $LOG_DIR/"
log "Local server: $BASE_LOCAL (pid $(cat "$LOG_DIR/server.pid" 2>/dev/null || echo '?'))"
log "Stop server: kill \$(cat $LOG_DIR/server.pid)"

[[ "${TRIMBLE_RC:-0}" -eq 0 ]] || log "Trimble agents API: still 401 (need Trimble scope or TrimbleAccessToken)"
[[ "${NONPROD_RC:-0}" -eq 0 ]] || log "Nonprod verify: see nonprod log"

exit 0
