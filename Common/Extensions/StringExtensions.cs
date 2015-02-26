using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Common.Extensions.String
{
    public static class StringExtensions
    {
        /// <summary>
        /// See <see cref="Utility.HexStringToBytes"/>
        /// </summary>
        /// <param name="hex"></param>
        /// <returns></returns>
        public static byte[] ConvertToBytes(this string hex) { return string.IsNullOrWhiteSpace(hex) ? Utility.Empty : Utility.HexStringToBytes(hex); }

        /// <summary>
        /// Attempts to modify the given string at the given index with the new value.
        /// </summary>
        /// <param name="toModify"></param>
        /// <param name="index"></param>
        /// <param name="newValue"></param>
        public unsafe static bool UnsafeTryModifyString(string toModify, int index, char newValue)
        {
            try
            {
                fixed (char* str = toModify)
                {
                    str[index] = newValue;

                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
