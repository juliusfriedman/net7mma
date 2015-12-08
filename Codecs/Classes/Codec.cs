using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//Maybe should be CodecInfo

namespace Media.Codec
{
    public abstract class Codec : Interfaces.ICodec
    {
        public static Guid ParseGuidAttribute(Type type)
        {
            object[] attributes = type.Assembly.GetCustomAttributes(typeof(System.Runtime.InteropServices.GuidAttribute), true);

            if (Common.Extensions.Array.ArrayExtensions.IsNullOrEmpty(attributes)) throw new System.InvalidOperationException("No GuidAttribute Attribute Found");

            System.Runtime.InteropServices.GuidAttribute attribute = (System.Runtime.InteropServices.GuidAttribute)attributes[0];

            Guid result;

            if (false == System.Guid.TryParse(attribute.Value, out result)) throw new System.InvalidOperationException("Invalid GuidAttribute Attribute Found");

            return result;
        }

        public readonly string Name;

        public readonly Guid Id;

        public readonly int DefaultComponentCount, DefaultBitsPerComponent;

        /// <summary>
        /// Defines the byte order used by default in the codec.
        /// </summary>
        public readonly Common.Binary.ByteOrder DefaultByteOrder;

        public readonly DataLayout DefaultDataLayout = DataLayout.Packed;

        public Codec(string name, Media.Codec.MediaType mediaType, Common.Binary.ByteOrder defaultByteOrder, int defaultComponentCount, int defaultBitsPerComponent)
        {
            MediaTypes = mediaType;

            if (string.IsNullOrWhiteSpace(name)) throw new System.InvalidOperationException("name cannot be null or consist only of whitespace.");

            Name = name;

            try
            {
                Id = ParseGuidAttribute(GetType());
            }
            catch
            {
                Id = Guid.NewGuid();
            }

            DefaultComponentCount = defaultComponentCount;

            DefaultBitsPerComponent = defaultBitsPerComponent;

            Codecs.TryRegisterCodec(this);
        }

        Guid Interfaces.ICodec.Id
        {
            get { return Id; }
        }

        string Interfaces.ICodec.Name
        {
            get { return Name; }
        }

        //Virtuals cause extra overhead, they aren't even really needed here until the pattern is developed.

        public virtual MediaType MediaTypes
        {
            get;
            protected set;
        }

        public virtual bool CanEncode
        {
            get;
            protected set;
        }

        public virtual bool CanDecode
        {
            get;
            protected set;
        }

        public virtual Media.Codec.Interfaces.IMediaBuffer CreateBuffer(byte[] data, long timestamp = 0, bool shouldDispose = true)
        {
            return new Media.Codec.MediaBuffer(MediaTypes, DefaultDataLayout, new Common.MemorySegment(data), DefaultByteOrder, DefaultBitsPerComponent, DefaultComponentCount, this, timestamp, shouldDispose);
        }

        Interfaces.IEncoder Interfaces.ICodec.Encoder
        {
            get { return null; }
        }

        Interfaces.IDecoder Interfaces.ICodec.Decoder
        {
            get { return null; }
        }
    }
}
