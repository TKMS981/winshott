#!/bin/sh
set -eu

ROOT_DIR="$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)"
export DOTNET_ROOT="$ROOT_DIR/.dotnet"
export DOTNET_CLI_HOME="$ROOT_DIR/.tools/dotnet-home"
export NUGET_PACKAGES="$ROOT_DIR/.tools/nuget"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1
export PATH="$DOTNET_ROOT:$PATH"

exec "$DOTNET_ROOT/dotnet" "$@"
