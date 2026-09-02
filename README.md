<div align="center">

<img src="./logo.png" alt="FlowLink Logo" width="140" style="border-radius: 28px; box-shadow: 0 8px 24px rgba(0,0,0,0.3); margin-bottom: 12px;"/>

# ⚡ FlowLink Desktop

### *High-Performance Windows Companion for FlowLink Android*

[![GitHub Android Repo](https://img.shields.io/badge/Android_Companion-FlowLink_Android-10b981?style=for-the-badge&logo=android&logoColor=white)](https://github.com/saferill/Flowlink-Android)
[![Windows](https://img.shields.io/badge/Windows-10%20%2F%2011-00d2ff?style=for-the-badge&logo=windows&logoColor=white)](https://github.com/saferill/Flowlink-Desktop)
[![.NET](https://img.shields.io/badge/.NET-9.0_%2F_WinUI_3-9d4edd?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com)
[![License](https://img.shields.io/badge/License-GPL_v3-f59e0b?style=for-the-badge)](LICENSE)

<br/>

**FlowLink Desktop** is a modern, lightweight, native Windows companion built with **WinUI 3 and .NET 9**. It bridges your PC with your Android smartphone featuring **silent system tray startup**, **adaptive fast network discovery**, **Windows Explorer right-click integration**, and **bi-directional clipboard synchronization**.

</div>

---

## 📥 Download FlowLink (Both Devices Required)

> 💡 **Penting**: Untuk menghubungkan perangkat, pasang FlowLink di laptop/PC Windows **DAN** di smartphone Android kamu.

| Platform | Download Link | Deskripsi / Format |
| :--- | :--- | :--- |
| 💻 **Windows PC** | 📥 [**FlowLink Desktop Setup (.exe)**](https://github.com/saferill/Flowlink-Desktop/releases/latest) <br><br> 🗜️ [**FlowLink Desktop Portable (.zip)**](https://github.com/saferill/Flowlink-Desktop/releases/latest) | Installer resmi dengan shortcut Start Menu & Desktop, atau versi Portable tanpa instalasi (Windows 10/11) |
| 📱 **Android** | [![Get it on IzzyOnDroid](https://gitlab.com/IzzyOnDroid/repo/-/raw/master/assets/IzzyOnDroid.png)](https://apt.izzysoft.de/fdroid/index/apk/com.castle.FlowLink) <br><br> 📦 [**Download APK Langsung (v1.0.0)**](https://github.com/saferill/Flowlink-Android/releases/latest) | Pasang via F-Droid / IzzyOnDroid untuk update otomatis, atau unduh file APK resmi (Android 8.0+) |

---

## 🔗 Repositori Resmi
* 💻 **Windows Desktop Repo**: [saferill/Flowlink-Desktop](https://github.com/saferill/Flowlink-Desktop)
* 📱 **Android App Repo**: [saferill/Flowlink-Android](https://github.com/saferill/Flowlink-Android)

---

## 🌟 Architecture Overview

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

## 🚀 Fitur Unggulan Desktop

### 1. 🪟 Silent System Tray Autostart on Boot / Restart
- Berjalan otomatis di latar belakang (*system tray*) saat Windows menyala tanpa membuka jendela yang mengganggu.
- Ikon tray Win32 dibuat langsung secara responsif.

### 2. 🖱️ Integrasi Menu Klik-Kanan Windows Explorer
- Klik kanan file apa saja di laptop kamu ➔ pilih **"Send to Phone"** untuk langsung mengirim file ke Android tanpa perantara cloud.

### 3. 📋 Sinkronisasi Clipboard Dua Arah
- Teks yang kamu copy di Windows langsung tersedia di clipboard Android dalam hitungan milidetik.

### 4. 🔒 Kendali Jarak Jauh dari Android
- Kunci layar Windows (*Lock Workstation*), tutup semua aplikasi, atau matikan laptop dari jarak jauh melalui smartphone kamu.

---

## 🛠️ Build dari Source Code

```bash
# Clone repository
git clone https://github.com/saferill/Flowlink-Desktop.git
cd Flowlink-Desktop

# Build Release Installer
powershell -ExecutionPolicy Bypass -File build-release.ps1
```

---

## 📜 Lisensi
Proyek ini dilisensikan di bawah lisensi **[GNU General Public License v3.0 (GPL-3.0)](LICENSE)**.
Dikembangkan dan dirawat secara aktif oleh **[saferill](https://github.com/saferill)**.
