#!/bin/zsh
# VPN connected: verify SQL (via MCP separately) + nonprod Windows IIS + Trimble agents.
set -e
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
BASE="${BASE_URL:-https://dbadash-nonprod.e-builder.net}"

echo "=== SQL (DBADashDB via MCP) ==="
echo "Run dbadash_list_instances in Cursor MCP — expect instance rows."

echo ""
echo "=== Nonprod Windows IIS ($BASE) ==="
"$ROOT/scripts/verify-nonprod.sh"

echo ""
echo "=== Trimble agents.stage (client credentials) ==="
"$ROOT/scripts/test-agentic-chat-local.sh" || true

echo ""
echo "=== Summary ==="
echo "Nonprod is your Windows test environment (IIS behind $BASE)."
echo "Deploy latest: copy dbadash-webview.zip → Windows → .\\scripts\\deploy-iis.ps1"
echo "Full Windows CI: GitHub Actions workflow windows-agentic-chat-test.yml"
