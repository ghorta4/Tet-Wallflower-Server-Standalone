using System;
using System.Collections.Generic;
using System.Text;

namespace Guildleader
{
    public static class PassiveAbilityLibrary
    {
        public static Dictionary<int, PokemonMove> AbilitiesByInternalID = new Dictionary<int, PokemonMove> { };
        public static Dictionary<string, PokemonMove> AbilitiesByName = new Dictionary<string, PokemonMove> { };

        public static void LoadAbilityLibrary()
        {

        }
    }

    public class PassiveAbility
    {
        public readonly int InternalID;

        public readonly string AbilityName;
    }
}
