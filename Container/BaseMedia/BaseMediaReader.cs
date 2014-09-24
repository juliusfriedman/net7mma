using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Container.BaseMedia
{
    /// <summary>
    /// Represents the logic necessary to read ISO Complaint Base Media Format Files.
    /// <see href="http://en.wikipedia.org/wiki/ISO_base_media_file_format">Wikipedia</see>
    /// Formats include QuickTime (.mov, .mp4, .m4v, .m4a), 
    /// Microsoft Smooth Streaming (.ismv, .isma, .ismc), 
    /// JPEG2000 (.jp2, .jpf, .jpx), Motion JPEG2000 (.mj2, .mjp2), 
    /// 3GPP/3GPP2 (.3gp, .3g2), Adobe Flash (.f4v, .f4p, .f4a, .f4b) and other conforming format extensions.
    /// </summary>
    public class BaseMediaReader : MediaFileStream
    {
        public static List<string> ParentBoxes = new List<string>()
        {
            "moov",
            "trak",
            "mdia",
            "minf",
            "dinf",
            "stbl",
            //"avc1",
            //"mp4a",
            "edts",
            "stsd",
            "udta"
        };

        static int MinimumSize = 8, IdentifierSize = 4, LengthSize = 4;

        public static string ToFourCharacterCode(byte[] identifier, int offset = 0, int count = 4) { return ToFourCharacterCode(Encoding.UTF8, identifier, offset, count); }

        public static string ToFourCharacterCode(Encoding encoding, byte[] identifier, int offset, int count)
        {
            if (encoding == null) throw new ArgumentNullException("encoding");

            if (identifier == null) throw new ArgumentNullException("identifier");

            if (offset + count > identifier.Length) throw new ArgumentOutOfRangeException("offset and count must relfect a position within identifier.");

            return encoding.GetString(identifier, offset, count);
        }

        public BaseMediaReader(string filename, System.IO.FileAccess access = System.IO.FileAccess.Read) : base(filename, access) { }

        public BaseMediaReader(Uri source, System.IO.FileAccess access = System.IO.FileAccess.Read) : base(source, access) { }

        /// <summary>
        /// Given a box string '*' all boxes will be read.
        /// Given a box string './*' all boxes in the current box will be read/
        /// Given a box string '/someBox/anotherBox/*' someBox/anotherBox will be read.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public Element ReadBox(string path)
        {
            throw new NotImplementedException();
        }

        public Element ReadNext()
        {
            if (Remaining <= MinimumSize) throw new System.IO.EndOfStreamException();

            long offset = Position;

            bool complete = true;

            byte[] lengthBytes = new byte[LengthSize];

            long length = 0;
            do
            {
                complete = LengthSize == Read(lengthBytes, 0, LengthSize);
                length = (lengthBytes[0] << 24) + (lengthBytes[1] << 16) + (lengthBytes[2] << 8) + lengthBytes[3];
            } while (length == 1 || (length & 0xffffffff) == 0);
            
            byte[] identifier = new byte[IdentifierSize];

            complete = (IdentifierSize == Read(identifier, 0, IdentifierSize));

            return  new Element(this, identifier, offset, length, complete);
        }

        public override IEnumerator<Element> GetEnumerator()
        {
            while (Remaining > MinimumSize)
            {
                Element next = ReadNext();
                if (next != null) yield return next;
                else yield break;

                if (ParentBoxes.Contains(ToFourCharacterCode(next.Identifier))) continue;
                
                Skip(next.Size - MinimumSize);
            }
        }

        public override Element Root
        {
            get
            {
                long position = Position;

                Position = 0;

                Element root = ReadNext();

                Position = position;

                return root;
            }
        }

        //https://github.com/communitymedia/mediautilities/blob/master/src/net/sourceforge/jaad/mp4/MP4Container.java

        //https://github.com/communitymedia/mediautilities/blob/master/src/net/sourceforge/jaad/mp4/api/Movie.java

        //https://github.com/communitymedia/mediautilities/blob/master/src/net/sourceforge/jaad/mp4/api/Track.java
        //https://github.com/communitymedia/mediautilities/blob/master/src/net/sourceforge/jaad/mp4/api/VideoTrack.java
        //https://github.com/communitymedia/mediautilities/blob/master/src/net/sourceforge/jaad/mp4/api/AudioTrack.java

        public override Element TableOfContents
        {
            get { return ReadBox("stsd") ?? ReadBox("stco") ?? ReadBox("64shit"); }
        }

        public override IEnumerable<Track> GetTracks()
        {

            //Get mvhd
            //foreach mvhd|child->trackBox
            //  createTrack(child)

            //minf
            //vmhd
            //stbl
            //stsd
                //stsd child|0 must be VisualSampleEntry  (for video)

            throw new NotImplementedException();
        }

        public override byte[] GetSample(Track track, out TimeSpan duration)
        {
            throw new NotImplementedException();
        }
    }
}
