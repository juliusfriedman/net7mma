namespace Media.Rtp
{
    //Todo, determine if this belongs in another assembly once the ProfileInformation aspect is complete.

    /// <summary>
    /// Provides support for depacketization and packetization of RFC2198 format packets.
    /// </summary>
    public static class RFC2198 // RFC6354
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

            bool marker = packet.Marker;

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
                Rtp.RtpHeader header = new RtpHeader(packet.Version, false, false, marker, tempPayloadType, 0, packet.SynchronizationSourceIdentifier, 
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
            tempHeader = new RtpHeader(packet.Version, false, false, marker, tempPayloadType, 0, packet.SynchronizationSourceIdentifier,
                packet.SequenceNumber,
                packet.Timestamp,
                shouldDispose);

            //Create the packet
            tempResult = new RtpPacket(tempHeader, tempPayload, shouldDispose);

            //Return the packet
            yield return tempResult;
        }

        //Should return an Enumerable<RtpPacket> and should ask for bytesPerPayload.
        /// <summary>
        /// Given many packets a single packet is made in accordance with RFC2198
        /// </summary>
        /// <param name="packets"></param>
        /// <returns></returns>
        public static RtpPacket Packetize(RtpPacket[] packets) //todo needs a timestamp of the packet created...
        {
            //Make one packet with the data of all packets
            //Must also include headers which is 4 * packets.Length + 1

            int packetsLength;

            if (Common.Extensions.Array.ArrayExtensions.IsNullOrEmpty(packets, out packetsLength)) return null;

            //4 byte headers are only needed if there are more than 1 packet.
            int headersNeeded = packetsLength - 1;

            //Calulcate the size of the single packet we will need.
            int size = RtpHeader.Length + ((4 * headersNeeded) + 1);

            //Create the packet with the known length
            RtpPacket result = new RtpPacket(new byte[size], 0); //RtpHeader.Length + 4 * packetsLength - (packetsLength * RtpHeader.Length) + 1

            int bitOffset = 0;

            int packetIndex = 0;

            bool marker = false;

            RtpPacket packet;

            int payloadDataOffset = (4 * headersNeeded) + 1;

            int blockLen;

            //For all but the last packet write a full header
            for (int e = headersNeeded; packetIndex < e; ++packetIndex)
            {
                packet = packets[packetIndex];

                //csrc Extensions and padding included...
                blockLen = packet.Payload.Count;

                //Write the payloadType
                Common.Binary.WriteBitsMSB(result.Payload.Array, ref bitOffset, (ulong)packet.PayloadType, 7);

                //Should be offset from timestamp
                Common.Binary.WriteBitsMSB(result.Payload.Array, ref bitOffset, (ulong)packet.Timestamp, 14);

                //Write the BlockLength
                Common.Binary.WriteBitsMSB(result.Payload.Array, ref bitOffset, (ulong)blockLen, 10);

                //Copy the data
                System.Array.Copy(packet.Payload.Array, packet.Payload.Offset, result.Payload.Array, payloadDataOffset, blockLen);

                //Move the payloadDataOffset for the block of data just copied.
                payloadDataOffset += blockLen;

                //If the marker was not already found check for it
                if (false == marker && packet.Marker) marker = true;
            }

            //Write the last header (1 byte with (F)irst bit set and PayloadType
            
            //Get the packet
            packet = packets[packetIndex];

            //Could just write 0x80 | PayloadType at payloadStart - 1
            //result.Payload.Array[payloadStart - 1] = (byte)(0x80 | packet.PayloadType);

            //Set the (F)irst bit
            Common.Binary.WriteBitsMSB(result.Payload.Array, ref bitOffset, (ulong)1, 1);

            //Write the payloadType
            Common.Binary.WriteBitsMSB(result.Payload.Array, ref bitOffset, (ulong)packets[packetIndex].PayloadType, 7);

            //Copy the data
            System.Array.Copy(packet.Payload.Array, packet.Payload.Offset, result.Payload.Array, payloadDataOffset, packet.Payload.Count);

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
    /// Provides an implementation of the packetization and depacketization described in <see href="https://tools.ietf.org/html/rfc2198">RFC2198</see>
    /// </summary>
    public class RedundantRtpFrame : RtpFrame
    {
        //Would need a place to put the data for each payloadType
        //This implies the Depacketized needs a KeyType as the Key so that it can reflect multiple payload types..
        //This could also be handled by using methods which can take a RtpHeader and the Depacketized data and creating the packets out of the Depacketized data when needed.

        //This would be used to store packets for the individual packets in the stream.
        //JitterBuffer j = new JitterBuffer(true);

        public override void Depacketize(RtpPacket packet)
        {
            //Get all subordinate packets in the packet
            foreach (RtpPacket subordinate in RFC2198.GetPackets(packet, false))
            {
                //Todo, needs a way to construct a RtpFrame via the codec / PayloadType
                //RtpFrame complete;

                ////If the frame is complete then...
                //if (j.Add(subordinate.PayloadType, subordinate, out complete))
                //{
                //    //It needs to be handled by it's own frame types logic.
                //}

                //For now just call Depacketize with the subordinate
                    //Whoever reads the data in those depacketized packets would use the GetPackets( overload ) which keeps this efficent.
                base.Depacketize(subordinate);
            }
        }

    }

    //Could also do something like this where the Packetize and Depacketize are left to the IRtpFrame but the profile information still contains the other relevant data.
    public interface IRtpProfileInformation
    {
        //int CalulcateProfileHeaderSize();

        bool HasVariableProfileHeaderSize { get; }

        int MinimumHeaderSize { get; }

        int MaximumHeaderSize { get; }
    }

    //Testing..
    public static class IRtpProfileInformationExtensions
    {
        public static bool IsValidPacket(this IRtpProfileInformation profileInfomation, RtpPacket packet)
        {
            if (Common.IDisposedExtensions.IsNullOrDisposed(packet)) return false;

            Common.MemorySegment packetData = packet.PayloadDataSegment;

            return packetData.Count > profileInfomation.MinimumHeaderSize && packetData.Count > profileInfomation.MaximumHeaderSize;
        }
    }

    //Not really useful except for the methods which could be static.(As shown above)
    public class RtpProfileInformationBase : IRtpProfileInformation
    {
        public virtual bool IsValidPacket(RtpPacket packet)
        {
            return IRtpProfileInformationExtensions.IsValidPacket(this, packet);
        }

        public virtual bool HasVariableProfileHeaderSize
        {
            get;
            protected set;
        }

        public virtual int MinimumHeaderSize
        {
            get;
            protected set;
        }

        public virtual int MaximumHeaderSize
        {
            get;
            protected set;
        }
    }

    /// <summary>
    /// Each instance would only need to define a class or specify a class which adhered to the profile
    /// </summary>
    public sealed class ProfileHeaderInformation : IRtpProfileInformation //doesn't have to be sealed.
    {
        internal const bool HasVariableProfileHeaderSize = true;

        public static int MinimumHeaderSize = 1;

        public static int MaximumHeaderSize = 4;

        bool IRtpProfileInformation.HasVariableProfileHeaderSize
        {
            get { return HasVariableProfileHeaderSize; }
        }

        int IRtpProfileInformation.MinimumHeaderSize
        {
            get { return MinimumHeaderSize; }
        }

        int IRtpProfileInformation.MaximumHeaderSize
        {
            get { return MaximumHeaderSize; }
        }
    }

    //Could also have a derived type which was specifically overriding the values...
    public class RFC2198ProfileHeaderInformation : RtpProfileInformationBase, IRtpProfileInformation
    {
        //Could also choose to define constants and use specific implementation as well

        public RFC2198ProfileHeaderInformation()
        {
            //Can override them here.
            HasVariableProfileHeaderSize = ProfileHeaderInformation.HasVariableProfileHeaderSize;

            MinimumHeaderSize = ProfileHeaderInformation.MinimumHeaderSize;

            MaximumHeaderSize = ProfileHeaderInformation.MaximumHeaderSize;
        }

        //Don't really need to override this, especially since the interface calls may turn out different especially considering how they are implemented...
        public override bool IsValidPacket(RtpPacket packet)
        {
            return base.IsValidPacket(packet);
        }

        //Don't really need to override these at this level...

        public override bool HasVariableProfileHeaderSize
        {
            get { return base.HasVariableProfileHeaderSize; }
            protected set { base.HasVariableProfileHeaderSize = value; }
        }

        public override int MinimumHeaderSize
        {
            get { return base.MinimumHeaderSize; }
            protected set { base.MinimumHeaderSize = value; }
        }

        public override int MaximumHeaderSize
        {
            get { return base.MaximumHeaderSize; }
            protected set { base.MaximumHeaderSize = value; }
        }

        // Explicit overrides for the interface...

        bool IRtpProfileInformation.HasVariableProfileHeaderSize
        {
            get { return ProfileHeaderInformation.HasVariableProfileHeaderSize; }
        }

        int IRtpProfileInformation.MinimumHeaderSize
        {
            get { return ProfileHeaderInformation.MinimumHeaderSize; }
        }

        int IRtpProfileInformation.MaximumHeaderSize
        {
            get { return ProfileHeaderInformation.MaximumHeaderSize; }
        }
    }
}
