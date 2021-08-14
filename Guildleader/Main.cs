using System;
using System.Collections.Generic;


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
        public static bool endProgramRequested;

        public static void Update()
        {
            endProgramRequested = true;
        }
    }
}
