using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Codec
{
    public class Encoder : Media.Codec.Interfaces.IEncoder
    {
        //public IEnumerable<Media.Codec.Interfaces.ICodec> InputFormats { get; protected set; }

        //public Media.Codec.Interfaces.ICodec OutputFormat { get; protected set; }

        public Media.Codec.Interfaces.ICodec Codec { get; protected set; }

        //Encode(byte[], int offset, int length)

        //Encode(offset, length)

    }
}
