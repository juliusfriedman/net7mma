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

namespace Media.Common
{
    public sealed class Unicode
    {

        
        /// <summary>
        /// ISO 8859-1 characters with the most-significant bit set are represented as 1100001x 10xxxxxx. (See RFC 2279 [21])
        /// </summary>
        const byte SurrogateMask = 0xC3; //11000011

        /// <summary>
        /// Checks the first two bits and the last two bits of each byte while moving the count to the correct position while doing so.
        /// The function does not check array bounds or preserve the stack and prevents math overflow.
        /// </summary>
        /// <param name="buffer">The array to check</param>
        /// <param name="start">The offset to start checking</param>
        /// <param name="count">The amount of bytes in the buffer</param>
        /// <param name="reverse">optionally indicates if the bytes being checked should be reversed before being checked</param>
        /// <returns></returns>
        /// <remarks>If knew the width did you, faster it could be..</remarks>
        public static bool FoundValidUniversalTextFormat(byte[] buffer, ref int start, ref int count, bool reverse = false)
        {
            unchecked //unaligned
            {
                //1100001 1
                while (((reverse ? (Common.Binary.ReverseU8(ref buffer[start])) : buffer[start]) & SurrogateMask) == 0 && start < --count) ++start;
                return count > 0;
            }
        }

    }
}
