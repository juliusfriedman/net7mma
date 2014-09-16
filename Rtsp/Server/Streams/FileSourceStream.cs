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
    public abstract class FileSourceStream : SourceStream
    {
        #region Nested Types

        //Should be a Top level class accessible from the RtspServer namespace?

        /// <summary>
        /// Information which describes a Track which is contained in a MediaFile
        /// </summary>
        public class TrackReference
        {
            public readonly byte[] Codec;

            public readonly int Id;
            
            //SampleSize?

            public readonly Sdp.MediaType MediaType;

            public readonly TimeSpan Duration;

            public TimeSpan Remaining { get { return Duration - Position; } }

            public TimeSpan Position;
        }

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
            readonly IEnumerable<TrackReference> Tracks;

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

                Tracks = Source.GetTracks();

                Fork = new System.IO.FileStream(Source.m_FileStream.SafeFileHandle, System.IO.FileAccess.Read);

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

                    foreach (TrackReference track in Tracks)
                    {
                        if (track.Remaining > TimeSpan.Zero)
                        {
                            Rtp.RtpFrame sample = Source.GetSample(track, out duration);

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
        internal protected System.IO.FileStream m_FileStream;

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
        public FileSourceStream(string name, Uri source, Guid? id = null)
            : base(name, source)
        {
            //Validate that the given file exists
            if (!System.IO.File.Exists(Source.AbsolutePath)) throw new System.IO.FileNotFoundException("Could not find" + source.AbsolutePath);

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
            //If the filestream has already been created then initialization has already been performed
            if (m_FileStream != null) return;

            //Create the FileStream with further writing allowed
            m_FileStream = new System.IO.FileStream(Source.AbsolutePath, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite);
        }

        /// <summary>
        /// Disposes all resources related to this instance.
        /// </summary>
        public override void Dispose()
        {
            if (Disposed) return;

            base.Dispose();

            m_FileStream.Dispose();

            m_FileStream = null;
        }

        /// <summary>
        /// Creates a <see cref="FileSourceStreamInformation"/> instance which can be used to maintain the state of playback during a session.
        /// </summary>
        /// <returns>The created <see cref="FileSourceStreamInformation"/></returns>
        internal FileSourceStreamInformation CreateInformation() { return new FileSourceStreamInformation(this); }

        #endregion

        #region Abstraction

        /// <summary>
        /// When overriden in a derived class, Provides information for each 'Track' in the Media
        /// </summary>
        public abstract IEnumerable<TrackReference> GetTracks();

        /// <summary>
        /// When overriden in a derived class, retrieves the <see cref="Rtp.RtpFrame"/> related to the given parameters
        /// </summary>
        /// <param name="track">The <see cref="TrackReference"/> which identifies the Track to retrieve the sample data from</param>       
        /// <param name="duration">The amount of time related to the result</param>
        /// <returns>The <see cref="Rtp.RtpFrame"/> containing the sample data</returns>
        public abstract Rtp.RtpFrame GetSample(TrackReference track, out TimeSpan duration);

        #endregion
    }
}
