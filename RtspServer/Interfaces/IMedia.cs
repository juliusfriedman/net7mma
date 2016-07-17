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

namespace Media.Rtsp.Server
{
    /// <summary>
    /// Represents an interface which defines properties and methods which are found in addition to an underlying implementations of <see cref="Common.IDisposed"/>
    /// </summary>
    public interface IMedia : Common.IDisposed, Media.Common.ILoggingReference
    {
        /// <summary>
        /// Alternate names of the media
        /// </summary>
        System.Collections.Generic.IEnumerable<string> Aliases { get; }

        /// <summary>
        /// The name of the media
        /// </summary>
        /// <remarks>string and not System.String</remarks>
        System.String Name { get; }

        /// <summary>
        /// The id of the media
        /// </summary>
        System.Guid Id { get; }

        /// <summary>
        /// The state of the media
        /// </summary>
        SourceMedia.StreamState State { get; }

        /// <summary>
        /// Describes the media
        /// </summary>
        Sdp.SessionDescription SessionDescription { get; }

        /// <summary>
        /// Used to identify a media with in the server
        /// </summary>
        System.Uri ServerLocation { get; }

        /// <summary>
        /// Indicates if the media is ready [for transport].
        /// </summary>
        bool Ready { get; }

        /// <summary>
        /// Gets a value which indicates if the media is disabled.
        /// </summary>
        bool Disabled { get; }

        /// <summary>
        /// Starts the media
        /// </summary>
        void Start();

        /// <summary>
        /// Stops the media
        /// </summary>
        void Stop();        
    }
}
