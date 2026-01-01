# Plan: New Link Types (Batch & Shortcut)

## Phase 1: Foundation & UI Updates [checkpoint: 2d32bf6]
- [x] Task: Create `LinkType` Enum extension or update existing logic to support `Batch` and `Shortcut`. ccb56d3
- [x] Task: Update `CreateLinkPage.xaml` to include the "Working Directory" GroupBox (TextBox + Button). 1799a53
- [x] Task: Reorder UI: Move "Working Directory" GroupBox above "Link Type" GroupBox in `CreateLinkPage.xaml`. 9f9ac5a
- [x] Task: Localization: Add resource strings for "Batch File", "Shortcut", and "Working Directory" in `Resources.resw` (en-US and zh-CN). eb13533
- [x] Task: Localization: Update `CreateLinkPage.xaml.cs` to apply localized strings to the new UI elements. 229c1bc
- [x] Task: Update `CreateLinkPage.xaml.cs` (ViewModel logic) to toggle visibility of "Working Directory" based on selected Link Type. 762f97b
- [x] Task: Conductor - User Manual Verification 'Foundation & UI Updates' (Protocol in workflow.md)

## Phase 2: Batch File Implementation [checkpoint: 232566f]
- [x] Task: Implement `BatchLinkService` (or similar). 1f60098
    - [x] Sub-task: Write Tests: Verify correct string generation for batch content (with and without working dir).
    - [x] Sub-task: Implement: `CreateBatchFile(string sourcePath, string targetPath, string workingDir)`.
- [x] Task: Integrate Batch creation into `MainWindow` / `CreateLinkPage` "Create" button logic. 1f60098
- [x] Task: Conductor - User Manual Verification 'Batch File Implementation' (Protocol in workflow.md)

## Phase 3: Shortcut Implementation [checkpoint: a496f76]
- [x] Task: Research and add necessary COM/Interop dependencies for creating `.lnk` files in .NET 10 / WinUI 3. 1a73d3e
- [x] Task: Implement `ShortcutService`. af508c3
    - [x] Sub-task: Write Tests: Verify shortcut creation logic (mocking file system/COM if possible, or integration test).
    - [x] Sub-task: Implement: `CreateShortcut(string sourcePath, string targetPath, string workingDir)`.
- [x] Task: Integrate Shortcut creation into "Create" button logic. af508c3
- [x] Task: Conductor - User Manual Verification 'Shortcut Implementation' (Protocol in workflow.md)

## Phase 4: Validation & Polish
- [x] Task: Manual Verification: Verify all 4 types (Symlink, Hardlink, Batch, Shortcut) works as expected. 1a73d3e
- [x] Task: Update "Help" or "About" page to mention new types if necessary. de7ce84
- [ ] Task: Conductor - User Manual Verification 'Validation & Polish' (Protocol in workflow.md)
