using nVideo.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nVideo.Codecs.H264
{
   public class CAVLCWriter {

    private CAVLCWriter() {
    }

    public static void writeU(BitWriter outb, int value, int n, String message)  {
        outb.writeNBit(value, n);
        //trace(message, value);
    }

    public static void writeUE(BitWriter outb, int value)  {
        int bits = 0;
        int cumul = 0;
        for (int i = 0; i < 15; i++) {
            if (value < cumul + (1 << i)) {
                bits = i;
                break;
            }
            cumul += (1 << i);
        }
        outb.writeNBit(0, bits);
        outb.write1Bit(1);
        outb.writeNBit(value - cumul, bits);
    }

    public static int golomb(int signedLevel)
    {
        if (signedLevel == 0)
            return 0;
        return (Math.Abs(signedLevel) << 1) - (~signedLevel >> 31);
    }


    public static void writeSE(BitWriter outb, int value)  {
        writeUE(outb, golomb(value));
    }

    public static void writeUE(BitWriter outb, int value, String message)  {
        writeUE(outb, value);
        //trace(message, value);
    }

    public static void writeSE(BitWriter outb, int value, String message)  {
        writeUE(outb, golomb(value));
        //trace(message, value);
    }

    public static void writeBool(BitWriter outb, bool value, String message)  {
        outb.write1Bit(value ? 1 : 0);
        //trace(message, value ? 1 : 0);
    }

    public static void writeU(BitWriter outb, int i, int n)  {
        outb.writeNBit(i, n);
    }

    public static void writeNBit(BitWriter outb, long value, int n, String message)  {
        for (int i = 0; i < n; i++) {
            outb.write1Bit((int) (value >> (n - i - 1)) & 0x1);
        }
        //trace(message, value);
    }

    public static void writeTrailingBits(BitWriter outb)  {
        outb.write1Bit(1);
        outb.flush();
    }

    public static void writeSliceTrailingBits() {
        throw new Exception("todo");
    }
}
}
