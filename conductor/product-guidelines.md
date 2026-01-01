# Product Guidelines

## Visual Identity
- **Native Experience:** LinkTo strictly adheres to the **Windows 11 Fluent Design System**. This includes:
  - Integration of **Mica** or **Mica Alt** backdrop materials to provide visual hierarchy and depth.
  - Consistent use of **rounded corners** for all windows, containers, and controls.
  - Standard WinUI 3 typography and iconography.
- **System Integration:** The application fully respects the user's **Windows Accent Color** and toggles seamlessly between **Light and Dark modes** based on system settings. This ensures the app feels like a first-party utility.

## Communication Style
- **Concise & Professional:** Prose should be direct, neutral, and focused on efficiency. Avoid excessive personality or fluff.
- **Terminology:** Use industry-standard terms (e.g., "Symbolic Link", "Hard Link") to maintain professional alignment and educate the user.
- **Layered Knowledge:** To support casual users, provide contextual help via **tooltips** or **info icons** that link to clearer explanations or documentation without cluttering the main UI.

## Interaction & Feedback
- **In-App Feedback:** Upon successful actions (like link creation), provide immediate but non-intrusive confirmation using **in-app notification banners** or toast-style popups within the application frame.
- **Navigation:** Maintain a clean, lateral navigation structure (e.g., NavigationView) to separate core tasks like "Create Link", "History", and "Settings".
