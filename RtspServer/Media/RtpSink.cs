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

namespace Media.Rtsp.Server.Media
{

    /// <summary>
    /// Provides the basic opertions for any locally created Rtp data
    /// </summary>
    public class RtpSink : RtpSource, IMediaSink
    {
        public RtpSink(string name, Uri source) : base(name, source) { }

        public virtual bool Loop { get; set; }

        protected Queue<Common.IPacket> Packets = new Queue<Common.IPacket>();

        //public double MaxSendRate { get; protected set; }

        //Fix

        public void SendData(byte[] data)
        {
            if (RtpClient != null) RtpClient.OnRtpPacketReceieved(new Rtp.RtpPacket(data, 0));
        }

        public void EnqueData(byte[] data)
        {
            if (RtpClient != null) Packets.Enqueue(new Rtp.RtpPacket(data, 0));
        }

        //

        public void SendPacket(Common.IPacket packet)
        {
            if (RtpClient != null)
            {
                if (packet is Rtp.RtpPacket) RtpClient.OnRtpPacketReceieved(packet as Rtp.RtpPacket);
                else if (packet is Rtcp.RtcpPacket) RtpClient.OnRtcpPacketReceieved(packet as Rtcp.RtcpPacket);
            }
        }

        public void EnquePacket(Common.IPacket packet)
        {
            if (RtpClient != null) Packets.Enqueue(packet);
        }

        public void SendReports()
        {
            if (RtpClient != null) RtpClient.SendReports();
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
                     Common.IPacket packet = Packets.Dequeue();

                     SendPacket(packet);

                    //If we are to loop images then add it back at the end
                    if (Loop) Packets.Enqueue(packet);

                    //Check for bandwidth and sleep if necessary
                }
                catch (Exception ex)
                {
                    if (ex is System.Threading.ThreadAbortException) return;
                    continue;
                }
            }
        }
    }
}
