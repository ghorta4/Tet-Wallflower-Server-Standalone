using System;
using Guildleader;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using ServerResources;
using System.Net;
using System.Threading;

using Guildleader.Entities;
using Guildleader.Entities.BasicEntities;

namespace PMDMMO_Main
{

    public static class Application
    {
        public static bool requestApplicationClosed;
        public static bool endHUD;

        public static WirelessServer Server;

        static void Main(string[] args)
        {
            ConsoleBasedHUD.consoleThread = new Thread(ConsoleBasedHUD.UpdateHUD);
            ConsoleBasedHUD.consoleThread.Start();

            Server = new WirelessServer();
            ErrorHandler.AddMessageToLog("Server started.");
            StartupAndEndFunctions.InitializeAll(Server);

            FileAccess.SetDefaultDirectory(FileAccess.AssetsFileLocation);

            InputHandler.inputThread = new Thread(InputHandler.HandleUserInput);
            InputHandler.inputThread.Start();
            ErrorHandler.AddMessageToLog("World generating...");
            WorldManager.currentWorld = new ServerWorldHandler();
            (WorldManager.currentWorld as ServerWorldHandler).InitializeAllChunks();
            ErrorHandler.AddMessageToLog("World generated.");

            WorldStateManager.Initialize();

            while (!requestApplicationClosed)
            {
                MainFunctions.Update();
                Server.Update();
                WorldStateManager.Update();
                GameStateCommunications.ShareServerStateThread();
                ErrorHandler.AddMessageToLog("main update.");
                if (MainFunctions.endProgramRequested)
                {
                    requestApplicationClosed = true;
                }
            }
            StartupAndEndFunctions.CleanupAll();
            ErrorHandler.AddMessageToLog("Server ended. Press any key to continue.");
            Console.ReadKey();
            endHUD = true;
        }

        public static void EndProgram()
        {
            requestApplicationClosed = true;
        }
    }
}
