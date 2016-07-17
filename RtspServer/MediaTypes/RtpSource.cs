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
namespace Media.Rtsp.Server.MediaTypes
{
    /// <summary>
    /// Provides the basic operations for consuming a remote rtp stream for which there is an existing <see cref="SessionDescription"/>
    /// </summary>
    public class RtpSource : SourceMedia, Common.IThreadReference
    {
        #region Properties

        /// <summary>
        /// Indicates if this <see cref="RtpSource"/> will NOT utilize the Real Time Communication Protocol.
        /// </summary>
        public bool RtcpDisabled
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_DisableQOS; }
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            set { m_DisableQOS = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// This will take effect after the change, existing clients will still have their connection.
        /// </remarks>
        public bool ForceTCP
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_ForceTCP; }
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            set { m_ForceTCP = value; }
        }

        public virtual Rtp.RtpClient RtpClient
        {
            get;
            protected set;
        }

        #endregion

        #region Informatics

        #region Threaded Frame Events

        //It is completely possible to allow another thread here, such a thread would be soley responsible for issuing the data to the handlers of the RtpClient's events and would provide better performance in some cases.
        //It's also possible to Multicast the source resulting in the network handling the aggregation (See Sink)

        #endregion

        #region Unused [Example of how an implementation would begin to propagate the realization that may have to actually do some type of work]        

        //Asked by a user here how they would save a rtsp stream to disk....
        //RtpTools, MediaFileWriter, FFMPEG, Media Foundation................................................................
        //http://stackoverflow.com/questions/37285323/how-to-record-a-rtsp-stream-to-disk-using-net7mma

        //System.Drawing.Image m_lastDecodedFrame;
        //internal virtual void DecodeFrame(Rtp.RtpClient sender, Rtp.RtpFrame frame)
        //{
        //    if (RtpClient == null || RtpClient != sender) return;
        //    try
        //    {
        //        //Get the MediaDescription (by ssrc so dynamic payload types don't conflict
        //        Media.Sdp.MediaDescription mediaDescription = RtpClient.GetContextBySourceId(frame.SynchronizationSourceIdentifier).MediaDescription;
        //        if (mediaDescription.MediaType == Sdp.MediaType.audio)
        //        {
        //            //Could have generic byte[] handlers OnAudioData OnVideoData OnEtc
        //            //throw new NotImplementedException();
        //        }
        //        else if (mediaDescription.MediaType == Sdp.MediaType.video)
        //        {
        //            if (mediaDescription.MediaFormat == 26)
        //            {
        //                OnFrameDecoded(m_lastDecodedFrame = (new RFC2435Stream.RFC2435Frame(frame)).ToImage());
        //            }
        //            else if (mediaDescription.MediaFormat >= 96 && mediaDescription.MediaFormat < 128)
        //            {
        //                //Dynamic..
        //                //throw new NotImplementedException();
        //            }
        //            else
        //            {
        //                //0 - 95 || >= 128
        //                //throw new NotImplementedException();
        //            }
        //        }
        //    }
        //    catch
        //    {
        //        return;
        //    }
        //}

        #endregion

        #endregion

        #region ILoggingReference

        public override bool TrySetLogger(Common.ILogging logger)
        {
            if (Ready.Equals(false)) return false;

            try
            {
                //Set the logger
                RtpClient.Logger = logger;

                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                //
            }
        }

        public override bool TryGetLogger(out Common.ILogging logger)
        {
            if (Ready.Equals(false))
            {
                return base.TryGetLogger(out logger);
            }

            try
            {
                //Set the logger
                logger = RtpClient.Logger;

                return true;
            }
            catch
            {
                logger = null; 
                
                return false;
            }
            finally
            {
                //
            }
        }

        #endregion

        #region SourceMedia

        public override void Start()
        {
            //When the stream is not fully stopped
            if (State >= StreamState.StopRequested) return;
            
            //If there is a RtpClient call Activate.
            if (object.ReferenceEquals(RtpClient, null).Equals(false)) RtpClient.Activate();

            //Should be done in first packet recieved...
            base.Ready = true;

            //Call start which sets state.
            base.Start();
        }

        public override void Stop()
        {
            if (State <= StreamState.StopRequested) return;

            //When the stream is not stared
            if (Common.IDisposedExtensions.IsNullOrDisposed(RtpClient).Equals(false)) RtpClient.Deactivate();

            //Indicate no longer ready.
            base.Ready = false;

            //Call stop which sets state
            base.Stop();
        }

        public override void Dispose()
        {
            if (IsDisposed || ShouldDispose.Equals(false)) return;

            Stop();

            base.Dispose();

            if (Common.IDisposedExtensions.IsNullOrDisposed(RtpClient).Equals(false))
            {
                RtpClient.Dispose();

                RtpClient = null;
            }
        }

        #endregion

        #region IThreadReference

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        System.Collections.Generic.IEnumerable<System.Threading.Thread> Common.IThreadReference.GetReferencedThreads()
        {
            return Common.IDisposedExtensions.IsNullOrDisposed(RtpClient) ? null : Media.Common.Extensions.Linq.LinqExtensions.Yield(RtpClient.m_WorkerThread);
        }

        System.Action<System.Threading.Thread> Common.IThreadReference.ConfigureThread
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return RtpClient.ConfigureThread; }
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            set { RtpClient.ConfigureThread = value; }
        }

        #endregion

        #region Fields

        //These could and probably should be turned into properties...

        public readonly bool PerPacket;

        public readonly bool PassthroughRtcp;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates an instance which will utilize `per-packet` or `packet for packet` event moddeling.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="source"></param>
        /// <param name="perPacket"></param>
        public RtpSource(string name, System.Uri source, bool perPacket = false)
            : base(name, source)
        {
            PerPacket = perPacket;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="source"></param>
        /// <param name="client"></param>
        /// <param name="perPacket"></param>
        public RtpSource(string name, System.Uri source, Rtp.RtpClient client, bool perPacket = false)
            : this(name, source, perPacket)
        {
            if (Common.IDisposedExtensions.IsNullOrDisposed(client)) throw new Media.Common.Extensions.Exception.ExceptionExtensions.ArgumentNullOrDisposedException(client);            

            RtpClient = client;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="sessionDescription"></param>
        public RtpSource(string name, Sdp.SessionDescription sessionDescription)
            : base(name, new System.Uri(string.Join(System.Uri.SchemeDelimiter, Rtp.RtpClient.RtpProtcolScheme, ((Sdp.Lines.SessionConnectionLine)sessionDescription.ConnectionLine).Host)))
        {
            if (Common.IDisposedExtensions.IsNullOrDisposed(sessionDescription)) throw new Media.Common.Extensions.Exception.ExceptionExtensions.ArgumentNullOrDisposedException("sessionDescription", sessionDescription);

            RtpClient = Rtp.RtpClient.FromSessionDescription(SessionDescription = sessionDescription);

            RtpClient.FrameChangedEventsEnabled = PerPacket == false;
        }

        #endregion
    }
}
