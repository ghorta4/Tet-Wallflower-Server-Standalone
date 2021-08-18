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

                if ((DateTime.Now - ci.cooldowns.lastChunkUpdateGiven).TotalMilliseconds < 250)
                {
                    Thread.Sleep(50);
                    continue;
                }
                ci.cooldowns.lastChunkUpdateGiven = DateTime.Now;

                //below is simply a test implementation
                Chunk[] testChunks = WorldManager.currentWorld.GetChunksInArea(0, 0, 0, 1, 1, 1);
                foreach (Chunk c in testChunks)
                {
                    byte[] data = c.ConvertChunkToBytesWithPositionInFrontUsingSimples(c.chunkPos);
                    Application.Server.SendDataToOneClient(ci, WirelessCommunicator.PacketType.gameStateDataNotOrdered, data, 2);
                }
                Thread.Sleep(20);
            }
        }
    }
}
