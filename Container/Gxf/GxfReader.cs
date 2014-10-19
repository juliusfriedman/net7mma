using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Container.Gxf
{
    //Namespace SMPTE ?
    // SMPTE 360M-2001: General Exchange Format (GXF)
    //https://tech.ebu.ch/docs/techreview/trev_291-edge.pdf

    public class GxfReader : MediaFileStream, IMediaContainer
    {
        //All are really should just be under an Identifier : byte

        public enum Identifier : byte
        {

            //public enum PacketType
            //{
            Map = 0xbc,
            Media = 0xbf,
            EndOfStream = 0xfb,
            Flt = 0xfc,
            Umf = 0xfd,
            //}

            //public enum Tag
            //{
            TagName = 0x40,
            FirstField = 0x41,
            LaztField = 0x42,
            MarkIn = 0x43,
            MarkOut = 0x44,
            Size = 0x45,
            //}

            //public enum TrackTag
            //{
            TrackName = 0x4c,
            Aux = 0x4d,
            Version = 0x4e,
            MPEGAux = 0x4f,
            FramesPerSeconds = 0x50,
            TrackLines = 0x51,
            FPF = 0x52
            //}

        }

        #region Constants

        const int IdentiferSize = 6, MinimumSize = IdentiferSize + 1, LengthSize = 2;

        #endregion

        #region Statics

        public static string ToTextualConvention(byte[] identifier)
        {
            return string.Empty;
        }

        #endregion

        public GxfReader(string filename, System.IO.FileAccess access = System.IO.FileAccess.Read) : base(filename, access) { }

        public GxfReader(Uri source, System.IO.FileAccess access = System.IO.FileAccess.Read) : base(source, access) { }

        public IEnumerable<Node> ReadElements(long offset, long count, params Identifier[] identifiers)
        {
            long position = Position;

            Position = offset;

            foreach (var element in this)
            {
                if (identifiers == null || identifiers.Count() == 0 || identifiers.Contains((Identifier)element.Identifier[0])) yield return element;

                count -= element.Size;

                if (element == null || count <= 0) break;
            }

            Position = position;
        }

        public Node ReadElement(Identifier identifier, long offset, long count)
        {
            long position = Position;
            Node result =  ReadElements(offset, count, identifier).FirstOrDefault();
            Position = position;
            return result;
        }

        public Node ReadNext()
        {
            byte[] identifier = new byte[IdentiferSize];
            Read(identifier, 0, IdentiferSize);

            byte[] lengthBytes = new byte[LengthSize];
            Read(lengthBytes, 0, LengthSize);

            long length = Common.Binary.ReadU16(lengthBytes, 0, BitConverter.IsLittleEndian);

            return new Node(this, identifier, Position, length, length <= Remaining);
        }
        public override IEnumerator<Node> GetEnumerator()
        {
            while (Remaining >= MinimumSize)
            {
                Node next = ReadNext();

                if (next == null) yield break;

                yield return next;

                Skip(next.Size);
            }
        }

        List<Track> m_Tracks;

        public override IEnumerable<Track> GetTracks()
        {

            if (m_Tracks != null)
            {
                foreach (Track track in m_Tracks) yield return track;
                yield break;
            }

            var tracks = new List<Track>();

            long position = Position;

            using (var root = Root)
            {
                //Parse tags
                //
                switch (root.Identifier[0])
                {
                    default: break;
                }

                Track created = null;

                yield return created;

                tracks.Add(created);
            }

            Position = position;

            m_Tracks = tracks;

            throw new NotImplementedException();
        }

        public override byte[] GetSample(Track track, out TimeSpan duration)
        {
            throw new NotImplementedException();
        }

        public override Node Root
        {
            get { return ReadElement(Identifier.Map, 0, MinimumSize); }
        }

        public override Node TableOfContents
        {
            get { return Root; }
        }
       
    }
}
