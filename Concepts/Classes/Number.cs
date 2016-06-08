using System;
using System.Linq;
/*
Copyright (c) 2013 juliusfriedman@gmail.com
  
 SR. Software Engineer ASTI Transportation Inc.

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

namespace Media.Concepts.Classes
{
    /// <summary>
    /// Caches of commonly used types
    /// </summary>
    public sealed class Types
    {

        public static Type Byte = typeof(byte);

        public static Type UShort = typeof(ushort);

        public static Type UInt = typeof(uint);

        public static Type ULong = typeof(ulong);

        public static Type SByte = typeof(sbyte);

        public static Type Short = typeof(short);

        public static Type Int = typeof(int);

        public static Type Long = typeof(long);

        public static Type Double = typeof(double);

        public static Type Float = typeof(float);

        public static Type Decimal = typeof(decimal);

        public static Type Complex = typeof(System.Numerics.Complex);

        public static Type BigInteger = typeof(System.Numerics.BigInteger);

        static Types() { }
    }

    /// <summary>
    /// Interface which allows Hardware access
    /// </summary>
    public interface IHardware
    {

    }

    #region IProcessor

    public interface IProcessor
    {
        bool IsNull(ref Bitable a);

        bool IsNullOrZero(ref Bitable a);

        void Swap(ref Bitable a, ref Bitable b);

        void ShiftLeft(ref Bitable a, int amount);

        void ShiftRight(ref Bitable a, int amount);

        void RotateLeft(ref Bitable a, int amount);

        void RotateRight(ref Bitable a, int amount);

        bool LessThan(ref Bitable a, ref Bitable b);

        bool GreaterThan(ref Bitable a, ref Bitable b);
    }

    #endregion

    #region Processor

    //Todo abstract, use CPUIDFeatures etc.

    /// <summary>
    /// Represents the Processor itself.. 
    /// </summary>
    public abstract class Processor : IProcessor
    {
        const ulong Reverse1 = 0x0202020202UL, Reverse2 = 0x010884422010UL;
        const int K = 1024,
            J = K - 1,
            Z = 0;

        public delegate Bitable Manipulation(Bitable a, Bitable b);

        public delegate void ReferenceManipulation(ref Bitable a, ref Bitable b);

        public delegate bool Evaluation(Bitable a, Bitable b);

        public delegate bool ReferenceEvaluation(ref Bitable a, ref Bitable b);

        public delegate void Operation(Bitable a, Bitable b);

        public delegate void ReferenceOperation(ref Bitable a, ref Bitable b);

        public static Common.Binary.ByteOrder SystemEndian = Common.Binary.IsLittleEndian ? Common.Binary.ByteOrder.Little : Common.Binary.ByteOrder.Big;

        public readonly IHardware Hardware;

        public static void Prepare(ref Bitable a, ref Bitable b)
        {
            //
        }

        public static void Swap(ref Bitable a, ref Bitable b, bool useSwap = true)
        {
            if (useSwap)
            {
                Bitable temp = b;
                b = a;
                a = temp;
            }
            else //Becareful of null
            {
                a ^= b;
                b ^= a;
                a ^= b;
            }
        }

        public static void ReverseByte(ref byte b) { b = (byte)((b * Reverse1 & Reverse2) % J); }

        public static bool IsNull(Bitable a) { return IsNull(ref a); }

        public static bool IsNull(ref Bitable a) { return a.Memory == null || a.Count == Z; }

        public static bool IsNullOrZero(ref Bitable a) { return IsNull(ref a) || a.Memory.AsParallel().Sum(b => b) > Z; }

        //public static Bitable Manipulate(Bitable a, Bitable b, Manipulation manipulation)
        //{
        //    return manipulation(a, b);
        //}

        //public static void Manipulate(ref Bitable a, ref Bitable b, ReferenceManipulation referenceManipulation)
        //{
        //    referenceManipulation(ref a, ref b);
        //}

        //Todo flags, efalgs.

        public static void Addition(ref Bitable a, ref Bitable b)
        {
            for (int i = 0, e = Math.Min(a.Count, b.Count); i < e; ++i)
                a[i] += b[i];
        }

        public static Bitable Add(Bitable a, Bitable b)
        {
            if (IsNullOrZero(ref a)) return b;
            else if (IsNullOrZero(ref b)) return a;
            else
            {
                Bitable result = new Bitable(a);
                Addition(ref result, ref b);
                return result;
            }
        }

        public static void Subtraction(ref Bitable a, ref Bitable b)
        {
            for (int i = 0, e = Math.Min(a.Count, b.Count); i < e; ++i)
                a[i] -= b[i];
        }

        public static Bitable Subtract(Bitable a, Bitable b)
        {
            if (IsNullOrZero(ref a)) return b;
            else if (IsNullOrZero(ref b)) return a;
            else
            {
                Bitable result = new Bitable(a);
                Subtraction(ref result, ref b);
                return result;
            }
        }

        public static void Multiplication(ref Bitable a, ref Bitable b)
        {
            for (int i = 0, e = Math.Min(a.Count, b.Count); i < e; ++i)
                a[i] *= b[i];
        }

        public static Bitable Multiply(Bitable a, Bitable b)
        {
            if (IsNullOrZero(ref a)) return b;
            else if (IsNullOrZero(ref b)) return a;
            else
            {
                Bitable result = new Bitable(a);
                Multiplication(ref result, ref b);
                return result;
            }
        }

        public static void Division(ref Bitable a, ref Bitable b)
        {
            for (int i = 0, e = Math.Min(a.Count, b.Count); i < e; ++i)
                a[i] /= b[i];
        }

        public static Bitable Divide(Bitable a, Bitable b)
        {
            if (IsNullOrZero(ref a)) return b;
            else if (IsNullOrZero(ref b)) return a;
            else
            {
                Bitable result = new Bitable(a);
                Division(ref result, ref b);
                return result;
            }
        }

        public static void Modulo(ref Bitable a, ref Bitable b)
        {
            for (int i = 0, e = Math.Min(a.Count, b.Count); i < e; ++i)
                a[i] %= b[i];
        }

        public static Bitable Modulus(Bitable a, Bitable b)
        {
            if (IsNullOrZero(ref a)) return b;
            else if (IsNullOrZero(ref b)) return a;
            else
            {
                Bitable result = new Bitable(a);
                Modulo(ref result, ref b);
                return result;
            }
        }

        public static bool GreaterThan(ref Bitable a, ref Bitable b)
        {
            return (a != b) && a.Memory.Sum(i => i) > b.Memory.Sum(i => i);
        }

        public static bool GreaterThan(Bitable a, Bitable b)
        {
            return GreaterThan(ref a, ref b);
        }

        public static bool LessThan(ref Bitable a, ref Bitable b)
        {
            return a < b;
        }

        public static bool LessThan(Bitable a, Bitable b)
        {
            return LessThan(ref a, ref b);
        }

        public static void OR(ref Bitable a, ref Bitable b)
        {
            for (int i = 0, e = Math.Min(a.Count, b.Count); i < e; ++i)
                a[i] |= b[i];
        }

        public static Bitable OR(Bitable a, Bitable b)
        {
            if (IsNullOrZero(ref a)) return b;
            else if (IsNullOrZero(ref b)) return a;
            else
            {
                Bitable result = new Bitable(a);
                OR(ref result, ref b);
                return result;
            }
        }

        public static void XOR(ref Bitable a, ref Bitable b)
        {
            for (int i = 0, e = Math.Min(a.Count, b.Count); i < e; ++i)
                a[i] ^= b[i];
        }

        public static Bitable XOR(Bitable a, Bitable b)
        {
            if (IsNullOrZero(ref a)) return b;
            else if (IsNullOrZero(ref b)) return a;
            else
            {
                Bitable result = new Bitable(a);
                XOR(ref result, ref b);
                return result;
            }
        }

        public static void AND(ref Bitable a, ref Bitable b)
        {
            for (int i = 0, e = Math.Min(a.Count, b.Count); i < e; ++i)
                a[i] &= b[i];
        }

        public static Bitable AND(Bitable a, Bitable b)
        {
            if (IsNullOrZero(ref a)) return b;
            else if (IsNullOrZero(ref b)) return a;
            else
            {
                Bitable result = new Bitable(a);
                AND(ref result, ref b);
                return result;
            }
        }

        public static void NOT(ref Bitable a, ref Bitable b)
        {
            for (int i = 0, e = Math.Min(a.Count, b.Count); i < e; ++i)
                a[i] = (byte)~b[i];
        }

        public static Bitable NOT(Bitable a, Bitable b)
        {
            if (IsNullOrZero(ref a)) return b;
            else if (IsNullOrZero(ref b)) return a;
            else
            {
                Bitable result = new Bitable(a);
                NOT(ref result, ref b);
                return result;
            }
        }

        public static void NAND(ref Bitable a, ref Bitable b)
        {
            for (int i = 0, e = Math.Min(a.Count, b.Count); i < e; ++i)
                a[i] &= (byte)(~b[i]);
        }

        public static Bitable NAND(Bitable a, Bitable b)
        {
            if (IsNullOrZero(ref a)) return b;
            else if (IsNullOrZero(ref b)) return a;
            else
            {
                Bitable result = new Bitable(a);
                NAND(ref result, ref b);
                return result;
            }
        }

        public static void NOR(ref Bitable a, ref Bitable b)
        {
            for (int i = 0, e = Math.Min(a.Count, b.Count); i < e; ++i)
                a[i] |= (byte)(~b[i]);
        }

        public static Bitable NOR(Bitable a, Bitable b)
        {
            if (IsNullOrZero(ref a)) return b;
            else if (IsNullOrZero(ref b)) return a;
            else
            {
                Bitable result = new Bitable(a);
                NOR(ref result, ref b);
                return result;
            }
        }

        //Todo, should take Shift class instance

        public static void ShiftLeft(ref Bitable a, ref int amount, ref int index)
        {
            for (int i = index, e = a.Count; i < e; ++i)
                a[i] <<= amount;
        }

        public static Bitable ShiftLeft(Bitable a, int amount, int index = 0)
        {
            if (IsNullOrZero(ref a)) return a;
            else
            {
                Bitable result = new Bitable(a);
                ShiftLeft(ref result, ref amount, ref index);
                return result;
            }
        }

        public static void ShiftRight(ref Bitable a, ref int amount, ref int index)
        {
            for (int i = index, e = a.Count; i < e; ++i)
                a[i] >>= amount;
        }

        public static Bitable ShiftRight(Bitable a, int amount, int index = 0)
        {
            if (IsNullOrZero(ref a)) return a;
            else
            {
                Bitable result = new Bitable(a);
                ShiftRight(ref result, ref amount, ref index);
                return result;
            }
        }

        public static void RotateRight(ref Bitable a, ref int amount, ref int index)
        {
            uint ofA = a.ToUInt32();
            ofA = ((ofA << (~amount)) << Bits.Size) | (ofA >> amount);
            a = ofA;
        }

        //Todo, make a RotateShift if I didn't already.

        public static Bitable RotateRight(Bitable a, int amount, int index = 0)
        {
            if (IsNullOrZero(ref a)) return a;
            else
            {
                Bitable result = new Bitable(a);
                RotateRight(ref result, ref amount, ref index);
                return result;
            }
        }

        public static void RotateLeft(ref Bitable a, ref int amount, ref int index)
        {
            uint ofA = a.ToUInt32();
            ofA = ((ofA << (~amount)) >> Bits.Size) | (ofA << amount);
            a = ofA;
        }

        public static Bitable RotateLeft(Bitable a, int amount, int index = 0)
        {
            if (IsNullOrZero(ref a)) return a;
            else
            {
                Bitable result = new Bitable(a);
                RotateLeft(ref result, ref amount, ref index);
                return result;
            }
        }

        bool IProcessor.IsNull(ref Bitable a)
        {
            return IsNull(ref a);
        }

        bool IProcessor.IsNullOrZero(ref Bitable a)
        {
            return IsNullOrZero(ref a);
        }

        void IProcessor.Swap(ref Bitable a, ref Bitable b)
        {
            Swap(ref a, ref b);
        }

        void IProcessor.ShiftLeft(ref Bitable a, int amount)
        {
            int index = Z;
            ShiftLeft(ref a, ref amount, ref index);
        }

        void IProcessor.ShiftRight(ref Bitable a, int amount)
        {
            int index = Z;
            ShiftRight(ref a, ref amount, ref index);
        }

        void IProcessor.RotateLeft(ref Bitable a, int amount)
        {
            int index = Z;
            ShiftRight(ref a, ref amount, ref index);
        }

        void IProcessor.RotateRight(ref Bitable a, int amount)
        {
            int index = Z;
            ShiftRight(ref a, ref amount, ref index);
        }

        bool IProcessor.LessThan(ref Bitable a, ref Bitable b)
        {
            return LessThan(ref a, ref b);
        }

        bool IProcessor.GreaterThan(ref Bitable a, ref Bitable b)
        {
            return GreaterThan(ref a, ref b);
        }
    }

    #endregion

    #region IMathProvider

    /// <summary>
    /// Represents a provider of math functions
    /// </summary>
    public interface IMathProvider
    {
        Radix Base { get; }

        IProcessor Processor { get; }

        Number Addition(ref Number a, ref Number b);

        Number Subtraction(ref Number a, ref Number b);

        Number Multiplication(ref Number a, ref Number b);

        Number Division(ref Number a, ref Number b);

        Number Modulus(ref Number a, ref Number b);

        bool GreaterThan(ref Number a, ref Number b);

        bool LessThan(ref Number a, ref Number b);

        bool Equals(ref Number a, ref Number b);

        Number Min(ref Number a, ref Number b);

        Number Max(ref Number a, ref Number b);
    }

    #endregion

    #region MathProvider

    /// <summary>
    /// The base class of all number complimentors.
    /// Retrieves Numbers from a Bitables Memory
    /// </summary>
    public abstract class MathProvider : IMathProvider
    {
        public abstract Radix Radix { get; }

        public abstract IProcessor Processor { get; }

        Radix IMathProvider.Base
        {
            get { return Radix; }
        }

        IProcessor IMathProvider.Processor
        {
            get { return Processor; }
        }


        public abstract Number Addition(ref Number a, ref Number b);

        public abstract Number Subtraction(ref Number a, ref Number b);

        public abstract Number Modulus(ref Number a, ref Number b);

        public abstract Number Multiplication(ref Number a, ref Number b);

        public abstract Number Division(ref Number a, ref Number b);

        public abstract bool GreaterThan(ref Number a, ref Number b);

        public abstract bool LessThan(ref Number a, ref Number b);

        public abstract Number Min(ref Number a, ref Number b);

        public abstract Number Max(ref Number a, ref Number b);

        public abstract bool Equals(ref Number a, ref Number b);
    }

    #endregion

    #region CLRMathProvider

    //Todo, Enhance speed.
    //Loyc has quite a good start on this
    //https://github.com/qwertie/ecsharp/blob/master/Core/Loyc.Essentials/Math/Math128.cs
    //https://github.com/qwertie/ecsharp/blob/master/Core/Loyc.Essentials/Math/MathEx.cs

    /// <summary>
    /// A basic two's complementor
    /// </summary>
    public class CLRMathProvider : MathProvider
    {
        //Numbers are stored in Twos Complement or Base 2 (0 and 1)
        const int Base = 2;

        public readonly static Radix DecimalBase = new Radix(Base);

        public override IProcessor Processor
        {
            get { return Processor; }
        }

        public override Radix Radix
        {
            get { return DecimalBase; }
        }

        #region Statics

        public bool IsNull(Bitable a) { return Processor.IsNull(ref a); }

        public bool IsNull(ref Bitable a) { return Processor.IsNull(ref a); }

        public bool IsZero(Bitable a) { return System.Linq.Enumerable.All(a.Memory, (b => b == 0)); }

        public bool IsNullOrZero(ref Number a) { return IsNull(a) || IsZero(a); }

        #endregion

        public override Number Addition(ref Number a, ref Number b)
        {
            switch (a.TypeCode)
            {
                case System.TypeCode.SByte: return new Number((a.ToSByte() + b.ToSByte()));
                case System.TypeCode.Byte: return new Number((a.ToByte() + b.ToByte()));
                case System.TypeCode.Int16: return new Number((a.ToInt16() + b.ToInt16()));
                case System.TypeCode.UInt16: return new Number((a.ToUInt16() + b.ToUInt16()));
                case System.TypeCode.Int32: return new Number((a.ToInt32() + b.ToInt32()));
                case System.TypeCode.UInt32: return new Number((a.ToUInt32() + b.ToUInt32()));
                case System.TypeCode.Int64: return new Number((a.ToInt64() + b.ToInt64()));
                case System.TypeCode.UInt64: return new Number((a.ToUInt64() + b.ToUInt64()));
                case System.TypeCode.Decimal: return new Number((a.ToDecimal() + b.ToDecimal()));
                case System.TypeCode.Double: return new Number((a.ToDouble() + b.ToDouble()));
                default: return (Number)Bitable.DoubleZero;
            }
        }

        public override Number Subtraction(ref Number a, ref Number b)
        {
            switch (a.TypeCode)
            {
                case System.TypeCode.SByte: return new Number((a.ToSByte() - b.ToSByte()));
                case System.TypeCode.Byte: return new Number((a.ToByte() - b.ToByte()));
                case System.TypeCode.Int16: return new Number((a.ToInt16() - b.ToInt16()));
                case System.TypeCode.UInt16: return new Number((a.ToUInt16() - b.ToUInt16()));
                case System.TypeCode.Int32: return new Number((a.ToInt32() - b.ToInt32()));
                case System.TypeCode.UInt32: return new Number((a.ToUInt32() - b.ToUInt32()));
                case System.TypeCode.Int64: return new Number((a.ToInt64() - b.ToInt64()));
                case System.TypeCode.UInt64: return new Number((a.ToUInt64() - b.ToUInt64()));
                case System.TypeCode.Decimal: return new Number((a.ToDecimal() - b.ToDecimal()));
                case System.TypeCode.Double: return new Number((a.ToDouble() - b.ToDouble()));
                default: return (Number)Bitable.DoubleZero;
            }
        }

        public override Number Multiplication(ref Number a, ref Number b)
        {
            switch (a.TypeCode)
            {
                case System.TypeCode.SByte: return new Number((a.ToSByte() * b.ToSByte()));
                case System.TypeCode.Byte: return new Number((a.ToByte() * b.ToByte()));
                case System.TypeCode.Int16: return new Number((a.ToInt16() * b.ToInt16()));
                case System.TypeCode.UInt16: return new Number((a.ToUInt16() * b.ToUInt16()));
                case System.TypeCode.Int32: return new Number((a.ToInt32() * b.ToInt32()));
                case System.TypeCode.UInt32: return new Number((a.ToUInt32() * b.ToUInt32()));
                case System.TypeCode.Int64: return new Number((a.ToInt64() * b.ToInt64()));
                case System.TypeCode.UInt64: return new Number((a.ToUInt64() * b.ToUInt64()));
                case System.TypeCode.Decimal: return new Number((a.ToDecimal() * b.ToDecimal()));
                case System.TypeCode.Double: return new Number((a.ToDouble() * b.ToDouble()));
                default: return (Number)Bitable.DoubleZero;
            }
        }

        public override Number Division(ref Number a, ref Number b)
        {
            switch (a.TypeCode)
            {
                case System.TypeCode.SByte: return new Number((a.ToSByte() / b.ToSByte()));
                case System.TypeCode.Byte: return new Number((a.ToByte() / b.ToByte()));
                case System.TypeCode.Int16: return new Number((a.ToInt16() / b.ToInt16()));
                case System.TypeCode.UInt16: return new Number((a.ToUInt16() / b.ToUInt16()));
                case System.TypeCode.Int32: return new Number((a.ToInt32() / b.ToInt32()));
                case System.TypeCode.UInt32: return new Number((a.ToUInt32() / b.ToUInt32()));
                case System.TypeCode.Int64: return new Number((a.ToInt64() / b.ToInt64()));
                case System.TypeCode.UInt64: return new Number((a.ToUInt64() / b.ToUInt64()));
                case System.TypeCode.Decimal: return new Number((a.ToDecimal() / b.ToDecimal()));
                case System.TypeCode.Double: return new Number((a.ToDouble() / b.ToDouble()));
                default: return (Number)Bitable.DoubleZero;
            }
        }

        public override Number Modulus(ref Number a, ref Number b)
        {
            switch (a.TypeCode)
            {
                case System.TypeCode.SByte: return new Number((a.ToSByte() % b.ToSByte()));
                case System.TypeCode.Byte: return new Number((a.ToSByte() % b.ToByte()));
                case System.TypeCode.Int16: return new Number((a.ToSByte() % b.ToInt16()));
                case System.TypeCode.UInt16: return new Number((a.ToSByte() % b.ToUInt16()));
                case System.TypeCode.Int32: return new Number((a.ToSByte() % b.ToInt32()));
                case System.TypeCode.UInt32: return new Number((a.ToSByte() % b.ToUInt32()));
                case System.TypeCode.Int64: return new Number((a.ToSByte() % b.ToInt64()));
                case System.TypeCode.UInt64: return new Number((a.ToByte() % b.ToUInt64()));
                case System.TypeCode.Decimal: return new Number((a.ToSByte() % b.ToDecimal()));
                case System.TypeCode.Double: return new Number((a.ToSByte() % b.ToDouble()));
                default: return (Number)Bitable.DoubleZero;
            }
        }

        public override bool GreaterThan(ref Number a, ref Number b)
        {
            Bitable x = a, y = b;
            return Processor.GreaterThan(ref x, ref y);
        }

        public override bool LessThan(ref Number a, ref Number b)
        {
            Bitable x = a, y = b;
            return Processor.LessThan(ref x, ref y);
        }

        public override Number Min(ref Number a, ref Number b)
        {
            return a > b ? a : b;
        }

        public override Number Max(ref Number a, ref Number b)
        {
            return a > b ? b : a;
        }

        public override bool Equals(ref Number a, ref Number b)
        {
            return a.Memory.Equals(b.Memory);
        }
    }

    #endregion

    #region Radix

    /// <summary>
    /// http://en.wikipedia.org/wiki/Radix
    /// In mathematical numeral systems, the radix or base is the number of unique digits, including zero, that a positional numeral system uses to represent numbers. 
    /// For example, for the decimal system (the most common system in use today) the radix is ten, because it uses the ten digits from 0 through 9.
    /// </summary>
    public class Radix
    {
        public Radix(Number b)
        {
            Base = b;
        }

        public readonly Number Base;
    }

    #endregion

    #region Bits

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Explicit,
        Size = 1,//This field must be equal or greater than the total size, in bytes, of the members of the class or structure.
        Pack = 0, //A value of 0 indicates that the packing alignment is set to the default for the current platform.
        CharSet = System.Runtime.InteropServices.CharSet.Auto)]
    public struct Bits : 
        System.Collections.IEnumerable, 
        System.Collections.Generic.IEnumerable<bool>, 
        System.Collections.Generic.IEnumerable<Byte>
    {
        /// <summary>
        /// The size of the structure in bytes
        /// </summary>
        public const int Size = 1;

        public const int BitSize = Common.Binary.BitsPerByte;

        [System.Runtime.InteropServices.FieldOffset(0)]
        internal byte m_Bits;

        unsafe byte* UnsafeBits
        {
            get
            {
                fixed (byte* memory = &m_Bits)
                {
                    return memory;
                }
            }
        }

        public byte ManagedBits { get { return m_Bits; } }

        public Bits(byte b) { m_Bits = b; }

        unsafe internal Bits(byte* b) : this(*b) { }

        public override bool Equals(object obj)
        {
            if (obj is Bits)
            {
                Bits unboxed = (Bits)obj;
                if (unboxed == null) return false;
                return unboxed == this;
            }
            else return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return m_Bits.GetHashCode();
        }

        public bool this[int index]
        {
            get { return GetBit(this, index); }
            set { SetBit(this, index); }
        }

        public static bool GetBit(Bits b, int index)
        {
            return (b.m_Bits & (Size << index)) > 0;
        }

        public static void SetBit(Bits b, int index)
        {
            b |= (Size << index);
        }

        public static void ClearBit(Bits b, int index)
        {
            b &= ~(Size << index);
        }

        public static void ToggleBit(Bits b, int index)
        {
            b ^= (byte)(Size << index);
        }

        public static implicit operator byte(Bits b) { return b.m_Bits; }

        public static implicit operator int(Bits b) { return b.m_Bits; }

        public static implicit operator Bits(byte b) { return new Bits(b); }

        public static Bits operator ^(Bits b, int amount)
        {
            return new Bits((byte)(b.m_Bits ^ amount));
        }

        public static Bits operator |(Bits b, int amount)
        {
            return new Bits((byte)(b.m_Bits | amount));
        }

        public static Bits operator &(Bits b, int amount)
        {
            return new Bits((byte)(b.m_Bits & amount));
        }

        public static bool operator >(Bits b, int amount)
        {
            return b.m_Bits > amount;
        }

        public static bool operator <(Bits b, int amount)
        {
            return b.m_Bits > amount;
        }

        public static Bits operator >>(Bits b, int amount)
        {
            return new Bits((byte)(b.m_Bits >> amount));
        }

        public static Bits operator <<(Bits b, int amount)
        {
            return new Bits((byte)(b.m_Bits << amount));
        }

        public static Bits operator +(Bits b, int amount)
        {
            return new Bits((byte)(b.m_Bits + amount));
        }

        public static Bits operator -(Bits b, int amount)
        {
            return new Bits((byte)(b.m_Bits - amount));
        }

        public static Bits operator *(Bits b, int amount)
        {
            return new Bits((byte)(b.m_Bits * amount));
        }

        public static Bits operator /(Bits b, int amount)
        {
            return new Bits((byte)(b.m_Bits / amount));
        }

        //

        public static Bits operator ^(Bits b, Bits other)
        {
            return new Bits((byte)(b.m_Bits ^ other.m_Bits));
        }

        public static Bits operator |(Bits b, Bits other)
        {
            return new Bits((byte)(b.m_Bits | other.m_Bits));
        }

        public static Bits operator &(Bits b, Bits other)
        {
            return new Bits((byte)(b.m_Bits & other.m_Bits));
        }

        public static bool operator >(Bits b, Bits other)
        {
            return b.m_Bits > other.m_Bits;
        }

        public static bool operator <(Bits b, Bits other)
        {
            return b.m_Bits > other.m_Bits;
        }

        public static Bits operator +(Bits b, Bits other)
        {
            return new Bits((byte)(b.m_Bits + other.m_Bits));
        }

        public static Bits operator -(Bits b, Bits other)
        {
            return new Bits((byte)(b.m_Bits - other.m_Bits));
        }

        public static Bits operator *(Bits b, Bits other)
        {
            return new Bits((byte)(b.m_Bits * other.m_Bits));
        }

        public static Bits operator /(Bits b, Bits other)
        {
            return new Bits((byte)(b.m_Bits / other.m_Bits));
        }

        //


        public static bool operator ==(Bits b, int a) { return a.Equals(b.m_Bits); }

        public static bool operator !=(Bits b, int a) { return false.Equals((a.Equals(b.m_Bits))); }

        public static bool operator ==(Bits b, byte a) { return a.Equals(b.m_Bits); }

        public static bool operator !=(Bits b, byte a) { return false.Equals((a.Equals(b.m_Bits))); }

        public static bool operator ==(Bits b, Bits a)
        {
            return a.m_Bits.Equals(b.m_Bits);
        }

        public static bool operator !=(Bits b, Bits a)
        {
            return false.Equals((a.m_Bits.Equals(b.m_Bits)));
        }

        public System.Collections.IEnumerator GetEnumerator()
        {
            return new Bitable.BitsEnumerator(this);
        }

        System.Collections.Generic.IEnumerator<bool> System.Collections.Generic.IEnumerable<bool>.GetEnumerator()
        {
            return new Bitable.BitsEnumerator(this);
        }

        internal void Reset()
        {
            m_Bits = default(byte);
        }

        System.Collections.Generic.IEnumerator<byte> System.Collections.Generic.IEnumerable<byte>.GetEnumerator()
        {
            yield return m_Bits;
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    #endregion      
   
    #region Bitable

    public class Bitable :
        System.Collections.Generic.IEnumerable<Bits>, 
        System.Collections.Generic.IEnumerable<byte>, 
        System.Collections.Generic.IEnumerable<bool>
    {
        #region Statics

        public const int SizeOfDouble = sizeof(double);

        public readonly static Bitable SingleZero = new Bitable((Single)0);

        public readonly static Bitable ByteZero = new Bitable((byte)0);

        public readonly static Bitable ShortZero = new Bitable((short)0);

        public readonly static Bitable IntZero = new Bitable(0);

        public readonly static Bitable LongZero = new Bitable(0L);

        public readonly static Bitable DoubleZero = new Bitable(0.0D);

        public readonly static Bitable DecimalZero = new Bitable(0.0M);

        public readonly static Bitable Null = new Bitable(Processor.SystemEndian);

        #endregion

        #region Properties

        unsafe byte* UnmanagedBytes
        {
            get
            {
                fixed (byte* memory = Memory)
                {
                    return memory;
                }
            }
        }

        public Common.Binary.ByteOrder Endian { get; protected set; }

        public int Count { get { return Memory.Length; } }

        public int BitCount { get { return Count << 3; } } //* 8

        public byte this[int index]
        {
            get
            {
                return Memory[index];
            }
            set
            {
                Memory[index] = value;
            }
        }

        #endregion

        #region Fields

        /// <summary>
        /// Todo, make MemorySegment
        /// </summary>
        internal byte[] Memory;

        #endregion

        #region Constructors

        internal Bitable(__arglist)
        {
            ArgIterator args = new ArgIterator(__arglist);

            int count = args.GetRemainingCount();

            while (count > 0)
            {
                --count;
            }
        }

        unsafe internal Bitable(byte* pointer)
        {

        }

        internal Bitable(ref byte[] memory, Common.Binary.ByteOrder? endian = Common.Binary.ByteOrder.Unknown)
        {
            Endian = endian.Value;
            Memory = memory;
        }

        internal Bitable(byte[] memory, Common.Binary.ByteOrder? endian = Common.Binary.ByteOrder.Unknown)
            : this(ref memory, endian) { }

        public Bitable(Bitable other)
        {
            if (other.Memory != null)
            {
                Memory = new byte[other.Count];
                other.Memory.CopyTo(Memory, 0);
            }
            Endian = other.Endian;
        }

        public Bitable(Bits bits, Common.Binary.ByteOrder? endian = Common.Binary.ByteOrder.Unknown)
        {
            Memory = new byte[1];
            Memory[0] = bits.m_Bits;
            Endian = endian.Value;
        }

        public Bitable(ref Bits bits, Common.Binary.ByteOrder? endian = Common.Binary.ByteOrder.Unknown)
        {
            Memory = new byte[1];
            Memory[0] = bits.m_Bits;
            Endian = endian.Value;
        }

        internal Bitable(Common.Binary.ByteOrder endian = Common.Binary.ByteOrder.Unknown)
        {
            Endian = endian == Media.Common.Binary.ByteOrder.Unknown ? (Common.Binary.IsLittleEndian ? Common.Binary.ByteOrder.Little : Common.Binary.ByteOrder.Big) : endian;
        }

        public Bitable(bool Boolean)
            : this((byte)(Boolean ? 1 : 0)) { }

        public Bitable(byte Byte, Common.Binary.ByteOrder endian = Common.Binary.ByteOrder.Unknown)
            : this(endian)
        {
            Memory = new byte[] { Byte };
        }

        public Bitable(sbyte SByte, Common.Binary.ByteOrder endian = Common.Binary.ByteOrder.Unknown)
            : this(endian)
        {
            Memory = new byte[] { (byte)SByte };
        }

        public Bitable(Int16 Int, Common.Binary.ByteOrder endian = Common.Binary.ByteOrder.Unknown)
            : this(endian)
        {
            Memory = BitConverter.GetBytes(Int);
        }

        public Bitable(UInt16 Int, Common.Binary.ByteOrder endian = Common.Binary.ByteOrder.Unknown)
            : this(endian)
        {
            Memory = BitConverter.GetBytes(Int);
        }

        public Bitable(Int32 Int, Common.Binary.ByteOrder endian = Common.Binary.ByteOrder.Unknown)
            : this(endian)
        {
            Memory = BitConverter.GetBytes(Int);
        }

        public Bitable(UInt32 Int, Common.Binary.ByteOrder endian = Common.Binary.ByteOrder.Unknown)
            : this(endian)
        {
            Memory = BitConverter.GetBytes(Int);
        }

        public Bitable(Int64 Int, Common.Binary.ByteOrder endian = Common.Binary.ByteOrder.Unknown)
            : this(endian)
        {
            Memory = BitConverter.GetBytes(Int);
        }

        public Bitable(UInt64 Int, Common.Binary.ByteOrder endian = Common.Binary.ByteOrder.Unknown)
            : this(endian)
        {
            Memory = BitConverter.GetBytes(Int);
        }

        public Bitable(float Single, Common.Binary.ByteOrder endian = Common.Binary.ByteOrder.Unknown)
            : this(endian)
        {
            Memory = BitConverter.GetBytes(Single);
        }

        public Bitable(Double Double, Common.Binary.ByteOrder endian = Common.Binary.ByteOrder.Unknown)
            : this(endian)
        {
            Memory = BitConverter.GetBytes(Double);
        }

        public Bitable(Decimal Decimal, Common.Binary.ByteOrder endian = Common.Binary.ByteOrder.Unknown)
            : this(endian)
        {

            Memory = System.Decimal.GetBits(Decimal).Select(a => BitConverter.GetBytes(a)).SelectMany(b => b).ToArray();
        }


        #endregion

        #region Overrides

        public override bool Equals(object obj)
        {
            if (obj is Bitable)
            {
                Bitable b = obj as Bitable;
                if (b.Memory == Memory) return true;
                for (int i = 0, e = Math.Min(Count, b.Count); i < e; ++i)
                    if (Memory[i] != b.Memory[i]) return false;
                return true;
            }
            else if (obj is Bits)
                return this.Contains((Bits)obj);//Search for Bits in self...
            else return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return Memory.GetHashCode();
        }

        #endregion

        #region Methods

        public bool Contains(Bits b, bool ignoreEndian = true)
        {
            return IndexOf(b, ignoreEndian) != -1;
        }

        public int IndexOfAny(Bits[] b, bool ignoreEndian = true)
        {
            int index = -1;
            foreach (Bits x in b)
                if ((index = IndexOf(x, ignoreEndian)) != -1) break;
            return index;

        }

        public int IndexOf(Bits b, bool ignoreEndian = true)
        {
            byte theirBits = b.m_Bits, reverse = 0;

            if (ignoreEndian)
            {
                reverse = theirBits;
                Processor.ReverseByte(ref reverse);
            }

            int x = 0;

            //Search for Bits in self...
            foreach (Bits bits in this)
            {
                byte bitz = bits.m_Bits;
                if (bitz == theirBits) return x;
                else if (!ignoreEndian && reverse == bitz) return x;
                ++x;
            }
            return -1;
        }

        internal virtual bool ToBool(int index = 0)
        {
            return BitConverter.ToBoolean(Memory, index);
        }

        public virtual byte ToByte(int index = 0) { return Memory[index]; }

        public virtual sbyte ToSByte(int index = 0) { return (sbyte)ToByte(index); }

        public virtual Int16 ToInt16(int index = 0) { return BitConverter.ToInt16(Memory, index); }

        public virtual UInt16 ToUInt16(int index = 0) { return BitConverter.ToUInt16(Memory, index); }

        public virtual Int32 ToInt32(int index = 0) { return BitConverter.ToInt32(Memory, index); }

        public virtual UInt32 ToUInt32(int index = 0) { return BitConverter.ToUInt32(Memory, index); }

        public virtual Int64 ToInt64(int index = 0) { return BitConverter.ToInt64(Memory, index); }

        public virtual UInt64 ToUInt64(int index = 0) { return BitConverter.ToUInt64(Memory, index); }

        public virtual Single ToSingle(int index = 0)
        {
            return BitConverter.ToSingle(Memory, index);
        }

        public virtual Double ToDouble(int index = 0)
        {
            int size = Count - index;
            if (size < SizeOfDouble)
            {
                switch (size)
                {
                    case 1: return (Double)ToByte();
                    case 2: return (Double)ToInt16(index);
                    case 3: //Reserved for 24Bit
                    case 4: return (Double)ToInt32(index);
                    default: break;
                }
            }
            else return BitConverter.ToDouble(Memory, index);
            return DoubleZero.ToDouble();
        }

        public virtual Decimal ToDecimal(int index = 0, int count = 4)
        {
            //Setup the index
            int offset = index;

            //Setup the parts
            System.Collections.Generic.IEnumerable<int> parts = Enumerable.Empty<int>();

            //For all parts concatenate the required memory
            for (int i = offset; i < count; i += 4)
                parts = parts.Concat(Media.Common.Extensions.Linq.LinqExtensions.Yield(BitConverter.ToInt32(Memory, i)));

            //While we do not have four parts of the decimal add the IntZero value
            while (parts.Count() < 4)
                parts = parts.Concat(Media.Common.Extensions.Linq.LinqExtensions.Yield(IntZero.ToInt32()));

            return new System.Decimal(parts.ToArray());
        }

        public Bitable Reversed()
        {
            Bitable result = new Bitable(this, Common.Binary.ByteOrder.Big);
            Array.Reverse(result.Memory);
            return result;
        }

        public Bitable ToBigEndian()
        {
            if (Endian == Common.Binary.ByteOrder.Unknown || Endian == Common.Binary.ByteOrder.Little)
                return Reversed();
            return new Bitable(this);
        }

        public Bitable ToLittleEndian()
        {
            if (Endian == Common.Binary.ByteOrder.Unknown || Endian == Common.Binary.ByteOrder.Big)
                return Reversed();
            return new Bitable(this);
        }

        public Bitable ToSystemEndian()
        {
            if (Endian != Processor.SystemEndian)
                return Reversed();
            return new Bitable(this);
        }

        #endregion

        #region Operators

        public static implicit operator long(Bitable b) { return b.ToInt64(); }

        public static implicit operator ulong(Bitable b) { return b.ToUInt64(); }

        public static implicit operator int(Bitable b) { return b.ToInt32(); }

        public static implicit operator uint(Bitable b) { return b.ToUInt32(); }

        public static implicit operator short(Bitable b) { return b.ToInt16(); }

        public static implicit operator ushort(Bitable b) { return b.ToUInt16(); }

        public static implicit operator byte(Bitable b) { return b.ToByte(); }

        public static implicit operator sbyte(Bitable b) { return b.ToSByte(); }

        public static implicit operator decimal(Bitable b) { return b.ToDecimal(); }

        public static implicit operator double(Bitable b) { return b.ToDouble(); }

        public static implicit operator byte[](Bitable b) { return b.Memory; }

        //

        public static implicit operator Bitable(long b) { return new Bitable(b); }

        public static implicit operator Bitable(ulong b) { return new Bitable(b); }

        public static implicit operator Bitable(int b) { return new Bitable(b); }

        public static implicit operator Bitable(uint b) { return new Bitable(b); }

        public static implicit operator Bitable(short b) { return new Bitable(b); }

        public static implicit operator Bitable(ushort b) { return new Bitable(b); }

        public static implicit operator Bitable(byte b) { return new Bitable(b); }

        public static implicit operator Bitable(sbyte b) { return new Bitable(b); }

        public static implicit operator Bitable(decimal b) { return new Bitable(b); }

        public static implicit operator Bitable(double b) { return new Bitable(b); }

        //                

        public static bool operator ==(Bitable a, Bitable b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(Bitable a, Bitable b)
        {
            return !(a == b);
        }

        public static Bitable operator +(Bitable a, Bitable b)
        {
            return Processor.Add(a, b);
        }

        public static Bitable operator -(Bitable a, Bitable b)
        {
            return Processor.Subtract(a, b);
        }

        public static Bitable operator /(Bitable a, Bitable b)
        {
            return Processor.Divide(a, b);
        }

        public static Bitable operator *(Bitable a, Bitable b)
        {
            return Processor.Multiply(a, b);
        }

        public static bool operator >(Bitable a, Bitable b)
        {
            return Processor.GreaterThan(a, b);
        }

        public static bool operator <(Bitable a, Bitable b)
        {
            return Processor.LessThan(a, b);
        }

        public static Bitable operator ^(Bitable a, Bitable b)
        {
            return Processor.XOR(a, b);
        }

        public static Bitable operator |(Bitable a, Bitable b)
        {
            return Processor.OR(a, b);
        }

        public static Bitable operator &(Bitable a, Bitable b)
        {
            return Processor.AND(a, b);
        }

        public static Bitable operator <<(Bitable a, int amount)
        {
            return Processor.ShiftLeft(a, amount);
        }

        public static Bitable operator >>(Bitable a, int amount)
        {
            return Processor.ShiftRight(a, amount);
        }

        #endregion

        #region BitsEnumerator

        public class BitsEnumerator : System.Collections.Generic.IEnumerator<bool>
        {
            public virtual Bits Bits { get; private set; }

            public virtual int BitIndex { get; private set; }

            public virtual int BitSize { get; set; }

            public virtual int Count { get { return BitSize - BitIndex; } }

            public BitsEnumerator(Bits bits, int start = 0, int bitSize = Bits.BitSize)
            {
                if (bits == null) throw new ArgumentNullException(); Bits = bits;
                if (start < 0) BitIndex = -1;
                else BitIndex = start;
            }

            public virtual bool Current
            {
                get { if (BitIndex < 0) throw new InvalidOperationException("Enumeration has not started. Call MoveNext"); return (Bits & (1 << BitIndex)) > 0; }
            }

            object System.Collections.IEnumerator.Current
            {
                get { return Current; }
            }

            public virtual bool MoveNext()
            {
                return ++BitIndex <= Bits.BitSize;
            }

            public virtual void Reset()
            {
                BitIndex = 0;
            }

            public virtual void Dispose() { Bits = default(Bits); }
        }

        #endregion

        #region BitableEnumerator

        public class BitableEnumeraor : System.Collections.Generic.IEnumerator<Bits>, System.Collections.Generic.IEnumerator<byte>, System.Collections.Generic.IEnumerator<bool>, System.Collections.IEnumerable
        {
            Bitable Bitable;

            int BlitableIndex = -1;

            bool m_Diposed;

            public BitableEnumeraor(Bitable bitable)
            {
                Bitable = bitable;
            }

            public BitableEnumeraor(BitableEnumeraor other)
            {
                BlitableIndex = other.BlitableIndex;
                Bitable = other.Bitable;
            }

            public BitsEnumerator CurrentBits { get { return new BitsEnumerator(Current); } }

            public Bits Current
            {
                get { if (BlitableIndex == -1) throw new InvalidOperationException("Enumeration has not started. Call MoveNext"); return new Bits(Bitable.ToByte(BlitableIndex)); }
            }

            object System.Collections.IEnumerator.Current
            {
                get { return Current; }
            }

            public bool MoveNext()
            {
                if (m_Diposed) return false;
                return ++BlitableIndex < Bitable.Count;
            }

            public void Reset()
            {
                if (m_Diposed) return;
                BlitableIndex = -1;
            }

            public virtual void Dispose() { m_Diposed = true; }

            byte System.Collections.Generic.IEnumerator<byte>.Current
            {
                get { return Bitable.ToByte(BlitableIndex); }
            }

            bool System.Collections.Generic.IEnumerator<bool>.Current
            {
                get { return Bitable.ToBool(BlitableIndex); }
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return new BitableEnumeraor(Bitable);
            }
        }

        #endregion

        #region Wrappers

        public System.Collections.IEnumerator GetEnumerator()
        {
            return new BitableEnumeraor(this);
        }

        System.Collections.Generic.IEnumerator<Bits> System.Collections.Generic.IEnumerable<Bits>.GetEnumerator()
        {
            return new BitableEnumeraor(this);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return new BitableEnumeraor(this);
        }

        System.Collections.Generic.IEnumerator<byte> System.Collections.Generic.IEnumerable<byte>.GetEnumerator()
        {
            return new BitableEnumeraor(this);
        }

        System.Collections.Generic.IEnumerator<bool> System.Collections.Generic.IEnumerable<bool>.GetEnumerator()
        {
            return new BitableEnumeraor(this);
        }

        #endregion

        internal Bits[] ToBits()
        {
            if (Processor.IsNull(this)) return null;
            Bits[] result = new Bits[Memory.Length >> 3]; // / Bits.BitSize
            for (int i = Memory.Length - 1; i >= 0; --i) result[i] = Memory[i];
            return result;
        }
    }

    #endregion    

    #region Number

    /// <summary>
    /// Handles the conversion from a Bitable to a Mathematical Number
    /// </summary>
    public class Number : Bitable
    {

        public static MathProvider DefaultMathProvider = new CLRMathProvider();

        //Adds BigInteger and Complex to TypeCode
        public enum NumberTypeCodes
        {
            BigInteger = 20,
            Complex = 21
        }

        #region Overrides

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override string ToString()
        {
            return BitConverter.ToString(Memory);
        }

        #endregion

        #region Constructors

        public Number(MathProvider mathProvider = null) : base() { MathProvider = mathProvider ?? DefaultMathProvider; }

        public Number(Number other, MathProvider mathProvider = null) : base(other) { MathProvider = mathProvider ?? DefaultMathProvider; }

        public Number(Bitable other, MathProvider mathProvider = null) : base(other) { MathProvider = mathProvider ?? DefaultMathProvider; }

        public Number(Bits other, MathProvider mathProvider = null) : base(other) { MathProvider = mathProvider ?? DefaultMathProvider; }

        public Number(bool Boolean, MathProvider mathProvider = null)
            : this((byte)(Boolean ? 1 : 0))
        {
            TypeCode = System.TypeCode.Boolean;
            MathProvider = mathProvider ?? DefaultMathProvider;
        }

        public Number(byte Byte, Common.Binary.ByteOrder endian = Common.Binary.ByteOrder.Unknown, MathProvider mathProvider = null)
            : base(Byte, endian)
        {
            TypeCode = System.TypeCode.Byte;
            MathProvider = mathProvider ?? DefaultMathProvider;
        }

        public Number(sbyte SByte, Common.Binary.ByteOrder endian = Common.Binary.ByteOrder.Unknown, MathProvider mathProvider = null)
            : base(SByte, endian)
        {
            TypeCode = System.TypeCode.SByte;
            MathProvider = mathProvider ?? DefaultMathProvider;
        }

        public Number(Int16 Int, Common.Binary.ByteOrder endian = Common.Binary.ByteOrder.Unknown, MathProvider mathProvider = null)
            : base(Int, endian)
        {
            TypeCode = System.TypeCode.Int16;
            MathProvider = mathProvider ?? DefaultMathProvider;
        }

        public Number(UInt16 Int, Common.Binary.ByteOrder endian = Common.Binary.ByteOrder.Unknown, MathProvider mathProvider = null)
            : base(Int, endian)
        {
            TypeCode = System.TypeCode.UInt16;
            MathProvider = mathProvider ?? DefaultMathProvider;
        }

        public Number(Int32 Int, Common.Binary.ByteOrder endian = Common.Binary.ByteOrder.Unknown, MathProvider mathProvider = null)
            : base(Int, endian)
        {
            TypeCode = System.TypeCode.Int32;
            MathProvider = mathProvider ?? DefaultMathProvider;
        }

        public Number(UInt32 Int, Common.Binary.ByteOrder endian = Common.Binary.ByteOrder.Unknown, MathProvider mathProvider = null)
            : base(Int, endian)
        {
            TypeCode = System.TypeCode.UInt32;
            MathProvider = mathProvider ?? DefaultMathProvider;
        }

        public Number(Int64 Int, Common.Binary.ByteOrder endian = Common.Binary.ByteOrder.Unknown, MathProvider mathProvider = null)
            : base(Int, endian)
        {
            TypeCode = System.TypeCode.Int64;
            MathProvider = mathProvider ?? DefaultMathProvider;
        }

        public Number(UInt64 Int, Common.Binary.ByteOrder endian = Common.Binary.ByteOrder.Unknown, MathProvider mathProvider = null)
            : base(Int, endian)
        {
            TypeCode = System.TypeCode.UInt64;
            MathProvider = mathProvider ?? DefaultMathProvider;
        }

        public Number(float Single, Common.Binary.ByteOrder endian = Common.Binary.ByteOrder.Unknown, MathProvider mathProvider = null)
            : base(Single, endian)
        {
            TypeCode = System.TypeCode.Single;
            MathProvider = mathProvider ?? DefaultMathProvider;
        }

        public Number(Double Double, Common.Binary.ByteOrder endian = Common.Binary.ByteOrder.Unknown, MathProvider mathProvider = null)
            : base(Double, endian)
        {
            TypeCode = System.TypeCode.Double;
            MathProvider = mathProvider ?? DefaultMathProvider;
        }

        public Number(Decimal Decimal, Common.Binary.ByteOrder endian = Common.Binary.ByteOrder.Unknown, MathProvider mathProvider = null)
            : base(Decimal, endian)
        {
            TypeCode = System.TypeCode.Decimal;
            MathProvider = mathProvider ?? DefaultMathProvider;
        }

        public unsafe Number(System.Numerics.Complex Complex, Common.Binary.ByteOrder endian = Common.Binary.ByteOrder.Unknown, MathProvider mathProvider = null)
            : base(endian)
        {
            TypeCode = (TypeCode)NumberTypeCodes.Complex;
            MathProvider = mathProvider ?? DefaultMathProvider;
            Memory = new byte[SizeOfComplex];
            BitConverter.GetBytes(Complex.Imaginary).CopyTo(Memory, 0);
            BitConverter.GetBytes(Complex.Real).CopyTo(Memory, sizeof(double));
        }

        public Number(byte[] bigInteger, Common.Binary.ByteOrder endian = Common.Binary.ByteOrder.Unknown, MathProvider mathProvider = null)
            : base(endian)
        {
            TypeCode = (TypeCode)NumberTypeCodes.BigInteger;
            MathProvider = mathProvider ?? DefaultMathProvider;
            Memory = bigInteger;
        }

        #endregion

        #region Statics

        public static Number Clamp(Number n, Number min, Number max) { return Math.Max(min, Math.Min(max, n)); }

        public static int SizeOfComplex = sizeof(double) * 2;

        public readonly static Number ComplexZero = new Number(System.Numerics.Complex.Zero);

        public static Number One = new Number(System.Numerics.Complex.One);

        public static Number ImaginaryOne = new Number(System.Numerics.Complex.ImaginaryOne);

        public static Number PositiveInfinty = new Number(double.PositiveInfinity);

        public static Number NegitiveInfinity = new Number(double.NegativeInfinity);

        public static Number NaN = new Number(double.NaN);

        public static bool IsInfinity(Number N) { return double.IsInfinity(N.ToDouble()); }

        public static bool IsNan(Number N) { return double.IsNaN(N.ToDouble()); }

        public static bool IsNanOrInfinity(Number n) { return IsNan(n) || IsInfinity(n); }

        public static Type NumberBestRepresentedType(Number n)
        {
            return BestRepresentsType(n.Count);
        }

        public unsafe static Type BestRepresentsType(int sizeInBytes)
        {
            switch (sizeInBytes)
            {
                case 0: return null;
                case 1: return Types.Byte; //Need to have 1Bit, 2Bit, 3Bit, 4Bit sturctures that can be implicitly casted to Bits
                case 2: return Types.Short;
                case 3: //Reserved for 24Bit Type
                case 4: return Types.Int;
                case 5: //Reserved for 40Bit Type
                case 6: //Reserved for 48Bit Type
                case 7: //Reserved for 56Bit Type
                case 8: return Types.Long;
                case 16: return Types.Double;
                default:
                    {
                        if (sizeInBytes == SizeOfComplex) return Types.Complex;
                        return Types.BigInteger;
                    }
            }
        }

        public static bool IsNull(Number a) { return IsNull(ref a); }

        public static bool IsNull(ref Number a) { return a.Memory == null || a.Count == 0; }

        public static bool IsNullOrZero(ref Number a) { return IsNull(ref a) && a.Memory.All(b => b == 0); }

        public static void Prepare(ref Number a, ref Number b)
        {
            Bitable x = a, y = b;
            if (IsNullOrZero(ref a))
                if (IsNullOrZero(ref b)) return;
                else b.MathProvider.Processor.Swap(ref x, ref y);
        }

        public static void Addition(ref Number a, ref Number b)
        {
            //Ensure compatible storage
            if (a.MathProvider != b.MathProvider)
                Prepare(ref a, ref b);
            else a.MathProvider.Addition(ref a, ref b);
        }

        public static void Subtraction(ref Number a, ref Number b)
        {
            if (a.MathProvider != b.MathProvider)
                Prepare(ref a, ref b);
            else a.MathProvider.Subtraction(ref a, ref b);
        }

        public static void Multiplication(ref Number a, ref Number b)
        {
            if (a.MathProvider != b.MathProvider)
                Prepare(ref a, ref b);
            else a.MathProvider.Multiplication(ref a, ref b);
        }

        public static void Division(ref Number a, ref Number b)
        {
            if (a.MathProvider != b.MathProvider)
                Prepare(ref a, ref b);
            else a.MathProvider.Division(ref a, ref b);
        }

        public static void Modulus(ref Number a, ref Number b)
        {
            if (a.MathProvider != b.MathProvider)
                Prepare(ref a, ref b);
            else a.MathProvider.Modulus(ref a, ref b);
        }

        #endregion

        #region Properties

        public readonly MathProvider MathProvider;

        public readonly TypeCode TypeCode;

        public Type BestRepresentedType
        {
            get
            {
                return NumberBestRepresentedType(this);
            }
        }

        public TypeCode BestRepresentedTypeCode
        {
            get
            {
                return Type.GetTypeCode(BestRepresentedType);
            }
        }

        #endregion

        #region Methods

        public System.Numerics.BigInteger ToBigInteger(int? index = null, int? count = null)
        {
            if (index.HasValue)
                if (index.Value < 0) index = 0;
                else if (index.Value > Count) return System.Numerics.BigInteger.Zero;

            if (count.HasValue)
                if (count.Value <= 0) return System.Numerics.BigInteger.Zero;
                else count = Math.Min(count.Value, Count);

            return new System.Numerics.BigInteger(Memory.Skip(index ?? 0).Take(count ?? Count).ToArray());
        }

        public System.Numerics.Complex ToComplex(int? realIndex = null, int? imaginaryIndex = null)
        {
            return new System.Numerics.Complex(ToDouble(realIndex ?? 0), ToDouble(imaginaryIndex ?? Bitable.SizeOfDouble));
        }

        #endregion

        #region Operators

        //Todo ensure b .Count is best compabile before calling... otherwise call to best type... then cast back
        //E.g. PAD
        //Number should not result in Argument out of range when being called

        public static implicit operator long(Number b) { return b.ToInt64(); }

        public static implicit operator ulong(Number b) { return b.ToUInt64(); }

        public static implicit operator int(Number b) { return b.ToInt32(); }

        public static implicit operator uint(Number b) { return b.ToUInt32(); }

        public static implicit operator short(Number b) { return b.ToInt16(); }

        public static implicit operator ushort(Number b) { return b.ToUInt16(); }

        public static implicit operator byte(Number b) { return b.ToByte(); }

        public static implicit operator sbyte(Number b) { return b.ToSByte(); }

        public static implicit operator decimal(Number b) { return b.ToDecimal(); }

        public static implicit operator float(Number b) { return b.ToSingle(); }

        public static implicit operator double(Number b) { return b.ToDouble(); }

        public static implicit operator byte[](Number b) { return b.Memory; }

        public static implicit operator System.Numerics.Complex(Number b) { return b.ToComplex(); }

        public static implicit operator System.Numerics.BigInteger(Number b) { return b.ToBigInteger(); }

        //

        public static implicit operator Number(long b) { return new Number(b); }

        public static implicit operator Number(ulong b) { return new Number(b); }

        public static implicit operator Number(int b) { return new Number(b); }

        public static implicit operator Number(uint b) { return new Number(b); }

        public static implicit operator Number(short b) { return new Number(b); }

        public static implicit operator Number(ushort b) { return new Number(b); }

        public static implicit operator Number(byte b) { return new Number(b); }

        public static implicit operator Number(sbyte b) { return new Number(b); }

        public static implicit operator Number(decimal b) { return new Number(b); }

        public static implicit operator Number(double b) { return new Number(b); }

        public static implicit operator Number(float b) { return new Number(b); }

        public static implicit operator Number(System.Numerics.Complex c) { return new Number(c); }

        public static implicit operator Number(System.Numerics.BigInteger c) { return new Number(c); }

        //Needs Complimenter => Add, Subtract, Multiply, Divide

        //
        public static Number operator +(Number a, Number b)
        {
            if (IsNull(a))
                if (!IsNull(b)) return b;
                else return (Number)Bitable.SingleZero;
            else return a.MathProvider.Addition(ref a, ref b);
        }

        public static Number operator -(Number a, Number b)
        {
            if (IsNull(a))
                if (!IsNull(b)) return b;
                else return (Number)Bitable.SingleZero;
            else return a.MathProvider.Subtraction(ref a, ref b);
        }

        public static Number operator /(Number a, Number b)
        {
            if (IsNull(a))
                if (!IsNull(b)) return b;
                else return (Number)Bitable.SingleZero;
            else return a.MathProvider.Division(ref a, ref b);
        }

        public static Number operator *(Number a, Number b)
        {
            if (IsNull(a))
                if (!IsNull(b)) return b;
                else return (Number)Bitable.SingleZero;
            else return a.MathProvider.Multiplication(ref a, ref b);
        }

        public static bool operator >(Number a, Number b)
        {
            return (Bitable)a > (Bitable)b;
        }

        public static bool operator <(Number a, Number b)
        {
            return !((Bitable)a > (Bitable)b);
        }

        public static Number operator ^(Number a, Number b)
        {
            return new Number((Bitable)a ^ (Bitable)b);
        }

        public static Number operator |(Number a, Number b)
        {
            return new Number((Bitable)a | (Bitable)b);
        }

        public static Number operator &(Number a, Number b)
        {
            return new Number((Bitable)a & (Bitable)b);
        }

        public static Number operator <<(Number a, int amount)
        {
            return new Number((Bitable)a << amount);
        }

        public static Number operator >>(Number a, int amount)
        {
            return new Number((Bitable)a >> amount);
        }

        #endregion

    }

    #endregion
}
