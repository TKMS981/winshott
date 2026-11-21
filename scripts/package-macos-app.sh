#!/bin/sh
set -eu

ROOT_DIR="$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)"
"$ROOT_DIR/scripts/dotnet-local.sh" restore "$ROOT_DIR/src/Winshott/Winshott.csproj" -r osx-arm64
"$ROOT_DIR/scripts/dotnet-local.sh" publish "$ROOT_DIR/src/Winshott/Winshott.csproj" -c Release -r osx-arm64 --self-contained true

PUBLISH_DIR="$ROOT_DIR/src/Winshott/bin/Release/net10.0/osx-arm64/publish"
APP_DIR="$ROOT_DIR/artifacts/Winshott.app"
rm -rf "$APP_DIR"
mkdir -p "$APP_DIR/Contents/MacOS"
cp -R "$PUBLISH_DIR"/. "$APP_DIR/Contents/MacOS/"
cp "$ROOT_DIR/packaging/macos/Info.plist" "$APP_DIR/Contents/Info.plist"
echo "$APP_DIR"
