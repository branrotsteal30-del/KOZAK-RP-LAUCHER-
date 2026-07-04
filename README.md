# KOZAK RP Launcher

Official launcher for KOZAK RP multiplayer server.

## Completed

- Cleaned decompiler-generated duplicate resource entries.
- Added required dependencies (`Newtonsoft.Json`, `System.Resources.Extensions`).
- Fixed decompilation compile artifacts in `MainWindow.cs`.
- Created Visual Studio solution: `legion-gta_Launcher.sln`.
- Verified build: `Release | net48`.
- Added code signing configuration for SmartScreen bypass.
- Added GitHub Actions workflow for automated signing.
- Updated assembly metadata for better Windows recognition.

## Build in Visual Studio

1. Open `legion-gta_Launcher.sln`.
2. Ensure workload **.NET desktop development** is installed.
3. Select `Release` or `Debug`.
4. Run **Build Solution**.

Output executable:

`bin\Release\net48\KOZAK RP.exe`

## Build Installer

Run the installer build script:

```batch
cd installer
build_inno_installer.bat
```

Output installer:

`installer\installer_output\KOZAK_RP_Launcher_Setup_v3.9.exe`

## Code Signing (SmartScreen Bypass)

To bypass Windows SmartScreen blocking, the executable needs to be digitally signed.

### Option 1: GitHub Actions (Recommended)

1. Push project to GitHub
2. Add secrets to repository:
   - `CERTIFICATE` - Base64 encoded PFX certificate
   - `CERTIFICATE_PASSWORD` - Certificate password
3. Run workflow: `.github/workflows/build-and-sign.yml`
4. Download signed artifacts

### Option 2: Local Signing

1. Obtain a code signing certificate (DigiCert, Sectigo, etc.)
2. Add certificate to project or set environment variables:
   ```bash
   set CERTIFICATE=path\to\certificate.pfx
   set CERTIFICATE_PASSWORD=your_password
   ```
3. Enable signing in `KOZAK RP.csproj`:
   ```xml
   <SignAssembly>true</SignAssembly>
   <AssemblyOriginatorKeyFile>path\to\certificate.pfx</AssemblyOriginatorKeyFile>
   ```
4. Build project - it will automatically sign the output

### Option 3: Free Signing (GitHub/GitLab)

Use free code signing from:
- GitHub Actions (Trusted Signing)
- GitLab CI/CD with free certificates
- SignPath.io (free for open source)

## SmartScreen Prevention Tips

Without a certificate, these changes help reduce blocking:
- Changed execution level from `requireAdministrator` to `asInvoker` in `app.manifest`
- Added proper assembly metadata (publisher, product, description)
- Synchronized version numbers
- Added detailed copyright information

## Notes

- This is decompiled code, so small behavior differences from the original binary are possible.
- Embedded resources and Costura payload files (`costura*.compressed`) are kept to preserve runtime behavior.
- For production release, always use code signing to avoid SmartScreen warnings.
