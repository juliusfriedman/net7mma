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
    /// A Track describes the information related to samples within a MediaFileStream
    /// </summary>
    public class Track
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="header"></param>
        /// <param name="name"></param>
        /// <param name="id"></param>
        /// <param name="created"></param>
        /// <param name="modified"></param>
        /// <param name="sampleCount"></param>
        /// <param name="height"></param>
        /// <param name="width"></param>
        /// <param name="position"></param>
        /// <param name="duration"></param>
        /// <param name="frameRate"></param>
        /// <param name="mediaType">This could be a type defined either here or in Common to reduce the need to have SDP as a reference</param>
        /// <param name="codecIndication">There needs to be either a method on each IMediaContainer to get a 4cc or a common mapping.</param>
        /// May not always be present...
        /// <param name="channels"></param>
        /// <param name="bitDepth"></param>
        public Track(Node header, string name, int id,  DateTime created, DateTime modified, long sampleCount, int height, int width, TimeSpan position, TimeSpan duration, double frameRate, Sdp.MediaType mediaType, byte[] codecIndication, byte channels = 0, byte bitDepth = 0)
        {
            this.Header = header;
            this.Width = width;
            this.Height = height;
            this.Id = id;
            this.Position = position;
            this.Duration = duration;
            this.Rate = frameRate;
            this.MediaType = mediaType;
            this.Name = name;
            this.SampleCount = sampleCount;
            this.CodecIndication = codecIndication;
            this.Channels = channels;
            this.BitDepth = bitDepth;
        }

        #region Fields

        //EncryptedTrack... or IsEncrypted...

        public readonly Node Header;

        public readonly long Offset;

        public readonly int Id;

        public readonly string Name;

        //public readonly string Language; //Useful?

        public readonly byte[] CodecIndication;

        public readonly double Rate;

        public readonly int Width, Height;

        public readonly Sdp.MediaType MediaType;

        public readonly TimeSpan Duration;

        public readonly DateTime Created, Modified;

        public readonly long SampleCount;

        public readonly byte Channels, BitDepth;

        /// <summary>
        /// Used to adjust the sample which is retrieved next.
        /// </summary>
        public TimeSpan Position;

        #endregion

        #region Properties

        public TimeSpan Remaining { get { return Duration - Position; } }

        #endregion
    }

    //VideoTrack

    //AudioTrack

    //TextTrack

}
