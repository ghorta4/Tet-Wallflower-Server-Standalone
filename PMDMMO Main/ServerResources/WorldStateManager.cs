using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using PMDMMO_Main;
using Guildleader;
using Guildleader.Entities;
using Guildleader.Entities.BasicEntities;
using System.Diagnostics;

namespace ServerResources
{
    //this class is meant to manage all things that get/modify data from the world, such as running the game. Note that this is probably going to be multithreaded; and, as such,
    //multiple threads may try and access the same data, or data may be accessed while changed. Expect LOTS of errors to be thrown.

    public static class WorldStateManager
    {
        public static ServerWorldHandler World { get { return WorldManager.currentWorld as ServerWorldHandler; } }
        public static WirelessServer Server { get { return Application.Server; } }

        public static void Initialize()
        {
            GameStateCommunications.Initialize();
            GameTimer = new Stopwatch();
            GameTimer.Start();
        }

        static Stopwatch GameTimer;

        static byte currentFrameNumber; //keep track of the frame number so that entities dont accidentally update twice in the same frame when moving between chunks
        public static void Update()
        {
            if (World == null || !World.worldLoaded)
            {
                return;
            }
            SpawnInPlayerCharacters();

            float timeElapsed = (float)GameTimer.Elapsed.TotalSeconds;
            timeElapsed = Math.Min(timeElapsed, 0.3f);
            GameTimer.Restart();
            List<Chunk> toUpdate = World.GetAllChunksLoaded();
            currentFrameNumber++;
            foreach (Chunk c in toUpdate)
            {
                c.Update(timeElapsed, currentFrameNumber);
            }
        }

        static void SpawnInPlayerCharacters()
        {
            foreach (ClientInfo ci in Server.clients)
            {
                if (ci == null || ci.thisUsersPokemon != null)
                {
                    continue;
                }

                ci.thisUsersPokemon = new PlayerPokemon();
                ci.thisUsersPokemon.PokemonProfileID = "19";
                ci.thisUsersPokemon.Initialize(Int3.Zero);
                ci.thisUsersPokemon.RiseToSurface(10);
            }
        }
    }
}
