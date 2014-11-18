using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Codecs.Video.Mpeg1
{
    public static class StartCode
    {
        public static byte[] Prefix = new byte[] { 0x00, 0x00, 0x01 };
    }
}
