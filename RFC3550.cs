#region Copyright
/*
Copyright (c) 2013 juliusfriedman@gmail.com
  
 SR. Software Engineer ASTI Transportation Inc.

Permission is hereby granted, free of charge, 
 * to any person obtaining a copy of this software and associated documentation files (the "Software"), 
 * to deal in the Software without restriction, 
 * including without limitation the rights to :
 * use, 
 * copy, 
 * modify, 
 * merge, 
 * publish, 
 * distribute, 
 * sublicense, 
 * and/or sell copies of the Software, 
 * and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * 
 * 
 * JuliusFriedman@gmail.com should be contacted for further details.

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. 
 * 
 * IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, 
 * DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, 
 * TORT OR OTHERWISE, 
 * ARISING FROM, 
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 * 
 * v//
 */
#endregion

#region Using Statements

using Media.Rtcp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Octet = System.Byte;
using OctetSegment = System.ArraySegment<byte>;

#endregion

namespace Media
{
    /// <summary>
    /// Provides an implementation of various abstractions presented in <see cref="http://tools.ietf.org/html/rfc3550">RFC3550</see>
    /// </summary>
    public sealed class RFC3550
    {
        #region Constants and Statics

        public const int RtcpValidMask = 0xc000 | 0x2000 | 0xfe;

        /// <summary>
        /// Calculates a value which can be used in conjunction with the RtcpValidMask to validate a RtcpHeader.
        /// <see cref="http://tools.ietf.org/html/rfc3550#appendix-A">Appendix A</see>
        /// </summary>
        /// <param name="version">The version of the RtcpPacket to validate</param>
        /// <param name="payloadType">The optional payloadType to use in the calulcation. Defaults to 201</param>
        /// <returns>The value calulcated</returns>
        public static int RtcpValidValue(int version, int payloadType = Rtcp.SendersReport.PayloadType) { return (version << 14 | payloadType); }

        public static bool IsValidRtcpHeader(Rtcp.RtcpHeader header, int version = 2)
        {
            return (header.ToInt32() & RtcpValidMask) == RtcpValidValue(version);
        }

        /// <summary>
        /// Creates a single RtcpPacket instance which is comprised of the given packets.
        /// Padding will be added to the last packet if required to pad until the next 32 bit boundary.
        /// Throws an ArgumentNullException if <paramref name="packets"/> is null.
        /// Throws an InvalidOperationException if <paramref name="packets"/> contains less then 2 RtcpPacket instances or the first RtcpPacket contains padding.
        /// Throws an InvalidOperationException if <paramref name="packets"/> has an <see cref="RtcpPacket.PayloadType">RtcpPacket with the PayloadType</see> NOT set to either SendersReport (200) or ReceiversReport (201) as the first element in the seqeuence 
        /// </summary>
        /// <param name="packets">The packets to compound</param>
        /// <returns>The sequence of bytes which represent the compound RtcpPacket.</returns>
        public static IEnumerable<byte> ToCompoundBytes(params RtcpPacket[] packets)
        {
            if (packets == null) throw new ArgumentNullException("packets");

            if (packets.Length < 2) throw new InvalidOperationException("packets must contain at least 2 RtcpPackets");

            RtcpPacket first = packets.First();

            if (first.Padding) throw new InvalidOperationException("Only the last packet in a compound RtcpPacket may have padding");

            int firstPayloadType = first.PayloadType, ssrc = first.SynchronizationSourceIdentifier, totalLength = first.Length;

            if (firstPayloadType != Rtcp.SendersReport.PayloadType && firstPayloadType != Rtcp.ReceiversReport.PayloadType) throw new InvalidOperationException("A Compound packet must start with either a SendersReport or a ReceiversReport.");

            //Each Compound RtcpPacket must have a SourceDescription with a CName and may have a goodbye
            bool hasSourceDescription = false, hasCName = false;

            //Iterate the remaining packets except the last packet
            foreach (RtcpPacket packet in packets.Skip(1))
            {
                //Summize the length
                totalLength += packet.Length;

                //New ssrc resets the required packets in the sequence
                if (packet.SynchronizationSourceIdentifier != ssrc) hasSourceDescription = hasCName = false;

                //if the packet is a SourceDescriptionReport ensure a CName is present.
                if (!hasCName && packet.PayloadType == SourceDescriptionReport.PayloadType)
                {
                    //The packets contained a SourceDescription Report.
                    hasSourceDescription = true;

                    //Check for the CName Item in all chunks
                    using (SourceDescriptionReport asReport = new SourceDescriptionReport(packet, false))
                    {
                        foreach (SourceDescriptionChunk chunk in asReport) if ((hasCName = chunk.Any(i => i.ItemType == SourceDescriptionItem.SourceDescriptionItemType.CName))) break;
                    }
                }                
            }

            //Verify the SourceDesscription has a CName Item if present
            if (hasSourceDescription && !hasCName) throw new InvalidOperationException("A Compound RtcpPacket must have a SourceDescriptionReport with a SourceDescriptionChunk with a CName item.");

            //Determine if padding is required
            int paddingAmount = totalLength % 32;

            //Add the padding to the last packet
            if(paddingAmount > 0)
            {
                //Get the last packet
                RtcpPacket last = packets.Last();

                /*
                 * 
                 * 
                 It is possible this logic could be it's own function, SetPadding(...)
                 
                 Padding is only performed here on the RtpClient for now.
                 
                 * 
                 * 
                 */

                //If the last packet was already padded
                if (last.Padding)
                {
                    //Obtain the amount of octets in the packet pertaining to padding
                    int existingPadding = last.PaddingOctets;

                    //If there is existingPadding or this amount would result in padding which is greater then can be stored in an 8 bit value truncate the remaining padding.
                    if (existingPadding > 0 || existingPadding + paddingAmount > byte.MaxValue) paddingAmount -= existingPadding;

                    //If there is any padding to add
                    if (paddingAmount > 0)
                    {

                        //Add the required padding
                        last.AddBytesToPayload(CreatePadding(paddingAmount), 0, paddingAmount);

                        //Summinze the previous padding
                        last.Payload.Array[last.Payload.Offset + last.Payload.Count] += (byte)(existingPadding);
                    }
                }
                else
                {
                    //Add the padding required
                    last.AddBytesToPayload(CreatePadding(paddingAmount), 0, paddingAmount);

                    //Ensure padding is set in the header now to reflect the added padding
                    last.Padding = true;
                }
            }

            //Return the projection of the sequence containing the compound data
             return packets.SelectMany(p => p.Prepare());
        }

