# YZ-Volume



YZ-Volume is a sophisticated audio control utility for Windows, designed for power users who need fast, granular control over their system's audio devices. It provides a sleek, modern UI that lives in the system tray and offers advanced integration with VB-Audio's powerful Matrix software.

This application was built to solve the workflow limitations of standard Windows audio controls, providing a centralized and highly configurable hub for complex audio setups.

---

## ✨ Features

*   **System Tray Integration:** Runs quietly in the system tray, providing instant access via a single click.
*   **Multi-Device Control:** Adjust the "base volume" (`mmsys.cpl` level) for all your selected Windows audio devices (speakers, microphones, etc.) from one place.
*   **Modern, Minimalist UI:** A clean, dark-themed, and borderless widget that feels like a native part of the Windows 11/10 UI.
*   **Advanced VB-Audio Matrix Integration (Opt-In):**
    *   **Preset Selector:** Switch between your saved VB-Audio Matrix presets directly from the main UI.
    *   **Dynamic Slider Generation:** The UI automatically rebuilds itself to show the specific channel sliders relevant to your active preset.
    *   **Multi-Crosspoint Control:** A single slider can control the gain of multiple crosspoints simultaneously.
    *   **Absolute & Relative Control:** Use the main slider to set absolute `dBGain` values, or use the `+` / `-` buttons to nudge the gain of all associated crosspoints relatively, preserving your mix.
*   **Powerful Preset Manager:**
    *   **Import:** Import presets directly from VB-Audio Matrix's native `.xml` files.
    *   **Export:** Export your configured presets back to `.xml` for backup or sharing.
    *   **Manage:** Add, delete, and re-order your library of presets.
    *   **Ships with Defaults:** Comes pre-configured with three powerful default presets (PC 5.1, PC 2.0, Beamer 5.1).
*   **Highly Configurable:**
    *   Choose exactly which Windows devices and Matrix sliders are visible.
    *   Assign custom, user-friendly names to all devices.

---

https://github.com/user-attachments/assets/b3212844-3708-45e6-9301-95ff9df91ef8

---

## 📸 Screenshots

#### Main Control Widget
*The main flyout provides instant access to your most important controls.*


#### Settings Window
*A powerful, all-in-one control center for configuring devices, VBAN, and managing presets.*


---

## 🚀 Getting Started

### Prerequisites

*   Windows 10 or 11
*   [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) (Download the "x64" version under the ".NET Desktop Runtime" section).
*   (Optional) [VB-Audio Matrix](https://vb-audio.com/Matrix/) for advanced VBAN integration.

### Installation

1.  Go to the [**Releases**](https://github.com/your-username/yz-volume/releases) page of this repository.
2.  Download the latest `YZ-Volume_vX.X.X_win-x64.zip` file.
3.  Unzip the contents to a folder on your computer (e.g., `C:\Program Files\YZ-Volume`).
4.  Run `YZ-Volume.exe`. The application icon will appear in your system tray.

### Configuration

1.  **Right-click** the YZ-Volume tray icon and select **"Settings..."**.
2.  **Windows Devices:** Check the boxes for the speakers and microphones you want to control. You can also assign custom names in the textboxes on the right.
3.  **VBAN Integration (Optional):**
    *   Toggle the **VBAN** button to ON.
    *   Ensure the IP Address and Port match your VB-Audio Matrix VBAN settings (default is `127.0.0.1` and `6980`).
    *   Use the **Preset Manager** to import your custom `.xml` preset files or manage the defaults. Make sure to assign the correct **VBAN Index** for each preset, which corresponds to its position in the Matrix UI (e.g., `1` for `PresetPatch[1]`).
4.  Click **Save**. Your main widget is now configured and ready to use.

---

## 🛠️ Built With

*   **C#** and **.NET 8**
*   **WPF (Windows Presentation Foundation)** for the user interface.
*   **NAudio** for interacting with Windows Core Audio APIs.
*   **Newtonsoft.Json** for saving settings and presets.



