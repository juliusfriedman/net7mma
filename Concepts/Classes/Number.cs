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
 * 1768557944168860 / 562949953421312 ﻿= 3.14159﻿
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

    //ICoreUnit

    /// <summary>
    /// Interface which allows Hardware access
    /// </summary>
    public interface IHardware
    {
        
    }

    #region IProcessor

    ///https://github.com/CosmosOS/Cosmos/blob/653b7a8321ffae5a6097a99846b95d3e70341a4a/Users/Gero%20Landmann/Cosmos.Assembler.X86.Gero/Registers.cs

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
    public /*abstract*/ class Processor : IProcessor
    {
        internal int Id = System.Threading.Thread.CurrentThread.ManagedThreadId;

        public Processor()
        {

        }

        public static readonly Processor DefaultProcessor = new Processor();

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

        //Machine
        public static void ReverseByte(ref byte b) { b = (byte)((b * Reverse1 & Reverse2) % J); }

        //On interface
        [System.CLSCompliant(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static bool IsNull(Bitable a)
        {
            return Common.Extensions.Array.ArrayExtensions.IsNullOrEmpty(a) || a.Count.Equals(Z); //implicit to byte[]
        }

        //On interface
        [System.CLSCompliant(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static bool IsNullOrZero(Bitable a) { return IsNull(a) || a.Memory.AsParallel().Sum(b => b).Equals(Z); }

        //public static Bitable Manipulate(Bitable a, Bitable b, Manipulation manipulation)
        //{
        //    return manipulation(a, b);
        //}

        //public static void Manipulate(ref Bitable a, ref Bitable b, ReferenceManipulation referenceManipulation)
        //{
        //    referenceManipulation(ref a, ref b);
        //}

        //Todo flags, eflags.

        public static void Addition(ref Bitable a, ref Bitable b)
        {
            for (int i = 0, e = System.Math.Min(a.Count, b.Count); i < e; ++i)
                a[i] += b[i];
        }

        public static Bitable Add(Bitable a, Bitable b)
        {
            if (IsNullOrZero(a)) return b;
            else if (IsNullOrZero(b)) return a;
            else
            {
                Bitable result = new Bitable(a);
                Addition(ref result, ref b);
                return result;
            }
        }

        public static void Subtraction(ref Bitable a, ref Bitable b)
        {
            for (int i = 0, e = System.Math.Min(a.Count, b.Count); i < e; ++i)
                a[i] -= b[i];
        }

        public static Bitable Subtract(Bitable a, Bitable b)
        {
            if (IsNullOrZero(a)) return b;
            else if (IsNullOrZero(b)) return a;
            else
            {
                Bitable result = new Bitable(a);
                Subtraction(ref result, ref b);
                return result;
            }
        }

        public static void Multiplication(ref Bitable a, ref Bitable b)
        {
            for (int i = 0, e = System.Math.Min(a.Count, b.Count); i < e; ++i)
                a[i] *= b[i];
        }

        public static Bitable Multiply(Bitable a, Bitable b)
        {
            if (IsNullOrZero(a)) return b;
            else if (IsNullOrZero(b)) return a;
            else
            {
                Bitable result = new Bitable(a);
                Multiplication(ref result, ref b);
                return result;
            }
        }

        public static void Division(ref Bitable a, ref Bitable b)
        {
            for (int i = 0, e = System.Math.Min(a.Count, b.Count); i < e; ++i)
                a[i] /= b[i];
        }

        public static Bitable Divide(Bitable a, Bitable b)
        {
            if (IsNullOrZero(a)) return b;
            else if (IsNullOrZero(b)) return a;
            else
            {
                Bitable result = new Bitable(a);
                Division(ref result, ref b);
                return result;
            }
        }

        public static void Modulo(ref Bitable a, ref Bitable b)
        {
            for (int i = 0, e = System.Math.Min(a.Count, b.Count); i < e; ++i)
                a[i] %= b[i];
        }

        public static Bitable Modulus(Bitable a, Bitable b)
        {
            if (IsNullOrZero(a)) return b;
            else if (IsNullOrZero(b)) return a;
            else
            {
                Bitable result = new Bitable(a);
                Modulo(ref result, ref b);
                return result;
            }
        }

        public static bool GreaterThan(ref Bitable a, ref Bitable b)
        {
            // (a != b) && 
            return a.Memory.Sum(i => i) > b.Memory.Sum(i => i);
        }

        [System.CLSCompliant(false)]
        public static bool GreaterThan(Bitable a, Bitable b)
        {
            return GreaterThan(ref a, ref b);
        }

        public static bool LessThan(ref Bitable a, ref Bitable b)
        {
            return GreaterThan(ref b, ref a);
        }

        [System.CLSCompliant(false)]
        public static bool LessThan(Bitable a, Bitable b)
        {
            return LessThan(ref a, ref b);
        }

        public static void OR(ref Bitable a, ref Bitable b)
        {
            for (int i = 0, e = System.Math.Min(a.Count, b.Count); i < e; ++i)
                a[i] |= b[i];
        }

        [System.CLSCompliant(false)]
        public static Bitable OR(Bitable a, Bitable b)
        {
            if (IsNullOrZero(a)) return b;
            else if (IsNullOrZero(b)) return a;
            else
            {
                Bitable result = new Bitable(a);
                OR(ref result, ref b);
                return result;
            }
        }

        public static void XOR(ref Bitable a, ref Bitable b)
        {
            for (int i = 0, e = System.Math.Min(a.Count, b.Count); i < e; ++i)
                a[i] ^= b[i];
        }

        [System.CLSCompliant(false)]
        public static Bitable XOR(Bitable a, Bitable b)
        {
            if (IsNullOrZero(a)) return b;
            else if (IsNullOrZero(b)) return a;
            else
            {
                Bitable result = new Bitable(a);
                XOR(ref result, ref b);
                return result;
            }
        }

        public static void AND(ref Bitable a, ref Bitable b)
        {
            for (int i = 0, e = System.Math.Min(a.Count, b.Count); i < e; ++i)
                a[i] &= b[i];
        }

        [System.CLSCompliant(false)]
        public static Bitable AND(Bitable a, Bitable b)
        {
            if (IsNullOrZero(a)) return b;
            else if (IsNullOrZero(b)) return a;
            else
            {
                Bitable result = new Bitable(a);
                AND(ref result, ref b);
                return result;
            }
        }

        public static void NOT(ref Bitable a, ref Bitable b)
        {
            for (int i = 0, e = System.Math.Min(a.Count, b.Count); i < e; ++i)
                a[i] = (byte)~b[i];
        }

        [System.CLSCompliant(false)]
        public static Bitable NOT(Bitable a, Bitable b)
        {
            if (IsNullOrZero(a)) return b;
            else if (IsNullOrZero(b)) return a;
            else
            {
                Bitable result = new Bitable(a);
                NOT(ref result, ref b);
                return result;
            }
        }

        public static void NAND(ref Bitable a, ref Bitable b)
        {
            for (int i = 0, e = System.Math.Min(a.Count, b.Count); i < e; ++i)
                a[i] &= (byte)(~b[i]);
        }

        [System.CLSCompliant(false)]
        public static Bitable NAND(Bitable a, Bitable b)
        {
            if (IsNullOrZero(a)) return b;
            else if (IsNullOrZero(b)) return a;
            else
            {
                Bitable result = new Bitable(a);
                NAND(ref result, ref b);
                return result;
            }
        }

        public static void NOR(ref Bitable a, ref Bitable b)
        {
            for (int i = 0, e = System.Math.Min(a.Count, b.Count); i < e; ++i)
                a[i] |= (byte)(~b[i]);
        }

        [System.CLSCompliant(false)]
        public static Bitable NOR(Bitable a, Bitable b)
        {
            if (IsNullOrZero(a)) return b;
            else if (IsNullOrZero(b)) return a;
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

        [System.CLSCompliant(false)]
        public static Bitable ShiftLeft(Bitable a, int amount, int index = 0)
        {
            if (IsNullOrZero(a)) return a;
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
        [System.CLSCompliant(false)]

        public static Bitable ShiftRight(Bitable a, int amount, int index = 0)
        {
            if (IsNullOrZero(a)) return a;
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

        [System.CLSCompliant(false)]
        public static Bitable RotateRight(Bitable a, int amount, int index = 0)
        {
            if (IsNullOrZero(a)) return a;
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

        [System.CLSCompliant(false)]
        public static Bitable RotateLeft(Bitable a, int amount, int index = 0)
        {
            if (IsNullOrZero(a)) return a;
            else
            {
                Bitable result = new Bitable(a);
                RotateLeft(ref result, ref amount, ref index);
                return result;
            }
        }

        bool IProcessor.IsNull(ref Bitable a)
        {
            return IsNull(a);
        }

        bool IProcessor.IsNullOrZero(ref Bitable a)
        {
            return IsNullOrZero(a);
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

        //IFormattable...
        //string DefaultFormat { get; }
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

    /// <summary>
    /// A basic two's complementor
    /// </summary>
    public class CLRMathProvider : MathProvider
    {
        //Numbers are stored in Twos Complement or Base 2 [0 := 1]
        //This should be DecimalBase and should be present on IMathProvider interface
        //BinaryRepresentation is a better use than int.
        //const Common.Machine.BinaryRepresentation BinaryRepresentation = Common.Machine.BinaryRepresentation.Base;
        //If decidedly so then possibly also include a information class such as below..
        //public class MathProviderInformation
        //{
        //    //etc

        //    Common.Machine.BinaryRepresentation BinaryRepresentation;

        //    int DecimalBase;
        //}

        const int Base = 2;

       

        //DefaultRadix is probably a better name.
        public readonly static Radix DecimalBase = new Radix(Base);

        /// <summary>
        /// The implementation of <see cref="IProcessor"/> which is utilized by this instance.
        /// </summary>
        IProcessor m_Processor;

        //Todo, if enabled then do not throw on overflow
        public bool DisableOverflowExceptions;

        //Todo, if enabled then do not throw on divide by zero
        public bool DisableDivideByZeroExceptions;

        //Number InfiniteQuotient = Number.NegitiveZero;

        /// <summary>
        /// 
        /// </summary>
        public override IProcessor Processor
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_Processor; }
        }

        /// <summary>
        /// The <see cref="Radix"/> of this instance.
        /// </summary>
        public override Radix Radix
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return DecimalBase; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="processor"></param>
        public CLRMathProvider(IProcessor processor = null)
        {
            m_Processor = processor ?? Classes.Processor.DefaultProcessor;
        }

        #region Statics

        public bool IsNull(Bitable a) { return Processor.IsNull(ref a); }
        
        [System.CLSCompliant(false)]
        public bool IsNull(ref Bitable a) { return Processor.IsNull(ref a); }

        public bool IsZero(Bitable a) { return System.Linq.Enumerable.All(a.Memory, (b => b == 0)); }

        public bool IsNullOrZero(ref Number a) { return IsNull(a) || IsZero(a); }

        #endregion

        //Todo, See options for the ability to disable Overflow and Divide by zero.
        //~ The quotient of N = 0 = X, how many times can you put 0 into 0? Infinity, 0, 1... -0?

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
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

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
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

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
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

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
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

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
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

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public override bool GreaterThan(ref Number a, ref Number b)
        {
            Bitable x = a, y = b;
            return Processor.GreaterThan(ref x, ref y);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public override bool LessThan(ref Number a, ref Number b)
        {
            Bitable x = a, y = b;
            return Processor.LessThan(ref x, ref y);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public override Number Min(ref Number a, ref Number b)
        {
            return a > b ? a : b;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public override Number Max(ref Number a, ref Number b)
        {
            return a > b ? b : a;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public override bool Equals(ref Number a, ref Number b)
        {
            return a.Memory.Equals(b.Memory);
        }
    }

    #endregion    

    //RomanNumeral (for fun) (https://github.com/IllidanS4/SharpUtils/blob/fd0e8fbab9fa45a23c9b380121952ef959df85bd/Numerics/RomanNumerals.cs)

    //Todo, Vector

    //Probably don't need Bits and Bitable for these purposes.
    //If for some reason they make it then don't use Bool as the CoreUnit
    //, Use Int and emulate X bits within

    #region Bits

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Explicit,
        Size = 1,//This field must be equal or greater than the total size, in bytes, of the members of the class or structure.
        Pack = 0, //A value of 0 indicates that the packing alignment is set to the default for the current platform.
        CharSet = System.Runtime.InteropServices.CharSet.Auto)]
    public struct Bits : 
        System.Collections.IEnumerable, 
        //ICoreUnit
        System.Collections.Generic.IEnumerable<bool>, 
        System.Collections.Generic.IEnumerable<Byte> //IList
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
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get
            {
                fixed (byte* memory = &m_Bits)
                {
                    return memory;
                }
            }
        }

        public byte ManagedBits
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_Bits; }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public Bits(byte b) { m_Bits = b; }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]

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

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return m_Bits.GetHashCode();
        }

        public bool this[int index]
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return GetBit(this, index); }
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            set { SetBit(this, index); }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static bool GetBit(Bits b, int index)
        {
            return (b.m_Bits & (Size << index)) > 0;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static void SetBit(Bits b, int index)
        {
            b |= (Size << index);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static void ClearBit(Bits b, int index)
        {
            b &= ~(Size << index);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static void ToggleBit(Bits b, int index)
        {
            b ^= (byte)(Size << index);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static implicit operator byte(Bits b) { return b.m_Bits; }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static implicit operator int(Bits b) { return b.m_Bits; }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static implicit operator Bits(byte b) { return new Bits(b); }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static Bits operator ^(Bits b, int amount)
        {
            return new Bits((byte)(b.m_Bits ^ amount));
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static Bits operator |(Bits b, int amount)
        {
            return new Bits((byte)(b.m_Bits | amount));
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static Bits operator &(Bits b, int amount)
        {
            return new Bits((byte)(b.m_Bits & amount));
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static bool operator >(Bits b, int amount)
        {
            return b.m_Bits > amount;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static bool operator <(Bits b, int amount)
        {
            return b.m_Bits > amount;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static Bits operator >>(Bits b, int amount)
        {
            return new Bits((byte)(b.m_Bits >> amount));
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static Bits operator <<(Bits b, int amount)
        {
            return new Bits((byte)(b.m_Bits << amount));
        }
        
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static Bits operator +(Bits b, int amount)
        {
            return new Bits((byte)(b.m_Bits + amount));
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static Bits operator -(Bits b, int amount)
        {
            return new Bits((byte)(b.m_Bits - amount));
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static Bits operator *(Bits b, int amount)
        {
            return new Bits((byte)(b.m_Bits * amount));
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static Bits operator /(Bits b, int amount)
        {
            return new Bits((byte)(b.m_Bits / amount));
        }

        //

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static Bits operator ^(Bits b, Bits other)
        {
            return new Bits((byte)(b.m_Bits ^ other.m_Bits));
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static Bits operator |(Bits b, Bits other)
        {
            return new Bits((byte)(b.m_Bits | other.m_Bits));
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static Bits operator &(Bits b, Bits other)
        {
            return new Bits((byte)(b.m_Bits & other.m_Bits));
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static bool operator >(Bits b, Bits other)
        {
            return b.m_Bits > other.m_Bits;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static bool operator <(Bits b, Bits other)
        {
            return b.m_Bits > other.m_Bits;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static Bits operator +(Bits b, Bits other)
        {
            return new Bits((byte)(b.m_Bits + other.m_Bits));
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static Bits operator -(Bits b, Bits other)
        {
            return new Bits((byte)(b.m_Bits - other.m_Bits));
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static Bits operator *(Bits b, Bits other)
        {
            return new Bits((byte)(b.m_Bits * other.m_Bits));
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static Bits operator /(Bits b, Bits other)
        {
            return new Bits((byte)(b.m_Bits / other.m_Bits));
        }

        //>=, <=

        //%

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Bits b, int a) { return a.Equals(b.m_Bits); }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Bits b, int a) { return false.Equals((a.Equals(b.m_Bits))); }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Bits b, byte a) { return a.Equals(b.m_Bits); }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Bits b, byte a) { return false.Equals((a.Equals(b.m_Bits))); }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Bits b, Bits a)
        {
            return a.m_Bits.Equals(b.m_Bits);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Bits b, Bits a)
        {
            return false.Equals((a.m_Bits.Equals(b.m_Bits)));
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public System.Collections.IEnumerator GetEnumerator()
        {
            return new Bitable.BitsEnumerator(this);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        System.Collections.Generic.IEnumerator<bool> System.Collections.Generic.IEnumerable<bool>.GetEnumerator()
        {
            return new Bitable.BitsEnumerator(this);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal void Reset()
        {
            m_Bits = default(byte);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
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
        System.Collections.Generic.IEnumerable<bool> //IList ?
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
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get
            {
                fixed (byte* memory = Memory)
                {
                    return memory;
                }
            }
        }

        public Common.Binary.ByteOrder Endian { get; protected set; }

        public int Count
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return Memory.Length; }
        }

        public int BitCount
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return Count << 3; }
        }//* 8

        public byte this[int index]
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get
            {
                return Memory[index];
            }
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
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

        internal Bitable(ref byte[] memory, Common.Binary.ByteOrder endian = Common.Binary.ByteOrder.Unknown)
        {
            Endian = endian;

            Memory = memory;
        }

        public Bitable(Bitable other)
        {
            //Todo, align size with Machine

            if (other.Memory != null)
            {
                Memory = new byte[other.Count];

                other.Memory.CopyTo(Memory, 0);
            }

            Endian = other.Endian;
        }

        public Bitable(Bits bits, Common.Binary.ByteOrder endian = Common.Binary.ByteOrder.Unknown)
        {
            Memory = new byte[1];
            Memory[0] = bits.m_Bits;
            Endian = endian;
        }

        [System.CLSCompliant(false)]
        public Bitable(ref Bits bits, Common.Binary.ByteOrder endian = Common.Binary.ByteOrder.Unknown)
        {
            Memory = new byte[1] { bits.m_Bits };

            Endian = endian;
        }

        internal Bitable(Common.Binary.ByteOrder endian = Common.Binary.ByteOrder.Unknown)
        {
            //Enum Equals Boxes
            Endian = endian.Equals(Media.Common.Binary.ByteOrder.Unknown) ? (Common.Binary.IsLittleEndian ? Common.Binary.ByteOrder.Little : Common.Binary.ByteOrder.Big) : endian;
        }

        public Bitable(bool Boolean)
            : this((byte)(Boolean ? 1 : 0)) { }

        public Bitable(byte Byte, Common.Binary.ByteOrder endian = Common.Binary.ByteOrder.Unknown)
            : this(endian)
        {
            Memory = new byte[] { Byte };
        }

        [System.CLSCompliant(false)]
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

        [System.CLSCompliant(false)]
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

        [System.CLSCompliant(false)]
        public Bitable(UInt32 UInt, Common.Binary.ByteOrder endian = Common.Binary.ByteOrder.Unknown)
            : this(endian)
        {
            Memory = BitConverter.GetBytes(UInt);
        }

        public Bitable(Int64 Long, Common.Binary.ByteOrder endian = Common.Binary.ByteOrder.Unknown)
            : this(endian)
        {
            Memory = BitConverter.GetBytes(Long);
        }

        [System.CLSCompliant(false)]
        public Bitable(UInt64 ULong, Common.Binary.ByteOrder endian = Common.Binary.ByteOrder.Unknown)
            : this(endian)
        {
            Memory = BitConverter.GetBytes(ULong);
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

        //Todo, non boxing

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public bool Equals(Bitable bitable)
        {
            if (bitable.Memory.Equals(Memory)) return true;

            for (int i = 0, e = System.Math.Min(Count, bitable.Count); i < e; ++i)
                if (false.Equals(Memory[i].Equals(bitable.Memory[i]))) return false;

            return true;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj)
        {
            if (obj is Bitable) return Equals(obj as Bitable);
            else if (obj is Bits) return this.Contains((Bits)obj);//Search for Bits in self...
            else return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return Memory.GetHashCode();
        }

        public override string ToString()
        {
            return ToString(0, Count);
        }

        public string ToString(int offset, int length)
        {
            return ToString(null, ref offset, ref length);
        }

        public string ToString(string format, ref int offset, ref int length)
        {
            if (string.IsNullOrWhiteSpace(format)) return BitConverter.ToString(Memory, offset, length);

            return string.Format(format, BitConverter.ToString(Memory, offset, length));
        }

        public string ToString(string format)
        {
            int offset = 0, count = Count;

            return ToString(format, ref offset, ref count);
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
            
            foreach (Bits x in b) if (false.Equals((index = IndexOf(x, ignoreEndian)).Equals(-1))) break;
                
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

                if (bitz.Equals(theirBits)) return x;
                else if (true.Equals(ignoreEndian) && reverse.Equals(bitz)) return x;

                ++x;
            }

            return -1;
        }        

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal /* virtual */ bool ToBool(int index = 0)
        {
            return BitConverter.ToBoolean(Memory, index);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public /* virtual */ byte ToByte(int index = 0) { return Memory[index]; }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        [System.CLSCompliant(false)]
        public /* virtual */ sbyte ToSByte(int index = 0) { return (sbyte)ToByte(index); }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public /* virtual */ Int16 ToInt16(int index = 0) { return Common.Binary.Read16(Memory, index, false); }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        [System.CLSCompliant(false)]
        public /* virtual */ UInt16 ToUInt16(int index = 0) { return Common.Binary.ReadU16(Memory, index, false); }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public /* virtual */ Int32 ToInt32(int index = 0) { return Common.Binary.Read32(Memory, index, false); }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        [System.CLSCompliant(false)]
        public /* virtual */ UInt32 ToUInt32(int index = 0) { return Common.Binary.ReadU32(Memory, index, false); }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public /* virtual */ Int64 ToInt64(int index = 0) { return Common.Binary.Read64(Memory, index, false); }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        [System.CLSCompliant(false)]
        public /* virtual */ UInt64 ToUInt64(int index = 0) { return Common.Binary.ReadU64(Memory, index, false); }

        //Todo, no BitConverter

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public /* virtual */ Single ToSingle(int index = 0)
        {
            return BitConverter.ToSingle(Memory, index);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public /* virtual */ Double ToDouble(int index = 0)
        {
            int size = Count - index;
            if (size < SizeOfDouble)
            {
                switch (size)
                {
                    case Common.Binary.BytesPerByte: return (Double)ToByte();
                    case Common.Binary.BytesPerChar: return (Double)ToInt16(index);
                    case Common.Binary.Three: //Reserved for 24Bit
                    default:
                    case Common.Binary.BytesPerInteger: return (Double)ToInt32(index);
                }
            }
            else return BitConverter.ToDouble(Memory, index); //Doesn't handle - 0...
        }

        //http://msdn2.microsoft.com/en-us/library/system.decimal.getbits.aspx

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public /* virtual */ Decimal ToDecimal(int index = 0, int count = Common.Binary.BytesPerInteger)
        {
            //Setup the index
            int offset = index;

            //Setup the parts
            System.Collections.Generic.IEnumerable<int> parts = Enumerable.Empty<int>();

            //For all parts concatenate the required memory
            for (int i = offset; i < count; i += Common.Binary.BytesPerInteger)
                parts = parts.Concat(Media.Common.Extensions.Linq.LinqExtensions.Yield(BitConverter.ToInt32(Memory, i)));

            //While we do not have four parts of the decimal add the IntZero value
            while (parts.Count() < Common.Binary.BytesPerInteger)
                parts = parts.Concat(Media.Common.Extensions.Linq.LinqExtensions.Yield(IntZero.ToInt32()));

            return new System.Decimal(parts.ToArray());
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public Bitable Reversed()
        {
            //Must switch anyway, the reverse may not always be big or little.

            //Enum Equals Boxes
            Bitable result = new Bitable(this, Endian.Equals(Common.Binary.ByteOrder.Big) ? Common.Binary.ByteOrder.Little : Common.Binary.ByteOrder.Big);

            Array.Reverse(result.Memory);

            return result;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public Bitable ToBigEndian()
        {
            //Enum Equals Boxes
            if (Endian.Equals(Common.Binary.ByteOrder.Unknown) || Endian.Equals(Common.Binary.ByteOrder.Little)) return Reversed();
            return new Bitable(this);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public Bitable ToLittleEndian()
        {
            //Enum Equals Boxes
            if (Endian.Equals(Common.Binary.ByteOrder.Unknown) || Endian.Equals(Common.Binary.ByteOrder.Big)) return Reversed();
            return new Bitable(this);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public Bitable ToSystemEndian()
        {
            //Enum Equals Boxes
            if (false.Equals(Endian.Equals(Processor.SystemEndian))) return Reversed();

            return new Bitable(this);
        }

        #endregion

        #region Operators

        public static implicit operator long(Bitable b) { return b.ToInt64(); }

        [System.CLSCompliant(false)]
        public static implicit operator ulong(Bitable b) { return b.ToUInt64(); }

        public static implicit operator int(Bitable b) { return b.ToInt32(); }

        [System.CLSCompliant(false)]
        public static implicit operator uint(Bitable b) { return b.ToUInt32(); }

        public static implicit operator short(Bitable b) { return b.ToInt16(); }

        [System.CLSCompliant(false)]
        public static implicit operator ushort(Bitable b) { return b.ToUInt16(); }

        public static implicit operator byte(Bitable b) { return b.ToByte(); }

        [System.CLSCompliant(false)]
        public static implicit operator sbyte(Bitable b) { return b.ToSByte(); }

        public static implicit operator decimal(Bitable b) { return b.ToDecimal(); }

        public static implicit operator double(Bitable b) { return b.ToDouble(); }

        public static implicit operator byte[](Bitable b) { return b.Memory; }

        //

        public static implicit operator Bitable(long b) { return new Bitable(b); }

        [System.CLSCompliant(false)]
        public static implicit operator Bitable(ulong b) { return new Bitable(b); }

        public static implicit operator Bitable(int b) { return new Bitable(b); }

        [System.CLSCompliant(false)]
        public static implicit operator Bitable(uint b) { return new Bitable(b); }

        public static implicit operator Bitable(short b) { return new Bitable(b); }

        [System.CLSCompliant(false)]
        public static implicit operator Bitable(ushort b) { return new Bitable(b); }

        public static implicit operator Bitable(byte b) { return new Bitable(b); }

        [System.CLSCompliant(false)]
        public static implicit operator Bitable(sbyte b) { return new Bitable(b); }

        public static implicit operator Bitable(decimal b) { return new Bitable(b); }

        public static implicit operator Bitable(double b) { return new Bitable(b); }

        //                

        public static bool operator ==(Bitable a, Bitable b)
        {
            return object.ReferenceEquals(b, null) ? object.ReferenceEquals(a, null) : a.Equals(b);
        }

        public static bool operator !=(Bitable a, Bitable b)
        {
            return (a == b).Equals(false);
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

        public static Bitable operator %(Bitable a, Bitable b)
        {
            return Processor.Modulus(a, b);
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

        public static bool operator <=(Bitable a, Bitable b)
        {
            return a.Equals(b) || Processor.LessThan(a, b);
        }

        public static bool operator >=(Bitable a, Bitable b)
        {
            return a.Equals(b) || Processor.GreaterThan(a, b);
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

        public class BitableEnumerator : System.Collections.Generic.IEnumerator<Bits>, System.Collections.Generic.IEnumerator<byte>, System.Collections.Generic.IEnumerator<bool>, System.Collections.IEnumerable
        {
            Bitable Bitable;

            int Index = -1;

            bool m_Diposed;

            public BitableEnumerator(Bitable bitable)
            {
                Bitable = bitable;
            }

            public BitableEnumerator(BitableEnumerator other)
            {
                Index = other.Index;
                Bitable = other.Bitable;
            }

            public BitsEnumerator CurrentBits { get { return new BitsEnumerator(Current); } }

            public Bits Current
            {
                get { if (Index == -1) throw new InvalidOperationException("Enumeration has not started. Call MoveNext"); return new Bits(Bitable.ToByte(Index)); }
            }

            object System.Collections.IEnumerator.Current
            {
                get { return Current; }
            }

            public bool MoveNext()
            {
                if (m_Diposed) return false;
                return ++Index < Bitable.Count;
            }

            public void Reset()
            {
                if (m_Diposed) return;
                Index = -1;
            }

            public virtual void Dispose() { m_Diposed = true; }

            byte System.Collections.Generic.IEnumerator<byte>.Current
            {
                get { return Bitable.ToByte(Index); }
            }

            bool System.Collections.Generic.IEnumerator<bool>.Current
            {
                get { return Bitable.ToBool(Index); }
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return new BitableEnumerator(Bitable);
            }
        }

        #endregion

        #region Wrappers

        public System.Collections.IEnumerator GetEnumerator()
        {
            return new BitableEnumerator(this);
        }

        System.Collections.Generic.IEnumerator<Bits> System.Collections.Generic.IEnumerable<Bits>.GetEnumerator()
        {
            return new BitableEnumerator(this);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return new BitableEnumerator(this);
        }

        System.Collections.Generic.IEnumerator<byte> System.Collections.Generic.IEnumerable<byte>.GetEnumerator()
        {
            return new BitableEnumerator(this);
        }

        System.Collections.Generic.IEnumerator<bool> System.Collections.Generic.IEnumerable<bool>.GetEnumerator()
        {
            return new BitableEnumerator(this);
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
    /// Handles the conversion from a Bitable to a Mathematical Number.
    /// </summary>
    public class Number : Bitable //Todo,
                                  //IFormattable, IComparable, IConvertible, IComparable<Number>, IEquatable<Number>, etc.
    {
        #region Statics

        public static IMathProvider DefaultMathProvider = new CLRMathProvider();

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static Number Clamp(Number n, Number min, Number max)
        {
            //Iterate the memory of n, min and max in reverse
            for (int e = n.Count - 1,
                k = Common.Binary.Min(min.Count, max.Count) - 1,
                i = 0;
                i >= e && i >= k;
                --i, --k)
            {
                //Set the memory in k to whatever is integral relative to the memory of min or max
                n.Memory[k] = Common.Binary.Clamp(ref n.Memory[i], ref min.Memory[i], ref max.Memory[i]);
            }

            return n;
        }

        public static int SizeOfComplex = sizeof(double) << 1;

        public readonly static Number Zero = new Number(System.Numerics.Complex.Zero);

        public static Number NegativeZero = Common.Binary.NegativeZeroBits;  //0x00000000 00000000 00000000 00000080

        public static Number DecimalNegativeZero = -0.0m; //0x00000000 00000000 00000000 80010000

        public static Number DecimalNegativeZeroAlt = new Decimal(0, 0, 0, true, 0); //0x00000000 00000000 00000000 80000000 

        public static Number One = new Number(System.Numerics.Complex.One);

        public static Number NegitiveOne = -One;

        public static Number ImaginaryOne = new Number(System.Numerics.Complex.ImaginaryOne);

        public static Number PositiveInfinty = new Number(double.PositiveInfinity);

        public static Number NegitiveInfinity = new Number(double.NegativeInfinity);

        public static Number NaN = new Number(double.NaN);

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static bool IsInfinity(Number N) { return double.IsInfinity(N.ToDouble()); }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static bool IsNan(Number N) { return double.IsNaN(N.ToDouble()); }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static bool IsNanOrInfinity(Number n) { return IsNan(n) || IsInfinity(n); }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static Number Sqrt(Number n, int? realIndex = null, int? imaginaryIndex = null)
        {
            if (n.IsAbsoluteZero)
            {
                return new Number(System.Math.Sqrt(n.ToDouble()));
            }
            else
            {
                return new Number(System.Numerics.Complex.Sqrt(n.ToComplex(realIndex, imaginaryIndex)));
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static Number Abs(Number n)
        {
            return n.IsNegative ? (Number)(-n.ToDouble()) : n;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static Number Negate(Number n)
        {
            //if already negative should still negate.
            return -n.ToDouble();

            //return n.IsNegative ? n : (Number)(-n.ToDouble());
        }

        //http://www.codeproject.com/Articles/9078/Fraction-class-in-C
        //http://stackoverflow.com/questions/20699596/reciprocal-a-number-and-then-reciprocal-it-again-to-the-original-number
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static Number Reciprocal(Number n)
        {
            return System.Numerics.Complex.Reciprocal(n.ToComplex());
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static Type NumberBestRepresentedType(Number n)
        {
            return BestRepresentsType(n.Count);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static Type BestRepresentsType(int sizeInBytes)
        {
            switch (sizeInBytes)
            {
                case Common.Binary.Zero: return null;
                case Common.Binary.BytesPerByte: return Types.Byte; //Need to have 1Bit, 2Bit, 3Bit, 4Bit sturctures that can be implicitly casted to Bits
                case Common.Binary.BytesPerShort: return Types.Short;
                case Common.Binary.Three: //Reserved for 24Bit Type
                case Common.Binary.BytesPerInteger: return Types.Int;
                case 5: //Reserved for 40Bit Type
                case 6: //Reserved for 48Bit Type
                case 7: //Reserved for 56Bit Type
                //8 could be double also...
                case Common.Binary.BytesPerLong: return Types.Long;
                case Common.Binary.BytesPerDecimal: return Types.Decimal;
                default:
                    {
                        if (sizeInBytes == SizeOfComplex) return Types.Complex;
                        return Types.BigInteger;
                    }
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static bool IsNull(Number a) { return Common.Extensions.Array.ArrayExtensions.IsNullOrEmpty(a); } //implicit to byte[]

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static bool IsNullOrZero(Number a) { return IsNull(a) || a.Memory.All(b => b.Equals(Common.Binary.ByteZero)); }

        //Todo, Distributive , Associative, Commutative, rules

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static void Prepare(ref Number a, ref Number b)
        {
            Bitable x = a, y = b;
            if (IsNullOrZero(a))
                if (IsNullOrZero(b)) return;
                else b.MathProvider.Processor.Swap(ref x, ref y);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static void Addition(ref Number a, ref Number b)
        {
            //Ensure compatible storage
            if (false.Equals(a.MathProvider.Equals(b.MathProvider)))
                Prepare(ref a, ref b);
            else a.MathProvider.Addition(ref a, ref b);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static void Subtraction(ref Number a, ref Number b)
        {
            if (false.Equals(a.MathProvider.Equals(b.MathProvider)))
                Prepare(ref a, ref b);
            else a.MathProvider.Subtraction(ref a, ref b);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static void Multiplication(ref Number a, ref Number b)
        {
            if (false.Equals(a.MathProvider.Equals(b.MathProvider)))
                Prepare(ref a, ref b);
            else a.MathProvider.Multiplication(ref a, ref b);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static void Division(ref Number a, ref Number b)
        {
            if (false.Equals(a.MathProvider.Equals(b.MathProvider)))
                Prepare(ref a, ref b);
            else a.MathProvider.Division(ref a, ref b);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static void Modulus(ref Number a, ref Number b)
        {
            if (false.Equals(a.MathProvider.Equals(b.MathProvider)))
                Prepare(ref a, ref b);
            else a.MathProvider.Modulus(ref a, ref b);
        }

        #endregion

        #region Parse

        //http://referencesource.microsoft.com/#mscorlib/system/number.cs

        const UInt32 MaxStep = (UInt32)0xFFFFFFFF / 16; //268435455.938	

        //Todo, respect numberStyle and FormatInfo

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <param name="encoding"></param>
        /// <param name="numberStyle"></param>
        /// <param name="numberFormatInfo"></param>
        /// <returns></returns>
        public static Number Parse(byte[] buffer, int offset, int count, System.Text.Encoding encoding, System.Globalization.NumberStyles numberStyle, System.Globalization.NumberFormatInfo numberFormatInfo)
        {
            int chars = encoding.GetCharCount(buffer, offset, count);

            if (chars <= 0) return ulong.MinValue;

            Number n = ulong.MinValue;

            foreach (char c in encoding.GetChars(buffer, offset, count))
            {
                //Change the base
                n *= 16;

                //Check if char has value

                if (false.Equals(c.Equals('\0')))
                {
                    Number newN = new Number(n);

                    //Check if char is digit
                    if (c >= '0' && c <= '9')
                    {
                        newN += c - '0';
                    }
                    else //Check if char is hex
                    {
                        if (c >= 'A' && c <= 'F')
                        {
                            newN += (c - 'A') + 10;
                        }
                        else if (c >= 'a' && c <= 'f')
                        {
                            newN += (c - 'a') + 10;
                        }
                    }

                    // Detect an overflow here...
                    if (newN < n)
                    {
                        break;
                    }

                    //Store and continue
                    n = newN;
                }
            }

            return n;
        }

        #endregion

        #region NestedTypes

        //Adds BigInteger and Complex to TypeCode
        public enum NumberTypeCodes
        {
            //From Syste.TypeCode
            Empty = 0,
            Object = 1,
            DBNull = 2,
            Boolean = 3,
            Char = 4,
            SByte = 5,
            Byte = 6,
            Int16 = 7,
            UInt16 = 8,
            Int32 = 9,
            UInt32 = 10,
            Int64 = 11,
            UInt64 = 12,
            Single = 13,
            Double = 14,
            Decimal = 15,
            DateTime = 16,
            String = 18,
            //
            BigInteger = 20,
            Complex = 21
        }

        #endregion

        #region Overrides

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public bool Equals(Number number) //ref int precision... / bool exact...
        {
            //Bitable
            return this == number;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        #endregion

        #region Constructors

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public Number(IMathProvider mathProvider = null)
            : base()
        {
            MathProvider = mathProvider ?? DefaultMathProvider;
            TypeCode = BestRepresentedTypeCode;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public Number(Number other, IMathProvider mathProvider = null)
            : base(other)
        {
            MathProvider = mathProvider ?? DefaultMathProvider;
            TypeCode = BestRepresentedTypeCode;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public Number(Bitable other, IMathProvider mathProvider = null)
            : base(other)
        {
            MathProvider = mathProvider ?? DefaultMathProvider;
            TypeCode = BestRepresentedTypeCode;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public Number(Bits other, IMathProvider mathProvider = null)
            : base(other)
        {
            MathProvider = mathProvider ?? DefaultMathProvider;
            TypeCode = BestRepresentedTypeCode;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public Number(bool Boolean, IMathProvider mathProvider = null)
            : this((byte)(Boolean ? 1 : 0))
        {
            TypeCode = System.TypeCode.Boolean;
            MathProvider = mathProvider ?? DefaultMathProvider;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public Number(byte Byte, Common.Binary.ByteOrder endian = Common.Binary.ByteOrder.Unknown, IMathProvider mathProvider = null)
            : base(Byte, endian)
        {
            TypeCode = System.TypeCode.Byte;
            MathProvider = mathProvider ?? DefaultMathProvider;
        }

        [System.CLSCompliant(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public Number(sbyte SByte, Common.Binary.ByteOrder endian = Common.Binary.ByteOrder.Unknown, IMathProvider mathProvider = null)
            : base(SByte, endian)
        {
            TypeCode = System.TypeCode.SByte;
            MathProvider = mathProvider ?? DefaultMathProvider;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public Number(Int16 Int, Common.Binary.ByteOrder endian = Common.Binary.ByteOrder.Unknown, IMathProvider mathProvider = null)
            : base(Int, endian)
        {
            TypeCode = System.TypeCode.Int16;
            MathProvider = mathProvider ?? DefaultMathProvider;
        }

        [System.CLSCompliant(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public Number(UInt16 Int, Common.Binary.ByteOrder endian = Common.Binary.ByteOrder.Unknown, IMathProvider mathProvider = null)
            : base(Int, endian)
        {
            TypeCode = System.TypeCode.UInt16;
            MathProvider = mathProvider ?? DefaultMathProvider;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public Number(Int32 Int, Common.Binary.ByteOrder endian = Common.Binary.ByteOrder.Unknown, IMathProvider mathProvider = null)
            : base(Int, endian)
        {
            TypeCode = System.TypeCode.Int32;
            MathProvider = mathProvider ?? DefaultMathProvider;
        }

        [System.CLSCompliant(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public Number(UInt32 Int, Common.Binary.ByteOrder endian = Common.Binary.ByteOrder.Unknown, IMathProvider mathProvider = null)
            : base(Int, endian)
        {
            TypeCode = System.TypeCode.UInt32;
            MathProvider = mathProvider ?? DefaultMathProvider;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public Number(Int64 Int, Common.Binary.ByteOrder endian = Common.Binary.ByteOrder.Unknown, IMathProvider mathProvider = null)
            : base(Int, endian)
        {
            TypeCode = System.TypeCode.Int64;
            MathProvider = mathProvider ?? DefaultMathProvider;
        }

        [System.CLSCompliant(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public Number(UInt64 Int, Common.Binary.ByteOrder endian = Common.Binary.ByteOrder.Unknown, IMathProvider mathProvider = null)
            : base(Int, endian)
        {
            TypeCode = System.TypeCode.UInt64;
            MathProvider = mathProvider ?? DefaultMathProvider;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public Number(float Single, Common.Binary.ByteOrder endian = Common.Binary.ByteOrder.Unknown, IMathProvider mathProvider = null)
            : base(Single, endian)
        {
            TypeCode = System.TypeCode.Single;
            MathProvider = mathProvider ?? DefaultMathProvider;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public Number(Double Double, Common.Binary.ByteOrder endian = Common.Binary.ByteOrder.Unknown, IMathProvider mathProvider = null)
            : base(Double, endian)
        {
            TypeCode = System.TypeCode.Double;
            MathProvider = mathProvider ?? DefaultMathProvider;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public Number(Decimal Decimal, Common.Binary.ByteOrder endian = Common.Binary.ByteOrder.Unknown, IMathProvider mathProvider = null)
            : base(Decimal, endian)
        {
            TypeCode = System.TypeCode.Decimal;
            MathProvider = mathProvider ?? DefaultMathProvider;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public unsafe Number(System.Numerics.Complex Complex, Common.Binary.ByteOrder endian = Common.Binary.ByteOrder.Unknown, IMathProvider mathProvider = null)
            : base(endian)
        {
            TypeCode = (TypeCode)NumberTypeCodes.Complex;
            MathProvider = mathProvider ?? DefaultMathProvider;
            Memory = new byte[SizeOfComplex];
            BitConverter.GetBytes(Complex.Imaginary).CopyTo(Memory, 0);
            BitConverter.GetBytes(Complex.Real).CopyTo(Memory, sizeof(double));
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public Number(byte[] bigInteger, Common.Binary.ByteOrder endian = Common.Binary.ByteOrder.Unknown, IMathProvider mathProvider = null)
            : base(endian)
        {
            TypeCode = (TypeCode)NumberTypeCodes.BigInteger;
            MathProvider = mathProvider ?? DefaultMathProvider;
            Memory = bigInteger;
        }

        #endregion

        #region Fields

        /// <summary>
        /// The <see cref="IMathProvider"/> responsible for evalutation of logic
        /// </summary>
        public readonly IMathProvider MathProvider;

        //Todo, readonly ValueType
        /// <summary>
        /// The <see cref="System.TypeCode"/> assigned to this instance when created.
        /// </summary>
        public readonly TypeCode TypeCode;

        #endregion

        #region Properties

        /// <summary>
        /// 
        /// </summary>
        public Type BestRepresentedType
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get
            {
                return NumberBestRepresentedType(this);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public TypeCode BestRepresentedTypeCode
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get
            {
                return Type.GetTypeCode(BestRepresentedType);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool IsComplex
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get
            {
                return TypeCode.Equals((TypeCode)NumberTypeCodes.Complex);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool IsWhole
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get
            {
                return IsAbsoluteZero || Number.IsNullOrZero(this % 1);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool IsDecimal
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get
            {
                return false.Equals(IsWhole);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool IsAbsoluteZero
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get
            {
                return Number.IsNullOrZero(this);              
            }
        }

        //Would really need a special Sign property which is why there are seperate types for signed and unsigned thus there should be seperate type of Numbers..
        //SignedNumber, UnsignedNumber
        //https://msdn.microsoft.com/en-us/library/swz6z5ks(v=vs.110).aspx
        public bool IsNegative
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get
            {
                switch (TypeCode)
                {
                    case System.TypeCode.Decimal:
                            return this >= DecimalNegativeZero || this >= DecimalNegativeZeroAlt;
                    default: return IsAbsoluteZero ? false : this >= NegativeZero;
                }

                //-0...

                //return Common.Binary.IsNegative(Real.ToDouble());
                
                //return Real < System.Numerics.Complex.Zero;
            }
        }

        public Number Sign
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get
            {
                return (Number)(IsAbsoluteZero ? Zero : this.IsNegative ? NegitiveOne : One);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public NumberTypeCodes NumberTypeCode
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get
            {
                return (NumberTypeCodes)TypeCode;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public Number Real
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get
            {
                return ToComplex().Real;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public Number Imaginary
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get
            {
                return ToComplex().Imaginary;
            }
        }

        #endregion

        #region Methods

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public System.Numerics.BigInteger ToBigInteger(int? index = null, int? count = null)
        {
            if (index.HasValue)
                if (index.Value < 0) index = 0;
                else if (index.Value > Count) return System.Numerics.BigInteger.Zero;

            if (count.HasValue)
                if (count.Value <= 0) return System.Numerics.BigInteger.Zero;
                else count = System.Math.Min(count.Value, Count);

            return new System.Numerics.BigInteger(Memory.Skip(index ?? 0).Take(count ?? Count).ToArray());
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public System.Numerics.Complex ToComplex(int? realIndex = null, int? imaginaryIndex = null)
        {
            return new System.Numerics.Complex(ToDouble(realIndex ?? 0), ToDouble(imaginaryIndex ?? Bitable.SizeOfDouble));
        }

        #endregion

        #region Operators

        //MathProvider should give back flags and clear for next calls, callers should look at flags and determine what to do on certain flags.

        //Todo ensure b .Count is best compabile before calling... otherwise call to best type... then cast back
        //E.g. PAD
        //Number should not result in Argument out of range when being called unless set in instance
        //Number should not result in DiviveByZeroException unless less set in instance

        //Todo, bool..

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static implicit operator long(Number b) { return b.ToInt64(); }

        [System.CLSCompliant(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static implicit operator ulong(Number b) { return b.ToUInt64(); }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static implicit operator int(Number b) { return b.ToInt32(); }

        [System.CLSCompliant(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static implicit operator uint(Number b) { return b.ToUInt32(); }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static implicit operator short(Number b) { return b.ToInt16(); }

        [System.CLSCompliant(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static implicit operator ushort(Number b) { return b.ToUInt16(); }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static implicit operator byte(Number b) { return b.ToByte(); }

        [System.CLSCompliant(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static implicit operator sbyte(Number b) { return b.ToSByte(); }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static implicit operator decimal(Number b) { return b.ToDecimal(); }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static implicit operator float(Number b) { return b.ToSingle(); }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static implicit operator double(Number b) { return b.ToDouble(); }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static implicit operator byte[](Number b) { return b.Memory; }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static implicit operator System.Numerics.Complex(Number b) { return b.ToComplex(); }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static implicit operator System.Numerics.BigInteger(Number b) { return b.ToBigInteger(); }

        //

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static implicit operator Number(long b) { return new Number(b); }

        [System.CLSCompliant(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static implicit operator Number(ulong b) { return new Number(b); }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static implicit operator Number(int b) { return new Number(b); }

        [System.CLSCompliant(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static implicit operator Number(uint b) { return new Number(b); }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static implicit operator Number(short b) { return new Number(b); }

        [System.CLSCompliant(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static implicit operator Number(ushort b) { return new Number(b); }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static implicit operator Number(byte b) { return new Number(b); }

        [System.CLSCompliant(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static implicit operator Number(sbyte b) { return new Number(b); }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static implicit operator Number(decimal b) { return new Number(b); }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static implicit operator Number(double b) { return new Number(b); }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static implicit operator Number(float b) { return new Number(b); }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static implicit operator Number(System.Numerics.Complex c) { return new Number(c); }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static implicit operator Number(System.Numerics.BigInteger c) { return new Number(c); }

        //Needs Complimenter/ Binary Adder => Add, Subtract, Multiply, Divide

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static Number operator +(Number a, Number b)
        {
            if (IsNull(a))
                if (false.Equals(IsNull(b))) return b;
                else return (Number)Bitable.SingleZero;
            else return a.MathProvider.Addition(ref a, ref b);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static Number operator -(Number a, Number b)
        {
            if (IsNull(a))
                if (false.Equals(IsNull(b))) return b;
                else return (Number)Bitable.SingleZero;
            else return a.MathProvider.Subtraction(ref a, ref b);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static Number operator /(Number a, Number b)
        {
            if (IsNull(a))
                if (false.Equals(IsNull(b))) return b;
                else return (Number)Bitable.SingleZero;
            else return a.MathProvider.Division(ref a, ref b);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static Number operator %(Number a, Number b)
        {
            if (IsNull(a))
                if (false.Equals(IsNull(b))) return b;
                else return (Number)Bitable.SingleZero;
            else return a.MathProvider.Modulus(ref a, ref b);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static Number operator *(Number a, Number b)
        {
            if (IsNull(a))
                if (false.Equals(IsNull(b))) return b;
                else return (Number)Bitable.SingleZero;
            else return a.MathProvider.Multiplication(ref a, ref b);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static bool operator >(Number a, Number b)
        {
            return (Bitable)a > (Bitable)b;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static bool operator <(Number a, Number b)
        {
            return ((Bitable)a < (Bitable)b);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static bool operator <=(Number a, Number b)
        {
            return a.Equals(b) || a < b;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static bool operator >=(Number a, Number b)
        {
            return a.Equals(b) || a > b;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static Number operator ^(Number a, Number b)
        {
            return new Number((Bitable)a ^ (Bitable)b);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static Number operator |(Number a, Number b)
        {
            return new Number((Bitable)a | (Bitable)b);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static Number operator &(Number a, Number b)
        {
            return new Number((Bitable)a & (Bitable)b);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static Number operator <<(Number a, int amount)
        {
            return new Number((Bitable)a << amount);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static Number operator >>(Number a, int amount)
        {
            return new Number((Bitable)a >> amount);
        }

        #endregion
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
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public Radix(Number b)
        {
            Base = b;
        }

        public readonly Number Base;
    }

    #endregion

    #region INumber

    public interface INumber
    {
        IMathProvider MathProvider { get; }

        Media.Concepts.Classes.I.IPointer Sign { get; }

        Media.Concepts.Classes.I.IPointer Value { get; }
    }

    #endregion

    #region IQuantity

    public interface IQuantity
    {
        INumber Quantity { get; }
    }

    #endregion

    #region Numerical

    public class Numerical : Number, INumber, IQuantity
    {
        readonly string Format;

        Radix Radix;

        public Numerical(Radix radix, Number number = null)
            : base(number ?? Number.Zero)
        {
            Radix = radix;
        }

        public Numerical(Number number, Radix radix, string format)
            : this(radix, number)
        {
            Format = format;
        }

        //On top of the old instance such that deriving would be possible.
        public virtual new IMathProvider MathProvider
        {
            get { return base.MathProvider; }
        }

        /// <summary>
        /// Allows deriving, not the actual value of the sign but the poniter to it for now.
        /// </summary>
        public virtual new I.IPointer Sign
        {
            get { throw new NotImplementedException(); } // Pointer => Number.Sign(this);
        }

        /// <summary>
        /// The value pointer.
        /// </summary>
        public virtual I.IPointer Value
        {
            get { throw new NotImplementedException(); } // Pointer => Number + Sign.Length
        }

        /// <summary>
        /// `this`
        /// </summary>
        public virtual INumber Quantity
        {
            get { return this; }
        }

        public virtual string ToString(IFormatProvider formatProvider)
        {
            return string.Format(formatProvider, Format, this);
        }

        public override string ToString()
        {
            return base.ToString(Format);
        }

    }

    #endregion

    //Todo, Derive further for precision requirements if desired or integrate with Astringent or Declension via Atonement

}
