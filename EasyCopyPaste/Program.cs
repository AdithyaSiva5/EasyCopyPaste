using System;
using System.Windows.Forms;
using System.Threading;

namespace EasyCopyPaste
{
    static class Program
    {
        private static Mutex mutex = new Mutex(true, "EnhancedCopyPasteApp");

        [STAThread]
        static void Main()
        {
            if (mutex.WaitOne(TimeSpan.Zero, true))
            {
                try
                {
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.Run(new Form1());
                }
                finally
                {
                    mutex.ReleaseMutex();
                }
            }
            else
            {
                MessageBox.Show("Application is already running!", "Enhanced Copy Paste",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}   