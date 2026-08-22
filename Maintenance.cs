using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;

namespace Hebnix_Updater
{
    internal sealed class UninstallOptions
    {
        public bool KeepPlugins { get; set; }
        public bool KeepThemes { get; set; }
        public bool KeepConfiguration { get; set; }
        public bool KeepTap { get; set; }
        public bool KeepPatches { get; set; }
        public bool KeepMaps { get; set; }
    }

    internal static class Maintenance
    {
        private const string Marker = "# hebnix spoofer";
        private static readonly string[] MapFiles = { "Labs_Utopia_P.upk", "Labs_Underpass_P.upk", "Labs_Octagon_B2B_02_P.upk", "Labs_PillarGlass_P.upk", "Labs_Hourglass_P.upk" };

        public static string InstallDirectory { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Hebnix"); } }

        public static void RestoreMaps()
        {
            foreach (var cooked in CookedDirectories())
            {
                var mods = Path.Combine(cooked, "mods");
                foreach (var file in MapFiles)
                {
                    DeleteFile(Path.Combine(mods, file));
                }
                DeleteFile(Path.Combine(mods, "workshop_maps.json"));
            }
            DeleteFile(Path.Combine(InstallDirectory, "plugins", "runtime", "workshop_map_loader", "active_maps.json"));
        }

        public static void RestoreSwaps()
        {
            foreach (var cooked in CookedDirectories())
            {
                var backups = Path.Combine(cooked, "Backups");
                var manifest = Path.Combine(backups, "swapper_swaps.json");
                if (!File.Exists(manifest))
                {
                    continue;
                }

                var entries = JArray.Parse(File.ReadAllText(manifest));
                foreach (var entry in entries.OfType<JObject>())
                {
                    RestoreBackup(cooked, backups, entry.Value<string>("target_upk"));
                    RestoreBackup(cooked, backups, entry.Value<string>("target_bnk"));
                    RestoreBackup(cooked, backups, entry.Value<string>("target_thumbnail"));
                }
                DeleteFile(manifest);
            }
        }

        public static void RestorePatches()
        {
            foreach (var cooked in CookedDirectories())
            {
                var backups = Path.Combine(cooked, "Backups");
                if (!Directory.Exists(backups))
                {
                    continue;
                }

                foreach (var backup in Directory.GetFiles(backups, "*.bak", SearchOption.TopDirectoryOnly))
                {
                    var targetName = Path.GetFileNameWithoutExtension(backup);
                    if (string.IsNullOrWhiteSpace(targetName))
                    {
                        continue;
                    }
                    File.Copy(backup, Path.Combine(cooked, targetName), true);
                    DeleteFile(backup);
                }
            }
        }

        public static void RemoveMultiplayerTap()
        {
            ClearEpicMultihome();
            RunHidden("powershell.exe", "-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command \"$device = Get-PnpDevice -Class Net -ErrorAction SilentlyContinue | Where-Object { $_.FriendlyName -eq 'Hebnix TAP' } | Select-Object -First 1; if ($null -ne $device) { & pnputil.exe /remove-device $device.InstanceId | Out-Null }\"");
        }

        public static void ClearEpicMultihome()
        {
            var configRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EpicGamesLauncher", "Saved", "Config");
            var roots = Directory.Exists(configRoot)
                ? Directory.GetFiles(configRoot, "GameUserSettings.ini", SearchOption.AllDirectories)
                : new string[0];
            foreach (var path in roots)
            {
                var lines = File.ReadAllLines(path).ToList();
                var changed = false;
                for (var i = 0; i < lines.Count; i++)
                {
                    if (lines[i].IndexOf(":Sugar_AdditionalCommands=", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        var equals = lines[i].IndexOf('=');
                        var prefix = lines[i].Substring(0, equals + 1);
                        var retained = lines[i].Substring(equals + 1).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Where(value => !value.StartsWith("-multihome=", StringComparison.OrdinalIgnoreCase)).ToArray();
                        var updated = prefix + string.Join(" ", retained);
                        changed |= !string.Equals(lines[i], updated, StringComparison.Ordinal);
                        lines[i] = updated;
                    }
                    else if (lines[i].IndexOf(":Sugar_AdditionalCommandsEnabled=", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        var equals = lines[i].IndexOf('=');
                        if (equals >= 0)
                        {
                            var updated = lines[i].Substring(0, equals + 1) + "False";
                            changed |= !string.Equals(lines[i], updated, StringComparison.Ordinal);
                            lines[i] = updated;
                        }
                    }
                }
                if (changed)
                {
                    File.WriteAllLines(path, lines);
                }
            }
        }

        public static void RemoveSpoofer()
        {
            var hosts = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "drivers", "etc", "hosts");
            if (File.Exists(hosts))
            {
                File.WriteAllLines(hosts, File.ReadAllLines(hosts).Where(line => line.IndexOf(Marker, StringComparison.OrdinalIgnoreCase) < 0));
            }
            RunHidden("netsh.exe", "winhttp reset proxy");
            RunHidden("reg.exe", "add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings\" /v ProxyEnable /t REG_DWORD /d 0 /f");
            RunHidden("reg.exe", "delete \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings\" /v ProxyServer /f");
            RunHidden("ipconfig.exe", "/flushdns");
        }

        public static void RemoveInstallData(UninstallOptions options)
        {
            ClearEpicMultihome();
            if (!options.KeepPatches)
            {
                RestoreSwaps();
                RestorePatches();
            }
            if (!options.KeepMaps)
            {
                RestoreMaps();
            }
var preserved = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (options.KeepPlugins) preserved.Add("plugins");
            if (options.KeepThemes) preserved.Add("themes");
            if (options.KeepConfiguration) preserved.Add("config.toml");
            if (!Directory.Exists(InstallDirectory))
            {
                return;
            }
            foreach (var path in Directory.GetFileSystemEntries(InstallDirectory))
            {
                var name = Path.GetFileName(path);
                if (preserved.Contains(name) || Application.ExecutablePath.StartsWith(Path.GetFullPath(path) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                if (Directory.Exists(path)) Directory.Delete(path, true); else File.Delete(path);
            }
        }

        public static void ScheduleRemainingRemoval(UninstallOptions options)
        {
            if (!Directory.Exists(InstallDirectory)) return;
            var keep = new List<string>();
            if (options.KeepPlugins) keep.Add("plugins");
            if (options.KeepThemes) keep.Add("themes");
            if (options.KeepConfiguration) keep.Add("config.toml");
            var quotedKeep = string.Join(",", keep.Select(value => "'" + value.Replace("'", "''") + "'"));
            var command = "$id=" + Process.GetCurrentProcess().Id + "; Wait-Process -Id $id -ErrorAction SilentlyContinue; Start-Sleep -Milliseconds 500; $keep=@(" + quotedKeep + "); Get-ChildItem -LiteralPath '" + InstallDirectory.Replace("'", "''") + "' -Force | Where-Object { $keep -notcontains $_.Name } | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue";
            Process.Start(new ProcessStartInfo("powershell.exe", "-NoProfile -NonInteractive -WindowStyle Hidden -Command \"" + command.Replace("\"", "\\\"") + "\"") { CreateNoWindow = true, UseShellExecute = false, WindowStyle = ProcessWindowStyle.Hidden });
        }
        private static IEnumerable<string> CookedDirectories()
        {
            var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            AddRoot(roots, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Epic Games", "rocketleague"));
            AddRoot(roots, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam", "steamapps", "common", "rocketleague"));
            AddRoot(roots, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Steam", "steamapps", "common", "rocketleague"));
            var config = Path.Combine(InstallDirectory, "config.toml");
            if (File.Exists(config))
            {
                foreach (Match match in Regex.Matches(File.ReadAllText(config), "(?im)^\\s*rl_path\\s*=\\s*\\\"([^\\\"]+)\\\""))
                {
                    AddRoot(roots, match.Groups[1].Value);
                }
            }
            return roots.Select(root => Path.Combine(root, "TAGame", "CookedPCConsole")).Where(Directory.Exists).ToArray();
        }

        private static void AddRoot(ISet<string> roots, string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            var candidate = path.Trim().Trim('"');
            if (File.Exists(candidate)) candidate = Path.GetDirectoryName(candidate);
            while (!string.IsNullOrWhiteSpace(candidate) && !Directory.Exists(Path.Combine(candidate, "TAGame", "CookedPCConsole")))
            {
                candidate = Directory.GetParent(candidate) == null ? null : Directory.GetParent(candidate).FullName;
            }
            if (!string.IsNullOrWhiteSpace(candidate)) roots.Add(candidate);
        }

        private static void RestoreBackup(string cooked, string backups, string name)
        {
            if (string.IsNullOrWhiteSpace(name) || !string.Equals(name, Path.GetFileName(name), StringComparison.Ordinal)) return;
            var backup = Path.Combine(backups, name + ".bak");
            if (!File.Exists(backup)) return;
            File.Copy(backup, Path.Combine(cooked, name), true);
            DeleteFile(backup);
        }

        private static void DeleteFile(string path)
        {
            if (File.Exists(path)) File.Delete(path);
        }

        private static void RunHidden(string fileName, string arguments)
        {
            using (var process = Process.Start(new ProcessStartInfo(fileName, arguments) { CreateNoWindow = true, UseShellExecute = false, WindowStyle = ProcessWindowStyle.Hidden }))
            {
                if (process != null) process.WaitForExit(10000);
            }
        }
    }
}






