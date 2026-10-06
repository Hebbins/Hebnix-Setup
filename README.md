# Hebnix Setup

The Windows installer for [Hebnix](https://hebnix.com). It installs, updates and uninstalls Hebnix and Hebnix Lite in `%AppData%\Hebnix`, and has a Cleanup menu that restores patched game files, removes workshop maps, the multiplayer TAP adapter and the spoofer's hosts/proxy changes.

## One-line install

Run in PowerShell (no admin needed):

**Hebnix**

```powershell
irm https://raw.githubusercontent.com/Hebbins/Hebnix-Setup/main/install.ps1 | iex
```

**Hebnix Lite**

```powershell
irm https://raw.githubusercontent.com/Hebbins/Hebnix-Setup/main/install-lite.ps1 | iex
```

The script does the same as the Install button in setup: it downloads the latest build from `api.hebnix.com`, extracts it to `%AppData%\Hebnix`, creates Desktop and Start Menu shortcuts and records the installed version. It also installs Hebnix Setup to `%AppData%\Hebnix\updater\setup.exe` with a **Start Menu > Hebnix > Hebnix Setup** shortcut.

## Uninstall and cleanup

Open **Start Menu > Hebnix > Hebnix Setup** and use Uninstall or Cleanup. This works the same whether Hebnix was installed with setup or the one-line command.

## Building

Requirements: Visual Studio 2022 or Build Tools with the .NET Framework 4.8 targeting pack.

The project expects NuGet packages in `..\packages` (one level above this folder):

```powershell
msbuild Hebnix-Updater.csproj -t:restore -p:RestorePackagesConfig=true -p:SolutionDir=..\
msbuild Hebnix-Updater.csproj -p:Configuration=Release
```

Output: `bin\Release\Hebnix-Updater.exe` (needs `Newtonsoft.Json.dll` beside it).

Running the built exe is not a dry run: it copies itself to `%AppData%\Hebnix\updater\setup.exe` and closes any running Hebnix on startup.

### Testing the install script

Set `HEBNIX_INSTALL_DIR` to install into a scratch folder instead of `%AppData%\Hebnix`. Shortcuts then go to `_test-shortcuts` inside it instead of your Desktop and Start Menu:

```powershell
$env:HEBNIX_INSTALL_DIR = "$env:TEMP\hebnix-test"
Get-Content .\install.ps1 -Raw | iex          # same path as irm | iex
& .\install.ps1 -Edition Lite
```
