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

namespace Media.Common
{
    /// <see href="http://www.asciitable.com/">The ASCII Table</see>
    [System.CLSCompliant(true)]
    public sealed class ASCII
    {
        public const byte Space = 0x20,// ` `
            LineFeed = 0x0A, // `\n` => 10 Decimal
            NewLine = 0x0D, // `\r` => 13 Decimal
            EqualsSign = 0x3d, // =
            HyphenSign = 0x2d, // -
            Comma = 0x2c, // ,
            Period = 0x2e, // .
            ForwardSlash = 0x2F, // '/'
            Colon = 0x3a, // :
            SemiColon = 0x3b, // ;
            AtSign = 0x40, // @
            R = 0x52, // 'R' = 82 Decimal
            BackSlash = 0x5C, // '\'
            DoubleQuote = (byte)'"',
            SingleQuote = (byte)'\'',
            Asterisk = (byte)Common.Binary.TheAnswerToEverything;

        //static char[] LineEndingCharacters = System.Text.Encoding.ASCII.GetChars(new byte[] { NewLine, LineFeed });

        /// <summary>
        /// Determines if the given <see cref="char"/> is inclusively within the range of `0-9, A-F and a-f` .
        /// </summary>
        /// <param name="c">the char</param>
        /// <returns>True if the character is within the alloted range, otherwise false.</returns>
        public static bool IsHexDigit(char c) { return IsHexDigit (ref c); }

        /// <summary>
        /// Determines if the given <see cref="char"/> is inclusively within the range of `0-9, A-F and a-f` .
        /// </summary>
        /// <param name="c">the reference to the car</param>
        /// <returns>True if the character is within the alloted range, otherwise false.</returns>
        [System.CLSCompliant(false)] //decimal values used describe 0 - 9,            A - F,                  a - f
        public static bool IsHexDigit(ref char c) { return (c >= 48 && c <= 57) || (c >= 65 && c <= 70) || (c >= 97 && c <= 102); }

        #region Number Extraction

        public static string ExtractPrecisionNumber(string input, char sign = (char)Common.ASCII.Period)
        {
            if (string.IsNullOrWhiteSpace(input)) throw new System.InvalidOperationException("input cannot be null or consist only of whitespace.");

            return ASCII.ExtractPrecisionNumber(input, 0, input.Length, sign);
        }

        public static string ExtractPrecisionNumber(string input, int offset, int length, char sign = (char)Common.ASCII.Period)
        {
            return ASCII.ExtractNumber(input, offset, length, sign);
        }

        public static string ExtractNumber(string input, char? sign = null)
        {
            if (string.IsNullOrWhiteSpace(input)) throw new System.InvalidOperationException("input cannot be null or consist only of whitespace.");

            return ASCII.ExtractNumber(input, 0, input.Length, sign);
        }

        public static string ExtractNumber(string input, int offset, int length, char? sign = null)
        {
            if (string.IsNullOrWhiteSpace(input)) throw new System.InvalidOperationException("input cannot be null or consist only of whitespace.");

            try
            {
                //Make a builder to extract the number
                System.Text.StringBuilder output = new System.Text.StringBuilder(input.Length);

                //Keep track of the sign if it was found
                bool foundSign = false == sign.HasValue;

                //Iterate the characters indicated.
                for (; offset < length; ++offset)
                {
                    //Look at the character
                    char c = input[offset];

                    //If its the sign
                    if (sign.HasValue && c == sign)
                    {
                        //If it was not found already
                        if (false == foundSign)
                        {
                            //Include it
                            foundSign = true;

                            output.Append(c);
                        }

                        //Skip
                        continue;
                    }

                    //If the value contains what is possibly realted to the number then append it.
                    //if (false == char.IsDigit(c)) continue;

                    //If the value is not a hex digit then do not allow it
                    if (false == ASCII.IsHexDigit(ref c)) continue;
                    //if(c > 'f' || c > 'F') continue;

                    //Append the char
                    output.Append(c);
                }

                //Return the string.
                return output.ToString();
            }
            catch
            {
                throw;
            }
        }

        #endregion
    }
}
