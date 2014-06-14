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

        public byte MessageLength
        {
            get
            {
                return m_Packet[HeaderLength + 1];
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

        public bool IsComplete
        {
            get { return m_Packet.Length >= HeaderLength + MessageLength; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public long Length
        {
            get { return m_Packet.Length; }
        }

        public void CompleteFrom(System.Net.Sockets.Socket socket)
        {
            if (IsComplete) return;

            int contained =  m_Packet.Length,
                needed = MessageLength - contained - HeaderLength;

            byte[] buffer = Enumerable.Repeat(default(byte), needed).ToArray();

            int r = 0;

            while (needed > 0)
            {
                r += socket.Receive(buffer, r, needed, System.Net.Sockets.SocketFlags.None);
                needed -= r;
            }

            m_Packet = m_Packet.Concat(buffer).ToArray();

            buffer = null;
        }

        public IEnumerable<byte> Prepare()
        {
            return m_Packet;
        }

        public override void Dispose()
        {
            if (!ShouldDispose || Disposed) return;
            m_Packet = null;
            base.Dispose();
        }
    }
}
