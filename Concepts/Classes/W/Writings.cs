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

namespace Media.Concepts.Classes.W
{
    /// <summary>
    /// `scriptum`
    /// </summary>
    public sealed class Writings
    {
        /// <summary>
        /// <see href="https://en.wikipedia.org/wiki/Literature">Literature</see>
        /// </summary>
        public interface Literature { }

        /// <summary>
        /// 
        /// </summary>
        public interface ILiterate : Media.Common.Interfaces.IMutable { }

        /// <summary>
        /// What is used to introduce a larger work which is usually written.
        /// </summary>
        public abstract class Prolegomena : ILiterate
        {
            /*public abstract*/
            bool Common.Interfaces.IMutable.Mutable
            {
                get { throw new System.NotImplementedException(); }
            }

            /*public abstract*/
            bool Common.Interfaces.IReadOnly.IsReadOnly
            {
                get { throw new System.NotImplementedException(); }
            }

            /*public abstract*/
            bool Common.Interfaces.IWriteOnly.IsWriteOnly
            {
                get { throw new System.NotImplementedException(); }
            }
        }

        /// <summary>
        /// In literature, an epigraph is a phrase, quotation, or poem that is set at the beginning of a document or component.
        /// The epigraph may serve as a preface, as a summary, as a counter-example, or to link the work to a wider literary canon, either to invite comparison or to enlist a conventional context.
        /// </summary>
        /// <remarks>
        /// From Wikipedia.
        /// </remarks>
        public class Epigraph : Prolegomena
        {

        }

        /// <summary>
        /// `clancularius` to an <see cref="Prolegomena"/>
        /// </summary>
        public class Preface : Prolegomena
        {

        }

        /// <summary>
        /// Unlike a <see cref="Preface"/> a <see cref="Foreward"/> is always signed.
        /// </summary>
        public class Foreward : Prolegomena
        {

        }

    }
}
