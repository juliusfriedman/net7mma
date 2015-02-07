using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Rtsp.Server.MediaTypes
{
    public interface IMedia : IDisposable
    {

        /// <summary>
        /// Alternate names of the media
        /// </summary>
        IEnumerable<string> Aliases { get; }

        /// <summary>
        /// The name of the media
        /// </summary>
        String Name { get; }

        /// <summary>
        /// The id of the media
        /// </summary>
        Guid Id { get; }

        /// <summary>
        /// Any logger associcated with the media
        /// </summary>
        //Media.Common.ILogging Logger { get; }

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
        Uri ServerLocation { get; }

        /// <summary>
        /// Indicates if the media is ready for transport
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

        void TrySetLogger(Media.Common.ILogging logger);
    }
}
