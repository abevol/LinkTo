# Technology Stack

## Core Technologies
- **Programming Language:** C# (Latest version compatible with .NET 10)
- **UI Framework:** WinUI 3 (Windows App SDK 1.8+)
- **Runtime:** .NET 10.0 (Targeting `net10.0-windows10.0.19041.0`)
- **Target OS:** Windows 10 version 1809 (Build 17763) and higher

## Development & Build Tools
- **Build System:** MSBuild / `dotnet build`
- **Project Format:** Single-project WinUI 3 with `LinkTo.csproj` and `LinkTo.slnx`
- **Package Manager:** NuGet
- **Primary Dependencies:**
  - `Microsoft.WindowsAppSDK`: Core UI and system integration.
  - `Microsoft.Windows.SDK.BuildTools`: Build support for Windows metadata.

## Architecture & Infrastructure
- **Deployment Model:** Unpackaged (Framework-dependent or AOT via `dotnet publish`)
- **Target Architectures:** x86, x64, ARM64
- **Manifest:** `app.manifest` for high DPI and administrative privilege management.
