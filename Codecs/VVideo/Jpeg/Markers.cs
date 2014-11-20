using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        /// In every marker segment the first two bytes after the marker shall be an unsigned value [In Network Endian] that denotes the length in bytes of 
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
}
