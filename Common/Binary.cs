#region Copyright
/*
This file came from Managed Media Aggregation, You can always find the latest version @ https://net7mma.codeplex.com/
  
 Julius.Friedman@gmail.com / (SR. Software Engineer ASTI Transportation Inc. http://www.asti-trans.com)

Permission is hereby granted, free of charge, 
 * to any person obtaining a copy of this software and associated documentation files (the "Software"), 
 * to deal in the Software without restriction, 
 * including without limitation the rights to :
 * use, 
 * copy, 
 * modify, 
 * merge, 
 * publish, 
 * distribute, 
 * sublicense, 
 * and/or sell copies of the Software, 
 * and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * 
 * 
 * JuliusFriedman@gmail.com should be contacted for further details.

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. 
 * 
 * IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, 
 * DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, 
 * TORT OR OTHERWISE, 
 * ARISING FROM, 
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 * 
 * v//
 */
#endregion

#region Using Statements

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Media.Common;

#endregion

namespace Media.Common
{
    #region Binary class

    /// <summary>
    /// Provides methods which are useful when working with binary data
    /// </summary>
    [CLSCompliant(true)]
    public static class Binary
    {
        #region Exceptions

        /// <summary>
        /// An exception utilized when a value larger than allowed is utilized in a quarter bit.
        /// </summary>
        internal static OverflowException QuarterBitOverflow = new OverflowException("Quarter bits cannot store values larger than 3.");

        /// <summary>
        /// An exception utilized when a value larger than allowed is utilized in a nibble.
        /// </summary>
        internal static OverflowException NybbleOverflow = new OverflowException("Cannot store a number higher than 15 in a nybble.");

        /// <summary>
        /// An exception utilized when a value larger than allowed is utilized in a 5 bit field.
        /// </summary>
        internal static OverflowException FiveBitOverflow = new OverflowException("Cannot store a number higher than 31 in 5 bits.");

        /// <summary>
        /// An exception utilized when a value larger than allowed is utilized in a 7 bit field (sbyte.MaxValue is not utilized because of the dependence on the value of sign bit)
        /// </summary>
        internal static OverflowException SevenBitOverflow = new OverflowException("Cannot store a number higher than 127 in a 7 bit structure.");

        /// <summary>
        /// An exception utilized when a value larger than allowed is utilized in a 16 bit field.
        /// </summary>
        internal static OverflowException SixteenBitOverflow = new OverflowException("Cannot store a number higher than 65535 in a 16 bit structue.");

        /// <summary>
        /// A string which contains has a format in which all Overflow exceptions Readd by the ReadBinaryOverflowException function utilize.
        /// </summary>
        static string OverFlowExceptionFormat = "{0} overflowed. Cannot store a number lower than {1} or higher than {2} in a {3} structure.";

        internal static OverflowException CreateOverflowException<T>(string parameter, T value, string minValue, string maxValue)
        {
            return new OverflowException(string.Format(OverFlowExceptionFormat, parameter, value.ToString(), minValue, maxValue));
        }

        #endregion

        #region Statics

        internal static byte[] BitsSetTable;

        internal static byte[] BitsReverseTable;

        static Binary()
        {
            BitsSetTable = new byte[256];
            BitsReverseTable = new byte[256];

            //Start at 2, since BitsSetTable[0] = BitsReverseTable[0] = 0 
            BitsSetTable[1] = BitsReverseTable[128] = 1; 
            BitsSetTable[128] = BitsReverseTable[1] = 128;
            BitsSetTable[255] = 8; BitsReverseTable[255] = 255;
            //253 Operations [2 -> 254]
            for (int i = 2; i < byte.MaxValue; ++i)
            {
                byte reverse = MultiplyReverseU8((byte)i);
                BitsReverseTable[i] = reverse;
                BitsSetTable[reverse] = BitsSetTable[i] = (byte)((i & 1) + BitsSetTable[i / 2]);
            }
        }

        #endregion

        #region Maximum Values

        /// <summary>
        /// The amount of bits it a single octet
        /// </summary>
        public const byte BitSize = 8;

        /// <summary>
        /// (0000)1111 in Binary
        /// </summary>
        public const byte FourBitMaxValue = 15;

