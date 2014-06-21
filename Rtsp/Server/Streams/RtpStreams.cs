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
    public abstract class RtpSource : SourceStream, Media.Common.IThreadOwner
    {
        public const string RtpMediaProtocol = "RTP/AVP";

        System.Drawing.Image m_lastDecodedFrame;

        public RtpSource(string name, Uri source) : base(name, source) { }
        
        public bool DisableRtcp { get { return m_DisableQOS; } set { m_DisableQOS = value; } }

        public abstract Rtp.RtpClient RtpClient { get; }

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
                        OnFrameDecoded(m_lastDecodedFrame = (new RFC2435Stream.RFC2435Frame(frame)).ToImage());
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

        System.Threading.Thread Common.IThreadOwner.OwnedThread
        {
            get { return RtpClient != null ? RtpClient.m_WorkerThread : null; }
        }
    }

    public class RtpSink : RtpSource, IMediaSink
    {
        public override Rtp.RtpClient RtpClient
        {
            get { return Client; }
        }

        public RtpSink(string name, Uri source) : base(name, source) { }

        public RtpSink(string name)
            : base(name, null)
        {
            m_Source = new Uri("rtsp://localhost/live/" + Id);
        }

        public Rtp.RtpClient Client { get; protected set; }

        public virtual bool Loop { get; set; }

        protected Queue<Media.Common.IPacket> Packets = new Queue<Media.Common.IPacket>();

        public double MaxSendRate { get; protected set; }

        public void SendData(byte[] data)
        {
            if (Client != null) Client.OnRtpPacketReceieved(new Rtp.RtpPacket(data, 0));
        }

        public void EnqueData(byte[] data)
        {
            if (Client != null) Packets.Enqueue(new Rtp.RtpPacket(data, 0));
        }

        public void SendPacket(Media.Common.IPacket packet)
        {
            if (Client != null)
            {
                if (packet is Rtp.RtpPacket) Client.OnRtpPacketReceieved(packet as Rtp.RtpPacket);
                else if (packet is Rtcp.RtcpPacket) Client.OnRtcpPacketReceieved(packet as Rtcp.RtcpPacket);
            }
        }

        public void EnquePacket(Media.Common.IPacket packet)
        {
            if (Client != null) Packets.Enqueue(packet);
        }

        public void SendReports()
        {
            if (Client != null) Client.SendReports();
        }

        internal virtual void SendPackets()
        {
            while (State == StreamState.Started)
            {
                try
                {
                    if (Packets.Count == 0)
                    {
                        System.Threading.Thread.Sleep(0);
                        continue;
                    }

                    //Dequeue a frame or die
                     Media.Common.IPacket packet = Packets.Dequeue();

                     SendPacket(packet);

                    //If we are to loop images then add it back at the end
                    if (Loop) Packets.Enqueue(packet);

                    //Check for bandwidth and sleep if necessary
                }
                catch (OverflowException)
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine("Sink " + Id + " Overflow");
#endif
                    System.Threading.Thread.Sleep(0);

                    continue;
                }
                catch (Exception ex)
                {
                    if (ex is System.Threading.ThreadAbortException) return;
                    continue;
                }
            }
        }
    }

    //public class RtpDumpSource : RtpSource
    //{
        //Determine here or Rtsp
    //}

    //public abstract class RtpChildStream
    //{
    //}

}
