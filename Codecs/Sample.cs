using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Codec
{
    public class Sample : Common.CommonDisposable, Media.Codec.Interfaces.ISample
    {
        //What codec is this sample relevent to
        public readonly Media.Codec.Interfaces.ICodec Codec;

        public readonly MediaType MediaType;

        public readonly Common.MemorySegment Data;

        //Already have base.Created
        public readonly long Timestamp;

        public Sample(MediaType type, Common.MemorySegment data, Media.Codec.Interfaces.ICodec codec = null, long timestamp = 0, bool shouldDispose = true)
            :base(shouldDispose)
        {
            Codec = codec;

            MediaType = type;

            Data = data;

            Timestamp = timestamp;
        }

        public Sample(MediaType type, byte[] data)
            : this(type, new Common.MemorySegment(data))
        {

        }

        public Sample(MediaType type, byte[] data, int offset)
            : this(type, new Common.MemorySegment(data, offset))
        {

        }

        public Sample(MediaType type, byte[] data, int offset, int length)
            : this(type, new Common.MemorySegment(data, offset, length))
        {

        }

        Media.Codec.Interfaces.ICodec Media.Codec.Interfaces.ISample.Codec
        {
            get { return Codec; }
        }


        Common.MemorySegment Interfaces.ISample.Data
        {
            get { return Data; }
        }
    }
}
