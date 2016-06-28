using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nVideo
{
    public abstract class VideoDecoder : Media.Codecs.Video.VideoCodec
    {

        public virtual Picture Decode(byte[] data, int[][] buffer) 
        {
            return null;
        }

        //public int Probe(byte[] data)

        public abstract int probe(MemoryStream data);

        public VideoDecoder(string name, Media.Common.Binary.ByteOrder byteOrder, int defaultComponentCount, int defaultBitsPerComponent)
            : base(name, byteOrder, defaultComponentCount, defaultBitsPerComponent)
        {

        }


    }
}
