# LinkTo

A Windows application for creating and managing symbolic links and hard links.

![WinUI 3](https://img.shields.io/badge/WinUI-3-blue)
![.NET 10](https://img.shields.io/badge/.NET-10.0-purple)
![Platform](https://img.shields.io/badge/platform-Windows-lightgrey)

## Features

- **Create Symbolic Links** - Create symbolic links for files and directories
- **Create Hard Links** - Create hard links for files (same volume only)
- **Data Migration** - Move source files to target location while keeping a link at the original path
- **Shell Integration** - Right-click context menu integration in Windows Explorer
- **Link History** - Track and manage created links
- **Multi-language** - English and Chinese interface
- **Modern UI** - Beautiful WinUI 3 interface with Mica backdrop

## Screenshots

![MainWindow](/LinkTo/Assets/MainWindow.png)

## Requirements

- Windows 10 version 1809 (build 17763) or later
- [.NET 10.0 Runtime](https://dotnet.microsoft.com/download/dotnet/10.0)

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
2. Select a source file or directory (or drag & drop)
3. Choose target directory
4. Optionally modify link name
5. Select link type (Symbolic or Hard)
6. Click "Create Link"

### Creating Links via Context Menu

1. Enable shell integration in Settings
2. Right-click any file or folder in Windows Explorer
3. Select "Link to..."
4. Choose target directory and create link

## Link Types

| Feature                | Symbolic Link  | Hard Link      |
| ---------------------- | -------------- | -------------- |
| Files                  | ✅              | ✅              |
| Directories            | ✅              | ❌              |
| Cross-volume           | ✅              | ❌              |
| Network paths          | ✅              | ❌              |
| Process Image Path [1] | Original File  | Link File      |
| Working Directory [2]  | Link Directory | Link Directory |
| Original deleted       | Broken         | Still works    |
| Admin required         | Yes [3]        | No             |

1. Programs that depend on the original file path may malfunction when using Hard Links.
2. When launched from Windows Explorer, the working directory is set to the link's location.
3. Not required if Developer Mode is enabled in Windows Settings.

## Permissions

- **Creating Symbolic Links**: Requires Administrator privileges OR Developer Mode enabled
- **Shell Integration**: Requires Administrator privileges
- **Creating Hard Links**: No special privileges required

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
