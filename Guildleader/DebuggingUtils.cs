using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Guildleader
{
    public static class DebuggingUtils
    {
        public static void PrintByteArray(byte[] list)
        {
            string s = ConvertBytesToReadableString(list);
            Console.WriteLine(s);
        }

        public static string ConvertBytesToReadableString(byte[] list)
        {
            StringBuilder sb = new StringBuilder("");
            foreach (byte b in list)
            {
                sb.Append(b);
                sb.Append('|');
            }
            return sb.ToString();
        }
    }

    public static class ErrorHandler
    {
        static bool readingLog;
        static Queue<Exception> errorLog = new Queue<Exception>();
        static bool pendingErrors;

        public static void AddErrorToLog(Exception e)
        {
            if (readingLog)
            {
                return;
            }
            errorLog.Enqueue(e);
            if (!pendingErrors)
            {
                Console.WriteLine("Error log updated.");
            }
            pendingErrors = true;

            while (errorLog.Count > 100)
            {
                errorLog.Dequeue();
            }
        }

        public static void PrintErrorLog()
        {
            readingLog = true;
            while(errorLog.Count > 0)
            {
                Exception e = errorLog.Dequeue();
                Console.WriteLine(e);
            }
            ClearErrorLog();
            readingLog = false;
        }

        public static void ClearErrorLog()
        {
            errorLog.Clear();
            pendingErrors = false;
        }
    }
}
