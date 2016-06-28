using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nVideo
{
    public class VideoEncoder
    {
        public Guid Id { get; internal protected set; }

        public virtual ColorSpace[] SupportedColorSpaces
        {
            get
            {
                return null;
            }
        }

        public virtual void EncodeFrame(Picture pic, System.IO.MemoryStream output)
        {
            return;
        }
    }
}
