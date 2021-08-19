using System;
using System.Collections.Generic;
using System.Text;

namespace Guildleader
{
    public static class PokemonLibrary
    {
        public static Dictionary<string, PokemonProfile> PokemonByInternalID = new Dictionary<string, PokemonProfile> { };
        public static Dictionary<string, PokemonProfile> PokemonByName = new Dictionary<string, PokemonProfile> { };

        public static void InitializePokemonLibrary()
        {

        }
    }

    public class PokemonProfile
    {
        public readonly string internalID;
        public readonly string DisplayName;
        public readonly string Species; //the mouse pokemon, happiness pokemon, etc

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

        public PokemonProfile GenerateFromFile()
        {

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
}
