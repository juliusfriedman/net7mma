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

#region References

//A Majority of this code comes from 
//https://github.com/qwertie/LoycCore/blob/master/Loyc.Essentials/Math/Math128.cs
//https://github.com/qwertie/LoycCore/blob/master/Loyc.Essentials/Math/MathEx.cs

//Some from here
//http://stackoverflow.com/questions/5558492/divide-by-10-using-bit-shifts
//http://stackoverflow.com/questions/6357038/is-multiplication-and-division-using-shift-operators-in-c-actually-faster

//Other parts from here
//http://www.hackersdelight.org/divcMore.pdf

//Divide by 15 and Align
//http://www.catch22.net/tuts/c-c-tricks

//And then there is of course yours truly

#endregion

#region WARNINGLIST

#define WARNINGLIST //CS1692, CA1724
#if WARNINGLIST
#if DEBUG
#define CA1724
#endif
#endif
#undef WARNINGLIST

#endregion

#pragma warning disable 1691, 1692

#pragma warning disable CA1724

//Todo, Inlining, Vectors etc.
//Use from Number and Processor

namespace Media.Concepts.Math
{
    /// <summary>
    /// Some processors handle <see ref="https://en.wikipedia.org/wiki/Division_(mathematics)">Division</see> very slowly either due to lack of optomization of the generated machine code or otherwise.
    /// These classes help to eliminate such nuances at a higher level, they may or may not help you depending on the type of processor you have and should only be used where they know they are needed.
    /// In general they will probably be slightly faster than normal math operations of the same type but will have less precision.
    /// The maximum and minimum precision should eventually be documented via some type of Attribute or otherwise.    
    /// </summary>
    /// <remarks>
    /// If you need to squeeze performance out of your application that badly then <see cref="Media.Concepts.Hardware"/> also.
    /// </remarks>
    public sealed class Math
    {
        #region DivideBy15Fast

        void DivideBy15Fast(ref uint x)
        {
            uint s,t;

            s = x >> 4;

            t = (x & 0xf) + s + 1;

            t += t >> 4;

            t = (t + (t >> 8) >> 4) + s;
        }

        void DivideBy15Fast(ref ushort x)
        {
            ++x;
            
            x += (ushort)(x << 4);

            x += (ushort)(x >> 8);

            x += (ushort)(x >> 16);

            x >>= 8;
        }

        void DivideBy15Fast(ref long x)
        {
            x = (x >> 4) + (x >> 8);

            x += x >> 8;

            x += x >> 8;
        }

        #endregion

        #region DivideBy10Fast

        //Divide by 10 fast, valid for 0 - 1028
        [System.CLSCompliant(false)]
        public static void DivideBy10(ref uint source)
        {
            source *= 205;

            source >>= 11;
        }

        public static void DivideBy10(ref int source)
        {
            source *= 205;

            source >>= 11;
        }

        [System.CLSCompliant(false)]
        public static void DivideBy10(ref ulong source)
        {
            source *= 205;

            source >>= 11;
        }

        public static void DivideBy10(ref long source)
        {
            source *= 205;

            source >>= 11;
        }

        #endregion

        #region MultiplyBy10Fast

        [System.CLSCompliant(false)]
        public static void Mul10Fast(ref uint source)
        {
            source += source * 4;

            source <<= 1;
        }

        #endregion

        #region Multiplication

        [System.CLSCompliant(false)]
        public static ulong Multiply(ulong a, ulong b, out ulong resultHi)
        {
            uint aH = (uint)(a >> 32), aL = (uint)a;
            uint bH = (uint)(b >> 32), bL = (uint)b;
            // Multiply a*b: (aH*EXP + aL) * (bH*EXP + bL), where EXP=1<<32
            //   Expand a*b: aH*bH*EXP*EXP + (aL*bH + aH*bL)*EXP + aL*bL
            //    High part: aH*bH
            //     Mid part: aL*bH + aH*bL
            //     Low part: aL*bL
            ulong mid1 = (ulong)aL * bH;
            ulong mid = (ulong)aH * bL + mid1;
            resultHi = (ulong)aH * bH + (mid >> 32);
            ulong lo1 = (ulong)aL * bL;
            ulong lo = lo1 + (mid << 32);

            if (mid < mid1)
                resultHi += (1 << 32); // mid (aL * bH + aH * bL) overflowed
            if (lo < lo1)
                resultHi++; // (aL * bL) + (lower half of mid << 32) overflowed
            return lo;
        }

