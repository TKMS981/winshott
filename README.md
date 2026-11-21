# Winshott

Winshott is a cross-platform screenshot application built with C#, .NET 10, Avalonia, and MVVM. macOS is the first development target; Windows 11 support is planned. Phase 2 adds macOS display enumeration, full-display and selected-region capture, image preview/copy, PNG/JPEG save, and shared image crop/file services. Window/scroll capture, OCR, QR reading, editing, and history storage remain unimplemented and are labeled as such.

## Requirements

- .NET 10 SDK (10.0.404 used for the current macOS build)
- macOS 14+ for development (the Swift ScreenCaptureKit bridge and Avalonia app were built on macOS 27.2.1 arm64)
- Xcode Command Line Tools for macOS development

The SDK used for this workspace is installed under `.dotnet/`, which is ignored by Git. The helper script keeps CLI state and NuGet packages under ignored `.tools/` as well, without changing shell profiles or system PATH.

## Build and run

On this workspace, use the local SDK helper:

```sh
./scripts/dotnet-local.sh --info
./scripts/dotnet-local.sh restore Winshott.slnx
./scripts/dotnet-local.sh build Winshott.slnx
./scripts/dotnet-local.sh run --project src/Winshott/Winshott.csproj
```

On another development machine with .NET 10 installed, use `dotnet restore`, `dotnet build`, `dotnet test`, and `dotnet run --project src/Winshott/Winshott.csproj`.

To publish a self-contained Apple Silicon build:

```sh
./scripts/dotnet-local.sh publish src/Winshott/Winshott.csproj -c Release -r osx-arm64 --self-contained true
```

The macOS build compiles `src/Winshott/Platform/macOS/WinshottCapture.swift` into a small C ABI library using Xcode Command Line Tools. To create a Finder `.app` bundle, run `./scripts/package-macos-app.sh`; the result is `artifacts/Winshott.app`. Screen Recording permission is requested only after the user presses **Request permission**. Grant it under System Settings → Privacy & Security → Screen Recording, then restart Winshott if macOS has not refreshed the permission state. The app does not capture until permission is granted. A stable signed distribution build is needed for persistent TCC identity across app replacements.

Cross-publish the common UI foundation for Windows x64 from macOS with `./scripts/dotnet-local.sh restore src/Winshott/Winshott.csproj -r win-x64` followed by `./scripts/dotnet-local.sh publish src/Winshott/Winshott.csproj -c Release -r win-x64 --self-contained true`. This verifies packaging compatibility only; Windows execution and capture APIs still require Windows testing.

## Tests

The xUnit suite covers initial settings creation, save/load, malformed JSON recovery, view-model initialization, platform service registration, logical-to-pixel conversion, edge clamping, PNG/JPEG crop/save/load, and safe timestamped names.

```sh
./scripts/dotnet-local.sh test Winshott.slnx
```

Some restricted runners prevent the .NET 10 `dotnet test` IPC named pipe from binding. In that case, build the solution and invoke the xUnit v3 test app directly: `./scripts/dotnet-local.sh build Winshott.slnx` followed by `./scripts/dotnet-local.sh tests/Winshott.Tests/bin/Debug/net10.0/Winshott.Tests.dll`.

## Settings and logs

Settings are JSON in the OS application support directory (`~/Library/Application Support/Winshott/settings.json` on macOS and `%AppData%/Winshott/settings.json` on Windows). Logs are written locally to the matching application data directory under `logs/winshott.log`, and to the console when available. Images, OCR text, and clipboard contents are not logged.

## Platform boundaries

`IScreenCaptureService`, `IScreenCapturePermissionService`, `IClipboardService`, `IOcrService`, `IQrCodeService`, `IImageProcessingService`, `IFileService`, and `IHotkeyService` are registered through DI. On macOS, ScreenCaptureKit enumerates displays and captures a display asynchronously; NSPasteboard writes PNG images. SkiaSharp crops and encodes PNG/JPEG, and Avalonia's storage picker chooses the local save path. Window/scroll capture, clipboard reads/text, OCR, QR, global hotkeys, and Windows-specific capture are still explicit unsupported operations; they never report placeholder success.

## Documentation

- [Architecture](docs/ARCHITECTURE.md)
- [Roadmap](docs/ROADMAP.md)
- [Feature status](docs/FEATURES.md)
