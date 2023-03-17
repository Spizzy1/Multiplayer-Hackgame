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
            return Encoding.GetString(input);
        }
        public static byte[] Encode(string input)
        {
            UTF8Encoding Encoding = new UTF8Encoding();
            return Encoding.GetBytes(input);
        }
    }
}
