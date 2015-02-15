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

        #region Nested Types

        /// <summary>
        /// Adopted from the example at <see href="http://en.wikipedia.org/wiki/Endianness">Wikipedia - Endianness</see>
        /// </summary>
        public enum Endian
        {
            Unknown = 0,
            //System = 1,
            Big = 0x0A0B0C0D,
            Little = 0x0D0C0B0A,
            Middle = 0x0B0A0D0C
            //
        }

        #endregion

        #region Todo

        //Binary Representation (Detection)

        //Min, Max

        #endregion

        #region Statics

        [CLSCompliant(false)]
        public static bool IsPowerOfTwo(ref int x) { return 0 == (x & (x - 1)); }

        public static bool IsPowerOfTwo(int x) { return IsPowerOfTwo(ref x); }

        [CLSCompliant(false)]
        public static bool IsEven(ref int x) { return 0 == (x & 1); }

        public static bool IsEven(int x) { return IsEven(ref x); }

        [CLSCompliant(false)]
        public static bool IsOdd(ref int x) { return 1 == (x & 1); }

        public static bool IsOdd(int x) { return IsOdd(ref x); }

        /// <summary>
        /// Corresponds to the value 0 with the sign bit set (-0).
        /// </summary>
        public static readonly long NegativeZeroBits = BitConverter.DoubleToInt64Bits(-0.0);

        /// <summary>
        /// Defines a mask which can be used to obtain the sign bit.
        /// </summary>
        public static readonly long SignMask = NegativeZeroBits ^ BitConverter.DoubleToInt64Bits(+0.0);

        /// <summary>
        /// Determines if the given value is negitive
        /// </summary>
        /// <param name="arg"></param>
        /// <returns>True if the value is negative, otherwise false</returns>
        [CLSCompliant(false)]
        public static bool IsNegative(ref double arg) { return (BitConverter.DoubleToInt64Bits(arg) & SignMask) == SignMask; }

        public static bool IsNegative(double arg) { return IsNegative(ref arg); }

        /// <summary>
        /// An equivalent to C's `signbit`
        /// </summary>
        /// <param name="d">The reference to the value</param>
        /// <returns>Hopefully true if arg is negative, false otherwise</returns>
        public unsafe static bool signbit(ref double d)
        {
            fixed (double* pRef = &d)
            {
                ushort* pd = (ushort*)pRef;
                return (pd[3] & 0x8000) != 0;
            }
        }

        /// <summary>
        /// The logical 0 based index of what this library reguards as the most significant bit of an octet according to system architecture.
        /// </summary>
        public static readonly int MostSignificantBit = BitConverter.IsLittleEndian ? 7 : -1;

        /// <summary>
        /// The logical 0 based index of what this library reguards as the least significant bit of an octet according to system architecture.
        /// </summary>
        public static readonly int LeastSignificantBit = BitConverter.IsLittleEndian ? 0 : -1;

        /// <summary>
        /// Indicates the byte order in which the data is stored in this computer architecture
        /// <see cref="System.BitConverter.IsLittleEndian"/>
        /// </summary>
        public static readonly bool IsLittleEndian = System.BitConverter.IsLittleEndian;

        internal static readonly byte[] BitsSetTable;

        internal static readonly byte[] BitsReverseTable;

        public static readonly Endian SystemEndian = Endian.Unknown;

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        static Binary()
        {
            //ensure not already called.
            if (SystemEndian != Endian.Unknown || BitsSetTable != null || BitsReverseTable != null) return;

            //Get the bytes of the little endian value
            //Then read them back as an integer
            int systemReadLittleEndian = BitConverter.ToInt32(BitConverter.GetBytes((int)Endian.Little), 0);

            //Assign the proerty SystemEndian as a result of the cast to Endian
            SystemEndian = ((Endian)systemReadLittleEndian);

            //Assign the system endian from the value read and then verify
            switch (SystemEndian)
            {
                case Endian.Little:
                    {
                        //If not little endian double check
                        if (false == IsLittleEndian) goto default;

                        MostSignificantBit = 7;

                        LeastSignificantBit = 0;

                        break;
                    }
                case Endian.Big:
                    {
                        //If little endian double check
                        if (true == IsLittleEndian) goto default;

                        MostSignificantBit = 0;

                        LeastSignificantBit = 7;
                        break;
                    }
                case Endian.Middle:
                    {
                        //If little endian double check
                        if (true == IsLittleEndian) goto default;

                        //Determine

                        break;
                    }
                default:
                    {
                        //Check endian again
                        if (IsLittleEndian = BitConverter.IsLittleEndian)
                        {
                            SystemEndian = Endian.Little;

                            break;
                        }

                        //Not sure
                        SystemEndian = Endian.Unknown;

                        throw new NotSupportedException("SystemEndian Unknown is not supported.");
                    }
            }

            //Build Tables.

            BitsSetTable = new byte[Binary.TrīgintāDuoBitSize];

            BitsReverseTable = new byte[Binary.TrīgintāDuoBitSize];

            //Start at 2, since BitsSetTable[0] = BitsReverseTable[0] = 0 
            BitsSetTable[Binary.Unum] = BitsReverseTable[Binary.SedecimBitSize] = Binary.Unum;
            
            BitsSetTable[Binary.SedecimBitSize] = BitsReverseTable[Binary.Unum] = Binary.SedecimBitSize;
            
            BitsSetTable[byte.MaxValue] = BitSize; 

            BitsReverseTable[byte.MaxValue] = byte.MaxValue;
            
            //253 Operations [2 -> 254]
            for (int i = Binary.Duo; i < byte.MaxValue; ++i)
            {
                byte reverse = MultiplyReverseU8((byte)i);

                BitsReverseTable[i] = reverse;

                BitsSetTable[reverse] = BitsSetTable[i] = (byte)((i & Binary.Unum) + BitsSetTable[i / Binary.Duo]);
            }
        }

        #endregion

        #region Maximum Values

        /// <summary>
        /// The amount of bits it a single octet
        /// </summary>
        public const byte BitSize = 8;

        /// <summary>
        /// (000000)11 in Binary
        /// </summary>
        public const byte TwoBitMaxValue = 3;

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

        /// <summary>
        /// Needs no summary.
        /// </summary>
        internal const uint TheAnswerToEverything = 42;

        /// <summary>
        /// Confidimus
        /// </summary>
        internal const int Unum = 1;

        //studet?
        internal const int Duo = 2;

        #endregion

        #region Sizes

        //16
        //scilicet duo
        public const byte DoubleBitSize = BitSize * 2;

        //24
        public const byte TripleBitSize = BitSize * 3;

        //32
        //scilicet duo
        public const byte QuadrupleBitSize = BitSize * 4;

        //64
        public const byte OctupleBitSize = BitSize * BitSize;

        //128
        public const byte SedecimBitSize = BitSize * DoubleBitSize;

        //256
        //Vele multiplica ea sicut predictum est scilicet duo 
        public const int TrīgintāDuoBitSize = BitSize * QuadrupleBitSize;
        //implete eam , ut praedictum est , scilicet duo centum et octo

        #endregion

        #region Bit Methods

        /// <summary>
        /// Determines the amount of bits set in the byte
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        [CLSCompliant(false)]
        public static int BitsSet(ref byte b) { return BitsSetTable[b]; }

        public static int BitsSet(byte b) { return BitsSet(ref b); }

        /// <summary>
        /// Determines the amount of bits not set in the byte
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        [CLSCompliant(false)]
        public static int BitsUnSet(ref byte b) { return BitSize - BitsSetTable[b]; }

        public static int BitsUnSet(byte b) { return BitsUnSet(ref b); }

        [CLSCompliant(false)]
        public static int BitsSet(ref int i) { return BitConverter.GetBytes(i).Sum(b => BitsSet(b)); }

        public static int BitsSet(int i) { return BitsSet(ref i); }

        [CLSCompliant(false)]
        public static int BitsUnSet(ref int i) { return QuadrupleBitSize - BitConverter.GetBytes(i).Sum(b => BitsSet(b)); }

        public static int BitsUnSet(int i) { return BitsUnSet(ref i); }

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

            switch (octet)
            {
                case byte.MinValue: return false;
                case byte.MaxValue: return true;
                default: return unchecked((octet & (1 << index)) != 0);
            }
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

            //Read the bit
            bool oldValue = GetBit(ref octet, index);

            //If the newValue has been set already return
            if (oldValue == newValue) return oldValue;

            //Set or clear the bit according to newValue
            octet = (byte)(newValue ? (octet | (1 << index)) : (octet & ~(1 << index)));

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

            octet |= (byte)(1 << index);
        }

        /// <summary>
        /// Sets the index of the given bit to 0 if not already set
        /// </summary>
        /// <param name="octet"></param>
        /// <param name="index"></param>
        public static void ClearBit(ref byte octet, int index)
        {
            if (index < 0 || index > BitSize) throw new ArgumentOutOfRangeException("index", "Must be a value 0 - 8");

            octet &= (byte)(~(1 << index));
        }

        /// <summary>
        /// Provides a method of setting a bit with XOR
        /// </summary>
        /// <param name="octet">The octet to set the bit in</param>
        /// <param name="index">The index of the bit to set</param>
        /// <remarks>
        /// http://stackoverflow.com/questions/2605913/invert-1-bit-in-c-sharp
        /// </remarks>
        public static void ToggleBit(ref byte octet, int index)
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

        public static long ReadBits(byte[] data, bool reverse = false)
        {
            if (data == null) throw new ArgumentNullException("data");

            int byteOffset = 0, bitOffset = 0;

            return reverse ? ReadBitsReverse(data, ref byteOffset, ref bitOffset, BitSize * data.Length) : ReadBits(data, ref byteOffset, ref bitOffset, BitSize * data.Length);
        }

        public static long ReadBits(byte[] data, int byteOffset, bool reverse = false)
        {
            if (data == null) throw new ArgumentNullException("data");

            int bitOffset = 0;

            return reverse ? ReadBitsReverse(data, ref byteOffset, ref bitOffset, BitSize * data.Length) : ReadBits(data, ref byteOffset, ref bitOffset, BitSize * data.Length);
        }

        public static long ReadBits(byte[] data, int byteOffset, int count, bool reverse = false)
        {
            if (data == null) throw new ArgumentNullException("data");

            int bitOffset = 0;

            return reverse ? ReadBitsReverse(data, ref byteOffset, ref bitOffset, count) : ReadBits(data, ref byteOffset, ref bitOffset, count);
        }


        public static long ReadBits(byte[] data, ref int byteOffset, ref int bitOffset, int count, bool reverse = false)
        {
            if (data == null) throw new ArgumentNullException("data");

            return reverse ? ReadBitsReverse(data, ref byteOffset, ref bitOffset, count) : ReadBits(data, ref byteOffset, ref bitOffset, count);
        }

        public static long ReadBits(byte[] data, ref int byteOffset, ref int bitOffset, int count)
        {
            unchecked
            {
                //The value and the placeHolder
                long value = 0, placeHolder = 1;

                //While there is a byte needed decrement for the bit consumed
                while (count-- > 0)
                {
                    //Check for the end of bits
                    if (bitOffset >= BitSize)
                    {
                        //reset
                        bitOffset = 0;

                        //move the index of the byte
                        ++byteOffset;

                        //Create the next value
                        placeHolder <<= BitSize;
                    }

                    //Get a bit from the byte at our offset to determine if the value needs to be incremented
                    if (GetBit(ref data[byteOffset], bitOffset))
                    {
                        value |= (1 << bitOffset) * placeHolder;
                        //value += 1 << bitOffset;
                    }

                    //Increment for the bit consumed
                    ++bitOffset;
                }

                return value;
            }
        }

        public static long ReadBitsReverse(byte[] data, ref int byteOffset, ref int bitOffset, int count)
        {
            unchecked
            {
                //The value and the placeHolder
                long value = 0, placeHolder = 1;

                //While there is a byte needed decrement for the bit consumed
                while (count-- > 0)
                {
                    //Check for the end of bits
                    if (bitOffset >= BitSize)
                    {
                        //reset
                        bitOffset = 0;

                        //move the index of the byte
                        ++byteOffset;

                        //Create the next value
                        placeHolder <<= BitSize;
                    }

                    //Get a bit from the byte at our offset to determine if the value needs to be incremented
                    if (GetBit(ref data[byteOffset], bitOffset))
                    {
                        //value |= (1 << bitOffset) * placeHolder;
                        //value += 1 << bitOffset;

                        //value |= ReverseU8((byte)(1 << bitOffset)) * placeHolder;

                        value += ReverseU8((byte)(1 << bitOffset));
                    }

                    //Increment for the bit consumed
                    ++bitOffset;
                }

                return value;
            }
        }

        //Write Bits

        public static byte[] SetBit(byte[] self, int index, bool value)
        {
            int bitIndex, byteIndex = Math.DivRem(index, BitSize, out bitIndex);

            SetBit(ref self[byteIndex], index, value);

            return self;
        }

        public static byte[] ToggleBit(byte[] self, int index)
        {
            int bitIndex, byteIndex = Math.DivRem(index, BitSize, out bitIndex);

            ToggleBit(ref self[byteIndex], index);

            return self;
        }

        public static bool GetBit(byte[] self, int index, bool value)
        {
            int bitIndex, byteIndex = Math.DivRem(index, BitSize, out bitIndex);

            return GetBit(ref self[byteIndex], index);
        }

        #endregion

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

                if (sizeInBytes > BitSize) throw new NotSupportedException("Only sizes up to 8 octets are supported.");

                var integerOctets = octets.Skip(offset).Take(sizeInBytes);

                //One byte only
                if (sizeInBytes == 1) return reverse ? ReverseU8(integerOctets.First()) : integerOctets.First();

                //Reverse the seqeuence if indicated.
                if (reverse) integerOctets = integerOctets.Reverse();

                //Get the first octet, the placeHolder is effectively 0
                ulong result = integerOctets.First();

                //Calulcate the placeHolder value which is equal to the largest size storable in the signed representation plus 1
                ulong placeHolder = 256;// (byte.MaxValue + 1); // base 2 = 2 * (ulong)(-sbyte.MaxValue) = 256;

                //Iterate each byte in the sequence skipping the first octet.
                foreach (byte b in integerOctets.Skip(1))
                {
                    //If the byte is greater than 0
                    if (b > byte.MinValue)
                    {
                        //Combine the result of the calculation of the base two value with the binary representation.
                        result |= b * placeHolder;
                    }

                    //Skip the 0 check using the adder
                    //result += b * placeHolder;

                    //Move the placeholder 8 bits left (This equates to a multiply by 4, [where << 4 would be a multiply of 3 etc])

                    //Should probably not check endian here because reverse was already given...

                    //Additionally there may be another endian such as middle endian in use.

                    placeHolder <<= BitSize;
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

        #region GetBytes

        public static byte[] GetBytes(short i, bool reverse)
        {
            byte[] result = new byte[2];
            Write16(result, 0, reverse, i);
            return result;
        }

        public static byte[] GetBytes(int i, bool reverse)
        {
            byte[] result = new byte[4];
            Write32(result, 0, reverse, i);
            return result;
        }

        public static byte[] GetBytes(long i, bool reverse)
        {
            byte[] result = new byte[8];
            Write64(result, 0, reverse, i);
            return result;
        }

        #endregion

        #region Writing

        //Todo Expand upon reading and writing a number in different lengths than normally found.

        [CLSCompliant(false)]
        public static void WriteInteger(byte[] buffer, int index, int count, ulong value, bool reverse)
        {
            if (reverse) WriteReversedInteger(buffer, index, count, value);
            else WriteInteger(buffer, index, count, (ulong)value);
        }

        public static void WriteInteger(byte[] buffer, int index, int count, long value, bool reverse)
        {
            WriteInteger(buffer, index, count, (ulong)value, reverse);
        }

        [CLSCompliant(false)]
        public static void WriteInteger(byte[] buffer, int index, int count, ulong value)
        {
            if (buffer == null || count == 0) return;

            unchecked
            {
                //While something remains
                while (count-- > 0)
                {
                    //Write the byte at the index
                    buffer[index++] = (byte)(value & byte.MaxValue);

                    //Remove the bits we used
                    value >>= BitSize;
                }
            }
        }

        public static void WriteReversedInteger(byte[] buffer, int index, int count, long value)
        {
            WriteReversedInteger(buffer, index, count, value);
        }

        [CLSCompliant(false)]
        public static void WriteReversedInteger(byte[] buffer, int index, int count, ulong value)
        {
            if (buffer == null || count == 0) return;

            unchecked
            {
                //While something remains
                while(count > 0)
                {
                    //Write the byte at the reversed index
                    buffer[index + --count] = (byte)(value & byte.MaxValue);

                    //Remove the bits we used
                    value >>= BitSize;
                }
            }
        }

        public static void WriteU8(byte[] buffer, int index, bool reverse, byte value)
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
        public static void Write16(byte[] buffer, int index, bool reverse, short value)
        {
            WriteInteger(buffer, index, 2, value, reverse);
        }

        [CLSCompliant(false)]
        public static void Write16(byte[] buffer, int index, bool reverse, ushort value)
        {
            WriteInteger(buffer, index, 2, (short)value, reverse);
        }

        [CLSCompliant(false)]
        public static void Write24(byte[] buffer, int index, bool reverse, uint value)
        {
            WriteInteger(buffer, index, 3, (int)value, reverse);
        }

        public static void Write24(byte[] buffer, int index, bool reverse, int value)
        {
            WriteInteger(buffer, index, 3, (int)value, reverse);
        }

        [CLSCompliant(false)]
        public static void Write32(byte[] buffer, int index, bool reverse, uint value)
        {
            WriteInteger(buffer, index, 4, (int)value, reverse);
        }

        //Todo
        public static void Write32(byte[] buffer, int index, bool reverse, int value)
        {
            WriteInteger(buffer, index, 4, (int)value, reverse);
        }

        /// <summary>
        /// Writes a Big Endian 64 bit value to the given buffer at the given index
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="index"></param>
        /// <param name="reverse">A value indicating if the given value should be written in reverse</param>
        /// <param name="value"></param>
        [CLSCompliant(false)]
        public static void Write64(byte[] buffer, int index, bool reverse, ulong value)
        {
            WriteInteger(buffer, index, 8, value, reverse);
        }

        //Todo
        public static void Write64(byte[] buffer, int index, bool reverse, long value)
        {
            WriteInteger(buffer, index, 8, value, reverse);
        }


        #endregion

        #region Reversal

        /// <summary>
        /// Reverses the given unsigned 8 bit value via table lookup of the reverse value.
        /// </summary>
        /// <param name="source">The unsigned 8 bit value which is requried to be reversed</param>
        /// <returns>The reversed unsigned 8 bit value</returns>
        [CLSCompliant(false)]
        public static byte ReverseU8(ref byte source)
        {
            //http://graphics.stanford.edu/~seander/bithacks.html
            return BitsReverseTable[source];
        }

        public static byte ReverseU8(byte source) { return ReverseU8(ref source); }

        /// <summary>
        /// Reverses the given unsigned 8 bit value via calculation of the reverse value.
        /// </summary>
        [CLSCompliant(false)]
        public static byte MultiplyReverseU8(ref byte source)
        {
            //http://graphics.stanford.edu/~seander/bithacks.html
            return (byte)(((source * 0x80200802UL) & 0x0884422110UL) * 0x0101010101UL >> QuadrupleBitSize);
        }

        public static byte MultiplyReverseU8(byte source) { return MultiplyReverseU8(ref source); }

        [CLSCompliant(false)]
        public static sbyte Reverse8(sbyte source)
        {
            return (sbyte)ReverseU8((byte)source);
        }

        [CLSCompliant(false)]
        public static ushort ReverseUnsignedShort(ref ushort source)
        {
            return (ushort)(((source & 0xFF) << 8) | ((source >> 8) & 0xFF));
        }

        [CLSCompliant(false)]
        public static uint ReverseUnsignedInt(ref uint source)
        {
            return (uint)((((source & 0x000000FF) << 24) | ((source & 0x0000FF00) << 8) | ((source & 0x00FF0000) >> 8) | ((source & 0xFF000000) >> 24)));
        }


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

        public static long Reverse64(long source) { return Reverse64(ref source); }

        [CLSCompliant(false)]
        public static long Reverse64(ref long source)
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

    }

    #endregion
}
