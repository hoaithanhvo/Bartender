using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using log4net;
using System.Windows.Threading;
using log4net;
using System.Text;
using System.Diagnostics;

namespace BarcodeCompareSystem
{

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private DispatcherTimer dailyTimer;



        private void DailyTimer_Tick(object sender, EventArgs e)
        {
            DateTime currentTime = DateTime.Now;

            // Kiểm tra nếu là 7 giờ sáng và chưa kiểm tra trong ngày nay
            
                // Tìm cửa sổ MainWindow hiện tại trong ứng dụng
                MainWindow mainWindow = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();

                if (mainWindow != null)
                {
                    mainWindow.LoadFilePath(); // Gọi lại hàm LoadFilePath
                }
            
        }
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger("Log4Net");

        protected override void OnStartup(StartupEventArgs e)
        {
            log4net.Config.XmlConfigurator.Configure();
            base.OnStartup(e);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

           
        }

        void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // Process unhandled exception
            log.Info(e.ToString());
            log.Error("***** UNHANDLED THREAD EXCEPTION *****" + e.ToString());
            MessageBox.Show(e.ToString());
            // Prevent default unhandled exception processing
            e.Handled = true;
        }


        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            // Process unhandled exception
            log.Info(e.ToString());
            log.Error("***** UNHANDLED THREAD EXCEPTION *****" + e.ToString());
            MessageBox.Show(e.ToString());
            // Prevent default unhandled exception processing
            log.Error(string.Format("##### UNHANDLED APPDOMAIN EXCEPTION ({0}) #####", e.IsTerminating ? "Terminating" : "Non-Terminating"), e.ExceptionObject as Exception);
        }

        public static void DebugLog(string str)
        {
            int pId = Process.GetCurrentProcess().Id;
            //int tId = System.Threading.Thread.CurrentThread.ManagedThreadId;
            int tId = AppDomain.GetCurrentThreadId();

            StringBuilder sb = new StringBuilder();
            sb.Append("[");
            sb.Append(pId);
            sb.Append("-");
            sb.Append(tId);
            sb.Append("] ");
            sb.Append(str);

            log.Debug(sb.ToString());
        }

        public static void ErrorLog(string str)
        {
            int pId = Process.GetCurrentProcess().Id;
            //int tId = System.Threading.Thread.CurrentThread.ManagedThreadId;
            int tId = AppDomain.GetCurrentThreadId();
            StringBuilder sb = new StringBuilder();
            sb.Append("[");
            sb.Append(pId);
            sb.Append("-");
            sb.Append(tId);
            sb.Append("] ");
            sb.Append(str);

            log.Error(sb.ToString());
        }

        public static void InfoLog(string str)
        {
            int pId = Process.GetCurrentProcess().Id;
            //int tId = System.Threading.Thread.CurrentThread.ManagedThreadId;
            int tId = AppDomain.GetCurrentThreadId();

            StringBuilder sb = new StringBuilder();
            sb.Append("[");
            sb.Append(pId);
            sb.Append("-");
            sb.Append(tId);
            sb.Append("] ");
            sb.Append(str);

            log.Info(sb.ToString());
        }

    }
}
