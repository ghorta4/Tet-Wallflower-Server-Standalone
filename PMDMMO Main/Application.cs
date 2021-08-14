using System;
using Guildleader;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace PMDMMO_Main
{

    public static class Application
    {
        static bool requestApplicationClosed;

        static bool pendingErrors;
        static List<Exception> errorLog = new List<Exception>();
        static void Main(string[] args)
        {
            Console.WriteLine("Server started.");
            StartupFunctions.InitializeAll();
            while (!requestApplicationClosed)
            {
                MainFunctions.Update();
            }

            Console.WriteLine("Server ended. Press any key to continue.");
            Console.ReadKey();
        }

        public static void addErrorToLog(Exception e)
        {
            errorLog.Add(e);
            if (!pendingErrors)
            {
                Console.WriteLine("Error log updated.");
            }
            pendingErrors = true;

            while (errorLog.Count > 100)
            {
                errorLog.RemoveAt(0);
            }
        }
    }
}
