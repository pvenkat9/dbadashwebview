#!/bin/zsh
# Post-deploy verification against dbadash-nonprod (or BASE_URL).
set -e
BASE="${BASE_URL:-https://dbadash-nonprod.e-builder.net}"
USER="${DBA_USER:-admin}"
PASS="${DBA_PASS:-admin}"

fail() { echo "FAIL: $1"; exit 1 }

echo "=== $BASE ==="
curl -sf "$BASE/api/health" | grep -q healthy || fail "/api/health"
echo "OK /api/health"

CHAT=$(curl -sf "$BASE/api/chat/health")
echo "$CHAT" | grep -q '"enabled":true' || fail "/api/chat/health — plugin disabled: $CHAT"
echo "OK /api/chat/health (plugin enabled)"

curl -sf -o /dev/null "$BASE/agentic-chat-shell.html" || fail "agentic-chat-shell.html"
echo "OK agentic-chat-shell.html"

JWT=$(curl -sf -X POST "$BASE/api/auth/login" -H 'Content-Type: application/json' \
  -d "{\"username\":\"$USER\",\"password\":\"$PASS\"}" | python3 -c "import sys,json; print(json.load(sys.stdin).get('token',''))")
[[ -n "$JWT" ]] || fail "login"
echo "OK login"

CFG=$(curl -sf "$BASE/api/chat/config" -H "Authorization: Bearer $JWT")
echo "$CFG" | grep -q agentId || fail "/api/chat/config not JSON"
echo "OK /api/chat/config"

DIAG=$(curl -sf "$BASE/api/chat/diagnostics" -H "Authorization: Bearer $JWT")
echo "$DIAG" | python3 -m json.tool

python3 << PY
import json, sys
d = json.loads('''$DIAG''')
issues = d.get("issues") or []
if d.get("agentsApiOk") is False:
    print("FAIL: agents.stage not authorized — set AgenticChat:TrimbleAccessToken or get Agents scope from Trimble")
    sys.exit(1)
if d.get("agentsApiOk") is True:
    print("PASS: agents API OK — embedded chat should work")
    sys.exit(0)
# Older deploy without agentsApiOk field
if "agentsApiOk" not in d:
    print("WARN: server build predates agents probe — redeploy latest dbadash-webview.zip")
    if issues:
        print("FAIL: diagnostics issues:", issues)
        sys.exit(1)
    print("WARN: cannot confirm agents API — test chat in browser")
    sys.exit(0)
if issues:
    print("FAIL:", issues)
    sys.exit(1)
print("OK diagnostics")
PY
