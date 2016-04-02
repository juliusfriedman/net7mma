#region Copyright
/*
This file came from Managed Media Aggregation, You can always find the latest version @ https://net7mma.codeplex.com/
  
 Julius.Friedman@gmail.com / (SR. Software Engineer ASTI Transportation Inc. http://www.asti-trans.com)

Permission is hereby granted, free of charge, 
 * to any person obtaining a copy of this software and associated documentation files (the "Software"), 
 * to deal in the Software without restriction, 
 * including without limitation the rights to :
 * use, 
 * copy, 
 * modify, 
 * merge, 
 * publish, 
 * distribute, 
 * sublicense, 
 * and/or sell copies of the Software, 
 * and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * 
 * 
 * JuliusFriedman@gmail.com should be contacted for further details.

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. 
 * 
 * IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, 
 * DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, 
 * TORT OR OTHERWISE, 
 * ARISING FROM, 
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 * 
 * v//
 */
#endregion

namespace Media.Common.Extensions.String
{
    public static class StringExtensions
    {
        public const string UnknownString = "Unknown";

        //Standard Numeric Format Strings
        //https://msdn.microsoft.com/en-us/library/dwhawy9k(v=vs.110).aspx

        //Custom Numeric Format Strings
        //https://msdn.microsoft.com/en-us/library/0c899ak8(v=vs.110).aspx

        public const string HexadecimalFormat = "X";


        #region Hex Functions

        public static byte HexCharToByte(char c) { c = char.ToUpperInvariant(c); return (byte)(c > '9' ? c - 'A' + 10 : c - '0'); }

        /// <summary>
        /// Converts a String in the form 0011AABB to a Byte[] using the chars in the string as bytes to caulcate the decimal value.
        /// </summary>
        /// <notes>
        /// Reduced string allocations from managed version substring
        /// About 10 milliseconds faster then Managed when doing it 100,000 times. otherwise no change
        /// </notes>
        /// <param name="str"></param>
        /// <returns></returns>
        public static byte[] HexStringToBytes(string str, int start = 0, int length = -1)
        {
            if (length == 0) return null;
            
            if (length <= -1) length = str.Length;

            if (start > length - start) throw new System.ArgumentOutOfRangeException("start");

            if (length > length - start) throw new System.ArgumentOutOfRangeException("length");

            System.Collections.Generic.List<byte> result = new System.Collections.Generic.List<byte>(length / 2);
            
            //Dont check the results for overflow
            unchecked
            {
                //Iterate the pointer using the managed length ....
                for (int i = start, e = length; i < e; i += 2)
                {
                    //to reduce string manipulations pre call
                    //while (str[i] == '-') i++;

                    //Conver 2 Chars to a byte
                    result.Add((byte)(HexCharToByte(str[i]) << 4 | HexCharToByte(str[i + 1])));
                }
            }

            //Dont use a List..

            //Return the bytes
            return result.ToArray();
        }

        #endregion

        /// <summary>
        /// See <see cref="Media.Common.Extensions.String.StringExtensions.HexStringToBytes"/>
        /// </summary>
        /// <param name="hex"></param>
        /// <returns></returns>
        public static byte[] ConvertToBytes(this string hex) { return string.IsNullOrWhiteSpace(hex) ? Media.Common.MemorySegment.EmptyBytes : HexStringToBytes(hex); }
    }
}
