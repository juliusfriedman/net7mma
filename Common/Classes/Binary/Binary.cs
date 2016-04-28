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

        #region Internal Constants

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

        #region Public Constants

        public const int Zero = Binary.Nihil;

        public const int One = Binary.Ūnus;

        public const int Three = Binary.Tres;

        public const int ThirtyOne = Binary.TrīgintāŪnus;

        #endregion

        //Could be moved to BitSizes
        #region Public Bit Sizes

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
        public const int BitsPerInteger = QuadrupleBitSize;

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

        //Could be moved to TypeSizes
        #region Public Type Sizes

        /// <summary>
        /// The size in bytes of values of the type <see cref="byte"/>
        /// </summary>
        public const int BytesPerByte = sizeof(byte);

        /// <summary>
        /// The size in bytes of values of the type <see cref="short"/>
        /// </summary>
        public const int BytesPerShort = sizeof(short);

        /// <summary>
        /// The size in bytes of values of the type <see cref="char"/>
        /// </summary>
        public const int BytesPerChar = sizeof(char);

        /// <summary>
        /// The size in bytes of values of the type <see cref="int"/>
        /// </summary>
        public const int BytesPerInteger = sizeof(int);

        /// <summary>
        /// The size in bytes of values of the type <see cref="long"/>
        /// </summary>
        public const int BytesPerLong = sizeof(long);

        /// <summary>
        /// The size in bytes of values of the type <see cref="double"/>
        /// </summary>
        public const int BytesPerDouble = sizeof(double);

        /// <summary>
        /// The size in bytes of values of the type <see cref="decimal"/>
        /// </summary>
        public const int BytesPerDecimal = sizeof(decimal);

        #endregion

        #region Maximum Values

        /// <summary>
        /// (000000)11 in Binary, Decimal 3
        /// </summary>
        public const byte TwoBitMaxValue = Binary.Tres;

        /// <summary>
        /// (00000)111 in Binary, Decimal 7
        /// </summary>
        public const byte ThreeBitMaxValue = Binary.Septem;

        /// <summary>
        /// (0000)1111 in Binary, Decimal 15
        /// </summary>
        public const byte FourBitMaxValue = Binary.Quīndecim;

        /// <summary>
        /// (000)11111 in Binary, Decimal 31
        /// </summary>
        public const byte FiveBitMaxValue = Binary.TrīgintāŪnus;

        //63 for 6 bit

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

        #region Enumerations

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

        #endregion

        #region Todo

        //Rounding

        //ParseInt (base)

        #endregion

        //Could be moved to BinaryMethods

        #region Abs

        public static int Abs(int value) { return Abs(ref value); }

        [CLSCompliant(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static int Abs(ref int value) { long v = value; return(int)Abs(ref v); }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static long Abs(long value) { return Abs(ref value); }

        [CLSCompliant(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static long Abs(ref long value)
        {
            long mask = value >> 28;//BytesPerInteger * Binary.Septem;

            return (value + mask) ^ mask;
        }

        #endregion

        #region Min, Max

        /// <summary>
        /// Returns the minimum value which is bounded inclusively by min and max.
        /// </summary>
        /// <param name="min">The minimum value</param>
        /// <param name="max">The maximum value</param>
        /// <returns>The value.</returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static byte Min(ref byte min, ref byte max)
        {
            return (byte)(max ^ ((min ^ max) & -(min < max ? 1 : 0)));

        }

        [CLSCompliant(false)]
        public static byte Min(byte min, byte max)
        {
            return Min(ref min, ref max);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static int Min(ref int min, ref int max)
        {
            return (int)(max ^ ((min ^ max) & -(min < max ? 1 : 0)));

        }

        [CLSCompliant(false)]
        public static int Min(int min, int max)
        {
            return Min(ref min, ref max);
        }

        [CLSCompliant(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static uint Min(ref uint min, ref uint max)
        {
            return (uint)(max ^ ((min ^ max) & (uint)(-(min < max ? 1 : 0))));

        }

        [CLSCompliant(false)]
        public static uint Min(uint min, uint max)
        {
            return Min(ref min, ref max);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static long Min(ref long min, ref long max)
        {
            return (long)(max ^ ((min ^ max) & -(min < max ? 1 : 0)));

        }

        [CLSCompliant(false)]
        public static ulong Min(ulong min, ulong max)
        {
            return Min(ref min, ref max);
        }

        [CLSCompliant(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static ulong Min(ref ulong min, ref ulong max)
        {
            return (ulong)(max ^ ((min ^ max) & (ulong)(-(min < max ? 1 : 0))));

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
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static byte Max(ref byte min, ref byte max)
        {
            return (byte)(min ^ ((min ^ max) & -(min < max ? 1 : 0)));
        }

        [CLSCompliant(false)]
        public static byte Max(byte min, byte max)
        {
            return Max(ref min, ref max);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static uint Max(ref uint min, ref uint max)
        {
            return min ^ ((min ^ max) & (uint)(-(min < max ? 1 : 0)));
        }

        [CLSCompliant(false)]
        public static uint Max(uint min, uint max)
        {
            return Max(ref min, ref max);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static int Max(ref int min, ref int max)
        {
            return min ^ ((min ^ max) & -(min < max ? 1 : 0));
        }

        [CLSCompliant(false)]
        public static int Max(int min, int max)
        {
            return Max(ref min, ref max);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static long Max(ref long min, ref long max)
        {
            return (long)(min ^ ((min ^ max) & -(min < max ? 1 : 0)));

        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static ulong Max(ref ulong min, ref ulong max)
        {
            return (ulong)(min ^ ((min ^ max) & (ulong)(-(min < max ? 1 : 0))));

        }

        [CLSCompliant(false)]
        public static ulong Max(ulong min, ulong max)
        {
            return Max(ref min, ref max);
        }

        [CLSCompliant(false)]
        public static long Max(long min, long max)
        {
            return Max(ref min, ref max);
        }

        #endregion

        #region Clamp

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]

        public static byte Clamp(ref byte value, ref byte min, ref byte max)
        {
            return Binary.Min(Binary.Max(min, value), max);
        }

        [CLSCompliant(false)]
        public static byte Clamp(byte value, byte min, byte max)
        {
            return Clamp(ref value, ref min, ref max);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static int Clamp(ref int value, ref int min, ref int max)
        {
            return Binary.Min(Binary.Max(ref min, ref value), max);
        }

        [CLSCompliant(false)]
        public static int Clamp(int value, int min, int max)
        {
            return Clamp(ref value, ref min, ref max);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
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
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
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
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]

        public static bool IsPowerOfTwo(ref long x) { return Binary.Nihil == (x & (x - Binary.Ūnus)); }

        [CLSCompliant(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]

        public static bool IsPowerOfTwo(ref int x) { return Binary.Nihil == (x & (x - Binary.Ūnus)); }

        public static bool IsPowerOfTwo(long x) { return IsPowerOfTwo(ref x); }

        public static bool IsPowerOfTwo(int x) { return IsPowerOfTwo(ref x); }

        #endregion

        #region IsEven

        [CLSCompliant(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]

        public static bool IsEven(ref int x) { return Binary.Nihil == (x & Binary.Ūnus); }

        public static bool IsEven(int x) { return IsEven(ref x); }

        #endregion

        #region IsOdd

        [CLSCompliant(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static bool IsOdd(ref int x) { return Binary.Ūnus == (x & Binary.Ūnus); }

        public static bool IsOdd(int x) { return IsOdd(ref x); }

        /// <summary>
        /// Determines if the given value is negitive
        /// </summary>
        /// <param name="arg"></param>
        /// <returns>True if the value is negative, otherwise false</returns>
        [CLSCompliant(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static bool IsNegative(ref int arg) { return (arg & SignMask) == SignMask; }

        public static bool IsNegative(int arg) { return IsNegative(ref arg); }

        /// <summary>
        /// Determines if the given value is negitive
        /// </summary>
        /// <param name="arg"></param>
        /// <returns>True if the value is negative, otherwise false</returns>
        [CLSCompliant(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static bool IsNegative(ref long arg) { return (arg & SignMask) == SignMask; }

        public static bool IsNegative(long arg) { return IsNegative(ref arg); }

        /// <summary>
        /// Determines if the given value is positive
        /// </summary>
        /// <param name="arg"></param>
        /// <returns>True if the value is positive, otherwise false</returns>
        [CLSCompliant(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static bool IsPositive(ref int arg) { return (BitConverter.DoubleToInt64Bits(arg) & SignMask) != SignMask; }

        public static bool IsPositive(int arg) { return IsPositive(ref arg); }

        /// <summary>
        /// Determines if the given value is positive
        /// </summary>
        /// <param name="arg"></param>
        /// <returns>True if the value is positive, otherwise false</returns>
        [CLSCompliant(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static bool IsPositive(ref long arg) { return (BitConverter.DoubleToInt64Bits(arg) & SignMask) != SignMask; }

        public static bool IsPositive(long arg) { return IsPositive(ref arg); }

        #endregion

        #region Sign

        public static int Sign(int value) { return Sign(ref value); }

        [CLSCompliant(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static int Sign(ref int value)
        {
            return ((value > Binary.Nihil) ? Binary.Ūnus : Binary.Nihil) - ((value < Binary.Nihil) ? Binary.Ūnus : Binary.Nihil);

            //Same as Math.Sign
            //return value > 0 ? 1 : value < 0 ? -1 : 0;
        }

        public static int Sign(long value) { return Sign(ref value); }

        [CLSCompliant(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static int Sign(ref long value)
        {
            return ((value > Binary.Nihil) ? Binary.Ūnus : Binary.Nihil) - ((value < Binary.Nihil) ? Binary.Ūnus : Binary.Nihil);

            //Same as Math.Sign
            //return value > 0 ? 1 : value < 0 ? -1 : 0;
        }

        #endregion

        //Could be moved to BinaryConversions
        #region Conversions

        #region BitsToBytes

        //Align

        public static int BitsToBytes(int bitCount, int bitsPerByte = Binary.BitsPerByte) { return (int)BitsToBytes((uint)bitCount, (uint) bitsPerByte); }

        [CLSCompliant(false)]
        public static uint BitsToBytes(uint bitCount, uint bitsPerByte = Binary.BitsPerByte) { return BitsToBytes(ref bitCount, bitsPerByte); }

        [CLSCompliant(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static uint BitsToBytes(ref uint bitCount, uint bitsPerByte = Binary.BitsPerByte)
        {
            if(bitCount == Binary.Nihil) return Binary.Nihil;

            long bits, bytes = Math.DivRem(bitCount, bitsPerByte, out bits);

            return (uint)(bits > Binary.Nihil ? ++bytes : bytes);
        }

        [CLSCompliant(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static int BitsToBytes(ref int bitCount)
        {
            int remainder = bitCount & Binary.Septem; return (bitCount >> Binary.Tres) + (remainder & Binary.Ūnus);
        }

        #endregion

        #region BytesToBits

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
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static uint BytesToBits(ref uint byteCount, uint bitSize = Binary.BitsPerByte) { return byteCount * bitSize; }

        [CLSCompliant(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static int BytesToBits(ref int byteCount){ return byteCount << Binary.Tres;}

        #endregion

        #region BytesToMachineWords

        public static int BytesToMachineWords(int byteCount, int bytesPerMachineWord = Binary.BytesPerInteger) { return (int)BytesToMachineWords((uint)byteCount, (uint)bytesPerMachineWord); }

        [CLSCompliant(false)]
        public static uint BytesToMachineWords(uint byteCount, uint bytesPerMachineWord = Binary.BytesPerInteger) { return BytesToMachineWords(ref byteCount, bytesPerMachineWord); }

        [CLSCompliant(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static uint BytesToMachineWords(ref uint byteCount, uint bytesPerMachineWord = Binary.BytesPerInteger)
        {
            if (byteCount == Binary.Nihil) return Binary.Nihil;

            long remainder, bytes = Math.DivRem(byteCount, bytesPerMachineWord, out remainder);

            return (uint)(remainder > Binary.Nihil ? ++bytes : bytes);
        }

        [CLSCompliant(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static int BytesToMachineWords(ref int byteCount)
        {
            int remainder = byteCount & Binary.Tres; return (byteCount >> Binary.Quinque) + (remainder & Binary.Ūnus);
        }

        #endregion

        #region MachineWordsToBytes

        public static int MachineWordsToBytes(int machineWords, int bytesPerMachineWord = Binary.BytesPerInteger) { return (int)MachineWordsToBytes((uint)machineWords, (uint)bytesPerMachineWord); }

        [CLSCompliant(false)]
        public static uint MachineWordsToBytes(uint machineWords, uint bytesPerMachineWord = Binary.BytesPerInteger) { return MachineWordsToBytes(ref machineWords, bytesPerMachineWord); }


        [CLSCompliant(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static uint MachineWordsToBytes(ref uint machineWords, uint bytesPerMachineWord = Binary.BytesPerInteger)
        {
            return machineWords * bytesPerMachineWord;
        }

        [CLSCompliant(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static int MachineWordsToBytes(ref int machineWords) { return machineWords << Binary.Quinque; }

        #endregion

        #endregion

        //Some could be moved to BinaryConversions
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
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static bool IsNegative(ref double arg) { return (BitConverter.DoubleToInt64Bits(arg) & SignMask) == SignMask; }

        public static bool IsNegative(double arg) { return IsNegative(ref arg); }

        /// <summary>
        /// Determines if the given value is positive
        /// </summary>
        /// <param name="arg"></param>
        /// <returns>True if the value is positive, otherwise false</returns>
        [CLSCompliant(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static bool IsPositive(ref double arg) { return (BitConverter.DoubleToInt64Bits(arg) & SignMask) != SignMask; }

        public static bool IsPositive(double arg) { return IsPositive(ref arg); }

        #endregion

        #region Fields

        #region Bit Tables

        /// <summary>
        /// Indicates if the byte is the same when interpreted in reverse
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        [CLSCompliant(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static bool IsPalindrome(ref byte b)
        {
            switch (b)
            {
                case 0:
                case 24:
                case 36:
                case 60:
                case 66:
                case 90:
                case 102:
                case 126:
                case 129:
                case 153:
                case 165:
                case 189:
                case 195:
                case 219:
                case 231:
                case 255:
                    return true;
                default: return false;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static bool IsPalindrome(byte b) { return IsPalindrome(ref b); }

        /// <summary>
        /// Assumes 8 bits per byte
        /// </summary>
        internal static readonly byte[] BitsSetTable;

        /// <summary>
        /// Assumes 8 bits per byte
        /// </summary>
        internal static readonly byte[] BitsReverseTable;

        #endregion

        //readonly ValueType
        /// <summary>
        /// The logical 0 based index of what this library reguards as the most significant bit of an byte according to system architecture.
        /// </summary>
        internal static int m_MostSignificantBit = -1;

        /// <summary>
        /// The logical 0 based index of what this library reguards as the most significant bit of an byte according to system architecture.
        /// </summary>
        public static int MostSignificantBit { get { return m_MostSignificantBit; } }

        //readonly ValueType
        /// <summary>
        /// The logical 0 based index of what this library reguards as the least significant bit of an byte according to system architecture.
        /// </summary>
        internal static int m_LeastSignificantBit = -1;

        /// <summary>
        /// The logical 0 based index of what this library reguards as the least significant bit of an byte according to system architecture.
        /// </summary>
        public static int LeastSignificantBit { get { return m_LeastSignificantBit; } }

        //readonly ValueType
        /// <summary>
        /// The <see cref="ByteOrder"/> of the current architecture
        /// </summary>
        internal static ByteOrder m_SystemByteOrder = ByteOrder.Unknown;

        /// <summary>
        /// The <see cref="BitOrder"/> of the current architecture
        /// </summary>
        public static ByteOrder SystemByteOrder { get { return m_SystemByteOrder; } }

        //readonly ValueType
        /// <summary>
        /// The <see cref="BitOrder"/> of the current architecture
        /// </summary>
        internal static BitOrder m_SystemBitOrder = BitOrder.Unknown;

        /// <summary>
        /// The <see cref="BitOrder"/> of the current architecture
        /// </summary>
        public static BitOrder SystemBitOrder { get { return m_SystemBitOrder; } }

        /// <summary>
        /// Gets a value indicating if the system's byte order is Big Endian.
        /// </summary>
        public static bool IsBigEndian { get { return m_SystemByteOrder == ByteOrder.Big; } }

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
            if (m_SystemBitOrder != BitOrder.Unknown ||
                m_SystemByteOrder != ByteOrder.Unknown || 
                BitsSetTable != null || 
                BitsReverseTable != null) return;

            //Todo, Determine MaxBits (BitSize)
            //Todo, Determine x64 or x86 paths.

            bool x64 = Environment.Is64BitProcess;

            unchecked
            {
                #region DetermineBitOrder and ByteOrder

                //Don't use unsafe code because eventually .Net MF will be completely supported.
                //Not because you can't but because of the implications
                //If Unsafe is used then only the Non - Generic Subset will be supported although you could just as well have Generics too...

                //Ensure integer, short and byte ...

                //Use 128 as a value and get the memory associated with the integer representation of the value
                byte[] memoryOf = BitConverter.GetBytes((int)Binary.SedecimBitSize); //Use ByteOrder

                //Iterate the memory looking for a non 0 value
                for (int offset = 0, endOffset = BytesPerInteger; offset < endOffset; ++endOffset)
                {
                    //Take a copy of the byte at the offset in memory
                    byte atOffset = memoryOf[offset];

                    //If the value is non 0
                    if (atOffset != Binary.Nihil)
                    {

                        //Determine the BitOrder using the value
                        //GetBit 0?
                        switch (m_SystemBitOrder = ((BitOrder)atOffset))
                        {
                            case BitOrder.LeastSignificant:
                                {

                                    m_MostSignificantBit = Binary.Nihil;

                                    m_LeastSignificantBit = Binary.Septem;

                                    break;
                                }
                            case BitOrder.MostSignificant:
                                {
                                    m_MostSignificantBit = Binary.Septem;

                                    m_LeastSignificantBit = Binary.Nihil;

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
                                m_SystemByteOrder = ByteOrder.Little;
                                break;
                            case Binary.Ūnus:
                                m_SystemByteOrder = ByteOrder.MiddleLittle;
                                break;
                            case Binary.Duo:
                                m_SystemByteOrder = ByteOrder.MiddleBig;
                                break;
                            case Binary.Tres:
                                m_SystemByteOrder = ByteOrder.Big;
                                break;
                        }

                        //Done once a non 0 value was found
                        break;
                    }
                }

                if ((int)m_SystemByteOrder != ReadInteger(BitConverter.GetBytes((int)ByteOrder.Little), Binary.Nihil, Binary.BytesPerInteger, false)) throw new InvalidOperationException("Did not correctly detect ByteOrder");

                if (GetBit((byte)m_SystemBitOrder, m_MostSignificantBit)) throw new InvalidOperationException("Did not correctly detect BitOrder");

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

                #region Palindromes

                //Todo, optimize...

                //Some bytes cannot be reversed and should skipped. (mod 3, mod 2)
                //There are at least 16 values out of 255 which are palindromes. (All are products of 3)
                //http://stackoverflow.com/questions/845772/how-to-check-if-the-binary-representation-of-an-integer-is-a-palindrome

                /*
              Value | Quotient of division 3
                0   | 0
                24  | 8
                36  | 12
                60  | 20
                66  | 22
                90  | 30
                102 | 34
                126 | 42
                129 | 43
                153 | 51
                165 | 55
                189 | 63
                195 | 65
                219 | 73
                231 | 77
                255 | 85
                 */

                //This implies that it should only take 83 total operations to populate the reverse table. (85 including 0 and 255)
                //All other values should be shifts from those values
                //Since there are 8 bits in a byte, 8 * 8 possible permuatations would be 64, half of those values are reversed of the prior so there are 32 unique permutations.
                //....

                #endregion

                //253 Operations [2 -> 254]
                for (byte i = Binary.Duo; i < byte.MaxValue; ++i)
                {
                    byte reverse = x64 ? MultiplyReverseU8_64(ref i) : MultiplyReverseU8_32(ref i);

                    BitsReverseTable[i] = reverse;

                    BitsSetTable[reverse] = BitsSetTable[i] = (byte)((i & Binary.Ūnus) + BitsSetTable[i / Binary.Duo]);
                }

                #endregion
            }
        }

        #endregion

        //Could be moved to BinaryMethods
        #region Bit Methods

        #region CountTrailing / CountLeading Zeros

        internal static int[] DeBruijnPositions =
	    {
	        0, 1, 28, 2, 29, 14, 24, 3, 30, 22, 20, 15, 25, 17, 4, 8,
	        31, 27, 13, 23, 21, 19, 16, 7, 26, 12, 18, 6, 11, 5, 10, 9
	    };

        /// <summary>
        /// builtin_ctz implementation
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [CLSCompliant(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static int CountTrailingZeros(ref uint value)
        {
            switch (value)
            {
                case 0: return BitsPerInteger;
                case uint.MaxValue:
                case int.MaxValue: return 0;
                default:
                    {
                        unchecked
                        {
                            //Convert the value if it's not a power of two
                            //if (false == IsPowerOfTwo(ref value))
                            //{
                            //    int valueCopy = value;
                            //    valueCopy |= valueCopy >> 1;
                            //    valueCopy |= valueCopy >> 2;
                            //    valueCopy |= valueCopy >> 4;
                            //    valueCopy |= valueCopy >> 8;
                            //    valueCopy |= valueCopy >> 16;
                            //    return DeBruijnPositions[(valueCopy * 0x07C4ACDD) >> 27];
                            //}

                            //Uses a lookup based on the magic constant

                            return DeBruijnPositions[((uint)(value & -value) * 0x077CB531U) >> 27];
                        }
                    }
            }
        }

        [CLSCompliant(false)]
        public static int CountTrailingZeros(uint value) { return CountTrailingZeros(ref value); }

        public static int CountTrailingZeros(int value) { uint temp = (uint)value; return CountTrailingZeros(ref temp); }

        /// <summary>
        /// builtin_ctz implementation
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [CLSCompliant(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static int CountTrailingZeros(ref ulong value)
        {
            switch (value)
            {
                case 0: return BitsPerLong;
                case ulong.MaxValue:
                case long.MaxValue:
                case uint.MaxValue:
                case int.MaxValue: return 0;
                default:
                    {
                        return CountTrailingZeros((uint)(value & int.MaxValue)) + CountTrailingZeros((uint)(value >> BitsPerInteger));
                    }
            }
        }

        [CLSCompliant(false)]
        public static int CountTrailingZeros(ulong value) { return CountTrailingZeros(ref value); }

        public static int CountTrailingZeros(long value) { ulong temp = (ulong)value;  return CountTrailingZeros(ref temp); }

        internal static int[] ReverseDeBruijnPositions =
	    {
	         0, 31, 9, 30, 3, 8, 13, 29, 2, 5, 7, 21, 12, 24, 28, 19,
             1, 10, 4, 14, 6, 22, 25, 20, 11, 15, 23, 26, 16, 27, 17, 18
	    };

        //https://gist.github.com/andrewrk/1883543
        // map a bit value mod 37 to its position for 64 bit..
        //about 3x slower than __builtin_ctz
        internal static uint[] Mod37BitPosition = new uint[]
        {
            4294967295, //-1
            0, 1, 26, 2, 23, 27, 0, 3, 16, 24, 30, 28, 11, 0, 13, 4,
            7, 17, 0, 25, 22, 31, 15, 29, 10, 12, 6, 0, 21, 14, 9, 5,
            20, 8, 19, 18
        };

        /// <summary>
        /// builtin_clz implementation
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [CLSCompliant(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static int CountLeadingZeros(ref uint value)
        {
            switch (value)
            {
                case 0: return BitsPerInteger;                
                case uint.MaxValue:
                case int.MaxValue: return 0;
                default:
                    {
                        //Use the 64 bit method if possible. (Machine may also be helpful)
                        if (Environment.Is64BitProcess)
                        {
                            return (int)Mod37BitPosition[(-value & value) % 37];
                        }

                        //Could reverse the int and return CountTrailing...

                        //Could use rebase
                        //int lz = (int)(32 - Math.Log((double)value + 1, 2d));
                        //lz += (int)((value - (0x80000000u >> lz)) >> 31);

                        //Ensure a power of two, could also use a different sequence and constant to avoid

                        //uint x = value;
                        value |= value >> 1;
                        value |= value >> 2;
                        value |= value >> 4;
                        value |= value >> 8;
                        value |= value >> 16;
                        value++;

                        return ReverseDeBruijnPositions[value * 0x076be629 >> 27];
                    }
            }
        }

        public static int CountLeadingZeros(int value) { uint temp = (uint)value; return CountLeadingZeros(ref temp); }

        [CLSCompliant(false)]
        public static int CountLeadingZeros(uint value) { return CountLeadingZeros(ref value); }

        [CLSCompliant(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static int CountLeadingZeros(ref ulong value)
        {
            switch (value)
            {
                case 0: return BitsPerLong;
                case ulong.MaxValue:
                case long.MaxValue:
                case uint.MaxValue:
                case int.MaxValue: return 0;
                default:
                    {
                        return CountLeadingZeros((uint)(value & int.MaxValue)) + CountLeadingZeros((uint)(value << BitsPerInteger));
                    }
            }
        }

        public static int CountLeadingZeros(long value) { ulong temp = (ulong)value; return CountLeadingZeros(ref temp); }

        public static long GreatestCommonDivisor(long a, long b)
        {
            return (long)GreatestCommonDivisor((ulong)a, (ulong)b);
        }

        [CLSCompliant(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static ulong GreatestCommonDivisor(ulong a, ulong b)
        {

            if (a == 0) return b;
            if (b == 0) return a;

            int shift = Common.Binary.CountTrailingZeros((uint)(a | b));
            a >>= Common.Binary.CountTrailingZeros((uint)a);

            do
            {
                b >>= Common.Binary.CountTrailingZeros((uint)b);

                if (a > b) a -= b;
                else b -= a;

            } while (b > 0);

            return a << shift;
        }

        #endregion

        #region BitsSet, BitsUnSet

        #region HammingWeight

        /// <summary>
        /// The Hamming weight of a string is the number of symbols that are different from the zero-symbol of the alphabet used. 
        /// It is thus equivalent to the Hamming distance from the all-zero string of the same length. 
        /// For the most typical case, a string of bits, this is the number of 1's in the string. In this binary case, it is also called the population count, popcount or sideways sum.
        /// https://en.wikipedia.org/wiki/Hamming_weight
        /// </summary>
        /// <param name="value">The input</param>
        /// <returns>The hamming weight for the given input</returns>
        [CLSCompliant(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static int HammingWeight(ref int value)
        {
            value = value - ((value >> 1) & 0x55555555);

            value = (value & 0x33333333) + ((value >> 2) & 0x33333333);

            return ((value + (value >> 4) & 0xF0F0F0F) * 0x1010101) >> 24;
        }

        public static int HammingWeight(int value) { return HammingWeight(ref value); }

        #endregion

        /// <summary>
        /// Determines the amount of bits set in the byte
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        [CLSCompliant(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static int BitsSet(ref byte b) { return BitsSetTable[b]; }

        public static int BitsSet(byte b) { return BitsSet(ref b); }

        /// <summary>
        /// Determines the amount of bits not set in the byte
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        [CLSCompliant(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static int BitsUnSet(ref byte b) { return Binary.BitsPerByte - BitsSetTable[b]; }

        public static int BitsUnSet(byte b) { return BitsUnSet(ref b); }

        [CLSCompliant(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static int BitsSet(ref int i) { return Binary.GetBytes(i).Sum(b => BitsSet(b)); }

        public static int BitsSet(int i) { return BitsSet(ref i); }

        [CLSCompliant(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static int BitsUnSet(ref int i) { return QuadrupleBitSize - Binary.GetBytes(i).Sum(b => BitsSet(b)); }

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
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
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
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
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
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
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

        [CLSCompliant(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static bool SetBitReverse(ref byte source, int index, bool newValue)
        {
            if (index < Binary.Nihil || index > Binary.BitsPerByte) throw new ArgumentOutOfRangeException("index", "Must be a value 0 - 8");

            //Read the bit in reverse
            bool oldValue = GetBitReverse(ref source, index);

            //If the newValue has been set already return
            if (oldValue == newValue) return oldValue;

            //Set or clear the bit according to newValue
            source = (byte)(newValue ? (source | (Binary.SedecimBitSize >> index)) : (source & ~(Binary.SedecimBitSize >> index)));

            //Return the old value
            return oldValue;
        }


        public static bool SetBitReverse(byte source, int index, bool newValue) { return SetBitReverse(ref source, index, newValue); }

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
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static void SetBitReverse(ref byte source, int index)
        {
            if (index < Binary.Nihil || index > Binary.BitsPerByte) throw new ArgumentOutOfRangeException("index", "Must be a value 0 - 8");

            //Set the bit
            source = (byte)(source | (Binary.SedecimBitSize >> index));
        }

        /// <summary>
        /// Sets the index of the given bit to 1 if not already set
        /// </summary>
        /// <param name="source"></param>
        /// <param name="index"></param>
        [CLSCompliant(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
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
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
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
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
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
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static void ToggleBit(ref byte source, int index)
        {
            if (index < Binary.Nihil || index > Binary.BitsPerByte) throw new ArgumentOutOfRangeException("index", "Must be a value 0 - 8");

            source ^= (byte)index;
        }

        public static void ToggleBit(byte source, int index) { ToggleBit(ref source, index); }

        //----- Array Overloads use the above calls.

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static byte[] SetBit(byte[] self, int index, bool value)
        {
            //int bitIndex, byteIndex = Math.DivRem(index, Binary.BitsPerByte, out bitIndex);

            //Only increases after bitIndex has been exhausted (bitIndex == 0)
            int byteIndex = index >> Binary.Tres;

            //(source index) Always <= 7, then decreases for each iteration
            int bitIndex = Binary.Septem - (index & Binary.Septem);

            SetBit(ref self[byteIndex], bitIndex, value);

            return self;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static byte[] ToggleBit(byte[] self, int index)
        {
            //int bitIndex, byteIndex = Math.DivRem(index, Binary.BitsPerByte, out bitIndex);

            //Only increases after bitIndex has been exhausted (bitIndex == 0)
            int byteIndex = index >> Binary.Tres;

            //(source index) Always <= 7, then decreases for each iteration
            int bitIndex = Binary.Septem - (index & Binary.Septem);

            ToggleBit(ref self[byteIndex], bitIndex);

            return self;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static bool GetBit(byte[] self, int index)
        {
            //int bitIndex, byteIndex = Math.DivRem(index, Binary.BitsPerByte, out bitIndex);

            //Only increases after bitIndex has been exhausted (bitIndex == 0)
            int byteIndex = index >> Binary.Tres;

            //(source index) Always <= 7, then decreases for each iteration
            int bitIndex = Binary.Septem - (index & Binary.Septem);

            return GetBit(ref self[byteIndex], bitIndex);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static byte[] ClearBit(byte[] self, int index)
        {
            //int bitIndex, byteIndex = Math.DivRem(index, Binary.BitsPerByte, out bitIndex);

            //Only increases after bitIndex has been exhausted (bitIndex == 0)
            int byteIndex = index >> Binary.Tres;

            //(source index) Always <= 7, then decreases for each iteration
            int bitIndex = Binary.Septem - (index & Binary.Septem);

            ClearBit(ref self[byteIndex], bitIndex);

            return self;
        }

        //Reverse overloads?

        #endregion

        #region ReadBits

        //Todo, overload for byteOffset and ref byteOffset

        public static long ReadBits(byte[] bytes, int bitOffset, int bitCount, bool reverse)
        {
            return ReadBits(bytes, bitOffset, bitCount,
                (Binary.m_SystemBitOrder == BitOrder.MostSignificant ? //Determine what to do based first on the Binary.SystemBitOrder
                //MostSignificant -> Determine reverse or not
                (reverse ? BitOrder.LeastSignificant : BitOrder.MostSignificant)
                : //LeastSignificant -> Determine reverse or not
                (reverse ? BitOrder.MostSignificant : BitOrder.LeastSignificant)));
        }

        //ByteOrder overloads?

        public static long ReadBits(byte[] bytes, int bitOffset, int bitCount, Binary.BitOrder order)
        {
            switch (order)
            {
                case BitOrder.LeastSignificant: return (long)ReadBitsLSB(bytes, bitOffset, bitCount);
                case BitOrder.MostSignificant: return (long)ReadBitsMSB(bytes, bitOffset, bitCount);
                default: throw new NotImplementedException("A definite BitOrder must be supplied.");
            }
        }

        [CLSCompliant(false)]
        public static long ReadBits(byte[] bytes, ref int bitOffset, int bitCount, Binary.BitOrder order)
        {
            switch (order)
            {
                case BitOrder.LeastSignificant: return (long)ReadBitsLSB(bytes, ref bitOffset, bitCount);
                case BitOrder.MostSignificant: return (long)ReadBitsMSB(bytes, ref bitOffset, bitCount);
                default: throw new NotImplementedException("A definite BitOrder must be supplied.");
            }
        }

        public static long ReadBitsMSB(byte bits, int bitOffset, int bitCount)
        {
            return (long)ReadBitsMSB(Common.Extensions.Object.ObjectExtensions.ToArray<byte>(bits), bitOffset, bitCount);
        }

        [CLSCompliant(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static ulong ReadBitsMSB(byte[] bytes, int bitOffset, int bitCount) //ref/out byteOffset
        {
            if (bytes == null || bitCount <= 0) return Binary.Nihil;

            ulong result = 0;

            int byteOffset, bitIndex, position, bitCountMinusOne = bitCount - 1;

            //optimize

            for (int index = bitOffset, end = bitOffset + bitCount; index < end; ++index)
            {
                //Only increases after bitIndex has been exhausted (bitIndex == 0)
                byteOffset = index >> Binary.Tres;

                //(source index) Always <= 7, then decreases for each iteration
                bitIndex = Binary.Septem - (index & Binary.Septem);

                //(destination index) decreases
                position = bitCountMinusOne - (index - bitOffset);
#if UNSAFE
                unsafe
                {
                    //Set the bits using the pointer to the byte to avoid the range check.
                    //Even if the array is moved the pointer is taken each time it's needed
                    //result |= ((ulong)((*((byte*)bytes[byteOffset]) >> bitIndex) & Binary.Ūnus) << position);

                    result |= ((ulong)((((byte)*(byte*)System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement<byte>(bytes, byteOffset)) >> bitIndex) & Binary.Ūnus) << position);

                    //fixed (byte* x = &bytes[byteOffset]) { result |= ((ulong)((*x >> bitIndex) & Binary.Ūnus) << position); } 
                }

#else
                //Set the bit in result by reading the bit from the source and moving it to the destination
                result |= ((ulong)((bytes[byteOffset] >> bitIndex) & Binary.Ūnus) << position);
#endif
            }

            return result;
        }

        [CLSCompliant(false)]
        public static ulong ReadBitsMSB(byte[] bytes, ref int bitOffset, int bitCount) 
        {
            ulong result = ReadBitsMSB(bytes, bitOffset, bitCount);

            bitOffset += bitCount;

            return result;
        }

        public static long ReadBitsLSB(byte bits, int bitOffset, int bitCount)
        {
            return (long)ReadBitsLSB(Common.Extensions.Object.ObjectExtensions.ToArray<byte>(bits), bitOffset, bitCount);
        }

        [CLSCompliant(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static ulong ReadBitsLSB(byte[] bytes, int bitOffset, int bitCount) //ref byteOffset
        {
            if (bytes == null || bitCount <= 0) return Binary.Nihil;

            ulong result = 0;

            int byteOffset, bitIndex, position;

            //optimize

            for (int index = bitOffset, end = bitOffset + bitCount; index < end; ++index)
            {
                //Divide by 8
                //Only increases after bitIndex has been exhausted (bitIndex == 7)
                byteOffset = index >> Binary.Tres;
                
                //Modulo 8
                //(source index) Always <= 7, then increases for each iteration
                bitIndex = index & Binary.Septem;

                //byteOffset = Math.DivRem(index, 8, out bitIndex);

                //(destination index) increases
                position = index - bitOffset;

#if UNSAFE
                unsafe
                {
                    //Set the bits using the pointer to the byte to avoid the range check.
                    //Even if the array is moved the pointer is taken each time it's needed
                    //result |= ((ulong)((*((byte*)bytes[byteOffset]) >> bitIndex) & Binary.Ūnus) << position);

                    result |= ((ulong)((((byte)*(byte*)System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement<byte>(bytes, byteOffset)) >> bitIndex) & Binary.Ūnus) << position);

                    //fixed (byte* x = &bytes[byteOffset]) { result |= ((ulong)((*x >> bitIndex) & Binary.Ūnus) << position); } 

                }

#else

                //Set the bit in result by reading the bit from the source and moving it to the destination
                result |= ((ulong)((bytes[byteOffset] >> bitIndex) & Binary.Ūnus) << position);
#endif
      
            }

            return result;
        }

        [CLSCompliant(false)]
        public static ulong ReadBitsLSB(byte[] bytes, ref int bitOffset, int bitCount)
        {
            ulong result = ReadBitsLSB(bytes, bitOffset, bitCount);
            
            bitOffset += bitCount;

            return result;
        }

        #endregion

        #region WriteBits

        public static void WriteBits(byte[] bytes, int bitOffset, int bitCount, long value, bool reverse)
        {
            WriteBits(bytes, bitOffset, bitCount, value,
                (Binary.m_SystemBitOrder == BitOrder.MostSignificant ? //Determine what to do based first on the Binary.SystemBitOrder
                  //MostSignificant -> Determine reverse or not
                (reverse ? BitOrder.LeastSignificant : BitOrder.MostSignificant)
                : //LeastSignificant -> Determine reverse or not
                (reverse ? BitOrder.MostSignificant : BitOrder.LeastSignificant)));
        }

        public static void WriteBits(byte[] bytes, int bitOffset, int bitCount, long value, Binary.BitOrder order)
        {
            switch (order)
            {
                case BitOrder.LeastSignificant: WriteBitsLSB(bytes, bitOffset, (ulong)value, bitCount); return;
                case BitOrder.MostSignificant: WriteBitsMSB(bytes, bitOffset, (ulong)value, bitCount); return;
                default: throw new NotImplementedException("A definite BitOrder must be supplied.");
            }
        }
        
        //ByteOrder overloads?

        [CLSCompliant(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static void WriteBitsLSB(byte[] bytes, int bitOffset, ulong value, int bitCount)
        {
            if (bytes == null || bitCount <= 0) return;

            int position, byteOffset, bitIndex;

            bool bitValue;

            //optimize

            for (int index = bitOffset, end = bitOffset + bitCount; index < end; ++index)
            {
                position = index - bitOffset;
                
                bitValue = ((value >> position) & Binary.Ūnus) > 0;
                
                byteOffset = index >> Binary.Tres;
                
                bitIndex = index & Binary.Septem;

                //byteOffset = Math.DivRem(index, 8, out bitIndex);
#if UNSAFE
                unsafe
                {
                    if (bitValue)
                    {
                        //*((byte*)bytes[byteOffset]) |= (byte)(Binary.Ūnus << bitIndex);

                        //result |= ((ulong)((((byte)*(byte*)System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement<byte>(bytes, byteOffset)) >> bitIndex) & Binary.Ūnus) << position);

                        *(byte*)System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement<byte>(bytes, byteOffset) |= (byte)(Binary.Ūnus << bitIndex);

                        //fixed (byte* x = &bytes[byteOffset]) { *x |= (byte)(Binary.Ūnus << bitIndex); } 
                    }
                    else
                    {
                        //*((byte*)bytes[byteOffset]) &= (byte)~(Binary.Ūnus << bitIndex);

                        *(byte*)System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement<byte>(bytes, byteOffset) &= (byte)~(Binary.Ūnus << bitIndex);

                        fixed (byte* x = &bytes[byteOffset]) { *x &= (byte)~(Binary.Ūnus << bitIndex); } 
                    }
                }
#else
                if (bitValue)
                {
                    bytes[byteOffset] |= (byte)(Binary.Ūnus << bitIndex);
                }
                else
                {
                    bytes[byteOffset] &= (byte)~(Binary.Ūnus << bitIndex);
                }
#endif
            }
        }

        [CLSCompliant(false)]
        public static void WriteBitsLSB(byte[] bytes, ref int bitOffset, ulong value, int bitCount)
        {
            WriteBitsLSB(bytes, bitOffset, value, bitCount);

            bitOffset += bitCount;
        }

        [CLSCompliant(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static void WriteBitsMSB(byte[] bytes, int bitOffset, ulong value, int bitCount)
        {
            if (bytes == null || bitCount <= 0) return;

            int position, byteOffset, bitIndex, bitCountMinusOne = (bitCount - Binary.Ūnus);

            bool bitValue;

            //optimize

            for (int index = bitOffset, end = bitOffset + bitCount; index < end; ++index)
            {
                position = bitCountMinusOne - (index - bitOffset);
                
                bitValue = ((value >> position) & Binary.Ūnus) > 0;

                byteOffset = index >> Binary.Tres;

                bitIndex = Binary.Septem - (index & Binary.Septem);

#if UNSAFE
                unsafe
                {
                    if (bitValue)
                    {
                        //*((byte*)bytes[byteOffset]) |= (byte)(Binary.Ūnus << bitIndex);

                        *(byte*)System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement<byte>(bytes, byteOffset) |= (byte)(Binary.Ūnus << bitIndex);

                        //fixed (byte* x = &bytes[byteOffset]) { *x |= (byte)(Binary.Ūnus << bitIndex); } 
                    }
                    else
                    {
                        //*((byte*)bytes[byteOffset]) &= (byte)~(Binary.Ūnus << bitIndex);

                        *(byte*)System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement<byte>(bytes, byteOffset) &= (byte)~(Binary.Ūnus << bitIndex);

                        //fixed (byte* x = &bytes[byteOffset]) { *x &= (byte)~(Binary.Ūnus << bitIndex); } 
                    }
                }
#else
                if (bitValue)
                {
                    bytes[byteOffset] |= (byte)(Binary.Ūnus << bitIndex);
                }
                else
                {
                    bytes[byteOffset] &= (byte)~(Binary.Ūnus << bitIndex);
                }

#endif
            }
        }

        [CLSCompliant(false)]
        public static void WriteBitsMSB(byte[] bytes, ref int bitOffset, ulong value, int bitCount)
        {
            WriteBitsMSB(bytes, bitOffset, value, bitCount);

            bitOffset += bitCount;
        }

        #endregion

        //Todo, CopyBits should allow destination to be specified...

        //Not used anywhere besides UnitTests...

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

        #region CopyBitsTo

        //Same as CopyBits but performs inplace.

        public static void CopyBitsTo(byte[] srcBits, int srcByteOffset, int srcBitOffset, byte[] destBits, int destByteOffset, int destBitOffset, int count)
        {
            if (count <= Binary.Nihil) return;

            unchecked
            {
                //While there is a bit needed decrement for the bit consumed
                while (count-- > Binary.Nihil)
                {
                    //Check for the end of bits
                    if (srcBitOffset >= Binary.BitsPerByte)
                    {
                        //reset
                        srcBitOffset = Binary.Nihil;

                        //move the index of the byte being read
                        ++srcByteOffset;
                    }

                    //Get a bit from the byte at our offset and Set the bit in the result
                    SetBit(ref destBits[srcByteOffset], srcBitOffset, GetBit(ref srcBits[srcByteOffset], srcBitOffset));

                    //Increment for the bit consumed
                    ++srcBitOffset;
                }
            }
        }

        #endregion

        #endregion

        #region Reading

        public static Guid ReadGuid(IEnumerable<byte> octets, int offset, bool reverse)
        {
            return new Guid(
                Read32(octets, ref offset, reverse),
                Read16(octets, ref offset, reverse),
                Read16(octets, ref offset, reverse),
                //Might not have to reverse these values... (would depend on how 8 bit values were stored in octets)
                ReadU8(octets, ref offset, reverse), //reverse && isBigEndian
                ReadU8(octets, ref offset, reverse),
                ReadU8(octets, ref offset, reverse),
                ReadU8(octets, ref offset, reverse),
                ReadU8(octets, ref offset, reverse),
                ReadU8(octets, ref offset, reverse),
                ReadU8(octets, ref offset, reverse),
                ReadU8(octets, ref offset, reverse));
        }              

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
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
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
        public static byte ReadU8(IEnumerable<byte> buffer, ref int index, bool reverse)
        {
            return ReadU8(buffer, index++, reverse);
        }

        [CLSCompliant(false)]
        public static sbyte Read8(IEnumerable<byte> buffer, int index, bool reverse)
        {
            return (sbyte)Binary.ReadInteger(buffer, index, Binary.Ūnus, reverse);
        }

        [CLSCompliant(false)]
        public static sbyte Read8(IEnumerable<byte> buffer, ref int index, bool reverse)
        {
            return Read8(buffer, index++, reverse);
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
            return (ushort)Binary.ReadInteger(buffer, index, Binary.BytesPerShort, reverse);
        }

        [CLSCompliant(false)]
        public static ushort ReadU16(IEnumerable<byte> buffer, ref int index, bool reverse)
        {
            ushort value = (ushort)Binary.ReadInteger(buffer, index, Binary.BytesPerShort, reverse);

            index += Binary.BytesPerShort;
            
            return value;
        }

        public static short Read16(IEnumerable<byte> buffer, int index, bool reverse)
        {
            return (short)Binary.ReadInteger(buffer, index, Binary.BytesPerShort, reverse);
        }

        [CLSCompliant(false)]
        public static short Read16(IEnumerable<byte> buffer, ref int index, bool reverse)
        {
            short value = (short)Binary.ReadInteger(buffer, index, Binary.BytesPerShort, reverse);

            index += Binary.BytesPerShort;

            return value;
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

        [CLSCompliant(false)]
        public static uint ReadU24(IEnumerable<byte> buffer, ref int index, bool reverse)
        {
            uint value = (uint)Binary.ReadInteger(buffer, index, Binary.Tres, reverse);
            
            index += Binary.Three;
            
            return value;
        }

        public static int Read24(IEnumerable<byte> buffer, int index, bool reverse)
        {
            return (int)Binary.ReadInteger(buffer, index, Binary.Tres, reverse);
        }

        [CLSCompliant(false)]
        public static int Read24(IEnumerable<byte> buffer, ref int index, bool reverse)
        {
            int value = (int)Binary.ReadInteger(buffer, index, Binary.Tres, reverse);
            
            index += Binary.Three;

            return value;
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
            return (uint)Binary.ReadInteger(buffer, index, Binary.BytesPerInteger, reverse);
        }

        [CLSCompliant(false)]
        public static uint ReadU32(IEnumerable<byte> buffer, ref int index, bool reverse)
        {
            uint value = (uint)Binary.ReadInteger(buffer, index, Binary.BytesPerInteger, reverse);

            index += Binary.BytesPerInteger;

            return value;
        }

        public static int Read32(IEnumerable<byte> buffer, int index, bool reverse)
        {
            return (int)Binary.ReadInteger(buffer, index, Binary.BytesPerInteger, reverse);
        }

        [CLSCompliant(false)]
        public static int Read32(IEnumerable<byte> buffer, ref int index, bool reverse)
        {
            int value = (int)Binary.ReadInteger(buffer, index, Binary.BytesPerInteger, reverse);

            index += Binary.BytesPerInteger;

            return value;
        }

        [CLSCompliant(false)]
        public static ulong ReadU64(IEnumerable<byte> buffer, int index, bool reverse)
        {
            return (ulong)Binary.ReadInteger(buffer, index, Binary.BytesPerLong, reverse);
        }

        [CLSCompliant(false)]
        public static ulong ReadU64(IEnumerable<byte> buffer, ref int index, bool reverse)
        {
            ulong value = (ulong)Binary.ReadInteger(buffer, index, Binary.BytesPerLong, reverse);

            index += Binary.BytesPerLong;

            return value;
        }

        public static long Read64(IEnumerable<byte> buffer, int index, bool reverse)
        {
            return Binary.ReadInteger(buffer, index, Binary.BytesPerLong, reverse);
        }

        [CLSCompliant(false)]
        public static long Read64(IEnumerable<byte> buffer, ref int index, bool reverse)
        {
            long value = Binary.ReadInteger(buffer, index, Binary.BytesPerLong, reverse);

            index += Binary.BytesPerLong;

            return value;
        }

        //double, decimal

        #endregion

        #region Writing

        static void WriteGuid(byte[] data, int offset, Guid value, bool reverse)
        {
            GetBytes(value, reverse).CopyTo(data, offset);
        }

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

        //Could take value by ref to reduce copies and help with offset tracking.

        [CLSCompliant(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
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
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static void WriteReversedInteger(byte[] buffer, int index, int count, ulong value, int shift = Binary.BitsPerByte)
        {
            if (buffer == null || count == 0) return;

            unchecked
            {
                //While something remains
                while (count/*--*/ > 0)
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

        [CLSCompliant(false)]
        public static void Write8(byte[] buffer, int index, bool reverse, sbyte value)
        {
            buffer[index] = reverse ? ReverseU8((byte)value) : (byte)value;
        }

        /// <summary>
        /// Writes a the given unsgined 16 bit value to the buffer at the given index.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="index"></param>
        /// <param name="reverse"></param>
        /// <param name="value"></param>
        public static void Write16(byte[] buffer, int index, bool reverse, short value)
        {
            WriteInteger(buffer, index, Binary.BytesPerShort, value, reverse);
        }

        [CLSCompliant(false)]
        public static void Write16(byte[] buffer, int index, bool reverse, ushort value)
        {
            WriteInteger(buffer, index, Binary.BytesPerShort, (short)value, reverse);
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
            WriteInteger(buffer, index, Binary.BytesPerInteger, (int)value, reverse);
        }

        //Todo
        public static void Write32(byte[] buffer, int index, bool reverse, int value)
        {
            WriteInteger(buffer, index, Binary.BytesPerInteger, (int)value, reverse);
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
            WriteInteger(buffer, index, Binary.BytesPerLong, value, reverse);
        }

        //Todo
        public static void Write64(byte[] buffer, int index, bool reverse, long value)
        {
            WriteInteger(buffer, index, Binary.BytesPerLong, value, reverse);
        }

        //double, decimal

        #endregion

        #region GetBytes

        //ref..

        static byte[] GetBytes(Guid value, bool reverse = false)
        {
            byte[] result = value.ToByteArray();

            if (reverse)
            {
                Array.Reverse(result, 0, 4);

                Array.Reverse(result, 4, 2);

                Array.Reverse(result, 6, 2);
            }

            return result;
        }
        public static byte[] GetBytes(short i, bool reverse = false)
        {
            byte[] result = new byte[Binary.BytesPerShort];
            Write16(result, 0, reverse, i);
            return result;
        }

        public static byte[] GetBytes(int i, bool reverse = false)
        {
            byte[] result = new byte[Binary.BytesPerInteger];
            Write32(result, 0, reverse, i);
            return result;
        }

        public static byte[] GetBytes(long i, bool reverse = false)
        {
            byte[] result = new byte[Binary.BytesPerLong];
            Write64(result, 0, reverse, i);
            return result;
        }

        #endregion

        //Should have ConvertEndian(data, SourceOrder, DestOrder)

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
        public static byte MultiplyReverseU8_64(ref byte source)
        {
            return (byte)(((source * 0x80200802UL) & 0x0884422110UL) * 0x0101010101UL >> QuadrupleBitSize);
        }

        [CLSCompliant(false)]
        public static byte MultiplyReverseU8_32(ref byte source)
        {
            return (byte)(((source * 0x0802LU & 0x22110LU) | (source * 0x8020LU & 0x88440LU)) * 0x10101LU >> DoubleBitSize);
        }

        public static byte MultiplyReverseU8_64(byte source) { return MultiplyReverseU8_64(ref source); }

        public static byte MultiplyReverseU8_32(byte source) { return MultiplyReverseU8_32(ref source); }

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

//Move to seperate assembly
namespace Media.UnitTests
{
    /// <summary>
    /// Provides tests which ensure the logic of the Binary class is correct
    /// </summary>
    internal class BinaryUnitTests
    {
        public static void TestSignMinMaxAbs()
        {
            if (Binary.Sign(1) != Math.Sign(1)) throw new Exception();

            if (Binary.Sign(-0) != Math.Sign(-0)) throw new Exception();

            if (Binary.Abs(-1) != Math.Abs(-1)) throw new Exception();

            if (Binary.Abs(-0) != Math.Abs(-0)) throw new Exception();

            if (Binary.Min(0, 1) != Math.Min(0, 1)) throw new Exception();

            if (Binary.Max(0, 1) != Math.Max(0, 1)) throw new Exception();

            if (Binary.Min(long.MinValue, long.MaxValue) != Math.Min(long.MinValue, long.MaxValue)) throw new Exception();

            if (Binary.Min(int.MinValue, int.MaxValue) != Math.Min(int.MinValue, int.MaxValue)) throw new Exception();

            if (Binary.Min(uint.MinValue, uint.MaxValue) != Math.Min(uint.MinValue, uint.MaxValue)) throw new Exception();

            if (Binary.Min(ulong.MinValue, ulong.MaxValue) != Math.Min(ulong.MinValue, ulong.MaxValue)) throw new Exception();

            if (Binary.Max(long.MinValue, long.MaxValue) != Math.Max(long.MinValue, long.MaxValue)) throw new Exception();

            if (Binary.Max(int.MinValue, int.MaxValue) != Math.Max(int.MinValue, int.MaxValue)) throw new Exception();

            if (Binary.Max(uint.MinValue, uint.MaxValue) != Math.Max(uint.MinValue, uint.MaxValue)) throw new Exception();

            if (Binary.Max(ulong.MinValue, ulong.MaxValue) != Math.Max(ulong.MinValue, ulong.MaxValue)) throw new Exception();
        }

        public static void TestManualBitReversal()
        {

            //Test reversing 127 which should equal 254

            //127 = 0x7F = 01111111

            //254 = 0xFE = 11111110

            byte testBits = Media.Common.Binary.ReverseU8((byte)sbyte.MaxValue);

            if (testBits != 254 && Media.Common.Binary.ReverseU8(1) != 128) throw new Exception("Bit 0 Not Correct");

            //Get the MostSignificantBit and ensure it is set.

            if (Media.Common.Binary.GetBit(ref testBits, Media.Common.Binary.LeastSignificantBit) != true) throw new Exception("GetBit Does not Work");

            //Unset the MostSignificantBit and ensure it was set.

            if (Media.Common.Binary.SetBit(ref testBits, Media.Common.Binary.LeastSignificantBit, false) != true) throw new Exception("SetBit Does not Work");

            //testBits should now equal 126

            //126 = 0x7E = 01111110

            if (testBits != 126) throw new Exception("No idea what we did");

            //Get the MostSignificantBit and ensure it is not set.

            if (Media.Common.Binary.GetBit(ref testBits, Media.Common.Binary.LeastSignificantBit) != false) throw new Exception("GetBit Does not Work");

            //Get the LeastSignificantBit and ensure it is not set.

            if (Media.Common.Binary.GetBit(ref testBits, Media.Common.Binary.MostSignificantBit) != false) throw new Exception("GetBit Does not Work");

            //Set the LeastSignificantBit and ensure it was not set.

            if (Media.Common.Binary.SetBit(ref testBits, Media.Common.Binary.MostSignificantBit, true) != false) throw new Exception("SetBit Does not Work");

            //Get the LeastSignificantBit and ensure it is was set.

            if (Media.Common.Binary.GetBit(ref testBits, Media.Common.Binary.MostSignificantBit) != true) throw new Exception("GetBit Does not Work");

            //testBits should now equal 127

            //127 = 0x7F = 01111111

            if (testBits != sbyte.MaxValue) throw new Exception("No idea what we did");       
        }

        public static void TestReadingAndWritingUnsignedTypes()
        {
            //Use 8 octets, each write over-writes the previous written value
            byte[] Octets = new byte[Media.Common.Binary.BytesPerLong];

            //The register where the resulting value read during tests is stored.
            long result;

            //Test is binary, so test both ways, 0 and 1
            for (int i = 0; i < 2; ++i)
            {

                //Test:
                //First test uses system byte order, next test is logical reverse of that order.
                bool reverse = i > 0;

                //int size = 16,
                //    ops = (int)Math.Pow(2, 16);              

                #region 8 Bit Values

                //Test Bit Methods from 0 - 255 inclusive
                //[256] Operations
                for (int test = Octets[0]; test <= byte.MaxValue; Octets[0] = (byte)(++test))
                {
                    //Get the value being tested by casting from the test varible
                    byte testBits = (byte)test;

                    int bitsSet = 0, bitsNotSet = 0;

                    //Console.WriteLine("Bit Testing:" + testBits);

                    //Test each bit in the byte
                    //[8 Operations]
                    for (int b = 0; b < Media.Common.Binary.BitsPerByte; ++b)
                    {
                        if (Media.Common.Binary.GetBit(ref testBits, b)) ++bitsSet;
                        else ++bitsNotSet;
                    }

                    //Test the BitSetTable and verify the result
                    if (bitsSet != Media.Common.Binary.BitsSet(testBits)) throw new Exception("GetBit Does not Work");

                    //Test the logic of BitsUnSet and verify the result
                    if (bitsNotSet != Media.Common.Binary.BitsUnSet(testBits)) throw new Exception("GetBit Does not Work");

                    if (Media.Common.Binary.HammingWeight(test) != bitsSet) throw new Exception("HammingWeight Does not Work");

                    if (Media.Common.Binary.BitsPerByte - Media.Common.Binary.HammingWeight(test) != bitsNotSet) throw new Exception("HammingWeight Does not Work");

                    //Console.WriteLine("Bits Set:" + bitsSet);

                    //Console.WriteLine("Bits Not Set:" + bitsNotSet);

                    //Copy the bits and verify the result
                    if (Media.Common.Binary.CopyBits(Octets, Media.Common.Binary.BitsPerByte)[0] != Octets[0])
                        throw new Exception("CopyBits Does not Work");

                    //Copy the bits in reverse and verify the result
                    if (Media.Common.Binary.CopyBitsReverse(Octets, Media.Common.Binary.BitsPerByte)[0] != Media.Common.Binary.ReverseU8(ref Octets[0]))
                        throw new Exception("CopyBitsReverse Does not Work");

                    //Console.WriteLine("Bits:" + Convert.ToString((long)testBits, 2));

                    //Todo Test writing and parsing the same value
                }
                #endregion

                #region 16 Bit Values

                //65535 iterations uses 16 bits of a 32 bit integer
                for (ushort v = ushort.MinValue; v < ushort.MaxValue; ++v)
                {                    
                    //Write the 16 bit value
                    Media.Common.Binary.Write16(Octets, 0, reverse, v);

                    //Determine what the reverse byte order would look like
                    ushort reversed = Media.Common.Binary.ReverseUnsignedShort(ref v);

                    //Use the system to get the binary representation of the number
                    byte[] SystemBits = BitConverter.GetBytes(reverse ? reversed : v);

                    //Ensure the bytes are equal to what the system would create
                    if (false == SystemBits.SequenceEqual(Octets.Take(SystemBits.Length))) throw new Exception("WriteInteger->Write16 Does not work");

                    Common.Binary.WriteBitsMSB(Octets, 0, (reverse ? v : reversed), Media.Common.Binary.BitsPerShort);

                    if (false == SystemBits.SequenceEqual(Octets.Take(SystemBits.Length))) throw new Exception("WriteBitsMSB Does not work");

                    Common.Binary.WriteBitsLSB(Octets, 0, (reverse ? reversed : v), Media.Common.Binary.BitsPerShort);

                    if (false == SystemBits.SequenceEqual(Octets.Take(SystemBits.Length))) throw new Exception("WriteBitsLSB Does not work");

                    //Ensure the value read is equal to what the system would read
                    if (Media.Common.Binary.ReadBitsLSB(Octets, 0, Media.Common.Binary.BitsPerShort) != (reverse ? reversed : v)) throw new Exception("ReadBitsLSB Does not work.");

                    if (Media.Common.Binary.ReadBitsMSB(Octets, 0, Media.Common.Binary.BitsPerShort) != (reverse ? v : reversed)) throw new Exception("ReadBitsMSB Does not work.");

                    //Ensure the value read is equal to what the system would read
                    if (Media.Common.Binary.ReadInteger(Octets, 0, Media.Common.Binary.BytesPerShort, reverse) != v) throw new Exception("ReadInteger Does not work.");

                    //Print the bytes tested
                    //Console.WriteLine(BitConverter.ToString(Octets, 0, SystemBits.Length));

                    //Print the bits tested
                    //Console.WriteLine(Convert.ToString(v, 2));
                }

                #endregion

                #region 24 Bit Values

                //Repeat the test using each permutation of 24 bits not yet tested within the 3 octets which provide an integer of 24 bits
                ////for (uint s = ushort.MinValue, e = Media.Common.Binary.U24MaxValue; s <= e; ++s)
                ////{
                ////    uint v = ushort.MaxValue * s;

                ////    Media.Common.Binary.Write32(Octets, 0, reverse, v);

                ////    uint reversed = (uint)System.Net.IPAddress.HostToNetworkOrder((int)v);

                ////    if (reversed != Media.Common.Binary.ReverseUnsignedInt(ref v)) throw new Exception("ReverseUnsignedInt Does not Work");

                ////    byte[] SystemBits = BitConverter.GetBytes(reverse ? reversed : v);

                ////    if (false == SystemBits.SequenceEqual(Octets.Take(SystemBits.Length))) throw new Exception("Incorrect bits when compared to SystemBits");
                ////    else if (Media.Common.Binary.ReadInteger(Octets, 0, 4, reverse) != v) throw new Exception("Can't read back what was written");

                ////    if ((read = Media.Common.Binary.ReadBinaryInteger(Octets, 0, Media.Common.Binary.QuadrupleBitsPerByte)) != (reverse ? reversed : v))
                ////        throw new Exception("GetBit Does not Work");

                ////    //if ((read = Media.Common.Binary.ReadBinaryInteger(Octets, 0, Media.Common.Binary.TripleBitsPerByte, !reverse)) != reversed)
                ////        //throw new Exception("GetBit Does not Work");

                ////    if ((uint)Media.Common.Binary.ReadInteger(Media.Common.Binary.ReadBits(Octets, Media.Common.Binary.QuadrupleBitsPerByte), 0, 4, reverse) != v)
                ////        throw new Exception("ReadBits Does not Work");

                ////    Console.WriteLine(BitConverter.ToString(Octets, 0, SystemBits.Length));

                ////    Console.WriteLine(Convert.ToString(v, 2));
                ////}

                #endregion

                #region 32 Bit Values

                //Repeat the test using each permutation of 16 bits not yet tested within the 4 octets which provide an integer of 32 bits
                for (uint s = ushort.MinValue, e = ushort.MaxValue; s <= e; ++s)
                {
                    //Create a 32 bit value from the 16 bit value
                    uint v = s << Media.Common.Binary.BitsPerShort | s;

                    //Write it
                    Media.Common.Binary.Write32(Octets, 0, reverse, v);

                    //Determine what the reverse byte order would look like
                    uint reversed = Media.Common.Binary.ReverseUnsignedInt(ref v);

                    //Use the system to get the binary representation of the number
                    byte[] SystemBits = BitConverter.GetBytes(reverse ? reversed : v);

                    //Ensure the bytes are equal to what the system would create
                    if (false == SystemBits.SequenceEqual(Octets.Take(SystemBits.Length))) throw new Exception("WriteInteger->Write32 Does not work");

                    Common.Binary.WriteBitsMSB(Octets, 0, (reverse ? v : reversed), Media.Common.Binary.BitsPerInteger);

                    if (false == SystemBits.SequenceEqual(Octets.Take(SystemBits.Length))) throw new Exception("WriteBitsMSB Does not work");

                    Common.Binary.WriteBitsLSB(Octets, 0, (reverse ? reversed : v), Media.Common.Binary.BitsPerInteger);

                    if (false == SystemBits.SequenceEqual(Octets.Take(SystemBits.Length))) throw new Exception("WriteBitsLSB Does not work");

                    //Ensure the value read is equal to what the system would read
                    if (Media.Common.Binary.ReadBitsLSB(Octets, 0, Media.Common.Binary.BitsPerInteger) != (reverse ? reversed : v)) throw new Exception("ReadBitsLSB Does not work.");

                    //Ensure the value read is equal to what the system would read
                    if (Media.Common.Binary.ReadBitsMSB(Octets, 0, Media.Common.Binary.BitsPerInteger) != (reverse ? v : reversed)) throw new Exception("ReadBitsMSB Does not work.");

                    //Ensure the value read is equal to what the system would read
                    if (Media.Common.Binary.ReadInteger(Octets, 0, Media.Common.Binary.BytesPerInteger, reverse) != v) throw new Exception("ReadInteger Does not work.");

                    //Print the bytes tested
                    //Console.WriteLine(BitConverter.ToString(Octets, 0, SystemBits.Length));

                    //Print the bits tested
                    //Console.WriteLine(Convert.ToString(v, 2));
                }

                #endregion

                #region 64 Bit Values

                //Repeat the test using each permuation of 16 bits within the 8 octets which provide an integer of 64 bits.
                for (uint s = ushort.MinValue, e = ushort.MaxValue; s <= e; ++s)
                {
                    //Create a 32 bit value from the 16 bit value
                    ulong v = s << Media.Common.Binary.BitsPerShort | s;

                    //Create a 64 bit value from the 32 bit value by duplicating the 32 bits already set
                    v |= v << Media.Common.Binary.BitsPerInteger;

                    //Write it
                    Media.Common.Binary.Write64(Octets, 0, reverse, v);

                    //Determine what the reverse byte order would look like
                    ulong reversed = Media.Common.Binary.ReverseUnsignedLong(ref v);

                    //Use the system to get the binary representation of the number
                    byte[] SystemBits = BitConverter.GetBytes(reverse ? reversed : v);

                    //Ensure the bytes are equal to what the system would create
                    if (false == SystemBits.SequenceEqual(Octets.Take(SystemBits.Length))) throw new Exception("WriteInteger->Write64 Does not work");

                    Common.Binary.WriteBitsMSB(Octets, 0, (reverse ? v : reversed), Media.Common.Binary.BitsPerLong);

                    if (false == SystemBits.SequenceEqual(Octets.Take(SystemBits.Length))) throw new Exception("WriteBitsMSB Does not work");

                    Common.Binary.WriteBitsLSB(Octets, 0, (reverse ? reversed : v), Media.Common.Binary.BitsPerLong);

                    if (false == SystemBits.SequenceEqual(Octets.Take(SystemBits.Length))) throw new Exception("WriteBitsLSB Does not work");

                    //Ensure the value read is equal to what the system would read
                    if (Media.Common.Binary.ReadBitsLSB(Octets, 0, Media.Common.Binary.BitsPerLong) != (reverse ? reversed : v)) throw new Exception("ReadBitsLSB Does not work.");

                    //Ensure the value read is equal to what the system would read
                    if (Media.Common.Binary.ReadBitsMSB(Octets, 0, Media.Common.Binary.BitsPerLong) != (reverse ? v : reversed)) throw new Exception("ReadBitsMSB Does not work.");

                    //Ensure the value read is equal to what the system would read
                    if ((ulong)Media.Common.Binary.ReadInteger(Octets, 0, Media.Common.Binary.BytesPerLong, reverse) != v) throw new Exception("ReadInteger Does not work.");

                    //Console.WriteLine(BitConverter.ToString(Octets, 0, SystemBits.Length));

                    //Console.WriteLine(Convert.ToString((long)v, 2));
                }

                #endregion

                #region Without a For

                ////Do it again in reverse (without a for)
                //if (reverse)
                //{
                //    reverse = false;

                //    goto Test;
                //}

                #endregion
            }
        }

        //Consider adding the Conversion type eventually
        public static void TestConversions()
        {
            //Iterate 65536 times
            for (int i = 0; i <= ushort.MaxValue; ++i)
            {
                int inBits = i, 
                    inBytes = Binary.BitsToBytes(i),
                    inWords = Binary.BytesToMachineWords(inBytes);

                if (Binary.BitsToBytes(inBits) != inBytes) throw new Exception("BitsToBytes Unexpected Result");

                if (Binary.BytesToBits(inBytes) != inBytes * Binary.BitsPerByte) throw new Exception("BytesToBits Unexpected Result");

                if (Binary.BytesToMachineWords(inBytes) != inWords) throw new Exception("BytesToMachineWords Unexpected Result");
                 
                int bytesInWords = Binary.MachineWordsToBytes(inWords);

                if (bytesInWords > Binary.BytesPerInteger * inBytes || 
                    bytesInWords % Binary.BytesPerInteger != 0) throw new Exception("MachineWordsToBytes Unexpected Result");
            }
        }

        public static void TestGCD()
        {
            for (int i = 0; i <= ushort.MaxValue; ++i)
            {
                var left = Common.Binary.GreatestCommonDivisor(i, i + 1);

                var right = Common.Extensions.Math.MathExtensions.GreatestCommonDivisor(i, i + 1);

                if (left != right) throw new Exception("Invalid result!");
            }
        }
    }
}