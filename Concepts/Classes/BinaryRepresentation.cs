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

    public sealed class BinaryRepresentations
    {
        #region Methods

        public static int OnesComplement(int value) { return OnesComplement(ref value); }

        [System.CLSCompliant(false)]
        public static int OnesComplement(ref int value) { return (~value); }

        public static int TwosComplement(int value) { return TwosComplement(ref value); }

        [System.CLSCompliant(false)]
        public static int TwosComplement(ref int value) { unchecked { return (~value + Common.Binary.One); } }

        public static int SignedMagnitude(int value) { int sign; return SignedMagnitude(ref value, out sign); }

        [System.CLSCompliant(false)]
        public static int SignedMagnitude(ref int value) { int sign; return SignedMagnitude(ref value, out sign); }

        /// <summary>
        /// Converts value to twos complement and returns the signed magnitude representation outputs the sign
        /// </summary>
        /// <param name="value"></param>
        /// <param name="sign"></param>
        /// <returns></returns>
        [System.CLSCompliant(false)]
        public static int SignedMagnitude(ref int value, out int sign)
        {
            unchecked
            {

                //If the sign is -1 then convert to twos complement
                //if ((sign = Math.Sign(value)) == -Binary.Ūnus) value = TwosComplement(ref value);

                if ((sign = Common.Binary.Sign(value)) == -Common.Binary.One) value = TwosComplement(ref value);

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
        public static bool IsTwosComplement()
        {
            //return Convert.ToSByte(byte.MaxValue.ToString(Media.Common.Extensions.String.StringExtensions.HexadecimalFormat), Binary.Sēdecim) == -Binary.Ūnus;

            return unchecked((sbyte)byte.MaxValue == -Media.Common.Binary.One);
        }

        /// <summary>
        /// Indicates if the architecture utilizes one's complement binary representation
        /// </summary>
        /// <returns>True if ones's complement is used, otherwise false</returns>
        public static bool IsOnesComplement()
        {
            //return Convert.ToSByte(sbyte.MaxValue.ToString(Media.Common.Extensions.String.StringExtensions.HexadecimalFormat), Binary.Sēdecim) == -Binary.Ūnus;

            return unchecked(sbyte.MaxValue == -Media.Common.Binary.One);
        }

        /// <summary>
        /// Indicates if the architecture utilizes sign and magnitude representation
        /// </summary>
        /// <returns>True if sign and magnitude representation is used, otherwise false</returns>
        public static bool IsSignedMagnitude()
        {
            return unchecked(((Common.Binary.Three & -Common.Binary.One) == Common.Binary.One)); //&& false == IsTwosComplement

            //e.g. (3 & -1) == 3, where as Media.Common.Binary.BitwiseAnd(-3, 1) == 1
        }

        //http://en.wikipedia.org/wiki/Signed_number_representations
        //Excess, Base, Biased

        #endregion

        //readonly ValueType
        /// <summary>
        /// The <see cref="BinaryRepresentation"/> of the current architecture used for the <see cref="int"/> type.
        /// </summary>
        public static readonly BinaryRepresentation SystemBinaryRepresentation = BinaryRepresentation.Unknown;
        
        #region Constructor

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        static BinaryRepresentations()
        {
            //ensure not already called.
            if (SystemBinaryRepresentation != BinaryRepresentation.Unknown) return;

            #region Determine BinaryRepresentation

            //Todo, branchless...

            switch ((SystemBinaryRepresentation = Common.Binary.Zero != (Common.Binary.One & -Common.Binary.One) ?
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

        #endregion
    }
}
