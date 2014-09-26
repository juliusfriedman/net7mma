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

        static DateTime IsoBaseDateUtc = new DateTime(1904, 1, 1, 0, 0, 0, DateTimeKind.Utc);

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

        static char[] PathSplits = new char[]{'/'};

        /// <summary>
        /// Given a box string '*' all boxes will be read.
        /// Given a box string './*' all boxes in the current box will be read/
        /// Given a box string '/someBox/anotherBox/*' someBox/anotherBox will be read.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public IEnumerable<Element> ReadBoxes(long offset = 0, params string[] names)
        {
            long position = Position;

            Position = offset;

            foreach (var box in this)
            {
                if (names.Contains(ToFourCharacterCode(box.Identifier)))
                {
                    yield return box;
                }
            }

            Position = position;
        }

        public Element ReadBox(string name, long offset = 0) { return ReadBoxes(offset, name).FirstOrDefault(); }

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
            get { return ReadBox("stco") ?? ReadBox("co64"); }
        }

        public override IEnumerable<Track> GetTracks()
        {

            int trackId = 0;

            ulong timeScale, duration;

            //Obtain the timeScale and duration from the mdhd box
            var mediaHeader = ReadBox("mdhd");

            using (var stream = mediaHeader.Data)
            {
                //12 comes from Version, Flags int in FullBox + 8 for box name and length
                stream.Position += 8;

                byte[] buffer = new byte[8];

                stream.Read(buffer, 0, 4);

                int versionAndFlags = Common.Binary.Read32(buffer, 0, BitConverter.IsLittleEndian), version = versionAndFlags >> 24 & 0xff;

                ulong created = 0, modified = 0;

                DateTime createdDate, modifiedDate;

                switch (version)
                {
                    case 0:
                        {
                            stream.Read(buffer, 0, 4);
                            created = Common.Binary.ReadU32(buffer, 0, BitConverter.IsLittleEndian);
                    
                            stream.Read(buffer, 0, 4);
                            modified = Common.Binary.ReadU32(buffer, 0, BitConverter.IsLittleEndian);
                    
                            stream.Read(buffer, 0, 4);
                            timeScale = Common.Binary.ReadU32(buffer, 0, BitConverter.IsLittleEndian);
                    
                            stream.Read(buffer, 0, 4);
                            duration = Common.Binary.ReadU32(buffer, 0, BitConverter.IsLittleEndian);

                            break;
                        }

                    case 1:
                        {


                            stream.Read(buffer, 0, 8);
                            created = Common.Binary.ReadU64(buffer, 0, BitConverter.IsLittleEndian);

                            stream.Read(buffer, 0, 8);
                            modified = Common.Binary.ReadU64(buffer, 0, BitConverter.IsLittleEndian);

                            stream.Read(buffer, 0, 8);
                            timeScale = Common.Binary.ReadU64(buffer, 0, BitConverter.IsLittleEndian);

                            stream.Read(buffer, 0, 8);
                            duration = Common.Binary.ReadU64(buffer, 0, BitConverter.IsLittleEndian);

                            break;
                        }
                    default: throw new NotSupportedException();
                }

                createdDate = IsoBaseDateUtc.AddMilliseconds(created * 1000);

                modifiedDate = IsoBaseDateUtc.AddMilliseconds(modified * 1000);

            }

            //For each trak box 
            foreach (var trakBox in ReadBoxes(Root.Offset, "trak"))
            {
                //Check tkhd for IsTrackInMovie and IsTrackEnabled

                var sampleToTimeBox = ReadBox("stts", trakBox.Offset);

                List<Tuple<long, long>> entries = new List<Tuple<long, long>>();

                List<long> offsets = new List<long>();

                List<int> sizes = new List<int>();

                if (sampleToTimeBox != null)
                {
                    using (var stream = sampleToTimeBox.Data)
                    {

                        //12 comes from Version, Flags int in FullBox + 8 for box name and length
                        stream.Position += 12;

                        byte[] buffer = new byte[8];

                        stream.Read(buffer, 0, 4);

                        int entryCount = Common.Binary.Read32(buffer, 0, BitConverter.IsLittleEndian);

                        for (int i = 0; i < entryCount; ++i)
                        {
                            stream.Read(buffer, 0, 8);

                            //Sample Count Sample Duration
                            entries.Add(new Tuple<long, long>(Common.Binary.Read32(buffer, 0, BitConverter.IsLittleEndian), 
                                Common.Binary.Read32(buffer, 4, BitConverter.IsLittleEndian)));
                        }
                    }
                }


                var sampleToSizeBox = ReadBox("stsz", trakBox.Offset);

                if (sampleToSizeBox != null)
                {
                    using (var stream = sampleToSizeBox.Data)
                    {

                        stream.Position += 12;

                        byte[] buffer = new byte[4];

                        stream.Read(buffer, 0, 4);

                        int defaultSize = Common.Binary.Read32(buffer, 0, BitConverter.IsLittleEndian);

                        stream.Read(buffer, 0, 4);

                        int count = Common.Binary.Read32(buffer, 0, BitConverter.IsLittleEndian);

                        if (defaultSize == 0)
                        {
                            for (int i = 0; i < count; ++i)
                            {
                                stream.Read(buffer, 0, 4);

                                sizes.Add(Common.Binary.Read32(buffer, 0, BitConverter.IsLittleEndian));
                            }
                        }

                        
                    }
                }

                var chunkOffsetsBox = ReadBox("stco", trakBox.Offset);
                if (chunkOffsetsBox == null)
                {
                    chunkOffsetsBox = ReadBox("co64", trakBox.Offset);

                    if(chunkOffsetsBox != null) using (var stream = chunkOffsetsBox.Data)
                    {
                        stream.Position += 12;

                        byte[] buffer = new byte[8];

                        stream.Read(buffer, 0, 4);

                        int chunkCount = Common.Binary.Read32(buffer, 0, BitConverter.IsLittleEndian);

                        for (int i = 0; i < chunkCount; ++i)
                        {
                            stream.Read(buffer, 0, 8);

                            offsets.Add(Common.Binary.Read64(buffer, 0, BitConverter.IsLittleEndian));
                        }
                    }

                }
                else
                {
                    using (var stream = chunkOffsetsBox.Data)
                    {
                        stream.Position += 12;

                        byte[] buffer = new byte[4];

                        stream.Read(buffer, 0, 4);

                        int chunkCount = Common.Binary.Read32(buffer, 0, BitConverter.IsLittleEndian);

                        for (int i = 0; i < chunkCount; ++i)
                        {
                            stream.Read(buffer, 0, 4);

                            offsets.Add((long)Common.Binary.Read32(buffer, 0, BitConverter.IsLittleEndian));
                        }
                    }
                }

                //Could also calc with entries.Item2 ?

                yield return new Track(trakBox, trackId++, TimeSpan.Zero, TimeSpan.FromMilliseconds(duration / (timeScale / 1000)));
            }
        }

        public override byte[] GetSample(Track track, out TimeSpan duration)
        {
            throw new NotImplementedException();
        }
    }
}
