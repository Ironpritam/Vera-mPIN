# Vera mPIN

**A Windows desktop companion for managing VeraCrypt volumes, with optional Windows Hello approval for saved credentials.**

Vera mPIN helps you add, mount, and dismount VeraCrypt container files and disk partitions from one interface. It does not replace VeraCrypt or change how a volume is encrypted.

> **Important security note:** When mPIN is enabled, Vera mPIN supplies the saved password to VeraCrypt using its command-line password option. VeraCrypt documents that this may expose the password to local process-inspection tools. The app requires explicit acknowledgement before saving a password. If this trade-off is not acceptable, use VeraCrypt's own password prompt without mPIN.

## Features

- Add VeraCrypt container files or partitions on internal and removable disks.
- Give saved mounts friendly names and choose a preferred drive letter.
- Automatically choose another free letter if the preferred one is occupied.
- Mount with VeraCrypt's native password prompt, or optionally request Windows Hello approval before using a saved password.
- Dismount volumes managed by Vera mPIN, dismount all app-managed volumes, or select a VeraCrypt volume mounted outside the app.
- Confirm mounted-volume identity before dismounting app-managed volumes.
- Keep mount profiles and saved credentials on the current Windows user account and PC.

## Get the app

The current release is **1.0.3**. Download and run the [Vera mPIN x64 installer](./Releases/VeraMPIN-Setup-1.0.3-x64.exe). It is self-contained and does not require a separate .NET Desktop Runtime installation.

The per-user installer offers typical and compact installation options. The typical option includes the corresponding VeraCrypt source archive; the compact option omits that archive. The installer does not install VeraCrypt system-wide, mount a volume, or modify disk partitions.

Windows may display a SmartScreen warning because the Vera mPIN installer is not code-signed. The bundled VeraCrypt executable is the official, Authenticode-signed 1.26.24 release.

## Requirements

### To run the installer

- 64-bit Windows 11, version 24H2 (build 26100) or later.
- Administrator access is not needed to install Vera mPIN for the current user. VeraCrypt may request elevation for operations that require it.

### To build from source

- Windows 11, version 24H2 (build 26100) or later, x64.
- .NET 10 SDK.
- Inno Setup 6, if building the installer.
- The bundled VeraCrypt 1.26.24 executable and matching source archive in `src/VeraMPIN/ThirdParty/`.

## Build from source

Run commands from the repository root.

Build the application:

```powershell
dotnet build .\src\VeraMPIN\VeraMPIN.csproj -c Release
```

Publish a framework-dependent build for local testing:

```powershell
dotnet publish .\src\VeraMPIN\VeraMPIN.csproj `
  -c Release -r win-x64 --self-contained false `
  -o .\src\VeraMPIN\artifacts\VeraMPIN-preview
Copy-Item .\src\VeraMPIN\ThirdParty\VeraCrypt-x64.exe `
  .\src\VeraMPIN\artifacts\VeraMPIN-preview\
& .\src\VeraMPIN\artifacts\VeraMPIN-preview\VeraMPIN.exe
```

Build the self-contained installer (requires Inno Setup 6):

```powershell
.\src\VeraMPIN\Installer\Build-Installer.ps1
```

The installer build script checks the VeraCrypt executable's version and Authenticode signature and checks that the source archive contains its license. It writes generated files under `src/VeraMPIN/artifacts/`, which is excluded from Git.

## Using Vera mPIN

1. Select **Add a partition** or **Add a VeraCrypt file**, then give the source a name you'll recognize.
2. Select the saved entry, choose a preferred mount letter, and click **Mount selected**.
3. VeraCrypt prompts for the password unless mPIN is configured for that entry.
4. To configure mPIN, choose **Set up mPIN for this volume**, enter and confirm the VeraCrypt password, and acknowledge the password-handling warning. Windows Hello approval is requested whenever the saved password is used.
5. Use **Dismount selected** for a volume managed by Vera mPIN. **Dismount all** attempts a normal dismount of every volume tracked by the app. Use **Dismount a VeraCrypt volume...** to choose a volume mounted directly in VeraCrypt.

The app does not force-dismount volumes. **Dismount all** only covers app-managed volumes; volumes mounted outside the app must be selected through the separate VeraCrypt-volume action.

At startup and every 30 seconds, Vera mPIN checks saved mount status against the current drive and recorded volume identity. Use **Refresh status** to check immediately. When a check finds a stale status or cannot verify a volume, the app shows a warning. If Windows or VeraCrypt already unmounted a volume (for example, after a restart), the app clears that stale status so the entry can be mounted again. If a drive letter is occupied by a different volume, it leaves that volume untouched and can choose another available letter. **Remove mPIN...** opens choices to remove the selected entry's saved password or all saved mPIN passwords.

### Partition-selection caution

The partition picker can show internal, system, boot, recovery, and EFI partitions, as well as removable disks. System and boot disks are marked and require an additional confirmation, but this is not a guarantee that a selected partition is safe to use. Check the disk and partition carefully and only select the intended encrypted volume.

## Where settings and credentials are stored

Mount profiles and preferences are stored per Windows user under:

```text
%LocalAppData%\Vera-mPIN
```

Saved passwords are protected with Windows DPAPI for the current Windows user, and Windows Hello approval is configured separately on each PC. These settings and credentials do **not** travel with the app or SSD. On first launch, the app migrates existing data from `%LocalAppData%\ExternalSsdUnlock` when present. If conflicting files are found, the app reports the migration problem and preserves the remaining legacy files rather than overwriting the new data.

This keeps the app itself portable: it can run from an unencrypted SSD partition alongside VeraCrypt, while its profiles and credentials remain local to each PC.

## Repository layout

```text
Vera-mPIN/
├── Releases/                       # Packaged installer
├── src/
│   └── VeraMPIN/
│       ├── Assets/                 # Application icon artwork
│       ├── Installer/              # Inno Setup definition and build script
│       ├── ThirdParty/             # VeraCrypt executable and matching source
│       ├── *.cs                   # Application source
│       └── VeraMPIN.csproj
├── .gitignore
└── README.md
```

## Third-party software

Vera mPIN bundles VeraCrypt 1.26.24, distributed under the GNU GPL. The installer includes the corresponding source archive in the typical installation. See [ThirdPartyNotices.txt](./src/VeraMPIN/Installer/ThirdPartyNotices.txt) for source links and SHA-256 checksums. Vera mPIN and VeraCrypt are separate programs.

## Project license

No license has been selected for Vera mPIN yet. Until a project license is added, do not assume that the Vera mPIN source is licensed for redistribution or modification. This is separate from the license applying to bundled third-party software.
