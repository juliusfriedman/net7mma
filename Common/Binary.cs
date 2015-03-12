﻿#region Copyright
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
        #region Constants

        /// <summary>
        /// 0
        /// </summary>
        internal const int Nihil = 0;

        /// <summary>
        /// 1
        /// </summary>
        internal const int Ūnus = 1;

        /// <summary>
        /// 2
        /// </summary>
        internal const int Duo = 2;

        /// <summary>
        /// 3
        /// </summary>
        internal const int Tres = 3;

        /// <summary>
        /// 4
        /// </summary>
        internal const int Quattuor = 4;

        /// <summary>
        /// 5
        /// </summary>
        internal const int Quinque = 5;

        /// <summary>
        /// 6
        /// </summary>
        internal const int Sex = 6;

        /// <summary>
        /// 7
        /// </summary>
        internal const int Septem = 7;

        /// <summary>
        /// 8
        /// </summary>
        internal const int Octo = 8;

        /// <summary>
        /// 9
        /// </summary>
        internal const int Novem = 9;

        /// <summary>
        /// 10
        /// </summary>
        internal const int Decem = 10;

        /// <summary>
        /// 15
        /// </summary>
        internal const int Quīndecim = 15;

        /// <summary>
        /// 16
        /// </summary>
        internal const int Sēdecim = 16;

        /// <summary>
        /// 31
        /// </summary>
        internal const int TrīgintāŪnus = 31;

        /// <summary>
        /// 42
        /// </summary>
        internal const uint QuadrāgintāDuo = 42;

        #endregion

        #region Exceptions

        /// <summary>
        /// An exception utilized when a value larger than allowed is utilized in a quarter bit.
        /// </summary>
        public static OverflowException QuarterBitOverflow = new OverflowException("Quarter bits cannot store values larger than 3.");

        /// <summary>
        /// An exception utilized when a value larger than allowed is utilized in a nibble.
        /// </summary>
        public static OverflowException NybbleOverflow = new OverflowException("Cannot store a number higher than 15 in a nybble.");

        /// <summary>
        /// An exception utilized when a value larger than allowed is utilized in a 5 bit field.
        /// </summary>
        public static OverflowException FiveBitOverflow = new OverflowException("Cannot store a number higher than 31 in 5 bits.");

        /// <summary>
        /// An exception utilized when a value larger than allowed is utilized in a 7 bit field (sbyte.MaxValue is not utilized because of the dependence on the value of sign bit)
        /// </summary>
        public static OverflowException SevenBitOverflow = new OverflowException("Cannot store a number higher than 127 in a 7 bit structure.");

        /// <summary>
        /// An exception utilized when a value larger than allowed is utilized in a 16 bit field.
        /// </summary>
        public static OverflowException SixteenBitOverflow = new OverflowException("Cannot store a number higher than 65535 in a 16 bit structue.");

        /// <summary>
        /// A string which contains has a format in which all Overflow exceptions Read by the ReadBinaryOverflowException function utilize.
        /// </summary>
        static string OverFlowExceptionFormat = "{0} overflowed. Cannot store a number lower than {1} or higher than {2} in a {3} structure.";

        public static OverflowException CreateOverflowException<T>(string parameter, T value, string minValue, string maxValue)
        {
            return new OverflowException(string.Format(OverFlowExceptionFormat, parameter, value.ToString(), minValue, maxValue));
        }
        #endregion

        #region Nested Types

        /// <summary>
        /// Defines a known ByteOrder
        /// </summary>
        /// <notes>
        /// Adopted from the example at <see href="http://en.wikipedia.org/wiki/Endianness">Wikipedia - Endianness</see>
        /// </notes>
        public enum ByteOrder
        {
            Unknown = Binary.Nihil,
            //System = 1,
            Big = 0x0A0B0C0D,
            Little = 0x0D0C0B0A,
            MiddleBig = 0x0B0A0D0C,
            MiddleLittle = 0x0C0D0A0B,
            Mixed = Big | Little | MiddleBig | MiddleLittle,
            Any = Mixed,
            All = int.MaxValue
        }

        /// <summary>
        /// Defines a known BitOrder <see href="http://en.wikipedia.org/wiki/Bit_numbering">Wikipedia - Bit numbering</see>
        /// </summary>
        public enum BitOrder
        {
            Unknown = Binary.Nihil,
            LeastSignificant = 0x80,
            MostSignificant = 0x01,
            Any = LeastSignificant | MostSignificant,
            All = int.MaxValue
        }

        /// <summary>
        /// Defines a known <see href="http://en.wikipedia.org/wiki/Signed_number_representations">Signed number representation</see>
        /// </summary>
        public enum BinaryRepresentation
        {
            Unknown = 0,
            NoSign = 1, 
            OnesComplement = 2, 
            SignedMagnitude = 4, 
            TwosComplement = 6,
            Excess = 8,
            Base = 16,
            Biased = 32,
            ZigZag = 64,
            Any = NoSign | OnesComplement | SignedMagnitude | TwosComplement | Excess | Base | Biased | ZigZag,
            All = int.MaxValue
        }

        #endregion

        #region Todo

        //Rounding

        //ParseInt (base)

        #endregion

        #region Statics

        #region Min, Max

        /// <summary>
        /// Returns the minimum value which is bounded inclusively by min and max.
        /// </summary>
        /// <param name="min">The minimum value</param>
        /// <param name="max">The maximum value</param>
        /// <returns>The value.</returns>
        public static byte Min(ref byte min, ref byte max)
        {
            //Keep the difference
            int difference = min - max;

            // make a mask that is all ones if x < y, or all zeroes if x >= y
            int mask = difference >> Binary.TrīgintāŪnus;

            // select x if x < y, or y if x >= y
            return (byte)((mask & min) | (~mask & max));

            // alternative: use arithmetic to select the minimum
            //return (byte)(max + (difference & mask));

        }

        [CLSCompliant(false)]
        public static byte Min(byte min, byte max)
        {
            return Min(ref min, ref max);
        }

        public static int Min(ref int min, ref int max)
        {
            //Keep the difference
            int difference = min - max;

            // make a mask that is all ones if x < y, or all zeroes if x >= y
            int mask = difference >> Binary.TrīgintāŪnus;

            // select x if x < y, or y if x >= y
            return ((mask & min) | (~mask & max));

            // alternative: use arithmetic to select the minimum
            //return (int)(max + (difference & mask));

        }

        [CLSCompliant(false)]
        public static int Min(int min, int max)
        {
            return Min(ref min, ref max);
        }

        public static long Min(ref long min, ref long max)
        {
            //Keep the difference
            long difference = min - max;

            // make a mask that is all ones if x < y, or all zeroes if x >= y
            long mask = difference >> Binary.TrīgintāŪnus;

            // select x if x < y, or y if x >= y
            return ((mask & min) | (~mask & max));

            // alternative: use arithmetic to select the minimum
            //return (int)(max + (difference & mask));

        }

        [CLSCompliant(false)]
        public static long Min(long min, long max)
        {
            return Min(ref min, ref max);
        }

        /// <summary>
        /// Returns the maximum value which is bounded inclusively by min and max.
        /// </summary>
        /// <param name="min">The minimum value</param>
        /// <param name="max">The maximum value</param>
        /// <returns>The value</returns>
        public static byte Max(ref byte min, ref byte max)
        {
            //Keep the difference
            int difference = min - max;

            // make a mask that is all ones if x < y, or all zeroes if x >= y
            int mask = difference >> Binary.TrīgintāŪnus;

            // select x if x < y, or y if x >= y
            return (byte)((mask & max) | (~mask & min));

            // alternative: use arithmetic to select the minimum
            //return (byte)(max + (difference & mask));
        }

        [CLSCompliant(false)]
        public static byte Max(byte min, byte max)
        {
            return Max(ref min, ref max);
        }

        public static int Max(ref int min, ref int max)
        {
            //Keep the difference
            int difference = min - max;

            // make a mask that is all ones if x < y, or all zeroes if x >= y
            int mask = difference >> Binary.TrīgintāŪnus;

            // select x if x < y, or y if x >= y
            return ((mask & max) | (~mask & min));

            // alternative: use arithmetic to select the minimum
            //return (int)(max + (difference & mask));

        }

        [CLSCompliant(false)]
        public static int Max(int min, int max)
        {
            return Max(ref min, ref max);
        }

        public static long Max(ref long min, ref long max)
        {
            //Keep the difference
            long difference = min - max;

            // make a mask that is all ones if x < y, or all zeroes if x >= y
            long mask = difference >> Binary.TrīgintāŪnus;

            // select x if x < y, or y if x >= y
            return ((mask & max) | (~mask & min));

            // alternative: use arithmetic to select the minimum
            //return (int)(max + (difference & mask));

        }

        [CLSCompliant(false)]
        public static long Max(long min, long max)
        {
            return Max(ref min, ref max);
        }

        #endregion

        #region Clamp

        public static byte Clamp(ref byte value, ref byte min, ref byte max)
        {
            return Binary.Min(Binary.Max(min, value), max);
        }

        [CLSCompliant(false)]
        public static byte Clamp(byte value, byte min, byte max)
        {
            return Clamp(ref value, ref min, ref max);
        }

        public static int Clamp(ref int value, ref int min, ref int max)
        {
            return Binary.Min(Binary.Max(ref min, ref value), max);
        }

        [CLSCompliant(false)]
        public static int Clamp(int value, int min, int max)
        {
            return Clamp(ref value, ref min, ref max);
        }

        public static long Clamp(ref long value, ref long min, ref long max)
        {
            return Binary.Min(Binary.Max(ref min, ref value), max);
        }

        [CLSCompliant(false)]
        public static long Clamp(long value, long min, long max)
        {
            return Clamp(ref value, ref min, ref max);
        }

        /// <summary>
        /// Provides a value which inclusively bound by the given parameters
        /// </summary>
        /// <param name="value">The value</param>
        /// <param name="min">The minimum value</param>
        /// <param name="max">The maximum value</param>
        /// <returns>The bound value</returns>
        public static double Clamp(ref double value, ref double min, ref double max)
        {
            return BitConverter.Int64BitsToDouble(Binary.Min(Binary.Max(BitConverter.DoubleToInt64Bits(min), BitConverter.DoubleToInt64Bits(value)), BitConverter.DoubleToInt64Bits(max)));
        }

        [CLSCompliant(false)]
        public static double Clamp(double value, double min, double max)
        {
            return Clamp(ref value, ref min, ref max);
        }

        #endregion

        #region IsPowerOfTwo

        [CLSCompliant(false)]
        public static bool IsPowerOfTwo(ref int x) { return Binary.Nihil == (x & (x - Binary.Ūnus)); }

        public static bool IsPowerOfTwo(int x) { return IsPowerOfTwo(ref x); }

        #endregion

        #region IsEven

        [CLSCompliant(false)]
        public static bool IsEven(ref int x) { return Binary.Nihil == (x & Binary.Ūnus); }

        public static bool IsEven(int x) { return IsEven(ref x); }

        #endregion

        #region IsOdd

        [CLSCompliant(false)]
        public static bool IsOdd(ref int x) { return Binary.Ūnus == (x & Binary.Ūnus); }

        public static bool IsOdd(int x) { return IsOdd(ref x); }

        /// <summary>
        /// Determines if the given value is negitive
        /// </summary>
        /// <param name="arg"></param>
        /// <returns>True if the value is negative, otherwise false</returns>
        [CLSCompliant(false)]
        public static bool IsNegative(ref int arg) { return (arg & SignMask) == SignMask; }

        public static bool IsNegative(int arg) { return IsNegative(ref arg); }

        /// <summary>
        /// Determines if the given value is negitive
        /// </summary>
        /// <param name="arg"></param>
        /// <returns>True if the value is negative, otherwise false</returns>
        [CLSCompliant(false)]
        public static bool IsNegative(ref long arg) { return (arg & SignMask) == SignMask; }

        public static bool IsNegative(long arg) { return IsNegative(ref arg); }

        /// <summary>
        /// Determines if the given value is positive
        /// </summary>
        /// <param name="arg"></param>
        /// <returns>True if the value is positive, otherwise false</returns>
        [CLSCompliant(false)]
        public static bool IsPositive(ref int arg) { return (BitConverter.DoubleToInt64Bits(arg) & SignMask) != SignMask; }

        public static bool IsPositive(int arg) { return IsPositive(ref arg); }

        /// <summary>
        /// Determines if the given value is positive
        /// </summary>
        /// <param name="arg"></param>
        /// <returns>True if the value is positive, otherwise false</returns>
        [CLSCompliant(false)]
        public static bool IsPositive(ref long arg) { return (BitConverter.DoubleToInt64Bits(arg) & SignMask) != SignMask; }

        public static bool IsPositive(long arg) { return IsPositive(ref arg); }

        #endregion

        #region BitsToBytes, BytesToBits

        public static int BitsToBytes(int bitCount, int bitsPerByte = Binary.BitsPerByte) { return (int)BitsToBytes((uint)bitCount, (uint) bitsPerByte); }

        [CLSCompliant(false)]
        public static uint BitsToBytes(uint bitCount, uint bitsPerByte = Binary.BitsPerByte) { return BitsToBytes(ref bitCount, bitsPerByte); }

        [CLSCompliant(false)]
        public static uint BitsToBytes(ref uint bitCount, uint bitsPerByte = Binary.BitsPerByte)
        {
            if(bitCount == Binary.Nihil) return Binary.Nihil;

            long bits, bytes = Math.DivRem(bitCount, bitsPerByte, out bits);

            return (uint)(bits > Binary.Nihil ? ++bytes : bytes);
        }

        public static int BytesToBits(int byteCount) { return (int)BytesToBits((uint)byteCount); }

        [CLSCompliant(false)]
        public static uint BytesToBits(uint byteCount) { return BytesToBits(ref byteCount); }

        /// <summary>
        /// Converts the given amount of bytes to the amount of bits needed to represent the same data using the specified options.
        /// </summary>
        /// <param name="byteCount"></param>
        /// <param name="bitSize"></param>
        /// <returns></returns>
        [CLSCompliant(false)]
        public static uint BytesToBits(ref uint byteCount, uint bitSize = Binary.BitsPerByte) { return byteCount * bitSize; }

        #endregion

        //Could be moved to a new class especially since if it's not needed it's wasting space
        //Same with almost everything except BitsToBytes and BytesToBits
        //E.g Binary.Representations...
        #region Binary Representations

        public static int OnesComplement(int value) { return OnesComplement(ref value); }

        [CLSCompliant(false)]
        public static int OnesComplement(ref int value) { return (~value); }

        public static int TwosComplement(int value) { return TwosComplement(ref value); }

        [CLSCompliant(false)]
        public static int TwosComplement(ref int value) { unchecked { return (~value + Binary.Ūnus); } }

        public static int SignedMagnitude(int value) { int sign; return SignedMagnitude(ref value, out sign); }

        [CLSCompliant(false)]
        public static int SignedMagnitude(ref int value) { int sign; return SignedMagnitude(ref value, out sign); }

        /// <summary>
        /// Converts value to twos complement and returns the signed magnitude representation outputs the sign
        /// </summary>
        /// <param name="value"></param>
        /// <param name="sign"></param>
        /// <returns></returns>
        [CLSCompliant(false)]
        public static int SignedMagnitude(ref int value, out int sign)
        {
            unchecked
            {

                //If the sign is -1 then convert to twos complement
                if ((sign = Math.Sign(value)) == -Binary.Ūnus) value = TwosComplement(ref value);

                //if (IsNegative(ref value))
                //{
                //    sign = -Binary.Ūnus;

                //    value = TwosComplement(ref value);
                //}
               
                //Return the value multiplied by sign
                return value * sign;
            }
        }

        /// <summary>
        /// Converts the given number in twos complement to signed magnitude representation
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static int TwosComplementToSignedMagnitude(ref int value)
        {
            unchecked
            {
                //Create a mask of the value holding the sign
                int sign = (value >> Binary.TrīgintāŪnus); //SignMask

                //Convert from TwosComplement to SignedMagnitude
                return (((value + sign) ^ sign) | (int)(value & SignMask));
            }
        }

        /// <summary>
        /// Converts the given number in signed magnitude representation to twos complement.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static long SignedMagnitudeToTwosComplement(ref int value)
        {
            unchecked
            {
                //Convert from SignedMagnitude to TwosComplement
                return ((~(value & int.MaxValue)) + Binary.Ūnus) | (int)(value & SignMask);
            }
        }

        /// <summary>
        /// Indicates if the architecture utilizes two's complement binary representation
        /// </summary>
        /// <returns>True if two's complement is used, otherwise false</returns>
        public static bool IsTwosComplement()
        {
            //return Convert.ToSByte(byte.MaxValue.ToString(Media.Common.Extensions.String.StringExtensions.HexadecimalFormat), Binary.Sēdecim) == -Binary.Ūnus;

            return unchecked((sbyte)byte.MaxValue == -Media.Common.Binary.Ūnus);
        }

        /// <summary>
        /// Indicates if the architecture utilizes one's complement binary representation
        /// </summary>
        /// <returns>True if ones's complement is used, otherwise false</returns>
        public static bool IsOnesComplement()
        {
            //return Convert.ToSByte(sbyte.MaxValue.ToString(Media.Common.Extensions.String.StringExtensions.HexadecimalFormat), Binary.Sēdecim) == -Binary.Ūnus;

            return unchecked(sbyte.MaxValue == -Media.Common.Binary.Ūnus);
        }

        /// <summary>
        /// Indicates if the architecture utilizes sign and magnitude representation
        /// </summary>
        /// <returns>True if sign and magnitude representation is used, otherwise false</returns>
        public static bool IsSignedMagnitude()
        {
            return unchecked(((Binary.Tres & -Binary.Ūnus) == Binary.Ūnus)); //&& false == IsTwosComplement

            //e.g. (3 & -1) == 3, where as Media.Common.Binary.BitwiseAnd(-3, 1) == 1
        }

        //http://en.wikipedia.org/wiki/Signed_number_representations
        //Excess, Base, Biased

        #endregion

        #region Double Masks and Conversions

        /// <summary>
        /// The value 0 represented as a <see cref="double"/>
        /// </summary>
        public const double DoubleZero = Binary.Nihil;

        /// <summary>
        /// Corresponds to the value 0 with the sign bit set (-0).
        /// </summary>
        public static readonly long NegativeZeroBits = BitConverter.DoubleToInt64Bits(-DoubleZero);

        /// <summary>
        /// Defines a mask which can be used to obtain the sign bit.
        /// </summary>
        public static readonly long SignMask = NegativeZeroBits ^ BitConverter.DoubleToInt64Bits(DoubleZero);

        /// <summary>
        /// The value of the sign bit, 1 or 0
        /// </summary>
        public static readonly bool SignBit = Convert.ToBoolean(SignMask);

        /// <summary>
        /// Determines if the given value is negitive
        /// </summary>
        /// <param name="arg"></param>
        /// <returns>True if the value is negative, otherwise false</returns>
        [CLSCompliant(false)]
        public static bool IsNegative(ref double arg) { return (BitConverter.DoubleToInt64Bits(arg) & SignMask) == SignMask; }

        public static bool IsNegative(double arg) { return IsNegative(ref arg); }

        /// <summary>
        /// Determines if the given value is positive
        /// </summary>
        /// <param name="arg"></param>
        /// <returns>True if the value is positive, otherwise false</returns>
        [CLSCompliant(false)]
        public static bool IsPositive(ref double arg) { return (BitConverter.DoubleToInt64Bits(arg) & SignMask) != SignMask; }

        public static bool IsPositive(double arg) { return IsPositive(ref arg); }

        #endregion

        #region Fields

        #region Bit Tables

        /// <summary>
        /// Assumes 8 bits per byte
        /// </summary>
        internal static readonly byte[] BitsSetTable;

        /// <summary>
        /// Assumes 8 bits per byte
        /// </summary>
        internal static readonly byte[] BitsReverseTable;

        #endregion

        /// <summary>
        /// The logical 0 based index of what this library reguards as the most significant bit of an byte according to system architecture.
        /// </summary>
        public static readonly int MostSignificantBit = -1;

        /// <summary>
        /// The logical 0 based index of what this library reguards as the least significant bit of an byte according to system architecture.
        /// </summary>
        public static readonly int LeastSignificantBit = -1;

        /// <summary>
        /// The <see cref="ByteOrder"/> of the current architecture
        /// </summary>
        public static readonly ByteOrder SystemByteOrder = ByteOrder.Unknown;

        /// <summary>
        /// The <see cref="BitOrder"/> of the current architecture
        /// </summary>
        public static readonly BitOrder SystemBitOrder = BitOrder.Unknown;

        /// <summary>
        /// The <see cref="BinaryRepresentation"/> of the current architecture used for the <see cref="int"/> type.
        /// </summary>
        public static readonly BinaryRepresentation SystemBinaryRepresentation = BinaryRepresentation.Unknown;

        //Check byte, sbyte, short, ushort, uint?

        //double, decimal?

        #endregion

        #region Constructor

        /// <summary>
        /// Determine BitOrder, ByteOrder and Build Bit Tables
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        static Binary()
        {
            //ensure not already called.
            if (SystemBitOrder != BitOrder.Unknown ||
                SystemBinaryRepresentation != BinaryRepresentation.Unknown ||
                SystemByteOrder != ByteOrder.Unknown || 
                BitsSetTable != null || 
                BitsReverseTable != null) return;

            //Todo, Determine MaxBits (BitSize)

            unchecked
            {
                #region Determine BinaryRepresentation

                //Todo, branchless...

                switch ((SystemBinaryRepresentation = Binary.Nihil != (Binary.Ūnus & -Binary.Ūnus) ?
                            (Binary.Tres & -Binary.Ūnus) == Binary.Ūnus ? 
                                        BinaryRepresentation.SignedMagnitude : BinaryRepresentation.TwosComplement
                        : BinaryRepresentation.OnesComplement))
                {
                    case BinaryRepresentation.TwosComplement:
                        {
                            if (false == IsTwosComplement()) throw new InvalidOperationException("Did not correctly detect BinaryRepresentation");

                            break;
                        }
                    case BinaryRepresentation.OnesComplement:
                        {
                            if (false == IsOnesComplement()) throw new InvalidOperationException("Did not correctly detect BinaryRepresentation");

                            break;
                        }
                    case BinaryRepresentation.SignedMagnitude:
                        {
                            if (false == IsSignedMagnitude()) throw new InvalidOperationException("Did not correctly detect BinaryRepresentation");

                            break;
                        }
                    default:
                        {
                            throw new NotSupportedException("Create an Issue for your Architecture to be supported.");
                        }
                }

                #endregion

                #region DetermineBitOrder and ByteOrder

                //Don't use unsafe code because eventually .Net MF will be completely supported.

                //Ensure integer, short and byte ...

                //Use 128 as a value and get the memory associated with the integer representation of the value
                byte[] memoryOf = BitConverter.GetBytes((int)Binary.SedecimBitSize); //Use ByteOrder

                //Iterate the memory looking for a non 0 value
                for (int offset = 0, endOffset = SizeOfInt; offset < endOffset; ++endOffset)
                {
                    //Take a copy of the byte at the offset in memory
                    byte atOffset = memoryOf[offset];

                    //If the value is non 0
                    if (atOffset != Binary.Nihil)
                    {

                        //Determine the BitOrder using the value
                        //GetBit 0?
                        switch (SystemBitOrder = ((BitOrder)atOffset))
                        {
                            case BitOrder.LeastSignificant:
                                {

                                    MostSignificantBit = Binary.Nihil;

                                    LeastSignificantBit = Binary.Septem;

                                    break;
                                }
                            case BitOrder.MostSignificant:
                                {
                                    MostSignificantBit = Binary.Septem;

                                    LeastSignificantBit = Binary.Nihil;

                                    break;
                                }
                            default:
                                {
                                    throw new NotSupportedException("Create an Issue for your Architecture to be supported.");
                                }
                        }

                        //Determine the ByteOrder using the offset where the value was found
                        switch (offset)
                        {
                            case Binary.Nihil:
                                SystemByteOrder = ByteOrder.Little;
                                break;
                            case Binary.Ūnus:
                                SystemByteOrder = ByteOrder.MiddleLittle;
                                break;
                            case Binary.Duo:
                                SystemByteOrder = ByteOrder.MiddleBig;
                                break;
                            case Binary.Tres:
                                SystemByteOrder = ByteOrder.Big;
                                break;
                        }

                        //Done once a non 0 value was found
                        break;
                    }
                }

                if ((int)SystemByteOrder != ReadInteger(BitConverter.GetBytes((int)ByteOrder.Little), Binary.Nihil, Binary.SizeOfInt, false)) throw new InvalidOperationException("Did not correctly detect ByteOrder");

                if (GetBit((byte)SystemBitOrder, MostSignificantBit)) throw new InvalidOperationException("Did not correctly detect BitOrder");

                memoryOf = null;

                #endregion

                #region Build Bit Tables

                //http://graphics.stanford.edu/~seander/bithacks.html

                BitsSetTable = new byte[Binary.TrīgintāDuoBitSize];

                BitsReverseTable = new byte[Binary.TrīgintāDuoBitSize];

                //Start at 2, since BitsSetTable[0] = BitsReverseTable[0] = 0 
                BitsSetTable[Binary.Ūnus] = BitsReverseTable[Binary.SedecimBitSize] = Binary.Ūnus;

                BitsSetTable[Binary.SedecimBitSize] = BitsReverseTable[Binary.Ūnus] = Binary.SedecimBitSize;

                BitsSetTable[byte.MaxValue] = Binary.BitsPerByte;

                BitsReverseTable[byte.MaxValue] = byte.MaxValue;

                //253 Operations [2 -> 254]
                for (int i = Binary.Duo; i < byte.MaxValue; ++i)
                {
                    byte reverse = MultiplyReverseU8((byte)i);

                    BitsReverseTable[i] = reverse;

                    BitsSetTable[reverse] = BitsSetTable[i] = (byte)((i & Binary.Ūnus) + BitsSetTable[i / Binary.Duo]);
                }

                #endregion
            }
        }

        #endregion

        #endregion

        #region Maximum Values

        /// <summary>
        /// (000000)11 in Binary
        /// </summary>
        public const byte TwoBitMaxValue = Binary.Tres;

        /// <summary>
        /// (0000)1111 in Binary
        /// </summary>
        public const byte FourBitMaxValue = Binary.Quīndecim;

        /// <summary>
        /// (000)11111 in Binary
        /// </summary>
        public const byte FiveBitMaxValue = Binary.TrīgintāŪnus;

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

        #region Bit Sizes

        /// <summary>
        /// 16
        /// </summary>
        public const int DoubleBitSize = Binary.BitsPerByte * Duo;

        /// <summary>
        ///24
        /// </summary>
        public const int TripleBitSize = Binary.BitsPerByte * Tres;

        /// <summary>
        /// 32
        /// </summary>
        public const int QuadrupleBitSize = Binary.BitsPerByte * Quattuor;

        /// <summary>
        /// 64
        /// </summary>
        public const int OctupleBitSize = Binary.BitsPerByte * Binary.BitsPerByte;

        /// <summary>
        /// 128
        /// </summary>
        internal const int SedecimBitSize = Binary.BitsPerByte * DoubleBitSize;

        /// <summary>
        /// 256
        /// </summary>
        internal const int TrīgintāDuoBitSize = Binary.BitsPerByte * QuadrupleBitSize;

        /// <summary>
        /// The amount of bits available to the <see cref="byte"/> type.
        /// </summary>
        public const int BitsPerByte = Binary.Octo;

        /// <summary>
        /// The amount of bits available to the <see cref="short"/> type.
        /// </summary>
        public const int BitsPerShort = DoubleBitSize;

        /// <summary>
        /// The amount of bits available to the <see cref="int"/> type.
        /// </summary>
        public const int BitsPerInt = QuadrupleBitSize;

        /// <summary>
        /// The amount of bits available to the <see cref="long"/> type.
        /// </summary>
        public const int BitsPerLong = OctupleBitSize;

        /// <summary>
        /// The amount of bits available to the <see cref="double"/> type.
        /// </summary>
        public const int BitsPerDouble = BitsPerLong;

        /// <summary>
        /// The amount of bits available to the <see cref="decimal"/> type.
        /// </summary>
        public const int BitsPerDecimal = SedecimBitSize;

        #endregion

        #region Type Sizes

        /// <summary>
        /// The size in bytes of values of the type <see cref="byte"/>
        /// </summary>
        public const int SizeOfByte = sizeof(byte);

        /// <summary>
        /// The size in bytes of values of the type <see cref="short"/>
        /// </summary>
        public const int SizeOfShort = sizeof(short);

        /// <summary>
        /// The size in bytes of values of the type <see cref="char"/>
        /// </summary>
        public const int SizeOfChar = sizeof(char);

        /// <summary>
        /// The size in bytes of values of the type <see cref="int"/>
        /// </summary>
        public const int SizeOfInt = sizeof(int);

        /// <summary>
        /// The size in bytes of values of the type <see cref="long"/>
        /// </summary>
        public const int SizeOfLong = sizeof(long);

        /// <summary>
        /// The size in bytes of values of the type <see cref="double"/>
        /// </summary>
        public const int SizeOfDouble = sizeof(double);

        /// <summary>
        /// The size in bytes of values of the type <see cref="long"/>
        /// </summary>
        public const int SizeOfDecimal = sizeof(decimal);

        #endregion

        #region Bit Methods

        #region BitsSet, BitsUnSet

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
        public static int BitsUnSet(ref byte b) { return Binary.BitsPerByte - BitsSetTable[b]; }

        public static int BitsUnSet(byte b) { return BitsUnSet(ref b); }

        [CLSCompliant(false)]
        public static int BitsSet(ref int i) { return BitConverter.GetBytes(i).Sum(b => BitsSet(b)); }

        public static int BitsSet(int i) { return BitsSet(ref i); }

        [CLSCompliant(false)]
        public static int BitsUnSet(ref int i) { return QuadrupleBitSize - BitConverter.GetBytes(i).Sum(b => BitsSet(b)); }

        public static int BitsUnSet(int i) { return BitsUnSet(ref i); }

        #endregion

        #region GetBit, SetBit, ClearBit, ToggleBit

        /// <summary>
        /// Retrieves a bitfield from the given byte via shifting to discard bits to create a mask
        /// </summary>
        /// <param name="source">The octet to reveal the bit field in</param>
        /// <param name="index">The non 0 based index of the octet to retrieve a bit from</param>
        /// <returns>True if the bit field is set, otherwise false.</returns>
        [CLSCompliant(false)]
        public static bool GetBit(ref byte source, int index)
        {

            if (index < Binary.Nihil || index > Binary.BitsPerByte) throw new ArgumentOutOfRangeException("index", "Must be a value 0 - 8");

            switch (source)
            {
                case byte.MinValue: return false;
                case byte.MaxValue: return true;
                default: return unchecked((source & (Binary.Ūnus << index)) != Binary.Nihil);
            }
        }

        public static bool GetBit(byte source, int index) { return GetBit(ref source, index); }

        public static bool GetBitReverse(byte source, int index) { return GetBitReverse(ref source, index); }

        /// <summary>
        /// Retrieves a bitfield from the given octet via shifting to discard bits to create a mask
        /// </summary>
        /// <param name="source">The octet to reveal the bit field in</param>
        /// <param name="index">The non 0 based index of the octet to retrieve a bit from</param>
        /// <returns>True if the bit field is set, otherwise false.</returns>
        [CLSCompliant(false)]
        public static bool GetBitReverse(ref byte source, int index)
        {

            if (index < Binary.Nihil || index > Binary.BitsPerByte) throw new ArgumentOutOfRangeException("index", "Must be a value 0 - 8");

            switch (source)
            {
                case byte.MinValue: return false;
                case byte.MaxValue: return true;
                default: return unchecked((source & (Binary.SedecimBitSize >> index)) != Binary.Nihil);
            }
        }

        /// <summary>
        /// Provides an implementation of setting a bit in a highly optomized fashion.
        /// Returns the value previously set in the bit.
        /// If the given bit is already set then no further modification is perfored.
        /// </summary>
        /// <param name="source">The octet to set the bit in</param>
        /// <param name="index">The index of the bit to set</param>
        /// <param name="newValue">The value to put in the bit, where true = 1 and false = 0</param>
        /// <returns>The value which was previously set in the bit where true = 1 and false = 0</returns>
        [CLSCompliant(false)]
        public static bool SetBit(ref byte source, int index, bool newValue)
        {
            if (index < Binary.Nihil || index > Binary.BitsPerByte) throw new ArgumentOutOfRangeException("index", "Must be a value 0 - 8");

            //Read the bit
            bool oldValue = GetBit(ref source, index);

            //If the newValue has been set already return
            if (oldValue == newValue) return oldValue;

            //Set or clear the bit according to newValue
            source = (byte)(newValue ? (source | (Binary.Ūnus << index)) : (source & ~(Binary.Ūnus << index)));

            //Return the old value
            return oldValue;
        }

        public static bool SetBit(byte source, int index, bool newValue) { return SetBit(ref source, index, newValue); }


        public static bool SetBitReverse(byte source, int index) { return SetBitReverse(ref source, index); }

        /// <summary>
        /// Provides an implementation of setting the reverse bit in a highly optomized fashion.
        /// Returns the value previously set in the bit.
        /// If the given bit is already set then no further modification is perfored.
        /// </summary>
        /// <param name="source">The octet to set the bit in</param>
        /// <param name="index">The index of the bit to set</param>
        /// <param name="newValue">The value to put in the bit, where true = 1 and false = 0</param>
        /// <returns>The value which was previously set in the bit where true = 1 and false = 0</returns>
        [CLSCompliant(false)]
        public static bool SetBitReverse(ref byte source, int index)
        {

            if (index < Binary.Nihil || index > Binary.BitsPerByte) throw new ArgumentOutOfRangeException("index", "Must be a value 0 - 8");

            switch (source)
            {
                case byte.MinValue: return false;
                case byte.MaxValue: return true;
                default: return unchecked((source | (Binary.SedecimBitSize >> index)) != Binary.Nihil);
            }
        }

        /// <summary>
        /// Sets the index of the given bit to 1 if not already set
        /// </summary>
        /// <param name="source"></param>
        /// <param name="index"></param>
        [CLSCompliant(false)]
        public static void SetBit(ref byte source, int index)
        {
            if (index < Binary.Nihil || index > Binary.BitsPerByte) throw new ArgumentOutOfRangeException("index", "Must be a value 0 - 8");

            source |= (byte)(Binary.Ūnus << index);
        }

        public static void SetBit(byte source, int index) { SetBit(ref source, index); }

        /// <summary>
        /// Clears a bit in <see cref="BitOrder.MostSignificant"/> order
        /// </summary>
        /// <param name="source"></param>
        /// <param name="index"></param>
        [CLSCompliant(false)]
        public static void ClearBit(ref byte source, int index)
        {
            if (index < Binary.Nihil || index > Binary.BitsPerByte) throw new ArgumentOutOfRangeException("index", "Must be a value 0 - 8");

            source &= (byte)(~(Binary.Ūnus << index));
        }

        public static void ClearBit(byte source, int index) { ClearBit(ref source, index); }

        /// <summary>
        /// Clears a bit in <see cref="BitOrder.LeastSignificant"/> order
        /// </summary>
        /// <param name="source"></param>
        /// <param name="index"></param>
        [CLSCompliant(false)]
        public static void ClearBitReverse(ref byte source, int index)
        {
            if (index < Binary.Nihil || index > Binary.BitsPerByte) throw new ArgumentOutOfRangeException("index", "Must be a value 0 - 8");

            source &= (byte)(~(Binary.SedecimBitSize >> index));
        }

        public static void ClearBitReverse(byte source, int index) { ClearBit(ref source, index); }

        
        /// <summary>
        /// Provides a method of setting a bit with XOR
        /// </summary>
        /// <param name="source">The octet to set the bit in</param>
        /// <param name="index">The index of the bit to set</param>
        /// <remarks>
        /// http://stackoverflow.com/questions/2605913/invert-1-bit-in-c-sharp
        /// </remarks>
        [CLSCompliant(false)]
        public static void ToggleBit(ref byte source, int index)
        {
            if (index < Binary.Nihil || index > Binary.BitsPerByte) throw new ArgumentOutOfRangeException("index", "Must be a value 0 - 8");

            source ^= (byte)index;
        }

        public static void ToggleBit(byte source, int index) { ToggleBit(ref source, index); }

        //----- Array Overloads use the above calls.

        public static byte[] SetBit(byte[] self, int index, bool value)
        {
            int bitIndex, byteIndex = Math.DivRem(index, Binary.BitsPerByte, out bitIndex);

            SetBit(ref self[byteIndex], index, value);

            return self;
        }

        public static byte[] ToggleBit(byte[] self, int index)
        {
            int bitIndex, byteIndex = Math.DivRem(index, Binary.BitsPerByte, out bitIndex);

            ToggleBit(ref self[byteIndex], index);

            return self;
        }

        public static bool GetBit(byte[] self, int index, bool value)
        {
            int bitIndex, byteIndex = Math.DivRem(index, Binary.BitsPerByte, out bitIndex);

            return GetBit(ref self[byteIndex], index);
        }

        public static byte[] ClearBit(byte[] self, int index, bool value)
        {
            int bitIndex, byteIndex = Math.DivRem(index, Binary.BitsPerByte, out bitIndex);

            ClearBit(ref self[byteIndex], index);

            return self;
        }

        #endregion

        #region Bitwise


        /// <summary>
        /// Converts the given numbers into equivalent binary representations
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <param name="format">The binary format to convert left and right to</param>
        public static void BitwisePrepare(ref int left, ref int right, BinaryRepresentation format = BinaryRepresentation.NoSign)
        {
            switch (format)
            {
                case BinaryRepresentation.SignedMagnitude:
                    {
                        left = SignedMagnitude(ref left);

                        right = SignedMagnitude(ref right);

                        break;
                    }
                case BinaryRepresentation.TwosComplement:
                    {
                        left = TwosComplement(ref left);

                        right = TwosComplement(ref right);

                        break;
                    }
                case BinaryRepresentation.OnesComplement:
                    {
                        left = OnesComplement(ref left);

                        right = OnesComplement(ref right);

                        break;
                    }
                case BinaryRepresentation.NoSign:
                    {
                        left = (int)((uint)left);

                        right = (int)((uint)right);

                        return;
                    }
                default:
                    {
                        throw new NotSupportedException("Create an issue to have your format supported.");
                    }
            }
        }

        public static int BitwiseAnd(int left, int right)
        {
            BitwisePrepare(ref left, ref right);
            
            return (left & right);
        }

        public static int BitwiseOr(int left, int right)
        {
            BitwisePrepare(ref left, ref right);
            
            return (left | right);
        }

        public static int BitwiseXor(int left, int right)
        {
            BitwisePrepare(ref left, ref right);

            return (left ^ right);
        }

        public static int BitwiseNand(int left, int right)
        {
            BitwisePrepare(ref left, ref right);

            return ~(BitwiseAnd(left, right));
        }

        public static int BitwiseNor(int left, int right)
        {
            BitwisePrepare(ref left, ref right);

            return ~(BitwiseOr(left, right));
        }

        #endregion

        #region ReadBinaryInteger

        //See BitOrder, reverse is typically obtained with knowing if data is the same format
        //e.g. someBitOrder != SystemBitOrder

        //Should be named ReadLeastSignificantBinaryInteger? and then ReadBigEndian could be ReadMostSignificant(Bits/BinaryInteger)

        public static long ReadBinaryInteger(byte[] data, bool reverse = false, int sign = Binary.Ūnus, int bitsPerByte = Binary.BitsPerByte)
        {
            if (data == null) throw new ArgumentNullException("data");

            int byteOffset = 0, bitOffset = 0;

            return (reverse ? ReadReverseBinaryInteger(data, ref byteOffset, ref bitOffset, Binary.BitsPerByte * data.Length, sign, bitsPerByte) : ReadBinaryInteger(data, ref byteOffset, ref bitOffset, bitsPerByte * data.Length, sign, bitsPerByte));
        }

        public static long ReadBinaryInteger(byte[] data, int byteOffset, bool reverse = false, int sign = Binary.Ūnus, int bitsPerByte = Binary.BitsPerByte)
        {
            if (data == null) throw new ArgumentNullException("data");

            int bitOffset = Binary.Nihil;

            return (reverse ? ReadReverseBinaryInteger(data, ref byteOffset, ref bitOffset, Binary.BitsPerByte * data.Length, sign, bitsPerByte) : ReadBinaryInteger(data, ref byteOffset, ref bitOffset, bitsPerByte * data.Length, sign, bitsPerByte));
        }

        public static long ReadBinaryInteger(byte[] data, int byteOffset, int count, bool reverse = false, int sign = Binary.Ūnus, int bitsPerByte = Binary.BitsPerByte)
        {
            if (data == null) throw new ArgumentNullException("data");

            int bitOffset = Binary.Nihil;

            return (reverse ? ReadReverseBinaryInteger(data, ref byteOffset, ref bitOffset, count, sign, bitsPerByte) : ReadBinaryInteger(data, ref byteOffset, ref bitOffset, count, sign, bitsPerByte));
        }

        public static long ReadBinaryInteger(byte[] data, int byteOffset, int count, int bitOffset, bool reverse = false, int sign = Binary.Ūnus, int bitsPerByte = Binary.BitsPerByte)
        {
            if (data == null) throw new ArgumentNullException("data");

            return (reverse ? ReadReverseBinaryInteger(data, ref byteOffset, ref bitOffset, count, sign, bitsPerByte) : ReadBinaryInteger(data, ref byteOffset, ref bitOffset, count, sign, bitsPerByte));
        }

        /// <summary>
        /// Calculates the result of reading a integer value from the data using the specified options.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="byteOffset"></param>
        /// <param name="bitOffset"></param>
        /// <param name="count"></param>
        /// <param name="reverse">Indicates if the <see cref="BitOrder"/> should be reversed in the result</param>
        /// <param name="sign"></param>
        /// <returns></returns>
        [CLSCompliant(false)]
        public static long ReadBinaryInteger(byte[] data, ref int byteOffset, ref int bitOffset, int count, bool reverse = false, int sign = Binary.Ūnus, int bitsPerByte = Binary.BitsPerByte)
        {
            if (data == null) throw new ArgumentNullException("data");

            return (reverse ? ReadReverseBinaryInteger(data, ref byteOffset, ref bitOffset, count, sign, bitsPerByte) : ReadBinaryInteger(data, ref byteOffset, ref bitOffset, count, sign, bitsPerByte));
        }

        /// <summary>
        /// Calculates the result of reading a integer value from the data in <see href="http://en.wikipedia.org/wiki/Bit_numbering">LeastSignificant</see> bit order using the specified options.
        /// </summary>
        /// <param name="data">The data to read</param>
        /// <param name="byteOffset">The offset in <paramref name="data"/> to read from</param>
        /// <param name="bitOffset">The bit offset to start reading from</param>
        /// <param name="count">The amount of bits to read</param>
        /// <param name="sign">The starting value to use to create the decimal representation</param>
        /// <param name="bitsPerByte">The amount of bits in each byte and the shift used to accumulate the result</param>
        /// <returns>The value calulated</returns>
        [CLSCompliant(false)]
        public static long ReadBinaryInteger(byte[] data, ref int byteOffset, ref int bitOffset, int count, long sign = Binary.Ūnus, int bitsPerByte = Binary.BitsPerByte)
        {
            if (count <= Binary.Nihil || sign == Binary.Nihil) return Binary.Nihil;

            unchecked
            {
                //The resulting value
                long value = Binary.Nihil;

                //While there is a bit needed decrement for the bit consumed
                while (count-- > Binary.Nihil)
                {
                    //Check for the end of bits
                    if (bitOffset >= bitsPerByte)
                    {
                        //reset
                        bitOffset = Binary.Nihil;

                        //move the index of the byte
                        ++byteOffset;

                        //Move the value left to 
                        sign <<= bitsPerByte;
                    }

                    //Get a bit from the byte at our offset, if the bit is set the value needs to be incremented
                    if (GetBit(ref data[byteOffset], bitOffset))
                    {
                        //Create the binary representation of the decimal number
                        //Combine the bits of the the represented number into the value
                        value |= (long)((Binary.Ūnus << bitOffset) * sign);
                         
                        //Using the addition operator
                        //value += (long)(Binary.Unum << bitOffset);
                    }

                    //Increment for the bit consumed
                    ++bitOffset;
                }

                //Return the value
                return value;
            }
        }

        /// <summary>
        /// Calculates the result of reading a integer value from the data in <see href="http://en.wikipedia.org/wiki/Bit_numbering">MostSignificant</see> bit order using the specified options.
        /// </summary>
        /// <param name="data">The data to read</param>
        /// <param name="byteOffset">The offset in <paramref name="data"/> to read from</param>
        /// <param name="bitOffset">The bit offset to start reading from</param>
        /// <param name="count">The amount of bits to read</param>
        /// <param name="sign">The starting value to use to create the decimal representation</param>
        /// <param name="bitsPerByte">The amount of bits in each byte and the shift used to accumulate the result</param>
        /// <returns>The value calulated</returns>
        [CLSCompliant(false)]
        public static long ReadReverseBinaryInteger(byte[] data, ref int byteOffset, ref int bitOffset, int count, long sign = Binary.Ūnus, int bitsPerByte = Binary.BitsPerByte)
        {
            if (count <= 0 || sign == Binary.Nihil) return Binary.Nihil;
            
            //The reading offsets
            int reverseByteOffset = byteOffset + Binary.BitsToBytes(count, bitsPerByte) - 1, 
                reverseBitOffset = Binary.Septem - bitOffset;

            unchecked
            {
                //The value and the placeHolder
                long value = Binary.Nihil;

                //While there is a bit needed decrement for the bit consumed
                while (count-- > Binary.Nihil)
                {
                    //Check for the end of bits
                    if (bitOffset >= bitsPerByte)
                    {
                        //Reset the offset of the bit being read
                        reverseBitOffset = Binary.Septem;

                        //Reset the offset of the bit being written
                        bitOffset = Binary.Nihil;

                        //Move the index which corresponds to the byte being read
                        --reverseByteOffset;

                        //Advance the offset which corresponds to the byte being written
                        ++byteOffset;

                        //Create the next value
                        sign <<= bitsPerByte;
                    }

                    //Get a bit from the byte at our offset to determine if the value needs to be incremented
                    if (GetBit(ref data[reverseByteOffset], reverseBitOffset--))
                    {
                        //Combine the bits of the represented number into the value
                        value |= (long)((Binary.Ūnus << bitOffset) * sign);

                        //Using the addition operator
                        //value += (long)(Binary.Unum << bitOffset);
                    }

                    //Increment for the bit consumed
                    ++bitOffset;
                }

                //Return the value
                return value;
            }
        }

        /// <summary>
        /// Calculates the result of reading a integer value from the data with the specified options
        /// </summary>
        /// <param name="data">The data to read</param>
        /// <param name="byteOffset">The offset in data to read from</param>
        /// <param name="bitOffset">The bit offset to start reading from in byteOffset</param>
        /// <param name="reverse">Indicates if the <see cref="BitOrder"/> should be reversed in the result</param>
        /// <param name="count">The amount of bits to read</param>
        /// <param name="sign">The starting value to use to create the decimal representation</param>
        /// <param name="bitsPerByte">The amount of bits in each byte and the shift used to accumulate the result</param>
        /// <returns>The value calulated</returns>
        public static long ReadBinaryInteger(byte[] data, ref int byteOffset, int count, ref int bitOffset, bool reverse = false, int sign = Binary.Ūnus, ByteOrder byteOrder = ByteOrder.Unknown, int bitsPerByte = Binary.BitsPerByte)
        {
            if (data == null) throw new ArgumentNullException("data");

            if (byteOrder == Binary.SystemByteOrder) return ReadBinaryInteger(data, ref byteOffset, ref bitOffset, count, sign, bitsPerByte);
            else if (byteOrder == ByteOrder.Big) return ReadBigEndianInteger(data, ref byteOffset, ref bitOffset, count, sign, bitsPerByte);

            //Todo
            throw new NotImplementedException("Must Implement ConvertToBigEndian");

            //This may be useful but it copies bytes to do the conversion
            //return (byteOrder == Binary.SystemByteOrder ?
            //    ReadBinaryInteger(data, ref byteOffset, ref bitOffset, count, sign, bitsPerByte)
            //    :
            //    ReadBigEndianInteger(ConvertFromBigEndian(data, byteOrder), ref byteOffset, ref bitOffset, count, sign, bitsPerByte));
        }

        #endregion

        #region ReadBigEndianInteger

        public static long ReadBigEndianInteger(byte[] data, long sign = Binary.Ūnus, int bitsPerByte = Binary.BitsPerByte)
        {
            if (data == null) throw new ArgumentNullException("data");

            return ReadBigEndianInteger(data, data.Length * bitsPerByte, sign, bitsPerByte);
        }

        public static long ReadBigEndianInteger(byte[] data, int count, long sign = Binary.Ūnus, int bitsPerByte = Binary.BitsPerByte)
        {
            if (data == null) throw new ArgumentNullException("data");

             int byteOffset = 0, bitOffset = 0;

             return ReadBigEndianInteger(data, ref byteOffset, ref bitOffset, count, sign, bitsPerByte);
        }

        public static long ReadBigEndianInteger(byte[] data, int byteOffset, int count, long sign = Binary.Ūnus, int bitsPerByte = Binary.BitsPerByte)
        {
            if (data == null) throw new ArgumentNullException("data");

            int bitOffset = 0;

            return ReadBigEndianInteger(data, ref byteOffset, ref bitOffset, count, sign, bitsPerByte);
        }

        public static long ReadBigEndianInteger(byte[] data, int byteOffset, int bitOffset, int count, long sign = Binary.Ūnus, int bitsPerByte = Binary.BitsPerByte)
        {
            if (data == null) throw new ArgumentNullException("data");

            return ReadBigEndianInteger(data, ref byteOffset, ref bitOffset, count, sign, bitsPerByte);
        }

        /// <summary>
        /// Calculates the result of reading a big endian integer value from the binary representation using the specified options.
        /// </summary>
        /// <param name="data">The data to read</param>
        /// <param name="byteOffset">The offset in <paramref name="data"/> to read from</param>
        /// <param name="bitOffset">The bit offset to start reading from</param>
        /// <param name="count">The amount of bits to read</param>
        /// <param name="sign">The starting value to use to create the decimal representation</param>
        /// <returns>The big endian value calulated</returns>
        [CLSCompliant(false)]
        public static long ReadBigEndianInteger(byte[] data, ref int byteOffset, ref int bitOffset, int count, long sign = Binary.Ūnus, int bitsPerByte = Binary.BitsPerByte)
        {
            if (count <= 0) return Binary.Nihil;

            //The reading offsets
            int reverseByteOffset = byteOffset + Binary.BitsToBytes(count, bitsPerByte) - 1,
                reverseBitOffset = Binary.Septem - bitOffset;

            unchecked
            {
                //The value and the placeHolder
                long value = Binary.Nihil;

                //While there is a bit needed decrement for the bit consumed
                while (count-- > Binary.Nihil)
                {
                    //Check for the end of bits
                    if (bitOffset >= bitsPerByte)
                    {
                        //Reset the offset of the bit being read
                        reverseBitOffset = Binary.Septem;

                        //Reset the offset of the bit being written
                        bitOffset = Binary.Nihil;

                        //Move the index which corresponds to the byte being read
                        --reverseByteOffset;

                        //Advance the offset which corresponds to the byte being written
                        ++byteOffset;

                        //Create the next value
                        sign <<= bitsPerByte;
                    }

                    //Get a bit from the byte at our offset to determine if the value needs to be incremented
                    if (GetBit(ref data[reverseByteOffset], reverseBitOffset--))
                    {
                        //Combine the bits of the represented number into the value
                        value |= (long)((Binary.SedecimBitSize >> bitOffset) * sign);
                    }

                    //Increment for the bit consumed
                    ++bitOffset;
                }

                //Return the value
                return value;
            }
        }

        #endregion

        #endregion

        #region BitEnumerator

        /// <summary>
        /// Iterates the bits in data according to the host <see cref="BitOrder"/>
        /// </summary>
        /// <param name="data"></param>
        /// <param name="byteOffset"></param>
        /// <param name="bitOffset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static IEnumerable<bool> GetEnumerator(byte[] data, int byteOffset, int bitOffset, int count)
        {
            if (count <= 0) yield break;

            unchecked
            {
                //While there is a bit needed decrement for the bit consumed
                while (count-- > Binary.Nihil)
                {
                    //Check for the end of bits
                    if (bitOffset >= Binary.BitsPerByte)
                    {
                        //Reset the bit offset
                        bitOffset = Binary.Nihil;

                        //Advance the index of the byte
                        ++byteOffset;
                    }

                    //Yeild the result of reading the bit at the bitOffset, increasing the bitOffset
                    yield return GetBit(ref data[byteOffset], bitOffset++);
                }
            }
        }

        /// <summary>
        /// Interates the bits in data in reverse of the host <see cref="BitOrder"/>
        /// </summary>
        /// <param name="data"></param>
        /// <param name="byteOffset"></param>
        /// <param name="bitOffset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static IEnumerable<bool> GetReverseEnumerator(byte[] data, int byteOffset, int bitOffset, int count)
        {
            if (count <= 0) yield break;

            unchecked
            {
                //While there is a bit needed decrement for the bit consumed
                while (count-- > Binary.Nihil)
                {
                    //Check for the end of bits
                    if (bitOffset >= Binary.BitsPerByte)
                    {
                        //reset the bit offset
                        bitOffset = Binary.Nihil;

                        //Advance the index of the byte being read
                        ++byteOffset;
                    }

                    //Yeild the result of reading the reverse bit at the bitOffset, increasing the bitOffset
                    yield return GetBitReverse(ref data[byteOffset], bitOffset++);
                }
            }
        }

        #endregion

        #region CopyBits


        public static byte[] CopyBits(byte[] data)
        {
            if (data == null) throw new ArgumentNullException("data");

            int byteOffset = 0, bitOffset = 0;

            return CopyBits(data, ref byteOffset, ref bitOffset, Binary.BitsPerByte * data.Length);
        }

        public static byte[] CopyBits(byte[] data, int count)
        {
            if (data == null) throw new ArgumentNullException("data");

            int byteOffset = 0, bitOffset = 0;

            return CopyBits(data, ref byteOffset, ref bitOffset, count);
        }

        public static byte[] CopyBits(byte[] data, int byteOffset, int count)
        {
            if (data == null) throw new ArgumentNullException("data");

            int bitOffset = 0;

            return CopyBits(data, ref byteOffset, ref bitOffset, count);
        }

        public static byte[] CopyBits(byte[] data, int byteOffset, int bitOffset, int count)
        {
            if (data == null) throw new ArgumentNullException("data");

            return CopyBits(data, ref byteOffset, ref bitOffset, count);
        }

        /// <summary>
        /// Creates a new byte array with the resulting bits set in the positions as they were encountered in.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="byteOffset"></param>
        /// <param name="bitOffset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        [CLSCompliant(false)]
        public static byte[] CopyBits(byte[] data, ref int byteOffset, ref int bitOffset, int count)
        {
            if (count <= Binary.Nihil) return Media.Common.MemorySegment.EmptyBytes;

            byte[] result = new byte[Binary.BitsToBytes(count)];

            unchecked
            {
                //While there is a bit needed decrement for the bit consumed
                while (count-- > Binary.Nihil)
                {

                    //Check for the end of bits
                    if (bitOffset >= Binary.BitsPerByte)
                    {
                        //reset
                        bitOffset = Binary.Nihil;

                        //move the index of the byte being read
                        ++byteOffset;
                    }

                    //Get a bit from the byte at our offset and if the bit is set 
                    if (GetBit(ref data[byteOffset], bitOffset))
                    {
                        //Set the bit in the result
                        SetBit(ref result[byteOffset], bitOffset);
                    }

                    //Increment for the bit consumed
                    ++bitOffset;
                }

                return result;
            }
        }

        public static byte[] CopyBitsReverse(byte[] data)
        {
            if (data == null) throw new ArgumentNullException("data");

            int byteOffset = 0, bitOffset = 0;

            return CopyBitsReverse(data, ref byteOffset, ref bitOffset, Binary.BitsPerByte * data.Length);
        }

        public static byte[] CopyBitsReverse(byte[] data, int count)
        {
            if (data == null) throw new ArgumentNullException("data");

            int byteOffset = 0, bitOffset = 0;

            return CopyBitsReverse(data, ref byteOffset, ref bitOffset, count);
        }

        public static byte[] CopyBitsReverse(byte[] data, int byteOffset, int count)
        {
            if (data == null) throw new ArgumentNullException("data");

            int bitOffset = 0;

            return CopyBitsReverse(data, ref byteOffset, ref bitOffset, count);
        }

        public static byte[] CopyBitsReverse(byte[] data, int byteOffset, int bitOffset, int count)
        {
            if (data == null) throw new ArgumentNullException("data");

            return CopyBitsReverse(data, ref byteOffset, ref bitOffset, count);
        }

        /// <summary>
        /// Creates a new array with the resulting bits set in the reverse positions as they were encountered in.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="byteOffset"></param>
        /// <param name="bitOffset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        [CLSCompliant(false)]
        public static byte[] CopyBitsReverse(byte[] data, ref int byteOffset, ref int bitOffset, int count)
        {
            //If there is nothing to read then do nothing
            if (count <= Binary.Nihil) return Media.Common.MemorySegment.EmptyBytes;

            //The reading offsets
            int reverseByteOffset = byteOffset + Binary.BitsToBytes(count),
                reverseBitOffset = Binary.Septem - bitOffset;

            //The resulting data, take 1 away because 0 is the lower bound.
            byte[] result = new byte[reverseByteOffset--];
            
            unchecked
            {
                //While there is a bit needed decrement for the bit consumed
                while (count-- > Binary.Nihil)
                {

                    //Check for the end of bits
                    if (bitOffset >= Binary.BitsPerByte)
                    {
                        //Reset the offset being read
                        reverseBitOffset = Binary.Septem;

                        //Reset the offset being written
                        bitOffset = Binary.Nihil;

                        //Advance the index which corresponds to the byte being read
                        ++byteOffset;
                        
                        //Move the index which corresponds to the byte beging written
                        --reverseByteOffset;
                    }

                    //Get a bit from the byte at our offset and if the bit is set 
                    if (GetBit(ref data[reverseByteOffset], reverseBitOffset--))
                    {
                        //Set the bit in the result
                        SetBit(ref result[byteOffset], bitOffset);
                    }

                    //Move to the next bit in the byte being read
                    ++bitOffset;
                }

                return result;
            }
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
        /// <param name="sign">The value to use as a sign</param>
        /// <param name="shift">The amount of bits to shift the sign for each byte</param>
        /// <returns>The calculated result</returns>
        public static long ReadInteger(IEnumerable<byte> octets, int offset, int sizeInBytes, bool reverse, long sign = Binary.Ūnus, int shift = Binary.BitsPerByte)
        {

            if (sizeInBytes < Binary.Nihil) throw new ArgumentException("sizeInBytes", "Must be at greater than 0.");

            if (sizeInBytes > Binary.Octo) throw new NotSupportedException("Only sizes up to 8 octets are supported in a long.");

            if (sizeInBytes == 0 || sign == 0) return Binary.Nihil;

            unchecked
            {
                //Start with 0
                long value = Binary.Nihil;

                //Select the range
                var selected = octets.Skip(offset).Take(sizeInBytes);

                //Reverse it
                if (reverse) selected = selected.Reverse();

                //Using an enumerator
                using (var enumerator = selected.GetEnumerator())
                {
                    //While there is a value to read
                    while (enumerator.MoveNext())
                    {
                        //If the byte is greater than 0
                        if (enumerator.Current > byte.MinValue)
                        {
                            //Combine the result of the calculation of the calulated value with the binary representation.
                            value |= (uint)enumerator.Current * sign;
                        }

                        //Move the sign shift left
                        sign <<= shift;
                    }
                }

                return value;
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
            return (byte)Binary.ReadInteger(buffer, index, Binary.Ūnus, reverse);
        }

        [CLSCompliant(false)]
        public static sbyte Read8(IEnumerable<byte> buffer, int index, bool reverse)
        {
            return (sbyte)Binary.ReadInteger(buffer, index, Binary.Ūnus, reverse);
        }

        /// <summary>
        /// Reads an unsigned 16 bit value type from the given buffer.
        /// </summary>
        /// <param name="buffer">The buffer to Read the unsigned 16 bit value from</param>
        /// <param name="index">The index in the buffer to Read the value from</param>
        /// <param name="reverse">A value which indicates if the value should be reversed</param>
        /// <returns>The unsigned 16 bit value from the given buffer</returns>
        /// <summary>
        /// The <paramref name="reverse"/> is typically utilized when creating Big ByteOrder \ Network Byte Order or encrypted values.
        /// </summary>
        [CLSCompliant(false)]
        public static ushort ReadU16(IEnumerable<byte> buffer, int index, bool reverse)
        {
            return (ushort)Binary.ReadInteger(buffer, index, Binary.SizeOfShort, reverse);
        }

        public static short Read16(IEnumerable<byte> buffer, int index, bool reverse)
        {
            return (short)Binary.ReadInteger(buffer, index, Binary.SizeOfShort, reverse);
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
            return (uint)Binary.ReadInteger(buffer, index, Binary.Tres, reverse);
        }

        public static int Read24(IEnumerable<byte> buffer, int index, bool reverse)
        {
            return (int)Binary.ReadInteger(buffer, index, Binary.Tres, reverse);
        }

        /// <summary>
        /// Reads an unsgined 32 bit value from the given buffer at the specified index.
        /// </summary>
        /// <param name="buffer">The buffer to Read the unsigned 32 bit value from</param>
        /// <param name="index"></param>
        /// <param name="reverse">A value which indicates if the value should be reversed</param>
        /// <returns>The unsigned 32 bit value from the given buffer</returns>
        /// <summary>
        /// The <paramref name="reverse"/> is typically utilized when creating Big ByteOrder \ Network Byte Order or encrypted values.
        /// </summary>
        [CLSCompliant(false)]
        public static uint ReadU32(IEnumerable<byte> buffer, int index, bool reverse)
        {
            return (uint)Binary.ReadInteger(buffer, index, Binary.SizeOfInt, reverse);
        }

        public static int Read32(IEnumerable<byte> buffer, int index, bool reverse)
        {
            return (int)Binary.ReadInteger(buffer, index, Binary.SizeOfInt, reverse);
        }

        [CLSCompliant(false)]
        public static ulong ReadU64(IEnumerable<byte> buffer, int index, bool reverse)
        {
            return (ulong)Binary.ReadInteger(buffer, index, Binary.SizeOfLong, reverse);
        }

        public static long Read64(IEnumerable<byte> buffer, int index, bool reverse)
        {
            return (long)Binary.ReadInteger(buffer, index, Binary.SizeOfLong, reverse);
        }

        #endregion

        #region GetBytes

        public static byte[] GetBytes(short i, bool reverse)
        {
            byte[] result = new byte[Binary.SizeOfShort];
            Write16(result, 0, reverse, i);
            return result;
        }

        public static byte[] GetBytes(int i, bool reverse)
        {
            byte[] result = new byte[Binary.SizeOfInt];
            Write32(result, 0, reverse, i);
            return result;
        }

        public static byte[] GetBytes(long i, bool reverse)
        {
            byte[] result = new byte[Binary.SizeOfShort];
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
        public static void WriteInteger(byte[] buffer, int index, int count, ulong value, int shift = Binary.BitsPerByte)
        {
            if (buffer == null || count == 0) return;

            unchecked
            {
                //While something remains
                while (count-- > 0)
                {

                    //Write the byte at the index
                    buffer[index++] = (byte)(value);

                    //Remove the bits we used
                    value >>= shift;
                }

                //For unaligned writes a new function should be created
                //if (value > byte.MinValue && value <= byte.MaxValue) buffer[index - 1] = (byte)value;
            }
        }

        public static void WriteReversedInteger(byte[] buffer, int index, int count, long value)
        {
            WriteReversedInteger(buffer, index, count, value);
        }

        [CLSCompliant(false)]
        public static void WriteReversedInteger(byte[] buffer, int index, int count, ulong value, int shift = Binary.BitsPerByte)
        {
            if (buffer == null || count == 0) return;

            unchecked
            {
                //While something remains
                while(count > 0)
                {
                    //Write the byte at the reversed index
                    buffer[index + --count] = (byte)(value);

                    //Remove the bits we used
                    value >>= shift;
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
            WriteInteger(buffer, index, Binary.SizeOfShort, value, reverse);
        }

        [CLSCompliant(false)]
        public static void Write16(byte[] buffer, int index, bool reverse, ushort value)
        {
            WriteInteger(buffer, index, Binary.SizeOfShort, (short)value, reverse);
        }

        [CLSCompliant(false)]
        public static void Write24(byte[] buffer, int index, bool reverse, uint value)
        {
            WriteInteger(buffer, index, Binary.Tres, (int)value, reverse);
        }

        public static void Write24(byte[] buffer, int index, bool reverse, int value)
        {
            WriteInteger(buffer, index, Binary.Tres, (int)value, reverse);
        }

        [CLSCompliant(false)]
        public static void Write32(byte[] buffer, int index, bool reverse, uint value)
        {
            WriteInteger(buffer, index, Binary.SizeOfInt, (int)value, reverse);
        }

        //Todo
        public static void Write32(byte[] buffer, int index, bool reverse, int value)
        {
            WriteInteger(buffer, index, Binary.SizeOfInt, (int)value, reverse);
        }

        /// <summary>
        /// Writes a Big ByteOrder 64 bit value to the given buffer at the given index
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="index"></param>
        /// <param name="reverse">A value indicating if the given value should be written in reverse</param>
        /// <param name="value"></param>
        [CLSCompliant(false)]
        public static void Write64(byte[] buffer, int index, bool reverse, ulong value)
        {
            WriteInteger(buffer, index, Binary.SizeOfLong, value, reverse);
        }

        //Todo
        public static void Write64(byte[] buffer, int index, bool reverse, long value)
        {
            WriteInteger(buffer, index, Binary.SizeOfLong, value, reverse);
        }

        #endregion

        #region ConvertFromBigEndian

        /// <summary>
        /// Re-orders the data in place based on the required byte order
        /// </summary>
        /// <param name="source"></param>
        /// <param name="byteOrder"></param>
        /// <returns></returns>
        public static byte[] ConvertFromBigEndian(byte[] source, ByteOrder byteOrder)
        {
            //Determine what byte order the data is going to
            switch (byteOrder)
            {
                case ByteOrder.Little:
                    Array.Reverse(source);

                    break;

                case ByteOrder.MiddleBig:
                    int halfSize = source.Length / 2;

                    Array.Reverse(source, halfSize, halfSize);

                    break;

                case ByteOrder.MiddleLittle:

                    Array.Reverse(source, 0, source.Length / 2);

                    break;

            }

            //Return the data
            return source;
        }

        #endregion

        #region ConvertToBigEndian


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
            return BitsReverseTable[source];
        }

        public static byte ReverseU8(byte source) { return ReverseU8(ref source); }

        /// <summary>
        /// Reverses the given unsigned 8 bit value via calculation of the reverse value.
        /// </summary>
        /// <notes><see href="http://graphics.stanford.edu/~seander/bithacks.html">Bit Twiddling Hacks</see></notes>
        [CLSCompliant(false)]
        public static byte MultiplyReverseU8(ref byte source)
        {
            return (byte)(((source * 0x80200802UL) & 0x0884422110UL) * 0x0101010101UL >> QuadrupleBitSize);
        }

        public static byte MultiplyReverseU8(byte source) { return MultiplyReverseU8(ref source); }

        [CLSCompliant(false)]
        public static sbyte Reverse8(sbyte source) { return (sbyte)ReverseU8((byte)source); }

        [CLSCompliant(false)]
        public static ushort ReverseUnsignedShort(ushort source) { return ReverseUnsignedShort(ref source); }

        [CLSCompliant(false)]
        public static ushort ReverseUnsignedShort(ref ushort source)
        {
            return (ushort)(((source & 0xFFU) << 8) | ((source >> 8) & 0xFFU));
        }

        [CLSCompliant(false)]
        public static uint ReverseUnsignedInt(uint source) { return ReverseUnsignedInt(ref source); }

        [CLSCompliant(false)]
        public static uint ReverseUnsignedInt(ref uint source)
        {
            return (uint)((((source & 0x000000FFU) << 24) | ((source & 0x0000FF00U) << 8) | ((source & 0x00FF0000U) >> 8) | ((source & 0xFF000000U) >> 24)));
        }


        [CLSCompliant(false)]
        public static ulong ReverseUnsignedLong(ulong source) { return ReverseUnsignedLong(ref source); }

        [CLSCompliant(false)]
        public static ulong ReverseUnsignedLong(ref ulong source)
        {
            return (source & 0x00000000000000FFUL) << 56 | (source & 0x000000000000FF00UL) << 40 | (source & 0x0000000000FF0000UL) << 24 | (source & 0x00000000FF000000UL) << 8 |
                   (source & 0x000000FF00000000UL) >> 8 | (source & 0x0000FF0000000000UL) >> 24 | (source & 0x00FF000000000000UL) >> 40 | (source & 0xFF00000000000000UL) >> 56;
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
            return ReverseUnsignedLong(ref source);
        }

        public static long Reverse64(long source) { return Reverse64(ref source); }

        [CLSCompliant(false)]
        public static long Reverse64(ref long source)
        {
            if (source == 0 || source == long.MaxValue) return source;
            ulong unsigned = (ulong)source;
            return (long)ReverseUnsignedLong(ref unsigned);
        }

        #endregion

        #region Roll

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
            switch (source)
            {
                case 0:
                case ulong.MaxValue:
                    return source;
                default: return source >> amount | source << amount;
            }
        }

        public static long Roll64(long source, int amount)
        {
            switch (source)
            {
                case 0:
                case long.MaxValue:
                    return source;
                default: return source >> amount | source << amount;
            }
        }

        #endregion

        //BCD?

        //ZigZag
    }

    #endregion
}
