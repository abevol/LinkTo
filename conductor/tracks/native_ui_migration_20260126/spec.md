# 规格说明书 (spec.md) - 集成 Windows 原生文件操作 UI

## 1. 概览 (Overview)
当前 `FileMigrationService` 使用的是 .NET 原生的 `File.Move` 和 `Directory.Move` 方法，这些操作是静默的且在处理跨卷移动或大文件时缺乏进度反馈。本任务旨在将文件移动逻辑替换为调用 Windows 原生 Shell UI（通过 `Microsoft.VisualBasic.FileIO`），以便用户在迁移数据时能够看到标准的进度对话框并能处理潜在的冲突。

## 2. 功能需求 (Functional Requirements)
- **原生 UI 集成**：在执行文件或目录移动时，必须调起 Windows 标准的“正在移动...”进度窗口。
- **冲突处理支持**：利用 Windows 原生对话框处理文件名冲突、权限请求和重试逻辑。
- **同步状态返回**：`MoveAsync` 必须等待 Shell 操作完全结束。
    - 如果用户点击“取消”或操作因错误停止，方法应返回 `Success = false`。
    - 只有在操作成功完成后，才返回 `Success = true` 以触发后续的链接创建逻辑。
- **类型支持**：同时支持单个文件和整个目录的移动。

## 3. 非功能需求 (Non-Functional Requirements)
- **用户体验**：迁移大文件夹时，用户应能通过进度条获知剩余时间。
- **可靠性**：确保在用户取消移动操作时，程序不会错误地在原始路径创建指向不存在目标的链接。

## 4. 技术实现细节 (Technical Details)
- **核心组件**：使用 `Microsoft.VisualBasic.FileIO.FileSystem.MoveFile` 和 `MoveDirectory`。
- **参数配置**：
    - `UIOption` 设置为 `AllDialogs` 以显示进度和错误。
    - `RecycleOption` 设置为 `DoNotSendToRecycleBin`（因为是移动操作）。
    - `UICancelOption` 设置为 `ThrowException`，以便捕获用户取消操作的行为并返回正确的业务状态。

## 5. 验收标准 (Acceptance Criteria)
- [ ] 移动小文件时，功能正常且无异常。
- [ ] 移动大文件夹时，能看到 Windows 标准进度条。
- [ ] 如果移动过程中用户点击“取消”，系统应停止后续的链接创建步骤。
- [ ] 跨盘符移动时，原生 UI 应能正确处理进度显示。

## 6. 超出范围 (Out of Scope)
- 自定义进度条 UI（本任务仅使用 Windows 系统自带 UI）。
- 撤销（Undo）功能的实现（将在后续 Track 中考虑）。
