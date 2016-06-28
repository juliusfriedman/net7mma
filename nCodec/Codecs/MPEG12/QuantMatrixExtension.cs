using nVideo.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nVideo.Codecs.MPEG12
{
    public class QuantMatrixExtension
    {

        public int[] intra_quantiser_matrix;
        public int[] non_intra_quantiser_matrix;
        public int[] chroma_intra_quantiser_matrix;
        public int[] chroma_non_intra_quantiser_matrix;

        public static QuantMatrixExtension read(BitReader inn) {
        QuantMatrixExtension qme = new QuantMatrixExtension();
        if (inn.read1Bit() != 0)
            qme.intra_quantiser_matrix = readQMat(inn);
        if (inn.read1Bit() != 0)
            qme.non_intra_quantiser_matrix = readQMat(inn);
        if (inn.read1Bit() != 0)
            qme.chroma_intra_quantiser_matrix = readQMat(inn);
        if (inn.read1Bit() != 0)
            qme.chroma_non_intra_quantiser_matrix = readQMat(inn);

        return qme;
    }

        private static int[] readQMat(BitReader inb)
        {
            int[] qmat = new int[64];
            for (int i = 0; i < 64; i++)
                qmat[i] = inb.readNBit(8);
            return qmat;
        }

        public void write(BitWriter ob)
        {
            ob.write1Bit(intra_quantiser_matrix != null ? 1 : 0);
            if (intra_quantiser_matrix != null)
                writeQMat(intra_quantiser_matrix, ob);
            ob.write1Bit(non_intra_quantiser_matrix != null ? 1 : 0);
            if (non_intra_quantiser_matrix != null)
                writeQMat(non_intra_quantiser_matrix, ob);
            ob.write1Bit(chroma_intra_quantiser_matrix != null ? 1 : 0);
            if (chroma_intra_quantiser_matrix != null)
                writeQMat(chroma_intra_quantiser_matrix, ob);
            ob.write1Bit(chroma_non_intra_quantiser_matrix != null ? 1 : 0);
            if (chroma_non_intra_quantiser_matrix != null)
                writeQMat(chroma_non_intra_quantiser_matrix, ob);
        }

        private void writeQMat(int[] matrix, BitWriter ob)
        {
            for (int i = 0; i < 64; i++)
                ob.writeNBit(matrix[i], 8);
        }
    }
}
