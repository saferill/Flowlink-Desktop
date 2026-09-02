<div align="center">

<img src="./banner.jpg" alt="FlowLink Desktop & Android Mockup Banner" width="100%" style="border-radius: 16px; margin-bottom: 20px; box-shadow: 0 12px 36px rgba(0,0,0,0.6);"/>

# 💻 FlowLink Desktop

### *The Lightweight, Native Windows Companion for Seamless Device Integration*

[![GitHub Android Repo](https://img.shields.io/badge/Android_Companion-FlowLink_Android-10b981?style=for-the-badge&logo=android&logoColor=white)](https://github.com/saferill/Flowlink-Android)
[![Windows](https://img.shields.io/badge/Windows-10%20%2F%2011%20(x64)-0078d4?style=for-the-badge&logo=windows&logoColor=white)](https://github.com/saferill/Flowlink-Desktop)
[![.NET 9](https://img.shields.io/badge/.NET-9.0_WinUI_3-512bd4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com)
[![License](https://img.shields.io/badge/License-GPL_v3-f59e0b?style=for-the-badge)](LICENSE)

<br/>

**FlowLink Desktop** is a modern, high-performance WinUI 3 application designed to seamlessly bridge your Windows PC with your Android smartphone. Experience **zero-latency real-time clipboard sync**, **encrypted peer-to-peer file transfers**, and **instant PC power controls** without third-party cloud servers.

</div>

---

## 📸 Tampilan Aplikasi (App Screenshots)

<div align="center">

### 💻 Antarmuka Windows Desktop
<img src="./screenshot_desktop.png" width="90%" style="border-radius: 12px; box-shadow: 0 8px 24px rgba(0,0,0,0.35); margin-bottom: 24px;"/>

### 📱 Pasangan Android Mobile
| Beranda & Remote Control | Manajemen Perangkat | Pengaturan Android |
| :---: | :---: | :---: |
| <img src="./screenshot_android_home.jpg" width="240" style="border-radius: 12px; box-shadow: 0 4px 12px rgba(0,0,0,0.2);"/> | <img src="./screenshot_android_devices.jpg" width="240" style="border-radius: 12px; box-shadow: 0 4px 12px rgba(0,0,0,0.2);"/> | <img src="./screenshot_android_settings.jpg" width="240" style="border-radius: 12px; box-shadow: 0 4px 12px rgba(0,0,0,0.2);"/> |

</div>

---

## 📥 Download FlowLink (Both Devices Required)

> 💡 **Penting**: Untuk menghubungkan perangkat, pasang FlowLink di laptop/PC Windows **DAN** di smartphone Android kamu.

| Platform | Download Link | Deskripsi / Format |
| :--- | :--- | :--- |
| 💻 **Windows PC** | 📥 [**FlowLink Desktop Setup (.exe)**](https://github.com/saferill/Flowlink-Desktop/releases/latest) <br><br> 🗜️ [**FlowLink Desktop Portable (.zip)**](https://github.com/saferill/Flowlink-Desktop/releases/latest) | Installer resmi dengan update otomatis, atau versi Portable tanpa instalasi (Windows 10/11) |
| 📱 **Android** | 📦 [**Download FlowLink APK (v1.0.0)**](https://github.com/saferill/Flowlink-Android/releases/latest) <br> *(Pengajuan IzzyOnDroid / F-Droid sedang dalam peninjauan)* | File APK resmi (Android 8.0+) |

---

## 🔗 Repositori Resmi
* 💻 **Windows Desktop Repo**: [saferill/Flowlink-Desktop](https://github.com/saferill/Flowlink-Desktop)
* 📱 **Android App Repo**: [saferill/Flowlink-Android](https://github.com/saferill/Flowlink-Android)

---

## 🌟 Arsitektur & Cara Kerja

<div align="center">
  <img src="./architecture.gif" alt="FlowLink Live Architecture Motion Flow" width="100%" style="border-radius: 16px; margin-bottom: 16px; box-shadow: 0 10px 30px rgba(0,0,0,0.5);"/>
</div>

Open the interactive simulation in your browser:  
👉 **[`architecture.html`](./architecture.html)** *(Interactive motion flow, live packet simulator, and real-time signal waveforms)*

---

## 🚀 Fitur Unggulan

### 1. ⚡ Transfer File Super Cepat
- Menggunakan socket buffer adaptif (hingga 4 MB mega-chunk) untuk transfer video 4K dan foto berukuran besar di jaringan Wi-Fi lokal dengan kecepatan maksimal.

### 2. 📋 Sinkronisasi Clipboard Instan
- Salin teks di laptop, langsung tempel (*paste*) di smartphone Android, begitu juga sebaliknya.

### 3. ⏱️ Auto-Reconnect 2 Detik Setelah Laptop Nyala / Restart
- FlowLink Desktop secara cerdas mencoba menyambung ulang setiap 2 detik ke IP HP kamu (baik Wi-Fi lokal maupun Tailscale) begitu laptop dihidupkan.

### 4. 🔒 100% Aman & Peer-to-Peer
- Seluruh komunikasi data dienkripsi langsung antar-perangkat (*End-to-End Encryption*) tanpa perantara server pihak ketiga.

---

## 🛠️ Build dari Source Code

```bash
# Clone repository
git clone https://github.com/saferill/Flowlink-Desktop.git
cd Flowlink-Desktop

# Publish Release
dotnet publish src/FlowLink/FlowLink.csproj -c Release -r win-x64 --self-contained
```

---

## 📜 Lisensi
Proyek ini dilisensikan di bawah lisensi **[GNU General Public License v3.0 (GPL-3.0)](LICENSE)**.
Dikembangkan dan dirawat secara aktif oleh **[saferill](https://github.com/saferill)**.
