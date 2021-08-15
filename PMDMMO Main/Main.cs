using System;
using System.Collections.Generic;


namespace Guildleader
{
    public static class StartupAndEndFunctions
    {
        static WirelessCommunicator mainWifiComm;

        public static void InitializeAll(WirelessCommunicator wifiComm)
        {
            FileAccess.Initialize();
            wifiComm.Initialize();
            mainWifiComm = wifiComm;
            FileAccess.Initialize();
            StartSubThreads(wifiComm);
        }

        static void StartSubThreads(WirelessCommunicator wifiComm)
        {
            wifiComm.StartListeningThread();
        }

        public static void CleanupAll()
        {
            mainWifiComm.Cleanup();
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
