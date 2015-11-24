namespace Media.Codecs.Image
{
    public enum Format
    {
        Unknown,
        RGB,
        YUV,
        YUV_400,
        YUV_420,
        YUV_421,
        YUV_222,
        YUV_224,
        YUV_422,
        YUV_444
    }

    //Could be a class, and could contain the logic for pixel translation

    //All formats would be registered before any Image could be created.

    public class Image : Common.BaseDisposable
    {
        protected readonly Format m_Format;
        protected readonly int m_Width;
        protected readonly int m_Height;

        protected readonly int m_Size;
        protected readonly byte[] m_Data;

        public Image(Format format, int width, int height)
        {
            m_Format = format;
            m_Width = width;
            m_Height = height;

            m_Size = width * height;
            
            switch (format)
            {
                case Format.YUV_400: break;
                case Format.YUV_420: m_Size *= 3 / 2; break;
                case Format.YUV_422:
                case Format.YUV_224: m_Size *= 2; break;
                case Format.YUV_444: m_Size *= 3; break;
            }

            m_Data = new byte[m_Size];
        }

        public int Width { get { return m_Width; } }
        public int Height { get { return m_Height; } }
        public int Size { get { return m_Size; } }
        public byte[] Data { get { return m_Data; } }

        public void convertYUVtoRGB(int[] pixels)
        {
            int scaleX, scaleY;

            switch (m_Format)
            {
                case Format.YUV_400: 
                case Format.YUV_444: scaleX = 1; scaleY = 1; break;
                case Format.YUV_420: scaleX = 2; scaleY = 2; break;
                case Format.YUV_422: scaleX = 1; scaleY = 2; break;
                case Format.YUV_224: scaleX = 2; scaleY = 1; break;
                default: scaleX = 1; scaleY = 1; break;
            }

            int base_y = 0;
            int base_u = base_y + m_Width * m_Height;
            int base_v = base_u + (m_Width / scaleX) * (m_Height / scaleY);
            int stride_y = m_Width;
            int stride_u = m_Width / scaleX;
            int stride_v = m_Width / scaleX;
            byte by = (byte)128;
            byte bu = (byte)128;
            byte bv = (byte)128;

            for (int y = 0; y < m_Height; y++)
            {
                for (int x = 0; x < m_Width; x++)
                {
                    by = m_Data[base_y + stride_y * y + x];

                    if (m_Format != Format.YUV_400) //444
                    {
                        bu = m_Data[base_u + stride_u * (y / scaleY) + (x / scaleX)];
                        bv = m_Data[base_v + stride_v * (y / scaleY) + (x / scaleX)];
                    }

                    //Set the pixel
                    pixels[m_Width * y + x] = Image.convertYuvPixel(by, bu, bv);
                }
            }
        }

        static int convertYuvPixel(byte y, byte u, byte v)
        {
            int iy = y & 0xff;
            int iu = u & 0xff;
            int iv = v & 0xff;

            int miy = iy - 16, miv = iv - 128, miu = iu - 128;

            float fiy = 1.164f * miy;

            float fr = fiy + 1.596f * miv;
            float fg = fiy - 0.391f * miu - 0.813f * miv;
            float fb = fiy + 2.018f * miu;

            int ir = (int)(fr > 255 ? 255 : fr < 0 ? 0 : fr);
            int ig = (int)(fg > 255 ? 255 : fg < 0 ? 0 : fg);
            int ib = (int)(fb > 255 ? 255 : fb < 0 ? 0 : fb);

            return (ir << 16) | (ig << 8) | (ib);
        }

    }
}
