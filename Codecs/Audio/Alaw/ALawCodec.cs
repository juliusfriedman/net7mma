using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Codecs.Audio.Alaw
{
    public class ALawCodec : IAudioCodec //: Media.Codec.Codec
    {
        public const string Name = "ALaw";

        public static readonly System.Guid Id;

        static ALawCodec()
        {
            object[] attributes = typeof(ALawCodec).Assembly.GetCustomAttributes(typeof(System.Runtime.InteropServices.GuidAttribute), true);

            if (Common.Extensions.Array.ArrayExtensions.IsNullOrEmpty(attributes)) throw new System.InvalidOperationException("No GuidAttribute Attribute Found");

            System.Runtime.InteropServices.GuidAttribute attribute = (System.Runtime.InteropServices.GuidAttribute)attributes[0];

            if (false == System.Guid.TryParse(attribute.Value, out Id)) throw new System.InvalidOperationException("Invalid GuidAttribute Attribute Found");
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
