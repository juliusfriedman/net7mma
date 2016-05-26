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

namespace Media.Codecs.Image.Jpeg
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

        //https://svn.xiph.org/experimental/giles/jpegdump.c

        public const byte Nul = 0x0;

        public const byte TEM = 0x01;        

        public const byte StartOfBaselineFrame = 0xc0;

        public const byte StartOfProgressiveFrame = 0xc2;

        public const byte StartOfLosslessHuffmanFrame = 0xc3;

        public const byte HuffmanTable = 0xc4;

        public const byte StartOfDifferentialSequentialHuffmanFrame = 0xc5;

        public const byte StartOfDifferentialProgressiveHuffmanFrame = 0xc6;

        public const byte StartOfDifferentialLosslessHuffmanFrame = 0xc7;

        public const byte Extension = 0xc8;        

        public const byte StartOfExtendedSequentialArithmeticFrame = 0xc9;

        public const byte StartOfProgressiveArithmeticFrame = 0xca;

        public const byte StartOfLosslessArithmeticFrame = 0xcb;

        public const byte ArithmeticConditioning = 0xcc;

        public const byte StartOfDifferentialSequentialArithmeticFrame = 0xcd;

        public const byte StartOfDifferentialProgressiveArithmeticFrame = 0xce;

        public const byte StartOfDifferentialLosslessArithmeticFrame = 0xcf;

        //0xd0 => 0xd7 RST => RestartMarker

        public const byte StartOfInformation = 0xd8;

        public const byte EndOfInformation = 0xd9;

        public const byte StartOfScan = 0xda;

        public const byte QuantizationTable = 0xdb;

        public const byte NumberOfLines = 0xdc;

        public const byte DataRestartInterval = 0xdd;

        public const byte HierarchialProgression = 0xde;
        /// <summary>
        /// Expand reference components
        /// </summary>
        public const byte Expand = 0xdf;

        public const byte AppFirst = 0xe0;

        //0xe1 => 0xee App 1 => App 14

        public const byte AppLast = 0xef;

        //0xf0 => 0xf6 Extension Data

        // 0xf7 => start of frame JPEG LS

        // 0xf8 = LSE extension parameters JPEG LS

        //0xf9 => 0xfd Extension Data

        public const byte TextComment = 0xfe;

        public const byte Prefix = 0xff;
    }

    public static class JpegMarkerExtensions
    {
        public static byte[] CreateJFIFHeader()
        {
            throw new System.NotImplementedException();
        }

        public static byte[] CreateJFXXHeader()
        {
            throw new System.NotImplementedException();
        }

        public static byte[] CreateStartOfFrameMarker()
        {
            throw new System.NotImplementedException();
        }

        public static byte[] CreateHuffmanTableMarker(byte[] codeLens, byte[] symbols, int tableNo, int tableClass)
        {
            int symbolsLength = symbols.Length;
            
            byte[] result = new byte[5 + codeLens.Length + symbolsLength];
            
            result[0] = Media.Codecs.Image.Jpeg.Markers.Prefix;
            
            result[1] = Media.Codecs.Image.Jpeg.Markers.HuffmanTable;

            //Length
            Common.Binary.Write16(result, 2, Media.Common.Binary.IsLittleEndian, (ushort)(3 + codeLens.Length + symbols.Length));

            result[4] = (byte)((tableClass << 4) | tableNo); //Id

            //Data
            codeLens.CopyTo(result, 5); 
            
            symbols.CopyTo(result, 5 + symbolsLength + 1);

            return result;
        }

        [System.CLSCompliant(false)]
        public static byte[] CreateDataRestartIntervalMarker(ushort dri)
        {
            return new byte[] { Media.Codecs.Image.Jpeg.Markers.Prefix, Media.Codecs.Image.Jpeg.Markers.DataRestartInterval, 0x00, 0x04, (byte)(dri >> 8), (byte)(dri) };
        }

        public static byte[] CreateDataRestartIntervalMarker(int dri) { return CreateDataRestartIntervalMarker((ushort)dri); }
    }

    //Todo Should be moved to JpegReader in Container or JpegReader should use this?
    //JfifReader..
}
