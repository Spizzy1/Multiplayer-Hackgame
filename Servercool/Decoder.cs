using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    internal static class Decoder
    {
        public static string Decode(byte[] input)
        {
            UTF8Encoding Encoding = new UTF8Encoding();
            return Encoding.GetString(input.Where(x => x != 00).ToArray());
        }
        public static byte[] Encode(string input)
        {
            UTF8Encoding Encoding = new UTF8Encoding();
            byte[] cleanByte = Encoding.GetBytes(input).Where(x => x != 00).ToArray();
            return Encoding.GetBytes(input);
        }
    }
}
