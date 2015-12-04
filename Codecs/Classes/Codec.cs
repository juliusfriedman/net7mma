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

        public Codec(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new System.InvalidOperationException("name cannot be null or consist only of whitespace.");

            Name = name;

            Id = ParseGuidAttribute(GetType());

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

        public virtual Media.Codec.Interfaces.ISample CreateSample(byte[] data, long timestamp = 0, bool shouldDispose = true)
        {
            return new Media.Codec.Sample(MediaTypes, new Common.MemorySegment(data), this, timestamp, shouldDispose);
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
