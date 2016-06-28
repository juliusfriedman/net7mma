using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nVideo.Codecs.H264
{
    public class Intra16x16PredictionBuilder
    {

        public static void predictWithMode(int predMode, int[] residual, bool leftAvailable, bool topAvailable,
                int[] leftRow, int[] topLine, int[] topLeft, int x)
        {
            switch (predMode)
            {
                case 0:
                    predictVertical(residual, topAvailable, topLine, x);
                    break;
                case 1:
                    predictHorizontal(residual, leftAvailable, leftRow, x);
                    break;
                case 2:
                    predictDC(residual, leftAvailable, topAvailable, leftRow, topLine, x);
                    break;
                case 3:
                    predictPlane(residual, leftAvailable, topAvailable, leftRow, topLine, topLeft, x);
                    break;
            }

        }

        public static void predictVertical(int[] residual, bool topAvailable,
                int[] topLine, int x)
        {
            int off = 0;
            for (int j = 0; j < 16; j++)
            {
                for (int i = 0; i < 16; i++, off++)
                    residual[off] = CABAC.clip(residual[off] + topLine[x + i], 0, 255);
            }
        }

        public static void predictHorizontal(int[] residual, bool leftAvailable, int[] leftRow,
                 int x)
        {
            int off = 0;
            for (int j = 0; j < 16; j++)
            {
                for (int i = 0; i < 16; i++, off++)
                    residual[off] = CABAC.clip(residual[off] + leftRow[j], 0, 255);
            }
        }

        public static void predictDC(int[] residual, bool leftAvailable, bool topAvailable, int[] leftRow,
                int[] topLine, int x)
        {
            int s0;
            if (leftAvailable && topAvailable)
            {
                s0 = 0;
                for (int i = 0; i < 16; i++)
                    s0 += leftRow[i];
                for (int i = 0; i < 16; i++)
                    s0 += topLine[x + i];

                s0 = (s0 + 16) >> 5;
            }
            else if (leftAvailable)
            {
                s0 = 0;
                for (int i = 0; i < 16; i++)
                    s0 += leftRow[i];
                s0 = (s0 + 8) >> 4;
            }
            else if (topAvailable)
            {
                s0 = 0;
                for (int i = 0; i < 16; i++)
                    s0 += topLine[x + i];
                s0 = (s0 + 8) >> 4;
            }
            else
            {
                s0 = 128;
            }

            for (int i = 0; i < 256; i++)
                residual[i] = CABAC.clip(residual[i] + s0, 0, 255);
        }

        public static void predictPlane(int[] residual, bool leftAvailable, bool topAvailable, int[] leftRow,
                int[] topLine, int[] topLeft, int x)
        {
            int H = 0;

            for (int i = 0; i < 7; i++)
            {
                H += (i + 1) * (topLine[x + 8 + i] - topLine[x + 6 - i]);
            }
            H += 8 * (topLine[x + 15] - topLeft[0]);

            int V = 0;
            for (int j = 0; j < 7; j++)
            {
                V += (j + 1) * (leftRow[8 + j] - leftRow[6 - j]);
            }
            V += 8 * (leftRow[15] - topLeft[0]);

            int c = (5 * V + 32) >> 6;
            int b = (5 * H + 32) >> 6;
            int a = 16 * (leftRow[15] + topLine[x + 15]);

            int off = 0;
            for (int j = 0; j < 16; j++)
            {
                for (int i = 0; i < 16; i++, off++)
                {
                    int val = CABAC.clip((a + b * (i - 7) + c * (j - 7) + 16) >> 5, 0, 255);
                    residual[off] = CABAC.clip(residual[off] + val, 0, 255);
                }
            }
        }
    }
}
