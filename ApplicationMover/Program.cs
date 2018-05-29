using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ApplicationMover
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            System.Diagnostics.Process[] processes = System.Diagnostics.Process.GetProcessesByName(Application.ProductName);
            if (processes.Length > 1)
            {
                MessageBox.Show("应用程序已经在运行中！");
                Thread.Sleep(1000);
                System.Environment.Exit(1);
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