        [System.CLSCompliant(false)]
        public static ulong Multiply(long a, long b, out long resultHi)
        {
            bool negative;
            if ((negative = (a < 0)))
                a = -a;
            if (b < 0)
            {
                b = -b;
                negative = !negative;
            }
            ulong resultHiU, resultLo = Multiply((ulong)a, (ulong)b, out resultHiU);
            resultHi = (long)resultHiU;
            if (negative)
                Negate(ref resultHi, ref resultLo);
            return resultLo;
        }

        #endregion

        #region Division (the really difficult one)

        /// <summary>Divides an signed 128-bit number by a signed 64-bit 
        /// number to produce a 64-bit result.</summary>
        /// <returns>Returns the division result (known as the "quotient"). If the 
        /// result is too large to represent, long.MinValue or long.MaxValue is 
        /// returned instead.</returns>
        /// <inheritdoc cref="Divide(long, ulong, long, out long, out long, bool)"/>
        public static long Divide(long aH, ulong aL, long b, out long remainder, bool roundDown)
        {
            long resultHi;
            ulong resultLo = Divide(aH, aL, b, out resultHi, out remainder, roundDown);
            if (resultHi == ((long)resultLo >= 0 ? 0 : -1L))
                return (long)resultLo;
            else
                // result does not fit in 64 bits
                return resultHi >= 0 ? long.MaxValue : long.MinValue;
        }

        /// <summary>Divides an signed 128-bit number by a signed 64-bit 
        /// number to produce a 128-bit result.</summary>
        /// <param name="roundDown">If true, the result is rounded down, instead
        /// of being rounded toward zero, which changes the remainder and 
        /// quotient if a is negative but b is positive.</param>
        /// <remarks>
        /// When dividing a negative number by a positive number, it is 
        /// conventionally rounded toward zero. Consequently, the remainder is zero 
        /// or negative to satisfy the standard integer division equation 
        /// <c>a = b*q + r</c>, where q is the quotient (result) and r is the 
        /// remainder.
        /// <para/>
        /// This is not always what you want. So, if roundDown is true, the result 
        /// is rounded down instead of toward zero. This ensures that if 'a' is 
        /// negative and 'b' is positive, the remainder is positive too, a fact 
        /// which is useful for modulus arithmetic. The table below illustrates 
        /// the difference:
        /// <pre>
        /// inputs   | standard  | roundDown
        ///          |  output   |  output  
        ///  a   b   |   q   r   |   q   r
        /// --- ---  |  --- ---  |  --- ---
        ///  7   3   |   2   1   |   2   1
        /// -7   3   |  -2  -1   |  -3   2
        ///  7  -3   |  -2   1   |  -3  -2
        /// -7  -3   |   2  -1   |   2  -1
        /// </pre>
        /// </remarks>
        /// <inheritdoc cref="Divide(ulong, ulong, ulong, out ulong, out ulong)"/>
        [System.CLSCompliant(false)]
        public static ulong Divide(long aH, ulong aL, long b, out long resultHi, out long remainder, bool roundDown)
        {
            bool negativeA, negativeB;
            if ((negativeA = (aH < 0)))
                Negate(ref aH, ref aL);
            if ((negativeB = (b < 0)))
                b = -b;

            ulong resultHiU, remainderU;
            ulong resultLo = Divide((ulong)aH, aL, (ulong)b, out resultHiU, out remainderU);
            resultHi = (long)resultHiU;
            if (negativeA == negativeB)
            {
                remainder = (negativeA ? -(long)remainderU : (long)remainderU);
                return resultLo;
            }
            else if (roundDown && remainderU != 0)
            {
                remainderU = (ulong)b - remainderU;
                remainder = (negativeB ? -(long)remainderU : (long)remainderU);
                // ~result is equivalent to (-result - 1)
                resultHi = ~resultHi;
                resultLo = ~resultLo;
                return resultLo;
            }
            else
            {
                remainder = (negativeA ? -(long)remainderU : (long)remainderU);
                Negate(ref resultHi, ref resultLo);
                return resultLo;
            }
        }

