using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

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
        public bool needsChunksResent = true;
        public void ProcessDebugCommand(byte[] command)
        {
            List<byte> holster = new List<byte>(command);

            int[] values = Convert.ExtractInts(holster, 3);

            GentleShove(new Int3(values[0], values[1], values[2]), 0);
        }

        public override void ActionsOnChunkChange()
        {
            needsChunksResent = true;
        }

        public enum PlayerCommand : byte
        {
            invalid,
            move,
            dig,
            useMove,
        }
        public void ProcessTypicalPlayerCommand(byte[] command)
        {
            if (command == null || command.Length <= 0)
            {
                return;
            }
            byte[] content = command.Skip(1).Take(command.Length - 1).ToArray();
            PlayerCommand action = (PlayerCommand)command[0];
            switch(action)
            {
                case PlayerCommand.dig:
                    ProcessPlayerDigCommand(command);
                    break;
                case PlayerCommand.invalid:
                default:
                    break;
            }
        }

        void ProcessPlayerDigCommand(byte[] command)
        {
            if (command.Length < sizeof(int) * 3)
            {
                return;
            }

            int[] vals = Convert.ExtractInts(new List<byte>(command), 3);

            Int3 targetPos = new Int3(vals[0], vals[1], vals[2]);

            EntityAction digAction = new EntityAction(this, 2f, 0, 0.2f);
            digAction.details.targetCoords = targetPos;
            digAction.postStartupAction = BasicEntityFunctions.DigBlock;
        }
    }
}
