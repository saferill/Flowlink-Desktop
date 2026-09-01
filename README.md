<div align="center">

# 💻 FlowLink Desktop
### *High-Performance Windows Companion for FlowLink Android*

[![GitHub Android Repo](https://img.shields.io/badge/Android_Repo-FlowLink_Android-10b981?style=for-the-badge&logo=android&logoColor=white)](https://github.com/saferill/Flowlink-Android)
[![Windows](https://img.shields.io/badge/Windows-10%20%2F%2011-00d2ff?style=for-the-badge&logo=windows&logoColor=white)](https://github.com/saferill/Flowlink-Desktop)
[![.NET](https://img.shields.io/badge/.NET-9.0_%2F_WinUI_3-9d4edd?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com)
[![License](https://img.shields.io/badge/License-GPL_v3-f59e0b?style=for-the-badge)](LICENSE)

<br/>

**FlowLink Desktop** is a modern, lightweight, native Windows application built with **WinUI 3 and .NET 9**. It bridges your PC with your Android smartphone with **silent startup system tray execution**, **adaptive Tailscale fast polling**, **Windows Explorer right-click integration**, and **zero-latency socket transfers**.

</div>

---

## 🌟 Interactive Live Architecture

Open the interactive simulation in your browser:
👉 **[`architecture.html`](./architecture.html)** *(Interactive motion flow, live packet simulator, and real-time signal waveforms)*

```mermaid
flowchart TD
    subgraph STARTUP["🚀 Windows Startup & Lifecycle"]
        direction TB
        B1["Laptop Power On / Restart"] --> B2["Silent Startup Mode (InTray)"]
        B2 --> B3["TrayIcon.ForceCreate() (Win32 NotifyIcon)"]
        B3 --> B4["4-Second Tailscale Initialization Delay"]
    end

    subgraph DISCOVERY["🌐 Connection & Polling Engine"]
        direction TB
        P1["Adaptive 3-Second Fast Polling Loop"] --> P2["Scan Tailscale Peers & UDP LAN"]
        P2 --> P3{"Android Found?"}
        P3 -- "Yes" --> P4["Establish TLS 1.3 Socket (NoDelay)"]
        P4 --> P5["Idle Mode (20s Heartbeat)"]
        P3 -- "No" --> P1
    end

    subgraph ACTIONS["⚡ System Actions & Explorer Menu"]
        direction TB
        A1["Right-Click Explorer Menu ➔ 'Send to Phone'"]
        A2["Win32 LockWorkStation() ➔ Instant Lock (0ms)"]
        A3["Win32 EnumWindows + WM_CLOSE ➔ Close All Open Apps & Tabs"]
        A4["shutdown.exe /s /t 0 /f ➔ Quick Power Off"]
    end

    STARTUP ==> DISCOVERY
    DISCOVERY ==> ACTIONS
```

---

## 🚀 Key Desktop Features

### 1. 🪟 Silent System Tray Autostart on Boot / Restart
- Starts seamlessly on Windows boot without opening intrusive windows.
- System tray icon is forced into the taskbar immediately via Win32 `Shell_NotifyIcon`.
- Closing the window minimizes back to the tray so background synchronization never stops.

### 2. ⏱️ Adaptive 3-Second Tailscale Fast Polling
- **Startup Grace Period**: Waits 4 seconds upon boot for Windows network stack & Tailscale daemon to be ready.
- **Fast Auto-Connect**: Searches every **3 seconds** until your Android phone is discovered, establishing connection automatically.
- **Power Efficiency**: Switches to a low-power 20-second heartbeat once connected.

### 3. 🖱️ Windows Explorer Right-Click Context Menu
- Right-click any file, photo, video, or folder in Windows Explorer:
  👉 **"Send to Phone (FlowLink)"**
- Instantly transmits the files to your active phone in milliseconds.

### 4. ⚡ Native Win32 System Power Execution
- **Lock Screen**: Calls `user32.dll` `LockWorkStation()` (0 ms).
- **Close All Apps**: Iterates over top-level windows and sends `WM_CLOSE` to close all browsers, open tabs, and applications cleanly.
- **Shutdown / Restart / Hibernate / Log Off**.

### 5. 🚀 Ultra-Speed Socket Transfers
- Configured with `OptionNoDelay = true` (disables Nagle's algorithm) and **2 MB – 4 MB socket buffers** for maximum wire-speed file throughput.

---

## 🏗️ Project Architecture

```
FlowLink Desktop/
├── src/FlowLink/
│   ├── Assets/Icons/          # High-resolution multi-layer vectors & app tiles
│   ├── Data/                  # Models, Contracts, SQLite repositories
│   ├── Helpers/               # App lifecycle, Windows context menu registration
│   ├── Platforms/Windows/     # Native Win32 services (WindowsActionService)
│   ├── Services/
│   │   ├── DiscoveryService.cs   # Tailscale adaptive polling & UDP discovery
│   │   ├── FileTransfer/         # Adaptive chunk stream engine (64KB–4MB)
│   │   └── Socket/               # Low-latency SocketProvider (NoDelay)
│   ├── UserControls/             # TrayIconControl (Win32 NotifyIcon)
│   └── Views/                    # WinUI 3 pages & dialogs
```

---

## 💻 How to Use

1. **Launch**: Open FlowLink Desktop (or let it launch automatically on startup).
2. **Pairing**: Connect via Wi-Fi LAN or Tailscale. Confirm pairing code on both devices.
3. **Enjoy Zero-Click Continuity**:
   - Copy text on laptop ➔ Pasted instantly on phone.
   - Right-click file ➔ **Send to Phone (FlowLink)**.
   - Control laptop power directly from your Android phone's control card!

---

<div align="center">
  <sub>Built with ❤️ for seamless cross-platform productivity.</sub>
</div>
