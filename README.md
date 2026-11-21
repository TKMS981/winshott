# Winshott

Winshott is a cross-platform screenshot application foundation built with C#, .NET 10, Avalonia, and MVVM. macOS is the first development target; Windows 11 support is planned. Phase 1 provides the desktop shell, settings, logging, service boundaries, and tests. Capture, OCR, QR reading, editing, and history storage are not implemented yet and are labeled as such in the UI.

## Requirements

- .NET 10 SDK (10.0.404 used for the current macOS build)
- macOS 14+ for development (Avalonia compatibility on macOS 27.2.1 is being validated in this phase)
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

To create a launchable `.app` bundle, run `./scripts/package-macos-app.sh`; the result is `artifacts/Winshott.app`. Open that bundle from Finder. Running the raw Avalonia executable from a non-interactive shell can fail to create macOS's display render timer even when the Mac desktop is available.

Cross-publish the common UI foundation for Windows x64 from macOS with `./scripts/dotnet-local.sh restore src/Winshott/Winshott.csproj -r win-x64` followed by `./scripts/dotnet-local.sh publish src/Winshott/Winshott.csproj -c Release -r win-x64 --self-contained true`. This verifies packaging compatibility only; Windows execution and capture APIs still require Windows testing.

## Tests

The xUnit suite covers initial settings creation, save/load, malformed JSON recovery, main view model initialization, platform service registration, and explicit unavailable behavior for unimplemented platform services.

```sh
./scripts/dotnet-local.sh test Winshott.slnx
```

## Settings and logs

Settings are JSON in the OS application support directory (`~/Library/Application Support/Winshott/settings.json` on macOS and `%AppData%/Winshott/settings.json` on Windows). Logs are written locally to the matching application data directory under `logs/winshott.log`, and to the console when available. Images, OCR text, and clipboard contents are not logged.

## Platform boundaries

`IScreenCaptureService`, `IClipboardService`, `IOcrService`, `IQrCodeService`, `IImageProcessingService`, `IFileService`, and `IHotkeyService` are registered through DI. Their current provider throws `PlatformNotSupportedException`; it does not return placeholder successes. OS-specific implementations will be added in later phases.

## Documentation

- [Architecture](docs/ARCHITECTURE.md)
- [Roadmap](docs/ROADMAP.md)
- [Feature status](docs/FEATURES.md)
