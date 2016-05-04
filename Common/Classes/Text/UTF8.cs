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
    [System.CLSCompliant(true)]
    public sealed class UTF8
    {
        public static readonly byte[] LineEndingBytes = new byte[] { Media.Common.ASCII.NewLine, Media.Common.ASCII.LineFeed }; //, Media.Common.ASCII.FormFeed

        public static readonly char[] LineEndingCharacters = System.Text.Encoding.UTF8.GetChars(LineEndingBytes);

        public static readonly byte[] WhiteSpaceBytes = new byte[] { Media.Common.ASCII.Space };

        public static readonly char[] WhiteSpaceCharacters = System.Text.Encoding.UTF8.GetChars(WhiteSpaceBytes);

        public static readonly byte[] SemiColonBytes = new byte[] { Common.ASCII.SemiColon };

        public static readonly char[] SemiColonCharacters = System.Text.Encoding.UTF8.GetChars(SemiColonBytes);

        public static readonly byte[] ColonBytes = new byte[] { Common.ASCII.Colon };

        public static readonly char[] ColonCharacters = System.Text.Encoding.UTF8.GetChars(ColonBytes);

        public static readonly byte[] ForwardSlashBytes = new byte[] { Common.ASCII.ForwardSlash };

        public static readonly char[] ForwardSlashCharacters = System.Text.Encoding.UTF8.GetChars(ForwardSlashBytes);

        public static readonly byte[] TabBytes = new byte[] { Common.ASCII.HorizontalTab };

        public static readonly char[] TabCharacters = System.Text.Encoding.UTF8.GetChars(TabBytes);

        //Todo, move to UTF8

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

            //Todo, unsafe and native

            unchecked //unaligned
            {
                //1100001 1
#if NATIVE
                //Copies the byte but skips the bounds checks.
                while (((reverse ? Common.Binary.ReverseU8(System.Runtime.InteropServices.Marshal.ReadByte(System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement<byte>(buffer, start))) : System.Runtime.InteropServices.Marshal.ReadByte(System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement<byte>(buffer, start))) & SurrogateMask) == 0) start++;
#else
                while (((reverse ? (Common.Binary.ReverseU8(ref buffer[start])) : buffer[start]) & SurrogateMask) == 0 && start < --count) ++start;
#endif
                return count > 0;
            }
        }

        //Todo, Encode / Decode

        //See https://github.com/dotnet/corefxlab/tree/master/src/System.Text.Utf8
        //See https://github.com/Quobject/EngineIoClientDotNet/blob/master/Src/EngineIoClientDotNet.mono/Modules/UTF8.cs
        //See https://gist.github.com/antonijn/8400302

        //java implementations
        // https://github.com/google/protobuf/blob/master/java/core/src/main/java/com/google/protobuf/Utf8.java

        // https://github.com/google/guava/blob/master/guava/src/com/google/common/base/Utf8.java

        // https://github.com/xetorthio/fastu/blob/master/src/main/java/com/github/xetorthio/Fastu.java

        // Ascii - https://github.com/pquiring/javaforce/blob/master/src/javaforce/ASCII8.java

        //
        //T.140
        //Needs UTF-8, should have it's own Assembly. (Codecs.Text)... Codecs.Text.UTF8, Codecs.Text.T140 etc.
    }
}
