using System;
using System.Collections.Generic;
using System.Text;
using Guildleader.Entities;

namespace Guildleader.Entities.BasicEntities
{
    public class PhysicalObjects : Entity
    {
        public virtual Int3 Size { get { return Int3.One; } }
        public int Durability;

        public override byte[] ConvertToBytesForDataStorage()
        {
            return PhysicalObjectConversions();
        }
        public override byte[] ConvertToBytesForClient()
        {
            return PhysicalObjectConversions();
        }

        byte[] PhysicalObjectConversions()
        {
            List<byte> holster = new List<byte>(base.ConvertToBytesForDataStorage());
            holster.AddRange(Convert.ToByte(Size.x));
            holster.AddRange(Convert.ToByte(Size.y));
            holster.AddRange(Convert.ToByte(Size.z));
            holster.AddRange(Convert.ToByte(Durability));
            return holster.ToArray();
        }

        
    }
}
