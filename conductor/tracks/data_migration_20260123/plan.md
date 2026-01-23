# Implementation Plan - 数据迁移功能

## Phase 1: Core Logic & Services (Backend)
本阶段专注于核心业务逻辑的实现，不涉及 UI。我们将扩展 `LinkService` 或创建新的 `MigrationService` 来处理文件移动和原子性操作。

- [x] Task: Create `FileMigrationService` structure and interfaces 11063dc
    - [x] Create `IMigrationService` interface definition
    - [x] Create `FileMigrationService` class skeleton
- [ ] Task: Implement Atomic Move Logic (TDD)
    - [ ] Write failing tests for successful file/folder move
    - [ ] Implement move logic using `System.IO`
    - [ ] Write failing tests for rollback scenario (move success, callback failure)
    - [ ] Implement rollback mechanism
- [ ] Task: Implement Conflict Handling (TDD)
    - [ ] Write failing tests for destination collision
    - [ ] Implement detection logic (File.Exists / Directory.Exists)
    - [ ] Implement resolution strategy (throw specific exception to be caught by UI or callback)
- [ ] Task: Integrate with existing `LinkService`
    - [ ] Write tests ensuring `LinkService` can accept a "MigrateFirst" flag or strategy
    - [ ] Update `LinkService` to call `MigrationService` before link creation
- [ ] Task: Conductor - User Manual Verification 'Core Logic & Services (Backend)' (Protocol in workflow.md)

## Phase 2: User Interface Implementation
本阶段将“数据迁移”的控件添加到 MainWindow，并将其绑定到后端的逻辑。

- [ ] Task: Update MainWindow UI Layout
    - [ ] Add `GroupBox` (or similar container) below Link Type selection in `MainWindow.xaml`
    - [ ] Add Checkbox and Description Textblock inside the container
- [ ] Task: Bind UI State to ViewModel/Code-behind
    - [ ] Add `IsMigrationEnabled` property to `MainWindow` or its ViewModel
    - [ ] Ensure the Checkbox state reflects this property
- [ ] Task: Implement Progress Feedback UI
    - [ ] Add/Reuse ProgressRing or ProgressBar in MainWindow for migration status
    - [ ] Wire up progress events from `FileMigrationService` to UI updates
- [ ] Task: Handle Conflict Dialogs
    - [ ] Implement ContentDialog for "File Exists" scenarios (Overwrite/Cancel)
    - [ ] Connect `FileMigrationService` conflict exceptions to trigger this dialog
- [ ] Task: Conductor - User Manual Verification 'User Interface Implementation' (Protocol in workflow.md)

## Phase 3: Integration & Polish
本阶段进行端到端的集成测试，权限处理优化以及最终的验收。

- [ ] Task: Implement Permission Handling (Admin Access)
    - [ ] Test migration to protected folders (e.g., Program Files)
    - [ ] Integrate with existing `AdminHelper` to request elevation if `UnauthorizedAccessException` occurs during move
- [ ] Task: End-to-End Testing & Bug Fixes
    - [ ] Manual test: Move file -> Create Symlink
    - [ ] Manual test: Move folder -> Create Junction
    - [ ] Manual test: Rollback scenario (force link creation failure)
- [ ] Task: Update Documentation
    - [ ] Update README or user help text to explain Data Migration mode
- [ ] Task: Conductor - User Manual Verification 'Integration & Polish' (Protocol in workflow.md)
