[English](README.md) | [简体中文](README-zh-CN.md)

# LinkTo

一个基于 WinUI 3 的交互式工具，用于在 Windows 上创建和管理符号链接、硬链接、批处理文件以及快捷方式。

![WinUI 3](https://img.shields.io/badge/WinUI-3-blue)
![.NET 10](https://img.shields.io/badge/.NET-10.0-purple)
![Platform](https://img.shields.io/badge/platform-Windows-lightgrey)

## 主要特性

- **创建符号链接** - 为文件和文件夹创建符号链接
- **创建硬链接** - 为文件创建硬链接（仅限同驱动器/卷）
- **批处理与快捷方式** - 生成带有自定义工作目录的 `.bat` 脚本和 `.lnk` 快捷方式，解决严格路径依赖的程序问题
- **数据迁移** - 使用原生的 Windows 文件复制对话框（支持真正的模态锁定）将源文件安全移动到目标位置，并在原位置创建链接
- **外壳集成** - 一键集成到 Windows 资源管理器的右键上下文菜单
- **链接历史** - 跟踪和管理历史创建的链接
- **多语言支持** - 原生支持简体中文与英文界面
- **NativeAOT** - 使用 NativeAOT 编译，体积小且启动极快
- **现代 UI** - 基于 WinUI 3 打造，支持精美的云母 (Mica) 材质背景

## 界面截图

![MainWindow](/LinkTo/Assets/MainWindow-zh-CN.png)

## 运行环境

- Windows 10 版本 1809 (内部版本 17763) 或更高版本
- [.NET 10.0 运行时](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Windows App SDK 运行时](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/downloads)

## 安装指南

### 从发布页面获取

1. 在 GitHub Releases 页面下载最新版本的压缩包
2. 解压文件
3. 直接运行 `LinkTo.exe`

### 从源码构建

```powershell
# 克隆仓库
git clone https://github.com/abevol/LinkTo.git
cd LinkTo

# 构建测试版本
dotnet build LinkTo.slnx -c Release

# 运行测试版本
.\LinkTo\bin\x64\Release\net10.0-windows10.0.19041.0\LinkTo.exe

# 独立发布 (Native AOT)
dotnet publish LinkTo\LinkTo.csproj /p:PublishProfile=win-x64

# 运行发布的独立应用
.\LinkTo\bin\Release\win-x64\publish\LinkTo.exe
```

## 使用方法

### 通过应用界面创建

1. 启动 LinkTo
2. 选择源文件或文件夹（支持直接拖放）
3. 选择放置位置
4. [可选] 修改链接名称
5. 选择链接类型（符号链接、硬链接、批处理、快捷方式）
6. 点击“创建链接”

### 通过右键菜单创建

1. 在应用的“设置”中启用“右键菜单外壳集成”
2. 在 Windows 资源管理器中右键点击任意文件或文件夹
3. 选择 `Link to...`（或你设置的本地化菜单项）
4. 在弹出的窗口中选择放置位置并创建链接

## 链接类型对比

| 特性 | 符号链接 | 硬链接 | 批处理文件 (.bat) | Windows 快捷方式 (.lnk) |
| --- | :---: | :---: | :---: | :---: |
| **支持文件** | ✅ | ✅ | ✅ | ✅ |
| **支持文件夹** | ✅ | ❌ | ✅ | ✅ |
| **跨盘符 / 卷** | ✅ | ❌ | ✅ | ✅ |
| **支持网络路径** | ✅ | ❌ | ✅ | ✅ |
| **自定义工作目录** | ❌ | ❌ | ✅ | ✅ |
| **进程映像路径 [1]** | 原始文件 | 链接文件 | 原始文件 | 原始文件 |
| **默认工作目录 [2]** | 链接所在目录 | 链接所在目录 | 自定义 / 原始目录 | 自定义 / 原始目录 |
| **原始文件删除后** | 失效 | 依然有效 | 失效 | 失效 |
| **需要管理员权限** | 是 [3] | 否 | 否 | 否 |

1. 依赖原始文件绝对路径或进程映像名称的程序在使用硬链接时可能会发生故障。
2. 从 Windows 资源管理器启动链接时，符号链接和硬链接的工作目录默认就是链接自身所在的目录。而批处理和快捷方式允许你显式设定程序运行时的实际工作目录。
3. 如果你在 Windows 系统设置中开启了“开发者模式”，则创建符号链接不再需要强制管理员权限。

## 权限说明

- **创建符号链接**：需要“管理员权限”或开启“开发者模式”
- **开启右键菜单外壳集成**：需要“管理员权限”（写入注册表）
- **创建硬链接 / 批处理 / 快捷方式**：普通权限即可，无需提权

## 配置文件与日志

配置文件保存在：
```
%APPDATA%\LinkTo\Config.json
```

日志文件保存在：
```
%APPDATA%\LinkTo\Logs\
```

## 开源协议

MIT License

## 作者

Abevol

## 参与贡献

欢迎提交 Issue 和 Pull Request，期待你的贡献！
