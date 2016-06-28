using nVideo.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace nVideo.Codecs.MPEG12
{
    public class SequenceScalableExtension
    {

        public static int DATA_PARTITIONING = 0;
        public static int SPATIAL_SCALABILITY = 1;
        public static int SNR_SCALABILITY = 2;
        public static int TEMPORAL_SCALABILITY = 3;

        public int scalable_mode;
        public int layer_id;
        public int lower_layer_prediction_horizontal_size;
        public int lower_layer_prediction_vertical_size;
        public int horizontal_subsampling_factor_m;
        public int horizontal_subsampling_factor_n;
        public int vertical_subsampling_factor_m;
        public int vertical_subsampling_factor_n;
        public int picture_mux_enable;
        public int mux_to_progressive_sequence;
        public int picture_mux_order;
        public int picture_mux_factor;

        public static SequenceScalableExtension read(BitReader inb) {
        SequenceScalableExtension sse = new SequenceScalableExtension();
        sse.scalable_mode = inb.readNBit(2);
        sse.layer_id = inb.readNBit(4);

        if (sse.scalable_mode == SPATIAL_SCALABILITY) {
            sse.lower_layer_prediction_horizontal_size = inb.readNBit(14);
            inb.read1Bit();
            sse.lower_layer_prediction_vertical_size = inb.readNBit(14);
            sse.horizontal_subsampling_factor_m = inb.readNBit(5);
            sse.horizontal_subsampling_factor_n = inb.readNBit(5);
            sse.vertical_subsampling_factor_m = inb.readNBit(5);
            sse.vertical_subsampling_factor_n = inb.readNBit(5);
        }

        if (sse.scalable_mode == TEMPORAL_SCALABILITY) {
            sse.picture_mux_enable = inb.read1Bit();
            if (sse.picture_mux_enable != 0)
                sse.mux_to_progressive_sequence = inb.read1Bit();
            sse.picture_mux_order = inb.readNBit(3);
            sse.picture_mux_factor = inb.readNBit(3);
        }

        return sse;
    }

        public void write(BitWriter outb)
        {
            outb.writeNBit(scalable_mode, 2);
            outb.writeNBit(layer_id, 4);

            if (scalable_mode == SPATIAL_SCALABILITY)
            {
                outb.writeNBit(lower_layer_prediction_horizontal_size, 14);
                outb.write1Bit(1); // todo: check this
                outb.writeNBit(lower_layer_prediction_vertical_size, 14);
                outb.writeNBit(horizontal_subsampling_factor_m, 5);
                outb.writeNBit(horizontal_subsampling_factor_n, 5);
                outb.writeNBit(vertical_subsampling_factor_m, 5);
                outb.writeNBit(vertical_subsampling_factor_n, 5);
            }

            if (scalable_mode == TEMPORAL_SCALABILITY)
            {
                outb.write1Bit(picture_mux_enable);
                if (picture_mux_enable != 0)
                    outb.write1Bit(mux_to_progressive_sequence);
                outb.writeNBit(picture_mux_order, 3);
                outb.writeNBit(picture_mux_factor, 3);
            }
        }
    }
}
