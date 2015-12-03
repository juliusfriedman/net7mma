using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Codec.Interfaces
{
    public interface ISample
    {
        ICodec Codec { get; }

        Media.Common.MemorySegment Data { get; }
    }
}
