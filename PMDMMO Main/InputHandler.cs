using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Guildleader;

namespace PMDMMO_Main
{
    public static class InputHandler
    {
        public static Thread inputThread;

        public static void HandleUserInput()
        {
            Console.WriteLine("User input thread started.");
            while (!Application.requestApplicationClosed)
            {
                string command = Console.ReadLine();

                switch (command.ToLower())
                {
                    case "q":
                    case "quit":
                    case "exit":
                    case "break":
                        CloseApp();
                        break;
                    case "server":
                    case "servers":
                    case "list":
                    case "connections":
                    case "a":
                    case "display servers":
                    case "displayservers":
                        ErrorHandler.AddErrorToLog(new Exception("Function not implemented."));
                        break;
                    case "e":
                    case "error":
                    case "error log":
                    case "log":
                    case "elog":
                    case "errorlog":
                        ErrorHandler.PrintErrorLog();
                        break;
                    case "c":
                    case "clear":
                    case "clean":
                        Console.Clear();
                        break;
                    case "gen progress":
                    case "build progress":
                    case "progress":
                        WriteWorldGenerationStatus();
                        break;
                    case "save world":
                        Write("Saving...");
                        WorldManager.currentWorld.SaveAllChunks();
                        Write("World data saved.");
                        break;
                }
            }
            Console.WriteLine("User input thread closed.");
        }

        static void CloseApp()
        {
            Application.requestApplicationClosed = true;
        }

        static void WriteWorldGenerationStatus()
        {
            if (WorldManager.currentWorld == null)
            {
                Write("World object not set.");
                return;
            }

            WorldDataStorageModuleGeneric world = WorldManager.currentWorld;

            Write("Finished: " + world.worldLoaded);
            Write("Thread states:");
            foreach (string s in world.threadStates)
            {
                Write(s);
            }
        }

        static void Write(string message)
        {
            Console.WriteLine(message);
        }
    }
}
