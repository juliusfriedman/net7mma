using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace Media.Rtsp.Server.Streams
{
    /// <summary>
    /// Each source stream the RtspServer encapsulates and can be played by clients
    /// </summary>    
    public class RtspSourceStream : RtpSourceStream
    {
        //needs to have a way to indicate the stream should be kept in memory for play on demand from a source which is not continious

        public static RtspChildStream CreateChild(RtspSourceStream source) { return new RtspChildStream(source); }

        #region Properties

        public virtual RtspClient Client { get; set; }

        public override Rtp.RtpClient RtpClient { get { return Client.Client; } }

        /// <summary>
        /// Indicates if the sources listener is connected
        /// </summary>
        public override bool Connected { get { return Client.Connected; } }

        /// <summary>
        /// Indicates if the sources listener is listening
        /// </summary>
        public override bool Listening { get { return Client.Listening; } }

        /// <summary>
        /// Sdp of the Stream
        /// </summary>
        public override Sdp.SessionDescription SessionDescription { get { return Client.SessionDescription; } }

        public override Uri Source
        {
            get
            {
                return base.Source;
            }
            set
            {
                //Experimental support for Unreliable and Http enabled with this line commented out
                if (value.Scheme != RtspMessage.ReliableTransport) throw new ArgumentException("value", "Must have the Reliable Transport scheme \"" + RtspMessage.ReliableTransport + "\"");

                base.Source = value;

                if (Client != null)
                {
                    bool wasConnected = Client.Connected;

                    if (wasConnected) Stop();

                    Client.Location = base.Source;

                    if (wasConnected) Start();
                }
            }
        }

        #endregion

        #region Constructor

        internal RtspSourceStream(string name, Uri sourceLocation) 
            : base(name, sourceLocation)
        {
            //Create the listener
            if (IsParent)
            {
                Client = new RtspClient(m_Source);
            }
        }

        /// <summary>
        /// Constructs a RtspStream for use in a RtspServer
        /// </summary>
        /// <param name="name">The name given to the stream on the RtspServer</param>
        /// <param name="sourceLocation">The rtsp uri to the media</param>
        public RtspSourceStream(string name, string sourceLocation) 
            : this(name, new Uri(sourceLocation)) { }

        /// <summary>
        /// Constructs a RtspStream for use in a RtspServer
        /// </summary>
        /// <param name="name">The name given to the stream on the RtspServer</param>
        /// <param name="sourceLocation">The rtsp uri to the media</param>
        /// <param name="credential">The network credential the stream requires</param>
        public RtspSourceStream(string name, Uri sourceLocation, NetworkCredential credential)
            : this(name, sourceLocation)
        {
            //Set the creds
            Client.Credential = SourceCredential = credential;
        }

        /// <summary>
        /// Constructs a RtspStream for use in a RtspServer
        /// </summary>
        /// <param name="name">The name given to the stream on the RtspServer</param>
        /// <param name="sourceLocation">The rtsp uri to the media</param>
        /// <param name="credential">The network credential the stream requires</param>
        public RtspSourceStream(string name, string sourceLocation, NetworkCredential credential)
            : this(name, sourceLocation)
        {
            //Set the creds
            Client.Credential = SourceCredential = credential;
        }

        #endregion

        /// <summary>
        /// Beings streaming from the source
        /// </summary>
        public override void Start()
        {
            State = StreamState.Started;
            if (!Client.Connected)
            {
                try
                {
                    Client.StartListening();
                }
                catch (RtspClient.RtspClientException)
                {
                    //Wrong Credentails etc...
                }
                catch
                {
                    throw;
                }
                if (RtpClient != null)
                {
                    RtpClient.RtpFrameChanged += Client_RtpFrameChanged;
                }
                m_Started = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Stops streaming from the source
        /// </summary>
        public override void Stop()
        {
            if (Client.Listening)
            {
                Client.StopListening();
                if (RtpClient != null)
                {
                    RtpClient.RtpFrameChanged -= Client_RtpFrameChanged;
                }
            }
            m_Started = null;
            State = StreamState.Stopped;
        }

        internal virtual void Client_RtpFrameChanged(Rtp.RtpClient sender, Rtp.RtpFrame frame)
        {
            if (Client.Client != sender) return;
            DecodeFrame(frame);
        }

        internal void DecodeFrame(Rtp.RtpFrame frame)
        {
            try
            {
                Media.Sdp.MediaDescription mediaDescription = this.Client.SessionDescription.MediaDescriptions[0];
                if (mediaDescription.MediaType == Sdp.MediaType.audio)
                {
                    //Could have generic byte[] handlers OnAudioData OnVideoData OnEtc
                    return;
                }
                else if (mediaDescription.MediaType == Sdp.MediaType.video)
                {
                    if (mediaDescription.MediaFormat == 26)
                    {
                        m_lastFrame = (new Rtp.JpegFrame(frame)).ToImage();
                        OnFrameDecoded(m_lastFrame);
                    }
                    else if (mediaDescription.MediaFormat >= 96 && mediaDescription.MediaFormat < 128)
                    {
                        //Dynamic..
                    }
                    else
                    {

                    }
                }
            }
            catch
            {
                return;
            }
        }

    }

    /// <summary>
    /// Encapsulates RtspStreams which are dependent on Parent RtspStreams
    /// </summary>
    public class RtspChildStream : ChildStream
    {
        public RtspChildStream(RtspSourceStream source)
            : base(source)
        {
        }

        public RtspClient Client
        {
            get
            {
                return ((RtspSourceStream)m_Parent).Client;
            }
            set
            {
                ((RtspSourceStream)m_Parent).Client = value;
            }
        }        
    }
}
