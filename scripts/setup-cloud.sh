#!/usr/bin/env bash
set -euo pipefail
export DOTNET_CLI_HOME=/workspace/.dotnet
export NUGET_PACKAGES=/workspace/.nuget
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export PATH="/workspace/tools/dotnet:$PATH"
if ! command -v dotnet >/dev/null; then
  curl -fLsS https://dot.net/v1/dotnet-install.sh -o /tmp/kopi-dotnet-install.sh
  bash /tmp/kopi-dotnet-install.sh --version 10.0.100 --install-dir /workspace/tools/dotnet
fi
cd /workspace/Kopi
dotnet restore Kopi.sln
dotnet build Kopi.sln
# WPF cannot run on Linux. Browser tests may use an installed Chromium:
# KOPI_CHROMIUM=/usr/bin/chromium dotnet test tests/Kopi.Tests --filter 'FullyQualifiedName!~BridgeTests&FullyQualifiedName!~RealModelTests'
# This cloud's managed Chromium forbids unpacked extensions; Windows CI runs BridgeTests.
