using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Codecs.Video.Mpeg4
{
    /// <summary>
    /// Contains common values used by the Motion Picture Experts Group for MPEG4
    /// as defined in: Table 6-3 — Start code values
    /// </summary>
    public static class StartCodes
    {
        //http://cgit.collabora.com/git/user/kakaroto/gst-plugins-bad.git/plain/gst-libs/gst/codecparsers/gstmpeg4parser.h

        public const byte VideoObjectFirst = 0x00;

        public const byte VideoObjectLast = 0x1f;

        public const byte VideoLayerFirst = 0x20;

        public const byte VideoLayerLast = 0x2f;

        public const byte VisualObjectSequence = 0xb0;

        public const byte VisualObjectSequenceEnd = 0xb1;

        public const byte UserData = 0xb2;

        public const byte GroupOfVideoObjectPlane = 0xb3;

        public const byte VideoSessionError = 0xb4;

        public const byte VisualObject = 0xb5;

        public const byte VideoObjectPlane = 0xb6;

        public const byte FBA = 0xba;

        public const byte FBAPlan = 0xbb;

        public const byte Mesh = 0xbc;

        public const byte MeshPlan = 0xbd;

        public const byte StillTextureObject = 0xbe;

        public const byte TextureSpatial = 0xbf;

        public const byte TextureSNRLayer = 0xc0;

        public const byte TextureTile = 0xc1;

        public const byte ShapeLayer = 0xc2;

        public const byte Stuffing = 0xc3;

        public const byte SystemFirst = 0xc6;

        public const byte SystemLast = byte.MaxValue;

        public static bool IsSystemStartCode(byte b) { return b >= SystemFirst && b <= SystemLast; }
    }
}
