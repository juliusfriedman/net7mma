using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.Rtcp
{
    public sealed class SourceDescription : RtcpPacket, System.Collections.IEnumerable
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

        public sealed class SourceDescriptionItem
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
                if (DescriptionType == SourceDescriptionType.End) return;
                m_Length = packet[offset ++];
                if (m_Length > 0) m_Text = Encoding.UTF8.GetString(packet, offset, m_Length);
                offset += m_Length;
            }

            #endregion

            #region Methods

            public byte[] ToBytes()
            {
                List<byte> result = new List<byte>();

                result.Add(m_Type);
                if (DescriptionType != SourceDescriptionType.End)
                {
                    result.Add((byte)m_Length);
                    if (m_Length > 0)
                    {
                        result.AddRange(Encoding.UTF8.GetBytes(m_Text));
                    }
                }

                return result.ToArray();
            }

            #endregion
        }

        public sealed class SourceDescriptionChunk
        {
            public SourceDescriptionChunk(uint ssrc, IEnumerable<SourceDescriptionItem> items)
            {
                SynchronizationSourceIdentifier = ssrc;
                foreach (SourceDescriptionItem item in items) Items.Add(item);
            }

            public SourceDescriptionChunk(uint ssrc, SourceDescriptionItem item) : this(ssrc, new SourceDescriptionItem[] { item }) { }

            public SourceDescriptionChunk(byte[] packet, ref int index)
            {
                SynchronizationSourceIdentifier = Utility.ReverseUnsignedInt(BitConverter.ToUInt32(packet, index));
                index += 4;
                //Items
                while (index < packet.Length)
                {
                    SourceDescriptionItem item = new SourceDescriptionItem(packet, ref index);
                    if (item.DescriptionType == SourceDescriptionType.End) break;
                    Items.Add(item);
                }
            }

            public int Length
            {
                get
                {
                    int result = 4;
                    Items.ForEach(i => result += i.Length);
                    return result;
                }
            }

            public uint SynchronizationSourceIdentifier { get; set; }

            public List<SourceDescriptionItem> Items = new List<SourceDescriptionItem>();

            public byte[] ToBytes() { List<byte> result = new List<byte>(); result.AddRange(BitConverter.GetBytes(Utility.ReverseUnsignedInt(SynchronizationSourceIdentifier))); Items.ForEach(i => result.AddRange(i.ToBytes())); result.AddRange(SourceDescriptionItem.Empty.ToBytes()); while (result.Count % 4 != 0) result.Add(0); return result.ToArray(); }
        }

        #endregion

        #region Properties

        public uint SynchronizationSourceIdentifier { get { return Utility.ReverseUnsignedInt(BitConverter.ToUInt32(Payload, 0)); } set { BitConverter.GetBytes(Utility.ReverseUnsignedInt(value)).CopyTo(Payload, 0); } }

        public SourceDescription.SourceDescriptionChunk this[int index]
        {
            get
            {
                if (index < 0 || index > BlockCount) throw new ArgumentOutOfRangeException();

                SourceDescription.SourceDescriptionChunk result = null;

                foreach (SourceDescription.SourceDescriptionChunk item in this)
                {
                    result = item;
                    if (--index <= 0) break;
                }

                return result;
            }
            set
            {
                if (index < 0 || index > BlockCount) throw new ArgumentOutOfRangeException();
                Remove(index);
                if (value != null) Insert(index, value);
            }
        }

        #endregion

        #region Constructor

        public SourceDescription(byte[] packet, int offset) : base(packet, offset, RtcpPacketType.SourceDescription){ }

        public SourceDescription(byte? channel = null) : base(RtcpPacketType.SourceDescription, channel) { Payload = new byte[4]; }

        public SourceDescription(RtcpPacket packet)
            : base(packet)
        {
            if (packet.PacketType != RtcpPacket.RtcpPacketType.SourceDescription) throw new Exception("Invalid Packet Type, Expected SourceDescription. Found: '" + (byte)packet.PacketType + '\'');
        }

        #endregion

        #region Methods

        public void Add(SourceDescription.SourceDescriptionChunk item)
        {
            BlockCount++; 
            List<byte> temp = new List<byte>(Payload); 
            temp.AddRange(item.ToBytes()); 
            Payload = temp.ToArray();
        }

        public void Clear() { BlockCount = 0; Payload = BitConverter.GetBytes(Utility.ReverseUnsignedInt(SynchronizationSourceIdentifier)); }

        public void Remove(int index)
        {
            if (index < 0 || index > BlockCount) throw new ArgumentOutOfRangeException();

            BlockCount--;

            //Determine offset of block
            int offset = 0, len = 0;
            for (int i = 0; i < index; ++i)
            {
                if (i == index)
                {
                    offset += len = this[i].Length;
                }
                else
                {

                    offset += this[i].Length;
                }
            }

            List<byte> temp = new List<byte>(Payload);
            temp.RemoveRange(offset, len);
            Payload = temp.ToArray();
        }

        public void Insert(int index, SourceDescription.SourceDescriptionChunk newItem)
        {
            if (index < 0 || index > BlockCount) throw new ArgumentOutOfRangeException();

            BlockCount++;

            //Determine offset of block
            int offset = 0;
            for (int i = 0; i < index; ++i) offset += this[i].Length;

            List<byte> temp = new List<byte>(Payload);
            temp.InsertRange(offset, newItem.ToBytes());
            Payload = temp.ToArray();
        }

        public System.Collections.IEnumerator GetEnumerator() 
        {
            SourceDescription.SourceDescriptionChunk chunk = null;
            for (int i = 0, offset = 0; i < BlockCount; ++i)
            {
                try { chunk = new SourceDescription.SourceDescriptionChunk(Payload, ref offset); }
                catch { break; }
                yield return chunk;
            }
        }

        #endregion
    }
}
