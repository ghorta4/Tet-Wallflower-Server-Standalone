﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Guildleader
{
    public class SingleWorldTile
    {
        public short tileID; //0 is empty
        public short tileHealth = 10;//for certain blocks, points to an entitydata
        public short variant;

        public SingleWorldTile(short id, Int3 pos)
        {
            tileID = id;
            TileProperties tp = TileLibrary.tileLib[id];
            tileHealth = tp.maxHealth;
            float variantTest = RNG.texturePositionFloat(pos.x, pos.y, pos.z, WorldManager.currentWorld.seed) * TileLibrary.tileLib[tileID].totalVariantWeight;
            int chosenVariant = 0;
            while (variantTest > tp.variantsAndWeights[chosenVariant].Item2)
            {
                variantTest -= tp.variantsAndWeights[chosenVariant].Item2;
                chosenVariant++;
            }
            variant = (short)chosenVariant;
        }

        public byte[] GetBytesV1()
        {
            List<byte> bytes = new List<byte> { };
            bytes.AddRange(Convert.ToByte(tileID));
            bytes.AddRange(Convert.ToByte(tileHealth));
            return bytes.ToArray();
        }
        public byte[] GetBytesSimpleV1()
        {
            List<byte> bytes = new List<byte> { };
            bytes.AddRange(Convert.ToByte(tileID));
            return bytes.ToArray();
        }

        public static SingleWorldTile BytesToTileV1(List<byte> data, Int3 pos)
        {
            short tileID = QuickShort(data);
            SingleWorldTile blank = new SingleWorldTile(tileID, pos);
            data.RemoveRange(0, sizeof(short));
            blank.tileHealth = QuickShort(data);
            data.RemoveRange(0, sizeof(short));
            return blank;
        }
        public static SingleWorldTile BytesToTileV1Simple(List<byte> data, Int3 pos)
        {
            short tileID = QuickShort(data);
            SingleWorldTile blank = new SingleWorldTile(tileID, pos);
            data.RemoveRange(0, sizeof(short));
            return blank;
        }

        public static short QuickShort(List<byte> data)
        {
            if (BitConverter.IsLittleEndian)
            {
                return (short)(data[1] | (data[0] << 8));
            }
            else
            {
                return (short)(data[0] | (data[1] << 8));
            }
            
        }

        public TileProperties Properties
        {
            get
            {
                return TileLibrary.tileLib[tileID];
            }
        }
    }
}
