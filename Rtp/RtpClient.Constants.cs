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
namespace Media.Rtp
{
	/// <summary>
    /// The constants of a <see cref="RtpClient"/> instance.
	/// </summary>
    public partial class RtpClient
    {
        #region Constants / Statics

        /// <summary>
        /// The default amount which is used a multiplier to set the ReceiveBufferSize
        /// </summary>
        const int DefaultRecieveBufferSizeMultiplier = 100;

        internal static void ConfigureRtpThread(System.Threading.Thread thread)//,Common.ILogging = null
        {
            thread.TrySetApartmentState(System.Threading.ApartmentState.MTA);
        }

        //Possibly should be moved to RFC3550

        public const string RtpProtcolScheme = "rtp", AvpProfileIdentifier = "avp", RtpAvpProfileIdentifier = "RTP/AVP";

        //Udp Hole Punch
        //Might want a seperate method for this... (WakeupRemote)
        //Most routers / firewalls will let traffic back through if the person from behind initiated the traffic.
        //Send some bytes to ensure the reciever is awake and ready... (SIP / RELOAD / ICE / STUN / TURN may have something specific and better)
        //e.g Port mapping request http://tools.ietf.org/html/rfc6284#section-4.2 
        static byte[] WakeUpBytes = new byte[] { 0x70, 0x70, 0x70, 0x70 };

        //Choose better name,,, 
        //And depending on how memory is aligned 36 may be a palindrome
        //FrameControl
        internal const byte BigEndianFrameControl = 36;//, // ASCII => $,  Hex => 24  Binary => (00)100100
        //LittleEndianFrameControl = 9;                   //                                        001001(00)

        //The point at which rollover occurs on the SequenceNumber

        /// <summary>
        /// Describes the size (in bytes) of the 
        /// [MAGIC , CHANNEL, {LENGTH}] octets which preceed any TCP RTP / RTCP data When multiplexing data on a single TCP port over RTSP.
        /// </summary>
        internal const int InterleavedOverhead = 4;
        //RTP/AVP/TCP Specifies only the Length bytes in network byte order. e.g. 2 bytes

        /// <summary>
        /// The default time assocaited with Rtcp report intervals for RtpClients. (Almost 5 seconds)
        /// </summary>
        public static readonly System.TimeSpan DefaultReportInterval = System.TimeSpan.FromSeconds(4.96);

        //Todo have a Context method which passes the necessary params to this function for reading various different types of framing

