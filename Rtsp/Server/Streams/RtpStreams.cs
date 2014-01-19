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

namespace Media.Rtsp.Server.Streams
{
    /// <summary>
    /// Adds an abstract RtpClient To SourceStream,   
    /// This could also just be an interface, could have protected set for RtpClient
    /// could also be a class which suscribes to events from the assigned RtpClient for RtpPackets etc
    /// </summary>
    public abstract class RtpSource : SourceStream
    {
        public const string RtpMediaProtocol = "RTP/AVP";

        Sdp.SessionDescription m_Sdp;

        System.Drawing.Image m_lastDecodedFrame;

        public RtpSource(string name, Uri source) : base(name, source) { }
        
        public bool DisableRtcp { get { return m_DisableQOS; } set { m_DisableQOS = value; } }

        public abstract Rtp.RtpClient RtpClient { get; }

        public virtual Sdp.SessionDescription SessionDescription { get { return m_Sdp; } protected set { m_Sdp = value; } }

        public bool ForceTCP { get { return m_ForceTCP; } set { m_ForceTCP = value; } } //This will take effect after the change, existing clients will still have their connection

        internal virtual void DecodeFrame(Rtp.RtpClient sender, Rtp.RtpFrame frame)
        {
            if (RtpClient == null || RtpClient != sender) return;
            try
            {
                //Get the MediaDescription (by ssrc so dynamic payload types don't conflict
                Media.Sdp.MediaDescription mediaDescription = RtpClient.GetContextBySourceId(frame.SynchronizationSourceIdentifier).MediaDescription;
                if (mediaDescription.MediaType == Sdp.MediaType.audio)
                {
                    //Could have generic byte[] handlers OnAudioData OnVideoData OnEtc
                    //throw new NotImplementedException();
                }
                else if (mediaDescription.MediaType == Sdp.MediaType.video)
                {
                    if (mediaDescription.MediaFormat == 26)
                    {
                        OnFrameDecoded(m_lastDecodedFrame = (new Rtp.RFC2435Frame(frame)).ToImage());
                    }
                    else if (mediaDescription.MediaFormat >= 96 && mediaDescription.MediaFormat < 128)
                    {
                        //Dynamic..
                        //throw new NotImplementedException();
                    }
                    else
                    {
                        //0 - 95 || >= 128
                        //throw new NotImplementedException();
                    }
                }
            }
            catch
            {
                return;
            }
        }

        public override void Start()
        {
            //Add handler for frame events
            //if (RtpClient != null) RtpClient.RtpFrameChanged += DecodeFrame;

            base.Start();
        }

        public override void Stop()
        {
            //Remove handler
            //if (RtpClient != null) RtpClient.RtpFrameChanged -= DecodeFrame;

            base.Stop();
        }

    }

    //public abstract class RtpChildStream
    //{
    //}

}
