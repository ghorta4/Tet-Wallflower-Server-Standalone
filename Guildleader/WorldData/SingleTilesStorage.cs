using System;
using System.Collections.Generic;
using System.Text;

namespace Guildleader
{
    public class SingleWorldTile
    {
        public short tileID; //0 is empty
        public short tileHealth = 10;//for liquids, this means its entity number (that represents its contents)
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

        public byte[] getBytesV1()
        {
            List<byte> bytes = new List<byte> { };
            bytes.AddRange(Convert.ToByte(tileID));
            bytes.AddRange(Convert.ToByte(tileHealth));
            //bytes.AddRange(BitConverter.GetBytes(variant));
            return bytes.ToArray();
        }

        public static SingleWorldTile bytesToTileV1(List<byte> data, Int3 pos)
        {
            short tileID = quickUshort(data);
            SingleWorldTile blank = new SingleWorldTile(tileID, pos);
            data.RemoveRange(0, sizeof(ushort));
            blank.tileHealth = quickUshort(data);
            data.RemoveRange(0, sizeof(ushort));
            // blank.variant = quickUshort(data);
            // data.RemoveRange(0, sizeof(ushort));
            return blank;
        }

        public static short quickUshort(List<byte> data)
        {
            return (short)(data[0] | (data[1] << 8));
        }

        public TileProperties properties
        {
            get
            {
                return TileLibrary.tileLib[tileID];
            }
        }
    }
}
