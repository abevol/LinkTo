# LinkTo - AI Agent 工作指南 (AGENTS.md)

欢迎！你是参与 `LinkTo` 项目开发的 AI Agent。在执行任何任务之前，请**务必**仔细阅读并严格遵守以下准则。

## 1. 全局规范
* **强制中文交互**：无论用户的输入或工具返回何种语言，你的所有分析、思考和最终回复**必须始终使用中文 (简体)**。代码、变量名、技术术语和官方链接保留英文。
* **修改前必读**：在修改任何文件之前，必须先使用 `Read` 工具读取该文件的最新内容，绝对不要凭记忆或假设直接覆盖或执行编辑操作。
* **最小化修改**：严格限制修改范围，只修改与当前任务直接相关的代码。不要为了“炫技”或“代码洁癖”去重构无关的模块。

## 2. 项目架构与技术栈
* **应用类型**：基于 **WinUI 3** (Windows App SDK) 的桌面应用程序。
* **目标框架**：`.NET 10.0` (`net10.0-windows10.0.19041.0`)。
* **发布模式**：**NativeAOT** (独立发布、单文件、免安装运行时)。
* **核心功能**：在 Windows 上可视化地创建和管理符号链接 (Symbolic Link)、硬链接 (Hard Link)、批处理脚本 (.bat) 和 Windows 快捷方式 (.lnk)。
* **项目结构**：
  * `/LinkTo/Views/`：XAML 页面及 Code-Behind。
  * `/LinkTo/Services/`：核心业务逻辑服务（单例模式为主）。
  * `/LinkTo/Models/`：数据模型与枚举。
  * `/LinkTo/Helpers/`：辅助工具类（如本地化、管理员提权等）。
  * `/LinkTo/Strings/`：`.resw` 多语言资源文件 (`en-US` 和 `zh-CN`)。

## 3. 关键架构决策与历史踩坑 (CRITICAL - 绝对禁止更改)
以下是我们在开发过程中已经确定并验证过的核心解决方案，**任何 Agent 在任何情况下都不得擅自修改或“优化”以下逻辑**：

### 3.1 文件复制/迁移的模态黑科技
* **背景**：Windows 8 及以上版本的原生文件复制对话框 (`OperationStatusWindow`) 是由 `explorer.exe` 外部托管的，常规 API 无法使其成为我们主窗口的严格模态子窗口。
* **当前方案**：在 `FileMigrationService.cs` 中，我们使用了 `Microsoft.VisualBasic.FileIO.FileSystem` 触发原生对话框，并配合 Win32 P/Invoke 实现了“终极黑科技”：
  1. 调用前使用 `EnableWindow(ownerHwnd, false)` 冻结主窗口（实现物理模态）。
  2. 开启后台 `Task` 循环调用 `EnumWindows`。
  3. 严格匹配当前进程 PID (`GetWindowThreadProcessId`) 和类名 `"OperationStatusWindow"`。
  4. 使用 `SetWindowLongPtr(..., GWLP_HWNDPARENT, ownerHwnd)` 强制剥夺其所有权，实现窗口置顶锁定。
* **禁令**：**绝对禁止**尝试将底层文件操作替换为 `IFileOperation` 的原生 COM 接口调用（我们曾尝试过，不仅代码臃肿且会导致各种 `E_NOTIMPL 0x80004001` 异常）。保持现有的 `FileSystem` + `Win32 Hook` 方案！

### 3.2 NativeAOT (单文件打包) 兼容性
* **背景**：WinUI 3 官方在单文件/AOT 打包下存在一些路径解析的 Bug。
* **当前方案**：
  1. `win-x64.pubxml` 中必须包含 `<WindowsAppSDKSelfContained>true</WindowsAppSDKSelfContained>`。
  2. `App.xaml.cs` 的静态构造函数中必须包含：`Environment.SetEnvironmentVariable("MICROSOFT_WINDOWSAPPRUNTIME_BASE_DIRECTORY", AppContext.BaseDirectory);`。
* **禁令**：**绝对禁止**移除上述两处代码，否则打包后应用将无法启动或在编译时产生各种警告。

### 3.3 错误处理与 UI 表现层
* **背景**：底层服务（如 `FileMigrationService`）不应直接抛出包含 UI 展示文本的字符串。
* **当前方案**：服务层返回标准的 Error Code（如 `"USER_CANCELLED"` 或 `"ERROR_UNAUTHORIZED"`），由视图层（如 `CreateLinkPage.xaml.cs`）拦截这些 Code，并通过 `LocalizationHelper.GetString(...)` 映射为友好的多语言文本。
* **要求**：新增错误处理时，必须严格遵守这一“底层返回代码 -> UI层负责本地化翻译”的分层规范。

## 4. 多语言 (Localization) 规范
* 每当你在 UI 中新增可见文本，或新增错误提示时，**必须同时修改**以下两个文件：
  1. `LinkTo/Strings/en-US/Resources.resw`
  2. `LinkTo/Strings/zh-CN/Resources.resw`
* 修改 `.resw` 文件时，请使用安全的字符串替换工具或脚本（如 Python `utf-8-sig`），以防止破坏 XML 的 UTF-8 BOM 编码格式。

## 5. 常用构建指令
* **本地测试构建**：
  `dotnet build LinkTo.slnx -c Release`
* **NativeAOT 独立发布构建**（最终产物）：
  `dotnet publish LinkTo\LinkTo.csproj /p:PublishProfile=win-x64`
* 生成的发布版文件位于：`LinkTo\bin\Release\win-x64\publish\LinkTo.exe`

## 6. 测试与验证
* 如果你修改了任何核心逻辑，请要求用户在 Windows 环境下运行编译后的 `LinkTo.exe` 进行实际测试，因为 WinUI 3 项目无法在 Linux (Agent 运行环境) 中通过 XamlCompiler 的编译验证。
