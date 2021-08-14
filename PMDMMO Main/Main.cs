using System;
using System.Collections.Generic;


namespace Guildleader
{
    public static class StartupAndEndFunctions
    {
        public static void InitializeAll(WirelessCommunicator wifiComm)
        {
            wifiComm.Initialize(); 
            StartSubThreads(wifiComm);
        }

        static void StartSubThreads(WirelessCommunicator wifiComm)
        {
            wifiComm.StartListeningThread();
        }

        public static void CleanupAll(WirelessCommunicator wifiComm)
        {
            wifiComm.Cleanup();
        }
    }

    public static class MainFunctions
    {
        public static bool endProgramRequested;

        public static void Update()
        {

        }
    }
}