        /// <summary>
        /// (000)11111 in Binary
        /// </summary>
        public const byte FiveBitMaxValue = 31;

        /// <summary>
        /// An octet which represents a set of 8 bits with only the 0th  bit clear. (127 Decimal)
        /// </summary>
        /// <remarks>
        /// 01111111 in Binary
        /// </remarks>
        public const byte SevenBitMaxValue = (byte)sbyte.MaxValue;

        /// <summary>
        /// 00000000 11111111 11111111 11111111 in binary
        /// </summary>
        public const int U24MaxValue = 16777215;

        #endregion

        #region Sizes

        //16
        public const byte DoubleBitSize = BitSize * 2;

        //24
        public const byte TripleBitSize = BitSize * 3;

        //32
        public const byte QuadrupleBitSize = BitSize * 4;

        #endregion

        #region Methods

        /// <summary>
        /// Determines the amount of bits set in the byte
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public static int BitsSet(byte b) { return BitsSetTable[b]; }

        /// <summary>
        /// Determines the amount of bits not set in the byte
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public static int BitsUnSet(byte b) { return BitSize - BitsSetTable[b]; }

        public static int BitsSet(int i) { return BitConverter.GetBytes(i).Sum(b => BitsSet(b)); }

        public static int BitsUnSet(int i) { return QuadrupleBitSize - BitConverter.GetBytes(i).Sum(b => BitsSet(b)); }

        /// <summary>
        /// Reads the given amount of bits from an octet via shifting.
        /// </summary>
        /// <param name="octet">The octet to read the bits from</param>
        /// <param name="shiftLeft">The amount of bits to shift left</param>
        /// <param name="shiftRight">The amount of bits to shift right</param>
        /// <returns>The 32 bit value remanining in the register after shifting.</returns>
        public static int ReadBitsWithShift(byte octet, int shiftLeft, int shiftRight, bool reverse = false)
        {
            return octet == 0 ? 0 : reverse ? ((octet >> shiftLeft) << shiftRight) : ((octet << shiftLeft) >> shiftRight);
        }

        /// <summary>
        /// Retrieves a bit from the given octet via shifting to discard bits.
        /// </summary>
        /// <param name="octet">The octet to reveal the bit field in</param>
        /// <param name="shiftLeft">The amount of shifting requried to put the bit at index 0</param>
        /// <param name="shiftRight">The amount of shifting requried to put the bit at index 7</param>
        /// <returns>True if the bit field is set, otherwise false.</returns>
        public static bool ReadBitWithShift(ref byte octet, int shiftLeft, int shiftRight, bool reverse = false)
        {
            return octet == 0 ? false : (reverse ? ReadBitsWithShift(octet, shiftRight, shiftLeft) : ReadBitsWithShift(octet, shiftLeft, shiftRight)) > 0;
        }

        /// <summary>
        /// Retrieves a bitfield from the given octet via shifting to discard bits.
        /// </summary>
        /// <param name="octet">The octet to reveal the bit field in</param>
        /// <param name="index">The non 0 based index of the octet to retrieve a bit from</param>
        /// <returns>True if the bit field is set, otherwise false.</returns>
        public static bool GetBit(ref byte octet, int index)
        {

            if (index < 0 || index > BitSize) throw new ArgumentOutOfRangeException("index", "Must be a value 0 - 8");

            return octet == 0 ? false : ReadBitsWithShift(octet, BitSize - index, BitSize - index) > 0;
        }

