using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using nVideo.Common;

namespace nVideo.Codecs.MJPEG
{
    public class FrameHeader {
    /**
     * Frame header length. Specifies the length of the frame header shown in
     * Figure B.3 (see B.1.1.4).
     */
        internal int headerLength;

    /**
     * Sample precision. Specifies the precision in bits for the samples of the
     * components in the frame.
     */
    internal int bitsPerSample;

    /**
     * Number of lines. Specifies the maximum number of lines in the source
     * image. This shall be equal to the number of lines in the component with
     * the maximum number of vertical samples (see A.1.1). Value 0 indicates
     * that the number of lines shall be defined by the DNL marker and
     * parameters at the end of the first scan (see B.2.5).
     */
    internal int height;

    /**
     * Number of samples per line. Specifies the maximum number of samples per
     * line in the source image. This shall be equal to the number of samples
     * per line in the component with the maximum number of horizontal samples
     * (see A.1.1).
     */
    internal int width;

    /**
     * Number of image components in frame Specifies the number of source image
     * components in the frame. The value of Nf shall be equal to the number of
     * sets of frame component specification parameters (Ci, Hi, Vi, and Tq)
     * present in the frame header.
     */
    internal int nComp;

    public class Component {
        /**
         * Component identifier. Assigns a unique label to the ith component in
         * the sequence of frame component specification parameters. These
         * values shall be used in the scan headers to identify the components
         * in the scan. The value of Ci shall be different from the values of C1
         * through Ci 1.
         */
        internal int index;
        /**
         * Horizontal sampling factor. Specifies the relationship between the
         * component horizontal dimension and maximum image dimension X (see
         * A.1.1); also specifies the number of horizontal data units of
         * component Ci in each MCU, when more than one component is encoded in
         * a scan.
         */
        internal int subH;
        /**
         * Vertical sampling factor. Specifies the relationship between the
         * component vertical dimension and maximum image dimension Y (see
         * A.1.1); also specifies the number of vertical data units of component
         * Ci in each MCU, when more than one component is encoded in a scan.
         */
        internal int subV;

        /**
         * Quantization table destination selector. Specifies one of four
         * possible quantization table destinations from which the quantization
         * table to use for dequantization of DCT coefficients of component Ci
         * is retrieved. If the decoding process uses the dequantization
         * procedure, this table shall have been installed in this destination
         * by the time the decoder is ready to decode the scan(s) containing
         * component Ci. The destination shall not be re- specified, or its
         * contents changed, until all scans containing Ci have been completed.
         */
        internal int quantTable;
    }

    public int getHmax() {
        int max = 0;
        foreach (var c in components) {
            max = Math.Max(max, c.subH);
        }
        return max;
    }

    public int getVmax() {
        int max = 0;
        foreach (var c in components) {
            max = Math.Max(max, c.subV);
        }
        return max;
    }

    Component[] components;

    public static FrameHeader read(MemoryStream iss) {
        FrameHeader frame = new FrameHeader();
        frame.headerLength = iss.getShort() & 0xffff;
        frame.bitsPerSample = iss.get() & 0xff;
        frame.height = iss.getShort() & 0xffff;
        frame.width = iss.getShort() & 0xffff;
        frame.nComp = iss.get() & 0xff;
        frame.components = new Component[frame.nComp];
        for (int i = 0; i < frame.components.Length; i++) {
            Component c = frame.components[i] = new Component();
            c.index = iss.get() & 0xff;
            int hv = iss.get() & 0xff;
            c.subH = (hv & 0xf0) >> 4;
            c.subV = (hv & 0x0f);
            c.quantTable = iss.get() & 0xff;
        }
        return frame;
    }
}
}
