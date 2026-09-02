<div align="center">

<img src="./banner.jpg" alt="FlowLink Banner" width="100%" style="border-radius: 12px; margin-bottom: 16px;"/>

# FlowLink Desktop

Native Windows app to connect your PC with your Android phone.

[![GitHub Android Repo](https://img.shields.io/badge/Companion-FlowLink_Android-3ddc84?style=flat-square&logo=android&logoColor=white)](https://github.com/saferill/Flowlink-Android)
[![Windows](https://img.shields.io/badge/Windows-10%20%2F%2011%20(x64)-0078d4?style=flat-square&logo=windows&logoColor=white)](https://github.com/saferill/Flowlink-Desktop)
[![.NET 9](https://img.shields.io/badge/.NET-9.0_WinUI_3-512bd4?style=flat-square&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com)
[![License](https://img.shields.io/badge/License-GPL_v3-blue?style=flat-square)](LICENSE)

<br/>

Built with WinUI 3 and .NET 9. Send files, sync clipboard automatically, and execute PC remote power actions from your phone over local Wi-Fi or Tailscale.

</div>

---

## Screenshots

<div align="center">

### Windows App
<img src="./screenshot_desktop.png" width="85%"/>

### Android Mobile Companion
| Home | Devices | Settings |
| :---: | :---: | :---: |
| <img src="./screenshot_android_home.jpg" width="220"/> | <img src="./screenshot_android_devices.jpg" width="220"/> | <img src="./screenshot_android_settings.jpg" width="220"/> |

</div>

---

## Download

Install FlowLink on both your Windows PC and your Android phone.

| Platform | Download | Note |
| :--- | :--- | :--- |
| 💻 **Windows PC** | [**FlowLink Setup (.exe)**](https://github.com/saferill/Flowlink-Desktop/releases/latest) <br/> [**Portable (.zip)**](https://github.com/saferill/Flowlink-Desktop/releases/latest) | Windows 10 & 11 (x64) |
| 📱 **Android** | [**Download APK (v1.0.0)**](https://github.com/saferill/Flowlink-Android/releases/latest) | Android 8.0+ |

---

## Features

- **Fast File Transfer**: Direct peer-to-peer file transfer over your local network.
- **Instant Clipboard Sync**: Copies on your PC appear immediately on your phone, and vice-versa.
- **Power Actions**: Lock, sleep, or shutdown your PC triggered directly from Android.
- **System Tray & Autostart**: Minimizes cleanly to the system tray and starts on Windows boot.
- **Works with Tailscale**: Seamless discovery across different subnets.

---

## How it works

<div align="center">
  <img src="./architecture.gif" alt="FlowLink Architecture" width="100%" style="border-radius: 12px; margin-bottom: 12px;"/>
</div>

---

## Building from source

Requirements:
- Windows 10/11
- .NET 9 SDK
- Visual Studio 2022 with Windows App SDK

```powershell
git clone https://github.com/saferill/Flowlink-Desktop.git
cd Flowlink-Desktop
dotnet build -c Release
```

---

## License

This project is licensed under the **[GNU General Public License v3.0 (GPL-3.0)](LICENSE)**.
Created by **[saferill](https://github.com/saferill)**.

