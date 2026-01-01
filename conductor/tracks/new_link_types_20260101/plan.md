# Plan: New Link Types (Batch & Shortcut)

## Phase 1: Foundation & UI Updates
- [x] Task: Create `LinkType` Enum extension or update existing logic to support `Batch` and `Shortcut`. ccb56d3
- [x] Task: Update `CreateLinkPage.xaml` to include the "Working Directory" GroupBox (TextBox + Button). 1799a53
- [x] Task: Reorder UI: Move "Working Directory" GroupBox above "Link Type" GroupBox in `CreateLinkPage.xaml`. 9f9ac5a
- [x] Task: Localization: Add resource strings for "Batch File", "Shortcut", and "Working Directory" in `Resources.resw` (en-US and zh-CN). eb13533
- [ ] Task: Localization: Update `CreateLinkPage.xaml.cs` to apply localized strings to the new UI elements.
- [x] Task: Update `CreateLinkPage.xaml.cs` (ViewModel logic) to toggle visibility of "Working Directory" based on selected Link Type. 762f97b
    - *Sub-task:* Implement `IsWorkingDirectoryVisible` property.
    - *Sub-task:* Bind GroupBox visibility to this property.
    - *Sub-task:* Logic to auto-fill default working directory (Source Path's folder) when Source is selected.
- [ ] Task: Conductor - User Manual Verification 'Foundation & UI Updates' (Protocol in workflow.md)

## Phase 2: Batch File Implementation
- [ ] Task: Implement `BatchLinkService` (or similar).
    - *Sub-task:* Write Tests: Verify correct string generation for batch content (with and without working dir).
    - *Sub-task:* Implement: `CreateBatchFile(string sourcePath, string targetPath, string workingDir)`.
- [ ] Task: Integrate Batch creation into `MainWindow` / `CreateLinkPage` "Create" button logic.
- [ ] Task: Conductor - User Manual Verification 'Batch File Implementation' (Protocol in workflow.md)

## Phase 3: Shortcut Implementation
- [ ] Task: Research and add necessary COM/Interop dependencies for creating `.lnk` files in .NET 10 / WinUI 3.
- [ ] Task: Implement `ShortcutService`.
    - *Sub-task:* Write Tests: Verify shortcut creation logic (mocking file system/COM if possible, or integration test).
    - *Sub-task:* Implement: `CreateShortcut(string sourcePath, string targetPath, string workingDir)`.
- [ ] Task: Integrate Shortcut creation into "Create" button logic.
- [ ] Task: Conductor - User Manual Verification 'Shortcut Implementation' (Protocol in workflow.md)

## Phase 4: Validation & Polish
- [ ] Task: Manual Verification: Verify all 4 types (Symlink, Hardlink, Batch, Shortcut) works as expected.
- [ ] Task: Update "Help" or "About" page to mention new types if necessary.
- [ ] Task: Conductor - User Manual Verification 'Validation & Polish' (Protocol in workflow.md)
