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

namespace Media.Common
{
    /// <summary>
    /// Provides functionality which can be though of based on machine concepts of the current architecture.
    /// </summary>
    /// <note><see href="http://blogs.msdn.com/b/vcblog/archive/2012/10/26/10362875.aspx">Hello ARM</see></note>
    /// <note><see href="https://msdn.microsoft.com/en-us/magazine/jj553518.aspx">.NET Development for ARM Processors</see></note>
    public static class Machine
    {
        #region Nested Types

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

        #region Shift Implementations

        /// <summary>
        /// Provides an API to implement left and right shifting
        /// </summary>
        public abstract class Shift : Common.BaseDisposable
        {
            /// <summary>
            /// Calulcates the Left Shift
            /// </summary>
            /// <param name="value"></param>
            /// <param name="amount"></param>
            /// <returns></returns>
            public abstract int Left(int value, int amount);

            /// <summary>
            /// Calulcates the Right Shift
            /// </summary>
            /// <param name="value"></param>
            /// <param name="amount"></param>
            /// <returns></returns>
            public abstract int Right(int value, int amount);

            //Enforce ShiftArray be implemented?
            //Or have ArrayShift class
        }

        /// <summary>
        /// Provides an implementation of sign extended shifting
        /// </summary>
        public class SignExtendedShift : Shift
        {
            public override int Left(int value, int amount)
            {
                return value << amount;
            }

            public override int Right(int value, int amount)
            {
                return value >> amount;
            }

            /// <summary>
            /// Creates a copy of the given array with all bits in the given array Shifted Left the specified amount of bits.
            /// </summary>
            /// <param name="value"></param>
            /// <param name="bitcount"></param>
            /// <returns></returns>
            public static byte[] ShiftLeft(byte[] value, int bitcount)
            {
                int length = value.Length, bits, rem;

                bits = System.Math.DivRem(bitcount, 8, out rem);

                byte[] temp = new byte[length];

                if (bitcount >= 8)
                {
                    System.Array.Copy(value, bits, temp, 0, length - bits);
                }
                else
                {
                    System.Array.Copy(value, temp, length);
                }

                if (rem != 0)
                {
                    for (int i = 0, e = length - 1; i < e; ++i)
                    {
                        temp[i] <<= rem;
                        temp[i] |= (byte)(temp[i + 1] >> 8 - rem);
                    }
                }

                return temp;
            }

            /// <summary>
            /// Creates a copy of the given array with all bits in the given array Shifted Right the specified amount of bits.
            /// </summary>
            /// <param name="value"></param>
            /// <param name="bitcount"></param>
            /// <returns></returns>
            public static byte[] ShiftRight(byte[] value, int bitcount)
            {
                int length = value.Length, bits, rem;

                bits = System.Math.DivRem(bitcount, 8, out rem);

                byte[] temp = new byte[length];

                if (bitcount >= 8)
                {
                    System.Array.Copy(value, 0, temp, bits, length - bits);
                }
                else
                {
                    System.Array.Copy(value, temp, length);
                }

                if (rem != 0)
                {
                    for (int i = length - 1; i >= 1; i--)
                    {
                        temp[i] >>= rem;
                        temp[i] |= (byte)(temp[i - 1] << 8 - rem);
                    }
                }

                return temp;
            }
        }

        /// <summary>
        /// Provides an implementation of the Logical or Arithmetic shifting
        /// </summary>
        public class LogicalShift : Shift
        {
            public override int Left(int value, int amount)
            {
                return unchecked((int)((uint)value << amount));
            }

            public long Left(long value, int amount)
            {
                return unchecked((long)((ulong)value << amount));
            }

            public override int Right(int value, int amount)
            {
                return unchecked((int)((uint)value >> amount));
            }

            public long Right(long value, int amount)
            {
                return unchecked((long)((ulong)value >> amount));
            }
        }

        /// <summary>
        /// Provides an implementation of Circular shifting
        /// </summary>
        public class CircularShift : Shift
        {
            public byte Left(byte value, int amount)
            {
                return (byte)(value << amount | value >> (8 - amount));
            }

            public byte Right(byte value, int amount)
            {
                return (byte)(value >> amount | value << (8 - amount));
            }


            public override int Left(int value, int amount)
            {
                return (byte)(value << amount | value >> (32 - amount));
            }

            public override int Right(int value, int amount)
            {
                return (byte)(value >> amount | value << (32 - amount));
            }

            //Array methods?
        }

