using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Hebnix_Updater
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length == 1 && args[0] == "--cleanup-spoofer")
            {
                Maintenance.RemoveSpoofer();
                return;
            }
            if (args.Length == 1 && args[0] == "--cleanup-tap")
            {
                Maintenance.RemoveMultiplayerTap();
                return;
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            var mainForm = new MainForm();
            mainForm.PrepareBeforeShowing();
            Application.Run(mainForm);
        }
    }
}


