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

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
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
            if (length.Equals(Common.Binary.Zero)) return null;
            
            if (length <= -1) length = str.Length;

            if (start > length - start) throw new System.ArgumentOutOfRangeException("start");

            if (length > length - start) throw new System.ArgumentOutOfRangeException("length");

            System.Collections.Generic.List<byte> result = new System.Collections.Generic.List<byte>(length >> 1); // / 2
            
            //Dont check the results for overflow
            unchecked
            {
                //Iterate the pointer using the managed length ....
                //Todo, optomize with reverse or i - 1
                for (int i = start, e = length; i < e; i += 2)
                {
                    //to reduce string manipulations pre call
                    //while (str[i] == '-') i++;

                    //Todo, Native and Unsafe
                    //Convert 2 Chars to a byte
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
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static byte[] ConvertToBytes(this string hex) { return string.IsNullOrWhiteSpace(hex) ? Media.Common.MemorySegment.EmptyBytes : HexStringToBytes(hex); }

        public static string Substring(this string source, string pattern, System.StringComparison comparison = System.StringComparison.OrdinalIgnoreCase)
        {
            return Substring(source, 0, -1, pattern, comparison);
        }

        /// <summary>
        /// Given a source string find the pattern and extract the data thereafter.
        /// </summary>
        /// <param name="source">The source <see cref="System.String"/></param>
        /// <param name="startIndex">in <paramref name="source"/></param>
        /// <param name="count">from <paramref name="startIndex"/>, ensured to result in a value outside of the length of <paramref name="source"/></param>
        /// <param name="pattern">The <see cref="System.String"/> to find in <paramref name="source"/></param>
        /// <param name="comparison"><see cref="System.StringComparison"/></param>
        /// <returns>
        /// <see cref="String.Empty"/> if no result was found or <paramref name="source"/> was null or empty. 
        /// When <paramref name="pattern"/> is null or empty <paramref name="source"/> is returned. 
        /// Otherwise the <see cref="System.String"/> which does not include the <paramref name="pattern"/>
        /// </returns>
        /// <remarks>8 bytes in worst case space complexity, time complexity is O(count) in worst cast</remarks>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        //refs
        public static string Substring(this string source, int startIndex, int count, string pattern, System.StringComparison comparison = System.StringComparison.OrdinalIgnoreCase) //Ordinal is faster.
        {
            if (string.IsNullOrEmpty(source)) return string.Empty;
            else if (string.IsNullOrEmpty(pattern)) return source;

            int sourceLength = source.Length;

            //Ensure the start is within the string.
            if (startIndex > sourceLength) startIndex -= sourceLength;

            int patternLength = pattern.Length;

            //Use the source length when count is negitive
            if (count < Common.Binary.Zero) count = sourceLength;

            //Only match up to the length of the source.
            count = Binary.Max(ref count, ref sourceLength);

            //Ensure the startIndex and count are within range.
            if (startIndex + count > sourceLength) count -= startIndex;

            //Determine where in the source string the substring resides
            startIndex = source.IndexOf(pattern, startIndex, count, comparison);

            //The substring must be within the source after the length of the pattern.
            return startIndex >= Common.Binary.Zero && startIndex <= sourceLength ? source.Substring(startIndex + patternLength) : string.Empty;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static string[] SplitTrim(this string ex, string[] seperator, int count, System.StringSplitOptions options)
        {
            if (count.Equals(Common.Binary.Zero) || Common.Extensions.Array.ArrayExtensions.IsNullOrEmpty(seperator)) return new string[Common.Binary.Zero];

            string[] results = ex.Split(seperator, count, options);

            for (int i = results.Length - 1; i >= Common.Binary.Zero; --i) results[i] = results[i].Trim();

            return results;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static string[] SplitTrim(this string ex, char[] seperator, int count, System.StringSplitOptions options)
        {
            if (count.Equals(Common.Binary.Zero) || Common.Extensions.Array.ArrayExtensions.IsNullOrEmpty(seperator)) return new string[0];

            string[] results = ex.Split(seperator, count, options);

            for (int i = results.Length - 1; i >= Common.Binary.Zero; --i) results[i] = results[i].Trim();

            return results;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static string[] SplitTrimEnd(this string ex, string[] seperator, int count, System.StringSplitOptions options)
        {
            if (count.Equals(Common.Binary.Zero) || Common.Extensions.Array.ArrayExtensions.IsNullOrEmpty(seperator)) return new string[Common.Binary.Zero];

            string[] results = ex.Split(seperator, count, options);

            for (int i = results.Length - 1; i >= Common.Binary.Zero; --i) results[i] = results[i].TrimEnd();

            return results;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static string[] SplitTrimEnd(this string ex, char[] seperator, int count, System.StringSplitOptions options)
        {
            if (count.Equals(Common.Binary.Zero) || Common.Extensions.Array.ArrayExtensions.IsNullOrEmpty(seperator)) return new string[Common.Binary.Zero];

            string[] results = ex.Split(seperator, count, options);

            for (int i = results.Length - 1; i >= Common.Binary.Zero; --i) results[i] = results[i].TrimEnd();

            return results;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static string[] SplitTrimStart(this string ex, string[] seperator, int count, System.StringSplitOptions options)
        {
            if (count.Equals(Common.Binary.Zero) || Common.Extensions.Array.ArrayExtensions.IsNullOrEmpty(seperator)) return new string[Common.Binary.Zero];

            string[] results = ex.Split(seperator, count, options);

            for (int i = results.Length - 1; i >= Common.Binary.Zero; --i) results[i] = results[i].TrimStart();

            return results;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static string[] SplitTrimStart(this string ex, char[] seperator, int count, System.StringSplitOptions options)
        {
            if (count.Equals(Common.Binary.Zero) || Common.Extensions.Array.ArrayExtensions.IsNullOrEmpty(seperator)) return new string[Common.Binary.Zero];

            string[] results = ex.Split(seperator, count, options);

            for (int i = results.Length - 1; i >= Common.Binary.Zero; --i) results[i] = results[i].TrimStart();

            return results;
        }

    }
}
