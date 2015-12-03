using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Codec
{
    public sealed class Codecs
    {
        static readonly HashSet<Media.Codec.Interfaces.ICodec> m_RegisteredCodecs = new HashSet<Interfaces.ICodec>();

        public static IEnumerable<Media.Codec.Interfaces.ICodec> GetAllCodecs()
        {
            return m_RegisteredCodecs;
        }

        public static Media.Codec.Interfaces.ICodec GetCodec(string name)
        {
            return m_RegisteredCodecs.FirstOrDefault(c=> string.Compare(c.Name, name, true) == 0);
        }

        public static bool RegisterCodec(Media.Codec.Interfaces.ICodec codec)
        {
            return m_RegisteredCodecs.Add(codec);
        }

        public static bool UnregisterCodec(Media.Codec.Interfaces.ICodec codec)
        {
            return m_RegisteredCodecs.Remove(codec);
        }
    }
}
