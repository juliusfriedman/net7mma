using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Codecs.Video.Mpeg4
{
    public static class StartCode
    {
        public static byte[] Prefix = Media.Codecs.Video.Mpeg2.StartCode.Prefix;

        public const byte Picture = Media.Codecs.Video.Mpeg2.StartCode.Picture;

        public static bool IsSystemStartCode(byte b) { return Media.Codecs.Video.Mpeg2.StartCode.IsSystemStartCode(b); }

        //0 - 31
        //Probably Key Frames.
        public static bool IsVideoObjectStartCode(byte b) { return Media.Codecs.Video.Mpeg2.StartCode.IsVideoObjectStartCode(b); }

        //0x20 probably a key frame.
        public static bool IsVisalObjectLayer(byte code) { return code >= 0x20 && code <= 0x2f; }

        public static bool IsSlice(byte code) { return code >= 0x01 && code <= 0xAF; }

        //Probably key frame
        public const byte VisalObjectSequence = 0xB0;

        public const byte End = 0xB1;

        //Group Video Object Plane

        public const byte UserMetaData = 0xB2;

        //Group Of Video Object Plane

        public const byte VideoSequenceHeader = 0xB3;

        public const byte SequenceError = 0xB4;

        public const byte VisualObject = 0xB5;

        //Key frame if next 2 bits are 0
        public const byte VideoObjectPlane = 0xB6;

        public const byte VisualObjectSequenceEnd = 0xB7;

        public const byte GroupOfPictures = 0xB8;
    }
}
