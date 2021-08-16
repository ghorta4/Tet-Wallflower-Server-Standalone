using System;
using System.Collections.Generic;
using System.IO;

namespace Guildleader
{
    public static class TileLibrary
    {
        public static Dictionary<short, TileProperties> tileLib = new Dictionary<short, TileProperties> { };

        const string tileInfoLocation = "TileInfo";
        public static void LoadTileLibrary()
        {
            FileInfo[] allFiles = FileAccess.GetAllFilesInDirectory(tileInfoLocation);
            if (allFiles == null)
            {
                ErrorHandler.AddErrorToLog("Unable to find files for tiles.");
                return;
            }
            foreach (FileInfo fi in allFiles)
            {
                if (fi.Extension != ".ti")
                {
                    continue;
                }
                ProcessTileFile(fi);
            }
        }

        static void ProcessTileFile(FileInfo fi)
        {
            StreamReader sr = new StreamReader(fi.FullName);
            TileProperties tp = new TileProperties();
            string line = "";
            while ((line = sr.ReadLine()) != null)
            {
                tp.ProcessTileFileLine(line);
            }
            sr.Close();

            if (tp.id == short.MaxValue)
            {
                ErrorHandler.AddErrorToLog($"Warning: Tile from file {fi.Name} does not have its ID assigned!");
            }
            else
            {
                tileLib[tp.id] = tp;
            }
        }
    }

    public class TileProperties
    {
        public short id = short.MaxValue;
        public short maxHealth;
        public List<Tuple<string, float>> variantsAndWeights = new List<Tuple<string, float>>();
        public float totalVariantWeight;
        string lastProcessedHeader;
        public List<string> tags = new List<string>();
        public void ProcessTileFileLine(string line)
        {
            string[] split = line.Split('/');
            string targetString = split[split.Length - 1];
            if (split.Length > 1)
            {
                lastProcessedHeader = split[0];
            }

            switch (lastProcessedHeader.ToLower())
            {
                case "health":
                    uint temp = uint.Parse(targetString);
                    maxHealth = (short)Math.Min(temp, short.MaxValue);
                    break;
                case "id":
                    id = short.Parse(targetString);
                    break;
                case "tile":
                    ProcessTileString(targetString);
                    break;
                case "tags":
                    ProcessTagsString(targetString);
                    break;
                default:
                    ErrorHandler.AddErrorToLog("Warning: Unrecognized tile info header " + lastProcessedHeader);
                    break;
            }
        }

        void ProcessTileString(string text)
        {
            string[] sortedByWeights = text.Split(';');
            for (int startPos = 0; startPos + 1 < sortedByWeights.Length; startPos += 2)
            {
                string[] acceptableAppearances = sortedByWeights[startPos].Split(',');
                for (int i = 0; i < acceptableAppearances.Length; i++)
                {
                    acceptableAppearances[i] = acceptableAppearances[i].Replace("\"", "");
                }
                float weight = float.Parse(sortedByWeights[startPos + 1]);
                foreach (string s in acceptableAppearances)
                {
                    variantsAndWeights.Add(new Tuple<string, float>(s, weight));
                    totalVariantWeight += weight;
                }
            }
        }
        void ProcessTagsString(string text)
        {
            string[] tags = text.Split();
            foreach (string t in tags)
            {
                this.tags.Add(t);
            }
        }
    }
}
