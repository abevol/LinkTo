# Initial Concept
A Windows application for creating and managing symbolic links and hard links.

# Product Vision
LinkTo aims to democratize the power of Windows file system links for casual users. By abstracting the complexity of command-line tools into a modern, intuitive WinUI 3 interface, LinkTo enables users to organize their files and optimize disk space without needing technical expertise.

# Target Audience
- **Casual Windows Users:** Individuals who want the benefits of symbolic and hard links (e.g., accessing files from multiple locations without duplication) but find the native `mklink` command intimidating.

# Core Value Proposition
- **Simplicity over Complexity:** Transforms a technical CLI task into a familiar drag-and-drop experience.
- **Accessibility:** Provides clear, goal-oriented guidance that helps users achieve their tasks without needing to understand underlying file system mechanics.

# Key Features
- **Integrated Guidance (Link Wizard):** An optional, non-intrusive "Show Help" panel within the main interface that explains link types in simple, action-oriented terms (e.g., "Access file from two places" instead of "Create Symbolic Link").
- **Drag & Drop Creation:** Simple interaction model for defining source and target paths.
- **Windows Explorer Integration:** Quick access via the right-click context menu.
- **Link History:** A clear view of managed links to ensure users always know where their data is truly stored.

# Design Tone & Style
- **Action-Oriented:** Focused on "what" the user wants to do.
- **Simple & Clear:** Avoiding technical jargon where possible.
- **Modern & Native:** Leveraging WinUI 3 and Mica effects to feel like a natural part of Windows.
