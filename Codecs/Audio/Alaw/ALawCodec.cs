using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Codecs.Audio.Alaw
{
    public class ALawCodec : /*: Media.Codec.Codec,*/ IAudioCodec
    {
        public const string Name = "ALaw";

        public static readonly System.Guid Id;

        static ALawCodec()
        {
            Id = Media.Codec.Codec.ParseGuidAttribute(typeof(ALawCodec));

            Media.Codec.Codecs.TryRegisterCodec(new ALawCodec());
        }

        System.Guid Codec.Interfaces.ICodec.Id
        {
            get { return ALawCodec.Id; }
        }

        string Codec.Interfaces.ICodec.Name
        {
            get { return ALawCodec.Name; }
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
            get { return new ALawEncoder(); }
        }

        Codec.Interfaces.IDecoder Codec.Interfaces.ICodec.Decoder
        {
            get { return new ALawDecoder(); }
        }
    }
}
