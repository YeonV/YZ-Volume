![version](https://img.shields.io/github/v/release/YeonV/YZ-Volume?label=VERSION&logo=git&logoColor=white) [![creator](https://img.shields.io/badge/CREATOR-Yeon-blue.svg?logo=github&logoColor=white)](https://github.com/YeonV) [![creator](https://img.shields.io/badge/A.K.A-Blade-darkred.svg?logo=github&logoColor=white)](https://github.com/YeonV)
<h1><img width="40" height="40" alt="YZ-Volume-Logo" src="https://github.com/user-attachments/assets/b0902573-34d1-4875-8a80-f34691738e05" align="top" />&nbsp;YZ-Volume</h1>

Tray-App to control the volume of selectable audio devices.
Bonus: VB-audio-Matrix integration

<img width="284" height="328" alt="image" src="https://github.com/user-attachments/assets/2fc1cc52-99a1-4c4c-8374-c77c713efbe8" />

<details>
   <summary>Settings Page</summary>   
   
   <img width="927" height="1346" alt="image" src="https://github.com/user-attachments/assets/aad78c0e-8119-4f11-9b7b-84fc1b797f41" />
</details>

<details>
   <summary> ✨ Video ✨ </summary>   
   
   https://github.com/user-attachments/assets/b3212844-3708-45e6-9301-95ff9df91ef8
   
</details>

---


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
*   **Highly Configurable:**
    *   Choose exactly which Windows devices and Matrix sliders are visible.
    *   Assign custom, user-friendly names to all devices.

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

1.  select **"Settings..."**.
2.  **Windows Devices:** Check the boxes for the speakers and microphones you want to control. You can also assign custom names in the textboxes on the right.
3.  **VBAN Integration (Optional):**
    *   Toggle the **VBAN** button to ON.
    *   Ensure the IP Address and Port match your VB-Audio Matrix VBAN settings (default is `127.0.0.1` and `6980`).
4.  Click **Save**. Your main widget is now configured and ready to use.

---

## 🛠️ Built With

*   **C#** and **.NET 8**
*   **WPF (Windows Presentation Foundation)** for the user interface.
*   **NAudio** for interacting with Windows Core Audio APIs.
*   **Newtonsoft.Json** for saving settings and presets.



