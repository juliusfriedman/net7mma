using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.Rtcp
{
    public sealed class ReceiversReport : RtcpPacket, System.Collections.IEnumerable
    {
        #region Constructor

        public ReceiversReport(uint ssrc) : base(RtcpPacketType.ReceiversReport) { Payload = new byte[4]; SendersSynchronizationSourceIdentifier = ssrc; }

        public ReceiversReport(RtcpPacket packet)
            : base(packet)
        {
            if (packet.PacketType != RtcpPacket.RtcpPacketType.ReceiversReport) throw new Exception("Invalid Packet Type, Expected RecieversReport. Found: '" + (byte)packet.PacketType + '\'');
        }

        public ReceiversReport(byte[] packet, int offset) : base(packet, offset, RtcpPacketType.ReceiversReport) { }

        #endregion

        #region Properties
        
        public uint SendersSynchronizationSourceIdentifier { get { return Utility.ReverseUnsignedInt(BitConverter.ToUInt32(Payload, 0)); } set { BitConverter.GetBytes(Utility.ReverseUnsignedInt(value)).CopyTo(Payload, 0); } }

        public ReportBlock this[int index]
        {
            get
            {
                if (index < 0 || index > BlockCount) throw new ArgumentOutOfRangeException();

                //Determine offset of block
                int offset = 0;
                if (index > 0) offset = 4 + (ReportBlock.Size * index);
                else offset = 4;

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
                    if (index > 0) offset = 4 + (ReportBlock.Size * index);
                    else offset = 4;

                    value.ToBytes().CopyTo(Payload, offset);
                }
            }
        }

        #endregion

        #region Methods

        public void Add(ReportBlock reportBlock) { BlockCount++; List<byte> temp = new List<byte>(Payload); temp.AddRange(reportBlock.ToBytes()); Payload = temp.ToArray(); }

        public void Clear() { BlockCount = 0; Payload = BitConverter.GetBytes(Utility.ReverseUnsignedInt(SendersSynchronizationSourceIdentifier)); }

        public void Remove(int index)
        {
            if (index < 0 || index > BlockCount) throw new ArgumentOutOfRangeException();

            BlockCount--;

            //Determine offset of block
            int offset = 0;
            if (index > 0) offset = 4 + (ReportBlock.Size * index);
            else offset = 4;

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
            if (index > 0) offset = 4 + (ReportBlock.Size * index);
            else offset = 4;

            List<byte> temp = new List<byte>(Payload);
            temp.InsertRange(offset, reportBlock.ToBytes());
            Payload = temp.ToArray();
        }

        public System.Collections.IEnumerator GetEnumerator() { for (int i = 0; i < BlockCount; ++i) yield return this[i]; }

        #endregion
    }

}
