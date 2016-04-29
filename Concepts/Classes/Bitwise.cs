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
namespace Media.Concepts.Classes
{
    //Candidate for integration with Machine if the size is not too much of an increase...

    public sealed class Bitwise
    {
        #region Bitwise

        /// <summary>
        /// Converts the given numbers into equivalent binary representations
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <param name="format">The binary format to convert left and right to</param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static void BitwisePrepare(ref int left, ref int right, Common.Machine.BinaryRepresentation format = Common.Machine.BinaryRepresentation.NoSign)
        {
            switch (format)
            {
                case Common.Machine.BinaryRepresentation.SignedMagnitude:
                    {
                        left = Common.Machine.SignedMagnitude(ref left);

                        right = Common.Machine.SignedMagnitude(ref right);

                        break;
                    }
                case Common.Machine.BinaryRepresentation.TwosComplement:
                    {
                        left = Common.Machine.TwosComplement(ref left);

                        right = Common.Machine.TwosComplement(ref right);

                        break;
                    }
                case Common.Machine.BinaryRepresentation.OnesComplement:
                    {
                        left = Common.Machine.OnesComplement(ref left);

                        right = Common.Machine.OnesComplement(ref right);

                        break;
                    }
                case Common.Machine.BinaryRepresentation.NoSign:
                    {
                        left = (int)((uint)left);

                        right = (int)((uint)right);

                        return;
                    }
                default:
                    {
                        throw new System.NotSupportedException("Create an issue to have your format supported.");
                    }
            }
        }


        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static int BitwiseAnd(int left, int right)
        {
            BitwisePrepare(ref left, ref right);

            return (left & right);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static int BitwiseOr(int left, int right)
        {
            BitwisePrepare(ref left, ref right);

            return (left | right);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static int BitwiseXor(int left, int right)
        {
            BitwisePrepare(ref left, ref right);

            return (left ^ right);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static int BitwiseNand(int left, int right)
        {
            BitwisePrepare(ref left, ref right);

            return ~(BitwiseAnd(left, right));
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static int BitwiseNor(int left, int right)
        {
            BitwisePrepare(ref left, ref right);

            return ~(BitwiseOr(left, right));
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static int BitwiseExclusiveXor(int left, int right)
        {
            return BitwiseOr(BitwiseAnd(~left, right), BitwiseAnd(left, ~right));
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static bool BitwiseExclusiveXor(bool left, bool right)
        {
            return ((false == left) && right) || (left && (false == right));
        }

        #endregion

        //private constructor
        Bitwise() { }
    }
}
