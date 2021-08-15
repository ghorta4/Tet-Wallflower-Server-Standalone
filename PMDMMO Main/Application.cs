using System;
using Guildleader;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using ServerResources;
using System.Net;
using System.Threading;

namespace PMDMMO_Main
{

    public static class Application
    {
        public static bool requestApplicationClosed;

        static WirelessServer Server;

        static void Main(string[] args)
        {
            Server = new WirelessServer();
            Console.WriteLine("Server started.");
            StartupAndEndFunctions.InitializeAll(Server);

            FileAccess.SetDefaultDirectory(FileAccess.AssetsFileLocation);

            TileLibrary.LoadTileLibrary();
            InputHandler.inputThread = new Thread(InputHandler.HandleUserInput);
            InputHandler.inputThread.Start();
            Console.WriteLine("World generating...");
            WorldManager.currentWorld = new WorldDataStorageModuleGeneric();
            WorldManager.currentWorld.InitializeAllChunks();
            Console.WriteLine("World generated.");

            while (!requestApplicationClosed)
            {
                MainFunctions.Update();
                Server.Update();
                if (MainFunctions.endProgramRequested)
                {
                    requestApplicationClosed = true;
                }
            }
            StartupAndEndFunctions.CleanupAll();
            Console.WriteLine("Server ended. Press any key to continue.");
            Console.ReadKey();
        }

        public static void EndProgram()
        {
            requestApplicationClosed = true;
        }
    }
}
