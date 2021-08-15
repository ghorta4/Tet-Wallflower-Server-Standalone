using System.Collections;
using System.Collections.Generic;
using System.Text;
using System;
using System.Net;
using System.Linq;

namespace Guildleader
{
    public static class primitiveByteConverter
    {
        public static byte[] ConvertDataToBytes(List<string> strings, List<float> floats, List<int> ints, List<byte> bytes)
        {
            List<byte> holster = new List<byte>();

            if (strings == null) strings = new List<string> { };
            if (floats == null) floats = new List<float> { };
            if (ints == null) ints = new List<int> { };
            if (bytes == null) bytes = new List<byte> { };

            holster.Add((byte)strings.Count);
            holster.Add((byte)floats.Count);
            holster.Add((byte)ints.Count);

            foreach(string s in strings)
            {
                holster.AddRange(Convert.ToByte(s));
            }
            foreach (float f in floats)
            {
                holster.AddRange(Convert.ToByte(f));
            }

            foreach (int i in ints)
            {
                holster.AddRange(Convert.ToByte(i));
            }

            holster.AddRange(bytes);

            return holster.ToArray();
        }

        public static primitiveDataPack ConvertBytesToPrimitiveDataPack(byte[] data)
        {
            List<byte> converted = new List<byte>();
            converted.AddRange(data);
            primitiveDataPack pdp = new primitiveDataPack();

            byte[] sizes = new byte[3];

            for (int i = 0; i < sizes.Length; i++)
            {
                sizes[i] = converted[0];
                converted.RemoveAt(0);
            }

            for (int i = 0; i < sizes[0]; i++)
            {
                int stringLength = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(converted.ToArray(), 0));
                converted.RemoveRange(0, sizeof(int));
                pdp.strings.Add(Encoding.UTF8.GetString(converted.ToArray(), 0, stringLength));
                converted.RemoveRange(0, stringLength);
            }

            for (int i = 0; i < sizes[1]; i++)
            {
                byte[] subsection = converted.Take(sizeof(float)).ToArray();
                if (BitConverter.IsLittleEndian)
                {
                    subsection = subsection.Reverse().ToArray();
                }
                pdp.floats.Add(BitConverter.ToSingle(subsection, 0));
                converted.RemoveRange(0, sizeof(float));
            }

            for (int i = 0; i < sizes[2]; i++)
            {
                pdp.ints.Add(IPAddress.NetworkToHostOrder(BitConverter.ToInt32(converted.ToArray(), 0)));
                converted.RemoveRange(0, sizeof(int));
            }

            pdp.bytes = converted;

            return pdp;
        }
    }

    public class primitiveDataPack
    {
        public List<string> strings = new List<string>();
        public List<float> floats = new List<float>();
        public List<int> ints = new List<int>();
        public List<byte> bytes = new List<byte>();
    }

    public static class Convert
    {
        public static byte[] ToByte(int n)
        {
            return BitConverter.GetBytes(IPAddress.HostToNetworkOrder(n));
        }
        public static byte[] ToByte(short n)
        {
            return BitConverter.GetBytes(IPAddress.HostToNetworkOrder(n));
        }
        public static byte[] ToByte(long n)
        {
            return BitConverter.GetBytes(IPAddress.HostToNetworkOrder(n));
        }
        public static byte[] ToByte(float n)
        {
            byte[] data = BitConverter.GetBytes(n);
            if (BitConverter.IsLittleEndian)
            {
                data = data.Reverse().ToArray();
            }
            return data;
        }
        public static byte[] ToByte(string s)
        {
            List<byte> holster = new List<byte>();
            byte[] message = Encoding.UTF8.GetBytes(s);
            holster.AddRange(ToByte(message.Length));
            holster.AddRange(message);
            return holster.ToArray();
        }

        public static int ToInt(byte[] b, int startIndex)
        {
            return IPAddress.NetworkToHostOrder(BitConverter.ToInt32(b, startIndex));
        }
        public static int ToInt(byte[] b)
        {
            return ToInt(b,0);
        }
        public static short ToShort(byte[] b, int startIndex)
        {
            return IPAddress.NetworkToHostOrder(BitConverter.ToInt16(b, startIndex));
        }
        public static short ToShort(byte[] b)
        {
            return ToShort(b, 0);
        }
        public static long ToLong(byte[] b, int startIndex)
        {
            return IPAddress.NetworkToHostOrder(BitConverter.ToInt64(b, startIndex));
        }
        public static long ToLong(byte[] b)
        {
            return ToLong(b, 0);
        }

        public static float ToFloat(byte[] b, int startIndex)
        {
            byte[] subSection = b.Skip(startIndex).Take(sizeof(float)).ToArray();
            if (BitConverter.IsLittleEndian)
            {
                subSection = subSection.Reverse().ToArray();
            }
            return BitConverter.ToSingle(subSection, 0);
        }
        public static float ToFloat(byte[] b)
        {
            return ToFloat(b, 0);
        }

        public static string ToString(byte[] b, int startIndex)
        {
            int length = ToInt(b, startIndex);
            byte[] subsection = b.Skip(startIndex + sizeof(int)).Take(length).ToArray();
            return Encoding.UTF8.GetString(subsection, 0, subsection.Length);
        }

        public static byte ToByte(bool[] source)
        {
            byte result = 0;
            // This assumes the array never contains more than 8 elements!
            int index = 8 - source.Length;

            // Loop through the array
            foreach (bool b in source)
            {
                // if the element is 'true' set the bit at that position
                if (b)
                    result |= (byte)(1 << (7 - index));

                index++;
            }

            return result;
        }

        public static bool[] ToBoolArray(byte b)
        {
            // prepare the return result
            bool[] result = new bool[8];

            // check each bit in the byte. if 1 set to true, if 0 set to false
            for (int i = 0; i < 8; i++)
                result[i] = (b & (1 << i)) == 0 ? false : true;

            // reverse the array
            Array.Reverse(result);

            return result;
        }
    }
}
