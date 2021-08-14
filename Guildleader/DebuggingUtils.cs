using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Guildleader
{
    public static class DebuggingUtils
    {
        public static void PrintByteArray(byte[] list)
        {
            string s = ConvertBytesToReadableString(list);
            Console.WriteLine(s);
        }

        public static string ConvertBytesToReadableString(byte[] list)
        {
            StringBuilder sb = new StringBuilder("");
            foreach (byte b in list)
            {
                sb.Append(b);
                sb.Append('|');
            }
            return sb.ToString();
        }
    }
}