        /// <summary>
        /// Provides a class to perform the reverse of the given shift
        /// </summary>
        public class ReverseShift : Shift
        {
            //Could just be virtual methods in Shift also...

            public ReverseShift(Shift actualShift)
            {
                if (actualShift == null) throw new System.ArgumentNullException("actualShift");

                this.ShiftClass = actualShift;
            }

            Shift ShiftClass;

            public override int Left(int value, int amount)
            {
                return ShiftClass.Right(value, amount);
            }

            public override int Right(int value, int amount)
            {
                return ShiftClass.Left(value, amount);
            }
        }

        #endregion

        #endregion

        #region Fields

        //Get the System.Type to which Machine corresponds
        internal static readonly System.Type SystemType = typeof(Machine);

        //Reflect the Assembly of that type
        internal static readonly System.Reflection.Assembly Assembly = SystemType.Assembly;

        //Get the AssemblyName from the Assembly
        internal static readonly System.Reflection.AssemblyName AssemblyName = Assembly.GetName(); //Should be okay in partial trust where as GetFullName is not...

        //Should work in netMf also
        //https://github.com/NETMF/netmf-interpreter/blob/d28c5365e35fa7c861312b702cde5b73e2ef3808/Framework/Subset_of_CorLib/System/Reflection/AssemblyNameFlags.cs

        public static System.Reflection.ProcessorArchitecture AssemblyNameProcessorArchitecture
        {

            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_AssemblyNameProcessorArchitecture; }
        }

        /// <summary>
        /// Indicates the Platform the code was compiled for.
        /// Identifies the processor and bits-per-word of the platform targeted by an executable.
        /// </summary>
        internal static System.Reflection.ProcessorArchitecture m_AssemblyNameProcessorArchitecture = AssemblyName.ProcessorArchitecture;