        /// <summary>Divides an signed 128-bit number by a signed 64-bit 
        /// number to produce a 64-bit result.</summary>
        /// <remarks>If the result did not fit in 64 bits, this method returns 
        /// ulong.MaxValue.</remarks>
        /// <inheritdoc cref="Divide(ulong, ulong, ulong, out ulong, out ulong)"/>
        [System.CLSCompliant(false)]
        public static ulong Divide(ulong aH, ulong aL, ulong b, out ulong remainder)
        {
            if (b <= aH) // overflow
                return (remainder = ulong.MaxValue);

            ulong resultHi, resultLo = Divide(aH, aL, b, out resultHi, out remainder);
            if (resultHi != 0)
                return ulong.MaxValue;
            return resultLo;
        }

        /// <summary>Divides an unsigned 128-bit number by an unsigned 64-bit 
        /// number to produce a 128-bit result.</summary>
        /// <param name="aH">High 64 bits of the dividend.</param>
        /// <param name="aL">Low 64 bits of the dividend.</param>
        /// <param name="b">The divisor.</param>
        /// <param name="resultHi">High 64 bits of result.</param>
        /// <param name="remainder">Remainder of the division.</param>
        /// <returns>The low 64 bits of the result (known as the "quotient").</returns>
        /// <exception cref="DivideByZeroException">b was zero.</exception>
        [System.CLSCompliant(false)]
        public static ulong Divide(ulong aH, ulong aL, ulong b, out ulong resultHi, out ulong remainder)
        {
            if (aH == 0)
            {
                // 64/64-bit division: use compiler intrinsic
                resultHi = 0;
                remainder = aL % b;
                return aL / b;
            }
            if ((b >> 32) == 0)
            {
                // 128/32-bit division
                uint bL = (uint)b;

                // Optimize for b=1 and b=2
                if (bL <= 2)
                {
                    if (bL == 2)
                    {
                        remainder = aL & 1;
                        resultHi = ShiftRightFast(aH, ref aL, 1);
                        return aL;
                    }
                    if (bL == 1)
                    {
                        remainder = 0;
                        resultHi = aH;
                        return aL;
                    }
                    throw new System.DivideByZeroException();
                }

                uint a4;
                ulong a3, a2, a1;
                uint result4, result3, result2, result1;
                uint r;

                // There are obvious machine-language optimizations here...
                // I hope the JIT is smart enough to see them.
                a4 = (uint)(aH >> 32);
                if (a4 == 0)
                    r = result4 = 0;
                else
                {
                    result4 = a4 / bL;
                    r = a4 % bL;
                }

                a3 = ((ulong)r << 32) + (uint)aH;
                result3 = (uint)(a3 / bL);
                r = (uint)(a3 % bL);

                a2 = ((ulong)r << 32) + (uint)(aL >> 32);
                result2 = (uint)(a2 / bL);
                r = (uint)(a2 % bL);

                a1 = ((ulong)r << 32) + (uint)aL;
                result1 = (uint)(a1 / bL);
                r = (uint)(a1 % bL);

                resultHi = (ulong)(result4 << 32) + result3;
                remainder = r;
                return (ulong)(result2 << 32) + result1;
            }
            else
            {
                System.Diagnostics.Debug.Assert(aH != 0);
                int iterations = 128;

                // Optimization 1: skip loop iterations that have no effect
                if ((aH >> 32) == 0)
                {
                    aH = ShiftLeftFast(aH, ref aL, 32);
                    iterations -= 32;
                }
                if (aH < (1 << (64 - 16)))
                {
                    aH = ShiftLeftFast(aH, ref aL, 16);
                    iterations -= 16;
                }
                if (aH < (1 << (64 - 8)))
                {
                    aH = ShiftLeftFast(aH, ref aL, 8);
                    iterations -= 8;
                }
                if (aH < (1 << (64 - 4)))
                {
                    aH = ShiftLeftFast(aH, ref aL, 4);
                    iterations -= 4;
                }
                if (aH < (1 << (64 - 2)))
                {
                    aH = ShiftLeftFast(aH, ref aL, 2);
                    iterations -= 2;
                }

                // Optimization 2: get a head start by shifting some bits into 
                // 'remainder', but not enough to change the outcome.
                System.Diagnostics.Debug.Assert(b > uint.MaxValue);
                int skip = Log2Floor((uint)(b >> 32)) + 32;
                iterations -= skip;
                remainder = ShiftLeftEx(ref aH, ref aL, skip);

                // The core division algorithm is based on the assembly code in 
                // http://www.codeproject.com/KB/recipes/MulDiv64.aspx
                // Unoptimized, it required an iteration for every bit of the input 
                // (a). The way it works is slightly subtle. The dividend 'a' 
                // slowly becomes the output as the loop progresses. The original 
                // bits of 'a' are shifted left one-by-one into 'remainder', and 'a'
                // is shifted 128 times so it eventually disappears. Meanwhile, the 
                // bits of the result are determined one-at-a-time and are shifted 
                // in as the new low bits of 'a'. In general, this is more efficient 
                // than using separate variables for the dividend and the result.
                System.Diagnostics.Debug.Assert(remainder < b);
                for (; iterations != 0; iterations--)
                {
                    ulong oldH = aH, oldL = aL, oldR = remainder;
                    remainder <<= 1;
                    aH <<= 1;
                    if (aH < oldH) // aH overflowed?
                        ++remainder;
                    aL <<= 1;
                    if (aL < oldL) // aL overflowed?
                        ++aH;
                    if (remainder < oldR || remainder >= b)
                    {
                        remainder -= b;
                        if (++aL == 0) // aL overflowed?
                            ++aH;
                    }
                }

                resultHi = aH;
                return aL;
            }
        }

