using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Container
{
    /// <summary>
    /// A Track is a describes the information related to samples within a MediaFileStream
    /// </summary>
    public class Track
    {

        public Track(Element header, string name, int id,  DateTime created, DateTime modified, long sampleCount, int height, int width, TimeSpan position, TimeSpan duration, double frameRate, Sdp.MediaType mediaType, byte[] codecIndication, byte channels = 0, byte bitDepth = 0)
        {
            this.Header = header;
            this.Width = width;
            this.Height = height;
            this.Id = (int)id;
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

        //EncryptedTrack...

        public readonly Element Header;

        public readonly long Offset;

        public readonly int Id;

        public readonly string Name;

        //public readonly string Language;

        public readonly byte[] CodecIndication;

        public readonly double Rate;

        public readonly int Width, Height;

        public readonly Sdp.MediaType MediaType;

        public readonly TimeSpan Duration;

        public readonly DateTime Created, Modified;

        public readonly long SampleCount;

        public readonly byte Channels, BitDepth;

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
