using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Guildleader
{
    public static class PokemonLibrary
    {
        public static Dictionary<string, PokemonProfile> PokemonByInternalID = new Dictionary<string, PokemonProfile> { };
        public static Dictionary<string, PokemonProfile> PokemonByName = new Dictionary<string, PokemonProfile> { };

        public static void InitializePokemonLibrary()
        {
            FileInfo[] allPokemon = FileAccess.GetAllFilesInDirectory(FileAccess.PokemonInfoLocation);
            foreach (FileInfo fi in allPokemon)
            {
                StreamReader sr = new StreamReader(fi.FullName);
                try
                {
                    PokemonProfile profile = new PokemonProfile(sr, fi.Name);
                    string identifyingName = profile.IdentifyingName; 
                    PokemonProfile duplicate1 = null, duplicate2 = null;
                    bool alreadyHasInternalID = PokemonByInternalID.TryGetValue(profile.internalID, out duplicate1);
                    bool alreadyHasPokemonOfName = PokemonByInternalID.TryGetValue(profile.IdentifyingName, out duplicate2);

                    if (alreadyHasInternalID)
                    {
                        ErrorHandler.AddErrorToLog($"Warning: Pokemon Library already contains a Pokemon with the ID {profile.internalID}. (Pokemon: {profile.IdentifyingName}, {duplicate1.IdentifyingName}.");
                    }
                    if (alreadyHasPokemonOfName)
                    {
                        ErrorHandler.AddErrorToLog($"Warning: Pokemon Library already contains a Pokemon with the Name {profile.internalID}. (Pokemon: {profile.IdentifyingName}, {duplicate2.IdentifyingName}.");
                    }
                    if (alreadyHasInternalID || alreadyHasPokemonOfName)
                    {
                        continue;
                    }
                    PokemonByInternalID.Add(profile.internalID, profile);
                    PokemonByName.Add(profile.IdentifyingName, profile);
                }
                catch (Exception e)
                {
                    ErrorHandler.AddErrorToLog(e);
                }
                sr.Close();
            }
        }
    }

    public class PokemonProfile
    {
        public readonly string internalID = "";
        public readonly int PokedexNumber;
        public readonly string DisplayName = "";
        public readonly string RegionalVariant = ""; //Used to mark variant names like "alolan rattata"
        public readonly string Species; //the mouse pokemon, happiness pokemon, etc
        public string IdentifyingName { get { return string.Concat(RegionalVariant, " ", DisplayName); } }

        public readonly int size; //size of the pokemon in world blocks. 1 = 1x1x1, 2 = 2x2x1, 3 = 3x3x1, 4 = 4x4x2

        public readonly PokemonType type1 = PokemonType.None, type2 = PokemonType.None;

        public Dictionary<PokemonStats, int> baseStats = new Dictionary<PokemonStats, int>
        {
            {PokemonStats.HP, 100 },
            {PokemonStats.Attack, 100 },
            {PokemonStats.Defense, 100 },
            {PokemonStats.SpAttack, 100 },
            {PokemonStats.SpDefense, 100 },
            {PokemonStats.Speed, 100 },
            {PokemonStats.Evasion, 100 },
            {PokemonStats.Accuracy, 100 }
        };

        public readonly int baseExpReward;

        public PokemonMove[] allAcquirableMoves;

        public PassiveAbility[] NormalAbilities, HiddenAbilities;

        public LevelingSpeed levelingSpeed;

        public enum LevelingSpeed
        {
            Erratic,
            Fast,
            MediumFast,
            MediumSlow,
            Slow,
            Fluctuating
        }

        public enum PokemonStats
        {
            HP,
            Attack,
            Defense,
            SpAttack,
            SpDefense,
            Speed,
            Evasion,
            Accuracy
        }

        PokemonProfile() { } //Here to force generation through file.

        public PokemonProfile(StreamReader sr, string filename)
        {
            PokemonProfile prof = new PokemonProfile();
            string s = null;
            int currentLine = -1;
            while ((s = sr.ReadLine())!= null)
            {
                currentLine++;
                if (s.Length < 4)
                {
                    if (s.Length != 0)
                    {
                        ErrorHandler.AddErrorToLog("Warning: File on pokemon contains invalid line. File: " + filename + ". Line: " + currentLine);
                    }
                    continue;
                }

                string[] split = s.Split(new[] { ' ' }, 2);
                string command = split[0];
                string argument = null;
                if (split.Length > 1)
                {
                    argument = split[1];
                }
                else
                {
                    ErrorHandler.AddErrorToLog("Warning: Generation instruction formatted improperly in file " + filename + ". Line: " + currentLine);
                    continue;
                }

                command = command.ToLower();

                int argInt = 0;
                bool argumentIsInt = int.TryParse(argument, out argInt);

                switch(command)
                {
                    case "iid":
                        internalID = argument;
                        break;
                    case "pdx":
                        if (argumentIsInt)
                        {
                            PokedexNumber = argInt;
                        }
                        else
                        {
                            ErrorHandler.AddErrorToLog("Warning: Non-int passed to int-only parameter in " + filename + ". Line: " + currentLine);
                        }
                        break;
                    case "nam":
                        DisplayName = argument;
                        break;
                    case "spe":
                        Species = argument;
                        break;
                    case "siz":
                        if (argumentIsInt)
                        {
                            size = argInt;
                        }
                        else
                        {
                            ErrorHandler.AddErrorToLog("Warning: Non-int passed to int-only parameter in " + filename + ". Line: " + currentLine);
                        }
                        break;
                    default:
                        ErrorHandler.AddErrorToLog("Warning: Unrecognized command "+ command + "! File: " + filename + ". Line: " + currentLine);
                        break;
                }
            }
        }

        public enum PokemonType
        {
            None,
            Normal, 
            Fire,
            Water,
            Grass,
            Electric,
            Ice,
            Fighting,
            Poison,
            Ground,
            Flying,
            Psychic,
            Bug,
            Rock,
            Ghost,
            Dark,
            Dragon,
            Steel,
            Fairy
        }
    }

    public class MoveProfile
    {
        //Contains move data, as well as conditions on how/when it's learned
    }
}
