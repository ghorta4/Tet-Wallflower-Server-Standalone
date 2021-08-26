using System;
using System.Collections.Generic;
using System.Text;

namespace Guildleader.Entities.BasicEntities
{
    public class Pokemon : Actors
    {
        //for Pokemon, they have an 'HP' that acts as a buffer for their health. Basically... Drop to 0 HP and you faint, drop to 0 health and you die.
        public int CurrentHealth = 10, maxHealth = 10;

        public string PokemonProfileID; //by internal ID
        PokemonProfile Profile { get { return PokemonLibrary.PokemonByInternalID[PokemonProfileID]; } }

        public override string GetSpriteName()
        {
            return PokemonLibrary.PokemonByInternalID[PokemonProfileID].SpriteName;
        }

        public override byte[] ConvertToBytesForDataStorage()
        {
            return base.ConvertToBytesForDataStorage();
        }

        public override void ReadEntityFromBytesServer(List<byte> data)
        {
            base.ReadEntityFromBytesServer(data);
        }
        public override void ReadEntityFromBytesClient(List<byte> data)
        {
            base.ReadEntityFromBytesClient(data);
        }
    }

    public class PlayerPokemon : Pokemon
    {
        public void ProcessDebugCommand(byte[] command)
        {
            ErrorHandler.AddMessageToLog("command GET");
            List<byte> holster = new List<byte>(command);

            int[] values = Convert.ExtractInts(holster, 3);

            GentleShove(new Int3(values[0], values[1], values[2]), 0);
        }
    }
}