        /// <summary>
        /// Provides an implementation of setting a bit in a highly optomized fashion.
        /// Returns the value previously set in the bit.
        /// If the given bit is already set then no further modification is perfored.
        /// </summary>
        /// <param name="octet">The octet to set the bit in</param>
        /// <param name="index">The index of the bit to set</param>
        /// <param name="newValue">The value to put in the bit, where true = 1 and false = 0</param>
        /// <returns>The value which was previously set in the bit where true = 1 and false = 0</returns>
        internal static bool SetBit(ref byte octet, int index, bool newValue)
        {

            if (index < 0 || index > BitSize) throw new ArgumentOutOfRangeException("index", "Must be a value 0 - 8");

            //Allows writes to be read (performs two shifts to measure the value)
            bool oldValue = GetBit(ref octet, index);

            //If the newValue has been set already return
            if (oldValue == newValue) return oldValue;

            //http://en.wikipedia.org/wiki/Bit_manipulation, Depends on the Sign Bit, however the value of the sign bit could be detected
            //m_Octet &= (byte)(~(BitSize - index));

            //Sets a bit without a dependence on the sign bit or detecting it

            //Shift to the index to obtain the previously set lower bits
            //Combine with the octet shifted right to the index to obtain the previously set higher bits
            octet = (byte)((octet << index) | (octet >> index));

            //Set the bit at the index because it is not already set
            if (newValue) SetBit(ref octet, index);

            //Return the old value
            return oldValue;
        }

        /// <summary>
        /// Sets the index of the given bit to 1 if not already set
        /// </summary>
        /// <param name="octet"></param>
        /// <param name="index"></param>
        internal static void SetBit(ref byte octet, int index)
        {
            if (index < 0 || index > BitSize) throw new ArgumentOutOfRangeException("index", "Must be a value 0 - 8");

            SetBit(ref octet, index, true);
        }

        /// <summary>
        /// Sets the index of the given bit to 0 if not already set
        /// </summary>
        /// <param name="octet"></param>
        /// <param name="index"></param>
        public static void ClearBit(ref byte octet, int index)
        {
            if (index < 0 || index > BitSize) throw new ArgumentOutOfRangeException("index", "Must be a value 0 - 8");

            SetBit(ref octet, index, false);
        }

        /// <summary>
        /// Provides a method of setting a bit with XOR
        /// </summary>
        /// <param name="octet">The octet to set the bit in</param>
        /// <param name="index">The index of the bit to set</param>
        /// <remarks>
        /// http://stackoverflow.com/questions/2605913/invert-1-bit-in-c-sharp
        /// </remarks>
        public static void ToggleBit(byte octet, int index)
        {
            if (index < 0 || index > BitSize) throw new ArgumentOutOfRangeException("index", "Must be a value 0 - 8");

            octet ^= (byte)index;
        }

        /// <summary>
        /// Combines two integers into a single byte using the binary | operator truncating the higher 24 bits.
        /// </summary>
        public static byte Or(int lsb, int msb) { return (byte)(lsb | msb); }

        /// <summary>
        /// Combines two integers into a single byte using binary & operator truncating the higher 24 bits.
        /// </summary>
        public static byte And(int lsb, int msb) { return (byte)(lsb & msb); }

        //public static int Nand, Nor

        #region Reading


        /// <summary>
        /// Calculates a 64 bit value from the given parameters.
        /// Throws an <see cref="ArgumentException"/> if <paramref name="sizeInBytes"/> is less than or equal to 0.
        /// </summary>
        /// <param name="octets">The sequence of <see cref="Byte"/> to enumerate</param>
        /// <param name="offset">The offset to skip to in the enumeration</param>
        /// <param name="sizeInBytes">The size of the binary representation of the integer to calculate</param>
        /// <param name="reverse">If true the sequence will be reversed before being calculated</param>
        /// <returns>The calculated result</returns>
        public static long ReadInteger(IEnumerable<byte> octets, int offset, int sizeInBytes, bool reverse)
        {
            unchecked
            {
                if (sizeInBytes == 0) throw new ArgumentException("sizeInBytes", "Must be at least 1.");

                if (sizeInBytes > 8) throw new NotSupportedException("Only sizes up to 8 octets are supported.");

                var integerOctets = octets.Skip(offset).Take(sizeInBytes);

                //One byte only
                if (sizeInBytes == 1) return reverse ? ReverseU8(integerOctets.First()) : integerOctets.First();

                //Reverse the seqeuence if indicated.
                if (reverse) integerOctets = integerOctets.Reverse();

                //Get the first octet, the placeHolder is 0
                ulong result = integerOctets.First();

                //Faster

                //Calulcate the placeHolder value which is equal to the largest size storable in the signed representation plus 1
                ulong placeHolder = (byte.MaxValue + 1); // base 2 = 2 * (ulong)(-sbyte.MaxValue) = 256;

                //Iterate each byte in the sequence skipping the first octet.
                foreach (byte b in integerOctets.Skip(1))
                {
                    //If the byte is greater than 0
                    if (b > 0)
                    {
                        //Combine the result of the calculation of the base two value with the binary representation.
                        result |= b * placeHolder;
                    }

                    //Move the placeholder 8 bits left (This equates to a multiply by 4, [where << 4 would be a multiply of 3 etc])
                    if (BitConverter.IsLittleEndian)
                        placeHolder <<= 8;
                    else
                        placeHolder >>= 8;
                }

                //Return the result
                return (long)result;
            }
        }

      

