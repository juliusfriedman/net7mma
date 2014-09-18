using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Container
{
    public interface IMediaContainer : IEnumerable<Element>, IDisposable
    {
        Uri Location { get; }
        
        Element Root { get; }

        Element TableOfContents { get; }

        IEnumerable<Track> GetTracks();

        System.IO.Stream BaseStream { get; }

        /// <summary>
        /// When overriden in a derived class, retrieves the <see cref="Rtp.RtpFrame"/> related to the given parameters
        /// </summary>
        /// <param name="track">The <see cref="TrackReference"/> which identifies the Track to retrieve the sample data from</param>       
        /// <param name="duration">The amount of time related to the result</param>
        /// <returns>The <see cref="Rtp.RtpFrame"/> containing the sample data</returns>
        Rtp.RtpFrame GetSample(Track track, out TimeSpan duration);
    }
}