        /// <summary>
        /// Provides the functionality of <see cref="ToCompoundBytes"/> with the added ability or preceeding the data with a 32 bit value.
        /// If <paramref name="random"/> is greater than 0 the big endian representation of the value will be included in the returned sequence.
        /// </summary>
        /// <param name="random">The optional 32 bit value which should be preceed the <see cref="RtcpHeader"/> of the Compund RtcpPacket created.</param>
        /// <param name="padding">A value indicating if padding should be used in the compound packet as a whole if required.</param>
        /// <param name="packets">The packets to compound</param>
        /// <returns>The sequence of bytes which represent the compound RtcpPacket preceeded with the given 32 bit value <paramref name="random"/>.</returns>
        public static IEnumerable<byte> ToEncryptedCompoundBytes(int random, System.Security.Cryptography.HashAlgorithm algorithm = null, params RtcpPacket[] packets)
        {
            throw new NotImplementedException("http://tools.ietf.org/html/rfc3550#section-9.1");
            //algorithm = algorithm ?? System.Security.Cryptography.DES.Create();
            //return Enumerable.Concat(randomBytes, ToCompoundBytes(packets));
        }

        /// <summary>
        /// Gets alll packets and verfies there is either a SendersReport or ReceiversReport as the first packet and only the last packet contains padding.
        /// </summary>
        /// <param name="array"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <param name="skipUnknownTypes"></param>
        /// <returns></returns>
        public static IEnumerable<RtcpPacket> FromCompoundBytes(byte[] array, int offset, int count, bool skipUnknownTypes = false, int version = 2, int? ssrc = null)
        {
            //Keep track of how many packets were parsed.
            int parsedPackets = 0;

            //Get all packets contained in the buffer.
            foreach (RtcpPacket currentPacket in RtcpPacket.GetPackets(array, offset, count, version, null, ssrc)) 
            {

                //Determine who the packet is from and who type it is.
                int firstPayloadType = currentPacket.PayloadType;

                //Check the packet to be valid to RFC3550 rules for compound packets.
                
                //An Empty SR / RR Will have a block count of 0 and not be valid....

                //If the next packet should be a Goodbye

                if (parsedPackets == 0 && currentPacket.BlockCount > 0 && !IsValidRtcpHeader(currentPacket.Header, currentPacket.Version)) yield break;

                yield return currentPacket;

                ++parsedPackets;

                //Each Compound RtcpPacket must have a SourceDescription with a CName and may have a goodbye
                bool hasSourceDescription = false, hasCName = false;

                if (currentPacket.SynchronizationSourceIdentifier != ssrc)
                {
                    hasSourceDescription = hasCName = false;
                }

                //if the packet is a SourceDescriptionReport ensure a CName is present.
                if (currentPacket.PayloadType == SourceDescriptionReport.PayloadType && !hasCName)
                {
                    //The packets contained a SourceDescription Report.
                    hasSourceDescription = true;

                    //Check for the CName Item in all chunks
                    SourceDescriptionReport asReport = new SourceDescriptionReport(currentPacket);
                    foreach (SourceDescriptionChunk chunk in asReport)
                    {
                        hasCName = chunk.Any(i => i.ItemType == SourceDescriptionItem.SourceDescriptionItemType.CName);
                        if (!hasCName) throw new InvalidOperationException("A Compound RtcpPacket must have a SourceDescriptionReport with a SourceDescriptionChunk with a CName item.");
                    }
                }

                //If only packets which have been implmented should be returned
                if (skipUnknownTypes && RtcpPacket.GetImplementationForPayloadType((byte)currentPacket.PayloadType) != null) continue;

                yield return currentPacket;
            }

            yield break;
        }

