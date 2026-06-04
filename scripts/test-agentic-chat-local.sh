#!/bin/zsh
# Local smoke test: Trimble OAuth + agents.stage (works on Mac without the .NET plugin).
set -e
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
PORT="${PORT:-5103}"
BASE="http://127.0.0.1:${PORT}"
CFG="${ROOT}/backend/appsettings.Development.json"
[[ -f "$CFG" ]] || CFG="${ROOT}/backend/appsettings.json"

echo "=== Trimble (from $(basename "$CFG")) ==="
python3 << PY
import json, subprocess, base64, urllib.parse, sys
cfg = json.load(open("$CFG"))["AgenticChat"]
cid, secret = cfg["TrimbleClientId"], cfg["TrimbleClientSecret"]
url = cfg["TrimbleTokenUrl"]
auth = base64.b64encode(f"{cid}:{secret}".encode()).decode()
agent_id = cfg["AgentId"]
candidates = cfg.get("TrimbleChatScopeCandidates") or []
for s in [cfg.get("TrimbleChatScope"), cfg.get("TrimbleScope"), "agentic", "DBA_Dash_Web_View"]:
    if s and s not in candidates:
        candidates.append(s)

def probe(scope, aud=None):
    params = {"grant_type":"client_credentials","scope":scope}
    if aud:
        params["audience"] = aud
    data = urllib.parse.urlencode(params)
    r = subprocess.run(["curl","-s","-X","POST",url,"-H",f"Authorization: Basic {auth}","-H","Content-Type: application/x-www-form-urlencoded","-d",data],capture_output=True,text=True)
    try:
        tok = json.loads(r.stdout)["access_token"]
    except Exception:
        print(f"  {scope!r}: no token")
        return False
    api = f"https://agents.stage.trimble-ai.com/v2/agents/{agent_id}"
    r2 = subprocess.run(["curl","-s","-w","%{http_code}","-o","/tmp/agprobe.txt",api,"-H",f"Authorization: Bearer {tok}"],capture_output=True,text=True)
    code = int(r2.stdout)
    body = open("/tmp/agprobe.txt").read()[:120]
    if code == 200:
        print(f"PASS scope={scope!r} -> agents.stage HTTP 200")
        return True
    print(f"FAIL scope={scope!r} -> agents.stage HTTP {code}: {body}")
    return False

aud = cfg.get("TrimbleChatAudience") or None
if aud == "":
    aud = None
ok = any(probe(s, aud) for s in candidates) or any(probe(s, None) for s in candidates)
sys.exit(0 if ok else 1)
PY
TRIMBLE_OK=$?

echo ""
echo "=== DBA Dash API at $BASE (optional; needs Windows or working plugin on Mac) ==="
if curl -sf "$BASE/api/health" >/dev/null 2>&1; then
  echo "GET /api/health: $(curl -sf "$BASE/api/health")"
  echo "GET /api/chat/health: $(curl -sf "$BASE/api/chat/health")"
  JWT=$(curl -sf -X POST "$BASE/api/auth/login" -H 'Content-Type: application/json' \
    -d '{"username":"admin","password":"admin"}' | python3 -c "import sys,json; print(json.load(sys.stdin).get('token',''))" 2>/dev/null || true)
  if [[ -n "$JWT" ]]; then
    DIAG=$(curl -sf "$BASE/api/chat/diagnostics" -H "Authorization: Bearer $JWT" 2>/dev/null || echo "(not available)")
    echo "GET /api/chat/diagnostics: ${DIAG:0:300}"
  fi
else
  echo "API not running on $BASE"
fi

[[ "$TRIMBLE_OK" -eq 0 ]] || exit 1
echo ""
echo "Trimble agents API OK — deploy to Windows IIS and run full chat test there."
