using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace SimShift
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.ApplicationExit +=ApplicationOnApplicationExit;
            Application.ThreadException += Application_ThreadException;
            AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            Debug.Listeners.Add(new TextWriterTraceListener("trace.txt"));
            log = File.OpenWrite("./exception.txt");
            log.Seek(0, SeekOrigin.End);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new FrmMain());
        }

        private static FileStream log; 
        private static void ApplicationOnApplicationExit(object sender, EventArgs eventArgs)
        {
            log.Close();
        }


        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Log("UnhandledException", (Exception)e.ExceptionObject);
        }

        static void CurrentDomain_FirstChanceException(object sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
        {
            Log("FirstChanceException", e.Exception);
        }

        private static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            Log("ThreadException", e.Exception);
        }
        private static void Log(string what, Exception e)
        {
            var h = "------------------ "+what+" ----------------\r\n" + e.Message + "\r\nSTACKTRACE: " + e.StackTrace + "\r\n" + e.ToString() + "\r\n";
            var h2 = ASCIIEncoding.ASCII.GetBytes(h);
            log.Write(h2,0,h2.Length);
        }
    }
}
