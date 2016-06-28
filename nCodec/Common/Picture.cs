using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nVideo
{
    public class Picture
    {
        private ColorSpace color;

        private int width;
        private int height;

        private int[][] data;

        //Use GetXY for better performance
        byte[] memory;

        private Rect crop;

        public int GetXY(int x, int y)
        {
            return (int)(BitConverter.ToInt32(memory, (x * width * height) + (y * width)) & 0xFFFFFF00);
        }

        public Picture(int width, int height, int[][] data, ColorSpace color)
            : this(width, height, data, color, new Rect(0, 0, width, height))
        {
        }

        public Picture(int width, int height, int[][] data, ColorSpace color, Rect crop)
        {
            this.width = width;
            this.height = height;
            this.data = data;
            this.color = color;
            this.crop = crop;

            this.memory = new byte[color.nComp * (width * height)];
        }

        public Picture(Picture other) : this(other.width, other.height, other.data, other.color, other.crop)
        {
            
        }

        public static Picture Create(int width, int height, ColorSpace colorSpace)
        {
            return Create(width, height, colorSpace, null);
        }

        public static Picture Create(int width, int height, ColorSpace colorSpace, Rect crop)
        {
            int[] planeSizes = new int[ColorSpace.MaximumPlanes];
            for (int i = 0; i < colorSpace.nComp; i++)
            {
                planeSizes[colorSpace.compPlane[i]] += (width >> colorSpace.compWidth[i])
                        * (height >> colorSpace.compHeight[i]);
            }
            int nPlanes = 0;
            for (int i = 0; i < ColorSpace.MaximumPlanes; i++)
                nPlanes += planeSizes[i] != 0 ? 1 : 0;

            int[][] data = new int[nPlanes][];
            for (int i = 0, plane = 0; i < ColorSpace.MaximumPlanes; i++)
            {
                if (planeSizes[i] != 0)
                {
                    data[plane++] = new int[planeSizes[i]];
                }
            }

            return new Picture(width, height, data, colorSpace, crop);
        }

        public int getWidth()
        {
            return width;
        }

        public int getHeight()
        {
            return height;
        }

        public int[] getPlaneData(int plane)
        {
            return data[plane];
        }

        public ColorSpace getColor()
        {
            return color;
        }

        public int[][] getData()
        {
            return data;
        }

        public Rect getCrop()
        {
            return crop;
        }

        public int getPlaneWidth(int plane)
        {
            return width >> color.compWidth[plane];
        }

        public int getPlaneHeight(int plane)
        {
            return height >> color.compHeight[plane];
        }

        public bool compatible(Picture src)
        {
            return src.color == color && src.width == width && src.height == height;
        }

        public Picture createCompatible()
        {
            return Picture.Create(width, height, color);
        }

        public void copyFrom(Picture src)
        {
            if (!compatible(src))
                throw new Exception("Can not copy to incompatible picture");
            for (int plane = 0; plane < color.nComp; plane++)
            {
                if (data[plane] == null)
                    continue;
                System.Array.Copy(src.data[plane], 0, data[plane], 0, (width >> color.compWidth[plane])
                        * (height >> color.compHeight[plane]));
            }
        }

        public Picture cropped()
        {
            if (crop == null
                    || (crop.getX() == 0 && crop.getY() == 0 && crop.getWidth() == width && crop.getHeight() == height))
                return this;
            Picture result = Picture.Create(crop.getWidth(), crop.getHeight(), color);

            for (int plane = 0; plane < color.nComp; plane++)
            {
                if (data[plane] == null)
                    continue;
                cropSub(data[plane], crop.getX() >> color.compWidth[plane], crop.getY() >> color.compHeight[plane],
                        crop.getWidth() >> color.compWidth[plane], crop.getHeight() >> color.compHeight[plane],
                        width >> color.compWidth[plane], result.data[plane]);
            }

            return result;
        }

        private void cropSub(int[] src, int x, int y, int w, int h, int srcStride, int[] tgt)
        {
            int srcOff = y * srcStride + x, dstOff = 0;
            for (int i = 0; i < h; i++)
            {
                for (int j = 0; j < w; j++)
                    tgt[dstOff + j] = src[srcOff + j];

                srcOff += srcStride;
                dstOff += w;
            }
        }

        public void setCrop(Rect crop)
        {
            this.crop = crop;
        }

        public int getCroppedWidth()
        {
            return crop == null ? width : crop.getWidth();
        }

        public int getCroppedHeight()
        {
            return crop == null ? height : crop.getHeight();
        }
    }
}
