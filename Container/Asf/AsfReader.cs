using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Container.Asf
{
    /// <summary>
    /// Represents the logic necessary to read files in the Advanced Systems Format (.asf)
    /// </summary>
    public class AsfReader : MediaFileStream, IMediaContainer
    {

        public static class Identifiers
        {
            /// <summary>
            ///    Indicates that an object is a <see cref="ContentDescriptionObject" />.
            /// </summary>
            public static readonly System.Guid AsfContentDescriptionObject = new System.Guid("75B22633-668E-11CF-A6D9-00AA0062CE6C");
                

            /// <summary>
            ///    Indicates that an object is a <see cref="ExtendedContentDescriptionObject" />.
            /// </summary>
            public static readonly System.Guid AsfExtendedContentDescriptionObject = new System.Guid("D2D0A440-E307-11D2-97F0-00A0C95EA850");
                

            /// <summary>
            ///    Indicates that an object is a <see cref="FilePropertiesObject" />.
            /// </summary>
            public static readonly System.Guid AsfFilePropertiesObject = new System.Guid("8CABDCA1-A947-11CF-8EE4-00C00C205365");
                

            /// <summary>
            ///    Indicates that an object is a <see cref="HeaderExtensionObject" />.
            /// </summary>
            public static readonly System.Guid AsfHeaderExtensionObject = new System.Guid("5FBF03B5-A92E-11CF-8EE3-00C00C205365");

            /// <summary>
            ///    Indicates that an object is a <see cref="HeaderObject" />.
            /// </summary>
            public static readonly System.Guid AsfHeaderObject = new System.Guid("75B22630-668E-11CF-A6D9-00AA0062CE6C");

            /// <summary>
            ///    Indicates that an object is a <see cref="MetadataLibraryObject" />.
            /// </summary>
            public static readonly System.Guid AsfMetadataLibraryObject = new System.Guid("44231C94-9498-49D1-A141-1D134E457054");
                
            /// <summary>
            ///    Indicates that an object is a <see cref="PaddingObject" />.
            /// </summary>
            public static readonly System.Guid AsfPaddingObject = new System.Guid("1806D474-CADF-4509-A4BA-9AABCB96AAE8");

            /// <summary>
            ///    Indicates that an object is a <see cref="StreamPropertiesObject" />.
            /// </summary>
            public static readonly System.Guid AsfStreamPropertiesObject = new System.Guid("B7DC0791-A9B7-11CF-8EE6-00C00C205365");
                
            /// <summary>
            ///    Indicates that a <see cref="StreamPropertiesObject" />
            ///    contains information about an audio stream.
            /// </summary>
            public static readonly System.Guid AsfAudioMedia = new System.Guid("F8699E40-5B4D-11CF-A8FD-00805F5C442B");
                
            /// <summary>
            ///    Indicates that a <see cref="StreamPropertiesObject" />
            ///    contains information about an video stream.
            /// </summary>
            public static readonly System.Guid AsfVideoMedia = new System.Guid("BC19EFC0-5B4D-11CF-A8FD-00805F5C442B");

            /// <summary>
            ///    Indicates a placeholder portion of a file is correctly
            ///    encoded.
            /// </summary>
            public static readonly System.Guid AsfReserved1 = new System.Guid("ABD3D211-A9BA-11cf-8EE6-00C00C205365");
        }

        public AsfReader(string filename, System.IO.FileAccess access = System.IO.FileAccess.Read) : base(filename, access) { }

        public AsfReader(Uri source, System.IO.FileAccess access = System.IO.FileAccess.Read) : base(source, access) { }

        /// <summary>
        /// Given a box string '*' all boxes will be read.
        /// Given a box string './*' all boxes in the current box will be read/
        /// Given a box string '/someBox/anotherBox/*' someBox/anotherBox will be read.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public Element ReadElement(string path)
        {
            throw new NotImplementedException();
        }

        public Element ReadNext()
        {
            if (Remaining < 16) return null;

            long offset = Position;

            byte[] identifier = new byte[16];

            Read(identifier, 0, 16);

            return new Element(this, identifier, offset, 0, true);

            //Guid parsed = new Guid(identifier);

            //switch (parsed)
            //{
            //    case Guid.AsfAudioMedia: break;

            //}
        }

        public override IEnumerator<Element> GetEnumerator()
        {
            while (Remaining > 16)
            {
                Element next = ReadNext();
                if (next != null) yield return next;
                else yield break;


                Skip(next.Size);
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

        public override Element TableOfContents
        {
            get { return ReadElement("?"); }
        }

        public override IEnumerable<Track> GetTracks()
        {
            throw new NotImplementedException();
        }

        public override byte[] GetSample(Track track, out TimeSpan duration)
        {
            throw new NotImplementedException();
        }
    }
}
