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

                if ((DateTime.Now - ci.cooldowns.lastChunkUpdateGiven).TotalMilliseconds < 334)
                {
                    Thread.Sleep(50);
                    continue;
                }
                ci.cooldowns.lastChunkUpdateGiven = DateTime.Now;

                PlayerPokemon poke = ci.thisUsersPokemon;

                if (poke == null)
                {
                    continue;
                }

                Int3 chunkPos = poke.GetChunkPosition();
                //Fetch nearby chunks in a 3x3x3 space and share it to the client
                Chunk[] testChunks = WorldManager.currentWorld.GetChunksInArea(chunkPos.x, chunkPos.y, chunkPos.z, 1, 1, 1);
                //also stores all nearby entities so the client knows the only entities it needs to render
                List<Entity> allNearbyEntities = new List<Entity>();
                foreach (Chunk c in testChunks)
                {
                    byte[] data = c.ConvertChunkToBytesWithPositionInFrontUsingSimples(c.chunkPos);
                    Application.Server.SendDataToOneClient(ci, WirelessCommunicator.PacketType.chunkInfo, data, 1);
                    allNearbyEntities.AddRange(c.containedEntities);
                }
                List<byte> serializedEntities = new List<byte>();
                foreach (Entity e in allNearbyEntities)
                {
                    byte[] bytes = e.ConvertToBytesForClient(poke);
                    if (bytes == null)
                    {
                        continue;
                    }
                    serializedEntities.AddRange(bytes);
                }
                Application.Server.SendDataToOneClient(ci, WirelessCommunicator.PacketType.nearbyEntityInfo, serializedEntities.ToArray(), 2);
                Thread.Sleep(20);
            }
        }
    }
}
