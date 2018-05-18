using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SystemsGarden.mc2.Tools.DevelopmentServer
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            bool result;
            var mutex = new System.Threading.Mutex(true, "{8A478B39-B95D-4D4D-B539-2463BBD2DD28}", out result);

            if (!result)
                return;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new DevelopmentServerForm());

            GC.KeepAlive(mutex);
        }
    }
}
