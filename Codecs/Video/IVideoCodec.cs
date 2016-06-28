using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Codecs.Video
{
    public interface IVideoCodec : Codec.Interfaces.ICodec
    {
        //Codec.Size
        //MinimumWidth, MinimumHeight

        //MaximumWidth, MaximumHeight

        //???
        //ImageFormat Format;

        //IEnumerable<Image.Format> Supported
    }
}
