using System;
using System.Collections.Generic;
using System.Text;

namespace Guildleader.Entities.BasicEntities
{
    class Pokemon : Actors
    {
        //for Pokemon, they have an 'HP' that acts as a buffer for their health. Basically... Drop to 0 HP and you faint, drop to 0 health and you die.
        public int CurrentHealth = 10, maxHealth = 10;

        public override string GetSpriteName()
        {
            throw new NotImplementedException();
        }

        public override byte[] ConvertToBytesForDataStorage()
        {
            return base.ConvertToBytesForDataStorage();
        }
        public override byte[] ConvertToBytesForClient()
        {
            return base.ConvertToBytesForClient();
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
}
