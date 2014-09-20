using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Container.Matroska
{
    /// <summary>
    /// Represents the logic necessary to read files in the Matroska Format (.mkv)
    /// </summary>
    public class MatroskaReader : MediaFileStream, IMediaContainer
    {

        #region Constants

        #endregion

        public static List<string> ParentElements = new List<string>()
        {
            Encoding.UTF8.GetString(new byte[]{0x1a, 0x45, 0xdf, 0xa3})
        };

        static byte[] ReadEbmlCode(MatroskaReader reader)
        {

            if (reader.Remaining <= 0) return null;

            //Begin loop with byte set to newly read byte.
            byte firstByte = (byte)reader.ReadByte();
            int numBytes = 0;

            //if(firstByte == byte.MaxValue) //indefinite...


            //Begin by counting the bits unset before the first '1'.
            long mask = 0x0080;
            for (int i = 0; i < 8; ++i)
            {
                //Start at left, shift to right.
                if ((firstByte & mask) == mask)
                { //One found
                    //Set number of bytes in size = i+1 ( we must count the 1 too)
                    numBytes = i + 1;
                    //exit loop by pushing i out of the limit
                    break;
                }
                mask >>= 1;
            }

            if (numBytes == 0) return null;

            //Setup space to store the bits
            byte[] data = new byte[numBytes];

            //TOdo check logic

            //Clear the 1 at the front of this byte, all the way to the beginning of the size

            //Doing so causes the first byte to be mis reresented vs the ids on the docs page.
            //Could also build the lookup table minus the first nibble..

            //E.g. a id 0x1a 0x45 would become
                      //0x10, 0x45?
            
            data[0] = (byte)(firstByte & ((0xFF >> (numBytes))));

            if (numBytes > 1) reader.Read(data, 1, numBytes - 1);

            return data;
        }

        static long ParseVInt(byte[] data, int offset = 0)
        {
            if (data == null)
                return 0;
            //Put this into a long
            long size = 0;
            long n = 0;
            for (int e = data.Length; offset < e; ++offset)
            {
                n = ((long)data[e - 1 - offset] << 56) >> 56;
                size |= (n << (8 * offset));
            }
            return size;
        }

        public static string ToFourCharacterCode(byte[] identifier, int offset = 0) { return Encoding.UTF8.GetString(identifier, offset, identifier.Length - offset); }

        public MatroskaReader(string filename, System.IO.FileAccess access = System.IO.FileAccess.Read) : base(filename, access) { }

        public MatroskaReader(Uri source, System.IO.FileAccess access = System.IO.FileAccess.Read) : base(source, access) { }

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
            if (Remaining <= 2) throw new System.IO.EndOfStreamException();

            long offset = Position;

            bool complete = true;
            
        Read:

            byte[] identifier = ReadEbmlCode(this);

            if (identifier == null) goto Read;

            byte[] lengthBytes = ReadEbmlCode(this);

            if (lengthBytes == null) goto Read;

            long length = ParseVInt(lengthBytes);

            //Double check logic
            if (length < 0) goto Read;

            complete = length < Remaining;

            return  new Element(this, identifier, offset, length, complete);
        }


        public override IEnumerator<Element> GetEnumerator()
        {
            while (Remaining > 2)
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
            get { return ReadElement("/Tracks/*"); }
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
