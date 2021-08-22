using System;
using System.Collections.Generic;
using System.Text;

namespace Guildleader
{
    public static class MovesLibrary
    {
        public static Dictionary<int, PokemonMove> MovesByInternalID = new Dictionary<int, PokemonMove> { };
        public static Dictionary<string, PokemonMove> MovesByName = new Dictionary<string, PokemonMove> { };

        public static void LoadMovesLibrary()
        {

        }
    }

    public class PokemonMove {
        public readonly int InternalID;

        public readonly string MoveName;

        public readonly int BasePower;
        public readonly PokemonProfile.PokemonType MoveType;

        public string[] tags;
    }
}
