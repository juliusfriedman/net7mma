using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.Rtcp.Extensions
{
    //RTCP XR Framework (eXtended Report)

    //Todo: Integrate 6.3.  The "rtcp-xr" SDP Attribute with RtspClient and RtpClient

    //http://www.networksorcery.com/enp/rfc/rfc3611.txt

    public class ExtendedReport : System.Collections.IEnumerable
    {

        #region Nested Types

        //6.2.  RTCP XR Block Type Registry
        public enum ExtendedReportBlockType : byte
        {
            Invalid = 0,
            Loss = 1,
            DuplicateRLE = 2,
            PacketReceiptTimes = 3,
            ReceiverReferenceTime = 4,
            DLRR = 5,
            StatisticsSummary = 6,
            VoIPMetric = 7,
            Reserved = 255
        }

        public class ExtendedReportBlock
        {
            #region Fields
            public byte BlockType, TypeSpecific;
            ushort m_BlockLength;
            byte[] m_Data;
            #endregion

            #region Propeties
            public ushort Length { get { return (ushort)(m_BlockLength * 4); } set { m_BlockLength = (ushort)(value / 4); } }
            public byte[] Data { get { return m_Data; } set { m_Data = value; Length = (ushort)m_Data.Length; } }
            public ExtendedReportBlockType ReportBlockType { get { return (ExtendedReportBlockType)BlockType; } set { BlockType = (byte)value; } }
            #endregion

            public ExtendedReportBlock(byte[] packet, ref int index)
            {
                BlockType = packet[index++];
                TypeSpecific = packet[index++];
                m_BlockLength = Utility.ReverseUnsignedShort(BitConverter.ToUInt16(packet, index));
                index += 2;
                Data = new byte[Length];
                System.Array.Copy(packet, index, Data, 0, Length);
            }

            public byte[] ToBytes()
            {
                byte[] result = new byte[4 + Data.Length];
                result[0] = BlockType;
                result[1] = TypeSpecific;
                BitConverter.GetBytes(Utility.ReverseUnsignedShort(m_BlockLength)).CopyTo(result, 2);
                Data.CopyTo(result, 4);
                return result;
            }
        }

        #endregion

        public uint SynchronizationSourceIdentifier;

        public List<ExtendedReportBlock> Blocks = new List<ExtendedReportBlock>();

        public ExtendedReport(RtcpPacket packet) { }

        public ExtendedReport(byte[] packet, int index)
        {
            SynchronizationSourceIdentifier = Utility.ReverseUnsignedInt(BitConverter.ToUInt32(packet, index + 0));

            while (index < packet.Length)
            {
                Blocks.Add(new ExtendedReportBlock(packet, ref index));
            }
        }

        public RtcpPacket ToPacket(byte? channel = null)
        {
            return new RtcpPacket(RtcpPacket.RtcpPacketType.ExtendedReport, channel)
            {
                Payload = ToBytes()
            };
        }

        public byte[] ToBytes()
        {
            List<byte> result = new List<byte>(4 + ReportBlock.Size * Blocks.Count);
            result.AddRange(BitConverter.GetBytes(Utility.ReverseUnsignedInt(SynchronizationSourceIdentifier)));
            Blocks.ForEach(rb => result.AddRange(rb.ToBytes()));
            return result.ToArray();
        }

        public System.Collections.IEnumerator GetEnumerator()
        {
            return Blocks.GetEnumerator();
        }

    }

    #region Todo

    //Nest in ExtendedReport

    //Loss RLE Report Block

    //---- Loss RLE Chunks

    /*
     
     0                   1                   2                   3
    0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |     BT=1      | rsvd. |   T   |         block length          |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |                        SSRC of source                         |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |          begin_seq            |             end_seq           |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |          chunk 1              |             chunk 2           |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   :                              ...                              :
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |          chunk n-1            |             chunk n           |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+

   block type (BT): 8 bits
         A Loss RLE Report Block is identified by the constant 1.

   rsvd.: 4 bits
         This field is reserved for future definition.  In the absence
         of such definition, the bits in this field MUST be set to zero
         and MUST be ignored by the receiver.

   thinning (T): 4 bits
         The amount of thinning performed on the sequence number space.
         Only those packets with sequence numbers 0 mod 2^T are reported
         on by this block.  A value of 0 indicates that there is no
         thinning, and all packets are reported on.  The maximum
         thinning is one packet in every 32,768 (amounting to two
         packets within each 16-bit sequence space).

   block length: 16 bits
         Defined in Section 3.

   SSRC of source: 32 bits
         The SSRC of the RTP data packet source being reported upon by
         this report block.

   begin_seq: 16 bits
         The first sequence number that this block reports on.

   end_seq: 16 bits
         The last sequence number that this block reports on plus one.


Friedman, et al.            Standards Track                    [Page 14]

RFC 3611                        RTCP XR                    November 2003


   chunk i: 16 bits
         There are three chunk types: run length, bit vector, and
         terminating null, defined in the following sections.  If the
         chunk is all zeroes, then it is a terminating null chunk.
         Otherwise, the left most bit of the chunk determines its type:
         0 for run length and 1 for bit vector.

4.1.1.  Run Length Chunk

    0                   1
    0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |C|R|        run length         |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+

   chunk type (C): 1 bit
         A zero identifies this as a run length chunk.

   run type (R): 1 bit
         Zero indicates a run of 0s.  One indicates a run of 1s.

   run length: 14 bits
         A value between 1 and 16,383.  The value MUST not be zero for a
         run length chunk (zeroes in both the run type and run length
         fields would make the chunk a terminating null chunk).  Run
         lengths of 15 or less MAY be described with a run length chunk
         despite the fact that they could also be described as part of a
         bit vector chunk.

4.1.2.  Bit Vector Chunk

    0                   1
    0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |C|        bit vector           |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+

   chunk type (C): 1 bit
         A one identifies this as a bit vector chunk.

   bit vector: 15 bits
         The vector is read from left to right, in order of increasing
         sequence number (with the appropriate allowance for
         wraparound).


Friedman, et al.            Standards Track                    [Page 15]

RFC 3611                        RTCP XR                    November 2003


4.1.3.  Terminating Null Chunk

   This chunk is all zeroes.

    0                   1
    0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0|
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
     
     */

    //Chunk { bool RunLength { Data[0] & 1 == 1}, BitVector { Data[0] & 1 << 1 == 1}, NullTerminating { Data[0] == 0 } }

    //Run Length Chunk

    //Bit Vector Chunk

    //Terminating Null Chunk

    //----

    //Duplicate RLE Report Block

    /*
     
     4.2.  Duplicate RLE Report Block

   This block type permits per-sequence-number reports on duplicates in
   a source's RTP packet stream.  Such information can be used for
   network diagnosis, and provide an alternative to packet losses as a
   basis for multicast tree topology inference.

   The Duplicate RLE Report Block format is identical to the Loss RLE
   Report Block format.  Only the interpretation is different, in that
   the information concerns packet duplicates rather than packet losses.
   The trace to be encoded in this case also consists of zeros and ones,
   but a zero here indicates the presence of duplicate packets for a
   given sequence number, whereas a one indicates that no duplicates
   were received.

   The existence of a duplicate for a given sequence number is
   determined over the entire reporting period.  For example, if packet
   number 12,593 arrives, followed by other packets with other sequence
   numbers, the arrival later in the reporting period of another packet
   numbered 12,593 counts as a duplicate for that sequence number.  The
   duplicate does not need to follow immediately upon the first packet
   of that number.  Care must be taken that a report does not cover a
   range of 65,534 or greater in the sequence number space.

   No distinction is made between the existence of a single duplicate
   packet and multiple duplicate packets for a given sequence number.
   Note also that since there is no duplicate for a lost packet, a loss
   is encoded as a one in a Duplicate RLE Report Block.


Friedman, et al.            Standards Track                    [Page 16]

RFC 3611                        RTCP XR                    November 2003


   The Duplicate RLE Report Block has the following format:

    0                   1                   2                   3
    0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |     BT=2      | rsvd. |   T   |         block length          |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |                        SSRC of source                         |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |          begin_seq            |             end_seq           |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |          chunk 1              |             chunk 2           |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   :                              ...                              :
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |          chunk n-1            |             chunk n           |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+

   block type (BT): 8 bits
         A Duplicate RLE Report Block is identified by the constant 2.

   rsvd.: 4 bits
         This field is reserved for future definition.  In the absence
         of such a definition, the bits in this field MUST be set to
         zero and MUST be ignored by the receiver.

   thinning (T): 4 bits
         As defined in Section 4.1.

   block length: 16 bits
         Defined in Section 3.

   SSRC of source: 32 bits
         As defined in Section 4.1.

   begin_seq: 16 bits
         As defined in Section 4.1.

   end_seq: 16 bits
         As defined in Section 4.1.

   chunk i: 16 bits
         As defined in Section 4.1.

     
     */

    //Packet Receipt Times Report Block

    /*
     
     * The Packet Receipt Times Report Block has the following format:

    0                   1                   2                   3
    0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |     BT=3      | rsvd. |   T   |         block length          |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |                        SSRC of source                         |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |          begin_seq            |             end_seq           |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |       Receipt time of packet begin_seq                        |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |       Receipt time of packet (begin_seq + 1) mod 65536        |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   :                              ...                              :
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |       Receipt time of packet (end_seq - 1) mod 65536          |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+

   block type (BT): 8 bits
         A Packet Receipt Times Report Block is identified by the
         constant 3.

   rsvd.: 4 bits
         This field is reserved for future definition.  In the absence
         of such a definition, the bits in this field MUST be set to
         zero and MUST be ignored by the receiver.

   thinning (T): 4 bits
         As defined in Section 4.1.

   block length: 16 bits
         Defined in Section 3.

   SSRC of source: 32 bits
         As defined in Section 4.1.

   begin_seq: 16 bits
         As defined in Section 4.1.

   end_seq: 16 bits
         As defined in Section 4.1.


Friedman, et al.            Standards Track                    [Page 19]

RFC 3611                        RTCP XR                    November 2003


   Packet i receipt time: 32 bits
         The receipt time of the packet with sequence number i at the
         receiver.  The modular arithmetic shown in the packet format
         diagram is to allow for sequence number rollover.  It is
         preferable for the time value to be established at the link
         layer interface, or in any case as close as possible to the
         wire arrival time.  Units and format are the same as for the
         timestamp in RTP data packets.  As opposed to RTP data packet
         timestamps, in which nominal values may be used instead of
         system clock values in order to convey information useful for
         periodic playout, the receipt times should reflect the actual
         time as closely as possible.  For a session, if the RTP
         timestamp is chosen at random, the first receipt time value
         SHOULD also be chosen at random, and subsequent timestamps
         offset from this value.  On the other hand, if the RTP
         timestamp is meant to reflect the reference time at the sender,
         then the receipt time SHOULD be as close as possible to the
         reference time at the receiver.
     
     */

    //Receive Reference Time Report Block

    /*
     
     4.4.  Receiver Reference Time Report Block

   This block extends RTCP's timestamp reporting so that non-senders may
   also send timestamps.  It recapitulates the NTP timestamp fields from
   the RTCP Sender Report [9, Sec. 6.3.1].  A non-sender may estimate
   its round trip time (RTT) to other participants, as proposed in [18],
   by sending this report block and receiving DLRR Report Blocks (see
   next section) in reply.

    0                   1                   2                   3
    0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |     BT=4      |   reserved    |       block length = 2        |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |              NTP timestamp, most significant word             |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |             NTP timestamp, least significant word             |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+

   block type (BT): 8 bits
         A Receiver Reference Time Report Block is identified by the
         constant 4.

   reserved: 8 bits
         This field is reserved for future definition.  In the absence
         of such definition, the bits in this field MUST be set to zero
         and MUST be ignored by the receiver.


Friedman, et al.            Standards Track                    [Page 20]

RFC 3611                        RTCP XR                    November 2003


   block length: 16 bits
         The constant 2, in accordance with the definition of this field
         in Section 3.

   NTP timestamp: 64 bits
         Indicates the wallclock time when this block was sent so that
         it may be used in combination with timestamps returned in DLRR
         Report Blocks (see next section) from other receivers to
         measure round-trip propagation to those receivers.  Receivers
         should expect that the measurement accuracy of the timestamp
         may be limited to far less than the resolution of the NTP
         timestamp.  The measurement uncertainty of the timestamp is not
         indicated as it may not be known.  A report block sender that
         can keep track of elapsed time but has no notion of wallclock
         time may use the elapsed time since joining the session
         instead.  This is assumed to be less than 68 years, so the high
         bit will be zero.  It is permissible to use the sampling clock
         to estimate elapsed wallclock time.  A report sender that has
         no notion of wallclock or elapsed time may set the NTP
         timestamp to zero.
     
     */

    //DLRR Report Block

    /*
     
     4.5.  DLRR Report Block

   This block extends RTCP's delay since the last Sender Report (DLSR)
   mechanism [9, Sec. 6.3.1] so that non-senders may also calculate
   round trip times, as proposed in [18].  It is termed DLRR for delay
   since the last Receiver Report, and may be sent in response to a
   Receiver Timestamp Report Block (see previous section) from a
   receiver to allow that receiver to calculate its round trip time to
   the respondent.  The report consists of one or more 3 word sub-
   blocks: one sub-block per Receiver Report.

  0                   1                   2                   3
  0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
 +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
 |     BT=5      |   reserved    |         block length          |
 +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
 |                 SSRC_1 (SSRC of first receiver)               | sub-
 +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+ block
 |                         last RR (LRR)                         |   1
 +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
 |                   delay since last RR (DLRR)                  |
 +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
 |                 SSRC_2 (SSRC of second receiver)              | sub-
 +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+ block
 :                               ...                             :   2
 +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+


Friedman, et al.            Standards Track                    [Page 21]

RFC 3611                        RTCP XR                    November 2003


   block type (BT): 8 bits
         A DLRR Report Block is identified by the constant 5.

   reserved: 8 bits
         This field is reserved for future definition.  In the absence
         of such definition, the bits in this field MUST be set to zero
         and MUST be ignored by the receiver.

   block length: 16 bits
         Defined in Section 3.

   last RR timestamp (LRR): 32 bits
         The middle 32 bits out of 64 in the NTP timestamp (as explained
         in the previous section), received as part of a Receiver
         Reference Time Report Block from participant SSRC_n.  If no
         such block has been received, the field is set to zero.

   delay since last RR (DLRR): 32 bits
         The delay, expressed in units of 1/65536 seconds, between
         receiving the last Receiver Reference Time Report Block from
         participant SSRC_n and sending this DLRR Report Block.  If a
         Receiver Reference Time Report Block has yet to be received
         from SSRC_n, the DLRR field is set to zero (or the DLRR is
         omitted entirely).  Let SSRC_r denote the receiver issuing this
         DLRR Report Block.  Participant SSRC_n can compute the round-
         trip propagation delay to SSRC_r by recording the time A when
         this Receiver Timestamp Report Block is received.  It
         calculates the total round-trip time A-LRR using the last RR
         timestamp (LRR) field, and then subtracting this field to leave
         the round-trip propagation delay as A-LRR-DLRR.  This is
         illustrated in [9, Fig. 2].
     
     */

    //Statistics Summary Report Block

    /*
     
     This block reports statistics beyond the information carried in the
   standard RTCP packet format, but is not as finely grained as that
   carried in the report blocks previously described.  Information is
   recorded about lost packets, duplicate packets, jitter measurements,
   and TTL or Hop Limit values.  Such information can be useful for
   network management.

   The report block contents are dependent upon a series of flag bits
   carried in the first part of the header.  Not all parameters need to
   be reported in each block.  Flags indicate which are and which are
   not reported.  The fields corresponding to unreported parameters MUST
   be present, but are set to zero.  The receiver MUST ignore any
   Statistics Summary Report Block with a non-zero value in any field
   flagged as unreported.


Friedman, et al.            Standards Track                    [Page 22]

RFC 3611                        RTCP XR                    November 2003


   The Statistics Summary Report Block has the following format:

    0                   1                   2                   3
    0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |     BT=6      |L|D|J|ToH|rsvd.|       block length = 9        |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |                        SSRC of source                         |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |          begin_seq            |             end_seq           |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |                        lost_packets                           |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |                        dup_packets                            |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |                         min_jitter                            |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |                         max_jitter                            |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |                         mean_jitter                           |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |                         dev_jitter                            |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   | min_ttl_or_hl | max_ttl_or_hl |mean_ttl_or_hl | dev_ttl_or_hl |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+

   block type (BT): 8 bits
         A Statistics Summary Report Block is identified by the constant
         6.

   loss report flag (L): 1 bit
         Bit set to 1 if the lost_packets field contains a report, 0
         otherwise.

   duplicate report flag (D): 1 bit
         Bit set to 1 if the dup_packets field contains a report, 0
         otherwise.

   jitter flag (J): 1 bit
         Bit set to 1 if the min_jitter, max_jitter, mean_jitter, and
         dev_jitter fields all contain reports, 0 if none of them do.

   TTL or Hop Limit flag (ToH): 2 bits
         This field is set to 0 if none of the fields min_ttl_or_hl,
         max_ttl_or_hl, mean_ttl_or_hl, or dev_ttl_or_hl contain
         reports.  If the field is non-zero, then all of these fields
         contain reports.  The value 1 signifies that they report on
         IPv4 TTL values.  The value 2 signifies that they report on


Friedman, et al.            Standards Track                    [Page 23]

RFC 3611                        RTCP XR                    November 2003


         IPv6 Hop Limit values.  The value 3 is undefined and MUST NOT
         be used.

   rsvd.: 3 bits
         This field is reserved for future definition.  In the absence
         of such a definition, the bits in this field MUST be set to
         zero and MUST be ignored by the receiver.

   block length: 16 bits
         The constant 9, in accordance with the definition of this field
         in Section 3.

   SSRC of source: 32 bits
         As defined in Section 4.1.

   begin_seq: 16 bits
         As defined in Section 4.1.

   end_seq: 16 bits
         As defined in Section 4.1.

   lost_packets: 32 bits
         Number of lost packets in the above sequence number interval.

   dup_packets: 32 bits
         Number of duplicate packets in the above sequence number
         interval.

   min_jitter: 32 bits
         The minimum relative transit time between two packets in the
         above sequence number interval.  All jitter values are measured
         as the difference between a packet's RTP timestamp and the
         reporter's clock at the time of arrival, measured in the same
         units.

   max_jitter: 32 bits
         The maximum relative transit time between two packets in the
         above sequence number interval.

   mean_jitter: 32 bits
         The mean relative transit time between each two packet series
         in the above sequence number interval, rounded to the nearest
         value expressible as an RTP timestamp.

   dev_jitter: 32 bits
         The standard deviation of the relative transit time between
         each two packet series in the above sequence number interval.


Friedman, et al.            Standards Track                    [Page 24]

RFC 3611                        RTCP XR                    November 2003


   min_ttl_or_hl: 8 bits
         The minimum TTL or Hop Limit value of data packets in the
         sequence number range.

   max_ttl_or_hl: 8 bits
         The maximum TTL or Hop Limit value of data packets in the
         sequence number range.

   mean_ttl_or_hl: 8 bits
         The mean TTL or Hop Limit value of data packets in the sequence
         number range, rounded to the nearest integer.

   dev_ttl_or_hl: 8 bits
         The standard deviation of TTL or Hop Limit values of data
         packets in the sequence number range.

     
     */

    //VoIP Metrics Report Block

    /*
     
     The block is encoded as seven 32-bit words:

    0                   1                   2                   3
    0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |     BT=7      |   reserved    |       block length = 8        |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |                        SSRC of source                         |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |   loss rate   | discard rate  | burst density |  gap density  |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |       burst duration          |         gap duration          |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |     round trip delay          |       end system delay        |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   | signal level  |  noise level  |     RERL      |     Gmin      |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |   R factor    | ext. R factor |    MOS-LQ     |    MOS-CQ     |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |   RX config   |   reserved    |          JB nominal           |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |          JB maximum           |          JB abs max           |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+

   block type (BT): 8 bits
         A VoIP Metrics Report Block is identified by the constant 7.

   reserved: 8 bits
         This field is reserved for future definition.  In the absence
         of such a definition, the bits in this field MUST be set to
         zero and MUST be ignored by the receiver.

   block length: 16 bits
         The constant 8, in accordance with the definition of this field
         in Section 3.

   SSRC of source: 32 bits
         As defined in Section 4.1.

   The remaining fields are described in the following six sections:
   Packet Loss and Discard Metrics, Delay Metrics, Signal Related
   Metrics, Call Quality or Transmission Quality Metrics, Configuration
   Metrics, and Jitter Buffer Parameters.
     
     */

    #endregion
}