        /// <summary>
        /// Read the RFC2326 amd RFC4751 Frame header.
        /// Returns the amount of bytes in the frame.
        /// Outputs the channel of the frame in the channel variable.
        /// </summary>
        /// <param name="buffer">The data containing the RFC4751 frame</param>
        /// <param name="offset">The offset in the </param>
        /// <param name="channel">The byte which will contain the channel if the reading succeeded</param>
        /// <param name="readFrameByte">Indicates if the frameByte should be read (RFC2326)</param>
        /// <param name="frameByte">Indicates the frameByte to read</param>
        /// <returns> -1 If the buffer does not contain a RFC2326 / RFC4751 frame at the offset given</returns>
        internal static int TryReadFrameHeader(byte[] buffer, int offset, out byte channel, byte? frameByte = BigEndianFrameControl, bool readChannel = true)
        {
            //Must be assigned
            channel = default(byte);

            if (Common.Extensions.Array.ArrayExtensions.IsNullOrEmpty(buffer)) return -1;

            //https://www.ietf.org/rfc/rfc2326.txt

            //10.12 Embedded (Interleaved) Binary Data

            //Todo, Native, Unsafe
            //If the buffer does not start with the magic byte this is not a RFC2326 frame, it could be a RFC4571 frame
            if (frameByte.HasValue && buffer[offset++].Equals(frameByte).Equals(false)) return -1; //goto ReadLengthOnly;

            /*
             Stream data such as RTP packets is encapsulated by an ASCII dollar
            sign (24 hexadecimal), followed by a one-byte channel identifier,
            followed by the length of the encapsulated binary data as a binary,
            two-byte integer in network byte order. The stream data follows
            immediately afterwards, without a CRLF, but including the upper-layer
            protocol headers. Each $ block contains exactly one upper-layer
            protocol data unit, e.g., one RTP packet.

            The channel identifier is defined in the Transport header with the
            interleaved parameter(Section 12.39).

            When the transport choice is RTP, RTCP messages are also interleaved
            by the server over the TCP connection. As a default, RTCP packets are
            sent on the first available channel higher than the RTP channel. The
            client MAY explicitly request RTCP packets on another channel. This
            is done by specifying two channels in the interleaved parameter of
            the Transport header(Section 12.39).
             */


            //Todo, Native, Unsafe
            //Assign the channel if reading framed.
            if (readChannel) channel = buffer[offset++];

            #region Babble

            //In stand alone operation mode the RtpClient should read only the length of the frame and decypher the contents based on the Payload.

            //SEE [COMEDIA]ly
            //http://tools.ietf.org/search/rfc4571#section-3

            //The ssrc may be useless due to middle boxes which have re-compresssed the data and sent it along with a new identifier....
            //The new identifier would be valid but would imply that the packet came from a different source (which since it was re-sampled it should)...
            //However
            //Based on my interpreatation the SSRC doesn't need to change @ all.
            //The packet's ContribuingSourceCount could be incremented by 1 (By the middle box who would subsequently add it's OWN identifier to the ContributingSourceList.)
            //The packet should become 4 bytes larger per hop that it is compressed or altered in due to the added entry.
            //If CC = 15 then no compression should be performed and the packet may need to be dropped if the destination is not within the next hop...

            //This would allow a receiver to dictate that middle boxes are causing unwanted compression or delay in the stream and subsequenlty the ability drop all packets from that middle box if required by iterating the contributing source list.
            //It would also allow such a receiver to either expediate or change the packet in another such way
            //IMHO IF THE ORIGINAL SSRC CHANGES before the packet reached the application THIS HAS DIRE CONSEQUENCES when the IDENTITY IS EXPECTED to be a particular value...
            //The application would have no way to verify that the data is indeed from Middle box X, Y or Z without using some form of verification i.e encryption.

            //Last but not least using 2 tcp sockets would be more performant but would require double the overhead from the provider, almost double the bandwidth (in protocol overhead) and definitely double the security issues.

            //RFC4571 - Out-of-band semantics
            //Section 2 does not define the RTP or RTCP semantics for closing a
            //TCP socket, or of any other "out of band" signal for the
            //connection.

            //With respect to Rtcp the sender should eventually timeout in the application, but the problem here lies in the fact the middle box has no control over that.
            //Thus the middle box it self will become conjested waiting for the timeout...
            //Additionally RTCP may not be enabled... if this is the case there would be no `Goodbye`
            //If RTCP was enabled then
            //Since the return route may not involve the same middle box which 'helped' it[the middlebox] may not get the `Goodbye` indication from the application participant,
            //Thus they would only timeout with respect to their own implementation rules for such,
            //BUT COULD ALSO receive another packet from another session which just happens to have the same SSRC
            //I / We would hope in such a case that the EndPoint would be different FROM the application's EndPoint because if it was not then that packet would subsequently routed to the application....

            //Lastly if the middle box compressed the data in any such way the payload indication would possibly be modified (and should be if the format changed)... thus breaking the compatability with the receiving application.
            //This implies that the Payload indication cannot change but the timestamp possibly could to reflect more delay if required but that should be handled by the application anyway.... not a middle box

            //Thus RTCP may be better suited for this type of 'change' e.g. each middle box could handle RtcpPackets to reflect the delay without changing the data within the rtp packet at all
            //upon receving a RtcpPacket The BlockCount could be incremented and an additional block could be added to indicate the metrics e.g. delay and jitter introduced by said middle box.
            //This would allow the receiving application to essentially ask that theat middle box not route packets any more or ask that it expedite routing et al.

            #endregion

            //Return the result of reversing the Unsigned 16 bit integer at the offset
            return Common.Binary.ReadU16(buffer, offset, Common.Binary.IsLittleEndian);
        }

