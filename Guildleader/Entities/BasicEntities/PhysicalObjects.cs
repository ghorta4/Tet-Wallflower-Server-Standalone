using System;
using System.Collections.Generic;
using System.Text;
using Guildleader.Entities;

namespace Guildleader.Entities.BasicEntities
{
    public class PhysicalObject : Entity
    {
        public virtual Int3 Size { get { return Int3.One; } }
        public int Durability;

        public PhysicalObject() { }

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
            holster.AddRange(Convert.ToByte(Durability));
            return holster.ToArray();
        }

        public override void ReadEntityFromBytesClient(List<byte> data)
        {
            base.ReadEntityFromBytesClient(data);
            ProcessPhysicalObjectData(data);
        }
        public override void ReadEntityFromBytesServer(List<byte> data)
        {
            base.ReadEntityFromBytesServer(data);
            ProcessPhysicalObjectData(data);
        }

        void ProcessPhysicalObjectData(List<byte> data)
        {
            int[] extracted = Convert.ExtractInts(data, 1);
            Durability = extracted[0];
        }
    }
}