        #endregion

        #region Shift left

        /// <summary>Shifts a 128-bit value left.</summary>
        /// <param name="aH">High 64 bits</param>
        /// <param name="aL">Low 64 bits</param>
        /// <param name="amount">Number of bits to shift.</param>
        /// <returns>The new value of aH</returns>
        /// <remarks>The convention is that signed numbers use Int64 for aH and 
        /// unsigned numbers used UInt64 for aH. The fact that aH is not passed 
        /// by reference makes it easier to shift a signed number left by casting 
        /// aH to UInt64. The cast would not be allowed if passing by reference.
        /// Of course, right shift, on the other hand, requires two separate 
        /// methods since it behaves differently for signed and unsigned inputs.
        /// <para/>
        /// This method does not allow shifting by a negative amount. The reason 
        /// is that there is only one ShiftLeft, so if the amount is negative, 
        /// it's not known whether a signed or unsigned ShiftRight is intended.
        /// </remarks>
        public static ulong ShiftLeft(ulong aH, ref ulong aL, int amount)
        {
            System.Diagnostics.Debug.Assert(amount >= 0);
            if (amount < 64)
                return ShiftLeftFast(aH, ref aL, amount);
            else
            {
                if (amount >= 128)
                    aH = 0;
                else
                    aH = aL << (amount - 64);
                aL = 0;
            }
            return aH;
        }

        /// <summary>Variation of ShiftLeft() for cases when you know 64 > amount >= 0.</summary>
        public static ulong ShiftLeftFast(ulong aH, ref ulong aL, int amount)
        {
            System.Diagnostics.Debug.Assert((uint)amount < 64u);
            aH = (aH << amount) + (aL >> (64 - amount));
            aL <<= amount;
            return aH;
        }

        /// <summary>Shifts a 128-bit value left and saves the overflowed bits.</summary>
        /// <param name="aH">High 64 bits</param>
        /// <param name="aL">Low 64 bits</param>
        /// <param name="amount">Number of bits to shift. Negative amounts are not permitted.</param>
        /// <returns>The bits that were shifted off the left side.</returns>
        /// <remarks>Asserts that amount > 0 (no exception)</remarks>
        public static ulong ShiftLeftEx(ref ulong aH, ref ulong aL, int amount)
        {
            System.Diagnostics.Debug.Assert(amount >= 0);
            ulong overflow;
            if (amount < 64)
            {
                ulong newL = (aL << amount);
                ulong newH = (aH << amount) + (aL >> (64 - amount));
                overflow = (aH >> (64 - amount));
                aL = newL;
                aH = newH;
            }
            else if (amount < 128)
            {
                overflow = aL >> (128 - amount);
                aH = aL << (amount - 64);
                aL = 0;
            }
            else if (amount < 192)
            {
                overflow = (aL << (amount - 128));
                aH = aL = 0;
            }
            else
            {
                aH = aL = 0;
                return 0;
            }
            return overflow;
        }
        public static ulong RotateLeft(ulong aH, ref ulong aL, int amount)
        {
            aL += ShiftLeftEx(ref aH, ref aL, amount);
            return aH;
        }

        #endregion

        #region Shift right

