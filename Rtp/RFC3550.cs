#region Copyright
/*
This file came from Managed Media Aggregation, You can always find the latest version @ https://net7mma.codeplex.com/
  
 Julius.Friedman@gmail.com / (SR. Software Engineer ASTI Transportation Inc. http://www.asti-trans.com)

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

using Media.Common;
using Media.Rtcp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#endregion

namespace Media
{
    /// <summary>
    /// Provides an implementation of various abstractions presented in <see cref="http://tools.ietf.org/html/rfc3550">RFC3550</see>
    /// </summary>
    public sealed class RFC3550
    {
        #region Constants and Statics

        /// <summary>
        /// Creates a random 32 bit integer as specified in RFC3550
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static int Random32(int type = 0)
        {
            #region Reference
            /*
               gettimeofday(&s.tv, 0);
               uname(&s.name);
               s.type = type;
               s.cpu  = clock();
               s.pid  = getpid();
               s.hid  = gethostid();
               s.uid  = getuid();
               s.gid  = getgid();
             */

            //UtsName equivelant information would be

            //char  sysname[]  name of this implementation of the operating system
            //char  nodename[] name of this node within an implementation-dependent                 communications network
            //char  release[]  current release level of this implementation
            //char  version[]  current version level of this release
            //char  machine[]  name of the hardware type on which the system is running

            #endregion            

            byte[] structure = Binary.GetBytes(type).//int     type;
                Concat(Binary.GetBytes(DateTime.UtcNow.Ticks)).Concat(Binary.GetBytes(Environment.TickCount)).//struct  timeval tv;
                Concat(Binary.GetBytes(TimeSpan.TicksPerMillisecond)).//clock_t cpu;
                Concat(Binary.GetBytes(System.Threading.Thread.GetDomainID() ^ System.Threading.Thread.CurrentThread.ManagedThreadId)).//pid_t   pid;
                Concat(System.Text.Encoding.Default.GetBytes(Environment.MachineName)).//u_long  hid;
                Concat(System.Text.Encoding.Default.GetBytes((Environment.UserName))).//uid_t   uid;
                Concat(Guid.NewGuid().ToByteArray()).//gid_t   gid;
                Concat(System.Text.Encoding.Default.GetBytes(Environment.OSVersion.VersionString)).ToArray();//struct  utsname name;

            //Perform MD5 on structure per 3550
            byte[] digest;

            digest = Cryptography.MD5.GetHash(structure);

            //Complete hash
            uint r = 0;
            r ^= BitConverter.ToUInt32(digest, 0);
            r ^= BitConverter.ToUInt32(digest, 4);
            r ^= BitConverter.ToUInt32(digest, 8);
            r ^= BitConverter.ToUInt32(digest, 12);
            return (int)r;
        }

        #region Rtcp

        /// <summary>
        /// A binary mask which is used to filter invalid Rtcp packets as specified in RFC3550
        /// </summary>
        public const int RtcpValidMask = 0xc000 | 0x2000 | 0xfe;

        /// <summary>
        /// Calculates a value which can be used in conjunction with the <see cref="RtcpValidMask"/> to validate a RtcpHeader.
        /// <see cref="http://tools.ietf.org/html/rfc3550#appendix-A">Appendix A</see>
        /// </summary>
        /// <param name="version">The version of the RtcpPacket to validate</param>
        /// <param name="payloadType">The optional payloadType to use in the calulcation. Defaults to 201</param>
        /// <returns>The value calulcated</returns>
        public static int RtcpValidValue(int version, int payloadType = Rtcp.SendersReport.PayloadType)
        {
            //Always calulated in Big ByteOrder
            return (version << 14 | payloadType);
        }

        /// <summary>
        /// Validates a RtcpPacket header using the <see cref="RtcpValidMask"/> and compares the result to the <see cref="RtcpValidValue"/> function.
        /// </summary>
        /// <param name="header"></param>
        /// <param name="version"></param>
        /// <returns>True if the values are equal, otherwise false</returns>
        public static bool IsValidRtcpHeader(Rtcp.RtcpHeader header, int version = 2) { return (header.First16Bits & RtcpValidMask) == RtcpValidValue(version); }

        /// <summary>
        /// Creates a single RtcpPacket instance which is comprised of the given packets.
        /// Padding will be added to the last packet if required to pad until the next 32 bit boundary.
        /// Throws an ArgumentNullException if <paramref name="packets"/> is null.
        /// Throws an InvalidOperationException if <paramref name="packets"/> contains less then 2 RtcpPacket instances or the first RtcpPacket contains padding.
        /// Throws an InvalidOperationException if <paramref name="packets"/> has an <see cref="RtcpPacket.PayloadType">RtcpPacket with the PayloadType</see> NOT set to either SendersReport (200) or ReceiversReport (201) as the first element in the seqeuence 
        /// </summary>
        /// <param name="packets">The packets to compound</param>
        /// <returns>The sequence of bytes which represent the compound RtcpPacket.</returns>
        public static IEnumerable<byte> ToCompoundBytes(IEnumerable<RtcpPacket> packets)
        {
            if (packets == null) throw new ArgumentNullException("packets");
            else if (packets.Count() < 2) goto PreparePackets; //Only a single packet can just be prepared.
            
            RtcpPacket first = packets.First();

            if (first.Padding) throw new InvalidOperationException("Only the last packet in a compound RtcpPacket may have padding");

            int firstPayloadType = first.PayloadType, ssrc = first.SynchronizationSourceIdentifier, totalLength = (int)first.Length;

            //When respecting RFC3550 the first packet must be a SendersReport or Receivers report with the version of 2, the version is implicit from the header at this point.
            if (false == IsValidRtcpHeader(first.Header, first.Version)) throw new InvalidOperationException("A Compound packet must start with either a SendersReport or a ReceiversReport.");

            //Each Compound RtcpPacket must have a SourceDescription with a CName and may have a goodbye
            bool hasSourceDescription = false, hasCName = false;

            //Iterate the remaining packets except the last packet
            foreach (RtcpPacket packet in packets.Skip(1))
            {
                //Summize the length
                totalLength += (int)packet.Length;

                //New ssrc resets the required packets in the sequence
                if (packet.SynchronizationSourceIdentifier != ssrc) hasSourceDescription = hasCName = false;

                //if the packet is a SourceDescriptionReport ensure a CName is present.
                if (false == hasCName && packet.PayloadType == SourceDescriptionReport.PayloadType)
                {
                    //The packets contained a SourceDescription Report.
                    hasSourceDescription = true;

                    //if not already checked for a cname check now
                    if (false == hasCName && packet.BlockCount > 0) using (SourceDescriptionReport asReport = new SourceDescriptionReport(packet, false)) if ((hasCName = asReport.HasCName)) break;
                }                
            }

            //Verify the SourceDesscription has a CName Item if present
            if (hasSourceDescription && false == hasCName) throw new InvalidOperationException("A Compound RtcpPacket must have a SourceDescriptionReport with a SourceDescriptionChunk with a CName item.");

            //Determine if padding is required
            int paddingAmount = totalLength & 3; // totalLength % 4;

            //Add the padding to the last packet
            if(paddingAmount > 0)
            {
                //Get the last packet
                RtcpPacket last = packets.Last();

                /*
                 * 
                 * 
                 It is possible this logic could be it's own function, SetPadding(...)
                 * 
                 * 
                 */

                //If the last packet was already padded (but still needs this padding)
                if (last.Padding)
                {
                    //Obtain the amount of octets in the packet pertaining to padding (Maybe 0)
                    int existingPaddingAmount = last.PaddingOctets;

                    //Determine how to handle adding the padding.
                    switch (existingPaddingAmount)
                    {
                        case byte.MaxValue: break; //Can't add anymore padding
                        case byte.MinValue://0
                            {
                                //Add the required padding
                                last.AddBytesToPayload(CreatePadding(paddingAmount), 0, paddingAmount);

                                break;
                            }
                        default: //1 - 254
                            {
                                
                                //Calulcate how much padding would be present in total
                                int totalPadding = existingPaddingAmount + paddingAmount;

                                //if this value exceeds or is equal the maximum
                                if (totalPadding >= byte.MaxValue)
                                {
                                    //Calulcate the new amount of padding needed
                                    paddingAmount = byte.MaxValue - existingPaddingAmount;

                                    //Could omit this step as padding would be assumed to be all 0's
                                    //Set the previous count of padding to 0
                                    last.Payload[last.Payload.Offset + last.Payload.Count - 1] = 0;

                                    //Create and add the required amount of padding (Could add overload for this)
                                    last.AddBytesToPayload(CreatePadding(paddingAmount), 0, paddingAmount);

                                    //Set the value manually to the maximum  (Could add overload for this)
                                    last.Payload[last.Payload.Offset + last.Payload.Count - 1] = byte.MaxValue;
                                }
                                else // < 255
                                {
                                    //Could omit this step as padding would be assumed to be all 0's
                                    //Set the previous count of padding to 0
                                    last.Payload[last.Payload.Offset + last.Payload.Count - 1] = 0;

                                    //Add the required padding.
                                    last.AddBytesToPayload(CreatePadding(paddingAmount), 0, paddingAmount);

                                    //Set the value manually to the totalPadding  (Could add overload for this)
                                    last.Payload[last.Payload.Offset + last.Payload.Count - 1] = (byte)totalPadding;
                                }

                                break;
                            }
                    }

                    //////If there is existingPadding or this amount would result in padding which is greater then can be stored in an 8 bit value truncate the remaining padding.
                    ////if (existingPadding > 0 || existingPadding + paddingAmount > byte.MaxValue) paddingAmount -= existingPadding;

                    //////If there is any padding to add
                    ////if (paddingAmount > 0)
                    ////{
                    ////    //int offset = last.Payload.Count;

                    ////    //Add the required padding
                    ////    last.AddBytesToPayload(CreatePadding(paddingAmount), 0, paddingAmount);

                    ////    //Summize the previous padding (leaves it inplace) but adds the amount to the value at the end of the payload
                    ////    last.Payload.Array[last.Payload.Offset + last.Payload.Count] += (byte)(existingPadding);
                    ////}
                }
                else
                {
                    //Add the padding required
                    last.AddBytesToPayload(CreatePadding(paddingAmount), 0, paddingAmount);

                    //Ensure padding is set in the header now to reflect the added padding
                    last.Padding = true;
                }
            }

            //Could use GetAllocate or InternalToBytes to reduce allocations

        PreparePackets:
            //Return the projection of the sequence containing the compound data
             return packets.SelectMany(p => p.Prepare());
        }

        //Needs an interface defined in Media.Cryptography

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
        public static IEnumerable<RtcpPacket> FromCompoundBytes(byte[] array, int offset, int count, bool skipUnknownTypes = false, int version = 2, int? ssrc = null, bool shouldDispose = true)
        {
            if (version < 2) throw new ArgumentException("There are no Compound Packets in Versions less than 2");

            //Keep track of how many packets were parsed.
            int parsedPackets = 0;

            //Each Compound RtcpPacket must have a SourceDescription with a CName and may have a goodbye
            bool hasSourceDescription = false, hasCName = false;

            //Get all packets contained in the buffer.
            foreach (RtcpPacket currentPacket in RtcpPacket.GetPackets(array, offset, count, version, null, ssrc, shouldDispose)) 
            {

                //Determine who the packet is from and what type it is.
                int firstPayloadType = currentPacket.PayloadType;

                //The first packet in a compound packet needs to be validated
                if (parsedPackets == 0 && !IsValidRtcpHeader(currentPacket.Header, currentPacket.Version)) yield break;
                else if (currentPacket.Version != version || skipUnknownTypes && RtcpPacket.GetImplementationForPayloadType((byte)currentPacket.PayloadType) == null) yield break;
                
                //Count the packets parsed
                ++parsedPackets;

                //if the packet is a SourceDescriptionReport ensure a CName is present.
                if (false == hasCName && currentPacket.PayloadType == SourceDescriptionReport.PayloadType)
                {
                    //The packets contained a SourceDescription Report.
                    hasSourceDescription = true;

                    //if not already checked for a cname check now
                    if (false == hasCName && currentPacket.BlockCount > 0) using (SourceDescriptionReport asReport = new SourceDescriptionReport(currentPacket, false)) if ((hasCName = asReport.HasCName)) break;
                }

                if (hasSourceDescription && false == hasCName) Media.Common.TaggedExceptionExtensions.RaiseTaggedException(currentPacket, "Invalid compound data, Source Description report did not have a CName SourceDescriptionItem.");

                yield return currentPacket;
            }

            yield break;
        }

        /// <summary>
        /// The amount of bytes which appear before all RtcpPacket's in a given buffer when using RFC3550 encryption.
        /// </summary>
        public static int MagicBytesSize = Binary.BytesPerInteger;

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
            return FromCompoundBytes(array, offset += MagicBytesSize, count -= MagicBytesSize, skipUnknownTypes);
        }

        #endregion

        #region Padding

        public static int ReadPadding(byte[] buffer, int offset, int count)
        {
            if (count <= 0 || buffer == null) return 0;

            int dataLength = buffer.Length;

            //If there are no more bytes to parse we cannot continue
            if (dataLength == 0 || offset > dataLength) return 0;

            /*
              If the padding bit is set, the packet contains one or more
              additional padding octets at the end which are not part of the
              payload.  The last octet of the padding contains a count of how
              many padding octets should be ignored, including itself.  Padding
              may be needed by some encryption algorithms with fixed block sizes
              or for carrying several RTP packets in a lower-layer protocol data unit.
          */

            byte val;

            //Iterate forwards looking for padding ending at the count of bytes in the segment of memory given
            while (count-- > 0 && offset < dataLength)
            {
                //get the val and move the position
                val = buffer[offset++];

                //If the value is non 0 this is supposed to be the amount of padding contained in the packet (in octets)
                if (val != default(byte))
                {
                    //The last octet is not part of the payload but should indicate the number of bytes in the padding.
                    return val;
                }
            }

            return 0;
        }

        /// <summary>
        /// Generates a sequence of null octets (0x00) terminated with the given value
        /// Throws an OverflowException if amount is greater than <see cref="Byte.MaxValue"/>
        /// </summary>
        /// <param name="amount">The amount of padding to create</param>
        /// <returns>The seqeuence containing indicated padding</returns>
        public static IEnumerable<byte> CreatePadding(int amount) //int? codeAmount
        {
            if (amount <= 0) return Enumerable.Empty<byte>();
            if (amount > byte.MaxValue) Common.Binary.CreateOverflowException("amount", amount, byte.MinValue.ToString(), byte.MaxValue.ToString());
            return Enumerable.Concat(Enumerable.Repeat(default(byte), amount - 1), Media.Common.Extensions.Linq.LinqExtensions.Yield(((byte)amount)));
        }

        #endregion

        #region Algorithms

        /*
         
         A.8 Estimating the Interarrival Jitter

           The code fragments below implement the algorithm given in Section
           6.4.1 for calculating an estimate of the statistical variance of the
           RTP data interarrival time to be inserted in the interarrival jitter
           field of reception reports.  The inputs are r->ts, the timestamp from
           the incoming packet, and arrival, the current time in the same units.
           Here s points to state for the source; s->transit holds the relative
           transit time for the previous packet, and s->jitter holds the
           estimated jitter.  The jitter field of the reception report is
           measured in timestamp units and expressed as an unsigned integer, but
           the jitter estimate is kept in a floating point.  As each data packet
           arrives, the jitter estimate is updated:

              int transit = arrival - r->ts;
              int d = transit - s->transit;
              s->transit = transit;
              if (d < 0) d = -d;
              s->jitter += (1./16.) * ((double)d - s->jitter);

           When a reception report block (to which rr points) is generated for
           this member, the current jitter estimate is returned:

              rr->jitter = (u_int32) s->jitter;

           Alternatively, the jitter estimate can be kept as an integer, but
           scaled to reduce round-off error.  The calculation is the same except
           for the last line:

              s->jitter += d - ((s->jitter + 8) >> 4);

           In this case, the estimate is sampled for the reception report as:

              rr->jitter = s->jitter >> 4;
         
         */

        /// <summary>
        /// Calulcates the jitter and transit value from the arrivalDifference as per RFC3550 6.4.1 [Page 93]A.8
        /// </summary>
        /// <param name="arrivalDifference"></param>
        /// <param name="existingJitter"></param>
        /// <param name="existingTransit"></param>
        public static void CalulcateJitter(ref TimeSpan arrivalDifference, ref int existingJitter, ref int existingTransit)
        {
            uint j = (uint)existingJitter, t = (uint)existingTransit;

            CalulcateJitter(ref arrivalDifference, ref j, ref t);

            existingJitter = (int)j;

            existingTransit = (int)t;
        }

        [CLSCompliant(false)]
        public static void CalulcateJitter(ref TimeSpan arrivalDifference, ref uint existingJitter, ref uint existingTransit)
        {
            existingJitter += ((existingTransit = (uint)arrivalDifference.TotalMilliseconds) - ((existingTransit + 8) >> 4));
        }

        //Maybe a nested type for the state of the context would be useful.
        //Values required for updating sequence number would be kept in state class and UpdateSequenceNumber on context would call the state would call this function...

        //The point at which rollover occurs on the SequenceNumber
        const uint RTP_SEQ_MOD = (1 << 16); //65536

        public const int DefaultMaxDropout = 500, DefaultMaxMisorder = 100, DefaultMinimumSequentalRtpPackets = 2;

        //Probe can be performed by using non ref overload (Todo)

        //Senders just update the seq number on the context (timestamp and ntp timestamp should also be sampled around the same time)

        // Per-source state information
        //typedef struct {
        //    u_int16 max_seq;        /* highest seq. number seen */
        //    u_int32 cycles;         /* shifted count of seq. number cycles */
        //    u_int32 base_seq;       /* base seq number */
        //    u_int32 bad_seq;        /* last 'bad' seq number + 1 */
        //    u_int32 probation;      /* sequ. packets till source is valid */
        //    u_int32 received;       /* packets received */
        //    u_int32 expected_prior; /* packet expected at last interval */
        //    u_int32 received_prior; /* packet received at last interval */
        //    u_int32 transit;        /* relative trans time for prev pkt */
        //    u_int32 jitter;         /* estimated jitter */
        //   /* ... */       
        //} source;
         

        [CLSCompliant(false)]
        public static bool UpdateSequenceNumber(ref ushort sequenceNumber,
            //From 'source' or TransportContext
            ref uint RtpBaseSeq, ref ushort RtpMaxSeq, ref uint RtpBadSeq, ref uint RtpSeqCycles, ref uint RtpReceivedPrior, ref uint RtpProbation, ref uint RtpPacketsRecieved,
            //Defaults
            int MinimumSequentialValidRtpPackets = DefaultMinimumSequentalRtpPackets, int MaxMisorder = DefaultMaxMisorder, int MaxDropout = DefaultMaxDropout)
        {
            // RFC 3550 A.1.
            ushort udelta = (ushort)(sequenceNumber - RtpMaxSeq);

            /*
            * Source is not valid until MIN_SEQUENTIAL packets with
            * sequential sequence numbers have been received.
            */
            if (RtpProbation > 0)
            {
                /* packet is in sequence */
                if (sequenceNumber == RtpMaxSeq + 1)
                {
                    RtpProbation--;

                    RtpMaxSeq = (ushort)sequenceNumber;

                    //If no more probation is required then reset the coutners and indicate the packet is in state
                    if (RtpProbation == 0)
                    {
                        ResetRtpValidationCounters(ref sequenceNumber, ref RtpBaseSeq, ref RtpMaxSeq, ref RtpBadSeq, ref RtpSeqCycles, ref RtpReceivedPrior, ref RtpPacketsRecieved);
                        return true;
                    }
                }
                //The sequence number is not as expected

                //Reset probation
                RtpProbation = (uint)(MinimumSequentialValidRtpPackets - 1);

                //Reset the sequence number
                RtpMaxSeq = (ushort)sequenceNumber;

                //The packet is not in state
                return false;
            }
            else if (udelta < MaxDropout)
            {
                /* in order, with permissible gap */
                if (sequenceNumber < RtpMaxSeq)
                {
                    /*
                    * Sequence number wrapped - count another 64K cycle.
                    */
                    RtpSeqCycles += RTP_SEQ_MOD;
                }

                //Set the maximum sequence number
                RtpMaxSeq = (ushort)sequenceNumber;
            }
            else if (udelta <= RTP_SEQ_MOD - MaxMisorder)
            {
                /* the sequence number made a very large jump */
                if (sequenceNumber == RtpBadSeq)
                {
                    /*
                     * Two sequential packets -- assume that the other side
                     * restarted without telling us so just re-sync
                     * (i.e., pretend this was the first packet).
                    */
                    ResetRtpValidationCounters(ref sequenceNumber, ref RtpBaseSeq, ref RtpMaxSeq, ref RtpBadSeq, ref RtpSeqCycles, ref RtpReceivedPrior, ref RtpPacketsRecieved);
                }
                else
                {
                    //Set the bad sequence to the packets sequence + 1 masking off the bits which correspond to the bits of the sequenceNumber which may have wrapped since SequenceNumber is 16 bits.
                    RtpBadSeq = (uint)((sequenceNumber + 1) & (RTP_SEQ_MOD - 1));
                    return false;
                }
            }
            else
            {
                /* duplicate or reordered packet */
                return false;
            }

            //The RtpPacket is in state
            return true;
        }        

        [CLSCompliant(false)]
        public static void ResetRtpValidationCounters(ref ushort sequenceNumber, ref uint RtpBaseSeq, ref ushort RtpMaxSeq, ref uint RtpBadSeq, ref uint RtpSeqCycles, ref uint RtpReceivedPrior, ref uint RtpPacketsRecieved)
        {
            RtpBaseSeq = RtpMaxSeq = (ushort)sequenceNumber;
            RtpBadSeq = RTP_SEQ_MOD + 1;   /* so seq == bad_seq is false */
            RtpSeqCycles = RtpReceivedPrior = RtpPacketsRecieved = 0;
        }

        //CalculateFractionAndLoss (RTCP?)

        [CLSCompliant(false)]
        public static void CalculateFractionAndLoss(ref uint RtpBaseSeq, ref ushort RtpMaxSeq, ref uint RtpSeqCycles, ref uint RtpPacketsRecieved, ref uint RtpReceivedPrior, ref uint RtpExpectedPrior, out uint fraction, out uint lost)
        {
            //Should be performed in the Conference level, these values here will only 
            //should allow a backoff to occur in reporting and possibly eventually to be turned off.
            fraction = 0;

            uint extended_max = (uint)(RtpSeqCycles + RtpMaxSeq);

            int expected = (int)(extended_max - RtpBaseSeq + 1);

            lost = (uint)(expected - RtpPacketsRecieved);

            int expected_interval = (int)(expected - RtpExpectedPrior);

            RtpExpectedPrior = (uint)expected;

            int received_interval = (int)(RtpPacketsRecieved - RtpReceivedPrior);

            RtpReceivedPrior = (uint)RtpPacketsRecieved;

            int lost_interval = expected_interval - received_interval;

            if (expected_interval == 0 || lost_interval <= 0)
            {
                fraction = 0;
            }
            else
            {
                fraction = (uint)((lost_interval << 8) / expected_interval);
            }
        }


        #endregion

        #endregion

        #region Nested Types

        #region CommonHeaderBits class

        /// <summary>
        /// Dervived from the fact that both abstractions presented utilize the first two octets.
        /// Represents all standard bit fields which can be found in the first 16 bits of any Rtp or Rtcp packet.
        /// </summary>    
        /// <remarks>        
        /// 
        /// Not a struct because: 
        ///     1) bit fields are utilized, structures can only be offset in bytes with a whole integer number. (Double precision would be required in FieldOffset this get to work or a BitFieldOffset which takes double.)
        ///     2) structures must be passed by reference and would force this abstraction to be copied unless every call took reference.
        ///     3) You cannot manually remove references to a value type or set a structure to null which then causes the GC to maintin the pointer and refrerence count for more time and would lead to more memory leaks.
        ///     4) You can't inherit a struct and subsequently any derived implementation would need to redudantly store reference to something it can't be rid of manually.
        ///     
        /// public to allow derived implementation, hence not sealed.
        /// 
        /// This Type only declares 2 fields which are value types and owns no other references.
        /// </remarks>
        public class CommonHeaderBits : BaseDisposable, IEnumerable<byte>
        {

            #region Notes

            /*

4. Byte Order, Alignment, and Time Format

   All integer fields are carried in network byte order, that is, most
   significant byte (octet) first.  This byte order is commonly known as
   big-endian.  The transmission order is described in detail in [3].
   Unless otherwise noted, numeric constants are in decimal (base 10).

   All header data is aligned to its natural length, i.e., 16-bit fields
   are aligned on even offsets, 32-bit fields are aligned at offsets
   divisible by four, etc.  Octets designated as padding have the value
   zero.

   Wallclock time (absolute date and time) is represented using the
   timestamp format of the Network Time Protocol (NTP), which is in
   seconds relative to 0h UTC on 1 January 1900 [4].  The full
   resolution NTP timestamp is a 64-bit unsigned fixed-point number with
   the integer part in the first 32 bits and the fractional part in the
   last 32 bits.  In some fields where a more compact representation is
   appropriate, only the middle 32 bits are used; that is, the low 16
   bits of the integer part and the high 16 bits of the fractional part.
   The high 16 bits of the integer part must be determined
   independently.

   An implementation is not required to run the Network Time Protocol in
   order to use RTP.  Other time sources, or none at all, may be used
   (see the description of the NTP timestamp field in Section 6.4.1).
   However, running NTP may be useful for synchronizing streams
   transmitted from separate hosts.

   The NTP timestamp will wrap around to zero some time in the year
   2036, but for RTP purposes, only differences between pairs of NTP
   timestamps are used.  So long as the pairs of timestamps can be
   assumed to be within 68 years of each other, using modular arithmetic
   for subtractions and comparisons makes the wraparound irrelevant.

5. RTP Data Transfer Protocol

5.1 RTP Fixed Header Fields

   The RTP header has the following format:

    0                   1                   2                   3
    0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |V=2|P|X|  CC   |M|     PT      |       sequence number         |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |                           timestamp                           |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |           synchronization source (SSRC) identifier            |
   +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
   |            contributing source (CSRC) identifiers             |
   |                             ....                              |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
             * 
   The draft header had the following format:
             *
    V V P X C C C C M T T T T T T T S S S S S S S S S S S S S S S S (RTP 2 Header Comparison)
    0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |Ver| ChannelID |P|S|  format   |       sequence number         |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |     timestamp (seconds)       |     timestamp (fraction)      |
   +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
   | options ...                                                   |
   +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
            */

            #endregion

            #region Statics and Constants

            /// <summary>
            /// The amount of octets this class represents.
            /// </summary>
            public const int Size = 2;

            /// <summary>
            /// 3 SHL 6 produces a 8 bit value of 11000000
            /// </summary>
            public const byte VersionMask = 192;

            /// <summary>
            /// 1 SHL 7 produces a 8 bit value of 1000000 (128) Decimal
            /// </summary>
            public const int RtpMarkerMask = 128;

            /// <summary>
            /// 1 SHL 5 produces a 8 bit value of 00100000 (32 Decimal)
            /// </summary>
            public const byte PaddingMask = 32;

            /// <summary>
            /// 1 SHL 4 produces a 8 bit value of 00010000 (16) Decimal
            /// </summary>
            internal static byte ExtensionMask = 16;

            /// <summary>
            /// Composes an octet with the common bit fields utilized by both the Rtp and Rtcp abstractions in the 1st octet of the first word.
            /// If <paramref name="extension"/> is true then only the high nybble of the <paramref name="remainingBits"/> integer will be masked into the resulting octet.
            /// </summary>
            /// <param name="version">a 2 bit value, 0, 1, 2 or 3.</param>
            /// <param name="padding">Indicates the value of the 2nd Bit</param>
            /// <param name="extension">Indicates the value of the 3rd Bit</param>
            /// <param name="remainingBits">Bits 4, 5, 6, 7 and 8</param>
            /// <returns>The octet which has been composed as a result of packing the bit fields</returns>
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public static byte PackOctet(int version, bool padding, bool extension, byte remainingBits = 0)
            {
                //Ensure the version is valid in a quarter bit
                if (version > 3) throw Binary.QuarterBitOverflow;

                //Check if the value can be packed into an octet.
                if (padding && extension)//Only 4 bits are available if padding and extensions are set
                {
                    //if (remainingBits > Binary.FiveBitMaxValue) throw new ArgumentException("Padding and Extensions cannot be set when remaining bits is greater than 31");
                    remainingBits |= (byte)(CommonHeaderBits.ExtensionMask | CommonHeaderBits.PaddingMask);
                }
                else if (padding)// 5 bits are available when Padding is set
                {
                    //if (remainingBits > Binary.FiveBitMaxValue) throw new ArgumentException("Padding cannot be set when remaining bits is greater than 31");
                    remainingBits |= CommonHeaderBits.PaddingMask;
                }
                else if (extension)// Could be considered to be the sign bit if not utilized when packing (Occupies the 5 bit)
                {
                    //if (remainingBits > Binary.FourBitMaxValue) throw new ArgumentException("Extensions cannot be set when remaining bits is greater than 15");
                    remainingBits |= CommonHeaderBits.ExtensionMask;
                }

                //if (BitConverter.IsLittleEndian) remainingBits = Common.Binary.ReverseU8((byte)remainingBits);

                //Pack the results into an octet
                return PacketOctet(version, remainingBits);
            }

            //Should ensure remainingBits in BigEndian...

            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public static byte PacketOctet(int version, byte remainingBits)
            {
                return (byte)(version << 6 | (byte)remainingBits);
            }

            /// <summary>
            /// Composes an octet with the common bit fields utilized by both Rtp and Rtcp abstractions in the 2nd octet of the first word.
            /// </summary>
            /// <param name="marker"></param>
            /// <param name="payloadTypeBits"></param>
            /// <returns></returns>
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public static byte PackOctet(bool marker, int payloadTypeBits)
            {
                return ((byte)(marker ? (RtpMarkerMask | (byte)payloadTypeBits) : payloadTypeBits));
            }

            #endregion

            #region Fields

            /// <summary>
            /// If created from memory existing
            /// </summary>
            internal readonly Common.MemorySegment m_Memory;

            /// <summary>
            /// The first and second octets themselves, utilized by both Rtp and Rtcp.
            /// Seperated to prevent checks on endian.
            /// </summary>
            //protected byte leastSignificant, mostSignificant; // Wastes 2 bytes when not used.

            #endregion

            #region Properties

            internal byte First8Bits
            {
                get { return m_Memory.Array[m_Memory.Offset]; }
                set
                {
                    m_Memory.Array[m_Memory.Offset] = value;
                }
            }
            
            internal byte Last8Bits
            {
                get { return m_Memory.Array[m_Memory.Offset + 1]; }
                set
                {
                    m_Memory.Array[m_Memory.Offset + 1] = value;
                }
            }

            /// <summary>
            /// Gets or sets bits 0 and 1; from the lowest quartet of the first octet.
            /// Throws a Overflow exception if the value is less than 0 or greater than 3.
            /// </summary>
            public int Version
            {
                //Only 1 shift is required to read the version and it shouldn't matter about the endian
                get { return First8Bits >> 6; } //BitConverter.IsLittleEndian ? First8Bits >> 6 : First8Bits << 6; }
                //get { return (int)Common.Binary.ReadBitsMSB(m_Memory.Array, Common.Binary.BitsToBytes(m_Memory.Offset), 2); }
                set
                {
                    //Get a unsigned copy to prevent two checks, the value is only 5 bits and must be aligned to this boundary in the octet
                    byte unsigned = (byte)value;

                    //Only 2 bits 4 possible values 0, 1, 2, 3, Compliments of two
                    //4 << 7 - 1 = 25 = 1 00 000000 which overflows byte
                    if (value > 3)
                        throw Binary.CreateOverflowException("Version", unsigned, 0x00.ToString(), 0x03.ToString());

                    //Values 0 - 3 only utilize 2 bits, shift the correct amount of places based on the input value
                    //0 << 7 - 1 = 00 000000 which no value is present in the lowest quartet
                    //1 << 7 - 1 = 64  = 01 000000
                    //2 << 7 - 1 = 128 = 10 000000
                    //3 << 7 - 1 = 192 = 11 000000
                    //Where 7 is the amount of `addressable` bits based on a 0 index
                    //leastSignificant = (byte)((value << 7 - 1) | (Padding ? PaddingMask : 0) | (Extension ? ExtensionMask : 0) | RtpContributingSourceCount);
                    //use the block count which encompasses the RtpContributingSourceCount
                    First8Bits = PackOctet(unsigned, Padding, Extension, (byte)RtcpBlockCount);
                }
            }

            //Draft only
            internal int Channel
            {
                get { return First8Bits & VersionMask; }
                //set
            }

            /// <summary>
            /// Gets or sets the Padding bit.
            /// </summary>
            public bool Padding
            {
                //Example 223 & 32 == 0
                //Where 32 == PaddingMask and 223 == (11011111) Binary and would indicate a version 3 header with no padding, extension set and 15 CC
                get { return (First8Bits & PaddingMask) > 0; }
                internal set
                {
                    //Comprise an octet with the required Version, Padding and Extension bit set
                    //Where 6 is the amount of unnecessary bits which preceeded the reqired value in the byte
                    //leastSignificant = (byte)((byte)Version << 6 | (value ? (PaddingMask) : 0) | (Extension ? (byte)(ExtensionMask) : 0) | RtpContributingSourceCount);
                    First8Bits = PackOctet((byte)Version, value, Extension, (byte)RtpContributingSourceCount);
                }
            }

            //Draft only
            internal bool OptionsPresent
            {
                get { return (Last8Bits >> 7) > 0; }
                //set
            }

            /// <summary>
            /// Gets or sets the Extension bit.
            /// </summary>
            public bool Extension
            {
                //There are 8 bits in a byte.
                //Where 3 is the amount of unnecessary bits preceeding the Extension bit
                //and 7 is amount of bits to discard to place the extension bit at the highest indicie of the octet (8)
                //get { return First8Bits > 0 && (Common.Binary.ReadBitsWithShift(First8Bits, 3, 7, Media.Common.Binary.IsBigEndian) & ExtensionMask) > 0; }
                get { return (First8Bits & ExtensionMask) > 0; }
                set { First8Bits = PackOctet((byte)Version, Padding, value, (byte)RtpContributingSourceCount); }
            }

            /// <summary>
            /// Gets or sets 5 bits value stored in the first octet of all RtcpPackets.
            /// Throws an Overflow exception if the value cannot be stored in the bit field.
            /// Throws an Argument exception if the value is equal to <see cref="Binary.FiveBitMaxValue"/> and the <see cref="CommonHeaderBits.Extension" /> bit is set.
            /// </summary>
            public int RtcpBlockCount
            {
                //Where 3 | PaddingMask = 224 (decimal) 11100000
                //Example 255 & 244 = 31 which is the Maximum value which is able to be stored in this field.
                //Where 255 = byte.MaxValue
                get { return (byte)(First8Bits & Common.Binary.FiveBitMaxValue); } // BitConverter.IsLittleEndian ? Common.Binary.ReverseU8((byte)(First8Bits << 3)) : Common.Binary.ReverseU8((byte)(First8Bits >> 3)); }
                //get { return (int)Common.Binary.ReadBitsMSB(m_Memory.Array, Common.Binary.BytesToBits(m_Memory.Offset) + 3, 5); }
                set
                {
                    if (value > Binary.FiveBitMaxValue)
                        throw Binary.CreateOverflowException("RtcpBlockCount", value, byte.MinValue.ToString(), Binary.FiveBitMaxValue.ToString());

                    //129 - 10000001 Little ByteOrder
                    //Version 2, Padding 0, 00001

                    //To make it correct unsigned has to be reversed 10000 = 16

                    //Get a unsigned copy to prevent two checks, the value is only 5 bits and must be aligned to this boundary in the octet
                    byte unsigned = (byte)value; //BitConverter.IsLittleEndian ? (byte)(Common.Binary.ReverseU8((byte)(value)) >> 3) : (byte)(value << 3);

                    //Include the padding bit if it was set prior
                    if (Padding) unsigned |= PaddingMask;

                    //Re pack the octet
                    First8Bits = PacketOctet(Version, unsigned);
                }
            }

            /// <summary>
            /// Gets or sets the nybble assoicted with the Rtp CC bit field.
            /// Throws an Overflow exception if the value is greater than <see cref="Binary.FourBitMaxValue"/>.
            /// </summary>
            public int RtpContributingSourceCount
            {
                //Contributing sources only exist in the highest half of the `leastSignificant` octet.
                //Example 240 = 11110000 and would indicate 0 Contributing Sources etc.
                //get { return Common.Binary.ReverseU8((byte)Common.Binary.ReadBitsWithShift(First8Bits, 0, 4, BitConverter.IsLittleEndian)); }
                //get { return BitConverter.IsLittleEndian ? Common.Binary.ReverseU8((byte)(First8Bits << 4)) : First8Bits << 4;} // Common.Binary.ReverseU8((byte)(First8Bits >> 4)); }
                get { return (First8Bits & Common.Binary.FourBitMaxValue); }
                //get { return (int)Common.Binary.ReadBitsMSB(m_Memory.Array, Common.Binary.BytesToBits(m_Memory.Offset) + 4, 4); }
                internal set
                {
                    //If the value exceeds the highest value which can be stored in the bit field throw an overflow exception
                    if (value > Binary.FourBitMaxValue)
                        throw Binary.CreateOverflowException("RtpContributingSourceCount", value, byte.MinValue.ToString(), Binary.FourBitMaxValue.ToString());

                    //Get a unsigned copy to prevent two checks, the value is only 4 bits and must be aligned to this boundary in the octet
                    //byte unsigned = BitConverter.IsLittleEndian ? (byte)(Common.Binary.ReverseU8((byte)(value)) >> 4) : (byte)(value << 4);

                    //re pack the octet
                    First8Bits = PackOctet(Version, Padding, Extension, (byte)value);
                }
            }

            /// <summary>
            /// Gets or sets the RtpMarker bit.
            /// </summary>
            public bool RtpMarker
            {
                get { return Last8Bits > Binary.SevenBitMaxValue; }
                set { Last8Bits = PackOctet(value, (byte)RtpPayloadType); }
            }

            //Draft only
            internal bool EndOfSynchroniztionUnit
            {
                get { return (Last8Bits & 64) > 0; }
                //set
            }

            /// <summary>
            /// Gets or sets the 7 bit value associated with the RtpPayloadType.
            /// </summary>            
            public int RtpPayloadType
            {
                //& Binary.SevenBitMaxValue may be faster
                //get { return Last8Bits > 0 ? (byte)((Last8Bits << 1)) >> 1 : 0; }
                get { return Last8Bits & Binary.SevenBitMaxValue; }
                //get { return (int)Common.Binary.ReadBitsMSB(m_Memory.Array, Common.Binary.BytesToBits(m_Memory.Offset + 1) + 1, 7); }
                set
                {
                    //Get an unsigned copy of the value to prevent 2 checks 
                    byte unsigned = (byte)value;

                    //If the value exceeds the highest value which can be stored in the bit field throw an overflow exception
                    if (unsigned > Binary.SevenBitMaxValue)
                        throw Binary.CreateOverflowException("RtpPayloadType", unsigned, byte.MinValue.ToString(), sbyte.MaxValue.ToString());

                    Last8Bits = PackOctet(RtpMarker, (byte)unsigned);
                }
            }

            //Draft only
            internal int Format
            {
                get { return (Last8Bits & VersionMask); }
                //set
            }

            /// <summary>
            /// Gets or sets the 8 bit value associated with the RtcpPayloadType.
            /// Note that in RtpPackets that this field is shared with the Marker bit and if the value has Bit 0 set then the RtpMarker property will be true.
            /// </summary>
            /// <remarks>
            /// A SendersReport has the RtpPayloadType of 72.
            /// </remarks>            
            public int RtcpPayloadType
            {
                get { return Last8Bits; }
                set { Last8Bits = (byte)value; } //Check Marker before setting?
            }

            #endregion

            #region Constructor

            /// <summary>
            /// Creates a exact copy of the given CommonHeaderBits
            /// </summary>
            /// <param name="other">The CommonHeaderBits instance to copy</param>
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public CommonHeaderBits(CommonHeaderBits other, bool reference = false)
            {
                if (reference)
                {
                    m_Memory = other.m_Memory;
                }
                else
                {
                    m_Memory = new MemorySegment(CommonHeaderBits.Size);

                    Array.Copy(other.m_Memory.Array, other.m_Memory.Offset, m_Memory.Array, 0, CommonHeaderBits.Size);
                }
            }

            /// <summary>
            /// Constructs a managed representation around a copy of the given two octets
            /// </summary>
            /// <param name="one">The first byte</param>
            /// <param name="two">The second byte</param>
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public CommonHeaderBits(byte one, byte two)
            {
                m_Memory = new MemorySegment(CommonHeaderBits.Size);

                m_Memory[0] = one;

                m_Memory[1] = two;
            }

            /// <summary>
            /// Expects at least 2 bytes of data
            /// </summary>
            /// <param name="data"></param>
            /// <param name="offset"></param>
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public CommonHeaderBits(byte[] data, int offset)
            {
                m_Memory = new MemorySegment(data, offset, CommonHeaderBits.Size);
            }

            /// <summary>
            /// Makes an exact copy of the header from the given memory.
            /// </summary>
            /// <param name="memory"></param>
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public CommonHeaderBits(Common.MemorySegment memory)//, int additionalOffset = 0)
            {
                //if (Math.Abs(memory.Count - additionalOffset) < CommonHeaderBits.Size) throw new InvalidOperationException("at least two octets are required in memory");

                if (memory == null || memory.Count < CommonHeaderBits.Size) throw new InvalidOperationException("at least two octets are required in memory");

                m_Memory = new Common.MemorySegment(memory.Array, memory.Offset /*+ additionalOffset*/, CommonHeaderBits.Size);
            }

            /// <summary>
            /// Constructs a new instance of the CommonHeaderBits with the given values packed into the bit fields.
            /// </summary>
            /// <param name="version">The version of the common header bits</param>
            /// <param name="padding">The value of the Padding bit</param>
            /// <param name="extension">The value of the Extension bit</param>
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public CommonHeaderBits(int version, bool padding, bool extension)
                : this(CommonHeaderBits.PackOctet(version, padding, extension), 0)
            {

            }

            /// <summary>
            /// Constructs a new instance of the CommonHeaderBits with the given values packed into the bit fields.
            /// The <paramref name="payloadTypeBits"/> usually refer to count of Contributing Sources and will be stored in the managed <propertyref name="RtpContributingSourceCount"/> property.
            /// </summary>
            /// <param name="version">The version of the common header bits</param>
            /// <param name="padding">The value of the Padding bit</param>
            /// <param name="extension">The value of the Extension bit</param>
            /// <param name="marker">The value of the Marker bit</param>
            /// <param name="payloadTypeBits">The value of the PayloadType bits</param>
            /// /// <param name="otherbits">The value of the remaning bits which are not utilized. (4 bits)</param>
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public CommonHeaderBits(int version, bool padding, bool extension, bool marker, int payloadTypeBits, byte otherBits)
                : this(CommonHeaderBits.PackOctet(version, padding, extension, otherBits), CommonHeaderBits.PackOctet(marker, payloadTypeBits))
            {
               
            }

            #endregion

            //Clone?

            #region IEnumerator Implementations

            public IEnumerator<byte> GetEnumerator()
            {
                if (IsDisposed) yield break;

                //return ((IEnumerable<byte>)m_Memory).GetEnumerator();

                Common.MemorySegment segment = m_Memory;

                byte[] array = segment.Array;

                int offset = segment.Offset;

                yield return array[offset++];

                yield return array[offset];
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            #endregion

            #region Overrides

            public override void Dispose()
            {
                base.Dispose();

                if (ShouldDispose)
                {
                    m_Memory.Dispose();
                }
            }

            public override int GetHashCode() { return (short)this; }

            public override bool Equals(object obj)
            {
                if (false == (obj is CommonHeaderBits)) return false;

                CommonHeaderBits bits = obj as CommonHeaderBits;

                if (bits.m_Memory != m_Memory) return false;

                return GetHashCode() == bits.GetHashCode();
            }

            #endregion

            #region Methods

            public int CopyTo(byte[] dest, int offset)
            {
                if (IsDisposed) return 0;

                int copied = 0;

                Common.MemorySegmentExtensions.CopyTo(m_Memory, dest, offset);

                copied += m_Memory.Count;

                offset += m_Memory.Count;

                return copied;
            }

            #endregion

            #region Implicit Operators

            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public static implicit operator short(CommonHeaderBits bits) { return Common.Binary.Read16(bits, 0, BitConverter.IsLittleEndian); }

            public static bool operator ==(CommonHeaderBits a, CommonHeaderBits b)
            {
                object boxA = a, boxB = b;
                return boxA == null ? boxB == null : a.Equals(b);
            }

            public static bool operator !=(CommonHeaderBits a, CommonHeaderBits b) { return false == (a == b); }

            #endregion
        }

        #endregion

        #region SourceList

        /// <summary>
        /// Provides a managed implementation around reading the SourceList from the binary present in a RtpPacket.
        /// Marked IDisposable incase derived and to indicate when the implementation is no longer required.
        /// 
        /// Note a mixer which is making a new SourceList should account for the fact that only 15 sources per RtpPacket can be indicated in a single RtpPacket,
        /// for more information see
        /// <see href="http://tools.ietf.org/html/rfc3550">Page 15, paragraph `CSRC list`</see>
        /// </summary>
        public sealed class SourceList : BaseDisposable, IEnumerator<uint>, IEnumerable<uint>, IReportBlock
            /*, IReadOnlyCollection<uint> */ //Only needed if modifications to a SourceList are allowed at run time.
        {
            #region Constants / Statics

            /// <summary>
            /// The size in octets of each element in the SourceList
            /// </summary>
            public const int ItemSize = 4;

            /// <summary>
            /// Maximum amount of items in a source list is 31
            /// </summary>
            public const int MaxItems = Common.Binary.FiveBitMaxValue;

            /// <summary>
            /// 31 * 4 = 124 bytes.
            /// </summary>
            public const int MaxSize = ItemSize * MaxItems;
            
            #endregion

            #region Fields

            /// <summary>
            /// The memory which contains the SourceList
            /// </summary>
            readonly Common.MemorySegment m_Binary = Common.MemorySegment.Empty;

            int m_CurrentOffset, //The current offset in parsing the binary
                m_SourceCount, //The amount of ContributingSources to read given from the CC nybble in a RtpHeader
                m_Read;//The amount of ContributingSources read so far.

            /// <summary>
            /// The current source item.
            /// </summary>
            uint m_CurrentSource;

            #endregion

            #region Constructor

            [CLSCompliant(false)]
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public SourceList(uint ssrc) : this(Media.Common.Extensions.Linq.LinqExtensions.Yield(ssrc)) { }

            /// <summary>
            /// Copies Data
            /// </summary>
            /// <param name="sources"></param>
            /// <param name="start"></param>
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public SourceList(IEnumerable<uint> sources, int start = 0)
            {
                IEnumerable<byte> binary = Media.Common.MemorySegment.EmptyBytes;

                foreach (var ssrc in sources.Skip(start))
                {
                    binary = binary.Concat(Binary.GetBytes(ssrc, BitConverter.IsLittleEndian)).ToArray();
                    
                    //Increment for the added value and determine if the maximum is reached.
                    if (++m_SourceCount >= SourceList.MaxItems) break;
                }

                m_Binary = new Common.MemorySegment(binary.ToArray(), 0, m_SourceCount * Binary.BytesPerInteger);
            }

            /// <summary>
            /// Creates a new source list from the given parameters.
            /// The SourceList has a reference to the buffer and always should be disposed of when no longer used.
            /// </summary>
            /// <param name="header">The <see cref="RtpHeader"/> to read the <see cref="RtpHeader.ContributingSourceCount"/> from</param>
            /// <param name="buffer">The buffer (which is vector of 32 bit values e.g. it will be read in increments of 32 bits per read)</param>
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public SourceList(Media.Rtp.RtpHeader header, byte[] buffer, int offset = 0)
            {
                if (header == null) throw new ArgumentNullException("header");

                if (header.IsCompressed) throw new NotSupportedException();

                //Assign the count (don't read it again)
                m_SourceCount = header.ContributingSourceCount;

                if (m_SourceCount > 0)
                {
                    if (buffer == null) throw new ArgumentNullException("buffer");

                    //Source lists are only inserted by a mixer and come directly after the header and would be present in the payload,
                    //before the RtpExtension (if present) and before the RtpPacket's actual binary data

                    //Make a segment to the data which corresponds to the data, preventing values less than 0 or greater than the amount of bytes needed to reference the data
                    m_Binary = new Common.MemorySegment(buffer, offset, Binary.Clamp(buffer.Length - offset, 0, m_SourceCount * Binary.BytesPerInteger));
                }
            }

            /// <summary>
            /// Creates a new source list from the given parameters.
            /// The SourceList owns ownly it's own resources and always should be disposed immediately.
            /// </summary>
            /// <param name="packet">The <see cref="RtpPacket"/> to create a SourceList from</param>
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public SourceList(Media.Rtp.RtpPacket packet)
                : this(packet.Header, packet.Payload.Array, packet.Payload.Offset)
            {

            }

            /// <summary>
            /// Creates a SourceList from the given data when the count of sources in the data is known in advance.
            /// </summary>
            /// <param name="sourceCount">The count of sources expected in the SourceList</param>
            /// <param name="data">The data contained in the SourceList.</param>
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public SourceList(int sourceCount)
            {
                m_SourceCount = Common.Binary.Min(SourceList.MaxItems, sourceCount);

                m_Binary = new Common.MemorySegment(Binary.BytesPerInteger * m_SourceCount);

            }

            /// <summary>
            /// Creates a SourceList from the data contained in the GoodbyeReport.
            /// Contains it's own reference to the payload and should be disposed of when no longer needed.
            /// </summary>
            /// <param name="goodbyeReport">The GoodbyeReport</param>
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public SourceList(GoodbyeReport goodbyeReport)
            {
                m_SourceCount = goodbyeReport.Header.BlockCount;

                //Make a new reference to the payload at the correct offset
                m_Binary = new Common.MemorySegment(goodbyeReport.Payload.Array, goodbyeReport.Payload.Offset, Binary.Min(goodbyeReport.Payload.Count, m_SourceCount * Binary.BytesPerInteger));
                    //new Common.MemorySegment(goodbyeReport.Payload);
            }

            #endregion

            #region Properties

            /// <summary>
            /// Indicates if there is enough data in the given binary to read the complete source list.
            /// </summary>
            public bool IsComplete { get { return false == IsDisposed && m_SourceCount * Binary.BytesPerInteger == m_Binary.Count; } }

            uint IEnumerator<uint>.Current
            {
                get
                {
                    CheckDisposed();
                    //if (m_CurrentOffset == m_Binary.Offset) throw new InvalidOperationException("Enumeration has not started yet.");
                    if (m_Read <= 0) throw new InvalidOperationException("Enumeration has not started yet.");
                    return m_CurrentSource;
                }
            }

            /// <summary>
            /// Provides an 32 bit signed representation of the CurrentSource.
            /// </summary>
            public int CurrentSource
            {
                get
                {
                    if (m_CurrentOffset == m_Binary.Offset) throw new InvalidOperationException("Enumeration has not started yet.");
                    return (int)m_CurrentSource;
                }
            }

            /// <summary>
            /// The current contributing source item
            /// </summary>
            object System.Collections.IEnumerator.Current
            {
                get
                {
                    //Check disposed
                    CheckDisposed();

                    //Indicate Enumeration has not yet started
                    if (m_CurrentOffset == m_Binary.Offset) return null;

                    //Return the current item
                    return m_CurrentSource;
                }
            }

            /// <summary>
            /// Gets the amount of sources which can be read from the list.
            /// </summary>
            public int Count { get { return m_SourceCount; } }

            /// <summary>
            /// Gets the length in octets of the SourceList.
            /// </summary>
            public int Size { get { return m_SourceCount * 4; } }

            /// <summary>
            /// Indicates how many indexes are left in the SourceList based on the current index.
            /// </summary>
            public int Remaining { get { return Count - m_Read; } }

            /// <summary>
            /// The Capacity of this SourceList.
            /// </summary>
            public int Capacity { get { return m_SourceCount; } }

            /// <summary>
            /// The index to which <see cref="Current"/> corresponds
            /// </summary>
            public int ItemIndex { get { return m_Read; } }

            /// <summary>
            /// Gets the memory assoicated with the instance
            /// </summary>
            public Common.MemorySegment Memory { get { return m_Binary; } }

            #endregion

            #region Methods

            //Should also modify csrc
            ///// <summary>
            ///// Add the given id to this SourceList at the current position and sets the CurrentSource.
            ///// </summary>
            ///// <param name="id"></param>
            //public void Add(int id)
            //{
            //    //Check capacity
            //    if (Remaining <= 0) return;

            //    //Set the current item
            //    m_CurrentSource = (uint)id;

            //    //Write the given value to the correct position
            //    Binary.WriteNetwork32(m_Binary.Array, m_CurrentOffset, Media.Common.Binary.IsBigEndian, m_CurrentSource);

            //    //Move the offset
            //    m_CurrentOffset += 4;

            //    //Incremnt read
            //    ++m_Read;
            //}

            /// <summary>
            /// Moves to the next offset and parses the next contributing source.
            /// </summary>
            /// <returns>True if a value was read, otherwise false.</returns>
            public bool MoveNext()
            {
                if (IsDisposed) return false;

                //If there is a value to read and the binary data encompasses the required offset.
                if (m_Read < m_SourceCount && m_CurrentOffset + Binary.BytesPerInteger < m_Binary.Count)
                {
                    //Read the unsigned 16 bit value from the binary data
                    m_CurrentSource = Binary.ReadU16(m_Binary.Array, m_CurrentOffset, true);

                    //advance the offset
                    m_CurrentOffset += Binary.BytesPerInteger;

                    ++m_Read;

                    //indicate success
                    return true;
                }

                //indicate failure
                return false;
            }

            /// <summary>
            /// Resets the Enumerator
            /// </summary>
            public void Reset()
            {
                //Prevent unintended behvavior
                CheckDisposed();

                //Reset the current offset
                m_CurrentOffset = m_Binary.Offset;

                //Reset the amount of items read
                m_Read = 0;

                //Reset the current source
                m_CurrentSource = default(uint);
            }

            /// <summary>
            /// Prepares a sequence of data containing the values indicated
            /// </summary>
            /// <param name="offset">The logical offset of the item in the list</param>
            /// <param name="count">the amount of items in the list</param>
            /// <returns>4 bytes for each value indicated by count starting at the given offset.</returns>
            public IEnumerable<byte> AsBinaryEnumerable(int offset, int count)
            {
                return m_Binary.Skip(offset * ItemSize).Take(count * ItemSize);
            }

            /// <summary>
            /// Prepares a binary sequence of 'Size' containing all of the data in the SourceList.
            /// </summary>
            /// <returns>The sequence created.</returns>
            public IEnumerable<byte> AsBinaryEnumerable()
            {
                return AsBinaryEnumerable(0, m_SourceCount);
            }

            /// <summary>
            /// Tries to copies the ContribingSourceList to another vector.
            /// </summary>
            /// <param name="other">The vector to copy to</param>
            /// <param name="offset">The offset in the vector</param>
            /// <returns>True if the copy succeeded otherwise false.</returns>
            public bool TryCopyTo(byte[] other, int offset)
            {
                try
                {
                    CheckDisposed();
                    Array.Copy(m_Binary.Array, m_Binary.Offset, other, offset, Math.Min(m_SourceCount * Binary.BytesPerInteger, m_Binary.Count));
                    return true;
                }
                catch { return false; }
            }

            /// <summary>
            /// Tries to copy the contained unsigned integers from the current in the enumeration to the given list.
            /// </summary>
            /// <param name="list">The list to add the items enumerated to.</param>
            /// <param name="index">The 0 based index of <paramref name="list"/> to start copying.</param>
            /// <returns>True if MoveNext was called, otherwise false.</returns>
            public bool TryCopyTo(IList<uint> list, int index = 0)
            {
                if (list == null || list.IsReadOnly) return false;
                try
                {
                    CheckDisposed();

                    while (MoveNext()) list.Insert(index++, m_CurrentSource);

                    return true;
                }
                catch { return false; }
            }

            public sealed override void Dispose()
            {

                if (IsDisposed) return;

                base.Dispose();

                if (ShouldDispose)
                {
                    m_Binary.Dispose();
                }
            }

            #endregion

            #region Interface Implementation Stubs

            IEnumerator<uint> IEnumerable<uint>.GetEnumerator()
            {
                return this;
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return this;
            }

            #endregion

            #region IReportBlock

            /// <summary>
            /// <see cref="Size"/>
            /// </summary>
            int IReportBlock.Size
            {
                get { return Size; }
            }

            /// <summary>
            /// <see cref="CurrentSource"/>
            /// </summary>
            int IReportBlock.BlockIdentifier
            {
                get { return CurrentSource; }
            }

            /// <summary>
            /// Provides the binary data assoicated with this SourceList
            /// </summary>
            IEnumerable<byte> IReportBlock.BlockData
            {
                get { return m_Binary; }
            }

            #endregion
        }

        #endregion

        //Would be removed from RtpTools and placed here but would provide no way to get a frametype unless a mapping was also created.
        //Would need mapping from rtpmap or fmtp because payloadtype alone is not enough.
        //Could use it with Payload unless Dynamic is found and then iterate Dynamic profiles...
        //RFC3551

        //public class RtpProfile
        //{
        //    //readonly ValueType
        //    readonly byte PayloadType;

        //    readonly string EncodingName;

        //    readonly Sdp.MediaType MediaType;

        //    //readonly ValueType
        //    readonly long ClockRate;
        //}

        //Something like H.264 would have the Sps and Pps as properties availble through a derived FormatLine.

        ////public class RtpAudioProfile : RtpProfile
        ////{
        ////    readonly int Channels, BitsPerSample;

        ////    //MediaType = Audio
        ////}

        ////public class RtpVideoProfile : RtpProfile
        ////{
        ////    readonly int Width, Height, BitsPerSample;

        ////    //MediaType = Video
        ////}

        //RtpProfiles?  

        //{ StaticProfiles, DynamicProfiles }

        // Register, Unregister, Reassign
      
        #endregion
    }
}
