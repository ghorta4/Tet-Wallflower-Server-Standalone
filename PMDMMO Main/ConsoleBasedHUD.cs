using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Guildleader;
using ServerResources;

namespace PMDMMO_Main
{
    public static class ConsoleBasedHUD
    {
        public static Thread consoleThread;
        const int displayHeight = 25, displayWidth = 100;

        static char[][] stringBuffer;

        public static void UpdateHUD()
        {
            stringBuffer = new char[displayWidth][];
            
            for (int i = 0; i < displayWidth; i++)
            {
                stringBuffer[i] = new char[displayHeight];
                for (int j = 0; j < displayHeight; j++)
                {
                    stringBuffer[i][j] = ' ';
                }
            }

            Console.CursorVisible = false;
            while (!Application.endHUD)
            {
                RefreshBuffer();
                DrawBoxShape(Int2.Zero, new Int2(displayWidth-1, displayHeight-1), '-');

                //draw message log
                int currentLineInConsole = displayHeight - 11;
                int currentlyDrawnMessage = 0;
                while (currentLineInConsole > 0)
                {
                    if (currentlyDrawnMessage >= ErrorHandler.messageLog.Count)
                    {
                        break;
                    }
                    Stack<string> split = new Stack<string> { };
                    string targetMessage = ErrorHandler.messageLog[ErrorHandler.messageLog.Count - 1 - currentlyDrawnMessage];

                    for (int i = 0; i * 100 < targetMessage.Length; i++)
                    {
                        split.Push(targetMessage.Substring(i * 100, Math.Min(100, targetMessage.Length - i*100)));
                    }
                    while (split.Count > 0)
                    {
                        string subSection = split.Pop();
                        WriteString(new Int2(1, currentLineInConsole), subSection);
                        currentLineInConsole--;
                        if (currentLineInConsole < 1)
                        {
                            goto stopDrawing;
                        }
                    }

                    currentlyDrawnMessage++;
                }
                stopDrawing:
                //draw server status
                DrawServerStatus(new Int2( 0, displayHeight - 7 - 3));

                //draw current input
                DrawBoxShape(new Int2(0, displayHeight-3), new Int2(displayWidth-1, displayHeight-1), '=');
                stringBuffer[1][displayHeight - 2] = '>';

                //clear the zone!
                WriteString(new Int2(3, displayHeight - 2), new string(' ', 99));

                string entry = InputHandler.currentEntry;
                if (InputHandler.currentEntry.Length < 96)
                {
                    WriteString(new Int2(3, displayHeight - 2), entry);
                }
                else
                {
                    WriteString(new Int2(3, displayHeight - 2), entry.Substring(entry.Length - 96, 96));
                }

                //draw everything to the console
                for (int y = 0; y < displayHeight; y++)
                {
                    StringBuilder sb = new StringBuilder();
                    for (int x = 0; x < displayWidth; x++)
                    {
                        sb.Append(stringBuffer[x][y]);
                    }
                    Console.SetCursorPosition(0, y);
                    Console.WriteLine(sb.ToString());
                }

            }
        }

        public static void DrawServerStatus(Int2 upperLeft)
        {
            DrawBoxShape(upperLeft, upperLeft + new Int2(50, 7), '+');
            string[] lines = new string[5];
            lines[0] = "--==Server Status==--";
            ServerWorldHandler swh = WorldManager.currentWorld as ServerWorldHandler;
            if (swh == null || swh.threadStates == null)
            {
                return;
            }
            for (int i = 0; i < lines.Length - 1 && i < swh.threadStates.Length; i++)
            {
                lines[i + 1] = swh.threadStates[i];
            }

            for (int i = 0; i < lines.Length; i++)
            {
                WriteString(new Int2(upperLeft.x + 1, upperLeft.y + 1 + i), lines[i], 48);
            }
        }

        public static void DrawBoxShape(Int2 start, Int2 end, char symbol)
        {
            int xDifference = end.x - start.x;
            int yDifference = end.y - start.y;
            DrawLine(start, new Int2(1,0), xDifference, symbol);
            DrawLine(start, new Int2(0, 1), yDifference, symbol);
            DrawLine(end, new Int2(-1, 0), xDifference, symbol);
            DrawLine(end, new Int2(0, -1), yDifference, symbol);
        }

        public static void DrawLine(Int2 start, Int2 direction, int size, char character)
        {
            Int2 currentPos = start;
            string tos = character + "";
            for (int i = 0; i < size; i++)
            {
                WriteString(currentPos, tos);
                currentPos += direction;
            }
        }

        public static void WriteString(Int2 start, string s)
        {
            WriteString(start, s, displayWidth);
        }
        public static void WriteString(Int2 start, string s, int maxWidth)
        {
            if (s == null)
            {
                return;
            }
            Int2 currentPos = start;

            for (int i = 0; i < Math.Min(s.Length, maxWidth); i++)
            {
                if (currentPos.x >= stringBuffer.Length || currentPos.y >= stringBuffer[0].Length)
                {
                    return;
                }
                stringBuffer[currentPos.x][currentPos.y] = s[i];
                currentPos += new Int2(1,0);
            }
        }

        static void RefreshBuffer()
        {
            foreach (char[] chararray in stringBuffer)
            {
                for (int i = 0; i < chararray.Length; i++)
                {
                    chararray[i] = ' ';
                }
            }
        }
    }
}