        /// <summary>Shifts a 128-bit value right.</summary>
        /// <param name="aH">High 64 bits</param>
        /// <param name="aL">Low 64 bits</param>
        /// <param name="amount">Number of bits to shift.</param>
        /// <returns>The new value of aH</returns>
        /// <remarks>This method, unlike ShiftLeft(), allows shifting by a negative 
        /// amount, which is translated to a left shift.
        /// <para/>
        /// TODO: ShiftRightEx</remarks>
        public static ulong ShiftRight(ulong aH, ref ulong aL, int amount)
        {
            if (amount < 0)
                return ShiftLeft(aH, ref aL, -amount);
            if (amount < 64)
                return ShiftRightFast(aH, ref aL, amount);
            else
            {
                if (amount >= 128)
                    aL = 0;
                else
                    aL = aH >> (amount - 64);
                aH = 0;
            }
            return aH;
        }

        /// <summary>Variation of ShiftRight() for cases when you know 64 > amount >= 0.</summary>
        private static ulong ShiftRightFast(ulong aH, ref ulong aL, int amount)
        {
            System.Diagnostics.Debug.Assert((uint)amount < 64u);
            aL = (aL >> amount) + (aH << (64 - amount));
            return aH >> amount;
        }

        /// <inheritdoc cref="ShiftRight(ulong, ref ulong, int)"/>
        public static long ShiftRight(long aH, ref ulong aL, int amount)
        {
            if (amount < 0)
                return (long)ShiftLeft((ulong)aH, ref aL, -amount);
            if (amount < 64)
                return ShiftRightFast(aH, ref aL, amount);
            else if (amount < 127)
            {
                aL = (ulong)(aH >> (amount - 64));
                aH >>= 63; // keep only the sign
            }
            else
                aL = (ulong)(aH >>= 63);
            return aH;
        }

        /// <summary>Variation of ShiftRight() for cases when you know 64 > amount >= 0.</summary>
        private static long ShiftRightFast(long aH, ref ulong aL, int amount)
        {
            System.Diagnostics.Debug.Assert((uint)amount < 64u);
            aL = (aL >> amount) + ((ulong)aH << (64 - amount));
            return aH >> amount;
        }

        #endregion

        #region Addition and subtraction

        /// <summary>Adds a 64-bit number to a 128-bit number.</summary>
        /// <param name="aH">High 64 bits of 128-bit number</param>
        /// <param name="aL">Low 64 bits of 128-bit number</param>
        /// <param name="amount">Amount to add</param>
        /// <returns>The high 64 bits of the result.</returns>
        public static ulong Add(ulong aH, ref ulong aL, ulong amount)
        {
            ulong oldL = aL;
            aL += amount;
            if (oldL > aL) // 64-bit overflow
                aH++;
            return aH;
        }

        /// <summary>Subtracts a 64-bit number from a 128-bit number.</summary>
        /// <param name="aH">High 64 bits of 128-bit number</param>
        /// <param name="aL">Low 64 bits of 128-bit number</param>
        /// <param name="amount">Amount to subtract</param>
        /// <returns>The high 64 bits of the result.</returns>
        public static ulong Subtract(ulong aH, ref ulong aL, ulong amount)
        {
            ulong oldL = aL;
            aL -= amount;
            if (aL > oldL) // 64-bit undeflow
                aH--;
            return aH;
        }

        /// <inheritdoc cref="Add(ulong, ref ulong, ulong)"/>
        public static long Add(long aH, ref ulong aL, long amount)
        {
            if (amount >= 0)
                return (long)Add((ulong)aH, ref aL, (ulong)amount);
            else
                return (long)Subtract((ulong)aH, ref aL, (ulong)(-amount));
        }

        /// <inheritdoc cref="Subtract(ulong, ref ulong, ulong)"/>
        public static long Subtract(long aH, ref ulong aL, long amount)
        {
            if (amount >= 0)
                return (long)Subtract((ulong)aH, ref aL, (ulong)amount);
            else
                return (long)Add((ulong)aH, ref aL, (ulong)(-amount));
        }

        #endregion

        #region Increment and other simple operations

        private static ulong Increment(ulong resultHi, ref ulong resultLo)
        {
            if (++resultLo == 0)
                ++resultHi;
            return resultHi;
        }
        private static ulong Decrement(ulong resultHi, ref ulong resultLo)
        {
            if ((long)--resultLo == -1L)
                --resultHi;
            return resultHi;
        }
        public static void Negate(ref long aH, ref ulong aL)
        {
            aH = ~aH;
            if ((aL = (ulong)(-(long)aL)) == 0)
                aH++;
        }
        public static long SignExtend(long a)
        {
            if (a < 0) a = -1;
            return a;
        }

