using System;
using System.Collections.Generic;
using PMDMMO_Main;

namespace Guildleader
{
    public static class StartupAndEndFunctions
    {
        public static void InitializeAll()
        {
            WirelessCommunicator.Initialize();
            StartSubThreads();
        }

        static void StartSubThreads()
        {
        }

        public static void CleanupAll()
        {
            WirelessCommunicator.Cleanup();
        }
    }

    public static class MainFunctions
    {
        public static void Update()
        {
            Application.EndProgram();
        }
    }
}
