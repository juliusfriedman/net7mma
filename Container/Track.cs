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

        public Track(Element header, int id, TimeSpan position, TimeSpan duration)
        {
            this.Header = header;
            this.Id = id;
            this.Position = position;
            this.Duration = duration;
        }

        #region Fields

        public readonly Element Header;

        public readonly long Offset;

        public readonly int Id;

        public readonly byte[] CodecIndication;

        //SampleSize?

        public readonly Sdp.MediaType MediaType;

        public readonly TimeSpan Duration;

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
