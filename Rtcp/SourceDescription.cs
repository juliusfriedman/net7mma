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

            byte m_Length;

            string m_Text;

            #endregion

            #region Properties

            public SourceDescriptionType DescriptionType { get { return (SourceDescriptionType)m_Type; } set { m_Type = (byte)value; } }

            public byte Length { get { return m_Length; } }

            public string Text
            {
                get { return m_Text; }
                set
                {
                    if (string.IsNullOrEmpty(value)) m_Length = 0;
                    else if (value.Length > 255) throw new ArgumentOutOfRangeException("value", "Cannot exceed 255 characters");
                    else
                    {
                        m_Text = value; m_Length = (byte)Encoding.UTF8.GetByteCount(m_Text);
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
            public SourceDescriptionItem(byte[] packet, ref int offset)
            {
                m_Type = packet[offset ++];
                m_Length = packet[offset ++];
                if (m_Length > 0)
                {
                    m_Text = Encoding.UTF8.GetString(packet, offset, m_Length);
                }
            }

            #endregion

            #region Methods

            public byte[] ToBytes()
            {
                List<byte> result = new List<byte>();

                result.Add(m_Type);
                result.Add((byte)m_Length);
                if (m_Length > 0)
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

            SynchronizationSourceIdentifier = (uint)System.Net.IPAddress.NetworkToHostOrder(BitConverter.ToInt32(packet, offset));

            offset = 4;

            //To cache the value of the cast because we can't use == / != on SourceDescriptionType because of enum
            byte SourceDescriptionEnd = (byte)SourceDescriptionType.End;

            while (offset < packet.Length && packet[offset] != SourceDescriptionEnd)
            {
                SourceDescriptionItem item = new SourceDescriptionItem(packet, ref offset);
                Items.Add(item);
                //offset += item.Length + 2; //Type and Length bytes (Now handled with ref)
            }

        }

        public SourceDescription(uint ssrc) { SynchronizationSourceIdentifier = ssrc; }

        public SourceDescription(RtcpPacket packet)
            : this(packet.Data, 0)
        {
            if (packet.PacketType != RtcpPacket.RtcpPacketType.SourceDescription) throw new Exception("Invalid Packet Type");

            //Read each Item...
            int offset = 0;
            while (offset < packet.Data.Length)
            {
                SourceDescriptionItem item = new SourceDescriptionItem(packet.Data, ref offset);
                Items.Add(item);
            }
        }

        #endregion

        public RtcpPacket ToPacket()
        {
            RtcpPacket output = new RtcpPacket(RtcpPacket.RtcpPacketType.SourceDescription);
            output.BlockCount = Items.Count;
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

            //Ensure header values right here but this is done when required in case some one wants to mangle with these fields for some reason
            //m_Count = Items.Count;
            //Length = (short)result.Count();

            //Align to multiple of 4 for rtcp Length
            while (result.Count % 4 != 0) result.Add(0);

            return result.ToArray();
        }

        public static implicit operator RtcpPacket(SourceDescription description) { return description.ToPacket(); }

        public static implicit operator SourceDescription(RtcpPacket packet) { return new SourceDescription(packet); }
    }
}
