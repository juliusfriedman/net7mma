using Media.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Rtmp
{
    //http://en.wikipedia.org/wiki/Real_Time_Messaging_Protocol#Specification_document
    public class RtmpPacket : BaseDisposable, IPacket
    {

        public RtmpPacket(byte[] packet) { m_Packet = packet; }

        byte[] m_Packet = Utility.Empty;

        public byte PacketType
        {
            get { return (byte)(m_Packet[0] & 0xf0); }
            set { m_Packet[0] = (byte)(value & 0x0f | StreamId); }
        }

        public byte StreamId
        {
            get { return (byte)(m_Packet[0] & 0x0f); }
            set { m_Packet[0] = (byte)(PacketType | (byte)(value >> 4)); }
        }

        public int HeaderLength
        {
            get
            {
                switch (PacketType)
                {
                    case 0: return 12;
                    case 1: return 8;
                    case 2: return 4;
                    case 3: return 1;
                    default: return 0;
                }
            }
        }

        public byte MessageType
        {
            get
            {
                return m_Packet[HeaderLength - 1];
            }
        }

        public int MessageLength
        {
            get
            {
                return Binary.ReadU16(m_Packet, HeaderLength + 1, !BitConverter.IsLittleEndian);
            }
            set
            {
                Binary.Write16(m_Packet, HeaderLength + 1, !BitConverter.IsLittleEndian, (ushort)value);
            }
        }

        public bool HasTimesamp
        {
            get
            {
                return PacketType > 0;
            }
        }

        public int? Timestamp
        {
            get
            {
                if (HasTimesamp)
                {
                    return (int)Binary.ReadU32(m_Packet, HeaderLength - 4, !BitConverter.IsLittleEndian);
                }

                return null;
            }
            set
            {
                if (HasTimesamp)
                {
                    Binary.Write32(m_Packet, HeaderLength - 4, !BitConverter.IsLittleEndian, (uint)value);
                }
            }
        }

        IEnumerable<byte> Payload { get { return m_Packet.Skip(HeaderLength); } }

        public readonly DateTime Created = DateTime.UtcNow;
        
        public readonly DateTime? Transferred;

        DateTime IPacket.Created
        {
            get { return Created; }
        }

        DateTime? IPacket.Transferred
        {
            get { return Transferred; }
        }

        public virtual bool IsComplete
        {
            get { return m_Packet.Length >= HeaderLength + MessageLength; }
        }

        public virtual bool IsCompressed { get { return false; } }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public long Length
        {
            get { return m_Packet.Length; }
        }

        public int CompleteFrom(System.Net.Sockets.Socket socket, Common.MemorySegment buffer)
        {
            if (IsComplete) return 0;

            int contained = m_Packet.Length,
                needed = MessageLength - contained - HeaderLength, recieved = 0;

            int r = buffer.Offset;

            while (needed > 0)
            {
                r += socket.Receive(buffer.Array, r, needed, System.Net.Sockets.SocketFlags.None);
                needed -= r;
                recieved += r;
            }

            m_Packet = m_Packet.Concat(buffer).ToArray();

            return recieved;

        }

        public IEnumerable<byte> Prepare()
        {
            return m_Packet;
        }

        public override void Dispose()
        {
            if (!ShouldDispose || IsDisposed) return;
            m_Packet = null;
            base.Dispose();
        }
    }
}