        /// <summary>
        /// Reads a unsigned 8 bit value from the buffer at the given index.
        /// </summary>
        /// <param name="buffer">The buffer to read from</param>
        /// <param name="index">The index to read</param>
        /// <param name="reverse">A value indicating if the value should be reversed</param>
        /// <returns>
        /// The unsigned 8 bit value with respect to the reverse parameter.
        /// </returns>
        /// <summary>
        /// Note if <paramref name=" reverse"/> is true then a cast to a unsigned 64 bit value is performed.
        /// </summary>
        /// <remarks>
        /// Provided for completeness, the call overhead let alone performing the check on the reverse condition and not to mention the cast is much worse than simply hardcoding for the particular application when required to.
        /// </remarks>
        public static byte ReadU8(IEnumerable<byte> buffer, int index, bool reverse)
        {
            return (byte)Binary.ReadInteger(buffer, index, 1, reverse);
        }

        [CLSCompliant(false)]
        public static sbyte Read8(IEnumerable<byte> buffer, int index, bool reverse)
        {
            return (sbyte)Binary.ReadInteger(buffer, index, 1, reverse);
        }

        /// <summary>
        /// Reads an unsigned 16 bit value type from the given buffer.
        /// </summary>
        /// <param name="buffer">The buffer to Read the unsigned 16 bit value from</param>
        /// <param name="index">The index in the buffer to Read the value from</param>
        /// <param name="reverse">A value which indicates if the value should be reversed</param>
        /// <returns>The unsigned 16 bit value from the given buffer</returns>
        /// <summary>
        /// The <paramref name="reverse"/> is typically utilized when creating Big Endian \ Network Byte Order or encrypted values.
        /// </summary>
        [CLSCompliant(false)]
        public static ushort ReadU16(IEnumerable<byte> buffer, int index, bool reverse)
        {
            return (ushort)Binary.ReadInteger(buffer, index, 2, reverse);
        }

        public static short Read16(IEnumerable<byte> buffer, int index, bool reverse)
        {
            return (short)Binary.ReadInteger(buffer, index, 2, reverse);
        }

        /// <summary>
        /// Reads a 24 bit unsigned integer from the given buffer at the given index.
        /// </summary>
        /// <param name="buffer">The buffer to Read the unsigned 24 bit value from</param>
        /// <param name="index">The index in the buffer to Read the value from</param>
        /// <param name="reverse">A value which indicates if the value should be reversed</param>
        /// <returns>The unsigned 24 bit value in the form of a 32 bit unsigned integer</returns>
        [CLSCompliant(false)]
        public static uint ReadU24(IEnumerable<byte> buffer, int index, bool reverse)
        {
            return (uint)Binary.ReadInteger(buffer, index, 3, reverse);
        }

        public static int Read24(IEnumerable<byte> buffer, int index, bool reverse)
        {
            return (int)Binary.ReadInteger(buffer, index, 3, reverse);
        }

        /// <summary>
        /// Reads an unsgined 32 bit value from the given buffer at the specified index.
        /// </summary>
        /// <param name="buffer">The buffer to Read the unsigned 32 bit value from</param>
        /// <param name="index"></param>
        /// <param name="reverse">A value which indicates if the value should be reversed</param>
        /// <returns>The unsigned 32 bit value from the given buffer</returns>
        /// <summary>
        /// The <paramref name="reverse"/> is typically utilized when creating Big Endian \ Network Byte Order or encrypted values.
        /// </summary>
        [CLSCompliant(false)]
        public static uint ReadU32(IEnumerable<byte> buffer, int index, bool reverse)
        {
            return (uint)Binary.ReadInteger(buffer, index, 4, reverse);
        }

