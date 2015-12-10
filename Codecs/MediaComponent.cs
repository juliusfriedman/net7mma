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

namespace Media.Codec
{
    /// <summary>
    /// Defines the base class of all components
    /// </summary>
    public class MediaComponent
    {
        /// <summary>
        /// The identifier assigned to the component
        /// </summary>
        public readonly byte Id;

        /// <summary>
        /// The size of the component in bits
        /// </summary>
        public readonly int Size;

        /// <summary>
        /// Constructs a new MediaComponent with the given configuration
        /// </summary>
        /// <param name="id">The identifier assigned to the component</param>
        /// <param name="size">The size of the component in bits</param>
        public MediaComponent(byte id, int size)
        {
            //Validate the size in bits
            if (size < 1) throw new System.ArgumentException("size", "Must be greater than 0.");
            
            //Assign the size in bits
            Size = size;

            //assign the id
            Id = id;
        }

        /// <summary>
        /// The size of the component in bytes
        /// </summary>
        public int Length { get { return Common.Binary.BitsToBytes(Size); } }
    }
}
