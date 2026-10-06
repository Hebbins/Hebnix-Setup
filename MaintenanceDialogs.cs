using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Hebnix_Updater
{
    internal sealed class FinalUninstallDialog : Form
    {
        private readonly CheckBox keepPlugins = CheckBox("Keep Plugins Folder");
        private readonly CheckBox keepThemes = CheckBox("Keep Themes Folder");
        private readonly CheckBox keepConfiguration = CheckBox("Keep Configuration");
        private readonly CheckBox keepTap = CheckBox("Keep TAP Adapters (if used)");
        private readonly CheckBox keepPatches = CheckBox("Keep File Patches & Swaps");
        private readonly CheckBox keepMaps = CheckBox("Keep Workshop Maps");

        public UninstallOptions Options
        {
            get
            {
                return new UninstallOptions
                {
                    KeepPlugins = keepPlugins.Checked,
                    KeepThemes = keepThemes.Checked,
                    KeepConfiguration = keepConfiguration.Checked,
                    KeepTap = keepTap.Checked,
                    KeepPatches = keepPatches.Checked,
                    KeepMaps = keepMaps.Checked
                };
            }
        }

        public FinalUninstallDialog()
        {
            Text = "Uninstall Hebnix";
            ClientSize = new Size(400, 360);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Theme.Background;
            ForeColor = Color.White;
            Font = new Font("Verdana", 10F);

            var heading = new Label { Text = "Keep selected data", AutoSize = true, Location = new Point(20, 18), Font = new Font("Verdana", 13F) };
            var checks = new[] { keepPlugins, keepThemes, keepConfiguration, keepTap, keepPatches, keepMaps };
            for (var index = 0; index < checks.Length; index++)
            {
                checks[index].Location = new Point(24, 55 + index * 35);
                Controls.Add(checks[index]);
            }
            var cancel = Theme.Button("Cancel", new Point(20, 290), new Size(170, 42));
            cancel.DialogResult = DialogResult.Cancel;
            var uninstall = Theme.Button("Uninstall", new Point(210, 290), new Size(170, 42));
            uninstall.DialogResult = DialogResult.OK;
            AcceptButton = uninstall;
            CancelButton = cancel;
            Controls.Add(heading);
            Controls.Add(cancel);
            Controls.Add(uninstall);
        }

        private static CheckBox CheckBox(string text)
        {
            return new CheckBox { Text = text, AutoSize = true, ForeColor = Color.White, BackColor = Theme.Background, FlatStyle = FlatStyle.Flat, Checked = false };
        }
    }

    internal sealed class CleanupDialog : Form
    {
        private readonly Button maps;
        private readonly Button patches;
        private readonly Button swaps;
        private readonly Button tap;
        private readonly Button spoofer;
        private readonly Button close;

        public CleanupDialog()
        {
            Text = "Hebnix Cleanup";
            ClientSize = new Size(400, 360);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Theme.Background;
            ForeColor = Color.White;
            Font = new Font("Verdana", 10F);

            maps = Theme.Button("Restore Maps", new Point(20, 20), new Size(360, 42));
            patches = Theme.Button("Restore Patches", new Point(20, 75), new Size(360, 42));
            swaps = Theme.Button("Restore Swaps", new Point(20, 130), new Size(360, 42));
            tap = Theme.Button("Remove Multiplayer TAP", new Point(20, 185), new Size(360, 42));
            spoofer = Theme.Button("Remove Spoofer", new Point(20, 240), new Size(360, 42));
            close = Theme.Button("Close", new Point(20, 300), new Size(360, 42));
            maps.Click += async (sender, args) => await RunAsync(Maintenance.RestoreMaps, false);
            patches.Click += async (sender, args) => await RunAsync(Maintenance.RestorePatches, false);
            swaps.Click += async (sender, args) => await RunAsync(Maintenance.RestoreSwaps, false);
            tap.Click += async (sender, args) => await RunAsync(Maintenance.RemoveMultiplayerTap, true);
            spoofer.Click += async (sender, args) => await RunAsync(Maintenance.RemoveSpoofer, true);
            close.Click += (sender, args) => Close();
            Controls.AddRange(new Control[] { maps, patches, swaps, tap, spoofer, close });
        }

        private async Task RunAsync(Action action, bool requiresElevation)
        {
            SetEnabled(false);
            try
            {
                if (requiresElevation)
                {
                    await Task.Run(() => ElevatedRunner.Run(action == Maintenance.RemoveSpoofer ? "--cleanup-spoofer" : "--cleanup-tap"));
                }
                else
                {
                    await Task.Run(action);
                }
            }
            catch (OperationCanceledException)
            {
                // the user declined UAC, nothing ran
            }
            catch (Exception exception)
            {
                MaintenanceError.Show(this, exception.ToString());
            }
            finally
            {
                SetEnabled(true);
            }
        }

        private void SetEnabled(bool enabled)
        {
            maps.Enabled = enabled;
            patches.Enabled = enabled;
            swaps.Enabled = enabled;
            tap.Enabled = enabled;
            spoofer.Enabled = enabled;
            close.Enabled = enabled;
        }
    }

    internal static class ElevatedRunner
    {
        private const int ErrorCancelled = 1223;

        public static void Run(params string[] arguments)
        {
            Process process;
            try
            {
                process = Process.Start(new ProcessStartInfo(Application.ExecutablePath, string.Join(" ", arguments)) { UseShellExecute = true, Verb = "runas", WindowStyle = ProcessWindowStyle.Hidden });
            }
            catch (Win32Exception exception) when (exception.NativeErrorCode == ErrorCancelled)
            {
                throw new OperationCanceledException("Administrator permission was declined.", exception);
            }

            using (process)
            {
                if (process == null) throw new InvalidOperationException("Could not start elevated cleanup.");
                process.WaitForExit();
                if (process.ExitCode != 0) throw new InvalidOperationException("Cleanup did not complete successfully.");
            }
        }
    }

    internal static class MaintenanceError
    {
        public static void Show(IWin32Window owner, string text)
        {
            using (var dialog = new Form())
            using (var message = new RichTextBox())
            {
                dialog.Text = "Hebnix Cleanup Error";
                dialog.ClientSize = new Size(400, 600);
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.FormBorderStyle = FormBorderStyle.FixedSingle;
                dialog.MaximizeBox = false;
                dialog.MinimizeBox = false;
                dialog.BackColor = Theme.Background;
                message.Dock = DockStyle.Fill;
                message.ReadOnly = true;
                message.BorderStyle = BorderStyle.None;
                message.BackColor = Theme.Background;
                message.ForeColor = Color.White;
                message.Font = new Font("Verdana", 10F);
                message.Text = text;
                dialog.Controls.Add(message);
                dialog.ShowDialog(owner);
            }
        }
    }

    internal static class Theme
    {
        public static readonly Color Background = Color.FromArgb(11, 12, 16);

        public static Button Button(string text, Point location, Size size)
        {
            var button = new Button { Text = text, Location = location, Size = size, BackColor = Color.FromArgb(47, 54, 64), ForeColor = Color.White, Cursor = Cursors.Hand, FlatStyle = FlatStyle.Flat, Font = new Font("Verdana", 11F), UseVisualStyleBackColor = false };
            button.FlatAppearance.BorderColor = Color.FromArgb(53, 59, 72);
            button.FlatAppearance.BorderSize = 3;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(52, 59, 69);
            return button;
        }
    }
}
