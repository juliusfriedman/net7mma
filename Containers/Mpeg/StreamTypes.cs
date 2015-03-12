using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Containers.Mpeg
{
    /// <summary>
    /// Describes the various MPEG Stream Types. (ISO13818-2 and compatible)
    /// <see href="http://www.mp4ra.org/object.html">MP4REG</see>
    /// </summary>
    //http://xhelmboyx.tripod.com/formats/mpeg-layout.txt
    public static class StreamTypes
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

        public const byte ProgramEnd = 0xB9;

        public const byte PackHeader = 0xBA;

        public const byte SystemHeader = 0xBB;

        public const byte ProgramStreamMap = 0xBC;

        public const byte PrivateStream1 = 0xBD;

        public const byte PaddingStream = 0xBE;

        public const byte PrivateStream2 = 0xBF;

        public static bool IsMpeg1or2AudioStream(byte code) { return code >= 0xC0 && code <= 0xDF; }

        public static bool IsMpeg1or2VideoStream(byte code) { return code >= 0xE0 && code <= 0xEF; }

        public const byte ECMStream = 0xF0;

        public const byte EMMStream = 0xF1;

        public const byte DMSCCStream = 0xF2;

        public const byte ISO13522Stream = 0xF3;

        public const byte H222TypeA = 0xF4;

        public const byte H222TypeB = 0xF5;

        public const byte H222TypeC = 0xF6;

        public const byte H222TypeD = 0xF7;

        public const byte H222TypeE = 0xF8;

        public const byte AncillaryStream = 0xF9;

        public static bool IsReserverd(byte b) { return b >= 0xFA && b <= 0xFE; }

        public static bool IsUserPrivate(byte b) { return b >= 0x20 && b <= 0x3F; }

        public const byte ProgramStreamDirectory = byte.MaxValue;

        internal static Dictionary<byte, string> StreamTypeMap = new Dictionary<byte, string>();

        public static string ToTextualConvention(byte b)
        {
            string name;
            if (StreamTypeMap.TryGetValue(b, out name)) return name;
            if (IsMpeg1or2AudioStream(b)) return "Audio";
            if (IsMpeg1or2VideoStream(b)) return "Video";
            if (IsReserverd(b)) return "Reserved";
            if (IsUserPrivate(b)) return "UserPrivate";
            return Media.Common.Extensions.String.StringExtensions.UnknownString;
        }

        static StreamTypes()
        {
            foreach (var fieldInfo in typeof(StreamTypes).GetFields()) StreamTypeMap.Add((byte)fieldInfo.GetValue(null), fieldInfo.Name);
        }

    }
}
