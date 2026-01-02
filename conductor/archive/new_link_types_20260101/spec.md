# Specification: New Link Types (Batch & Shortcut)

## Overview
Expand the capabilities of LinkTo by adding two new "link" types: Batch File Launchers (.bat) and Windows Shortcuts (.lnk). Both new types will support a configurable "Working Directory" property.

## Detailed Requirements

### 1. Batch File Launcher (.bat)
- **Concept:** Create a `.bat` file that sets a working directory and then launches the target file.
- **UI Changes:**
  - Add "Batch File" to the Link Type selection.
  - When selected, show a new GroupBox: "Working Directory".
    - Controls: TextBox (path input), Button ("Browse...").
    - Behavior: Defaults to the source file's parent directory.
- **Logic:**
  - Content of generated `.bat` file:
    ```batch
    @echo off
    cd /d "UserSelectedWorkingDirectory"
    start "" "PathToSourceFile"
    ```
  - If "Working Directory" is empty or invalid, omit the `cd` command.

### 2. Windows Shortcut (.lnk)
- **Concept:** Create a standard Windows Shell Link (IShellLink).
- **UI Changes:**
  - Add "Shortcut" to the Link Type selection.
  - Reuse the "Working Directory" GroupBox.
- **Logic:**
  - Use Windows API (Shell32/IShellLink) to create the `.lnk` file.
  - Set `TargetPath` to source file.
  - Set `WorkingDirectory` to user input (defaulting to source file's directory).

### 3. UI/UX General
- The "Working Directory" GroupBox should be visible **only** when "Batch File" or "Shortcut" is selected.
- **Layout Order:** The "Working Directory" GroupBox must be positioned **above** the "Link Type" GroupBox.
- **Localization:** All new UI strings (e.g., "Batch File", "Shortcut", "Working Directory") must support localization in both English and Chinese.
- Validation: Ensure the target path for the `.bat` or `.lnk` does not already exist (or prompt to overwrite).

## Technical Considerations
- **Batch File:** Simple text file writing. Encoding should be safe (ANSI/UTF-8 with BOM? System default usually works best for batch).
- **Shortcut:** Requires COM interop.
  - *Option A:* Use `Windows Script Host Object Model` (IWshRuntimeLibrary).
  - *Option B:* P/Invoke `IShellLink`.
  - *Decision:* Since this is a WinUI 3 app with .NET 10, check if `Microsoft.Windows.SDK.NET` provides a projection for ShellLink or if we need a COM wrapper. If strictly native is preferred, P/Invoke or a helper library is standard.

## Acceptance Criteria
- [ ] User can select "Batch File" and create a functional .bat file that opens the target.
- [ ] User can set a custom Working Directory for the Batch File, and it is reflected in the generated script.
- [ ] User can select "Shortcut" and create a functional .lnk file.
- [ ] User can set a custom Working Directory for the Shortcut, and checking "Properties" on the generated shortcut confirms it is set.
- [ ] "Working Directory" UI is hidden for Symlink/Hardlink types.
