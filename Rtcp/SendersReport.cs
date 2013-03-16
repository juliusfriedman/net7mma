using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.Rtcp
{
    public sealed class SendersReport : RtcpPacket, System.Collections.IEnumerable
    {
        #region Fields

        public uint SynchronizationSourceIdentifier { get { return Utility.ReverseUnsignedInt(BitConverter.ToUInt32(Payload, 0)); } set { BitConverter.GetBytes(Utility.ReverseUnsignedInt(value)).CopyTo(Payload, 0); } }

        //2 Words which make up the NtpTimestamp
        internal uint m_NtpMsw { get { return Utility.ReverseUnsignedInt(BitConverter.ToUInt32(Payload, 4)); } set { BitConverter.GetBytes(Utility.ReverseUnsignedInt(value)).CopyTo(Payload, 4); } }
        internal uint m_NtpLsw { get { return Utility.ReverseUnsignedInt(BitConverter.ToUInt32(Payload, 8)); } set { BitConverter.GetBytes(Utility.ReverseUnsignedInt(value)).CopyTo(Payload, 8); } }
        public ulong NtpTimestamp { get { return (ulong)m_NtpMsw << 32 | m_NtpLsw; } set { m_NtpLsw = (uint)(value & uint.MaxValue); m_NtpMsw = (uint)(value >> 32); } }

        #endregion

        #region Properties

        public uint RtpTimestamp { get { return Utility.ReverseUnsignedInt(BitConverter.ToUInt32(Payload, 12)); } set { BitConverter.GetBytes(Utility.ReverseUnsignedInt(value)).CopyTo(Payload, 12); } }

        public uint SendersPacketCount { get { return Utility.ReverseUnsignedInt(BitConverter.ToUInt32(Payload, 16)); } set { BitConverter.GetBytes(Utility.ReverseUnsignedInt(value)).CopyTo(Payload, 16); } }

        public uint SendersOctetCount { get { return Utility.ReverseUnsignedInt(BitConverter.ToUInt32(Payload, 20)); } set { BitConverter.GetBytes(Utility.ReverseUnsignedInt(value)).CopyTo(Payload, 20); } }

        public ReportBlock this[int index]
        {
            get
            {
                if (index < 0 || index > BlockCount) throw new ArgumentOutOfRangeException();

                //Determine offset of block
                int offset = 0;
                if (index > 0) offset = 24 + (ReportBlock.Size * index);
                else offset = 24;

                return new ReportBlock(Payload, ref offset);
            }
            set
            {
                if (index < 0 || index > BlockCount) throw new ArgumentOutOfRangeException();
                if (value == null)
                {
                    Remove(index);
                    return;
                }
                else
                {
                    //Blocks[index] = value;

                    //Determine offset of block
                    int offset = 0;
                    if (index > 0) offset = 24 + (ReportBlock.Size * index);
                    else offset = 24;

                    value.ToBytes().CopyTo(Payload, offset);
                }
            }
        }

        #endregion

        #region Constructor

        public SendersReport(uint ssrc) : base(RtcpPacketType.SendersReport) { Payload = new byte[24]; SynchronizationSourceIdentifier = ssrc; }

        public SendersReport(byte[] packet, int offset) : base(packet, offset, RtcpPacketType.SendersReport) { }

        public SendersReport(RtcpPacket packet) : base(packet) { if (packet.PacketType != RtcpPacket.RtcpPacketType.SendersReport) throw new Exception("Invalid Packet Type, Expected SendersReport. Found: '" + (byte)packet.PacketType + '\''); }
            
        #endregion

        #region Methods

        public void Add(ReportBlock reportBlock) { BlockCount++; List<byte> temp = new List<byte>(Payload); temp.AddRange(reportBlock.ToBytes()); Payload = temp.ToArray(); }

        public void Clear() { BlockCount = 0; Payload = BitConverter.GetBytes(Utility.ReverseUnsignedInt(SynchronizationSourceIdentifier)); }

        public void Remove(int index)
        {
            if (index < 0 || index > BlockCount) throw new ArgumentOutOfRangeException();

            BlockCount--;

            //Determine offset of block
            int offset = 0;
            if (index > 0) offset = 24 + (ReportBlock.Size * index);
            else offset = 24;

            List<byte> temp = new List<byte>(Payload);
            temp.RemoveRange(offset, ReportBlock.Size);
            Payload = temp.ToArray();
        }

        public void Insert(int index, ReportBlock reportBlock)
        {
            if (index < 0 || index > BlockCount) throw new ArgumentOutOfRangeException();

            BlockCount++;

            //Determine offset of block
            int offset = 0;
            if (index > 0) offset = 24 + (ReportBlock.Size * index);
            else offset = 24;

            List<byte> temp = new List<byte>(Payload);
            temp.InsertRange(offset, reportBlock.ToBytes());
            Payload = temp.ToArray();
        }

        public System.Collections.IEnumerator GetEnumerator() { for (int i = 0; i < BlockCount; ++i) yield return this[i]; }

        #endregion
    }
}
