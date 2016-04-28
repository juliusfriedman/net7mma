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

namespace Media.Concepts.Classes
{
    /// <summary>
    /// Provides functionality which can be though of based on machine concepts
    /// </summary>
    /// <note><see href="http://blogs.msdn.com/b/vcblog/archive/2012/10/26/10362875.aspx">Hello ARM</see></note>
    /// <note><see href="https://msdn.microsoft.com/en-us/magazine/jj553518.aspx">.NET Development for ARM Processors</see></note>
    public static class Machine
    {
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
        public class MachineShift : Shift
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

        #region Fields

        static readonly System.Type Type = typeof(Machine);

        static readonly System.Reflection.Assembly Assembly = Type.Assembly;

        static readonly System.Reflection.AssemblyName AssemblyName = Assembly.GetName();

        //https://github.com/NETMF/netmf-interpreter/blob/d28c5365e35fa7c861312b702cde5b73e2ef3808/Framework/Subset_of_CorLib/System/Reflection/AssemblyNameFlags.cs

        /// <summary>
        /// Indicates the Platform the code was compiled for.
        /// Identifies the processor and bits-per-word of the platform targeted by an executable.
        /// </summary>
        static readonly System.Reflection.ProcessorArchitecture AssemblyNameProcessorArchitecture = AssemblyName.ProcessorArchitecture;

        /// <summary>
        /// Indicates the type of machine code produced by JIT when the code is compiled.
        /// </summary>
        public static readonly System.Reflection.PortableExecutableKinds CodeType;

        /// <summary>
        /// Indicates the CPU instructions used by the JIT when the code is compiled.
        /// </summary>
        public static readonly System.Reflection.ImageFileMachine MachineType;

        internal static int m_BitPatternSize = 0;

        /// <summary>
        /// The maximum amount of shifting which can occur before the bit pattern space repeats
        /// </summary>
        public static int BitPatternSize { get { return m_BitPatternSize; } }

        #endregion

        public static bool FiniteBitPattern() { return BitPatternSize > 0; }

        public static bool IsArm()
        {
            //Directly uses the CPU Bit Pattern Space if compilation supports
            return 0 == 1 << Media.Common.Binary.BitsPerInteger;
        }

        public static bool IsX86()
        {
            return 1 == (((uint)1) << Media.Common.Binary.BitsPerInteger);
        }

        public static bool IsX64()
        {
            return 4294967296 == (((ulong)1) << 96); //Should always be 96
        }

        //Detection? (Model, Speed, Stepping, Instruction Support ...)

        static Machine()
        {
            //No overflow anyway
            unchecked
            {
                #region Compilation Check

                //Determine how the code was compiled
                foreach (System.Reflection.Module module in Type.Assembly.Modules)
                {
                    module.GetPEKind(out CodeType, out MachineType);

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

                //Caclulcate the pattern size until the value approaches 1 again
                while (1 >> ++m_BitPatternSize != 1 && m_BitPatternSize <= int.MaxValue) ;

                #endregion               
            }
        }
    }
}

namespace Media.UnitTests
{
    internal class MachineUnitTests
    {
        public void ShowBitPatternSize()
        {
            System.Console.WriteLine("BitPatternSize:" + Media.Concepts.Classes.Machine.BitPatternSize);
        }

        public void ShowCodeCompilation()
        {
            System.Console.WriteLine("CodeType:" + Media.Concepts.Classes.Machine.CodeType);
            
            System.Console.WriteLine("MachineType:" + Media.Concepts.Classes.Machine.MachineType);
        }

        public void ShowCpuType()
        {
            System.Console.WriteLine("IsX86:" + Media.Concepts.Classes.Machine.IsX86());

            System.Console.WriteLine("IsX64:" + Media.Concepts.Classes.Machine.IsX64());

            System.Console.WriteLine("IsArm:" + Media.Concepts.Classes.Machine.IsArm());
        }
    
        public void TestMachineShift()
        {
            //throw new System.NotImplementedException();
        }

        public void TestLogicalShift()
        {
            //throw new System.NotImplementedException();
        }

        public void TestCircularShift()
        {
            //throw new System.NotImplementedException();
        }

        public void TestReverseShift()
        {
            //throw new System.NotImplementedException();
        }
    }
}
