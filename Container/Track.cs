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
        public readonly Element Header;

        public readonly long Offset;

        public readonly byte[] Codec;

        public readonly int Id;

        //SampleSize?

        public readonly Sdp.MediaType MediaType;

        public readonly TimeSpan Duration;

        public TimeSpan Remaining { get { return Duration - Position; } }

        public TimeSpan Position;
    }
}