        public static int Read32(IEnumerable<byte> buffer, int index, bool reverse)
        {
            return (int)Binary.ReadInteger(buffer, index, 4, reverse);
        }

        [CLSCompliant(false)]
        public static ulong ReadU64(IEnumerable<byte> buffer, int index, bool reverse)
        {
            return (ulong)Binary.ReadInteger(buffer, index, 8, reverse);
        }

        public static long Read64(IEnumerable<byte> buffer, int index, bool reverse)
        {
            return (long)Binary.ReadInteger(buffer, index, 8, reverse);
        }

        #endregion

        //GetBytes Methods?

        #region Writing (Provided to reduce unsafe transition when using BitConverter)

        public static void WriteNetworkU8(byte[] buffer, int index, bool reverse, byte value)
        {
            buffer[index] = reverse ? ReverseU8(value) : value;
        }

        //Todo
        /// <summary>
        /// Writes a the given unsgined 16 bit value to the buffer at the given index.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="index"></param>
        /// <param name="reverse"></param>
        /// <param name="value"></param>
        public static void WriteNetwork16(byte[] buffer, int index, bool reverse, short value)
        {
            BitConverter.GetBytes(reverse ? System.Net.IPAddress.HostToNetworkOrder(value) : value).ToArray().CopyTo(buffer, index);
        }

        [CLSCompliant(false)]
        public static void WriteNetwork16(byte[] buffer, int index, bool reverse, ushort value)
        {
            WriteNetwork16(buffer, index, reverse, (short)value);
        }

        [CLSCompliant(false)]
        public static void WriteNetwork24(byte[] buffer, int index, bool reverse, uint value)
        {
            WriteNetwork24(buffer, index, reverse, (int)value);
        }

        public static void WriteNetwork24(byte[] buffer, int index, bool reverse, int value)
        {
            //If writing in reverse
            if (reverse)
            {
                //Start at the highest index
                buffer[index + 2] = (byte)(value & byte.MaxValue);
                buffer[index + 1] = (byte)(value >> 8 & byte.MaxValue);
                buffer[index] = (byte)(value >> 16 & byte.MaxValue);
            }
            else
            {
                //Start at the lowest index
                buffer[index] = (byte)(value & byte.MaxValue);
                buffer[index + 1] = (byte)(value >> 8 & byte.MaxValue);
                buffer[index + 2] = (byte)(value >> 16 & byte.MaxValue);
            }
        }

        [CLSCompliant(false)]
        public static void WriteNetwork32(byte[] buffer, int index, bool reverse, uint value)
        {
            WriteNetwork32(buffer, index, reverse, (int)value);
        }

        //Todo
        public static void WriteNetwork32(byte[] buffer, int index, bool reverse, int value)
        {
            BitConverter.GetBytes(reverse ? System.Net.IPAddress.HostToNetworkOrder(value) : value).ToArray().CopyTo(buffer, index);
        }

        /// <summary>
        /// Writes a Big Endian 64 bit value to the given buffer at the given index
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="index"></param>
        /// <param name="reverse">A value indicating if the given value should be written in reverse</param>
        /// <param name="value"></param>
        [CLSCompliant(false)]
        public static void WriteNetwork64(byte[] buffer, int index, bool reverse, ulong value)
        {
            WriteNetwork64(buffer, index, reverse, (long)value);
        }

        //Todo
        public static void WriteNetwork64(byte[] buffer, int index, bool reverse, long value)
        {
            BitConverter.GetBytes(reverse ? System.Net.IPAddress.HostToNetworkOrder(value) : value).ToArray().CopyTo(buffer, index);
        }


        #endregion

        #region Reversal

