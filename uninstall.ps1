# Hebnix one-line uninstaller
#
#   irm https://raw.githubusercontent.com/Hebbins/Hebnix-Setup/main/uninstall.ps1 | iex
#
# Removes Hebnix and Hebnix Lite from %APPDATA%\Hebnix, whether they were installed with
# setup.exe or install.ps1. Run it from inside a portable folder (one with portable.txt)
# to remove that copy instead.
#
# Does what Uninstall in setup.exe does (MainForm.UninstallAsync, Maintenance.RemoveInstallData):
# restores patched game files, removes workshop maps, the spoofer's hosts lines and proxy, the
# multiplayer TAP adapter and the Epic -multihome argument. It also undoes changes Hebnix makes
# that setup.exe leaves behind: the spoofer's CA certificate and recovery tasks, the multiplayer
# service and firewall rules, the start-with-Windows entry and the hebnix:// link handler.
#
# Steps that need admin run together behind one UAC prompt, and only when there is something
# for them to do. Declining it cancels the uninstall before anything is removed.
#
# This runs inside the caller's session through iex, so it never calls `exit` and keeps
# everything inside Uninstall-Hebnix to avoid leaking variables.

function Uninstall-Hebnix {
    $ErrorActionPreference = 'Stop'
    $ProgressPreference = 'SilentlyContinue'

    # HEBNIX_INSTALL_DIR is only for testing: it points at a scratch install, and everything
    # outside it (system settings, the default game folders) is left alone.
    # HEBNIX_UNINSTALL_DRYRUN=1 lists what would be removed without changing anything
    $testInstall = [bool]$env:HEBNIX_INSTALL_DIR
    $dryRun = $env:HEBNIX_UNINSTALL_DRYRUN -eq '1'
    $warnings = New-Object System.Collections.Generic.List[string]
    $interactive = -not [Console]::IsInputRedirected

    function Invoke-Step([string]$What, [scriptblock]$Action) {
        if ($dryRun) {
            Write-Host "  [dry run] would $What" -ForegroundColor DarkGray
            return
        }
        try { & $Action } catch { $warnings.Add("Could not $What`: $($_.Exception.Message)") }
    }

    function Test-Inside([string]$Path) {
        $Path -and $Path.Trim('"').StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)
    }

    # --- which copy -------------------------------------------------------------------------

    $location = Get-Location
    $here = if ($location.Provider.Name -eq 'FileSystem') { $location.ProviderPath } else { $null }
    $portable = [bool]($here -and (Test-Path -LiteralPath (Join-Path $here 'portable.txt')))
    if ($portable) {
        $installDirectory = $here
    } elseif ($testInstall) {
        $installDirectory = $env:HEBNIX_INSTALL_DIR
    } else {
        $installDirectory = Join-Path $env:APPDATA 'Hebnix'
    }
    $installDirectory = [System.IO.Path]::GetFullPath($installDirectory).TrimEnd('\')
    $prefix = $installDirectory + '\'

    # same folders install.ps1 refuses; a stray portable.txt must not make us empty them
    if ($portable) {
        $unsafe = @($env:USERPROFILE, [Environment]::GetFolderPath('DesktopDirectory'), [System.IO.Path]::GetPathRoot($installDirectory)) |
            Where-Object { $_ -and $installDirectory -ieq $_.TrimEnd('\') }
        foreach ($systemFolder in @($env:windir, $env:ProgramFiles, ${env:ProgramFiles(x86)})) {
            if ($systemFolder -and $prefix.StartsWith($systemFolder.TrimEnd('\') + '\', [StringComparison]::OrdinalIgnoreCase)) { $unsafe = $true }
        }
        if ($unsafe) { throw "Not uninstalling from $installDirectory, it is not a Hebnix folder." }
    }

    $installed = Test-Path -LiteralPath $installDirectory
    $label = if ($portable) { "Hebnix (portable) in $installDirectory" } else { 'Hebnix and Hebnix Lite' }
    if ($installed) {
        Write-Host "Uninstalling $label" -ForegroundColor Cyan
    } else {
        Write-Host "Hebnix is not installed in $installDirectory, cleaning up what it left on the system" -ForegroundColor Cyan
    }

    $keepData = $false
    if ($interactive -and -not $dryRun) {
        Write-Host '  This closes Hebnix and Rocket League, restores the game files Hebnix changed and'
        Write-Host '  undoes its spoofer and multiplayer settings.'
        $answer = Read-Host '  Continue? [y/N]'
        if ($answer -notmatch '^(y|yes)$') { Write-Host '  Cancelled, nothing was changed.'; return }
        if ($installed) {
            $answer = Read-Host '  Keep your plugins, themes and settings (config.toml) in case you reinstall? [y/N]'
            $keepData = $answer -match '^(y|yes)$'
        }
    }

    # --- what is there ----------------------------------------------------------------------

    $hostsPath = Join-Path $env:SystemRoot 'System32\drivers\etc\hosts'
    # the same lines hosts_file.rs and the recovery task treat as Hebnix's
    $hostsPattern = '(?im)^[\t ]*127\.0\.0\.1[\t ]+[^#\r\n]+#[\t ]*hebnix(?: spoofer)?[\t ]*(?:\r?\n|$)'
    $serviceName = 'HebnixTailscale'
    $nrptKey = 'HKLM:\SOFTWARE\Policies\Microsoft\Windows NT\DNSClient\DnsPolicyConfig\{5abe529b-675b-4486-8459-25a634dacc23}'

    $processes = @(Get-Process -ErrorAction SilentlyContinue | Where-Object {
        if (Test-Inside $_.Path) { return $true }
        # a Hebnix running as admin hides its path from us, so match an installed copy by name
        (-not $portable) -and (-not $testInstall) -and -not $_.Path -and @('Hebnix', 'Hebnix Lite', 'Hebnite Lite') -contains $_.ProcessName
    })

    $hostsLines = 0
    if (-not $testInstall -and (Test-Path -LiteralPath $hostsPath)) {
        try { $hostsLines = [regex]::Matches([System.IO.File]::ReadAllText($hostsPath), $hostsPattern).Count } catch { }
    }
    $machine = [ordered]@{}
    if (-not $testInstall) {
        $machine['hosts'] = $hostsLines -gt 0
        $machine['service'] = [bool](Get-Service -Name $serviceName -ErrorAction SilentlyContinue)
        $machine['firewall'] = [bool](Get-NetFirewallRule -DisplayName 'Hebnix Workshop LAN*' -ErrorAction SilentlyContinue | Select-Object -First 1)
        $machine['tap'] = [bool](Get-PnpDevice -Class Net -ErrorAction SilentlyContinue | Where-Object { $_.FriendlyName -eq 'Hebnix TAP' } | Select-Object -First 1)
        # the recovery tasks run as SYSTEM, which a normal user may not be able to list; the
        # registry key is written alongside them, so either one means they need removing
        $tasks = @(schtasks.exe /Query /FO CSV /NH 2>$null | ForEach-Object { ($_ -split '","')[0].Trim('"').TrimStart('\') } |
            Where-Object { $_ -eq 'Hebnix Epic Connection Recovery' -or $_ -like 'Hebnix Epic Cache Recovery *' })
        $machine['tasks'] = $tasks.Count -gt 0 -or (Test-Path 'HKLM:\SOFTWARE\Hebnix')
        $machine['nrpt'] = Test-Path $nrptKey
    }
    $isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

    # --- close what is running --------------------------------------------------------------

    # restored game files are locked while the game runs. A test install leaves the real game alone
    $rocketLeague = if ($testInstall) { @() } else { @(Get-Process -Name 'RocketLeague' -ErrorAction SilentlyContinue) }
    if ($rocketLeague) {
        Invoke-Step 'close Rocket League' {
            Write-Host '  Closing Rocket League...'
            $rocketLeague | ForEach-Object { $_.Kill(); $_.WaitForExit(10000) | Out-Null }
        }
    }
    $stubborn = New-Object System.Collections.Generic.List[int]
    foreach ($process in $processes) {
        if ($dryRun) { Write-Host "  [dry run] would close $($process.ProcessName) ($($process.Id))" -ForegroundColor DarkGray; continue }
        try {
            Write-Host "  Closing $($process.ProcessName)..."
            $process.Kill()
            $process.WaitForExit(3000) | Out-Null
        } catch {
            # running as admin, the elevated step closes it
            $stubborn.Add($process.Id)
        }
    }

    # --- admin steps, one UAC prompt --------------------------------------------------------

    $needsAdmin = $stubborn.Count -gt 0 -or @($machine.Values | Where-Object { $_ }).Count -gt 0
    if ($needsAdmin -and $dryRun) {
        $found = @($machine.Keys | Where-Object { $machine[$_] })
        if ($stubborn.Count) { $found += 'processes running as admin' }
        Write-Host "  [dry run] would ask for admin to remove: $($found -join ', ')" -ForegroundColor DarkGray
    } elseif ($needsAdmin) {
        # self-contained: it runs in a separate elevated PowerShell
        $elevated = @'
$ErrorActionPreference = 'Continue'
$failures = @()
foreach ($id in $cfg.Pids) { try { Stop-Process -Id $id -Force -ErrorAction Stop } catch { } }
if (Get-Service -Name $cfg.Service -ErrorAction SilentlyContinue) {
    sc.exe stop $cfg.Service | Out-Null
    for ($i = 0; $i -lt 20 -and (Get-Service -Name $cfg.Service).Status -ne 'Stopped'; $i++) { Start-Sleep -Milliseconds 500 }
    sc.exe delete $cfg.Service | Out-Null
    if ($LASTEXITCODE -ne 0) { $failures += 'remove the multiplayer service' }
}
foreach ($task in @('Hebnix Epic Connection Recovery') + @(Get-ScheduledTask -ErrorAction SilentlyContinue | Where-Object { $_.TaskName -like 'Hebnix Epic Cache Recovery *' } | ForEach-Object { $_.TaskName })) {
    schtasks.exe /Query /TN $task 2>$null | Out-Null
    if ($LASTEXITCODE -eq 0) {
        schtasks.exe /Delete /TN $task /F 2>$null | Out-Null
        if ($LASTEXITCODE -ne 0) { $failures += "remove the scheduled task $task" }
    }
}
Remove-Item -LiteralPath 'HKLM:\SOFTWARE\Hebnix' -Recurse -Force -ErrorAction SilentlyContinue
try { Get-NetFirewallRule -DisplayName 'Hebnix Workshop LAN*' -ErrorAction SilentlyContinue | Remove-NetFirewallRule -ErrorAction Stop } catch { $failures += 'remove the multiplayer firewall rules' }
Get-PnpDevice -Class Net -ErrorAction SilentlyContinue | Where-Object { $_.FriendlyName -eq 'Hebnix TAP' } | ForEach-Object {
    pnputil.exe /remove-device $_.InstanceId | Out-Null
    if ($LASTEXITCODE -ne 0) { $failures += 'remove the Hebnix TAP adapter' }
}
if (Test-Path -LiteralPath $cfg.Hosts) {
    # the same lock Hebnix and its recovery task take before editing the hosts file
    $mutex = New-Object System.Threading.Mutex($false, 'Global\HebnixHostsCleanup')
    try {
        try { $null = $mutex.WaitOne(30000) } catch [System.Threading.AbandonedMutexException] { }
        $item = Get-Item -LiteralPath $cfg.Hosts -Force
        $text = [IO.File]::ReadAllText($cfg.Hosts)
        $clean = [regex]::Replace($text, $cfg.HostsPattern, '')
        if ($clean -ne $text) {
            $readOnly = $item.IsReadOnly
            if ($readOnly) { $item.IsReadOnly = $false }
            [IO.File]::WriteAllText($cfg.Hosts, $clean)
            if ($readOnly) { $item.IsReadOnly = $true }
        }
    } catch { $failures += "clean the hosts file: $($_.Exception.Message)" } finally { try { $mutex.ReleaseMutex() } catch { }; $mutex.Dispose() }
}
# a Tailscale DNS rule left by a crashed multiplayer session breaks DNS for every app. Leave it
# if something answers at its address, that is a working Tailscale of the user's own
if (Test-Path $cfg.Nrpt) {
    $client = New-Object Net.Sockets.TcpClient
    $answers = $false
    try { $answers = $client.ConnectAsync('100.100.100.100', 53).Wait(300) -and $client.Connected } catch { } finally { $client.Dispose() }
    if (-not $answers) { Remove-Item -LiteralPath $cfg.Nrpt -Recurse -Force -ErrorAction SilentlyContinue }
}
ipconfig.exe /flushdns | Out-Null
[IO.File]::WriteAllText($cfg.Result, (ConvertTo-Json @($failures)))
'@
        $resultPath = Join-Path ([System.IO.Path]::GetTempPath()) ("Hebnix-Uninstall-" + [Guid]::NewGuid().ToString('N') + '.json')
        $config = @{ Pids = @($stubborn); Service = $serviceName; Hosts = $hostsPath; HostsPattern = $hostsPattern; Nrpt = $nrptKey; Result = $resultPath } | ConvertTo-Json -Compress
        $script = "`$cfg = '" + $config.Replace("'", "''") + "' | ConvertFrom-Json`n" + $elevated
        Write-Host '  Removing system changes (needs admin)...'
        try {
            if ($isAdmin) {
                & ([scriptblock]::Create($script))
            } else {
                $encoded = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($script))
                $process = Start-Process powershell.exe -Verb RunAs -WindowStyle Hidden -Wait -PassThru `
                    -ArgumentList "-NoProfile -ExecutionPolicy Bypass -EncodedCommand $encoded"
                if (-not $process) { throw 'no process' }
            }
        } catch {
            Write-Host '  Administrator permission was declined, so the uninstall was cancelled. Nothing was removed.' -ForegroundColor Yellow
            return
        }
        if (Test-Path -LiteralPath $resultPath) {
            foreach ($failure in @(Get-Content -LiteralPath $resultPath -Raw | ConvertFrom-Json)) { if ($failure) { $warnings.Add("Could not $failure") } }
            Remove-Item -LiteralPath $resultPath -Force -ErrorAction SilentlyContinue
        } else {
            $warnings.Add('The admin cleanup did not finish, run the uninstaller again')
        }
    }

    # --- spoofer and launch settings (this user) --------------------------------------------

    if (-not $testInstall) {
        $internetSettings = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Internet Settings'
        $proxy = Get-ItemProperty -LiteralPath $internetSettings -ErrorAction SilentlyContinue
        # only a proxy that is still Hebnix's own; leave one the user set up alone
        if ($proxy -and $proxy.ProxyEnable -and $proxy.ProxyServer -eq '127.0.0.1:8080') {
            Invoke-Step 'turn off the spoofer proxy' {
                $backupPath = Join-Path $installDirectory 'spoofer\proxy_backup.json'
                $backup = if (Test-Path -LiteralPath $backupPath) { Get-Content -LiteralPath $backupPath -Raw | ConvertFrom-Json }
                # put back what Hebnix saved before it switched the proxy on (spoofer/mod.rs ProxyBackup)
                $previous = @{ ProxyServer = $backup.proxy_server; ProxyOverride = $backup.proxy_override }
                Set-ItemProperty -LiteralPath $internetSettings -Name ProxyEnable -Value ([int]$backup.proxy_enable) -Type DWord
                foreach ($name in $previous.Keys) {
                    if ($previous[$name]) { Set-ItemProperty -LiteralPath $internetSettings -Name $name -Value $previous[$name] }
                    else { Remove-ItemProperty -LiteralPath $internetSettings -Name $name -ErrorAction SilentlyContinue }
                }
                # tell running apps the setting changed, like Hebnix does after restoring it
                if (-not ('HebnixUninstall.WinInet' -as [type])) {
                    Add-Type -Namespace HebnixUninstall -Name WinInet -MemberDefinition '[DllImport("wininet.dll")] public static extern bool InternetSetOption(System.IntPtr h, int option, System.IntPtr buffer, int length);'
                }
                [HebnixUninstall.WinInet]::InternetSetOption([IntPtr]::Zero, 39, [IntPtr]::Zero, 0) | Out-Null
                [HebnixUninstall.WinInet]::InternetSetOption([IntPtr]::Zero, 37, [IntPtr]::Zero, 0) | Out-Null
            }
        }

        if (Get-ChildItem Cert:\CurrentUser\Root -ErrorAction SilentlyContinue | Where-Object { $_.Subject -eq 'CN=Hebnix Local CA' }) {
            Invoke-Step 'remove the spoofer certificate' {
                Write-Host '  Removing the spoofer certificate (Windows asks you to confirm this)...'
                certutil.exe -user -delstore root 'Hebnix Local CA' | Out-Null
                if (Get-ChildItem Cert:\CurrentUser\Root | Where-Object { $_.Subject -eq 'CN=Hebnix Local CA' }) { throw 'it is still installed (was the Windows prompt declined?)' }
            }
        }

        $run = Get-ItemProperty -LiteralPath 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run' -ErrorAction SilentlyContinue
        foreach ($name in @('Hebnix', 'Hebnix Lite')) {
            if ($run -and (Test-Inside $run.$name)) {
                Invoke-Step "remove $name from startup" { Remove-ItemProperty -LiteralPath 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run' -Name $name }
            }
        }
        $protocolCommand = (Get-ItemProperty -LiteralPath 'HKCU:\Software\Classes\hebnix\shell\open\command' -ErrorAction SilentlyContinue).'(default)'
        if (Test-Inside $protocolCommand) {
            Invoke-Step 'remove the hebnix:// link handler' { Remove-Item -LiteralPath 'HKCU:\Software\Classes\hebnix' -Recurse -Force }
        }

        # multiplayer adds -multihome to Rocket League's launch arguments in Epic (Maintenance.ClearEpicMultihome).
        # Only the -multihome value is removed; the user's own arguments and their toggle stay
        $epicConfig = Join-Path $env:LOCALAPPDATA 'EpicGamesLauncher\Saved\Config'
        if (Test-Path -LiteralPath $epicConfig) {
            foreach ($ini in Get-ChildItem -LiteralPath $epicConfig -Filter 'GameUserSettings.ini' -Recurse -File -ErrorAction SilentlyContinue) {
                $lines = [System.IO.File]::ReadAllLines($ini.FullName)
                $changed = $false
                $emptied = $false
                for ($i = 0; $i -lt $lines.Length; $i++) {
                    if ($lines[$i] -match '^(.*:Sugar_AdditionalCommands=)(.*)$') {
                        $arguments = @($Matches[2] -split ' ' | Where-Object { $_ })
                        $kept = @($arguments | Where-Object { $_ -notlike '-multihome=*' })
                        if ($kept.Count -ne $arguments.Count) {
                            $lines[$i] = $Matches[1] + ($kept -join ' ')
                            $changed = $true
                            $emptied = $kept.Count -eq 0
                        }
                    }
                }
                if ($emptied) {
                    for ($i = 0; $i -lt $lines.Length; $i++) {
                        if ($lines[$i] -match '^(.*:Sugar_AdditionalCommandsEnabled=)') { $lines[$i] = $Matches[1] + 'False' }
                    }
                }
                if ($changed) {
                    Invoke-Step "remove -multihome from Epic's Rocket League launch arguments" { [System.IO.File]::WriteAllLines($ini.FullName, $lines) }
                }
            }
        }

        # the spoofer leaves a marker while Rocket League may have cached its redirected pages
        if (Test-Path -LiteralPath (Join-Path $installDirectory 'state\proxy_session.dirty')) {
            $webCache = Join-Path ([Environment]::GetFolderPath('MyDocuments')) 'My Games\Rocket League\TAGame\Cache\WebCache'
            if (Test-Path -LiteralPath $webCache) {
                Invoke-Step "clear Rocket League's web cache" { Remove-Item -LiteralPath $webCache -Recurse -Force }
            }
        }
    }

    # --- game files (Maintenance.RestoreSwaps, RestorePatches, RestoreMaps) -----------------

    $roots = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
    function Add-GameRoot([string]$Path) {
        if (-not $Path) { return }
        $candidate = $Path.Trim().Trim('"', "'")
        if (Test-Path -LiteralPath $candidate -PathType Leaf) { $candidate = Split-Path $candidate }
        while ($candidate -and -not (Test-Path -LiteralPath (Join-Path $candidate 'TAGame\CookedPCConsole'))) {
            $candidate = Split-Path $candidate
        }
        if ($candidate) { [void]$roots.Add([System.IO.Path]::GetFullPath($candidate)) }
    }
    if (-not $testInstall) {
        Add-GameRoot (Join-Path $env:ProgramFiles 'Epic Games\rocketleague')
        Add-GameRoot (Join-Path ${env:ProgramFiles(x86)} 'Steam\steamapps\common\rocketleague')
        Add-GameRoot (Join-Path $env:ProgramFiles 'Steam\steamapps\common\rocketleague')
        # Epic installs to any folder, and its own list knows where
        $launcherInstalled = Join-Path $env:ProgramData 'Epic\UnrealEngineLauncher\LauncherInstalled.dat'
        if (Test-Path -LiteralPath $launcherInstalled) {
            try {
                (Get-Content -LiteralPath $launcherInstalled -Raw | ConvertFrom-Json).InstallationList |
                    Where-Object { $_.AppName -eq 'Sugar' } | ForEach-Object { Add-GameRoot $_.InstallLocation }
            } catch { }
        }
    }
    $configPath = Join-Path $installDirectory 'config.toml'
    if (Test-Path -LiteralPath $configPath) {
        # Hebnix writes TOML literal strings ('...'), older versions basic strings ("...")
        foreach ($match in [regex]::Matches((Get-Content -LiteralPath $configPath -Raw), '(?im)^\s*rl_path\s*=\s*(?:''([^'']+)''|"([^"]+)")')) {
            Add-GameRoot ($match.Groups[1].Value + $match.Groups[2].Value.Replace('\\', '\'))
        }
    }

    $mapFiles = @('Labs_Utopia_P.upk', 'Labs_Underpass_P.upk', 'Labs_Octagon_B2B_02_P.upk', 'Labs_PillarGlass_P.upk', 'Labs_Hourglass_P.upk')
    foreach ($root in $roots) {
        $cooked = Join-Path $root 'TAGame\CookedPCConsole'
        $backups = Join-Path $cooked 'Backups'
        $restored = 0
        $swapManifest = Join-Path $backups 'swapper_swaps.json'
        $names = @()
        if (Test-Path -LiteralPath $swapManifest) {
            try {
                foreach ($entry in @(Get-Content -LiteralPath $swapManifest -Raw | ConvertFrom-Json)) {
                    $names += @($entry.target_upk, $entry.target_bnk, $entry.target_thumbnail)
                }
            } catch { $warnings.Add("Could not read $swapManifest`: $($_.Exception.Message)") }
        }
        if (Test-Path -LiteralPath $backups) {
            $names += @(Get-ChildItem -LiteralPath $backups -Filter '*.bak' -File | ForEach-Object { $_.BaseName })
        }
        foreach ($name in ($names | Where-Object { $_ -and $_ -eq [System.IO.Path]::GetFileName($_) } | Select-Object -Unique)) {
            $backup = Join-Path $backups "$name.bak"
            if (-not (Test-Path -LiteralPath $backup)) { continue }
            Invoke-Step "restore $name" {
                Copy-Item -LiteralPath $backup -Destination (Join-Path $cooked $name) -Force
                Remove-Item -LiteralPath $backup -Force
            }
            $restored++
        }
        if (Test-Path -LiteralPath $swapManifest) { Invoke-Step 'remove the swap list' { Remove-Item -LiteralPath $swapManifest -Force } }
        $mods = Join-Path $cooked 'mods'
        $removedMaps = 0
        foreach ($file in $mapFiles + 'workshop_maps.json') {
            $path = Join-Path $mods $file
            if (Test-Path -LiteralPath $path) {
                Invoke-Step "remove the workshop map $file" { Remove-Item -LiteralPath $path -Force }
                if ($file -ne 'workshop_maps.json') { $removedMaps++ }
            }
        }
        if ($restored -or $removedMaps) {
            Write-Host "  Restored $restored game file(s) and removed $removedMaps workshop map(s) in $root"
        }
    }

    # --- shortcuts ----------------------------------------------------------------------------

    if (-not $testInstall) {
        $shell = New-Object -ComObject WScript.Shell
        $startMenu = Join-Path ([Environment]::GetFolderPath('Programs')) 'Hebnix'
        $shortcuts = @(Get-ChildItem -LiteralPath ([Environment]::GetFolderPath('DesktopDirectory')) -Filter '*.lnk' -File -ErrorAction SilentlyContinue)
        if (Test-Path -LiteralPath $startMenu) { $shortcuts += @(Get-ChildItem -LiteralPath $startMenu -Filter '*.lnk' -File) }
        foreach ($shortcut in $shortcuts) {
            # only Hebnix's own launch shortcuts, not ones the user made to its folders
            $target = $shell.CreateShortcut($shortcut.FullName).TargetPath
            if ((Test-Inside $target) -and $target -like '*.exe') {
                Invoke-Step "remove the shortcut $($shortcut.Name)" { Remove-Item -LiteralPath $shortcut.FullName -Force }
            }
        }
        if ((Test-Path -LiteralPath $startMenu) -and -not (Get-ChildItem -LiteralPath $startMenu -Force) -and -not $dryRun) {
            Remove-Item -LiteralPath $startMenu -Force -ErrorAction SilentlyContinue
        }
    }

    # --- the install folder -----------------------------------------------------------------

    $keep = if ($keepData) { @('plugins', 'themes', 'config.toml') } else { @() }
    $left = @()
    if ($installed) {
        $entries = @(Get-ChildItem -LiteralPath $installDirectory -Force | Where-Object { $keep -notcontains $_.Name })
        if ($portable) {
            # a portable copy may share its folder with the user's own files, so only remove
            # what Hebnix itself creates there (hebnix-sdk paths::base_dir and its callers)
            $ours = @('Hebnix.exe', 'Hebnix Lite.exe', 'Hebnite Lite.exe', 'portable.txt', 'rlapi-bridge.exe', 'steam_api64.dll',
                'assets', 'balls', 'boosts', 'cars', 'decals', 'fonts', 'locales', 'multiplayer-lan', 'pending-theme-installs',
                'plugins', 'presets', 'spoofer', 'state', 'themes', 'updater', 'webview2',
                '.appdata-migration-v1', '.first', '.inst', '.launch', 'config.ini', 'config.toml', 'config.toml.tmp', 'crash.txt',
                'chat_log.jsonl', 'discord_linked_accounts.json', 'enable_owned_replacements.pending', 'friends.json', 'hebnix.ico',
                'items.lock', 'local_maps.json', 'multiplayer_settings.json', 'owned_products.json', 'replay_upload_history.json',
                'spoofer_settings.json', 'steam_download.json', 'synthetic_items.json', 'synthetic_items.json.bak', 'theme_errors.txt',
                'background_swaps.json', 'ca-cert.pem', 'ca-key.pem', 'hebnix-ca.cer')
            $patterns = @('hebnix*.log', 'hebnix*.log.old', 'rank_spoofer_*.log')
            $left = @($entries | Where-Object {
                $name = $_.Name
                -not ($ours -contains $name -or @($patterns | Where-Object { $name -like $_ }).Count)
            })
            $entries = @($entries | Where-Object { $left -notcontains $_ })
        }
        foreach ($entry in $entries) {
            Invoke-Step "delete $($entry.FullName)" { Remove-Item -LiteralPath $entry.FullName -Recurse -Force }
        }
        if (-not $dryRun -and -not (Get-ChildItem -LiteralPath $installDirectory -Force)) {
            if ($portable) { Set-Location (Split-Path $installDirectory) }
            Remove-Item -LiteralPath $installDirectory -Force -ErrorAction SilentlyContinue
        }
    }

    # --- report -------------------------------------------------------------------------------

    Write-Host ''
    if ($dryRun) {
        Write-Host 'Dry run finished, nothing was changed.' -ForegroundColor Green
        foreach ($warning in $warnings) { Write-Host "  - $warning" -ForegroundColor Yellow }
    } elseif ($warnings.Count) {
        Write-Host 'Hebnix was uninstalled, but some things could not be undone:' -ForegroundColor Yellow
        foreach ($warning in $warnings) { Write-Host "  - $warning" -ForegroundColor Yellow }
        Write-Host '  Run the uninstaller again, as admin if needed.'
    } else {
        Write-Host "$label uninstalled." -ForegroundColor Green
    }
    if ($keepData) { Write-Host "  Kept plugins, themes and config.toml in $installDirectory" }
    if ($left) {
        Write-Host "  Left these in $installDirectory because they may not be Hebnix's:"
        foreach ($entry in $left) { Write-Host "    $($entry.Name)" }
    }
    Write-Host '  If anything in Rocket League still looks modded, verify the game files in Epic or Steam.'
}

Uninstall-Hebnix
