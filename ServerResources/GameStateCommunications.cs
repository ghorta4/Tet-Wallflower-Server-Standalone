using System;
using System.Collections.Generic;
using System.Text;
using Guildleader;
using System.Threading;
using PMDMMO_Main;

namespace ServerResources
{
    static class GameStateCommunications
    {
        static List<Thread> CurrentHelpingThreads = new List<Thread>();

        public static void Initialize()
        {
            Thread helper = new Thread(ShareServerStateThread);
            CurrentHelpingThreads.Add(helper);
            helper.Start();
        }

        static int lastAssistedClient;
        public static void ShareServerStateThread()
        {
            while (!Application.requestApplicationClosed)
            {
                if (Application.Server == null || Application.Server.clients.Count <= 0)
                {
                    Thread.Sleep(50);
                    continue;
                }
                lastAssistedClient++;
                lastAssistedClient %= Application.Server.clients.Count;
                ClientInfo ci = Application.Server.clients[lastAssistedClient];

                Chunk[] testChunks = WorldManager.currentWorld.GetChunksInArea(0, 0, 0, 2, 2, 0);

                foreach (Chunk c in testChunks)
                {
                    byte[] data = c.convertChunkToBytes();
                    Application.Server.sendDataToOneClient(ci, WirelessCommunicator.PacketType.gameStateData, data, 1);
                }

                Thread.Sleep(20);
            }
        }
    }
}