        /// <summary>
        /// Reverses the given unsigned 8 bit value via calulcation of the reverse value.
        /// </summary>
        /// <param name="source">The unsigned 8 bit value which is requried to be reversed</param>
        /// <returns>The reversed unsigned 8 bit value</returns>
        public static byte ReverseU8(byte source)
        {
            //http://graphics.stanford.edu/~seander/bithacks.html
            //per Rich Schroeppel in the Programming Hacks section of  Beeler, M., Gosper, R. W., and Schroeppel, R. HAKMEM. MIT AI Memo 239, Feb. 29, 1972. 
            return BitsReverseTable[source];
        }

        public static byte MultiplyReverseU8(byte source)
        {
            //http://graphics.stanford.edu/~seander/bithacks.html
            //per Rich Schroeppel in the Programming Hacks section of  Beeler, M., Gosper, R. W., and Schroeppel, R. HAKMEM. MIT AI Memo 239, Feb. 29, 1972. 
            return (byte)(((source * 0x80200802UL) & 0x0884422110UL) * 0x0101010101UL >> 32);
        }

        [CLSCompliant(false)]
        public static sbyte Reverse8(sbyte source)
        {
            return (sbyte)ReverseU8((byte)source);
        }

        [CLSCompliant(false)]
        public static ushort ReverseUnsignedShort(ref ushort source) { return (ushort)(((source & 0xFF) << 8) | ((source >> 8) & 0xFF)); }

        [CLSCompliant(false)]
        public static uint ReverseUnsignedInt(ref uint source) { return (uint)((((source & 0x000000FF) << 24) | ((source & 0x0000FF00) << 8) | ((source & 0x00FF0000) >> 8) | ((source & 0xFF000000) >> 24))); }

        /// <summary>
        /// Reverses the given unsigned 16 bit value via left and right shift and casting to a unsigned 64 bit value.
        /// </summary>
        /// <param name="source">The unsigned 16 bit value which is required to be reversed</param>
        /// <returns>The reversed unsigned 16 bit value</returns>
        [CLSCompliant(false)]
        public static ushort ReverseU16(ushort source)
        {
            if (source == 0 || source == ushort.MaxValue) return source;
            return ReverseUnsignedShort(ref source);
        }
        
        public static short Reverse16(short source)
        {
            if (source == 0 || source == short.MaxValue) return source;
            ushort unsigned = (ushort) source;
            return (short)ReverseUnsignedShort(ref unsigned);
        }

        /// <summary>
        /// Reverses the given unsigned 32 value via left and right shift and casting to a unsigned 64 bit value.
        /// </summary>
        /// <param name="source">The unsigned 32 bit value which is requried to be reversed</param>
        /// <returns>The reversed unsigned 32 bit value</returns>            
        [CLSCompliant(false)]
        public static uint ReverseU32(uint source)
        {
            if (source == 0 || source == uint.MaxValue) return source;
            return ReverseUnsignedInt(ref source);
        }

        public static int Reverse32(int source)
        {
            if (source == 0) return source;
            uint unsigned = (uint)source;
            return (int)ReverseUnsignedInt(ref unsigned);
        }

        [CLSCompliant(false)]
        public static ulong ReverseU64(ulong source)
        {
            if (source == 0 || source == ulong.MaxValue) return source;
            return RollU64(source, QuadrupleBitSize);
        }

        public static long Reverse64(long source)
        {
            if (source == 0 || source == long.MaxValue) return source;
            return Roll64(source, QuadrupleBitSize);
        }

        /// <summary>
        /// Double shifts the given unsigned 64 bit value via left and right shift on the register(s) in use to perform this operation.
        /// </summary>
        /// <param name="source">The unsgined 64 bit value to reverse</param>
        /// <param name="amount">The amount of shifting left and right to perform</param>
        /// <returns>The reversed unsigned 64 bit value</returns>
        /// <remarks>
        /// On 32 bit Architectures two registers are beging used to perform this operation and on some optomized a single instruction is unsed
        /// </remarks>
        [CLSCompliant(false)]
        public static ulong RollU64(ulong source, int amount)
        {
            if (source == 0 || source == ulong.MaxValue) return source;
            return source >> amount | source << amount;
        }

        public static long Roll64(long source, int amount)
        {
            if (source == 0 || source == long.MaxValue) return source;
            return source >> amount | source << amount;
        }

        #endregion

        #endregion
    }

    #endregion
}
