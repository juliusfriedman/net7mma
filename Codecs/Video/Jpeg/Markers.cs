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

namespace Media.Codecs.Video.Jpeg
{
    /// <summary>
    /// Markers which are contained in a valid Jpeg Image
    /// <see cref="http://www.jpeg.org/public/fcd15444-10.pdf">A.1 Extended capabilities</see>
    /// </summary>
    public sealed class Markers
    {
        static Markers() { }

        /// <summary>
        /// In every marker segment the first two bytes after the marker shall be an unsigned value [In Network ByteOrder] that denotes the length in bytes of 
        /// the marker segment parameters (including the two bytes of this length parameter but not the two bytes of the marker itself). 
        /// </summary>

        public const byte Prefix = 0xff;

        public const byte TextComment = 0xfe;

        public const byte StartOfBaselineFrame = 0xc0;

        public const byte StartOfProgressiveFrame = 0xc2;

        public const byte HuffmanTable = 0xc4;

        public const byte StartOfInformation = 0xd8;

        public const byte AppFirst = 0xe0;

        public const byte AppLast = 0xee;

        public const byte EndOfInformation = 0xd9;

        public const byte QuantizationTable = 0xdb;

        public const byte DataRestartInterval = 0xdd;

        public const byte StartOfScan = 0xda;
    }

    //Todo Should be moved to JpegReader in Container or JpegReader should use this?
    //JfifReader..
}
