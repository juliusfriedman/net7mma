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
    public class RtspSourceStream : RtpSource
    {
        //needs to have a way to indicate the stream should be kept in memory for play on demand from a source which is not continious
        public static RtspChildStream CreateChild(RtspSourceStream source) { return new RtspChildStream(source); }

        System.Drawing.Image m_lastFrame;

        #region Properties

        /// <summary>
        /// Gets the RtspClient this RtspSourceStream uses to provide media
        /// </summary>
        public virtual RtspClient RtspClient { get; set; }

        /// <summary>
        /// Gets the RtpClient used by the RtspClient to provide media
        /// </summary>
        public override Rtp.RtpClient RtpClient { get { return RtspClient.Client; } }

        /// <summary>
        /// SessionDescription from the source RtspClient
        /// </summary>
        public override Sdp.SessionDescription SessionDescription { get { return RtspClient.SessionDescription; } }

        /// <summary>
        /// Gets or sets the source Uri used in the RtspClient
        /// </summary>
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

                if (RtspClient != null)
                {
                    bool wasConnected = RtspClient.Connected;

                    if (wasConnected) Stop();

                    RtspClient.Location = base.Source;

                    if (wasConnected) Start();
                }
            }
        }

        /// <summary>
        /// Indicates if the source RtspClient is Connected and has began to receive data via Rtp
        /// </summary>
        public override bool Ready { get { return RtspClient != null && RtspClient.Connected && RtspClient.Listening && RtpClient != null && RtpClient.Connected && RtpClient.TotalRtpBytesReceieved >= 0; ;} }

        #endregion

        #region Constructor

        internal RtspSourceStream(string name, Uri sourceLocation, Rtsp.RtspClient.ClientProtocolType? rtpProtocolType = null) 
            : base(name, sourceLocation)
        {
            //Create the listener if we are the top level stream (Parent)
            if (IsParent)
            {
                RtspClient = new RtspClient(m_Source, rtpProtocolType);
            }
            //else it is already assigned via the child
        }

        /// <summary>
        /// Constructs a RtspStream for use in a RtspServer
        /// </summary>
        /// <param name="name">The name given to the stream on the RtspServer</param>
        /// <param name="sourceLocation">The rtsp uri to the media</param>
        public RtspSourceStream(string name, string sourceLocation, Rtsp.RtspClient.ClientProtocolType? rtpProtocolType = null) : this(name, new Uri(sourceLocation), rtpProtocolType) { }
        
        /// <summary>
        /// Constructs a RtspStream for use in a RtspServer
        /// </summary>
        /// <param name="name">The name given to the stream on the RtspServer</param>
        /// <param name="sourceLocation">The rtsp uri to the media</param>
        /// <param name="credential">The network credential the stream requires</param>
        public RtspSourceStream(string name, string sourceLocation, NetworkCredential credential) : this(name, new Uri(sourceLocation), credential) { }

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
            RtspClient.Credential = SourceCredential = credential;
        }

        #endregion

        /// <summary>
        /// Beings streaming from the source
        /// </summary>
        public override void Start()
        {            
            if (!RtspClient.Connected)
            {
                try
                {
                    RtspClient.StartListening();
                    m_Started = DateTime.UtcNow;
                    if (RtpClient != null) RtpClient.RtpFrameChanged += DecodeFrame;
                }
                catch (RtspClient.RtspClientException)
                {
                    //Wrong Credentails etc...
                    //Swallow, Stream still 'Started'?
                }
                catch
                {
                    //Stop?
                    throw;
                }
            }
            //Call base
            base.Start();
        }

        /// <summary>
        /// Stops streaming from the source
        /// </summary>
        public override void Stop()
        {
            if (RtspClient.Listening)
            {
                try
                {
                    RtspClient.StopListening();
                    if (RtpClient != null) RtpClient.RtpFrameChanged -= DecodeFrame;
                }
                catch { }
            }
            base.Stop();
            m_Started = null;
        }

        internal virtual void DecodeFrame(Rtp.RtpClient sender, Rtp.RtpFrame frame)
        {
            if (RtspClient.Client == null || RtspClient.Client != sender) return;
            try
            {
                //Get the MediaDescription
                Media.Sdp.MediaDescription mediaDescription = this.RtspClient.Client.GetContextBySourceId(frame.SynchronizationSourceIdentifier).MediaDescription;
                if (mediaDescription.MediaType == Sdp.MediaType.audio)
                {
                    //Could have generic byte[] handlers OnAudioData OnVideoData OnEtc
                    //throw new NotImplementedException();
                }
                else if (mediaDescription.MediaType == Sdp.MediaType.video)
                {
                    if (mediaDescription.MediaFormat == 26)
                    {
                       OnFrameDecoded(m_lastFrame = (new Rtp.JpegFrame(frame)).ToImage());
                    }
                    else if (mediaDescription.MediaFormat >= 96 && mediaDescription.MediaFormat < 128)
                    {
                        //Dynamic..
                        //throw new NotImplementedException();
                    }
                    else
                    {
                        //0 - 95
                        //throw new NotImplementedException();
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
        public RtspChildStream(RtspSourceStream source) : base(source) { }

        public RtspClient Client
        {
            get
            {
                return ((RtspSourceStream)m_Parent).RtspClient;
            }
            internal set
            {
                ((RtspSourceStream)m_Parent).RtspClient = value;
            }
        }        
    }
}
