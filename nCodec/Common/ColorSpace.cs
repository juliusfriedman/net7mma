using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nVideo
{
    public class ColorSpace
    {
        public static int MaximumPlanes = 4;

        public int nComp;

        public int[] compPlane;

        public int[] compWidth;

        public int[] compHeight;

        ColorSpace(int nComp, int[] compPlane, int[] compWidth, int[] compHeight)
        {
            this.nComp = nComp;
            this.compPlane = compPlane;
            this.compWidth = compWidth;
            this.compHeight = compHeight;
        }


        public static ColorSpace RGB = new ColorSpace(3, new int[] { 0, 0, 0 }, new int[] { 0, 0, 0 }, new int[] { 0, 0, 0 }),

        YUV420 = new ColorSpace(3, new int[] { 0, 1, 2 }, new int[] { 0, 1, 1 }, new int[] { 0, 1, 1 }),

        YUV420J = new ColorSpace(3, new int[] { 0, 1, 2 }, new int[] { 0, 1, 1 }, new int[] { 0, 1, 1 }),

        YUV422 = new ColorSpace(3, new int[] { 0, 1, 2 }, new int[] { 0, 1, 1 }, new int[] { 0, 0, 0 }),

        YUV422J = new ColorSpace(3, new int[] { 0, 1, 2 }, new int[] { 0, 1, 1 }, new int[] { 0, 0, 0 }),

        YUV444 = new ColorSpace(3, new int[] { 0, 1, 2 }, new int[] { 0, 0, 0 }, new int[] { 0, 0, 0 }),

        YUV444J = new ColorSpace(3, new int[] { 0, 1, 2 }, new int[] { 0, 0, 0 }, new int[] { 0, 0, 0 }),

        YUV422_10 = new ColorSpace(3, new int[] { 0, 1, 2 }, new int[] { 0, 1, 1 }, new int[] { 0, 0, 0 }),

        GREY = new ColorSpace(1, new int[] { 0 }, new int[] { 0 }, new int[] { 0 }),

        MONO = new ColorSpace(1, new int[] { 0, 0, 0 }, new int[] { 0, 0, 0 }, new int[] { 0, 0, 0 }),

        YUV444_10 = new ColorSpace(3, new int[] { 0, 1, 2 }, new int[] { 0, 0, 0 }, new int[] { 0, 0, 0 });
    }
}
