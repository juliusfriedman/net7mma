#region Copyright
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
#endregion

namespace Media.Rtsp
{
    /// <summary>
    /// Represents the resources in use by each Session created (during SETUP)
    /// </summary>
    internal class RtspSession : Common.SuppressedFinalizerDisposable
    {

        #region Fields

        /// <summary>
        /// Keep track of certain values.
        /// </summary>
        int m_SentBytes, m_ReceivedBytes,
             m_RtspPort,
             m_CSeq, m_RCSeq, //-1 values, rtsp 2. indicates to start at 0...
             m_SentMessages, m_ReTransmits,
             m_ReceivedMessages,
             m_PushedMessages,
             m_MaximumTransactionAttempts = (int)Media.Common.Extensions.TimeSpan.TimeSpanExtensions.MicrosecondsPerMillisecond,//10
             m_SocketPollMicroseconds;

        #endregion

        #region Properties [obtained during OPTIONS]

        //Options message...

        #endregion

        //Socket?

        #region Properties [obtained during DESCRIBE]

        //Describe message? only because the Content-Base is relevant, could also just keep a Uri for the Location...

        /// <summary>
        /// The Uri to which RtspMessages must be addressed within the session.
        /// Typically this is the same as Location in the RtspClient but under NAT a different address may be used here.
        /// </summary>
        public System.Uri ControlLocation { get; internal protected set; }

        public Sdp.SessionDescription SessionDescription { get; protected set; }

        #endregion

        #region Properties [obtained during SETUP]

        /// <summary>
        /// 3.4 Session Identifiers
        /// Session identifiers are opaque strings of arbitrary length. Linear
        /// white space must be URL-escaped. A session identifier MUST be chosen
        /// randomly and MUST be at least eight octets long to make guessing it
        /// more difficult. (See Section 16.)
        /// </summary>
        public string SessionId { get; internal protected set; }

        /// <summary>
        /// The time in which a request must be sent to keep the session active.
        /// </summary>
        public System.TimeSpan Timeout { get; protected set; }

        /// <summary>
        /// The TransportContext of the RtspSession
        /// </summary>
        /// Should be either a object or a derived class, should not be required due to raw or other transport, could be ISocketReference
        /// Notes that a session can share one or more context's
        public Rtp.RtpClient.TransportContext Context { get; internal protected set; }

        //public Sdp.MediaDescription MediaDescription { get; protected set; } => {Context.MediaDescription;}

        // DateTimeOffset Started or LastStarted

        // TimeSpan Remaining
        

        //PauseTime

        #endregion

        #region Properties

