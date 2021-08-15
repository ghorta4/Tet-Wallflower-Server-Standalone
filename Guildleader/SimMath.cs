using System;
using System.Collections.Generic;
using System.Text;

namespace Guildleader
{
    public class Int2
    {
        public int x, y;

        public Int2() { }
        public Int2(int xg, int yg)
        {
            x = xg; y = yg;
        }

        public static Int2 zero{
            get
            {
                return new Int2();
            }
        }
    }

    public class Int3
    {
        public int x, y, z;

        public Int3() { }
        public Int3(int xg, int yg, int zg)
        {
            x = xg; y = yg; z = zg;
        }

        public static Int3 zero
        {
            get
            {
                return new Int3();
            }
        }

        public static Int3 operator +(Int3 a, Int3 b)
        {
            return new Int3(a.x + b.x, a.y+b.y, a.z+b.z);
        }
    }

    public static class RNG
    {
        public static float heightRetrieverBasedOnPosition(int x, int y, int seed, int bandwith, float mountainHeight)
        {
            float fraction = (float)seed / int.MaxValue;

            float height = (perlinAtPosition(x, y, fraction));

            int usedbandwidth = (int)(bandwith + Math.Round(bandwith * 0.25f * Math.Sin(fraction * 98)));
            height += interpPerlinNoiseAtPosition(x, y, fraction * 2, usedbandwidth / 2 + 2) * mountainHeight;
            height += interpPerlinNoiseAtPosition(x, y, fraction * 3, usedbandwidth) * 2 * mountainHeight;
            height += interpPerlinNoiseAtPosition(x, y, fraction * 4, usedbandwidth * 2 + 3) * 3 * mountainHeight;
            return height;
        }

        public static float interpPerlinNoiseAtPosition(int x, int y, float seed, int blockSize)
        {
            float noiseOfSquare = perlinAtPosition(x / blockSize, y / blockSize, seed);
            int halfBlock = blockSize / 2;
            float nearbyNoisex = (perlinAtPosition((x + halfBlock) / blockSize, y / blockSize, seed) + perlinAtPosition((x - halfBlock) / blockSize, y / blockSize, seed) - noiseOfSquare);
            float nearbyNoisey = (perlinAtPosition(x / blockSize, (y + halfBlock) / blockSize, seed) + perlinAtPosition(x / blockSize, (y - halfBlock) / blockSize, seed) - noiseOfSquare);
            float diffx = ((Math.Abs(x) % blockSize) - blockSize / 2);
            float diffy = ((Math.Abs(y) % blockSize) - blockSize / 2);
            float distance = (float)(Math.Sqrt(Math.Pow(diffx, 2) + Math.Pow(diffy, 2)) / (blockSize / 2 * Math.Sqrt(2)));

            nearbyNoisex -= noiseOfSquare;
            nearbyNoisey -= noiseOfSquare;
            noiseOfSquare += (diffx * nearbyNoisex) / blockSize * (1 - (diffy / blockSize));
            noiseOfSquare += (diffy * nearbyNoisey) / blockSize * (1 - (diffx / blockSize));
            noiseOfSquare *= 1 - distance;
            noiseOfSquare *= 1 - distance;
            return noiseOfSquare;
        }
        public static float perlinAtPosition(int x, int y, float seed)
        {
            float p1 = randomPositionBasedFloat(x, y, seed);
            float p2 = randomPositionBasedFloat(x + 1, y, seed);
            float p3 = randomPositionBasedFloat(x, y + 1, seed);
            float p4 = randomPositionBasedFloat(x + 1, y + 1, seed);

            double temp = Math.Sin(p1);
            temp += Math.Cos(p2);
            temp -= Math.Sin(p3);
            temp -= Math.Cos(p4);

            temp /= 4;

            return (float)temp;
        }
        public static float randomPositionBasedFloat(int x, int y, float seed)
        {
            float xMod = seed * (x << 4 ^ x ^ (y << 5)) % 0.1f, yMod = seed * (y >> 8 ^ y ^ (x << 5)) % 0.412f;
            return (float)Math.Sin(x * xMod * 600 + y * yMod * 50 + 25 * seed + x * y * xMod * yMod);
        }
        static int lastInt;
        public static float texturePositionFloat(int x, int y, int z, int seed)
        {
            float f = (float)Math.Sin(5.006f * x + 4963.1f * y + z * seed / 1000f);
            return (f + 1) % 0.25f * 4;
        }
        public static int positionBasedQuickInt(int x, int y, int z, int seed)
        {
            return ((x + y << 4 + z << 8) ^ (seed << (x & 0x0F) + (3 * y & 0x0F) + 5 * (z & 0x0F))) << 8;
        }
    }

}
