using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nVideo.Codecs.H264
{
    public class AspectRatio
    {

        public static AspectRatio Extended_SAR = new AspectRatio(255);

        private int value;

        private AspectRatio(int value)
        {
            this.value = value;
        }

        public static AspectRatio fromValue(int value)
        {
            if (value == Extended_SAR.value)
            {
                return Extended_SAR;
            }
            return new AspectRatio(value);
        }

        public int getValue()
        {
            return value;
        }
    }
}
