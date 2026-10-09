# Hebnix one-line installer
#
#   irm https://raw.githubusercontent.com/Hebbins/Hebnix-Setup/main/install.ps1 | iex
#   irm https://raw.githubusercontent.com/Hebbins/Hebnix-Setup/main/install-lite.ps1 | iex
#
# Does the same thing as the Install button in setup.exe (MainForm.InstallOrUpdateAsync),
# writing the same files, shortcuts and .inst entries, so the GUI can update, uninstall
# and clean up a copy installed this way. It also drops setup.exe into the updater folder
# and adds a "Hebnix Setup" Start Menu shortcut so that GUI is easy to find.
#
# This runs inside the caller's session through iex, so it never calls `exit` and keeps
# everything inside Install-Hebnix to avoid leaking variables.

param(
    [ValidateSet('Hebnix', 'Lite')]
    [string]$Edition = 'Hebnix'
)

function Install-Hebnix {
    param([string]$Edition)

    $ErrorActionPreference = 'Stop'
    $ProgressPreference = 'SilentlyContinue'
    [Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12
    Add-Type -AssemblyName System.Net.Http
    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem

    $apiBaseUrl = 'https://api.hebnix.com'
    $browserUserAgent = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36'
    # HEBNIX_INSTALL_DIR is only for testing; setup.exe always uses %APPDATA%\Hebnix
    $installDirectory = if ($env:HEBNIX_INSTALL_DIR) { $env:HEBNIX_INSTALL_DIR } else { Join-Path $env:APPDATA 'Hebnix' }
    $installStatePath = Join-Path $installDirectory '.inst'
    $liteExecutableNames = @('Hebnix Lite.exe', 'Hebnite Lite.exe')

    if ($Edition -eq 'Lite') {
        $editionName = 'Hebnix Lite'
        $endpoint = '/download-hebnix-lite'
    } else {
        $editionName = 'Hebnix'
        $endpoint = '/download-hebnix'
    }

    function Invoke-Download([string]$Url, [string]$Destination) {
        # HttpClient rather than Invoke-WebRequest: PowerShell 5.1 refuses to set Referer
        $client = New-Object System.Net.Http.HttpClient
        $client.Timeout = [TimeSpan]::FromMinutes(10)
        try {
            $request = New-Object System.Net.Http.HttpRequestMessage([System.Net.Http.HttpMethod]::Get, $Url)
            $request.Headers.UserAgent.ParseAdd($browserUserAgent)
            $request.Headers.Referrer = [Uri]"$apiBaseUrl/"
            $response = $client.SendAsync($request, [System.Net.Http.HttpCompletionOption]::ResponseHeadersRead).GetAwaiter().GetResult()
            try {
                $response.EnsureSuccessStatusCode() | Out-Null
                $expectedLength = $response.Content.Headers.ContentLength
                $source = $response.Content.ReadAsStreamAsync().GetAwaiter().GetResult()
                $target = [System.IO.File]::Create($Destination)
                try { $source.CopyTo($target) } finally { $target.Dispose(); $source.Dispose() }
                # the server sometimes drops the connection mid-file without an error
                $actualLength = (Get-Item $Destination).Length
                if ($expectedLength -and $actualLength -ne $expectedLength) {
                    throw "download was cut off ($actualLength of $expectedLength bytes)"
                }
            } finally {
                $response.Dispose()
            }
        } finally {
            $client.Dispose()
        }
    }

    function Invoke-DownloadWithRetry([string]$Url, [string]$Destination) {
        $attempts = 3
        for ($attempt = 1; $attempt -le $attempts; $attempt++) {
            try {
                Invoke-Download $Url $Destination
                return
            } catch {
                if ($attempt -eq $attempts) { throw }
                Write-Host "  Download failed ($($_.Exception.Message)), retrying..." -ForegroundColor Yellow
                Start-Sleep -Seconds (3 * $attempt)
            }
        }
    }

    # mirrors MainForm.ExtractAsync, including the check that no entry escapes the folder
    function Expand-ZipSafely([string]$ZipPath, [string]$Destination) {
        New-Item -ItemType Directory -Force -Path $Destination | Out-Null
        $root = [System.IO.Path]::GetFullPath($Destination.TrimEnd('\') + '\')
        $archive = [System.IO.Compression.ZipFile]::OpenRead($ZipPath)
        try {
            foreach ($entry in $archive.Entries) {
                $target = [System.IO.Path]::GetFullPath((Join-Path $Destination $entry.FullName))
                if (-not $target.StartsWith($root, [StringComparison]::OrdinalIgnoreCase)) {
                    throw "The download contains an unsafe path: $($entry.FullName)"
                }
                if ([string]::IsNullOrEmpty($entry.Name)) {
                    New-Item -ItemType Directory -Force -Path $target | Out-Null
                } else {
                    New-Item -ItemType Directory -Force -Path (Split-Path $target) | Out-Null
                    [System.IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $target, $true)
                }
            }
        } finally {
            $archive.Dispose()
        }
    }

    function New-Shortcut([string]$ShortcutPath, [string]$TargetPath, [string]$WorkingDirectory) {
        New-Item -ItemType Directory -Force -Path (Split-Path $ShortcutPath) | Out-Null
        $shell = New-Object -ComObject WScript.Shell
        $shortcut = $shell.CreateShortcut($ShortcutPath)
        $shortcut.TargetPath = $TargetPath
        $shortcut.WorkingDirectory = $WorkingDirectory
        $shortcut.Save()
    }

    # same Edition=version format and ordering as MainForm.SaveInstalledVersions
    function Set-InstalledVersion([string]$Edition, [string]$Version) {
        $versions = @{}
        if (Test-Path $installStatePath) {
            foreach ($line in Get-Content $installStatePath) {
                $separator = $line.IndexOf('=')
                if ($separator -le 0 -or $separator -eq $line.Length - 1) { continue }
                $versions[$line.Substring(0, $separator).Trim()] = $line.Substring($separator + 1).Trim()
            }
        }
        $versions[$Edition] = $Version
        $lines = foreach ($name in @('Hebnix', 'Hebnix Lite')) {
            if ($versions[$name]) { "$name=$($versions[$name])" }
        }
        [System.IO.File]::WriteAllLines($installStatePath, [string[]]@($lines))
    }

    Write-Host "Installing $editionName" -ForegroundColor Cyan

    Write-Host '  Checking latest version...'
    $info = Invoke-RestMethod -Uri "$apiBaseUrl/info" -UserAgent 'Hebnix-Updater' -TimeoutSec 15 -UseBasicParsing
    $latestVersion = "$($info.latest_version)".Trim()
    if (-not $latestVersion) { throw 'The update API did not return a version.' }
    Write-Host "  Latest version: $latestVersion"

    New-Item -ItemType Directory -Force -Path $installDirectory | Out-Null

    foreach ($processName in @('Hebnix', 'Hebnix Lite', 'Hebnite Lite')) {
        Get-Process -Name $processName -ErrorAction SilentlyContinue | ForEach-Object {
            Write-Host "  Closing $($_.ProcessName)..."
            $_.Kill()
            $_.WaitForExit(3000) | Out-Null
        }
    }

    $temporaryZip = Join-Path ([System.IO.Path]::GetTempPath()) ("Hebnix-" + [Guid]::NewGuid().ToString('N') + '.zip')
    try {
        Write-Host '  Downloading (this can take a minute)...'
        Invoke-DownloadWithRetry "$apiBaseUrl$endpoint" $temporaryZip
        Write-Host '  Extracting...'
        Expand-ZipSafely $temporaryZip $installDirectory
    } finally {
        Remove-Item -Force -ErrorAction SilentlyContinue $temporaryZip
    }

    if ($Edition -eq 'Lite') {
        $executableName = $liteExecutableNames | Where-Object { Test-Path (Join-Path $installDirectory $_) } | Select-Object -First 1
    } else {
        $executableName = if (Test-Path (Join-Path $installDirectory 'Hebnix.exe')) { 'Hebnix.exe' }
    }
    if (-not $executableName) { throw "The download did not contain the $editionName executable." }
    $executablePath = Join-Path $installDirectory $executableName

    if ($env:HEBNIX_INSTALL_DIR) {
        # test installs keep their shortcuts out of the real Desktop and Start Menu
        $desktop = Join-Path $installDirectory '_test-shortcuts\Desktop'
        $startMenu = Join-Path $installDirectory '_test-shortcuts\Programs\Hebnix'
    } else {
        $desktop = [Environment]::GetFolderPath('DesktopDirectory')
        $startMenu = Join-Path ([Environment]::GetFolderPath('Programs')) 'Hebnix'
    }
    New-Shortcut (Join-Path $desktop "$editionName.lnk") $executablePath $installDirectory
    New-Shortcut (Join-Path $startMenu "$editionName.lnk") $executablePath $installDirectory

    Set-InstalledVersion $editionName $latestVersion

    # put the GUI where it would copy itself (MainForm.CopySetupToInstallDirectory) so
    # uninstall, updates and cleanup work the same as after a normal install
    $setupPath = Join-Path $installDirectory 'updater\setup.exe'
    $setupZip = Join-Path ([System.IO.Path]::GetTempPath()) ("Hebnix-Setup-" + [Guid]::NewGuid().ToString('N') + '.zip')
    $setupStaging = Join-Path ([System.IO.Path]::GetTempPath()) ("Hebnix-Setup-" + [Guid]::NewGuid().ToString('N'))
    try {
        Write-Host '  Installing Hebnix Setup (for uninstall and cleanup)...'
        Invoke-DownloadWithRetry "$apiBaseUrl/download-setup" $setupZip
        Expand-ZipSafely $setupZip $setupStaging
        $downloadedSetup = Get-ChildItem -Path $setupStaging -Filter 'setup.exe' -Recurse | Select-Object -First 1
        if (-not $downloadedSetup) { throw 'setup.exe was not found in the download.' }
        New-Item -ItemType Directory -Force -Path (Split-Path $setupPath) | Out-Null
        Copy-Item -Force $downloadedSetup.FullName $setupPath
        New-Shortcut (Join-Path $startMenu 'Hebnix Setup.lnk') $setupPath (Split-Path $setupPath)
        $setupInstalled = $true
    } catch {
        Write-Host "  Warning: could not install Hebnix Setup: $($_.Exception.Message)" -ForegroundColor Yellow
        $setupInstalled = $false
    } finally {
        Remove-Item -Force -ErrorAction SilentlyContinue $setupZip
        Remove-Item -Recurse -Force -ErrorAction SilentlyContinue $setupStaging
    }

    Write-Host ''
    Write-Host "$editionName $latestVersion installed to $installDirectory" -ForegroundColor Green
    Write-Host "  Launch it from the Desktop or Start Menu shortcut '$editionName'."
    if ($setupInstalled) {
        Write-Host "  To update, uninstall or clean up: Start Menu > Hebnix > Hebnix Setup"
    } else {
        Write-Host "  To uninstall later, download setup from https://hebnix.com/download"
    }
}

Install-Hebnix -Edition $Edition