        /// <summary>
        /// The current SequenceNumber of the RtspClient
        /// </summary>
        public int ClientSequenceNumber
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_CSeq; }
        }

        /// <summary>
        /// The current SequenceNumber of the remote RTSP party
        /// </summary>
        public int RemoteSequenceNumber
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_RCSeq; }
        }

        /// <summary>
        /// Increments and returns the current <see cref="ClientSequenceNumber"/>
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        internal int NextClientSequenceNumber() { return ++m_CSeq; }

        /// <summary>
        /// Determines if a request must be sent periodically to keep the session and any underlying connection alive
        /// </summary>
        public bool EnableKeepAliveRequest { get; internal protected set; }

        /// <summary>
        /// The last RtspMessage sent
        /// </summary>
        public RtspMessage LastRequest { get; internal protected set; }

        /// <summary>
        /// The last RtspMessage received
        /// </summary>
        public RtspMessage LastResponse { get; internal protected set; }

        /// <summary>
        /// The amount of time taken from when the LastRequest was sent to when the LastResponse was created.
        /// </summary>
        public System.TimeSpan RoundTripTime
        {
            get { return LastResponse.Created - (LastRequest.Transferred ?? LastRequest.Created); }
        }

        /// <summary>
        /// The last RtspMessage recieved from the remote source
        /// </summary>
        public RtspMessage LastInboundRequest { get; internal protected set; }

        /// <summary>
        /// The last RtspMessage sent in response to a RtspMessage received from the remote source
        /// </summary>
        public RtspMessage LastInboundResponse { get; internal protected set; }

        /// <summary>
        /// The amount of time taken from when the LastInboundRequest was received to when the LastInboundResponse was Transferred.
        /// </summary>
        public System.TimeSpan ResponseTime
        {
            get { return LastInboundResponse.Created - (LastInboundRequest.Transferred ?? LastInboundRequest.Created); }
        }

        /// <summary>
        /// Time time remaining before the the session becomes Inactive
        /// </summary>
        public System.TimeSpan SessionTimeRemaining
        {
            get { return Timeout <= Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan ? Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan : Timeout - (System.DateTime.UtcNow - (LastResponse.Transferred ?? LastResponse.Created)); }
        }

        /// <summary>
        /// Indicates if the session has become inactive
        /// </summary>
        public bool TimedOut
        {
            get { return SessionTimeRemaining > System.TimeSpan.Zero; }
        }
        
        ///HasAuthenticated

        #endregion

        //Playing List

        #region Constructor

        public RtspSession(string sessionId, bool shouldDispose = true)
            :base(shouldDispose)
        {
            SessionId = sessionId;
        }

        public RtspSession(RtspMessage response, bool shouldDispose = true)
            : base(shouldDispose)
        {
            if (response != null)
            {
                ParseSessionIdAndTimeout(LastResponse = response);
            }
        }

        public RtspSession(RtspMessage request, RtspMessage response, bool shouldDispose = true)
            :this(response, shouldDispose)
        {
            LastRequest = request;
        }

        #endregion

        #region Methods

        public void UpdateMessages(RtspMessage request, RtspMessage response)
        {
            if (Common.IDisposedExtensions.IsNullOrDisposed(request).Equals(false) && 
                Common.IDisposedExtensions.IsNullOrDisposed(LastRequest).Equals(false))
            {
                LastRequest.IsPersistent = false;

                LastRequest.Dispose();
            }

            if (Common.IDisposedExtensions.IsNullOrDisposed(LastRequest = request).Equals(false))
            {
                LastRequest.IsPersistent = true;
            }

            if (Common.IDisposedExtensions.IsNullOrDisposed(LastResponse).Equals(false))
            {
                LastResponse.IsPersistent = false;

                LastResponse.Dispose();
            }

            if (Common.IDisposedExtensions.IsNullOrDisposed(LastResponse = response).Equals(false))
            {
                LastResponse.IsPersistent = true;
            }
        }

        public void UpdatePushedMessages(RtspMessage request, RtspMessage response)
        {
            if (false.Equals(Common.IDisposedExtensions.IsNullOrDisposed(request)) && false.Equals(Common.IDisposedExtensions.IsNullOrDisposed(LastInboundRequest)))
            {
                LastInboundRequest.IsPersistent = false;

                LastInboundRequest.Dispose();
            }


            if (Common.IDisposedExtensions.IsNullOrDisposed(LastInboundRequest = request).Equals(false))
            {
                LastInboundRequest.IsPersistent = true;
            }

            if (false.Equals(Common.IDisposedExtensions.IsNullOrDisposed(LastInboundResponse)))
            {
                LastInboundResponse.IsPersistent = false;

                LastInboundResponse.Dispose();
            }

            if (Common.IDisposedExtensions.IsNullOrDisposed(LastInboundResponse = response).Equals(false))
            {
                LastInboundResponse.IsPersistent = true;
            }
        }

        public bool ParseSessionIdAndTimeout(RtspMessage from)
        {
            SessionId = from[RtspHeaders.Session];

            Timeout = System.TimeSpan.FromSeconds(60);//Default

            //If there is a session header it may contain the option timeout
            if (false == string.IsNullOrWhiteSpace(SessionId))
            {
                //Check for session and timeout

                //Get the values
                string[] sessionHeaderParts = SessionId.Split(RtspHeaders.SemiColon);

                int headerPartsLength = sessionHeaderParts.Length;

                //Check if a valid value was given
                if (headerPartsLength > 0)
                {
                    //Trim it of whitespace
                    string value = System.Linq.Enumerable.LastOrDefault(sessionHeaderParts, (p => false == string.IsNullOrWhiteSpace(p)));

                    //If we dont have an exiting id then this is valid if the header was completely recieved only.
                    if (false == string.IsNullOrWhiteSpace(value) &&
                        true == string.IsNullOrWhiteSpace(SessionId) ||
                        value[0] != SessionId[0])
                    {
                        //Get the SessionId if present
                        SessionId = sessionHeaderParts[0].Trim();

                        //Check for a timeout
                        if (sessionHeaderParts.Length > 1)
                        {
                            string timeoutPart = sessionHeaderParts[1];

                            if (false == string.IsNullOrWhiteSpace(timeoutPart))
                            {
                                int timeoutStart = 1 + timeoutPart.IndexOf(Media.Sdp.SessionDescription.EqualsSign);

                                if (timeoutStart >= 0 && int.TryParse(timeoutPart.Substring(timeoutStart), out timeoutStart))
                                {
                                    if (timeoutStart > 0)
                                    {
                                        Timeout = System.TimeSpan.FromSeconds(timeoutStart);
                                    }
                                }
                            }
                        }

                        value = null;
                    }
                }

                sessionHeaderParts = null;

                return true;
            }

            return false;
        }

        public string TransportHeader;

        public System.TimeSpan LastServerDelay { get; protected set; }

        public void ParseDelay(RtspMessage from)
        {
            //Determine if delay was honored.
            string timestampHeader = from.GetHeader(RtspHeaders.Timestamp);

            //If there was a Timestamp header
            if (false == string.IsNullOrWhiteSpace(timestampHeader))
            {
                timestampHeader = timestampHeader.Trim();

                //check for the delay token
                int indexOfDelay = timestampHeader.IndexOf("delay=");

                //if present
                if (indexOfDelay >= 0)
                {
                    //attempt to calculate it from the given value
                    double delay = double.NaN;

                    if (double.TryParse(timestampHeader.Substring(indexOfDelay + 6).TrimEnd(), out delay))
                    {
                        //Set the value of the servers delay
                        LastServerDelay = System.TimeSpan.FromSeconds(delay);

                        //Could add it to the existing SocketReadTimeout and SocketWriteTimeout.
                    }
                }
                else
                {
                    //MS servers don't use a ; to indicate delay
                    string[] parts = timestampHeader.Split(RtspMessage.SpaceSplit, 2);

                    //If there was something after the space
                    if (parts.Length > 1)
                    {
                        //attempt to calulcate it from the given value
                        double delay = double.NaN;

                        if (double.TryParse(parts[1].Trim(), out delay))
                        {
                            //Set the value of the servers delay
                            LastServerDelay = System.TimeSpan.FromSeconds(delay);
                        }
                    }

                }
            }
        }

        #endregion

        #region Overloads

        /// <summary>
        /// Disposes <see cref="LastRequest"/> and <see cref="LastResponse"/>.
        /// Removes any references stored in the instance.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (false == disposing || false == ShouldDispose) return;

            base.Dispose(ShouldDispose);

            //If there is a LastRequest
            if (LastRequest != null)
            {
                //It is no longer persistent
                using (LastRequest) LastRequest.IsPersistent = false;
                
                //It is no longer scoped.
                LastRequest = null;
            }

            //If there is a LastResponse
            if(LastResponse != null)
            { 
                //It is no longer persistent
                using (LastResponse) LastResponse.IsPersistent = false;

                //It is no longer scoped.
                LastResponse = null;
            }

            //If there is a SessionDescription
            if (Common.IDisposedExtensions.IsNullOrDisposed(SessionDescription).Equals(false))
            {
                //Call dispose
                SessionDescription.Dispose();

                //Remove the reference
                SessionDescription = null;
            }

            //If there is a MediaDescription
            //if (MediaDescription != null)
            //{
            //    //Call dispose
            //    //MediaDescription.Dispose();

            //    //Remove the reference
            //    MediaDescription = null;
            //}

            //If there is a Context
            if (Context != null)
            {
                //Call dispose
                //Context.Dispose();

                //Remove the reference
                Context = null;
            }

            TransportHeader = null;
        }

        #endregion
    }
}
