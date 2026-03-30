[English](README.md) | [简体中文](README-zh-CN.md)

# LinkTo

A WinUI 3-based application for creating and managing symbolic links, hard links, batch files, and Windows shortcuts on Windows.

![WinUI 3](https://img.shields.io/badge/WinUI-3-blue)
![.NET 10](https://img.shields.io/badge/.NET-10.0-purple)
![Platform](https://img.shields.io/badge/platform-Windows-lightgrey)

## Features

- **Create Symbolic Links** - Create symbolic links for files and folders
- **Create Hard Links** - Create hard links for files (same volume only)
- **Batch & Shortcuts** - Generate `.bat` scripts and `.lnk` files with customizable working directories to solve strict path dependency issues
- **Data Migration** - Safely move source files to target locations using the native Windows file operation dialog (fully modal), leaving a link at the original path
- **Shell Integration** - Right-click context menu integration in Windows Explorer
- **Link History** - Track and manage created links
- **Multi-language** - English and Chinese interfaces
- **NativeAOT** - Compiled with NativeAOT for minimal footprint and blazing fast startup
- **Modern UI** - Beautiful WinUI 3 interface with Mica backdrop

## Screenshots

![MainWindow](/LinkTo/Assets/MainWindow.png)

## Requirements

- Windows 10 version 1809 (build 17763) or later
- [.NET 10.0 Runtime](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Windows App SDK Runtime](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/downloads)

## Installation

### From Release

1. Download the latest release from the Releases page
2. Extract the archive
3. Run `LinkTo.exe`

### Build from Source

```powershell
# Clone the repository
git clone https://github.com/abevol/LinkTo.git
cd LinkTo

# Build
dotnet build LinkTo.slnx -c Release

# Run
.\LinkTo\bin\x64\Release\net10.0-windows10.0.19041.0\LinkTo.exe

# Publish (AOT)
dotnet publish LinkTo\LinkTo.csproj /p:PublishProfile=win-x64

# Run published app
.\LinkTo\bin\Release\win-x64\publish\LinkTo.exe
```

## Usage

### Creating Links via UI

1. Launch LinkTo
2. Select a source file or folder (or drag & drop)
3. Choose target location
4. Optionally modify link name
5. Select link type (Symbolic, Hard, Batch, or Shortcut)
6. Click "Create Link"

### Creating Links via Context Menu

1. Enable shell integration in Settings
2. Right-click any file or folder in Windows Explorer
3. Select "Link to..."
4. Choose target location and create link

## Link Types

| Feature | Symbolic Link | Hard Link | Batch File (.bat) | Windows Shortcut (.lnk) |
| --- | :---: | :---: | :---: | :---: |
| **Supports Files** | ✅ | ✅ | ✅ | ✅ |
| **Supports Folders** | ✅ | ❌ | ✅ | ✅ |
| **Cross-volume** | ✅ | ❌ | ✅ | ✅ |
| **Network paths** | ✅ | ❌ | ✅ | ✅ |
| **Custom Working Dir** | ❌ | ❌ | ✅ | ✅ |
| **Process Image Path [1]** | Original File | Link File | Original File | Original File |
| **Working Directory [2]** | Link Directory | Link Directory | Custom / Original | Custom / Original |
| **Original deleted** | Broken | Still works | Broken | Broken |
| **Admin required** | Yes [3] | No | No | No |

1. Programs that depend on the original file path may malfunction when using Hard Links.
2. When launched from Windows Explorer, the working directory is set to the link's location by default for Symbolic and Hard links. Batch and Shortcut links allow you to explicitly define the working directory.
3. Not required if Developer Mode is enabled in Windows Settings.

## Permissions

- **Creating Symbolic Links**: Requires Administrator privileges OR Developer Mode enabled
- **Shell Integration**: Requires Administrator privileges
- **Creating Hard Links / Batches / Shortcuts**: No special privileges required

## Configuration

Configuration is stored at:
```
%APPDATA%\LinkTo\Config.json
```

Logs are stored at:
```
%APPDATA%\LinkTo\Logs\
```

## License

MIT License

## Author

Abevol

## Contributing

Contributions are welcome! Please feel free to submit issues and pull requests.
