using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Guildleader
{
    public static class FileAccess
    {
        static string DefaultFileLocation { get { return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData); } }
        public static string SaveLocation { get { return string.Concat(DefaultFileLocation, "/TetriaryWallflower/"); } }
        public static string DownloadedFileLocation { get { return string.Concat(SaveLocation, "Downloads/"); } } //for client to download assets from server
        public static string AssetsFileLocation { get { return string.Concat(SaveLocation, "Assets/"); } } //for server to store downloadable assets
        static string BackupFileLocation { get { return string.Concat(SaveLocation, "Backups/"); } }

        static string CurrentDefaultDirectory;

        public static void Initialize()
        {
            PokeDirectiory(SaveLocation);
            PokeDirectiory(DownloadedFileLocation);
            PokeDirectiory(AssetsFileLocation);
            PokeDirectiory(BackupFileLocation);
        }
        static void PokeDirectiory(string location)
        {
            if (!Directory.Exists(location))
            {
                Directory.CreateDirectory(location);
            }
        }

        public static void SetDefaultDirectory(string location)
        {
            PokeDirectiory(location);
            CurrentDefaultDirectory = location;
        }

        public static byte[] LoadFile(string fileName)
        {
            if (CurrentDefaultDirectory == null)
            {
                ErrorHandler.AddErrorToLog(new Exception("Warning: Current load directory is not set. Use the 'SetDefaultDirectory' function from the FileAccess class."));
                return null;
            }
            if (!Directory.Exists(CurrentDefaultDirectory))
            {
                ErrorHandler.AddErrorToLog(new Exception("Warning: Target directory does not exist. Directory: " + CurrentDefaultDirectory));
                return null;
            }
            string targetPath = CurrentDefaultDirectory + fileName;
            string backupPath = BackupFileLocation + fileName;
            if (File.Exists(targetPath))
            {
                return File.ReadAllBytes(targetPath);
            }
            else if (File.Exists(backupPath))
            {
                return File.ReadAllBytes(backupPath);
            }
            else
            {
                ErrorHandler.AddErrorToLog(new Exception("Warning: Cannot find file of name, nor backup of name, " + fileName));
                return null;
            }
        }
        public static FileInfo[] GetAllFilesInDirectory(string directory)
        {
            string target = CurrentDefaultDirectory + directory;
            string[] results = null;
            if (Directory.Exists(target))
            {
                results = Directory.GetFiles(CurrentDefaultDirectory + directory);
            }
            else if (Directory.Exists(BackupFileLocation + directory))
            {
                results = Directory.GetFiles(BackupFileLocation + directory);
            }
            else
            {
                ErrorHandler.AddErrorToLog("Warning: Cannot find filepath " + target);
                return null;
            }

            List<FileInfo> holster = new List<FileInfo>();
            foreach (string s in results)
            {
                holster.Add(new FileInfo(s));
            }
            return holster.ToArray();
        }
    }
}
