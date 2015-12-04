using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Codecs.Audio.Mulaw
{
    public class MulawCodec : Media.Codec.Codec, IAudioCodec //: Media.Codec.Codec
    {
        public const string CodecName = "MuLaw";

        public static readonly System.Guid CodecId;

        public static readonly Common.Binary.ByteOrder CodecDefaultByteOrder = Common.Binary.ByteOrder.Little;

        public static readonly int CodecDefaultChannels = 2;

        public static readonly int CodecDefaultBitsPerComponent = 16;

        public static readonly Packing CodecDefaultPacking = Packing.Logarithmic;

        //needs to be in an abstract AudioCodec : Media.Codec.Codec
        public static readonly int DefaultSampleRate = 8000;

        static MulawCodec()
        {
            CodecId = Media.Codec.Codec.ParseGuidAttribute(typeof(MulawCodec));

            Media.Codec.Codecs.TryRegisterCodec(new MulawCodec());
        }

        public MulawCodec()
            : base(CodecName, Common.Binary.ByteOrder.Little, CodecDefaultChannels, CodecDefaultBitsPerComponent)
        {
        }

        public override Codec.Interfaces.IMediaBuffer CreateBuffer(byte[] data, long timestamp = 0, bool shouldDispose = true)
        {
            return new Media.Codecs.Audio.AudioBuffer(CodecDefaultPacking, CodecDefaultByteOrder, DefaultComponentCount, DefaultSampleRate, DefaultBitsPerComponent, 1);
        }

        System.Guid Codec.Interfaces.ICodec.Id
        {
            get { return MulawCodec.CodecId; }
        }

        string Codec.Interfaces.ICodec.Name
        {
            get { return MulawCodec.CodecName; }
        }

        Codec.MediaType Codec.Interfaces.ICodec.MediaTypes
        {
            get { return Codec.MediaType.Audio; }
        }

        bool Codec.Interfaces.ICodec.CanEncode
        {
            get { return true; }
        }

        bool Codec.Interfaces.ICodec.CanDecode
        {
            get { return true; }
        }


        Codec.Interfaces.IEncoder Codec.Interfaces.ICodec.Encoder
        {
            get { return new MuLawEncoder(); }
        }

        Codec.Interfaces.IDecoder Codec.Interfaces.ICodec.Decoder
        {
            get { return new MuLawDecoder(); }
        }
    }
}