        #endregion

        #region MulShift
        /// <summary>Multiplies two integers, internally producing a double-size 
        /// result so that overflow is not possible, then divides the result by the 
        /// specified power of two using a right shift.</summary>
        /// <returns>a * mulBy >> shiftBy, without overflow during multiplication.</returns>
        /// <remarks>This method does not handle the case that the result is too
        /// large to fit in the original data type.</remarks>
        public static int MulShift(int a, int mulBy, int shiftBy)
        {
            return (int)((long)a * mulBy >> shiftBy);
        }
        /// <inheritdoc cref="MulShift(int,int,int)"/>
        public static uint MulShift(uint a, uint mulBy, int shiftBy)
        {
            return (uint)((ulong)a * mulBy >> shiftBy);
        }
        /// <inheritdoc cref="MulShift(int,int,int)"/>
        public static long MulShift(long a, long mulBy, int shiftBy)
        {
            long rH;
            ulong rL = Multiply(a, mulBy, out rH);
            ShiftRight(rH, ref rL, shiftBy);
            return (long)rL;
        }
        /// <inheritdoc cref="MulShift(int,int,int)"/>
        public static ulong MulShift(ulong a, ulong mulBy, int shiftBy)
        {
            ulong rH;
            ulong rL = Multiply(a, mulBy, out rH);
            ShiftRight(rH, ref rL, shiftBy);
            return rL;
        }
        #endregion

        #region MulDiv
        /// <summary>Multiplies two integers, internally producing a double-size 
        /// result so that overflow is not possible, then divides the result by the 
        /// specified number.</summary>
        /// <param name="remainder">The remainder of the division is placed here. 
        /// The remainder is computed properly even if the main result overflows.</param>
        /// <returns>a * mulBy / divBy, without overflow during multiplication.</returns>
        /// <remarks>If the final result does not fit in the original data type, 
        /// this method returns largest possible value of the result type 
        /// (int.MaxValue, or int.MinValue if the overflowing result is negative).
        /// </remarks>
        public static int MulDiv(int a, int mulBy, int divBy, out int remainder)
        {
            long m = (long)a * mulBy;
            remainder = (int)(m % divBy);
            return (int)(m / divBy);
        }
        /// <inheritdoc cref="MulDiv(int,int,int,out int)"/>
        /// <remarks>If the final result does not fit in the original data type, 
        /// this method returns largest possible value of the result type 
        /// (uint.MaxValue).</remarks>
        public static uint MulDiv(uint a, uint mulBy, uint divBy, out uint remainder)
        {
            ulong m = (ulong)a * mulBy;
            remainder = (uint)(m % divBy);
            return (uint)(m / divBy);
        }
        /// <inheritdoc cref="MulDiv(int,int,int,out int)"/>
        /// <remarks>If the final result does not fit in the original data type, 
        /// this method returns largest possible value of the result type 
        /// (long.MaxValue, or long.MinValue if the overflowing result is negative).
        /// </remarks>
        public static long MulDiv(long a, long mulBy, long divBy, out long remainder)
        {
            long mH;
            ulong mL = Multiply(a, mulBy, out mH);
            return Divide(mH, mL, divBy, out remainder, false);
        }
        /// <inheritdoc cref="MulDiv(int,int,int,out int)"/>
        /// <remarks>If the final result does not fit in the original data type, 
        /// this method returns largest possible value of the result type 
        /// (ulong.MaxValue).</remarks>
        public static ulong MulDiv(ulong a, ulong mulBy, ulong divBy, out ulong remainder)
        {
            ulong mH;
            ulong mL = Multiply(a, mulBy, out mH);
            return Divide(mH, mL, divBy, out remainder);
        }

