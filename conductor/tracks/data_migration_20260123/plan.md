# Implementation Plan - 数据迁移功能

## Phase 1: Core Logic & Services (Backend) [checkpoint: 2850677]
本阶段专注于核心业务逻辑的实现，不涉及 UI。我们将扩展 `LinkService` 或创建新的 `MigrationService` 来处理文件移动和原子性操作。

- [x] Task: Create `FileMigrationService` structure and interfaces 11063dc
    - [x] Create `IMigrationService` interface definition
    - [x] Create `FileMigrationService` class skeleton
- [x] Task: Implement Atomic Move Logic (TDD) 12996ed
    - [x] Write failing tests for successful file/folder move
    - [x] Implement move logic using `System.IO`
    - [x] Write failing tests for rollback scenario (move success, callback failure)
    - [x] Implement rollback mechanism
- [x] Task: Implement Conflict Handling (TDD) 395ba8c
    - [x] Write failing tests for destination collision
    - [x] Implement detection logic (File.Exists / Directory.Exists)
    - [x] Implement resolution strategy (throw specific exception to be caught by UI or callback)
- [x] Task: Integrate with existing `LinkService` 90ca001
    - [x] Write tests ensuring `LinkService` can accept a "MigrateFirst" flag or strategy
    - [x] Update `LinkService` to call `MigrationService` before link creation
- [x] Task: Conductor - User Manual Verification 'Core Logic & Services (Backend)' (Protocol in workflow.md) 2850677

## Phase 2: User Interface Implementation
本阶段将“数据迁移”的控件添加到 MainWindow，并将其绑定到后端的逻辑。

- [x] Task: Update MainWindow UI Layout df0f030
    - [x] Add `GroupBox` (or similar container) below Link Type selection in `MainWindow.xaml`
    - [x] Add Checkbox and Description Textblock inside the container
- [x] Task: Bind UI State to ViewModel/Code-behind df0f030
    - [x] Add `IsMigrationEnabled` property to `MainWindow` or its ViewModel
    - [x] Ensure the Checkbox state reflects this property
- [x] Task: Implement Progress Feedback UI 0d07a2c
    - [x] Add/Reuse ProgressRing or ProgressBar in MainWindow for migration status
    - [x] Wire up progress events from `FileMigrationService` to UI updates
- [x] Task: Handle Conflict Dialogs 0d07a2c
    - [x] Implement ContentDialog for "File Exists" scenarios (Overwrite/Cancel)
    - [x] Connect `FileMigrationService` conflict exceptions to trigger this dialog
- [ ] Task: Conductor - User Manual Verification 'User Interface Implementation' (Protocol in workflow.md)

## Phase 2.5: Refinements & Enhancements (User Requested) [checkpoint: 6d5a31e]
- [x] Task: Refactor UI Layout 6d5a31e
    - [x] Rename "Data Migration" group to "Extended Options"
    - [x] Move description text to be inline with the CheckBox
- [x] Task: Complete Internationalization 6d5a31e
    - [x] Identify missing strings (Group Header, CheckBox content, etc.)
    - [x] Add translations for zh-CN and en-US
- [x] Task: Implement Cross-Volume Move Support 6d5a31e
    - [x] Update `FileMigrationService` to detect cross-volume move
    - [x] Implement Copy+Delete fallback logic for directories
    - [x] Add tests for cross-volume scenario (simulation)
- [x] Task: Conductor - User Manual Verification 'Refinements & Enhancements' (Protocol in workflow.md) 6d5a31e

## Phase 3: Integration & Polish [checkpoint: f342014]
本阶段进行端到端的集成测试，权限处理优化以及最终的验收。

- [x] Task: Implement Permission Handling (Admin Access) f342014
    - [x] Test migration to protected folders (e.g., Program Files)
    - [x] Integrate with existing `AdminHelper` to request elevation if `UnauthorizedAccessException` occurs during move
- [x] Task: End-to-End Testing & Bug Fixes f342014
    - [x] Manual test: Move file -> Create Symlink
    - [x] Manual test: Move folder -> Create Junction
    - [x] Manual test: Rollback scenario (force link creation failure)
- [x] Task: Update Documentation f342014
    - [x] Update README or user help text to explain Data Migration mode
- [x] Task: Conductor - User Manual Verification 'Integration & Polish' (Protocol in workflow.md) f342014
