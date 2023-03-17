using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hackerman
{
    internal static class Decoder
    {
        public static string Decode(byte[] input)
        {
            byte[] cleanByte = input.Where(x => x != 00).ToArray();
            UTF8Encoding Encoding = new UTF8Encoding();
            return Encoding.GetString(cleanByte);
        }
        public static byte[] Encode(string input)
        {
            UTF8Encoding Encoding = new UTF8Encoding();
            byte[] cleanByte = Encoding.GetBytes(input).Where(x => x != 00).ToArray();
            return cleanByte;
        }
    }
}
