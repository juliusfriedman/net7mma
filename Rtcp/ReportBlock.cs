using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.Rtcp
{
    public class ReportBlock
    {
        public const int Size = 24;

        public uint SynchronizationSourceIdentifier, FractionLost, ExtendedHigestSequenceNumber, InterArrivalJitter, LastSendersReport, DelaySinceLastSendersReport;
        public int CumulativePacketsLost;

        public ReportBlock(uint ssrc)
        {
            SynchronizationSourceIdentifier = ssrc;
        }

        public ReportBlock(byte[] packet, ref int index)
        {
            if (packet.Length - index < Size) throw new ArgumentOutOfRangeException("index", "Must allow 24 bytes in buffer");

            //Should check endian before swapping
            SynchronizationSourceIdentifier = Utility.SwapUnsignedInt(BitConverter.ToUInt32(packet, index));
            index += 4;

            FractionLost = packet[index++];

            //Should check endian before writing
            CumulativePacketsLost = (packet[index++] << 24 | packet[index++] << 16 | packet[index++] << 8 | byte.MaxValue);

            //Should check endian before swapping
            ExtendedHigestSequenceNumber = Utility.SwapUnsignedInt(BitConverter.ToUInt32(packet, index));
            index += 4;

            //Should check endian before swapping
            InterArrivalJitter = Utility.SwapUnsignedInt(BitConverter.ToUInt32(packet, index));
            index += 4;

            //Should check endian before swapping
            LastSendersReport = Utility.SwapUnsignedInt(BitConverter.ToUInt32(packet, index));
            index += 4;

            //Should check endian before swapping
            DelaySinceLastSendersReport = Utility.SwapUnsignedInt(BitConverter.ToUInt32(packet, index));
            index += 4;
        }

        public byte[] ToBytes(uint? ssrc = null)
        {
            List<byte> result = new List<byte>();

            //result.Add((byte)((SynchronizationSourceIdentifier >> 24) | 0xFF));
            //result.Add((byte)((SynchronizationSourceIdentifier >> 16) | 0xFF));
            //result.Add((byte)((SynchronizationSourceIdentifier >> 8) | 0xFF));
            //result.Add((byte)((SynchronizationSourceIdentifier) | 0xFF));

            result.AddRange(BitConverter.GetBytes(Utility.SwapUnsignedInt(ssrc ?? SynchronizationSourceIdentifier)));

            //FractionsLost
            result.Add((byte)FractionLost);
            
            // cumulative packets lost
            result.Add((byte)((CumulativePacketsLost >> 24) | 0xFF));
            result.Add((byte)((CumulativePacketsLost >> 16) | 0xFF));
            result.Add((byte)((CumulativePacketsLost >> 8 | 0xFF)));
            
            // extended highest sequence number
            //result.Add((byte)((ExtendedHigestSequenceNumber >> 24) | 0xFF));
            //result.Add((byte)((ExtendedHigestSequenceNumber >> 16) | 0xFF));
            //result.Add((byte)((ExtendedHigestSequenceNumber >> 8) | 0xFF));
            //result.Add((byte)((ExtendedHigestSequenceNumber) | 0xFF));
            result.AddRange(BitConverter.GetBytes(Utility.SwapUnsignedInt(ExtendedHigestSequenceNumber)));
            
            // jitter
            //result.Add((byte)((InterArrivalJitter >> 24) | 0xFF));
            //result.Add((byte)((InterArrivalJitter >> 16) | 0xFF));
            //result.Add((byte)((InterArrivalJitter >> 8) | 0xFF));
            //result.Add((byte)((InterArrivalJitter) | 0xFF));
            result.AddRange(BitConverter.GetBytes(Utility.SwapUnsignedInt(InterArrivalJitter)));

            // last SR
            //result.Add((byte)((LastSendersReport >> 24) | 0xFF));
            //result.Add((byte)((LastSendersReport >> 16) | 0xFF));
            //result.Add((byte)((LastSendersReport >> 8) | 0xFF));
            //result.Add((byte)((LastSendersReport) | 0xFF));

            result.AddRange(BitConverter.GetBytes(Utility.SwapUnsignedInt(LastSendersReport)));

            // delay since last SR
            //result.Add((byte)((DelaySinceLastSendersReport >> 24) | 0xFF));
            //result.Add((byte)((DelaySinceLastSendersReport >> 16) | 0xFF));
            //result.Add((byte)((DelaySinceLastSendersReport >> 8) | 0xFF));
            //result.Add((byte)((DelaySinceLastSendersReport) | 0xFF));

            result.AddRange(BitConverter.GetBytes(Utility.SwapUnsignedInt(DelaySinceLastSendersReport)));

            return result.ToArray();
        }

    }
}
