using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ch.aigis.qubic.lib.Helper
{
    /// <summary>
    /// Legacy Utils Collection
    /// </summary>
    public static class Utils
    {

        public static GroupCollection RegexGetGroups(string input, string pattern)
        {
            Regex r = new Regex(pattern);

            // Match the regular expression pattern against a text string.
            Match m = r.Match(input);
            while (m.Success)
            {
                return m.Groups;
            }
            return null;
        }

        public static string RegexGetCapture(string input, string pattern, int group = 1)
        {
            Regex r = new Regex(pattern);

            // Match the regular expression pattern against a text string.
            Match m = r.Match(input);
            while (m.Success)
            {
                Group g = m.Groups[group];
                if (g != null && g.Success)
                {
                    return g.Captures.First().Value;
                }
            }
            return "";
        }

        /// <summary>
        /// returns a timestamp in format: yyyy-MM-dd HH:mm:ss
        /// </summary>
        /// <returns></returns>
        public static string TimeStamp()
        {
            return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        /// <summary>
        /// returns a timestamp in format: yyyy-MM-dd_HH-mm-ss
        /// </summary>
        /// <returns></returns>
        public static string FileTimeStamp()
        {
            return DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        }

        public static byte[] ConvertBinaryStringToBytes(string binary)
        {
            if (binary == null || binary.Length % 8 != 0)
                throw new ArgumentException("Binary string must be a multiple of 8 bits.");

            byte[] byteArray = new byte[binary.Length / 8];

            for (int i = 0; i < binary.Length; i += 8)
            {
                byteArray[i / 8] = Convert.ToByte(binary.Substring(i, 8), 2);
            }

            return byteArray;
        }
    }
}
