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
        public static ChildStream CreateChild(RtspSourceStream source) { return new RtspChildStream(source); }

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
                //if (value.Scheme != RtspMessage.ReliableTransport) throw new ArgumentException("value", "Must have the Reliable Transport scheme \"" + RtspMessage.ReliableTransport + "\"");

                base.Source = value;

                if (Client != null)
                {
                    bool wasConnected = Client.Connected;

                    if (wasConnected) Stop();

                    Client.Location = m_Source;

                    if (wasConnected) Start();
                }
            }
        }

        #endregion

        #region Constructor

        internal RtspSourceStream(string name, Uri sourceLocation, bool child) : base(name, sourceLocation)
        {
            //The stream name cannot be null or consist only of whitespace
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("The stream name cannot be null or consist only of whitespace", "name");

            //Set the name
            m_Name = name;

            //Set the source
            m_Source = sourceLocation;

            //Create the listener
            if (!(m_Child = child))
            {
                Client = new RtspClient(m_Source);
            }
        }

        /// <summary>
        /// Constructs a RtspStream for use in a RtspServer
        /// </summary>
        /// <param name="name">The name given to the stream on the RtspServer</param>
        /// <param name="sourceLocation">The rtsp uri to the media</param>
        public RtspSourceStream(string name, Uri sourceLocation) : this(name, sourceLocation, false) { }

        /// <summary>
        /// Constructs a RtspStream for use in a RtspServer
        /// </summary>
        /// <param name="name">The name given to the stream on the RtspServer</param>
        /// <param name="sourceLocation">The rtsp uri to the media</param>
        public RtspSourceStream(string name, string sourceLocation) : this(name, new Uri(sourceLocation)) { }

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
            Client.Credential = m_Cred = credential;
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
            Client.Credential = m_Cred = credential;
        }

        #endregion

        /// <summary>
        /// Beings streaming from the source
        /// </summary>
        public override void Start()
        {
            if (!Client.Connected && State == StreamState.Started)
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
                Client.Client.RtpFrameCompleted += new Rtp.RtpClient.RtpFrameHandler(Client_RtpFrameCompleted);
                State = StreamState.Started;
                m_Started = DateTime.Now;
            }
        }

        internal virtual void Client_RtpFrameCompleted(Rtp.RtpClient sender, Rtp.RtpFrame frame)
        {
            if (Client.Client != sender) return;
            DecodeFrame(frame);
            //System.Threading.ThreadPool.QueueUserWorkItem(new System.Threading.WaitCallback((o) => DecodeFrame(frame)));
        }

        internal void DecodeFrame(Rtp.RtpFrame frame)
        {
            try
            {
                Media.Sdp.MediaDescription mediaDescription = this.Client.SessionDescription.MediaDescriptions[0];
                if (mediaDescription.MediaType == Sdp.MediaType.audio) return;
                else if (mediaDescription.MediaFormat == 26)
                {
                    m_lastFrame = (new Rtp.JpegFrame(frame)).ToImage();
                    OnFrameDecoded(m_lastFrame);
                }
                else if (mediaDescription.MediaFormat == 96)
                {
                    //Dynamic..
                }
            }
            catch
            {
                return;
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
                Client.Client.RtpFrameCompleted -= Client_RtpFrameCompleted;
            }
            m_Started = null;
            State = StreamState.Stopped;
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
