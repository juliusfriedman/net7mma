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

//namespace Media.Concepts
//{
//    /// <summary>
//    /// Defines properties common to objects which correspond to 'Text' data.
//    /// Could also be used like ITextView in VisualStudio Namespace.
//    /// </summary>
//    public interface ITextData /*View*/
//    {
//        /// <summary>
//        /// Gets the <see cref="System.Text.Encoding"/> associated with the text by default.
//        /// </summary>
//        System.Text.Encoding DefaultEncoding { get; } //Encoding

//        /// <summary>
//        /// Gets the binary data associated with establishing a single 'line' in the text.
//        /// To obtain the size of the individual data constituents known as <see cref="char">characters</see> or <see cref="string">strings</see> see the appropraite method of <see cref="DefaultEncoding"/>.
//        /// </summary>
//        System.Collections.Generic.IEnumerable<byte> LineEnds { get; } //char in DefaultEncoding

//        /// <summary>
//        /// Gets the binary data associated with the text (including any <see cref="LineEnds"/> which may be present).
//        /// </summary>
//        System.Collections.Generic.IEnumerable<byte> Data { get; }
//    }
//}
