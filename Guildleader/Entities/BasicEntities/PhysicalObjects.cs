﻿using System;
using System.Collections.Generic;
using System.Text;
using Guildleader.Entities;

namespace Guildleader.Entities.BasicEntities
{
    public abstract class PhysicalObject : Entity
    {
        public virtual Int3 Size { get { return Int3.One; } }
        public int Durability;

        public abstract string GetSpriteName();
        string stringRetrievedFromServer;

        public override byte[] ConvertToBytesForDataStorage()
        {
            return PhysicalObjectConversions();
        }
        public override byte[] ConvertToBytesForClient()
        {
            List<byte> temp = new List<byte> (PhysicalObjectConversions());
            temp.AddRange(Convert.ToByte(GetSpriteName()));
            return temp.ToArray();
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
            string[] strings = Convert.ExtractStrings(data, 1);
            stringRetrievedFromServer = strings[0];
        }

        //Methods

        //The below function is used to move entities without entering impassible blocks.
        public void GentleShove(Int3 direction, int maxSlopeHeight)
        {
            Int3 endPosition = worldPositon;
            int maxChecks = (int)Math.Round((double)Math.Min(10, (int)direction.Magnitude + 1));
            int numberOfTimesLifted = 0;
            for (int i = 0; i <= maxChecks; i++)
            {
                Int3 targetPos = worldPositon + i * direction / maxChecks + new Int3(0, 0, 1) * (numberOfTimesLifted);
                SingleWorldTile[,,] occupiedArea = WorldManager.currentWorld.GetAllTilesInArea(targetPos - new Int3(0, 0, 1) * maxSlopeHeight, Size + new Int3(0, 0, 1) * maxSlopeHeight * 2);
                int mostAppropriateZHeight = -9999999;
                for (int z = 0; z < occupiedArea.GetLength(2); z++)
                {
                    for (int x = 0; x < occupiedArea.GetLength(0); x++)
                    {
                        for (int y = 0; y < occupiedArea.GetLength(1); y++)
                        {
                            SingleWorldTile swt = occupiedArea[x, y, z];
                            if (!swt.properties.tags.Contains("nonsolid"))
                            {
                                goto blockBreak;
                            }
                        }
                    }
                    if (Math.Abs(z - maxSlopeHeight) < Math.Abs(mostAppropriateZHeight - maxSlopeHeight))
                    {
                        mostAppropriateZHeight = z;
                    }
                    numberOfTimesLifted = mostAppropriateZHeight - maxSlopeHeight;
                    blockBreak:;
                }
                if (mostAppropriateZHeight < -maxSlopeHeight)
                {
                    //could not find space to shove!
                    break;
                }
                endPosition = targetPos + new Int3(0, 0, 1) * (numberOfTimesLifted);
            }
            worldPositon = endPosition;
        }
    }
}
