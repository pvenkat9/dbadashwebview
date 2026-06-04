#!/bin/zsh
# One-time: add Trimble Artifactory credentials for NuGet restore.
# Get API key from Trimble Cloud / your team's Artifactory admin.

set -e
echo "You need a Trimble Artifactory API key (not the OAuth client secret)."
echo "Often: Artifactory profile -> Generate API Key"
echo ""
read "? Artifactory username (often your email): " TRIMBLE_USER
read -s "? Artifactory API key/password: " TRIMBLE_PASS
echo ""

dotnet nuget remove source trimble-agentic-external-nuget-local 2>/dev/null || true

dotnet nuget add source \
  "https://artifactory.trimble.tools/artifactory/api/nuget/v3/trimble-agentic-external-nuget-local" \
  --name trimble-agentic-external-nuget-local \
  --username "$TRIMBLE_USER" \
  --password "$TRIMBLE_PASS" \
  --store-password-in-clear-text

echo ""
echo "Testing restore..."
cd "$(dirname "$0")/../backend"
dotnet restore

echo "Restore succeeded."
