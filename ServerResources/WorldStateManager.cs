using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using PMDMMO_Main;
using Guildleader;

namespace ServerResources
{
    //this class is meant to manage all things that get/modify data from the world. Note that this is probably going to be multithreaded; and, as such,
    //multiple threads may try and access the same data, or data may be accessed while changed. Expect LOTS of errors to be thrown.
    public static class WorldStateManager
    {
        public static void Initialize()
        {
            GameStateCommunications.Initialize();
        }
    }
}
