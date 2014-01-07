using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.Common
{
    #region Binary class

    /// <summary>
    /// Provides methods which are useful when working with binary data
    /// </summary>
    [CLSCompliant(false)]
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

        #region Maximum Values

        /// <summary>
        /// The amount of bits it a single octet
        /// </summary>
        internal const int BitSize = 8;

        /// <summary>
        /// (0000)1111 in Binary
        /// </summary>
        public const uint FourBitMaxValue = 15;

        /// <summary>
        /// (000)11111 in Binary
        /// </summary>
        public const byte FiveBitMaxValue = 31;

        /// <summary>
        /// An octet which represents a set of 8 bits with only the 0th  bit clear.
        /// </summary>
        /// <remarks>
        /// 01111111 in Binary
        /// </remarks>
        public const byte SevenBitMaxValue = 127;

        /// <summary>
        /// 00000000 11111111 11111111 11111111 in binary
        /// </summary>
        public const uint U24MaxValue = 16777215;

        #endregion

        #region Sizes

        public const int DoubleBitSize = BitSize * 2;

        public const int TripleBitSize = BitSize * 3;

        public const int QuadrupleBitSize = BitSize * 4;

        #endregion

        #region Methods

        /// <summary>
        /// Reads the given amount of bits from an octet via shifting.
        /// </summary>
        /// <param name="octet">The octet to read the bits from</param>
        /// <param name="shiftLeft">The amount of bits to shift left</param>
        /// <param name="shiftRight">The amount of bits to shift right</param>
        /// <returns>The 32 bit value remanining in the register after shifting.</returns>
        public static int ReadBitsWithShift(byte octet, int shiftLeft, int shiftRight)
        {
            return octet == 0 ? 0 : ((octet << shiftLeft) >> shiftRight);
        }

        /// <summary>
        /// Retrieves a bit from the given octet via shifting to discard bits.
        /// </summary>
        /// <param name="octet">The octet to reveal the bit field in</param>
        /// <param name="shiftLeft">The amount of shifting requried to put the bit at index 0</param>
        /// <param name="shiftRight">The amount of shifting requried to put the bit at index 7</param>
        /// <returns>True if the bit field is set, otherwise false.</returns>
        public static bool ReadBitWithShift(ref byte octet, int shiftLeft, int shiftRight)
        {
            return octet == 0 ? false : ReadBitsWithShift(octet, shiftLeft, shiftRight) > 0;
        }

        /// <summary>
        /// Retrieves a bitfield from the given octet via shifting to discard bits.
        /// </summary>
        /// <param name="octet">The octet to reveal the bit field in</param>
        /// <param name="index">The non 0 based index of the octet to retrieve a bit from</param>
        /// <returns>True if the bit field is set, otherwise false.</returns>
        public static bool GetBit(ref byte octet, int index)
        {
            return ReadBitsWithShift(octet, BitSize - index, BitSize - index) > 0;
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

            //Obtain the mask for the index with 1 << index if there is a value to be set
            if (newValue) octet |= (byte)(1 << index);

            //Return the old value
            return oldValue;
        }

        /// <summary>
        /// Directly sets the bit in the given octet at the given index to 1.
        /// </summary>
        /// <remarks>
        /// http://stackoverflow.com/questions/2605913/invert-1-bit-in-c-sharp
        /// </remarks>
        /// <param name="octet">The reference of the octet to modify</param>
        /// <param name="index">The index in the octet of the bit to set</param>
        internal static void SetBit(ref byte octet, int index) { octet ^= (byte)index; }

        public static void ClearBit(ref byte octet, int index) { SetBit(ref octet, index, false); }

        /// <summary>
        /// Provides a method of setting a bit with XOR
        /// </summary>
        /// <param name="octet">The octet to set the bit in</param>
        /// <param name="index">The index of the bit to set</param>
        /// <remarks>
        /// http://stackoverflow.com/questions/2605913/invert-1-bit-in-c-sharp
        /// </remarks>
        public static void ToggleBit(byte octet, int index) { octet ^= (byte)(1 << index); }

        /// <summary>
        /// Combines two integers into a single byte using the binary | operator.
        /// </summary>
        public static byte Or(int lsb, int msb) { return (byte)(lsb | msb); }

        /// <summary>
        /// Combines two integers into a single byte using binary & operator
        /// </summary>
        /// <param name="lowQuarterByte"></param>
        /// <param name="highQuarterByte"></param>
        /// <returns></returns>
        public static byte And(int lsb, int msb) { return (byte)(lsb & msb); }

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
            //Negitive values yei
            if(sizeInBytes == 0) throw new ArgumentException("sizeInBytes","Must be at least 1.");

            int count = octets.Count();

            if (offset > count) throw new ArgumentOutOfRangeException("offset", "Cannot be greater than the amount of elements contained in the given sequence.");

            var integerOctets = octets.Skip(offset).Take(sizeInBytes);

            if (sizeInBytes > count) throw new ArgumentOutOfRangeException("sizeInBytes", "Cannot be greater than the amount of elements contained in the given sequence");
            
            if (sizeInBytes > 8) throw new NotSupportedException("Only sizes up to 8 octets are supported.");

            //Reverse the seqeuence if indicated.
            if (reverse) integerOctets = integerOctets.Reverse();

            //Get the first octet, the placeHolder is 0
            ulong result = integerOctets.First();

            //If there is more than 1 octet in the integer representation
            if (sizeInBytes > 1)
            {
                //Use an unchecked block to calculate the result which is faster than keeping track of the sign.
                unchecked
                {
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
                        placeHolder <<= 8; // placeHolder *= 4;
                    }
                }
            }

            return (long)result;
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
        public static ushort ReadU16(IEnumerable<byte> buffer, int index, bool reverse)
        {
            return (ushort)Binary.ReadInteger(buffer, index, 2, reverse);
        }

        /// <summary>
        /// Reads a 24 bit unsigned integer from the given buffer at the given index.
        /// </summary>
        /// <param name="buffer">The buffer to Read the unsigned 24 bit value from</param>
        /// <param name="index">The index in the buffer to Read the value from</param>
        /// <param name="reverse">A value which indicates if the value should be reversed</param>
        /// <returns>The unsigned 24 bit value in the form of a 32 bit unsigned integer</returns>
        public static uint ReadU24(IEnumerable<byte> buffer, int index, bool reverse)
        {
            return (uint)Binary.ReadInteger(buffer, index, 3, reverse);
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
        public static uint ReadU32(IEnumerable<byte> buffer, int index, bool reverse)
        {
            return (uint)Binary.ReadInteger(buffer, index, 4, reverse);
        }

        public static ulong ReadU64(IEnumerable<byte> buffer, int index, bool reverse)
        {
            return (ulong)Binary.ReadInteger(buffer, index, 8, reverse);
        }

        #endregion

        //Could Write and IEnumerable with reflection or unsafe code.

        #region Writing (Provided to reduce unsafe transition when using BitConverter)

        public static void WriteU8(byte[] buffer, int index, bool reverse, byte value)
        {
            buffer[index] = reverse ? ReverseU8(value) : value;
        }

        /// <summary>
        /// Writes a the given unsgined 16 bit value to the buffer at the given index.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="index"></param>
        /// <param name="reverse"></param>
        /// <param name="value"></param>
        public static void Write16(byte[] buffer, int index, bool reverse, ushort value)
        {
            //If writing in reverse
            if (reverse)
            {
                //Start at the highest index
                buffer[index + 1] = (byte)(value & 0xff);
                buffer[index] = (byte)(value >> 8 & 0xff);
            }
            else
            {
                //Start at the lowest index
                buffer[index] = (byte)(value & 0xff);
                buffer[index + 1] = (byte)(value >> 8 & 0xff);
            }
        }

        public static void Write24(byte[] buffer, int index, bool reverse, uint value)
        {
            //If writing in reverse
            if (reverse)
            {
                //Start at the highest index
                buffer[index + 2] = (byte)(value & 0xff);
                buffer[index + 1] = (byte)(value >> 8 & 0xff);
                buffer[index] = (byte)(value >> 16 & 0xff);
            }
            else
            {
                //Start at the lowest index
                buffer[index] = (byte)(value & 0xff);
                buffer[index + 1] = (byte)(value >> 8 & 0xff);
                buffer[index + 2] = (byte)(value >> 16 & 0xff);
            }
        }


        public static void Write32(byte[] buffer, int index, bool reverse, uint value)
        {
            //If writing in reverse
            if (reverse)
            {
                //Start at the highest index
                buffer[index + 3] = (byte)(value & 0xff);
                buffer[index + 2] = (byte)(value >> 8 & 0xff);
                buffer[index + 1] = (byte)(value >> 16 & 0xff);
                buffer[index] = (byte)(value >> 24 & 0xff);
            }
            else
            {
                //Start at the lowest index
                buffer[index] = (byte)(value & 0xff);
                buffer[index + 1] = (byte)(value >> 8 & 0xff);
                buffer[index + 2] = (byte)(value >> 16 & 0xff);
                buffer[index + 3] = (byte)(value >> 24 & 0xff);
            }
        }

        /// <summary>
        /// Writes a Big Endian 64 bit value to the given buffer at the given index
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="index"></param>
        /// <param name="reverse">A value indicating if the given value should be written in reverse</param>
        /// <param name="value"></param>
        public static void Write64(byte[] buffer, int index, bool reverse, ulong value)
        {
            if (reverse)
            {
                //Write the highest 32 bits first
                Write32(buffer, index + 4, reverse, (uint)(value >> 32));
                Write32(buffer, index, reverse, (uint)value);

            }
            else
            {
                //Otherwise write the lower 32 bits first
                Write32(buffer, index, reverse, (uint)value);
                Write32(buffer, index + 4, reverse, (uint)(value >> 32));
            }
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
            //If no reversal is required return the value
            if (source == byte.MaxValue) return source;
            
            //If the value is greater then or equal to 127 the reverse is obtained by 127 - source
            if (source >= sbyte.MaxValue) return (byte)(sbyte.MaxValue - source);

            //If the value is less the reverse if obtained with 127 + source
            return (byte)(sbyte.MaxValue + source);
        }

        /// <summary>
        /// Reverses the given unsigned 16 bit value via left and right shift and casting to a unsigned 64 bit value.
        /// </summary>
        /// <param name="source">The unsigned 16 bit value which is required to be reversed</param>
        /// <returns>The reversed unsigned 16 bit value</returns>
        public static ushort ReverseU16(ushort source)
        {
            return (ushort)RollU64((ulong)source, 16);
        }

        /// <summary>
        /// Reverses the given unsigned 32 value via left and right shift and casting to a unsigned 64 bit value.
        /// </summary>
        /// <param name="source">The unsigned 32 bit value which is requried to be reversed</param>
        /// <returns>The reversed unsigned 32 bit value</returns>            
        public static uint ReverseU32(uint source)
        {
            return (uint)RollU64((ulong)source, 32);
        }

        /// <summary>
        /// Reverses the given unsigned 64 bit value via left and right shift and on the register(s) in use to perform this operation.
        /// </summary>
        /// <param name="source">The unsgined 64 bit value to reverse</param>
        /// <param name="amount">The amount of shifting left and right to perform</param>
        /// <returns>The reversed unsigned 64 bit value</returns>
        /// <remarks>
        /// On 32 bit Architectures two registers are beging used to perform this operation
        /// </remarks>
        public static ulong RollU64(ulong source, int amount)
        {
            return source >> amount | source << amount;
        }

        #endregion

        #endregion
    }

    #endregion
}
