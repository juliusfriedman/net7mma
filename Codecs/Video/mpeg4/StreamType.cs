using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Codecs.Video.mpeg4
{
   /// <summary>
   /// Describes the various MPEG Stream Types.
   /// <see href="http://www.mp4ra.org/object.html">MP4REG</see>
   /// </summary>
    public static class StreamType
    {
        public const byte Forbidden = 0x00;

        /// <summary>
        /// see 8.5
        /// </summary>
        public const byte ObjectDescriptorStream = 0x01;

        /// <summary>
        /// see 10.2.5
        /// </summary>
        public const byte ClockReferenceStream = 0x02;

        /// <summary>
        /// see 9.2.1
        /// </summary>
        public const byte SceneDescriptionStream = 0x03;

        public const byte VisualStream = 0x04;

        public const byte AudioStream = 0x05;

        public const byte MPEG7Stream = 0x06;

        /// <summary>
        /// see 8.3.2
        /// </summary>
        public const byte IPMPStream = 0x07;

        /// <summary>
        /// see 8.4.2
        /// </summary>
        public const byte ObjectContentInfoStream = 0x08;

        public const byte MPEGJStream = 0x09;

        public const byte InteractionStream = 0x0A;

        public const byte IPMPToolStream = 0x0B;

        public const byte FontDataStream = 0x0C;

        public const byte StreamingText = 0x0D;

        public static bool IsUserPrivate(byte b) { return b >= 0x20 && b <= 0x3f; }
    }
}