        //Todo, cleanup and allow existing Rtp and Rtcp socket.

        /// <summary>
        /// Will create a <see cref="RtpClient"/> based on the given parameters
        /// </summary>
        /// <param name="sessionDescription"></param>
        /// <param name="sharedMemory"></param>
        /// <param name="incomingEvents"></param>
        /// <param name="rtcpEnabled"></param>
        /// <returns></returns>
        public static RtpClient FromSessionDescription(Sdp.SessionDescription sessionDescription, Common.MemorySegment sharedMemory = null, bool incomingEvents = true, bool rtcpEnabled = true, System.Net.Sockets.Socket existingSocket = null, int? rtpPort = null, int? rtcpPort = null, int remoteSsrc = 0, int minimumSequentialRtpPackets = 2, bool connect = true, System.Action<System.Net.Sockets.Socket> configure = null)
        {
            if (Common.IDisposedExtensions.IsNullOrDisposed(sessionDescription)) throw new System.ArgumentNullException("sessionDescription");

            Sdp.Lines.SessionConnectionLine connectionLine = new Sdp.Lines.SessionConnectionLine(sessionDescription.ConnectionLine);

            System.Net.IPAddress remoteIp = System.Net.IPAddress.Parse(connectionLine.Host), localIp;

            System.Net.NetworkInformation.NetworkInterface localInterface;

            //If the socket is NOT null and IS BOUND use the localIp of the same address family
            if (object.ReferenceEquals(existingSocket, null).Equals(false) && existingSocket.IsBound)
            {
                //If the socket is IP based
                if (existingSocket.LocalEndPoint is System.Net.IPEndPoint)
                {
                    //Take the localIp from the LocalEndPoint
                    localIp = (existingSocket.LocalEndPoint as System.Net.IPEndPoint).Address;
                }
                else
                {
                    throw new System.NotSupportedException("Please create an issue for your use case.");
                }
            }
            else // There is no socket existing.
            {
                //If the remote address is the broadcast address or the remote address is multicast
                if (System.Net.IPAddress.Broadcast.Equals(remoteIp) || Common.Extensions.IPAddress.IPAddressExtensions.IsMulticast(remoteIp))
                {
                    //This interface should be the interface you plan on using for the Rtp communication
                    localIp = Media.Common.Extensions.Socket.SocketExtensions.GetFirstMulticastIPAddress(remoteIp.AddressFamily, out  localInterface);
                }
                else
                {
                    //This interface should be the interface you plan on using for the Rtp communication
                    localIp = Media.Common.Extensions.Socket.SocketExtensions.GetFirstUnicastIPAddress(remoteIp.AddressFamily, out  localInterface);
                }
            }

            RtpClient client = new RtpClient(sharedMemory, incomingEvents);

            byte lastChannel = 0;

            //Todo, check for session level ssrc 
            //if (remoteSsrc.Equals(0))
            //{
            //    //Sdp.SessionDescriptionLine ssrcLine = sessionDescription.SsrcGroupLine; // SsrcLine @ the session level could imply Group
            //}

            //For each MediaDescription in the SessionDescription
            foreach (Media.Sdp.MediaDescription md in sessionDescription.MediaDescriptions)
            {
                //Make a RtpClient.TransportContext from the MediaDescription being parsed.
                TransportContext tc = TransportContext.FromMediaDescription(sessionDescription, lastChannel++, lastChannel++, md,
                    rtcpEnabled, remoteSsrc, minimumSequentialRtpPackets,
                    localIp, remoteIp, //The localIp and remoteIp
                    rtpPort, rtcpPort, //The remote ports to receive data from
                    connect, existingSocket, configure);

                //Try to add the context
                try
                {
                    client.AddContext(tc);
                }
                catch (System.Exception ex)
                {
                    Media.Common.TaggedExceptionExtensions.RaiseTaggedException(tc, "See Tag, Could not add the created TransportContext.", ex);
                }
            }

            //Return the participant
            return client;
        }

        #endregion

    }
}
