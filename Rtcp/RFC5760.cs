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
        RoundTripTime = 6,
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
        public SubReportBlockType BlockType { get; set; }
        protected byte m_Length;
        public int Length { get { return m_Length * 4;} set { m_Length = (byte)(value / 4); } }
        byte[] m_Data;
        public byte[] Data { get { return m_Data; } set { m_Data = value; Length = m_Data.Length; } }

        public SubReportBlock(SubReportBlockType blockType) { BlockType = blockType; }

        public SubReportBlock(byte[] packet, ref int index)
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

    /*
     
     0                   1                   2                   3
    0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   | SRBT={0,1,2}  |     Length    |             Port              |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |                                                               |
   :                            Address                            :
   |                                                               |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+

   SRBT: 8 bits
      The type of sub-report block that corresponds to the type of
      address is as follows:

         0: IPv4 address
         1: IPv6 address
         2: DNS name

   Length: 8 bits
      The length of the sub-report block in 32-bit words.  For an IPv4
      address, this should be 2 (i.e., total length = 4 + 4 = 2 * 4
      octets).  For an IPv6 address, this should be 5.  For a DNS name,
      the length field indicates the number of 32-bit words making up
      the string plus 1 byte and any additional padding required to
      reach the next word boundary.

   Port: 2 octets
      The port number to which receivers send feedback reports.  A port
      number of 0 is invalid and MUST NOT be used.

   Address: 4 octets (IPv4), 16 octets (IPv6), or n octets (DNS name)
      The address to which receivers send feedback reports.  For IPv4
      and IPv6, fixed-length address fields are used.  A DNS name is an
      arbitrary-length string that is padded with null bytes to the next
      32-bit boundary.  The string MAY contain Internationalizing Domain
      Names in Applications (IDNA) domain names and MUST be UTF-8
      encoded [11].

   A Feedback Target Address block for a certain address type (i.e.,
   with a certain SRBT of 0, 1, or 2) MUST NOT occur more than once
   within a packet.  Numerical Feedback Target Address blocks for IPv4
   and IPv6 MAY both be present.  If so, the resulting transport
   addresses MUST point to the same logical entity.

   If a Feedback Target address block with an SRBT indicating a DNS name
   is present, there SHOULD NOT be any other numerical Feedback Target
   Address blocks present.

   The Feedback Target Address presents a significant security risk if
   accepted from unauthenticated RTCP packets.  See Sections 11.3 and
   11.4 for further discussion.
     
     */

    public class FeedbackTargetAddressSubReportBlock : SubReportBlock 
    {
        public ushort Port { get { return Utility.ReverseUnsignedShort(BitConverter.ToUInt16(Data, 0)); } set { BitConverter.GetBytes(Utility.ReverseUnsignedShort(value)).CopyTo(Data, 0); } } public uint Address { get { return Utility.ReverseUnsignedShort(BitConverter.ToUInt16(Data, 2)); } set { BitConverter.GetBytes(Utility.ReverseUnsignedInt(value)).CopyTo(Data, 2); } }

        public System.Net.IPAddress IPAddress { get { return new System.Net.IPAddress((long)Address); } set { Address = (uint)value.Address; } }

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

        public FeedbackTargetAddressSubReportBlock(byte[] packet, ref int index, SubReportBlockType type) : base(packet, ref index)
        {
            if (BlockType != SubReportBlockType.DNSName && BlockType != SubReportBlockType.IPv4Address && BlockType != SubReportBlockType.IPv6Address) throw new InvalidOperationException("BlockType must be either DNSName, IPv4Address or IPv6Address. Found: " + BlockType);
        }

        public static implicit operator FeedbackTargetAddressSubReportBlock(System.Net.IPEndPoint ipEndPoint) { return FeedbackTargetAddressSubReportBlock.IPAddressTarget(ipEndPoint); }

        public static implicit operator System.Net.IPEndPoint(FeedbackTargetAddressSubReportBlock ftasb) { return ftasb.ToIPEndPoint(); }
    }

    public class CollisionSubReportBlock : SubReportBlock 
    {
        public CollisionSubReportBlock() : base(SubReportBlockType.Collisions) { }
        public ushort Reserved { get { return Utility.ReverseUnsignedShort(BitConverter.ToUInt16(Data, 0)); } set { BitConverter.GetBytes(Utility.ReverseUnsignedShort(value)).CopyTo(Data, 0); } }
        public uint CollisionSynchronizationSourceIdentifier { get { return Utility.ReverseUnsignedInt(BitConverter.ToUInt32(Data, 2)); } set { BitConverter.GetBytes(Utility.ReverseUnsignedInt(value)).CopyTo(Data, 2); } }
    }

    public class GeneralStatisticsSubReportBlock : SubReportBlock 
    {
        public GeneralStatisticsSubReportBlock() : base(SubReportBlockType.Stats) { }
        public ushort Reserved { get { return Utility.ReverseUnsignedShort(BitConverter.ToUInt16(Data, 0)); } set { BitConverter.GetBytes(Utility.ReverseUnsignedShort(value)).CopyTo(Data, 0); } }
        public byte MedianFractionLost { get { return Data[2]; } set { Data[2] = value; } }
        public uint HighestCumulativeNumberPacketsLost { get { return (uint)(Data[3] << 24 | Data[4] << 16 | Data[5] << 8 | byte.MaxValue); } set { Data[3] = (byte)((value >> 24) | 0xFF); Data[4] = (byte)((value >> 16) | 0xFF); Data[5] = (byte)((value >> 8) | 0xFF); } }
        public uint MedianInterArrivalJitter { get { return Utility.ReverseUnsignedInt(BitConverter.ToUInt32(Data, 6)); } set { BitConverter.GetBytes(Utility.ReverseUnsignedInt(value)).CopyTo(Data, 6); } }
    }

    public class BandwidthIndicationSubReportBlock : SubReportBlock 
    {
        public BandwidthIndicationSubReportBlock() : base(SubReportBlockType.Bandwidth) { }
        public bool Sender { get { return (Data[0] & 0x7F) == 1; } set { if (value) Data[0] |= (byte)(1 << 7); else Data[0] &= unchecked((byte)(~(1 << 7))); } }
        public bool Receiver { get { return (Data[0] & 0xBF) == 1; } set { if (value) Data[0] |= (byte)(1 << 6); else Data[0] &= unchecked((byte)(~(1 << 6))); } }
        public ushort Reserved { get { return (ushort)(Utility.ReverseUnsignedShort(BitConverter.ToUInt16(Data, 0)) & 0x3FFF); } set { BitConverter.GetBytes(Utility.ReverseUnsignedShort((ushort)(value & 0x3FFF))).CopyTo(Data, 0); } }
        public uint Bandwidth { get { return Utility.ReverseUnsignedInt(BitConverter.ToUInt32(Data, 2)); } set { BitConverter.GetBytes(Utility.ReverseUnsignedInt(value)).CopyTo(Data, 2); } } 
    }

    public class PacketSizeSubReportBlock : SubReportBlock 
    { 
        public PacketSizeSubReportBlock() : base(SubReportBlockType.GroupInfo) { }
        public ushort AveragePacketSize { get { return (ushort)(Utility.ReverseUnsignedShort(BitConverter.ToUInt16(Data, 0)) & 0xFFF0); } set { BitConverter.GetBytes(Utility.ReverseUnsignedShort((ushort)(value & 0xFFF0))).CopyTo(Data, 0); } }
        public uint ReceiverGroupSize { get { return Utility.ReverseUnsignedInt(BitConverter.ToUInt32(Data, 2)); } set { BitConverter.GetBytes(Utility.ReverseUnsignedInt(value)).CopyTo(Data, 2); } }  
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
        #region Properties

        //12 Bit
        public ushort NumberDistributionBuckets { get { return (ushort)(Utility.ReverseUnsignedShort(BitConverter.ToUInt16(Data, 0)) & 0xFFF0); } set { BitConverter.GetBytes(Utility.ReverseUnsignedShort((ushort)(value & 0xFFF0))).CopyTo(Data, 0); } }

        //4 Bit
        public byte MultiplicativeFactor
        {
            get { return (byte)(Data[3] & 0xF0); }
            set { Data[3] |= (byte)(value & 0xF0); }
        }

        uint m_MinimumDistributionValue { get { return Utility.ReverseUnsignedInt(BitConverter.ToUInt32(Data, 3)); } set { BitConverter.GetBytes(Utility.ReverseUnsignedInt(value)).CopyTo(Data, 3); } }

        uint m_MaximumDistributionValue { get { return Utility.ReverseUnsignedInt(BitConverter.ToUInt32(Data, 7)); } set { BitConverter.GetBytes(Utility.ReverseUnsignedInt(value)).CopyTo(Data, 7); } }  

        public virtual uint MinimumDistributionValue
        {
            get { return m_MinimumDistributionValue; }
            set { if (value >= m_MaximumDistributionValue) throw new ArgumentOutOfRangeException("Value must be greater than MaximumDistributionValue: " + m_MaximumDistributionValue); m_MinimumDistributionValue = value; }
        }

        public virtual uint MaximumDistributionValue
        {
            get { return m_MaximumDistributionValue; }
            set { if (value <= m_MinimumDistributionValue) throw new ArgumentOutOfRangeException("Value must be less than MinimumDistributionValue: " + m_MinimumDistributionValue); m_MaximumDistributionValue = value; }
        }

        /// <summary>
        /// The size of each distribution bucket in bits.
        /// </summary>
        public int BucketBitSize { get { return Length - 12 * 8 / NumberDistributionBuckets; } }

        public System.Collections.BitArray this[int index]
        {
            get
            {
                if (index < 0 || index > NumberDistributionBuckets) throw new ArgumentOutOfRangeException();
                System.Collections.BitArray source = new System.Collections.BitArray(Data.Skip(11).ToArray());
                source.Length = BucketBitSize;
                return source;
            }
            set
            {
                if (value.Length > BucketBitSize) throw new ArgumentOutOfRangeException();
                value.CopyTo(Data, (int)(MinimumDistributionValue + (index + 1) * (MaximumDistributionValue - MinimumDistributionValue) / NumberDistributionBuckets));
            }
        }

        #endregion

        public GenericSubReportBlock(SubReportBlockType blockType = SubReportBlockType.Unassigned) : base(blockType) { }
    }

    //LossSubReportBlock : GenericSubReportBlock

    public class LossSubReportBlock : GenericSubReportBlock
    {
        public LossSubReportBlock() : base(SubReportBlockType.Loss) { }

        public override uint MaximumDistributionValue
        {
            get
            {
                return base.MaximumDistributionValue;
            }
            set
            {
                if (value < 1 || value > 255) throw new ArgumentOutOfRangeException("Value must be in the range of 1 - 255.");
                base.MaximumDistributionValue = value;
            }
        }

        public override uint MinimumDistributionValue
        {
            get
            {
                return base.MinimumDistributionValue;
            }
            set
            {
                if (value < 0 || value > 254) throw new ArgumentOutOfRangeException("Value must be in the range of 0 - 254.");
                base.MinimumDistributionValue = value;
            }
        }
    }

    //JitterSubReportBlock : GenericSubReportBlock

    public class JitterSubReportBlock : GenericSubReportBlock
    {
        public JitterSubReportBlock() : base(SubReportBlockType.Jitter) { }
    }

    //RoundTripTimeSubReportBlock : GenericSubReportBlock

    public class RoundTripTimeSubReportBlock : GenericSubReportBlock
    {
        public RoundTripTimeSubReportBlock() : base(SubReportBlockType.RoundTripTime) { }

        TimeSpan RoundTripTime { get { return TimeSpan.FromMilliseconds(base.MaximumDistributionValue) - TimeSpan.FromMilliseconds(base.MinimumDistributionValue); } }
    }

    //CumulativeLossSubReportBlock : GenericSubReportBlock

    public class CumulativeLossSubReportBlock : GenericSubReportBlock
    {
        public CumulativeLossSubReportBlock() : base(SubReportBlockType.CumulativeLoss) { }

        public override uint MaximumDistributionValue
        {
            get
            {
                return base.MaximumDistributionValue;
            }
            set
            {
                if (value < 1 || value > 255) throw new ArgumentOutOfRangeException("Value must be in the range of 1 - 255.");
                base.MaximumDistributionValue = value;
            }
        }

        public override uint MinimumDistributionValue
        {
            get
            {
                return base.MinimumDistributionValue;
            }
            set
            {
                if (value < 0 || value > 254) throw new ArgumentOutOfRangeException("Value must be in the range of 0 - 254.");
                base.MinimumDistributionValue = value;
            }
        }

    }

    #endregion

}