        /// <summary>
        /// Reads the first 32 bit value present in the Compound Data and parses all contained packets.
        /// </summary>
        /// <param name="random">The 32 bit value read from the array</param>
        /// <param name="array">The array to parse for RtcpPackets</param>
        /// <param name="offset">The offset to being parsing</param>
        /// <param name="count">The amount of to parse within <paramref name="array"/>.</param>
        /// <param name="skipUnknownTypes">A value indicating if only implemented PayloadTypes should be returned.</param>
        /// <returns>The packets parsed based on the given parameters.</returns>
        public static IEnumerable<RtcpPacket> FromEncryptedCompoundBytes(out int random, byte[] array, int offset, int count, bool skipUnknownTypes = false)
        {
            random = (int)Media.Common.Binary.ReadU32(array, offset, BitConverter.IsLittleEndian);
            return FromCompoundBytes(array, offset += 4, count -= 4, skipUnknownTypes);
        }

        /// <summary>
        /// Reads the bytes designated as padding in the given segment.
        /// Note that the amount of octets in the padding cannot be determined until all previous data required by the packets header has been received.
        /// </summary>
        /// <param name="segment">The memory which contains binary data</param>
        /// <param name="position">The position in the segment to start looking for padding</param>
        /// <returns>The value of the last non 0 octet in the given segment deleniated by position</returns>
        public static int ReadPadding(ArraySegment<byte> segment, int position)
        {

            //If there are no more bytes to parse we cannot continue
            if (segment.Count == 0) return 0;

            /*
              If the padding bit is set, the packet contains one or more
              additional padding octets at the end which are not part of the
              payload.  The last octet of the padding contains a count of how
              many padding octets should be ignored, including itself.  Padding
              may be needed by some encryption algorithms with fixed block sizes
              or for carrying several RTP packets in a lower-layer protocol data unit.
          */

            //Iterate forwards looking for padding ending at the count of bytes in the segment of memory given
            for (int e = segment.Count; position < e; ++position)
            {
                //If the value is non 0 this is supposed to be the amount of padding contained in the packet (in octets)
                if (segment.Array[position] != 0)
                {
                    //The last octet is not part of the payload but should indicate the number of bytes in the padding.
                    break;
                }
            }


            //Return the amount of bytes in the padding.
            return segment.Array[position];
        }

        /// <summary>
        /// Reads the bytes designated as padding in the given segment.
        /// Note that the amount of octets in the padding cannot be determined until all previous data required by the packets header has been received.
        /// </summary>
        /// <param name="data">The sequence of octets which contains binary data</param>
        /// <param name="position">The position in the data to skip while looking for padding</param>
        /// <returns>The value of the octet at the position given</returns>
        public static int ReadPadding(IEnumerable<byte> data, int position = 0)
        {
            return data.Skip(position).First();
        }

        /// <summary>
        /// Generates a sequence of null octets (0x00) terminated with the given value
        /// Throws an OverflowException if amount is greater than <see cref="Byte.MaxValue"/>
        /// </summary>
        /// <param name="amount">The amount of padding to create</param>
        /// <returns>The seqeuence containing indicated padding</returns>
        public static IEnumerable<byte> CreatePadding(int amount)
        {
            if (amount <= 0) return Enumerable.Empty<byte>();
            if (amount > byte.MaxValue) Common.Binary.CreateOverflowException("amount", amount, byte.MinValue.ToString(), byte.MinValue.ToString());
            return Enumerable.Concat(Enumerable.Repeat(default(byte), amount - 1), ((byte)amount).Yield());
        }

        //Random32 etc

        #endregion
    }
}
