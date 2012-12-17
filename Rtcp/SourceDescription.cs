using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.Rtcp
{
    public class SourceDescription
    {
        #region Nested Types

        public enum SourceDescriptionType : byte
        {
            End = 0,
            CName = 1,
            Name = 2,
            Email = 3,
            Phone = 4,
            Location = 5,
            Tool = 6,
            Note = 7,
            Private = 8
        }

        public class SourceDescriptionItem
        {
            public readonly static SourceDescriptionItem Empty = new SourceDescriptionItem(SourceDescriptionType.End);

            public readonly static SourceDescriptionItem CName = new SourceDescriptionItem(SourceDescriptionType.CName, Environment.MachineName);

            #region Fields

            byte m_Type;

            int m_Length;

            string m_Text;

            #endregion

            #region Properties

            public SourceDescriptionType DescriptionType { get { return (SourceDescriptionType)m_Type; } set { m_Type = (byte)value; } }

            public int Length { get { return m_Length; } }

            public string Text
            {
                get { return m_Text; }
                set
                {
                    if (string.IsNullOrEmpty(value)) m_Length = 0;
                    else if (value.Length > 255) throw new ArgumentOutOfRangeException("value", "Cannot exceed 255 characters");
                    else
                    {
                        m_Text = value; m_Length = Encoding.UTF8.GetByteCount(m_Text);
                    }
                }
            }

            #endregion

            #region Constructor

            public SourceDescriptionItem(SourceDescription.SourceDescriptionType type, string text = null)
            {
                DescriptionType = type;
                Text = text;
            }

            //Used to take RtcpPacket
            public SourceDescriptionItem(byte[] packet, int offset)
            {
                m_Type = packet[offset + 0];
                m_Length = packet[offset + 1];
                //Could check length to make sure its 255 or lower...
                m_Text = Encoding.UTF8.GetString(packet, offset + 2, m_Length);
            }

            #endregion

            #region Methods

            public byte[] ToBytes()
            {
                List<byte> result = new List<byte>();

                result.Add(m_Type);
                result.Add((byte)m_Length);
                if (Length > 0)
                {
                    result.AddRange(Encoding.UTF8.GetBytes(m_Text));
                }

                return result.ToArray();
            }

            #endregion
        }

        #endregion

        #region Properties

        public uint SynchronizationSourceIdentifier { get; set; }

        public List<SourceDescriptionItem> Items = new List<SourceDescriptionItem>();

        #endregion

        #region Constructor

        public SourceDescription(byte[] packet, int offset) 
        {

            //SynchronizationSourceIdentifier = (uint)(packet[offset + 0] << 24 | packet[offset + 1] << 16 | packet[offset + 2] << 8 | packet[offset + 3]);

            SynchronizationSourceIdentifier = (uint)System.Net.IPAddress.NetworkToHostOrder(BitConverter.ToInt32(packet, offset));

            offset = 4;

            byte SourceDescriptionEnd = (byte)SourceDescriptionType.End;

            while (offset < packet.Length && packet[offset] != SourceDescriptionEnd)
            {
                SourceDescriptionItem item = new SourceDescriptionItem(packet, offset);
                Items.Add(item);
                offset += item.Length + 2; //Type and Length
            }

        }

        public SourceDescription(uint ssrc) { SynchronizationSourceIdentifier = ssrc; }

        public SourceDescription(RtcpPacket packet)
            : this(packet.Data, 0)
        {
            if (packet.PacketType != RtcpPacket.RtcpPacketType.SourceDescription) throw new Exception("Invalid Packet Type");
        }

        #endregion

        public RtcpPacket ToPacket()
        {
            RtcpPacket output = new RtcpPacket(RtcpPacket.RtcpPacketType.SourceDescription);
            output.Data = ToBytes();
            return output;
        }

        public byte[] ToBytes()
        {
            List<byte> result = new List<byte>();

            //Add Ssrc
            result.AddRange(BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder((int)SynchronizationSourceIdentifier)));

            //Add Sded Items
            foreach (SourceDescriptionItem item in Items) result.AddRange(item.ToBytes());
                            
            //Add terminator
            result.AddRange(SourceDescriptionItem.Empty.ToBytes());

            //Ensure header values
            //m_Count = Items.Count;
            //Length = (short)result.Count();

            //Align to multiple of 4 for rtcp Length
            while (result.Count % 4 != 0) result.Add(0);

            //Header
            //result.InsertRange(0, base.ToBytes());

            return result.ToArray();
        }

        public static implicit operator RtcpPacket(SourceDescription description) { return description.ToPacket(); }

        public static implicit operator SourceDescription(RtcpPacket packet) { return new SourceDescription(packet); }
    }
}
