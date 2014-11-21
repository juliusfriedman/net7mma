using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Containers.Mpeg
{
    /// <summary>
    /// Contains common values used by the Motion Picture Experts Group.
    /// </summary>
    public static class StartCodes
    {
        /// <summary>
        /// Syncwords are determined using 4 bytes, the first 3 of which are always 0x000001
        /// </summary>
        public static byte[] Prefix = new byte[] { 0x00, 0x00, 0x01 };

        public static bool IsReserved(byte b) { return b == 0xB0 || b == 0xB1 || b == 0xB6; }

        public static bool IsSystemStartCode(byte b) { return b >= 0xB9 && b <= byte.MaxValue; }

        //0 - 31
        public const byte Picture = 0x00;

        //Probably Key Frames.
        public static bool IsVideoObjectStartCode(byte b) { return b >= Picture && b <= Common.Binary.FiveBitMaxValue; }

        //0x20 probably a key frame.
        public static bool IsVisalObjectLayerStartCode(byte code) { return code >= 0x20 && code <= 0x2f; }

        public static bool IsSliceStartCode(byte code) { return code >= 0x01 && code <= 0xAF; }

        //Probably key frame
        public const byte VisalObjectSequence = 0xB0;

        public const byte End = 0xB1;

        public const byte UserData = 0xB2;

        public const byte SequenceHeader = 0xB3;

        public const byte SequenceError = 0xB4;

        public const byte Extension = 0xB5;

        public const byte SequenceEnd = 0xB7;

        public const byte Group = 0xB8;

        //Group Video Object Plane

        public const byte UserMetaData = 0xB2;

        //Group Of Video Object Plane

        public const byte VideoSequenceHeader = 0xB3;

        public const byte VisualObject = Extension;

        //Key frame if next 2 bits are 0
        public const byte VideoObjectPlane = 0xB6;

        public const byte VisualObjectSequenceEnd = 0xB7;

        public const byte GroupOfPictures = 0xB8;

        public const byte SyncByte = 0xBA;

        public const byte SystemHeader = 0xBB;

        public const byte EndCode = 0xB9;
    }
}
