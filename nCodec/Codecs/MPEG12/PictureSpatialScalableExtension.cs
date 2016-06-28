using nVideo.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nVideo.Codecs.MPEG12
{
    public class PictureSpatialScalableExtension
    {
        public int lower_layer_temporal_reference;
        public int lower_layer_horizontal_offset;
        public int lower_layer_vertical_offset;
        public int spatial_temporal_weight_code_table_index;
        public int lower_layer_progressive_frame;
        public int lower_layer_deinterlaced_field_select;

        public static PictureSpatialScalableExtension read(BitReader inb)
        {
            PictureSpatialScalableExtension psse = new PictureSpatialScalableExtension();

            psse.lower_layer_temporal_reference = inb.readNBit(10);
            inb.read1Bit();
            psse.lower_layer_horizontal_offset = inb.readNBit(15);
            inb.read1Bit();
            psse.lower_layer_vertical_offset = inb.readNBit(15);
            psse.spatial_temporal_weight_code_table_index = inb.readNBit(2);
            psse.lower_layer_progressive_frame = inb.read1Bit();
            psse.lower_layer_deinterlaced_field_select = inb.read1Bit();

            return psse;
        }

        public void write(BitWriter outb)
        {
            outb.writeNBit(lower_layer_temporal_reference, 10);
            outb.write1Bit(1); // todo: verify this
            outb.writeNBit(lower_layer_horizontal_offset, 15);
            outb.write1Bit(1); // todo: verify this
            outb.writeNBit(lower_layer_vertical_offset, 15);
            outb.writeNBit(spatial_temporal_weight_code_table_index, 2);
            outb.write1Bit(lower_layer_progressive_frame);
            outb.write1Bit(lower_layer_deinterlaced_field_select);
        }
    }
}
