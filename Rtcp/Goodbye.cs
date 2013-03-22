using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.Rtcp
{
    public sealed class Goodbye : RtcpPacket, System.Collections.IEnumerable
    {
        #region Nested Types

        public class GoodbyeChunk
        {
            public uint SynchronizationSourceIdentifier;

            string m_Reason = string.Empty;

            public string Reason { get { return m_Reason; } set { if (!string.IsNullOrWhiteSpace(value) && value.Length > 255) throw new ArgumentException("Reason cannot be longer than 255 characters."); m_Reason = value; } }

            public GoodbyeChunk(uint ssrc, string reason = null) { SynchronizationSourceIdentifier = ssrc; Reason = reason; }

            public GoodbyeChunk(byte[] packet, ref int offset)
            {
                SynchronizationSourceIdentifier = Utility.ReverseUnsignedInt(BitConverter.ToUInt32(packet, offset));
                offset += 4;
                if (offset < packet.Length)
                {
                    int len = offset++;
                    Reason = Encoding.UTF8.GetString(packet, offset, len);
                    offset += len;
                }
                else Reason = string.Empty;
            }

            public byte[] ToBytes()
            {
                List<byte> result = new List<byte>();
                result.AddRange(BitConverter.GetBytes(Utility.ReverseUnsignedInt(SynchronizationSourceIdentifier)));
                if (!string.IsNullOrWhiteSpace(Reason))
                {
                    result.Add((byte)Encoding.UTF8.GetByteCount(Reason));
                    result.AddRange(Encoding.UTF8.GetBytes(Reason));
                }
                return result.ToArray();
            }

            public int Length { get { return 4 + (string.IsNullOrWhiteSpace(Reason) ? 0 : 1 + Reason.Length); } }
        }

        #endregion

        #region Properties

        public GoodbyeChunk this[int index]
        {
            get
            {
                if (index < 0 || index > BlockCount) throw new ArgumentOutOfRangeException();
                if (index == 0)
                {
                    return new GoodbyeChunk(Payload, ref index);
                }
                else
                {
                    int offset = 0;
                    for (int i = 0; i < index; ++i)
                    {
                        offset += this[i].Length;                        
                    }
                    return new GoodbyeChunk(Payload, ref offset);
                }
            }
            set
            {
                if (index < 0 || index > BlockCount) throw new ArgumentOutOfRangeException();
                List<byte> temp = new List<byte>(Payload);

                if (value == null)
                {
                    Remove(index);
                    return;
                }

                GoodbyeChunk old = this[index];

                if (index == 0)
                {
                    temp.RemoveRange(0, old.Length);
                    temp.AddRange(value.ToBytes());
                }
                else
                {
                    int offset = 0;
                    for (int i = 0; i < index; ++i)
                    {
                        offset += this[i].Length;
                    }
                    //Add new bytes at correct offset
                    temp.InsertRange(offset, value.ToBytes());
                    //Remove old bytes at offet + new value length, count = old.Length
                    temp.RemoveRange(offset + value.Length, old.Length);
                }
                Payload = temp.ToArray();
            }
        }

        public IEnumerable<GoodbyeChunk> Chunks { get { return (IEnumerable<GoodbyeChunk>)GetEnumerator(); } }

        #endregion

        #region Constructor

        public Goodbye(byte? channel = null) : base(RtcpPacketType.Goodbye, channel) { Payload = new byte[0]; }

        public Goodbye(RtcpPacket packet) : base(packet) { if (packet.PacketType != RtcpPacket.RtcpPacketType.Goodbye) throw new Exception("Invalid Packet Type, Expected Goodbye. Found: '" + (byte)packet.PacketType + '\''); }

        public Goodbye(byte[] packet, int index) : base(packet, index, RtcpPacketType.Goodbye) { }

        #endregion

        public void Remove(int index)
        {
            if (index < 0 || index > BlockCount) throw new ArgumentOutOfRangeException("index", "Cannot be less than 0 or greater than BlockCount");
            List<byte> temp = new List<byte>(Payload);
            BlockCount--;
            GoodbyeChunk toRemove = this[index];
            if (index == 0)
            {
                temp.RemoveRange(0, toRemove.Length);
            }
            else
            {
                int offset = 0;
                for (int i = 0; i < index; ++i)
                {
                    offset += this[i].Length;
                }
                temp.RemoveRange(offset, toRemove.Length);
            }
            Payload = temp.ToArray();
        }

        public void Add(GoodbyeChunk chunk)
        {
            List<byte> temp = new List<byte>(Payload);
            BlockCount++;
            temp.AddRange(chunk.ToBytes());
            Payload = temp.ToArray();
        }

        public void Insert(int index, GoodbyeChunk chunk)
        {
            List<byte> temp = new List<byte>(Payload);
            BlockCount++;
            if (index == 0)
            {
                temp.AddRange(chunk.ToBytes());
            }
            else
            {
                int offset = 0;
                for (int i = 0; i < index; ++i)
                {
                    offset += this[i].Length;
                }
                //Add new bytes at correct offset
                temp.InsertRange(offset, chunk.ToBytes());
            }
            Payload = temp.ToArray();
        }

        public System.Collections.IEnumerator GetEnumerator() { for (int i = 0; i < BlockCount; ++i) yield return this[i]; }
    }
}
