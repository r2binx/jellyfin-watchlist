#!/usr/bin/env bash
# Build the plugin inside the official .NET 10 SDK container (no local SDK needed).
set -euo pipefail
cd "$(dirname "$0")/.."
mkdir -p "$HOME/.nuget/packages"
exec docker run --rm \
  --userns=host \
  -e HOME=/tmp -e DOTNET_CLI_HOME=/tmp -e DOTNET_CLI_TELEMETRY_OPTOUT=1 -e NUGET_PACKAGES=/nuget \
  -v "$HOME/.nuget/packages":/nuget \
  -v "$PWD":/src -w /src \
  mcr.microsoft.com/dotnet/sdk:10.0 \
  dotnet build Jellyfin.Plugin.Watchlist/Jellyfin.Plugin.Watchlist.csproj -c Release "$@"
