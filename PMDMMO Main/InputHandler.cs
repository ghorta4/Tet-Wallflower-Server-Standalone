using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Guildleader;
using ServerResources;

namespace PMDMMO_Main
{
    public static class InputHandler
    {
        public static Thread inputThread;
        public static string currentEntry = "";
        public static void HandleUserInput()
        {
            ErrorHandler.AddMessageToLog("User input thread started.");
            while (!Application.requestApplicationClosed)
            {
                ConsoleKeyInfo cki = Console.ReadKey(true);

                switch (cki.Key)
                {
                    case ConsoleKey.Enter:
                        ProcessCommand(currentEntry);
                        currentEntry = "";
                        break;
                    case ConsoleKey.Backspace:
                        if (currentEntry.Length > 0)
                        {
                            currentEntry = currentEntry.Substring(0, currentEntry.Length - 1);
                        }
                        break;
                    default:
                        currentEntry += cki.KeyChar;
                        break;
                }
            }
            ErrorHandler.AddMessageToLog("User input thread closed.");
        }

        static void ProcessCommand(string s)
        {
            switch (s.ToLower())
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

            ServerWorldHandler world = WorldManager.currentWorld as ServerWorldHandler;

            Write("Finished: " + world.worldLoaded);
            Write("Thread states:");
            foreach (string s in world.threadStates)
            {
                Write(s);
            }
        }

        static void Write(string message)
        {
            ErrorHandler.AddMessageToLog(message);
        }
    }
}
