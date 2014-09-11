/*
This file came from Managed Media Aggregation, You can always find the latest version @ https://net7mma.codeplex.com/
  
 Julius.Friedman@gmail.com / (SR. Software Engineer ASTI Transportation Inc. http://www.asti-trans.com)

Permission is hereby granted, free of charge, 
 * to any person obtaining a copy of this software and associated documentation files (the "Software"), 
 * to deal in the Software without restriction, 
 * including without limitation the rights to :
 * use, 
 * copy, 
 * modify, 
 * merge, 
 * publish, 
 * distribute, 
 * sublicense, 
 * and/or sell copies of the Software, 
 * and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * 
 * 
 * JuliusFriedman@gmail.com should be contacted for further details.

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. 
 * 
 * IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, 
 * DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, 
 * TORT OR OTHERWISE, 
 * ARISING FROM, 
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 * 
 * v//
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace Media.Rtsp.Server.Streams
{
    /// <summary>
    /// A remote stream the RtspServer aggregates and can be played by clients.
    /// </summary>    
    public class RtspSourceStream : RtpSource
    {
        //needs to have a way to indicate the stream should be kept in memory for play on demand from a source which is not continious, e.g. archiving / caching etc.
        //public static RtspChildStream CreateChild(RtspSourceStream source) { return new RtspChildStream(source); }        

        /// <summary>
        /// If not null the only type of media which will be setup from the source.
        /// </summary>
        public readonly Sdp.MediaType? SpecificMediaType;

        /// <summary>
        /// If not null, The time at which to start the media in the source.
        /// </summary>
        public readonly TimeSpan? MediaStartTime;

        #region Properties

        /// <summary>
        /// Gets the RtspClient this RtspSourceStream uses to provide media
        /// </summary>
        public virtual RtspClient RtspClient { get; set; }

        /// <summary>
        /// Gets the RtpClient used by the RtspClient to provide media
        /// </summary>
        public override Rtp.RtpClient RtpClient { get { return RtspClient.Client; } }

        public override NetworkCredential SourceCredential
        {
            get
            {
                return base.SourceCredential;
            }
            set
            {
                if (RtspClient != null) RtspClient.Credential = value;
                base.SourceCredential = value;
            }
        }

        public override AuthenticationSchemes SourceAuthenticationScheme
        {
            get
            {
                return base.SourceAuthenticationScheme;
            }
            set
            {
                if (RtspClient != null) RtspClient.AuthenticationScheme = value;
                base.SourceAuthenticationScheme = value;
            }
        }

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
        public override bool Ready { get { return RtspClient != null && RtspClient.Playing; } }

        #endregion

        #region Constructor

        public RtspSourceStream(string name, string location, RtspClient.ClientProtocolType rtpProtocolType, int bufferSize = 8192, Sdp.MediaType? specificMedia = null, TimeSpan? startTime = null)
            : this(name, location, null, AuthenticationSchemes.None, rtpProtocolType, bufferSize, specificMedia, startTime) { }

        public RtspSourceStream(string name, string sourceLocation, NetworkCredential credential = null, AuthenticationSchemes authType = AuthenticationSchemes.None, Rtsp.RtspClient.ClientProtocolType? rtpProtocolType = null, int bufferSize = 8192, Sdp.MediaType? specificMedia = null, TimeSpan? startTime = null)
            : this(name, new Uri(sourceLocation), credential, authType, rtpProtocolType, bufferSize, specificMedia, startTime) { }

        /// <summary>
        /// Constructs a RtspStream for use in a RtspServer
        /// </summary>
        /// <param name="name">The name given to the stream on the RtspServer</param>
        /// <param name="sourceLocation">The rtsp uri to the media</param>
        /// <param name="credential">The network credential the stream requires</param>
        /// /// <param name="authType">The AuthenticationSchemes the stream requires</param>
        public RtspSourceStream(string name, Uri sourceLocation, NetworkCredential credential = null, AuthenticationSchemes authType = AuthenticationSchemes.None, Rtsp.RtspClient.ClientProtocolType? rtpProtocolType = null, int bufferSize = 8192, Sdp.MediaType? specificMedia = null, TimeSpan? startTime = null)
            : base(name, sourceLocation)
        {
            //Create the listener if we are the top level stream (Parent)
            if (IsParent)
            {
                RtspClient = new RtspClient(m_Source, rtpProtocolType, bufferSize);
            }
            //else it is already assigned via the child

            if (credential != null)
            {
                RtspClient.Credential = SourceCredential = credential;

                if (authType != AuthenticationSchemes.None) RtspClient.AuthenticationScheme = SourceAuthenticationScheme = authType;
            }
            
            //If only certain media should be setup 
            if (specificMedia.HasValue) SpecificMediaType = specificMedia;

            //If there was a start time given
            if (startTime.HasValue) MediaStartTime = startTime;
        }

        #endregion


        /// <summary>
        /// Beings streaming from the source
        /// </summary>
        public override void Start()
        {            
            if (!RtspClient.Connected)
            {
                RtspClient.OnConnect += RtspClient_OnConnect;
                RtspClient.OnDisconnect += RtspClient_OnDisconnect;
                RtspClient.OnPlay += RtspClient_OnPlay;
                RtspClient.Connect();
            }
        }

        void RtspClient_OnPlay(RtspClient sender, object args)
        {
            RtspClient.Client.FrameChangedEventsEnabled = false;
            Ready = true;
        }

        void RtspClient_OnDisconnect(RtspClient sender, object args)
        {
            if (RtspClient != sender) return;
            RtspClient.OnPlay -= RtspClient_OnPlay;
            RtspClient.OnDisconnect -= RtspClient_OnDisconnect;
            Ready = false;
        }

        void RtspClient_OnConnect(RtspClient sender, object args)
        {
            if (RtspClient != sender || RtspClient.Playing) return;
            RtspClient.OnConnect -= RtspClient_OnConnect;
            try
            {
                //Start listening
                RtspClient.StartPlaying(MediaStartTime, SpecificMediaType);

                //Set the time for stats
                m_StartedTimeUtc = DateTime.UtcNow;

                //Call base to set started etc.
                base.Start();
            }
            catch (Common.Exception<RtspClient>)
            {
                //Wrong Credentails etc...

                //Call base to set to stopped
                base.Stop();
            }
            catch
            {
                //Stop?
                throw;
            }
        }

        /// <summary>
        /// Stops streaming from the source
        /// </summary>
        public override void Stop()
        {
            if (RtspClient.Playing) RtspClient.StopPlaying();                    
            base.Stop();
            m_StartedTimeUtc = null;
        }

        
    }

    /// <summary>
    /// Encapsulates RtspStreams which are dependent on Parent RtspStreams
    /// </summary>
    //public class RtspChildStream : ChildStream
    //{
    //    public RtspChildStream(RtspSourceStream source) : base(source) { }

    //    public RtspClient Client
    //    {
    //        get
    //        {
    //            return ((RtspSourceStream)m_Parent).RtspClient;
    //        }
    //        internal set
    //        {
    //            ((RtspSourceStream)m_Parent).RtspClient = value;
    //        }
    //    }
    //}
}
