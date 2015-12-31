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

namespace Media.Rtsp.Server.MediaTypes
{
    /// <summary>
    /// A remote stream the RtspServer aggregates and can be played by clients.
    /// </summary>    
    public class RtspSource : RtpSource
    {
        //needs to have a way to indicate the stream should be kept in memory for play on demand from a source which is not continious, e.g. archiving / caching etc.
        //public static RtspChildStream CreateChild(RtspSourceStream source) { return new RtspChildStream(source); }        

        /// <summary>
        /// If not null the only type of media which will be setup from the source.
        /// </summary>
        public readonly IEnumerable<Sdp.MediaType> SpecificMediaTypes;

        /// <summary>
        /// If not null, The time at which to start the media in the source.
        /// </summary>
        public readonly TimeSpan? MediaStartTime, MediaEndTime;

        #region Properties

        /// <summary>
        /// Gets the RtspClient this RtspSourceStream uses to provide media
        /// </summary>
        public virtual RtspClient RtspClient { get; set; }

        /// <summary>
        /// Gets the RtpClient used by the RtspClient to provide media
        /// </summary>
        public override Rtp.RtpClient RtpClient { get { return IsDisposed ? null : RtspClient.Client; } }

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
                if (value.Scheme != RtspMessage.ReliableTransportScheme) throw new ArgumentException("value", "Must have the Reliable Transport scheme \"" + RtspMessage.ReliableTransportScheme + "\"");

                base.Source = value;

