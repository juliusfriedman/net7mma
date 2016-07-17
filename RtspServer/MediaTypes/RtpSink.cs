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
    /// Provides the basic opertions for any locally created Rtp data.
    /// </summary>
    /// <remarks>
    /// Should also allow for a real endpoint to send data in addition to just emiting events as useful in unicast, multicast et al.
    /// </remarks>
    public class RtpSink : RtpSource, IMediaSink
    {
        #region Properties

        /// <summary>
        /// Probably useful at a lower level..
        /// </summary>
        public virtual bool Loop { get; set; }

        /// <summary>
        /// Packets...
        /// </summary>
        /// <remarks>In or Out is a semantic not distinguished here</remarks>
        protected System.Collections.Generic.Queue<Common.IPacket> Packets = new System.Collections.Generic.Queue<Common.IPacket>();

        //public double MaxSendRate { get; protected set; }

        #endregion

        #region Methods

        #region Elaboration

        //SipSorcery had methods like this and in an attempt to allow the unification of the two libraries this method was added.
        //Unfortunately for one reason or another (probably multiple) the author(s) of that project have all but abandoned this project and revoked their membership.
        //It is for better considering that even though their implementation was free and somewhat of a starting point that it suffered from multiple weaknesses, the particulars of such will not be elaborated on further [here].
        //[sic]

        #endregion

        #region Useless

        //-- probably remove because it's not particularly useful unless it is known who is sending data to what, and what type of data?

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public void SendData(byte[] data, int offset = 0, int length = -1)
        {
            if (Common.IDisposedExtensions.IsNullOrDisposed(RtpClient)) return;

            RtpClient.OnRtpPacketReceieved(new Rtp.RtpPacket(data, offset, length >= 0 ? length : data.Length - offset));
        }

        //possibly also useful as a signal

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public void EnqueData(byte[] data, int offset = 0, int length = -1)
        {
            if (Common.IDisposedExtensions.IsNullOrDisposed(RtpClient)) return;

            Packets.Enqueue(new Rtp.RtpPacket(data, offset, length >= 0 ? length : data.Length - offset));
        }

        #endregion

        //Todo, Needs EndPoints to which to send? [as if the handler would not be able to determine this....]

        /// <summary>
        /// 
        /// </summary>
        /// <param name="packet"></param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public void SendPacket(Common.IPacket packet)
        {
            if (Common.IDisposedExtensions.IsNullOrDisposed(RtpClient) ||
                Common.IDisposedExtensions.IsNullOrDisposed(packet)) return;

            if (packet is Rtp.RtpPacket) RtpClient.OnRtpPacketReceieved(packet as Rtp.RtpPacket);
            else if (packet is Rtcp.RtcpPacket) RtpClient.OnRtcpPacketReceieved(packet as Rtcp.RtcpPacket);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="packet"></param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public void EnquePacket(Common.IPacket packet)
        {
            if (Common.IDisposedExtensions.IsNullOrDisposed(RtpClient) ||
                Common.IDisposedExtensions.IsNullOrDisposed(packet)) return; 
            
            Packets.Enqueue(packet);
        }

        /// <summary>
        /// 
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public void SendReports()
        {
            if (Common.IDisposedExtensions.IsNullOrDisposed(RtpClient)) return;

            RtpClient.SendReports();
        }

        //@IThreadReference
        /// <summary>
        /// 
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal virtual void SendPackets()
        {
            while (Common.IDisposedExtensions.IsNullOrDisposed(this).Equals(false) && State == StreamState.Started)
            {
                try
                {
                    if (Packets.Count.Equals(0))
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
                catch (System.Exception ex)
                {
                    if (ex is System.Threading.ThreadAbortException) return;
                    continue;
                }
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates an instance
        /// </summary>
        /// <param name="name"></param>
        /// <param name="source"></param>
        public RtpSink(string name, System.Uri source) : base(name, source) { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="source"></param>
        /// <param name="client"></param>
        /// <param name="perPacket"></param>
        public RtpSink(string name, System.Uri source, Rtp.RtpClient client, bool perPacket = false)
            : base(name, source, perPacket)
        {
            //RtpClient = client;
        }

        #endregion
    }
}
