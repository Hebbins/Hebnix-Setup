using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace Hebnix_Updater
{
    public partial class MainForm : Form
    {
        private const string ApiBaseUrl = "https://api.hebnix.com";
        private const string FullEdition = "Hebnix";
        private const string LiteEdition = "Hebnix Lite";
        private static readonly string[] LiteExecutableNames = { "Hebnix Lite.exe", "Hebnite Lite.exe" };
        // the version check is on the startup path so it gets a short timeout, downloads
        // get a long one because the lite build can take over a minute to start sending
        private static readonly HttpClient VersionClient = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        private static readonly HttpClient HttpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
        private readonly string installDirectory;
        private readonly string installStatePath;
        private Dictionary<string, string> installedVersions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private string latestVersion = string.Empty;
        private string discordInvite = string.Empty;
        private bool hebnixInstalled;
        private bool liteInstalled;
        private string liteExecutableName;
        private string startupError;
        private Button launchCleanup;

        public MainForm()
        {
            InitializeComponent();
            installDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Hebnix");
            installStatePath = Path.Combine(installDirectory, ".inst");
            launchCleanup = Theme.Button("Cleanup", new Point(742, 436), new Size(117, 35));
            launchCleanup.Click += LaunchCleanup_Click;
            Controls.Add(launchCleanup);
        }

        public void PrepareBeforeShowing()
        {
            try
            {
                Directory.CreateDirectory(installDirectory);
                if (!File.Exists(installStatePath))
                {
                    File.WriteAllText(installStatePath, string.Empty);
                }

                CopySetupToInstallDirectory();
                CloseProcess("Hebnix");
                installedVersions = ReadInstalledVersions();
                ScanInstallation();
                ApplyInstallationState();
            }
            catch (Exception exception)
            {
                startupError = exception.ToString();
                ScanInstallation();
                ApplyInstallationState();
            }
        }

        protected override async void OnShown(EventArgs eventArgs)
        {
            base.OnShown(eventArgs);
            if (!string.IsNullOrWhiteSpace(startupError))
            {
                BeginInvoke(new Action(() => ShowError(startupError)));
            }

            await CheckLatestVersionAsync();
        }

        // runs after the window is visible so a slow or offline API can't hold up startup
        private async Task CheckLatestVersionAsync()
        {
            SetButtonsEnabled(false);
            foreach (var updateStatus in new[] { HebnixUpdateStatus, LiteUpdateStatus })
            {
                updateStatus.BackColor = Color.MidnightBlue;
                updateStatus.Text = "Checking...";
            }

            try
            {
                latestVersion = await GetLatestVersionAsync();
            }
            catch (Exception exception)
            {
                ShowError("Could not check for the latest Hebnix version.\r\n\r\n" + exception);
            }
            finally
            {
                ApplyInstallationState();
                SetButtonsEnabled(true);
            }
        }

        private async Task<string> GetLatestVersionAsync()
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, ApiBaseUrl + "/info"))
            {
                request.Headers.UserAgent.ParseAdd("Hebnix-Updater");
                using (var response = await VersionClient.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();
                    var json = await response.Content.ReadAsStringAsync();
                    var info = JsonConvert.DeserializeObject<HebnixApiResponse>(json);
                    if (info == null || string.IsNullOrWhiteSpace(info.latest_version))
                    {
                        throw new InvalidOperationException("The update API did not return a version.");
                    }

                    discordInvite = info.discord_invite ?? string.Empty;
                    return info.latest_version.Trim();
                }
            }
        }

        private void ScanInstallation()
        {
            hebnixInstalled = File.Exists(Path.Combine(installDirectory, "Hebnix.exe"));
            liteExecutableName = null;
            foreach (var executableName in LiteExecutableNames)
            {
                if (File.Exists(Path.Combine(installDirectory, executableName)))
                {
                    liteExecutableName = executableName;
                    break;
                }
            }

            liteInstalled = !string.IsNullOrWhiteSpace(liteExecutableName);
        }
        private Dictionary<string, string> ReadInstalledVersions()
        {
            var versions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (!File.Exists(installStatePath))
            {
                return versions;
            }

            foreach (var line in File.ReadAllLines(installStatePath))
            {
                var separator = line.IndexOf('=');
                if (separator <= 0 || separator == line.Length - 1)
                {
                    continue;
                }

                var edition = line.Substring(0, separator).Trim();
                var version = line.Substring(separator + 1).Trim();
                if (!string.IsNullOrWhiteSpace(edition) && !string.IsNullOrWhiteSpace(version))
                {
                    versions[edition] = version;
                }
            }

            return versions;
        }

        private void SaveInstalledVersions()
        {
            var lines = new List<string>();
            foreach (var edition in new[] { FullEdition, LiteEdition })
            {
                string version;
                if (installedVersions.TryGetValue(edition, out version) && !string.IsNullOrWhiteSpace(version))
                {
                    lines.Add(edition + "=" + version);
                }
            }

            File.WriteAllLines(installStatePath, lines);
        }

        private void ApplyInstallationState()
        {
            UpdateEditionState(hebnixInstalled, FullEdition, HebnixStatus, HebnixUpdateStatus, hebnixinstupdate, hebnixuninstall);
            UpdateEditionState(liteInstalled, LiteEdition, LiteStatus, LiteUpdateStatus, liteinstupdate, liteuninstall);
        }

        private void UpdateEditionState(bool installed, string edition, Label installStatus, Label updateStatus, Button installButton, Button uninstallButton)
        {
            installStatus.Visible = true;
            installStatus.BackColor = installed ? Color.Green : Color.Maroon;
            installStatus.Text = installed ? "Installed" : "Not Installed";

            uninstallButton.Visible = installed;
            updateStatus.Visible = installed;
            var updatePending = installed && IsUpdatePending(edition);
            updateStatus.BackColor = updatePending ? Color.MidnightBlue : Color.Green;
            updateStatus.Text = updatePending ? "Update Pending" : "Up to date";
            installButton.Text = installed ? (updatePending ? "Update" : "Reinstall") : "Install";
        }

        private bool IsUpdatePending(string edition)
        {
            string installedVersion;
            return string.IsNullOrWhiteSpace(latestVersion)
                || !installedVersions.TryGetValue(edition, out installedVersion)
                || !VersionsMatch(installedVersion, latestVersion);
        }

        private static bool VersionsMatch(string installed, string available)
        {
            Version installedVersion;
            Version availableVersion;
            if (Version.TryParse(installed.Trim().TrimStart('v', 'V'), out installedVersion)
                && Version.TryParse(available.Trim().TrimStart('v', 'V'), out availableVersion))
            {
                return installedVersion == availableVersion;
            }

            return string.Equals(installed.Trim(), available.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private async void hebnixinstupdate_Click(object sender, EventArgs eventArgs)
        {
            await InstallOrUpdateAsync(FullEdition, "Hebnix.exe", "/download-hebnix", HebnixPB);
        }

        private async void liteinstupdate_Click(object sender, EventArgs eventArgs)
        {
            await InstallOrUpdateAsync(LiteEdition, liteExecutableName ?? LiteExecutableNames[0], "/download-hebnix-lite", LitePB);
        }

        private async void hebnixuninstall_Click(object sender, EventArgs eventArgs)
        {
            await UninstallAsync(FullEdition, "Hebnix.exe", "Hebnix.lnk", HebnixPB);
        }

        private async void liteuninstall_Click(object sender, EventArgs eventArgs)
        {
            await UninstallAsync(LiteEdition, liteExecutableName ?? LiteExecutableNames[0], "Hebnix Lite.lnk", LitePB);
        }

        private async Task InstallOrUpdateAsync(string edition, string executableName, string endpoint, ProgressBar progressBar)
        {
            SetBusy(true, progressBar);
            string temporaryZip = null;
            try
            {
                CloseProcess(Path.GetFileNameWithoutExtension(executableName));
                temporaryZip = Path.Combine(Path.GetTempPath(), "Hebnix-" + Guid.NewGuid().ToString("N") + ".zip");
                await DownloadAsync(ApiBaseUrl + endpoint, temporaryZip, progressBar);
                await ExtractAsync(temporaryZip, progressBar);
                ScanInstallation();
                var installedExecutable = edition == LiteEdition ? liteExecutableName ?? executableName : executableName;
                CreateShortcut(installedExecutable, edition + ".lnk");
                installedVersions[edition] = latestVersion;
                SaveInstalledVersions();
                ScanInstallation();
                ApplyInstallationState();
            }
            catch (Exception exception)
            {
                ShowError(exception.ToString());
            }
            finally
            {
                if (!string.IsNullOrWhiteSpace(temporaryZip) && File.Exists(temporaryZip))
                {
                    File.Delete(temporaryZip);
                }

                SetBusy(false, progressBar);
            }
        }

        private async Task UninstallAsync(string edition, string executableName, string shortcutName, ProgressBar progressBar)
        {
            var lastEdition = edition == FullEdition ? !liteInstalled : !hebnixInstalled;
            UninstallOptions options = null;
            if (lastEdition)
            {
                using (var dialog = new FinalUninstallDialog())
                {
                    if (dialog.ShowDialog(this) != DialogResult.OK)
                    {
                        return;
                    }
                    options = dialog.Options;
                }
            }

            SetBusy(true, progressBar);
            try
            {
                progressBar.Value = 15;
                await Task.Run(() => CloseProcess(Path.GetFileNameWithoutExtension(executableName)));
                if (lastEdition)
                {
                    // elevated cleanup goes first, in one UAC prompt, so declining it
                    // leaves the install untouched instead of half removed
                    var cleanupArguments = options.KeepTap
                        ? new[] { "--cleanup-spoofer" }
                        : new[] { "--cleanup-spoofer", "--cleanup-tap" };
                    await Task.Run(() => ElevatedRunner.Run(cleanupArguments));
                    await Task.Run(() => Maintenance.RemoveInstallData(options));
                }
                else
                {
                    var executablePath = Path.Combine(installDirectory, executableName);
                    if (File.Exists(executablePath))
                    {
                        File.Delete(executablePath);
                    }
                }

                DeleteShortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), shortcutName));
                DeleteShortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "Hebnix", shortcutName));
                installedVersions.Remove(edition);
                if (lastEdition)
                {
                    installedVersions.Clear();
                }
                if (!lastEdition && Directory.Exists(installDirectory))
                {
                    SaveInstalledVersions();
                }
                progressBar.Value = 100;
                if (lastEdition)
                {
                    Maintenance.ScheduleRemainingRemoval(options);
                    BeginInvoke(new Action(Close));
                    return;
                }
                ScanInstallation();
                ApplyInstallationState();
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show(this, "Administrator permission was declined, so the uninstall was cancelled. Nothing was removed.", "Uninstall cancelled", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception exception)
            {
                ShowError(exception.ToString());
            }
            finally
            {
                SetBusy(false, progressBar);
            }
        }

        private void LaunchCleanup_Click(object sender, EventArgs eventArgs)
        {
            using (var dialog = new CleanupDialog())
            {
                dialog.ShowDialog(this);
            }
        }
        private async Task DownloadAsync(string url, string destination, ProgressBar progressBar)
        {
            progressBar.Value = 0;
            using (var request = new HttpRequestMessage(HttpMethod.Get, url))
            {
                request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                request.Headers.Referrer = new Uri(ApiBaseUrl + "/");
                using (var response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(true))
                {
                    response.EnsureSuccessStatusCode();
                    var totalBytes = response.Content.Headers.ContentLength;
                    using (var source = await response.Content.ReadAsStreamAsync().ConfigureAwait(true))
                    using (var destinationStream = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true))
                    {
                        var buffer = new byte[81920];
                        long downloaded = 0;
                        int read;
                        while ((read = await source.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(true)) > 0)
                        {
                            await destinationStream.WriteAsync(buffer, 0, read).ConfigureAwait(true);
                            downloaded += read;
                            progressBar.Value = totalBytes.HasValue && totalBytes.Value > 0
                                ? Math.Min(80, (int)(downloaded * 80L / totalBytes.Value))
                                : Math.Min(80, progressBar.Value + 1);
                        }
                    }
                }
            }
        }

        private void CopySetupToInstallDirectory()
        {
            var updaterDirectory = Path.Combine(installDirectory, "updater");
            var destination = Path.Combine(updaterDirectory, "setup.exe");
            var source = Application.ExecutablePath;
            if (string.Equals(Path.GetFullPath(source), Path.GetFullPath(destination), StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            Directory.CreateDirectory(updaterDirectory);
            File.Copy(source, destination, true);
        }
        private async Task ExtractAsync(string zipPath, ProgressBar progressBar)
        {
            await Task.Run(() =>
            {
                Directory.CreateDirectory(installDirectory);
                var installRoot = Path.GetFullPath(installDirectory + Path.DirectorySeparatorChar);
                using (var archive = ZipFile.OpenRead(zipPath))
                {
                    var entryCount = archive.Entries.Count;
                    for (var index = 0; index < entryCount; index++)
                    {
                        var entry = archive.Entries[index];
                        var destination = Path.GetFullPath(Path.Combine(installDirectory, entry.FullName));
                        if (!destination.StartsWith(installRoot, StringComparison.OrdinalIgnoreCase))
                        {
                            throw new InvalidDataException("The update archive contains an unsafe path.");
                        }

                        if (string.IsNullOrEmpty(entry.Name))
                        {
                            Directory.CreateDirectory(destination);
                        }
                        else
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(destination));
                            entry.ExtractToFile(destination, true);
                        }

                        var progress = 80 + Math.Min(19, (index + 1) * 19 / Math.Max(1, entryCount));
                        BeginInvoke(new Action(() => progressBar.Value = progress));
                    }
                }
            }).ConfigureAwait(true);
        }

        private void CreateShortcut(string executableName, string shortcutName)
        {
            var executablePath = Path.Combine(installDirectory, executableName);
            if (!File.Exists(executablePath))
            {
                return;
            }

            CreateShortcutAt(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), shortcutName), executablePath);
            var startMenuDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "Hebnix");
            Directory.CreateDirectory(startMenuDirectory);
            CreateShortcutAt(Path.Combine(startMenuDirectory, shortcutName), executablePath);
        }

        private void CreateShortcutAt(string shortcutPath, string executablePath)
        {
            var shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType == null)
            {
                return;
            }

            dynamic shell = Activator.CreateInstance(shellType);
            dynamic shortcut = shell.CreateShortcut(shortcutPath);
            shortcut.TargetPath = executablePath;
            shortcut.WorkingDirectory = installDirectory;
            shortcut.Save();
        }

        private static void DeleteShortcut(string shortcutPath)
        {
            if (File.Exists(shortcutPath))
            {
                File.Delete(shortcutPath);
            }
        }

        private static void CloseProcess(string processName)
        {
            foreach (var process in Process.GetProcessesByName(processName))
            {
                try
                {
                    process.Kill();
                    process.WaitForExit(3000);
                }
                finally
                {
                    process.Dispose();
                }
            }
        }

        private void SetButtonsEnabled(bool enabled)
        {
            hebnixinstupdate.Enabled = enabled;
            liteinstupdate.Enabled = enabled;
            hebnixuninstall.Enabled = enabled;
            liteuninstall.Enabled = enabled;
            launchCleanup.Enabled = enabled;
        }

        private void SetBusy(bool busy, ProgressBar activeProgressBar)
        {
            SetButtonsEnabled(!busy);
            HebnixPB.Visible = busy && activeProgressBar == HebnixPB;
            LitePB.Visible = busy && activeProgressBar == LitePB;
            if (busy)
            {
                activeProgressBar.Value = 0;
            }
        }

        private void discordJoin_Click(object sender, EventArgs eventArgs)
        {
            if (!string.IsNullOrWhiteSpace(discordInvite))
            {
                Process.Start(new ProcessStartInfo(discordInvite) { UseShellExecute = true });
            }
        }

        private void ShowError(string error)
        {
            using (var dialog = new Form())
            using (var message = new RichTextBox())
            {
                dialog.Text = "Hebnix Installer Error";
                dialog.ClientSize = new Size(400, 600);
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.FormBorderStyle = FormBorderStyle.FixedSingle;
                dialog.MaximizeBox = false;
                dialog.MinimizeBox = false;
                dialog.BackColor = BackColor;
                dialog.ForeColor = ForeColor;

                message.Dock = DockStyle.Fill;
                message.ReadOnly = true;
                message.BorderStyle = BorderStyle.None;
                message.BackColor = Color.FromArgb(11, 12, 16);
                message.ForeColor = Color.White;
                message.Font = Font;
                message.Text = error;
                dialog.Controls.Add(message);
                dialog.ShowDialog(this);
            }
        }
    }

    public class HebnixApiResponse
    {
        public string latest_version { get; set; }
        public string discord_invite { get; set; }
    }
}





