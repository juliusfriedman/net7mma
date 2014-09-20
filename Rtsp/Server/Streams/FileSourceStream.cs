using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Rtsp.Server.Streams
{
    /// <summary>    
    /// Represents the base class for any stream which is consumed from a local file.
    /// Created for each file which is streamed from the <see cref="RtspServer"/>
    /// </summary>
    public class FileSourceStream : SourceStream
    {
        #region Nested Types

        /// <summary>
        /// Created for each <see cref="ClientSession"/> when streaming media from a local file.
        /// Contains the Timing information and other information which is only relevent to the ClientSession
        /// </summary>
        internal class FileSourceStreamInformation : Common.BaseDisposable
        {

            #region Fields

            /// <summary>
            /// The instance of the <see cref="FileSourceStream"/> which created this information
            /// </summary>
            readonly FileSourceStream Source;

            /// <summary>
            /// The <see cref="System.IO.FileStream"/> which was created from the <see cref="Source"/>
            /// </summary>
            readonly System.IO.FileStream Fork;

            /// <summary>
            /// The thread which is responsible for reading samples from the Fork
            /// </summary>
            readonly System.Threading.Thread SampleReader;

            /// <summary>
            /// The tracks which are sampled
            /// </summary>
            readonly IEnumerable<Container.Track> Tracks;

            /// <summary>
            /// The RtpClient which provides the events for gathered samples.
            /// </summary>
            internal Rtp.RtpClient m_Client;

            #endregion

            #region Properties

            /// <summary>
            /// Indicates if the current instance is gathering samples
            /// </summary>
            public bool Sampling { get { return SampleReader != null && SampleReader.IsAlive; } }

            #endregion

            #region Constructor

            /// <summary>
            /// Creates an instance of the FileSourceStreamInformation
            /// </summary>
            /// <param name="source">The <see cref="FileSourceStream"/> which contains the resources for streaming</param>
            internal FileSourceStreamInformation(FileSourceStream source)
            {
                Source = source;

                Tracks = Source.MediaContainer.GetTracks();

                Fork = new System.IO.FileStream(((System.IO.FileStream)Source.MediaContainer.BaseStream).SafeFileHandle, System.IO.FileAccess.Read);

                SampleReader = new System.Threading.Thread(new System.Threading.ThreadStart(GatherSamples));
            }

            #endregion

            #region Methods

            /// <summary>
            /// Will use the implemenation made available from derivations via a Delegate RtpFrame ReadSample(Sdp.MediaType type, TimeSpan offset, out TimeSpan duration)
            /// </summary>
            void GatherSamples()
            {
                while (!Disposed)
                {
                    TimeSpan duration = TimeSpan.Zero;

                    foreach (Container.Track track in Tracks)
                    {
                        if (track.Remaining > TimeSpan.Zero)
                        {

                            //Here a byte[] Sample can be retrieved, if the source can directly get RtpSamples then another interface definition is required

                            //Then the byte[] is turned into a RtpFrame by a Packetize Delegate, the need is to determine which Implmentation to use which can be determined by
                            //The codec of the track.

                            Rtp.RtpFrame sample = null; // Source.MediaContainer.GetSample(track, out duration);

                            if (sample != null && m_Client.GetContextByPayloadType(sample.PayloadTypeByte) != null) foreach (Rtp.RtpPacket packet in sample) m_Client.OnRtpPacketReceieved(packet);

                            track.Position += duration;
                        }
                    }
                }
            }

            /// <summary>
            /// Releases all resources used by this instance.
            /// </summary>
            public override void Dispose()
            {
                if (Disposed) return;

                base.Dispose();

                m_Client.Dispose();

                m_Client = null;

                Fork.Dispose();
            }

            #endregion
        }

        #endregion

        #region Fields

        /// <summary>
        /// A stream containing the data required for streaming
        /// </summary>
        internal protected Container.IMediaContainer MediaContainer;

        #endregion

        #region Properties

        #endregion

        #region Constructor / Destructor

        /// <summary>
        /// Constructs an instance of the FileSourceStream with the given parameters.
        /// If the given file is not found a <see cref="System.IO.FileNotFoundException"/> will be thrown.
        /// </summary>
        /// <param name="name">The name of this stream</param>
        /// <param name="source">The <see cref="Uri"/> whose AbsolutePath points to the file which contains the resources to be streamed</param>
        public FileSourceStream(Container.IMediaContainer mediaFileStream, string name, Uri source, Guid? id = null)
            : base(name, source)
        {
            if (mediaFileStream == null) throw new ArgumentNullException("mediaFileStream");

            MediaContainer = mediaFileStream;

            if (id.HasValue) m_Id = id.Value;

            //Call the initializer
            Initialize();
        }

        ~FileSourceStream() { Dispose(); }

        #endregion

        #region Methods

        /// <summary>
        /// Creates the required <see cref="SourceStream.SessionDescription"/> and any required Queue as well as seeks the stream to the location required for streaming.
        /// Must throw an exception if the format of the underlying stream is not supported.
        /// </summary>
        protected virtual void Initialize()
        {
            if (MediaContainer.Root == null) throw new Exception();
        }

        /// <summary>
        /// Disposes all resources related to this instance.
        /// </summary>
        public override void Dispose()
        {
            if (Disposed) return;

            base.Dispose();

            MediaContainer.Dispose();

            MediaContainer = null;
        }

        /// <summary>
        /// Creates a <see cref="FileSourceStreamInformation"/> instance which can be used to maintain the state of playback during a session.
        /// </summary>
        /// <returns>The created <see cref="FileSourceStreamInformation"/></returns>
        internal FileSourceStreamInformation CreateInformation() { return new FileSourceStreamInformation(this); }

        #endregion       
    }
}
