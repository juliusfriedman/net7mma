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

namespace Media.Concepts.Classes
{
    /// <summary>
    /// Provides functionality which can be though of based on machine concepts
    /// </summary>
    public static class Machine
    {
        #region Shift Implementations

        /// <summary>
        /// Provides an API to implement left and right shifting
        /// </summary>
        public abstract class Shift
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
                int length = value.Length;
                byte[] temp = new byte[length];
                if (bitcount >= 8)
                {
                    System.Array.Copy(value, bitcount / 8, temp, 0, length - (bitcount / 8));
                }
                else
                {
                    System.Array.Copy(value, temp, length);
                }
                if (bitcount % 8 != 0)
                {
                    for (int i = 0; i < length; i++)
                    {
                        temp[i] <<= bitcount % 8;
                        if (i < temp.Length - 1)
                        {
                            temp[i] |= (byte)(temp[i + 1] >> 8 - bitcount % 8);
                        }
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
                int length = value.Length;
                byte[] temp = new byte[length];
                if (bitcount >= 8)
                {
                    System.Array.Copy(value, 0, temp, bitcount / 8, length - (bitcount / 8));
                }
                else
                {
                    System.Array.Copy(value, temp, length);
                }

                if (bitcount % 8 != 0)
                {
                    for (int i = length - 1; i >= 0; i--)
                    {
                        temp[i] >>= bitcount % 8;
                        if (i > 0)
                        {
                            temp[i] |= (byte)(temp[i - 1] << 8 - bitcount % 8);
                        }
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
        /// Provides an implementation of CircularShifting
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
        }

        #endregion

        /// <summary>
        /// The maximum amount of shifting which can occur before the bit pattern space repeats
        /// </summary>
        public static readonly int BitPatternSize = -1;

        static Machine()
        {
            int test = 1 >> 1;

            BitPatternSize = 0;

            while(1 != test)
            {
                test = 1 >> ++BitPatternSize;
            }

        }
    }
}
