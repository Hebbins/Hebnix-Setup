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
            // the elevated cleanup flags can be combined so uninstall only needs one UAC prompt
            var handledCleanup = false;
            foreach (var arg in args)
            {
                if (arg == "--cleanup-spoofer")
                {
                    Maintenance.RemoveSpoofer();
                    handledCleanup = true;
                }
                else if (arg == "--cleanup-tap")
                {
                    Maintenance.RemoveMultiplayerTap();
                    handledCleanup = true;
                }
            }
            if (handledCleanup)
            {
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


