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

#region Using Statements
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#endregion

namespace Media.Container
{
    /// <summary>
    /// Decribes the properties which are contained on any MediaContainer implementation
    /// </summary>
    public interface IMediaContainer : IEnumerable<Media.Container.Node>,  /*IEnumerable<Track>,*/ IDisposable
    {
        //IdentifierSize

        //ReadIdentifier

        //ReadNode..

        /// <summary>
        /// Indicates if the instance was disposed.
        /// </summary>
        bool IsDisposed { get; }

        /// <summary>
        /// The Uri which describes the location of the data contained in this IMediaContainer
        /// </summary>
        Uri Location { get; }
        
        /// <summary>
        /// The first element in parsing, usually described the file type and version
        /// </summary>
        Node Root { get; }

        //*Determine if helpful*
        /// <summary>
        /// When supported returns the <see cref="Node"/> which describes the type(s) of data contained in this MediaContainer
        /// </summary>
        Node TableOfContents { get; }

        /// <summary>
        /// When overriden in a derived class, retrieves the <see cref="System.IO.Stream"/> assocaited with this MediaContainer
        /// </summary>
        System.IO.Stream BaseStream { get; }

        /// <summary>
        /// When supported returns the <see cref="Track"/>'s contained in this MediaContainer
        /// </summary>
        /// <returns></returns>
        IEnumerable<Track> GetTracks();

        /// <summary>
        /// When overriden in a derived class, retrieves the data related to the given parameters
        /// </summary>
        /// <param name="track">The <see cref="Track"/> which identifies the Track to retrieve the sample data from</param>       
        /// <param name="duration">The amount of time related to the result</param>
        /// <returns>The sample data</returns>
        byte[] GetSample(Track track, out TimeSpan duration);

        /// <summary>
        /// Reads the data from the given position from the <see cref="BaseStream"/>
        /// </summary>
        /// <param name="position"></param>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        int ReadAt(long position, byte[] buffer, int offset, int count);


        /// <summary>
        /// Write the data from the given position into the <see cref="BaseStream"/>
        /// </summary>
        /// <param name="position"></param>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        void WriteAt(long position, byte[] buffer, int offset, int count);

        /// <summary>
        /// Provides a string description of the given <see cref="Node"/>
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        string ToTextualConvention(Node node);
    }
}
