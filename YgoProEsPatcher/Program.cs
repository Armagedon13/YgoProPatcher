using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace YgoProEsPatcher
{
    static class Program
    {
        /// <summary>
        /// Punto de entrada principal para la aplicación.
        /// </summary>
        [STAThread]
        static void Main()
        {
            using (Mutex mutex = new Mutex(false, "Global\\" + appGuid))
            {
                if (!mutex.WaitOne(0, false))
                {
                    MessageBox.Show("YgoProPatcher is already running!");
                    return;
                }
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                try
                {
                    Application.Run(new YgoProEsPatcher());
                }
                catch (Exception e)
                {
                    MessageBox.Show("UNEXPECTED ERROR HAS OCCURED IN THE YGOPROPATCHER!\nIF YOU SEE THIS MESSAGE AGAIN, PLEASE SEND SCREENSHOT OF THIS MESSAGE TO THE DEVELOPER!\n\n" + e.Message + "\n" + e.ToString(), "Unexpected Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        private static readonly string appGuid = "50d03ea6-81a4-4c8e-a2fa-8bb86606608e";
    }
    
}
