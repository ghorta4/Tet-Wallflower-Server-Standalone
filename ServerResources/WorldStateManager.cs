using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using PMDMMO_Main;
using Guildleader;
using Guildleader.Entities;
using Guildleader.Entities.BasicEntities;

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
        }

        public static void Update()
        {
            if (World == null)
            {
                return;
            }
            SpawnInPlayerCharacters();
        }

        static void SpawnInPlayerCharacters()
        {
            foreach (ClientInfo ci in Server.clients)
            {
                if (ci.thisUsersPokemon != null)
                {
                    continue;
                }

                ci.thisUsersPokemon = new PlayerPokemon();
            }
        }
    }
}
