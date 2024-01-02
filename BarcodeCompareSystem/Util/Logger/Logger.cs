//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using log4net;
//using System.Diagnostics;

//namespace BarcodeCompareSystem
//{

//    class Logger
//    {
//        public static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
//        public static void DebugLog(string content)
//        {
//            StringBuilder sb = Logger.GetCurrentThread();
//            sb.Append(content);
//            log.Debug(sb.ToString());
//        }

//        public static void InfoLog(string content) {
//            StringBuilder sb = Logger.GetCurrentThread();
//            sb.Append(content);
//            log.Info(sb.ToString());
//        }

//        public static void ErrorLog(string content)
//        {
//            StringBuilder sb = Logger.GetCurrentThread();
//            sb.Append(content);
//            log.Error(sb.ToString());
//        }

//        public static StringBuilder GetCurrentThread() {
//            int pId = Process.GetCurrentProcess().Id;
//            int tId = System.Threading.Thread.CurrentThread.ManagedThreadId;

//            StringBuilder sb = new StringBuilder();
//            sb.Append("[");
//            sb.Append(pId);
//            sb.Append("-");
//            sb.Append(tId);
//            sb.Append("] ");
//            return sb;
//        }

//    }
//}
