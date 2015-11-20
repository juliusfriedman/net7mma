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
using Media.Common;
using System.Text;

namespace Media.Rtsp.Server.MediaTypes
{
    /// <summary>
    /// Provides the basic operations for consuming a remote rtp stream for which there is an existing <see cref="SessionDescription"/>
    /// </summary>
    public class RtpSource : SourceMedia, Common.IThreadReference
    {
        public RtpSource(string name, Uri source, bool perPacket = false) : base(name, source) { PerPacket = perPacket; }

        public RtpSource(string name, Uri source, Rtp.RtpClient client, bool perPacket = false)
            : this(name, source, perPacket)
        {
            if (client == null) throw new ArgumentNullException("client");
            RtpClient = client;
        }

        public readonly bool PerPacket;

        public bool RtcpDisabled { get { return m_DisableQOS; } set { m_DisableQOS = value; } }

        public virtual Rtp.RtpClient RtpClient { get; protected set; }

        //This will take effect after the change, existing clients will still have their connection
        public bool ForceTCP { get { return m_ForceTCP; } set { m_ForceTCP = value; } } 
        
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

        public override bool TrySetLogger(Common.ILogging logger)
        {
            if (false == Ready) return false;

            try
            {
                //Set the logger
                RtpClient.Logger = logger;

                return true;
            }
            catch { return false; }
        }

        public override void Start()
        {
            //Add handler for frame events
            if (State == StreamState.Stopped)
            {
                if (RtpClient != null) RtpClient.Activate();

                base.Ready = true;

                base.Start();
            }
        }

        public override void Stop()
        {
            //Remove handler
            if (State == StreamState.Started)
            {
                if (RtpClient != null) RtpClient.Deactivate();

                base.Ready = false;

                base.Stop();
            }
        }

        public override void Dispose()
        {
            if (IsDisposed) return;
            base.Dispose();
            if (RtpClient != null) RtpClient.Dispose();
        }

        public RtpSource(string name, Sdp.SessionDescription sessionDescription)
            : base(name, new Uri(Rtp.RtpClient.RtpProtcolScheme + "://" + ((Sdp.Lines.SessionConnectionLine)sessionDescription.ConnectionLine).IPAddress))
        {
            if (sessionDescription == null) throw new ArgumentNullException("sessionDescription");

            RtpClient = Rtp.RtpClient.FromSessionDescription(SessionDescription = sessionDescription);

            RtpClient.FrameChangedEventsEnabled = PerPacket == false;
        }

        IEnumerable<System.Threading.Thread> Common.IThreadReference.GetReferencedThreads()
        {
            return RtpClient != null ? Media.Common.Extensions.Linq.LinqExtensions.Yield(RtpClient.m_WorkerThread) : null;
        }
    }
}
