using System;
using System.Collections.Generic;
using System.Linq;
using Guildleader;
using System.Threading;
using PMDMMO_Main;
using Guildleader.Entities;
using Guildleader.Entities.BasicEntities;

namespace ServerResources
{
    public static class GameStateCommunications
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

                PlayerPokemon poke = ci.thisUsersPokemon;
                Int3 chunkPos = poke.GetChunkPosition();
                //below is simply a test implementation
                Chunk[] testChunks = WorldManager.currentWorld.GetChunksInArea(chunkPos.x, chunkPos.y, chunkPos.z, 1, 1, 1);
                foreach (Chunk c in testChunks)
                {
                    byte[] data = c.ConvertChunkToBytesWithPositionInFrontUsingSimples(c.chunkPos);
                    Application.Server.SendDataToOneClient(ci, WirelessCommunicator.PacketType.chunkInfo, data, 2);
                }
                Thread.Sleep(20);
            }
        }
    }
}
