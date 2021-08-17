using System;
using System.Collections.Generic;


namespace Guildleader
{
    public static class StartupAndEndFunctions
    {
        static WirelessCommunicator mainWifiComm;

        public static void InitializeAll(WirelessCommunicator wifiComm)
        {
            Entities.Entity.InitializeEntities();
            FileAccess.Initialize();
            wifiComm.Initialize();
            mainWifiComm = wifiComm;
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
