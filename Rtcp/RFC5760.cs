using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.Rtcp
{
    //http://www.faqs.org/rfcs/rfc5760.html

    //http://datatracker.ietf.org/doc/rfc5760/?include_text=1

    /*
     
        The RSI report block has a fixed header size followed by a variable
   length report:

   0                   1                   2                   3
   0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |V=2|P|reserved |   PT=RSI=209  |             length            |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |                           SSRC                                |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |                       Summarized SSRC                         |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |              NTP Timestamp (most significant word)            |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |              NTP Timestamp (least significant word)           |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   :                       Sub-report blocks                       :
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
     
     */

    public enum SubReportBlockType : byte
    {
        IPv4Address = 0,
        IPv6Address = 1,
        DNSName = 2,
        Reserved = 3,
        Loss = 4,
        Jitter = 5,
        RTT = 6,
        CumulativeLoss = 7,
        Collisions = 8,
        Stats = 10,
        Bandwidth = 11,
        GroupInfo = 12,
        Unassigned = 13
    }

    public class RecieverSummaryInformation
    {
        uint SynchronizationSourceIdentifier, SummarizedSynchronizationSourceIdentifier;
        public ulong NtpTimestamp { get { return (ulong)m_NtpMsw << 32 | m_NtpLsw; } set { m_NtpLsw = (uint)(value & uint.MaxValue); m_NtpMsw = (uint)(value >> 32); } }
        uint m_NtpMsw, m_NtpLsw;

        List<SubReportBlock> Reports = new List<SubReportBlock>();
    }

    #region SubReportBlock and SubReportBlock Types

    /*
     
     0                   1                   2                   3
    0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
   +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
   |     SRBT      |    Length     |        NDB            |   MF  |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |                   Minimum Distribution Value                  |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |                   Maximum Distribution Value                  |
   +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
   |                      Distribution Buckets                     |
   |                             ...                               |
   |                             ...                               |
   +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+

     
     */

    public class SubReportBlock
    {
        SubReportBlockType BlockType { get; set; }
        byte m_Length;
        public int Length { get { return m_Length * 4;} set { m_Length = (byte)(value / 4); } }
        byte[] m_Data;
        byte[] Data { get { return m_Data; } set { m_Data = value; Length = m_Data.Length; } }

        public SubReportBlock(SubReportBlockType blockType) { BlockType = blockType; }

        public SubReportBlock(byte[] packet, int index)
        {
            BlockType = (SubReportBlockType)packet[index++];
            m_Length = packet[index++];
            System.Array.Copy(packet, index, Data, 0, Length);
        }

        byte[] ToBytes()
        {
            byte[] result = new byte[2 + Length];
            result[0] = (byte)BlockType;
            result[1] = m_Length;
            Data.CopyTo(result, 2);
            return result;
        }

    }

    public class FeedbackTargetAddressSubReportBlock : SubReportBlock 
    { 
        public ushort Port; public uint Address; 

        public System.Net.IPEndPoint ToIPEndPoint() { return new System.Net.IPEndPoint((long)Address, Port); }

        internal FeedbackTargetAddressSubReportBlock(SubReportBlockType type) : base(type) { }

        public static FeedbackTargetAddressSubReportBlock IPAddressTarget(System.Net.IPEndPoint ipEndPoint) { if (ipEndPoint.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork || ipEndPoint.AddressFamily != System.Net.Sockets.AddressFamily.InterNetworkV6) throw new NotSupportedException("Only the InterNetwork or InterNetworkV6 AddressFamily is supported."); return ipEndPoint.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork ? IPv4AddressTarget(ipEndPoint.Address, (ushort)ipEndPoint.Port) : IPv6AddressTarget(ipEndPoint.Address, (ushort)ipEndPoint.Port); }

        public static FeedbackTargetAddressSubReportBlock IPv4AddressTarget(System.Net.IPEndPoint ipEndPoint) { return IPv4AddressTarget(ipEndPoint.Address, (ushort)ipEndPoint.Port); }

        public static FeedbackTargetAddressSubReportBlock IPv4AddressTarget(System.Net.IPAddress address, ushort port)
        {
            if (address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork) throw new InvalidOperationException("address.AddressFamily must be InterNetworkV4");
            return new FeedbackTargetAddressSubReportBlock(SubReportBlockType.IPv4Address)
            {
                Address = (uint)address.Address,
                Port = port
            };
        }

        public static FeedbackTargetAddressSubReportBlock IPv6AddressTarget(System.Net.IPEndPoint ipEndPoint) { return IPv6AddressTarget(ipEndPoint.Address, (ushort)ipEndPoint.Port); }

        public static FeedbackTargetAddressSubReportBlock IPv6AddressTarget(System.Net.IPAddress address, ushort port)
        {
            if (address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetworkV6) throw new InvalidOperationException("address.AddressFamily must be InterNetworkV6");
            return new FeedbackTargetAddressSubReportBlock(SubReportBlockType.IPv6Address)
            {
                Address = (uint)address.Address,
                Port = port
            };
        }

        public static FeedbackTargetAddressSubReportBlock DNSName(System.Net.IPAddress address, ushort port)
        {
            return new FeedbackTargetAddressSubReportBlock(SubReportBlockType.DNSName)
            {
                Address = (uint)address.Address,
                Port = port
            };
        }
    }

    public class CollisionSubReportBlock : SubReportBlock 
    {
        public CollisionSubReportBlock() : base(SubReportBlockType.Collisions) { }

        public ushort Reserved; public uint CollisionSynchronizationSourceIdentifier;
    }

    public class GeneralStatisticsSubReportBlock : SubReportBlock 
    {
        public GeneralStatisticsSubReportBlock() : base(SubReportBlockType.Stats) { }
        public ushort Reserved; public byte MedianFractionLost; public uint HighestCumulativeNumberPacketsLost; public uint MedianInterArrivalJitter;
    }

    public class BandwidthIndicationSubReportBlock : SubReportBlock 
    {
        public BandwidthIndicationSubReportBlock() : base(SubReportBlockType.Bandwidth) { }
        public bool Sender, Receiver; public ushort Reserved; public uint Bandwidth; 
    }

    public class PacketSizeSubReportBlock : SubReportBlock 
    { 
        public PacketSizeSubReportBlock() : base(SubReportBlockType.GroupInfo) { }
        public ushort AveragePacketSize; public uint ReceiverGroupSize; 
    }

    #endregion

    #region Generic Sub Reports

    /*
     
    0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
   +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
   |     SRBT      |    Length     |        NDB            |   MF  |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |                   Minimum Distribution Value                  |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |                   Maximum Distribution Value                  |
   +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
   |                      Distribution Buckets                     |
   |                             ...                               |
   |                             ...                               |
   +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
     
     */

    public class GenericSubReportBlock : SubReportBlock 
    { 
        public ushort NumberDistributionBuckets; public byte MultiplicativeFactor;
        public uint MinimumDistributionValue, MaximumDistributionValue;

        public int BucketCount { get { return Length - 12 * 8 / NumberDistributionBuckets; } }

        public long this[int index]
        {
            get
            {
                if (index < 0 || index > BucketCount) throw new ArgumentOutOfRangeException();
                return MinimumDistributionValue + (index + 1) * (MaximumDistributionValue - MinimumDistributionValue) / NumberDistributionBuckets;
            }
        }

        public GenericSubReportBlock(SubReportBlockType blockType = SubReportBlockType.Unassigned) : base(blockType) { }
    }

    //LossSubReportBlock : GenericSubReportBlock

    //JitterSubReportBlock : GenericSubReportBlock

    //RoundTripTimeSubReportBlock : GenericSubReportBlock

    //CumulativeLossSubReportBlock : GenericSubReportBlock

    #endregion

}
