using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Diagnostics;

namespace Media.Rtsp
{

    /// <summary>
    /// Each source stream the RtspServer encapsulates and can be played by clients
    /// </summary>    
    public class RtspStream
    {
        public enum StreamState
        {
            Stopped,
            Started
        }

        #region Fields

        internal Guid m_Id = Guid.NewGuid();
        internal string m_Name;
        internal Uri m_Source;
        internal NetworkCredential m_Cred;
        internal NetworkCredential m_RtspCred;
        internal List<string> m_Aliases = new List<string>();

        #endregion

        #region Properties

        /// <summary>
        /// The unique Id of the RtspStream
        /// </summary>
        public Guid Id { get { return m_Id; } set { m_Id = value; } }

        /// <summary>
        /// The name of this stream, also used as the location on the server
        /// </summary>
        public string Name { get { return m_Name; } set { if (string.IsNullOrWhiteSpace(value)) throw new ArgumentNullException("Name", "Cannot consist of only null or whitespace"); m_Aliases.Add(m_Name); m_Name = value; } }

        /// <summary>
        /// Any Aliases the stream is known by
        /// </summary>
        public string[] Aliases { get { return m_Aliases.ToArray(); } }

        /// <summary>
        /// The credential the source requires
        /// </summary>
        public NetworkCredential SourceCredential
        {
            get { return m_Cred; }
            set
            {
                m_Cred = value;

                bool wasConnected = Client.Connected;

                if (wasConnected)
                {
                    //Disconnect with the old password
                    Stop();
                }

                //Set the new creds
                Client.Credential = value;

                if (wasConnected)
                {
                    //Connect again if we need to
                    Start();
                }
            }
        }

        /// <summary>
        /// The credential of the stream which will be exposed to clients
        /// </summary>
        public NetworkCredential RtspCredential
        {
            get { return m_RtspCred; }
            set { m_RtspCred = value; }
        }

        //Will need to have supported methods also.. and a handler dictionary to get the handler for each request server will use its by default..
        //Will need to adjust or subclass for Archived Streams        
        public RtspClient Client { get; set; }

        /// <summary>
        /// Indicates if the sources listener is connected
        /// </summary>
        public bool Connected { get { return Client.Connected; } }

        /// <summary>
        /// Indicates if the sources listener is listening
        /// </summary>
        public virtual bool Listening { get { return Client.Listening; } }

        /// <summary>
        /// Sdp of the Stream
        /// </summary>
        public virtual Sdp.SessionDescription SessionDescription { get { return Client.SessionDescription; } }

        /// <summary>
        /// State of the stream 
        /// </summary>
        public StreamState State { get; set; }

        /// <summary>
        /// The Uri to the source media
        /// </summary>
        public Uri Source
        {
            get { return m_Source; }
            set
            {

                if (value.Scheme != RtspMessage.ReliableTransport) throw new ArgumentException("value", "Must have the Reliable Transport scheme \"" + RtspMessage.ReliableTransport + "\"");
                
                m_Source = value;

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

        #region Events

        public delegate void FrameDecodedHandler(RtspStream stream, System.Drawing.Image decoded);

        public event FrameDecodedHandler FrameDecoded;

        internal void OnFrameDecoded(System.Drawing.Image decoded)
        {
            if (FrameDecoded != null) FrameDecoded(this, decoded);
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Constructs a RtspStream for use in a RtspServer
        /// </summary>
        /// <param name="name">The name given to the stream on the RtspServer</param>
        /// <param name="sourceLocation">The rtsp uri to the media</param>
        public RtspStream(string name, Uri sourceLocation)
        {
            //The stream name cannot be null or consist only of whitespace
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("The stream name cannot be null or consist only of whitespace", "name");
            
            //Set the name
            m_Name = name;

            //Set the source
            Source = sourceLocation;

            //Create the listener
            Client = new RtspClient(m_Source);
        }

        /// <summary>
        /// Constructs a RtspStream for use in a RtspServer
        /// </summary>
        /// <param name="name">The name given to the stream on the RtspServer</param>
        /// <param name="sourceLocation">The rtsp uri to the media</param>
        public RtspStream(string name, string sourceLocation) : this(name, new Uri(sourceLocation)) { }
        
        /// <summary>
        /// Constructs a RtspStream for use in a RtspServer
        /// </summary>
        /// <param name="name">The name given to the stream on the RtspServer</param>
        /// <param name="sourceLocation">The rtsp uri to the media</param>
        /// <param name="credential">The network credential the stream requires</param>
        public RtspStream(string name, Uri sourceLocation, NetworkCredential credential)
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
        public RtspStream(string name, string sourceLocation, NetworkCredential credential)
            : this(name, sourceLocation)
        {
            //Set the creds
            Client.Credential = m_Cred = credential;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Beings streaming from the source
        /// </summary>
        public virtual void Start()
        {            
            if (!Client.Connected && State == StreamState.Started)
            {
                try
                {
                    Client.StartListening();
                    Client.Client.RtpFrameCompleted += new Rtp.RtpClient.RtpFrameHandler(Client_RtpFrameCompleted);                    
                }
                catch (RtspClient.RtspClientException)
                {
                    //Wrong Credentails etc...
                }
                catch
                {
                    throw;
                }
                State = StreamState.Started;
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
                Media.Sdp.SessionDescription.SessionMediaDescription mediaDescription = this.Client.SessionDescription.MediaDesciptions[0];
                if (mediaDescription.MediaType.ToLower() == "audio") return;
                else if (mediaDescription.MediaFormat == "26")
                {
                    m_lastFrame = (new Rtp.JpegFrame(frame)).ToImage();
                    OnFrameDecoded(m_lastFrame);
                }
                else if (mediaDescription.MediaFormat == "96")
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
        public virtual void Stop()
        {
            if (Client.Listening)
            {
                Client.StopListening();
            }
            State = StreamState.Stopped;
        }

        public void AddAlias(string name)
        {
            if (m_Aliases.Contains(name)) return;
            m_Aliases.Add(name);
        }

        public void RemoveAlias(string alias)
        {
            m_Aliases.Remove(alias);
        }

        //The last frame decoded
        internal System.Drawing.Image m_lastFrame;

        public System.Drawing.Image GetFrame()
        {
            return m_lastFrame;
        }

        #endregion
    }
}