        public static System.Reflection.PortableExecutableKinds CodeType
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_CodeType; }
        }

        /// <summary>
        /// Indicates the type of machine code produced by JIT when the code is compiled.
        /// </summary>
        internal static System.Reflection.PortableExecutableKinds m_CodeType;

        public static System.Reflection.ImageFileMachine MachineType
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_MachineType; }
        }

        /// <summary>
        /// Indicates the CPU instructions used by the JIT when the code is compiled.
        /// </summary>
        internal static System.Reflection.ImageFileMachine m_MachineType;

        internal static int m_BitPatternSize = 0;

        //If the static doesn't inline then would have to be a Method
        /// <summary>
        /// The maximum amount of shifting which can occur before the bit pattern space repeats
        /// </summary>
        public static int BitPatternSize
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_BitPatternSize; }
        }

        internal static BinaryRepresentation m_SystemBinaryRepresentation = BinaryRepresentation.Unknown;

        /// <summary>
        /// The <see cref="BinaryRepresentation"/> of the current architecture used for the <see cref="int"/> type.
        /// </summary>
        public static BinaryRepresentation SystemBinaryRepresentation
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_SystemBinaryRepresentation; }
        }

        #endregion

        #region Machine Methods

        /// <summary>
        /// Indicates if the Machine has a finite bit pattern.
        /// </summary>
        /// <returns></returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static bool HasFiniteBitPattern() { return BitPatternSize > 0; }

        /// <summary>
        /// Indicates if the Machine can execute ARM instructions
        /// </summary>
        /// <returns></returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static bool IsArm()
        {
            //Directly uses the CPU Bit Pattern Space if compilation supports
            return 0 == 1 << Media.Common.Binary.BitsPerInteger;
        }

        /// <summary>
        /// Indicates if the Machine can execute x86 instructions
        /// </summary>
        /// <returns></returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]        
        public static bool IsX86()
        {
            return 1 == (((uint)1) << Media.Common.Binary.BitsPerInteger);
        }

        /// <summary>
        /// Indicates if the Machine can execute x64 instructions
        /// </summary>
        /// <returns></returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static bool IsX64()
        {
            return 4294967296 == (((ulong)1) << 96); //Should always be 96
        }

        //Detection? (Model, Speed, Stepping, Instruction Support ...)

        #endregion

        #region BinaryRepresentation Methods

        public static int OnesComplement(int value) { return OnesComplement(ref value); }

        /// <summary>
        /// Convert from two's to one's complement.
        /// </summary>
        /// <param name="value">A value in two's complement</param>
        /// <returns>The one's complement representation of <paramref name="value"/></returns>
        [System.CLSCompliant(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static int OnesComplement(ref int value) { return (~value); }

        /// <summary>
        /// Convert from one's to two's complement.
        /// </summary>
        /// <param name="value">A value in one's complement</param>
        /// <returns>The two's complement representation of <paramref name="value"/></returns>
        public static int TwosComplement(int value) { return TwosComplement(ref value); }

        [System.CLSCompliant(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static int TwosComplement(ref int value) { unchecked { return (~value + Common.Binary.One); } }

        public static int SignedMagnitude(int value) { int sign; return SignedMagnitude(ref value, out sign); }

        [System.CLSCompliant(false)]
        public static int SignedMagnitude(ref int value) { int sign; return SignedMagnitude(ref value, out sign); }

        /// <summary>
        /// Converts value to twos complement if the value is negitive and returns the signed magnitude representation outputs the sign
        /// </summary>
        /// <param name="value">A binary value</param>
        /// <param name="sign">The sign of <paramref name="value"/></param>
        /// <returns>The signed magnitude representation of <paramref name="value"/></returns>
        [System.CLSCompliant(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static int SignedMagnitude(ref int value, out int sign)
        {
            unchecked
            {

                //If the sign is -1 then convert to twos complement < 0 should be faster
                //if ((sign = Math.Sign(value)) == -Binary.Ūnus) value = TwosComplement(ref value);

                if ((sign = Common.Binary.Sign(value)) < Common.Binary.Zero) value = TwosComplement(ref value);

                //Doesn't output the sign...
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
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static int TwosComplementToSignedMagnitude(ref int value)
        {
            unchecked
            {
                //Create a mask of the value holding the sign
                int sign = (value >> Common.Binary.ThirtyOne); //SignMask

                //Convert from TwosComplement to SignedMagnitude
                return (((value + sign) ^ sign) | (int)(value & Common.Binary.SignMask));
            }
        }

        /// <summary>
        /// Converts the given number in signed magnitude representation to twos complement.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static long SignedMagnitudeToTwosComplement(ref int value)
        {
            unchecked
            {
                //Convert from SignedMagnitude to TwosComplement
                return ((~(value & int.MaxValue)) + Common.Binary.One) | (int)(value & Common.Binary.SignMask);
            }
        }

        /// <summary>
        /// Indicates if the architecture utilizes two's complement binary representation
        /// </summary>
        /// <returns>True if two's complement is used, otherwise false</returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static bool IsTwosComplement()
        {
            //return Convert.ToSByte(byte.MaxValue.ToString(Media.Common.Extensions.String.StringExtensions.HexadecimalFormat), Binary.Sēdecim) == -Binary.Ūnus;

            return unchecked((sbyte)byte.MaxValue == -Media.Common.Binary.One);
        }

        /// <summary>
        /// Indicates if the architecture utilizes one's complement binary representation
        /// </summary>
        /// <returns>True if ones's complement is used, otherwise false</returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static bool IsOnesComplement()
        {
            //return Convert.ToSByte(sbyte.MaxValue.ToString(Media.Common.Extensions.String.StringExtensions.HexadecimalFormat), Binary.Sēdecim) == -Binary.Ūnus;

            return unchecked(sbyte.MaxValue == -Media.Common.Binary.One);
        }

        /// <summary>
        /// Indicates if the architecture utilizes sign and magnitude representation
        /// </summary>
        /// <returns>True if sign and magnitude representation is used, otherwise false</returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static bool IsSignedMagnitude()
        {
            return unchecked(((Common.Binary.Three & -Common.Binary.One) == Common.Binary.One)); //&& false == IsTwosComplement

            //e.g. (3 & -1) == 3, where as Media.Common.Binary.BitwiseAnd(-3, 1) == 1
        }

        //http://en.wikipedia.org/wiki/Signed_number_representations
        //Excess, Base, Biased

        #endregion

        #region Constructor

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized | System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static Machine()
        {
            //Ensure not already called.
            if (m_BitPatternSize != 0 | m_SystemBinaryRepresentation != BinaryRepresentation.Unknown) return;

            //No overflow anyway
            unchecked
            {
                #region Compilation Check

                //Determine how the code was compiled
                foreach (System.Reflection.Module module in SystemType.Assembly.Modules)
                {
                    module.GetPEKind(out m_CodeType, out m_MachineType);

                    break;
                }

                //https://msdn.microsoft.com/en-us/library/system.reflection.processorarchitecture.aspx

                //Verify the probe
                switch (AssemblyNameProcessorArchitecture)
                {
                    case System.Reflection.ProcessorArchitecture.None://An unknown or unspecified combination of processor and bits-per-word.
                        {
                            throw new System.InvalidOperationException("Please create an issue for your architecture to be supported.");
                        }
                    case System.Reflection.ProcessorArchitecture.MSIL://Neutral with respect to processor and bits-per-word.
                        //Should follow the X86 style
                    case System.Reflection.ProcessorArchitecture.X86://A 32-bit Intel processor, either native or in the Windows on Windows environment on a 64-bit platform (WOW64).
                        {
                            if (false == Machine.IsX86()) throw new System.InvalidOperationException("Did not detect an x86 Machine");
                            break;
                        }
                    case System.Reflection.ProcessorArchitecture.IA64:
                    case System.Reflection.ProcessorArchitecture.Amd64:
                        {
                            if (false == Machine.IsX64()) throw new System.InvalidOperationException("Did not detect an x64 Machine");
                            break;
                        }
                    case System.Reflection.ProcessorArchitecture.Arm:
                        {
                            if (false == Machine.IsArm()) throw new System.InvalidOperationException("Did not detect an Arm Machine");
                            break;
                        }
                }

                //Environment check?
                //http://superuser.com/questions/305901/possible-values-of-processor-architecture

                //Interop
                //http://stackoverflow.com/questions/767613/identifying-the-cpu-architecture-type-using-c-sharp/25284569#25284569

                #endregion

                #region Check Bit Pattern Space

                //Caclulcate the pattern size until the value approaches 1 again (compare against 0 should be faster)
                while (1 >> ++m_BitPatternSize != 1 && m_BitPatternSize > 0) ;

                #endregion               

                #region Determine BinaryRepresentation

                //Todo, branchless...

                switch ((m_SystemBinaryRepresentation = Common.Binary.Zero != (Common.Binary.One & -Common.Binary.One) ?
                            (Common.Binary.Three & -Common.Binary.One) == Common.Binary.One ?
                                        BinaryRepresentation.SignedMagnitude : BinaryRepresentation.TwosComplement
                        : BinaryRepresentation.OnesComplement))
                {
                    case BinaryRepresentation.TwosComplement:
                        {
                            if (false == IsTwosComplement()) throw new System.InvalidOperationException("Did not correctly detect BinaryRepresentation");

                            break;
                        }
                    case BinaryRepresentation.OnesComplement:
                        {
                            if (false == IsOnesComplement()) throw new System.InvalidOperationException("Did not correctly detect BinaryRepresentation");

                            break;
                        }
                    case BinaryRepresentation.SignedMagnitude:
                        {
                            if (false == IsSignedMagnitude()) throw new System.InvalidOperationException("Did not correctly detect BinaryRepresentation");

                            break;
                        }
                    default:
                        {
                            throw new System.NotSupportedException("Create an Issue for your Architecture to be supported.");
                        }
                }

                #endregion
            }
        }

        #endregion

        //See also
        //http://apichange.codeplex.com/SourceControl/changeset/view/76c98b8c7311#ApiChange.Api/src/Introspection/CorFlagsReader.cs
    }
}

namespace Media.UnitTests
{
    internal class MachineUnitTests
    {
        public void ShowBitPatternSize()
        {
            System.Console.WriteLine("BitPatternSize:" + Media.Common.Machine.BitPatternSize);
        }

        public void ShowCodeCompilation()
        {
            System.Console.WriteLine("CodeType:" + Media.Common.Machine.CodeType);

            System.Console.WriteLine("MachineType:" + Media.Common.Machine.MachineType);
        }

        public void ShowCpuType()
        {
            System.Console.WriteLine("IsX86:" + Media.Common.Machine.IsX86());

            System.Console.WriteLine("IsX64:" + Media.Common.Machine.IsX64());

            System.Console.WriteLine("IsArm:" + Media.Common.Machine.IsArm());
        }

        public static void ShowBinaryRepresentation()
        {
            System.Console.WriteLine("Detected a: " + Media.Common.Machine.SystemBinaryRepresentation.ToString() + ' ' + Media.Common.Machine.SystemBinaryRepresentation.GetType().Name + " System.");

            //Todo, test conversions
        }
    
        //public void TestSignExtendedShift()
        //{
        //    //throw new System.NotImplementedException();
        //}

        //public void TestLogicalShift()
        //{
        //    //throw new System.NotImplementedException();
        //}

        //public void TestCircularShift()
        //{
        //    //throw new System.NotImplementedException();
        //}

        //public void TestReverseShift()
        //{
        //    //throw new System.NotImplementedException();
        //}
    }
}
