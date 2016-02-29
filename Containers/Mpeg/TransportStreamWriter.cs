using System;
using System.Collections.Generic;

namespace Media.Containers.Mpeg
{
    /// <summary>
    /// 
    /// </summary>
    /// <notes>
    /// Needs BitWriter to ensure writing is effecient on all architectures.
    /// </notes>
    class TransportStreamWriter
    {

        #region Fields

        int m_PacketsWritten;

        int m_BytesWritten;

        //

        private Dictionary<ushort, int> m_continuityCounter = new Dictionary<ushort, int>();
        
        private double m_systemTimeClock; // In seconds (to avoid integer precision issues)

        #endregion

        #region Readonly Fields

        /// <summary>
        /// PTS starts at an arbitrary value, it's not specified in the standard.
        /// Most programs/devices that produce transport streams start at 0 for the beginning of each stream.
        /// tsMuxeR uses 378000000.
        /// </summary>
        public readonly int ProgramClockReferenceBase = 0;

        // time units per second, [ISO/IEC 13818-1] "Time stamps are generally in units of 90 KHz".
        public readonly int TimestampResolution = 90000;

        public readonly int HeaderLength = 4;

        public readonly int PacketLength = 188;

        // [2.B Audio Visual Application Format Specifications for BD-ROM]:
        // "The maximum multiplex rate of the BDAV MPEG-2 Transport Stream is 48Mbps"
        public readonly long MaximumMultiplexRate = 48000000L;

        #endregion

        #region Methods

        /// <param name="systemTimeClock">in units of 90 KHz</param>
        public void WritePCRPacket(ushort pid)
        {
            // In a packet that contains a PCR, the PCR will be a few ticks later than the arrival_time_stamp.
            // The exact difference between the arrival_time_stamp and the PCR (and the number of bits between them)
            // indicates the intended fixed bitrate of the variable rate Transport Stream.
            byte[] packet = new byte[PacketLength];

            //Should not reverse on BigEndian CPU when writing BigEndian... See notes

            //Pid
            Common.Binary.WriteBigEndianBinaryInteger(packet, 0, 13, 16, pid);

            //Scrambling
            Common.Binary.WriteBigEndianBinaryInteger(packet, 0, 29, 2, 0);

            //AdaptationFieldControl
            Common.Binary.WriteBigEndianBinaryInteger(packet, 0, 31, 2, 3);

            //AdaptationField.FieldLength
            Common.Binary.WriteBigEndianBinaryInteger(packet, 5, 0, 8, PacketLength - HeaderLength);

            //packet.AdaptationField.PCRFlag = true;
            Common.Binary.WriteBigEndianBinaryInteger(packet, 6, 1, 1, 1);

            //packet.AdaptationField.ProgramClockReferenceBase = (ulong)(ProgramClockReferenceBase + m_systemTimeClock * TimestampResolution);
            Common.Binary.WriteBigEndianBinaryInteger(packet, 6, 5, 33, (ulong)(ProgramClockReferenceBase + m_systemTimeClock * TimestampResolution));
            
            // Note: The PCR represent the system clock, and thus must be equal to or greater than the value in the previous packet.

            // [ISO/IEC 13818-1] The continuity_counter shall not be incremented when the adaptation_field_control of the packet equals '00' or '10'.
            WritePacketAndIncrementClock(packet, false);
        }

        private void WritePacketAndIncrementClock(byte[] packet, bool incrementContinuityCountr)
        {
            if (HeaderLength != 4)
            {
                // Note: both ArrivalTimeStamp and the PCR represent the system clock, and thus must be equal to or
                // greater than the value in the previous packet.
                // 27 MHz = 300 * 90 KHz
                Common.Binary.WriteBigEndianBinaryInteger(packet, 0, 0, 32, (uint)(ProgramClockReferenceBase + 300 * m_systemTimeClock * TimestampResolution));
            }

            ushort pid = (ushort)TransportStreamReader.GetPacketIdentifier(null, packet);

            if (false == m_continuityCounter.ContainsKey(pid))
            {
                m_continuityCounter[pid] = 0;
            }

            int continuityCounter = m_continuityCounter[pid];

            //ScramblingControl 2 bit
            //AdaptationFieldControl 2 bit

            //packet.Header.ContinuityCounter (last 4 bits)
            packet[3] = (byte)(((continuityCounter % 16) & Common.Binary.FourBitMaxValue) >> 4);
            
            //m_stream.WritePacket(packet);
            //WriteBytes(packet.GetBytes());

            double packetTimeSpan = (double)packet.Length / MaximumMultiplexRate;
            
            m_systemTimeClock += packetTimeSpan;

            if (incrementContinuityCountr)
            {
                m_continuityCounter[pid]++;
            }
        }

        public void FillAlignedUnit(int alignmentBoundary)
        {
            while (m_BytesWritten % alignmentBoundary != 0)
            {
                byte[] nullPacket = new byte[PacketLength];

                //nullPacket.Header.PID = Mpeg2TransportStream.NullPacketPID;

                //Pid
                Common.Binary.WriteBigEndianBinaryInteger(nullPacket, 0, 13, 16, (long)TransportStreamUnit.PacketIdentifier.NullPacket);
                
                WritePacketAndIncrementClock(nullPacket, false);
            }
        }

        private List<byte[]> Packetize(byte[] payload, bool isStuffingAllowed)
        {
            List<byte[]> result = new List<byte[]>();
            int maxPayloadBytesPerPacket = PacketLength - HeaderLength;
            int packetCount = (int)Math.Ceiling((double)payload.Length / maxPayloadBytesPerPacket);
            for (int index = 0; index < packetCount; index++)
            {
                byte[] packet = new byte[PacketLength];
                
                //packet.Header.PayloadUnitStartIndicator = (index == 0);
                
                //packet.Header.PayloadExist = true;

                int bytesInPayload = Common.Binary.Min(payload.Length - index * maxPayloadBytesPerPacket, maxPayloadBytesPerPacket);
                
                if ((bytesInPayload < maxPayloadBytesPerPacket) && !isStuffingAllowed)
                {
                    // Packet stuffing bytes of value 0xFF may be found in the payload of Transport Stream packets carrying PSI and/or private_sections.
                    // i.e. for PES we must use adaptation field
                    //packet.Header.AdaptationFieldExist = true;
                    
                    //packet.AdaptationField.FieldLength = (byte)(maxPayloadBytesPerPacket - bytesInPayload - 1);
                }

                Array.Copy(payload, index * maxPayloadBytesPerPacket, packet, HeaderLength, maxPayloadBytesPerPacket);

                result.Add(packet);
            }

            return result;
        }

        /// <param name="systemTimeClock">seconds since the first packet was written</param>
        public void SetSystemTimeClock(double systemTimeClock)
        {
            m_systemTimeClock = systemTimeClock;
        }

        #endregion

    }
}
