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

        //https://www.ffmpeg.org/doxygen/trunk/asfdec_8c_source.html

        #region Constants
 
        //   ASF data packet structure
        //   =========================
        //
        //
        //  -----------------------------------
        // | Error Correction Data             |  Optional
        //  -----------------------------------
        // | Payload Parsing Information (PPI) |
        ////  -----------------------------------
        //// | Payload Data                      |
        ////  -----------------------------------
        //// | Padding Data                      |
        ////  -----------------------------------
 
 
        //// PPI_FLAG - Payload parsing information flags
        //#define ASF_PPI_FLAG_MULTIPLE_PAYLOADS_PRESENT 1
 
        //#define ASF_PPI_FLAG_SEQUENCE_FIELD_IS_BYTE  0x02 //0000 0010
        //#define ASF_PPI_FLAG_SEQUENCE_FIELD_IS_WORD  0x04 //0000 0100
        //#define ASF_PPI_FLAG_SEQUENCE_FIELD_IS_DWORD 0x06 //0000 0110
        //#define ASF_PPI_MASK_SEQUENCE_FIELD_SIZE     0x06 //0000 0110
 
        //#define ASF_PPI_FLAG_PADDING_LENGTH_FIELD_IS_BYTE  0x08 //0000 1000
        //#define ASF_PPI_FLAG_PADDING_LENGTH_FIELD_IS_WORD  0x10 //0001 0000
        //#define ASF_PPI_FLAG_PADDING_LENGTH_FIELD_IS_DWORD 0x18 //0001 1000
        //#define ASF_PPI_MASK_PADDING_LENGTH_FIELD_SIZE     0x18 //0001 1000
 
        //#define ASF_PPI_FLAG_PACKET_LENGTH_FIELD_IS_BYTE  0x20 //0010 0000
        //#define ASF_PPI_FLAG_PACKET_LENGTH_FIELD_IS_WORD  0x40 //0100 0000
        //#define ASF_PPI_FLAG_PACKET_LENGTH_FIELD_IS_DWORD 0x60 //0110 0000
        //#define ASF_PPI_MASK_PACKET_LENGTH_FIELD_SIZE     0x60 //0110 0000
 
        //// PL_FLAG - Payload flags
        //#define ASF_PL_FLAG_REPLICATED_DATA_LENGTH_FIELD_IS_BYTE   0x01 //0000 0001
        //#define ASF_PL_FLAG_REPLICATED_DATA_LENGTH_FIELD_IS_WORD   0x02 //0000 0010
        //#define ASF_PL_FLAG_REPLICATED_DATA_LENGTH_FIELD_IS_DWORD  0x03 //0000 0011
        //#define ASF_PL_MASK_REPLICATED_DATA_LENGTH_FIELD_SIZE      0x03 //0000 0011
 
        //#define ASF_PL_FLAG_OFFSET_INTO_MEDIA_OBJECT_LENGTH_FIELD_IS_BYTE  0x04 //0000 0100
        //#define ASF_PL_FLAG_OFFSET_INTO_MEDIA_OBJECT_LENGTH_FIELD_IS_WORD  0x08 //0000 1000
        //#define ASF_PL_FLAG_OFFSET_INTO_MEDIA_OBJECT_LENGTH_FIELD_IS_DWORD 0x0c //0000 1100
        //#define ASF_PL_MASK_OFFSET_INTO_MEDIA_OBJECT_LENGTH_FIELD_SIZE     0x0c //0000 1100
 
        //#define ASF_PL_FLAG_MEDIA_OBJECT_NUMBER_LENGTH_FIELD_IS_BYTE  0x10 //0001 0000
        //#define ASF_PL_FLAG_MEDIA_OBJECT_NUMBER_LENGTH_FIELD_IS_WORD  0x20 //0010 0000
        //#define ASF_PL_FLAG_MEDIA_OBJECT_NUMBER_LENGTH_FIELD_IS_DWORD 0x30 //0011 0000
        //#define ASF_PL_MASK_MEDIA_OBJECT_NUMBER_LENGTH_FIELD_SIZE     0x30 //0011 0000
 
        //#define ASF_PL_FLAG_STREAM_NUMBER_LENGTH_FIELD_IS_BYTE  0x40 //0100 0000
        //#define ASF_PL_MASK_STREAM_NUMBER_LENGTH_FIELD_SIZE     0xc0 //1100 0000
 
        //#define ASF_PL_FLAG_PAYLOAD_LENGTH_FIELD_IS_BYTE  0x40 //0100 0000
        //#define ASF_PL_FLAG_PAYLOAD_LENGTH_FIELD_IS_WORD  0x80 //1000 0000
        //#define ASF_PL_MASK_PAYLOAD_LENGTH_FIELD_SIZE     0xc0 //1100 0000
 
        //#define ASF_PL_FLAG_KEY_FRAME 0x80 //1000 0000

        #endregion

        public static string ToFourCharacterCode(byte[] identifier, int offset = 0, int count = 4) { return Encoding.UTF8.GetString(identifier, offset, count); }

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
            throw new NotImplementedException();
        }


        public override IEnumerator<Element> GetEnumerator()
        {
            while (Remaining > 2)
            {
                Element next = ReadNext();
                if (next != null) yield return next;
                else yield break;


                Skip(next.Size - 1);
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
