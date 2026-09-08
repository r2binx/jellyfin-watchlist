#!/usr/bin/env bash
# Build the plugin inside the official .NET 10 SDK container, running as the host user on both Docker and rootless Podman.
set -euo pipefail
command -v docker >/dev/null || { echo "docker (or podman with docker emulation) is required" >&2; exit 1; }

cd "$(dirname "$0")/.."
mkdir -p .nuget-cache

# Detect runtime and choose appropriate user mapping.
if docker --version 2>/dev/null | grep -qi podman; then
  user_args=(--userns=keep-id)
else
  user_args=(--user "$(id -u):$(id -g)")
fi

exec docker run --rm \
  "${user_args[@]}" \
  -e HOME=/tmp -e DOTNET_CLI_HOME=/tmp -e DOTNET_CLI_TELEMETRY_OPTOUT=1 -e NUGET_PACKAGES=/nuget \
  -v "$PWD/.nuget-cache":/nuget \
  -v "$PWD":/src -w /src \
  mcr.microsoft.com/dotnet/sdk:10.0 \
  dotnet build Jellyfin.Plugin.Watchlist/Jellyfin.Plugin.Watchlist.csproj -c Release "$@"
