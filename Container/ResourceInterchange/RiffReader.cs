using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Container.Riff
{
    /// <summary>
    /// Represents the logic necessary to read files in the Resource Interchange File Format Format (.avi)
    /// </summary>
    public class RiffReader : MediaFileStream, IMediaContainer
    {

        #region CONSTANTS

        public const int DWORDSIZE = 4;
        public const int TWODWORDSSIZE = 8;
        public static readonly string RIFF4CC = "RIFF";
        public static readonly string RIFX4CC = "RIFX";
        public static readonly string LIST4CC = "LIST";

        // Known file types
        public static readonly int ckidAVI = ToFourCC("AVI ");
        public static readonly int ckidWAV = ToFourCC("WAVE");
        public static readonly int ckidRMID = ToFourCC("RMID");


        #endregion

        #region FourCC conversion methods

        public static string FromFourCC(int FourCC)
        {
            char[] chars = new char[4];
            chars[0] = (char)(FourCC & 0xFF);
            chars[1] = (char)((FourCC >> 8) & 0xFF);
            chars[2] = (char)((FourCC >> 16) & 0xFF);
            chars[3] = (char)((FourCC >> 24) & 0xFF);

            return new string(chars);
        }

        public static int ToFourCC(string FourCC)
        {
            if (FourCC.Length != 4)
            {
                throw new Exception("FourCC strings must be 4 characters long " + FourCC);
            }

            int result = ((int)FourCC[3]) << 24
                        | ((int)FourCC[2]) << 16
                        | ((int)FourCC[1]) << 8
                        | ((int)FourCC[0]);

            return result;
        }

        public static int ToFourCC(char[] FourCC)
        {
            if (FourCC.Length != 4)
            {
                throw new Exception("FourCC char arrays must be 4 characters long " + new string(FourCC));
            }

            int result = ((int)FourCC[3]) << 24
                        | ((int)FourCC[2]) << 16
                        | ((int)FourCC[1]) << 8
                        | ((int)FourCC[0]);

            return result;
        }

        public static int ToFourCC(char c0, char c1, char c2, char c3)
        {
            int result = ((int)c3) << 24
                        | ((int)c2) << 16
                        | ((int)c1) << 8
                        | ((int)c0);

            return result;
        }

        #endregion

        static int MinimumSize = 8, IdentifierSize = 4, LengthSize = 4;

        public static string ToFourCharacterCode(byte[] identifier, int offset = 0, int count = 4) { return FromFourCC(ToFourCC(Array.ConvertAll<byte, char>(identifier.Skip(offset).Take(count).ToArray(), Convert.ToChar))); }

        public RiffReader(string filename, System.IO.FileAccess access = System.IO.FileAccess.Read) : base(filename, access) { }

        public RiffReader(Uri source, System.IO.FileAccess access = System.IO.FileAccess.Read) : base(source, access) { }

        /// <summary>
        /// Given a box string '*' all boxes will be read.
        /// Given a box string './*' all boxes in the current box will be read/
        /// Given a box string '/someBox/anotherBox/*' someBox/anotherBox will be read.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public Element ReadChunk(string path)
        {
            throw new NotImplementedException();
        }

        public Element ReadNext()
        {
            if (Remaining <= MinimumSize) throw new System.IO.EndOfStreamException();

            long offset = Position;

            bool complete = true;

            byte[] identifier = new byte[IdentifierSize];

            complete = (IdentifierSize == Read(identifier, 0, IdentifierSize));

            byte[] lengthBytes = new byte[LengthSize];

            complete = LengthSize == Read(lengthBytes, 0, LengthSize);

            long length = Common.Binary.Read32(lengthBytes, 0, false);

            complete = length < Remaining;

            return  new Element(this, identifier, offset, length, complete);
        }


        public override IEnumerator<Element> GetEnumerator()
        {
            while (Remaining > TWODWORDSSIZE)
            {
                Element next = ReadNext();
                if (next != null) yield return next;
                else yield break;

                //To use binary comparison
                string fourCC = FromFourCC(Common.Binary.Read32(next.Identifier, 0, false));

                if (fourCC == RIFF4CC || fourCC == RIFX4CC)
                {
                    //FileType 4 bytes? usually 'AVI '
                    Skip(IdentifierSize);
                }
                else if (fourCC == LIST4CC)
                {
                    //Skip the list data (accessible through next.Data)
                    Skip(next.Size);
                }
                else
                {

                    //Calculate padded size (to word boundary)
                    int paddedSize = (int)next.Size;

                    if (0 != (next.Size & 1)) ++paddedSize;

                    Skip(paddedSize);
                }
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

                //To be a valid file the RootElement must have a Identifier of RIFF4CC or RIFX4CC

                return root;
            }
        }

        public override Element TableOfContents
        {
            get { return ReadChunk("idx1") ?? ReadChunk("indx"); }
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
