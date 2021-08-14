using System;
using PMDMMO_Main;

namespace Guildleader
{
    public static class StartupFunctions
    {
        public static void InitializeAll()
        {
            WirelessCommunicator.Initialize();
            StartSubThreads();
        }

        static void StartSubThreads()
        {
        }
    }

    public static class MainFunctions
    {
        public static void Update()
        {
            
        }
    }
}
