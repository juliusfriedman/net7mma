using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nVideo.Codecs.H264
{
    public class Frame : Picture
    {
        private int frameNo;
        private int[][][][] mvs;
        private Frame[][][] refsUsed;
        private bool shortTerm;
        private int poc;

        public Frame(int width, int height, int[][] data, ColorSpace color, Rect crop, int frameNo, int[][][][] mvs, Frame[][][] refsUsed, int poc)
            : base(width, height, data, color, crop)
        {
            this.frameNo = frameNo;
            this.mvs = mvs;
            this.refsUsed = refsUsed;
            this.poc = poc;
            shortTerm = true;
        }

        public static Frame createFrame(Frame pic)
        {
            Picture comp = pic.createCompatible();
            return new Frame(comp.getWidth(), comp.getHeight(), comp.getData(), comp.getColor(), pic.getCrop(),
                    pic.frameNo, pic.mvs, pic.refsUsed, pic.poc);
        }

        public Frame cropped()
        {
            Picture cropped = base.cropped();
            return new Frame(cropped.getWidth(), cropped.getHeight(), cropped.getData(), cropped.getColor(), null, frameNo, mvs, refsUsed, poc);
        }

        public void copyFrom(Frame src)
        {
            base.copyFrom(src);
            this.frameNo = src.frameNo;
            this.mvs = src.mvs;
            this.shortTerm = src.shortTerm;
            this.refsUsed = src.refsUsed;
            this.poc = src.poc;
        }

        public int getFrameNo()
        {
            return frameNo;
        }

        public int[][][][] getMvs()
        {
            return mvs;
        }

        public bool isShortTerm()
        {
            return shortTerm;
        }

        public void setShortTerm(bool shortTerm)
        {
            this.shortTerm = shortTerm;
        }

        public int getPOC()
        {
            return poc;
        }

        //public static IComparer<Frame> POCAsc = new IComparer<Frame>() {
        //    public int compare(Frame o1, Frame o2) {
        //        if (o1 == null && o2 == null)
        //            return 0;
        //        else if (o1 == null)
        //            return 1;
        //        else if (o2 == null)
        //            return -1;
        //        else
        //            return o1.poc > o2.poc ? 1 : (o1.poc == o2.poc ? 0 : -1);
        //    }
        //};

        //public static Comparator<Frame> POCDesc = new Comparator<Frame>() {
        //    public int compare(Frame o1, Frame o2) {
        //        if (o1 == null && o2 == null)
        //            return 0;
        //        else if (o1 == null)
        //            return 1;
        //        else if (o2 == null)
        //            return -1;
        //        else
        //            return o1.poc < o2.poc ? 1 : (o1.poc == o2.poc ? 0 : -1);
        //    }
        //};

        public Frame[][][] getRefsUsed()
        {
            return refsUsed;
        }

        public static IComparer<Frame> POCDesc { get; set; }

        public static IComparer<Frame> POCAsc { get; set; }
    }
}
