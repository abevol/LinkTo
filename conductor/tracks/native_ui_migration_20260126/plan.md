# 实施计划 (plan.md) - 集成 Windows 原生文件操作 UI

## 阶段 1：环境准备与依赖集成 [checkpoint: b6d9ad4]
- [x] **任务：确认 Microsoft.VisualBasic 引用** <!-- 00be1b5 -->
- [x] **任务：Conductor - 用户手册验证 '阶段 1' (Protocol in workflow.md)**

## 阶段 2：单元测试定义 (TDD Red Phase)
- [~] **任务：更新 FileMigrationServiceTests**
    - [ ] 编写测试用例验证 `MoveAsync` 在模拟用户取消时的返回值（期望 `Success = false`）。
    - [ ] 编写测试用例验证基本移动功能的接口契约是否保持一致。
- [ ] **任务：Conductor - 用户手册验证 '阶段 2' (Protocol in workflow.md)**

## 阶段 3：核心逻辑实现 (Green Phase)
- [ ] **任务：重构 FileMigrationService.cs**
    - [ ] 引入 `Microsoft.VisualBasic.FileIO` 命名空间。
    - [ ] 修改 `MoveAsync` 内部实现，移除现有的 `Directory.Move`/`File.Move` 及手动递归复制逻辑。
    - [ ] 调用 `FileSystem.MoveFile` 和 `FileSystem.MoveDirectory` 并配置 `UIOption.AllDialogs`。
    - [ ] 封装 `OperationCanceledException` 异常处理，确保用户取消时返回 `(false, "USER_CANCELLED")`。
- [ ] **任务：Conductor - 用户手册验证 '阶段 3' (Protocol in workflow.md)**

## 阶段 4：验证与质量保证
- [ ] **任务：运行所有测试并验证覆盖率**
- [ ] **任务：手动集成测试**
    - [ ] 验证移动大文件时的进度条显示。
    - [ ] 验证冲突对话框（如目标已存在文件）的表现。
    - [ ] 验证点击取消后，LinkTo 是否正确停止了链接创建逻辑。
- [ ] **任务：Conductor - 用户手册验证 '阶段 4' (Protocol in workflow.md)**