                if (RtspClient != null)
                {
                    bool wasConnected = RtspClient.IsConnected;

                    if (wasConnected) Stop();

                    RtspClient.CurrentLocation = base.Source;

                    if (wasConnected) Start();
                }
            }
        }

        /// <summary>
        /// Indicates if the source RtspClient is Connected and has began to receive data via Rtp
        /// </summary>
        public override bool Ready { get { return base.Ready && RtspClient != null && RtspClient.IsPlaying; } }

        #endregion

        #region Constructor

        //Todo, make constructor easier to call

        public RtspSource(string name, string location, RtspClient.ClientProtocolType rtpProtocolType, int bufferSize = RtspClient.DefaultBufferSize, Sdp.MediaType? specificMedia = null, TimeSpan? startTime = null, TimeSpan? endTime = null, bool perPacket = false)
            : this(name, location, null, AuthenticationSchemes.None, rtpProtocolType, bufferSize, specificMedia, startTime, endTime, perPacket) { }

        public RtspSource(string name, string sourceLocation, NetworkCredential credential = null, AuthenticationSchemes authType = AuthenticationSchemes.None, Rtsp.RtspClient.ClientProtocolType? rtpProtocolType = null, int bufferSize = RtspClient.DefaultBufferSize, Sdp.MediaType? specificMedia = null, TimeSpan? startTime = null, TimeSpan? endTime = null, bool perPacket = false)
            : this(name, new Uri(sourceLocation), credential, authType, rtpProtocolType, bufferSize, specificMedia.HasValue ? Common.Extensions.Linq.LinqExtensions.Yield(specificMedia.Value) : null, startTime, endTime, perPacket)
        {
            //Check for a null Credential and UserInfo in the Location given.
            if (credential == null && !string.IsNullOrWhiteSpace(m_Source.UserInfo))
            {
                RtspClient.Credential = Media.Common.Extensions.Uri.UriExtensions.ParseUserInfo(m_Source);

                //Remove the user info from the location
                RtspClient.CurrentLocation = new Uri(RtspClient.CurrentLocation.AbsoluteUri.Replace(RtspClient.CurrentLocation.UserInfo + (char)Common.ASCII.AtSign, string.Empty).Replace(RtspClient.CurrentLocation.UserInfo, string.Empty));
            }
        }

        public RtspSource(string name, Uri source, bool perPacket, RtspClient client)
            : base(name, source, perPacket)
        {
            if (client == null) throw new ArgumentNullException("client");

            RtspClient = client;
        }

        /// <summary>
        /// Constructs a RtspStream for use in a RtspServer
        /// </summary>
        /// <param name="name">The name given to the stream on the RtspServer</param>
        /// <param name="sourceLocation">The rtsp uri to the media</param>
        /// <param name="credential">The network credential the stream requires</param>
        /// /// <param name="authType">The AuthenticationSchemes the stream requires</param>
        public RtspSource(string name, Uri sourceLocation, NetworkCredential credential = null, AuthenticationSchemes authType = AuthenticationSchemes.None, Rtsp.RtspClient.ClientProtocolType? rtpProtocolType = null, int bufferSize = RtspClient.DefaultBufferSize, IEnumerable<Sdp.MediaType> specificMedia = null, TimeSpan? startTime = null, TimeSpan? endTime = null, bool perPacket = false)
            : base(name, sourceLocation, perPacket)
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
            if (specificMedia != null) SpecificMediaTypes = specificMedia;

            //If there was a start time given
            if (startTime.HasValue) MediaStartTime = startTime;

            if (endTime.HasValue) MediaEndTime = endTime;
        }

        #endregion

        /// <summary>
        /// Beings streaming from the source
        /// </summary>
        public override void Start()
        {
            if (IsDisposed) return;

            if (false == RtspClient.IsConnected)
            {
                RtspClient.OnConnect += RtspClient_OnConnect;
                RtspClient.OnDisconnect += RtspClient_OnDisconnect;
                RtspClient.OnPlay += RtspClient_OnPlay;
                RtspClient.OnPause += RtspClient_OnPausing;
                RtspClient.OnStop += RtspClient_OnStop;

                try { RtspClient.Connect(); }
                catch { RtspClient.StopPlaying(); } //Stop stop
            }
            else if (false == RtspClient.IsPlaying)
            {
                try
                {
                    //Start the playing again
                    RtspClient.StartPlaying(MediaStartTime, MediaEndTime, SpecificMediaTypes);
                    
                    //Indicate when the stream was started.
                    m_StartedTimeUtc = DateTime.UtcNow;

                    //Call base to set started etc.
                    base.Start();
                }
                catch { RtspClient.StopPlaying(); } //Not stop
            }
        }

        void RtspClient_OnStop(RtspClient sender, object args)
        {
            base.Ready = RtspClient.IsPlaying;

            //Should also push event to all clients that the stream is stopping.
        }

        void RtspClient_OnPlay(RtspClient sender, object args)
        {
            if ((base.Ready = RtspClient.IsPlaying)) //  && RtspClient.PlayingMedia.Count is equal to what is supposed to be playing
            {
                RtspClient.Client.FrameChangedEventsEnabled = PerPacket == false;
            }
        }

        void RtspClient_OnDisconnect(RtspClient sender, object args)
        {
            base.Ready = false;
        }

        void RtspClient_OnPausing(RtspClient sender, object args)
        {
            base.Ready = RtspClient.IsPlaying;
        }

        void RtspClient_OnConnect(RtspClient sender, object args)
        {
            if (RtspClient != sender || false == RtspClient.IsConnected || RtspClient.IsPlaying) return;
            RtspClient.OnConnect -= RtspClient_OnConnect;
            try
            {
                //Start listening is not already playing
                if(false == RtspClient.IsPlaying) RtspClient.StartPlaying(MediaStartTime, MediaEndTime, SpecificMediaTypes);

                //Set the time for stats
                m_StartedTimeUtc = DateTime.UtcNow;

                //Call base to set started etc.
                base.Start();
            }
            catch
            {
                //Indicate not ready
                base.Ready = false;
            }
        }

        public override bool TrySetLogger(Common.ILogging logger)
        {
            if (false == Ready) return false;

            try
            {
                //Set the rtp logger and the rtsp logger
                RtspClient.Logger = logger;

                return base.TrySetLogger(logger);
            }
            catch { return false; }
        }

        /// <summary>
        /// Stops streaming from the source
        /// </summary>
        public override void Stop()
        {
            if (RtspClient != null)
            {
                if (RtspClient.IsPlaying) RtspClient.StopPlaying();

                else if (RtspClient.IsConnected) RtspClient.Disconnect();
                
                RtspClient.OnConnect -= RtspClient_OnConnect;
                RtspClient.OnDisconnect -= RtspClient_OnDisconnect;
                RtspClient.OnPlay -= RtspClient_OnPlay;
                RtspClient.OnStop -= RtspClient_OnStop;
            }


            base.Stop();

            m_StartedTimeUtc = null;
        }

        public override void Dispose()
        {
            if (IsDisposed) return;
           
            base.Dispose();

            if (RtspClient != null)
            {
                RtspClient.Dispose();
                RtspClient = null;
            }
        }
    }
}
