namespace Media.Rtp
{
    /// <summary>
    /// Provides support for depacketization and packetization of RFC2198 format packets.
    /// </summary>
    public static class RFC2198
    {
        /// <summary>
        /// Given a packet using the redundant audio format, the expanded rtp packets are derived from the contents.
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="shouldDispose"></param>
        /// <returns></returns>
        public static System.Collections.Generic.IEnumerable<RtpPacket> GetPackets(RtpPacket packet, bool shouldDispose = true)
        {

            if (Common.IDisposedExtensions.IsNullOrDisposed(packet)) yield break;

            int headerOctets = packet.HeaderOctets, 
                offset = packet.Payload.Offset + headerOctets, startOffset = offset,
                remaining = packet.Payload.Count - (headerOctets + packet.PaddingOctets), 
                endHeaders = remaining, headersContained = 0;

            //If there are not enough bytes for the profile header break.
            if (remaining < Common.Binary.BytesPerInteger) yield break;

            byte toCheck;

            //Iterare from the offset of the end of the rtp header until the end of data in the payload
            while (remaining >= Common.Binary.BytesPerInteger)
            {
                // 0                   1                    2                   3
                // 0 1 2 3 4 5 6 7 8 9 0 1 2 3  4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
                //+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                //|F|   block PT  |  timestamp offset         |   block length    |
                //+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+

                toCheck = packet.Payload.Array[offset];

                //Check for (F)irst bit
                if ((toCheck & 0x80) != 0)
                {
                    //Store the offset of the last header.
                    endHeaders = offset;

                    //This byte does not belong to the data.
                    --remaining;

                    break;
                }

                //Increment for the header contained.
                ++headersContained;

                //Decrement for the header read.
                remaining -= Common.Binary.BytesPerInteger;

                //Move by 4 bytes beause the size of the header is 4 bytes.
                offset += Common.Binary.BytesPerInteger;
            }

            //Nothing more to return
            if (remaining < 0) yield break;

            Common.MemorySegment tempPayload;

            Rtp.RtpHeader tempHeader;

            RtpPacket tempResult;

            int tempPayloadType, tempTimestamp, tempBlockLen;

            //Start at the offset in bits of the end of header.
            if(headersContained > 0) for (int headOffset = startOffset, i = 0, bitOffset = Common.Binary.BytesToBits(ref headOffset); i < headersContained; ++i)
            {
                //Read the payloadType out of the header
                tempPayloadType = (int)Common.Binary.ReadBitsMSB(packet.Payload.Array, Common.Binary.BytesToBits(ref headOffset) + 1, 7);

                //Read the timestamp offset  from the header
                tempTimestamp = (int)Common.Binary.ReadBitsMSB(packet.Payload.Array, ref bitOffset, 10);

                //Read the blockLength from the header
                tempBlockLen = (int)Common.Binary.ReadBitsMSB(packet.Payload.Array, ref bitOffset, 14);

                //If there are less bytes in the payload than remain in the block stop 
                //if (remaining < tempBlockLen) break;

                //Get the payload
                Common.MemorySegment payload = new Common.MemorySegment(packet.Payload.Array, Common.Binary.BitsToBytes(ref bitOffset), tempBlockLen, shouldDispose);

                //Create the header
                Rtp.RtpHeader header = new RtpHeader(packet.Version, false, false, packet.Marker, tempPayloadType, 0, packet.SynchronizationSourceIdentifier, 
                    packet.SequenceNumber, 
                    packet.Timestamp + tempTimestamp, 
                    shouldDispose);

                //Create the packet
                RtpPacket result = new RtpPacket(header, payload, shouldDispose);

                //Return the packet
                yield return result;

                //Move the offset
                bitOffset += Common.Binary.BytesToBits(ref tempBlockLen);
                
                //Remove the blockLength from the count
                remaining -= tempBlockLen;
            }

            //If there is anymore data it's values are defined in the header of the given packet.

            //Read the payloadType out of the headers area (1 bit after the end of headers) 7 bits in size
            tempPayloadType = (int)Common.Binary.ReadBitsMSB(packet.Payload.Array, Common.Binary.BytesToBits(ref startOffset) + 1, 7);

            //Get the payload of the temp packet, the blockLen is given by the count in this packet minus the 
            tempPayload = new Common.MemorySegment(packet.Payload.Array, endHeaders, remaining, shouldDispose);

            //Create the header
            tempHeader = new RtpHeader(packet.Version, false, false, packet.Marker, tempPayloadType, 0, packet.SynchronizationSourceIdentifier,
                packet.SequenceNumber,
                packet.Timestamp,
                shouldDispose);

            //Create the packet
            tempResult = new RtpPacket(tempHeader, tempPayload, shouldDispose);

            //Return the packet
            yield return tempResult;
        }

        /// <summary>
        /// Given many packets a single packet is made in accordance with RFC2198
        /// </summary>
        /// <param name="packets"></param>
        /// <returns></returns>
        public static RtpPacket Packetize(RtpPacket[] packets)
        {
            if (Common.Extensions.Array.ArrayExtensions.IsNullOrEmpty(packets)) return null;

            //Make one packet with the data of all packets
            //Must also include headers which is 4 * packets.Length + 1

            int packetsLength = packets.Length;

            //Create the packet with the known length
            RtpPacket result = new RtpPacket(new byte[RtpHeader.Length + 4 * packetsLength - (packetsLength * RtpHeader.Length) + 1], 0);

            int offset = 0;

            int packetIndex = 0;

            bool marker = false;

            RtpPacket packet;

            //For all but the last packet write a full header
            for (int e = packetsLength - 1; packetIndex < e; ++packetIndex)
            {
                packet = packets[packetIndex];

                Common.Binary.WriteBitsMSB(result.Payload.Array, ref offset, (ulong)packet.PayloadType, 7);

                Common.Binary.WriteBitsMSB(result.Payload.Array, ref offset, (ulong)packet.Timestamp, 14);

                Common.Binary.WriteBitsMSB(result.Payload.Array, ref offset, (ulong)packet.PayloadDataSegment.Count, 10);

                //If the marker was not already found check for it
                if (false == marker && packet.Marker) marker = true;
            }

            //Write the last header
            packet = packets[packetIndex];

            //Set the (F)irst bit
            Common.Binary.WriteBitsMSB(result.Payload.Array, ref offset, (ulong)1, 1);

            //Write the payloadType
            Common.Binary.WriteBitsMSB(result.Payload.Array, ref offset, (ulong)packets[packetIndex].PayloadType, 7);

            //Set the Timestamp
            result.Timestamp = packet.Timestamp;

            //Set the SequenceNumber
            result.Timestamp = packet.SequenceNumber;

            //Set the marker bit if it needs to be set.
            result.Marker = marker || packet.Marker;

            //Return the single packet created.
            return result;

        }
    }

    /// <summary>
    /// Todo, should hanlde the RtpFrame API needs for the case of RFC2198 by using the logic above..
    /// </summary>
    public class RedundantRtpFrame : RtpFrame
    {

    }
}