        /// <inheritdoc cref="MulDiv(int, int, int, out int)"/>
        public static int MulDiv(int a, int mulBy, int divBy)
        {
            return (int)((long)a * mulBy / divBy);
        }
        /// <inheritdoc cref="MulDiv(uint, uint, uint, out uint)"/>
        public static uint MulDiv(uint a, uint mulBy, uint divBy)
        {
            return (uint)((ulong)a * mulBy / divBy);
        }
        /// <inheritdoc cref="MulDiv(long, long, long, out long)"/>
        public static long MulDiv(long a, long mulBy, long divBy)
        {
            long mH, remainder;
            ulong mL = Multiply(a, mulBy, out mH);
            return Divide(mH, mL, divBy, out remainder, false);
        }
        /// <inheritdoc cref="MulDiv(ulong, ulong, ulong, out ulong)"/>
        public static ulong MulDiv(ulong a, ulong mulBy, ulong divBy)
        {
            ulong mH, remainder;
            ulong mL = Multiply(a, mulBy, out mH);
            return Divide(mH, mL, divBy, out remainder);
        }
        #endregion

        #region Mod

        public static double Mod(double x, double y)
        {
            double m = x % y;
            return m + (m < 0 ? y : 0);
        }
        public static float Mod(float x, float y)
        {
            float m = x % y;
            return m + (m < 0 ? y : 0);
        }
        public static int Mod(int x, int y)
        {
            int m = x % y;
            return m + ((m >> 31) & y);
        }
        public static long Mod(long x, long y)
        {
            long m = x % y;
            return m + (m < 0 ? y : 0);
        }

        #endregion

        #region Log2Floor and NextPowerOf2
        /// <inheritdoc cref="Log2Floor(int)"/>
        [System.CLSCompliant(false)]
        public static int Log2Floor(ref uint x)
        {
            x |= (x >> 1);
            x |= (x >> 2);
            x |= (x >> 4);
            x |= (x >> 8);
            x |= (x >> 16);
            return (Common.Binary.BitsSet(ref x) - 1);
        }
        /// <summary>
        /// Returns the floor of the base-2 logarithm of x. e.g. 1024 -> 10, 1000 -> 9
        /// </summary><remarks>
        /// The return value is -1 for an input that is zero or negative.
        /// <para/>
        /// Some processors have a dedicated instruction for this operation, but
        /// the .NET framework provides no access to it.
        /// </remarks>
        public static int Log2Floor(int x)
        {
            if (x < 0)
                return -1;
            return Log2Floor((uint)x);
        }

        [System.CLSCompliant(false)]
        /// <inheritdoc cref="Log2Floor(int)"/>
        public static int Log2Floor(ref ulong x)
        {
            uint xHi = (uint)(x >> 32);
            if (xHi != 0)
                return 32 + Log2Floor(xHi);
            return Log2Floor((uint)x);
        }

        /// <inheritdoc cref="Log2Floor(int)"/>
        public static int Log2Floor(long x)
        {
            if (x < 0)
                throw new System.ArgumentException("x must be greater than or equal to zero.");
            ulong unsigned = Concepts.Classes.Unsafe.UInt64ToInt64Bits(ref x);
            return Log2Floor(ref unsigned);
        }
        
        /// <summary>Gets the next higher power of 2, e.g. 4=>8, 13=>16.</summary>
        /// <remarks>For negative values of x, NextPowerOf2((uint)x) is 0.</remarks>
        [System.CLSCompliant(false)]
        public static uint NextPowerOf2(ref uint x)
        {
            x |= (x >> 1);
            x |= (x >> 2);
            x |= (x >> 4);
            x |= (x >> 8);
            x |= (x >> 16);
            return x + 1u;
        }
        /// <summary>Gets the next higher power of 2, e.g. 4=>8, 13=>16.</summary>
        /// <remarks>For negative values of (long)x, NextPowerOf2((ulong)x) is 0.</remarks>
        [System.CLSCompliant(false)]
        public static ulong NextPowerOf2(ref ulong x)
        {
            x |= (x >> 1);
            x |= (x >> 2);
            x |= (x >> 4);
            x |= (x >> 8);
            x |= (x >> 16);
            x |= (x >> 32);
            return x + 1u;
        }
        /// <summary>Calls <see cref="NextPowerOf2(uint)"/>.</summary>
        public static int NextPowerOf2(int x)
        {
            return (int)NextPowerOf2((uint)x);
        }
        /// <summary>Calls <see cref="NextPowerOf2(ulong)"/>.</summary>
        public static int NextPowerOf2(long x)
        {
            return (int)NextPowerOf2((uint)x);
        }

        #endregion
    }
}

#pragma warning restore 1691, 1692, CA1724

//#pragma warning disable 13213

#region WARNINGLIST

#if !DEBUG
#if CA1724
#if DEBUG
    //
#else
#undef CA1724
#endif
#endif
#endif

#endregion
