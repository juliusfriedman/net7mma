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

        public static int Random32(int type = 0)
        {

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

            byte[] structure = BitConverter.GetBytes(type).//int     type;
                Concat(BitConverter.GetBytes(DateTime.UtcNow.Ticks)).Concat(BitConverter.GetBytes(Environment.TickCount)).//struct  timeval tv;
                Concat(BitConverter.GetBytes(TimeSpan.TicksPerMillisecond)).//clock_t cpu;
                Concat(BitConverter.GetBytes(System.Diagnostics.Process.GetCurrentProcess().Id)).//pid_t   pid;
                Concat(BitConverter.GetBytes(42)).//u_long  hid;
                Concat(BitConverter.GetBytes(7)).//uid_t   uid;
                Concat(Guid.NewGuid().ToByteArray()).//gid_t   gid;
                Concat(System.Text.Encoding.Default.GetBytes(Environment.OSVersion.VersionString)).ToArray();//struct  utsname name;

            //UtsName equivelant information would be

            //char  sysname[]  name of this implementation of the operating system
            //char  nodename[] name of this node within an implementation-dependent                 communications network
            //char  release[]  current release level of this implementation
            //char  version[]  current version level of this release
            //char  machine[]  name of the hardware type on which the system is running

            //Perform MD5 on structure per 3550
            byte[] digest;
            
            using(var md5 = Utility.CreateMD5HashAlgorithm()) digest = md5.ComputeHash(structure);

            //Complete hash
            uint r = 0;
            r ^= BitConverter.ToUInt32(digest, 0);
            r ^= BitConverter.ToUInt32(digest, 4);
            r ^= BitConverter.ToUInt32(digest, 8);
            r ^= BitConverter.ToUInt32(digest, 12);
            return (int)r;
        }

        public const int RtcpValidMask = 0xc000 | 0x2000 | 0xfe;

        /// <summary>
        /// Calculates a value which can be used in conjunction with the RtcpValidMask to validate a RtcpHeader.
        /// <see cref="http://tools.ietf.org/html/rfc3550#appendix-A">Appendix A</see>
        /// </summary>
        /// <param name="version">The version of the RtcpPacket to validate</param>
        /// <param name="payloadType">The optional payloadType to use in the calulcation. Defaults to 201</param>
        /// <returns>The value calulcated</returns>
        public static int RtcpValidValue(int version, int payloadType = Rtcp.SendersReport.PayloadType)
        {
            //Always calulated in Big Endian
            return (version << 14 | payloadType);
        }

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
            if (!IsValidRtcpHeader(first.Header, first.Version)) throw new InvalidOperationException("A Compound packet must start with either a SendersReport or a ReceiversReport.");

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
                if (!hasCName && packet.PayloadType == SourceDescriptionReport.PayloadType)
                {
                    //The packets contained a SourceDescription Report.
                    hasSourceDescription = true;

                    //if not already checked for a cname check now
                    if (!hasCName && packet.BlockCount > 0) using (SourceDescriptionReport asReport = new SourceDescriptionReport(packet, false)) if ((hasCName = asReport.HasCName)) break;
                }                
            }

            //Verify the SourceDesscription has a CName Item if present
            if (hasSourceDescription && !hasCName) throw new InvalidOperationException("A Compound RtcpPacket must have a SourceDescriptionReport with a SourceDescriptionChunk with a CName item.");

            //Determine if padding is required
            int paddingAmount = totalLength % 4;

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

            PreparePackets:
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
            if (version < 2) throw new ArgumentException("There are no Compound Packets in Versions less than 2");

            //Keep track of how many packets were parsed.
            int parsedPackets = 0;

            //Each Compound RtcpPacket must have a SourceDescription with a CName and may have a goodbye
            bool hasSourceDescription = false, hasCName = false;

            //Get all packets contained in the buffer.
            foreach (RtcpPacket currentPacket in RtcpPacket.GetPackets(array, offset, count, version, null, ssrc)) 
            {

                //Determine who the packet is from and what type it is.
                int firstPayloadType = currentPacket.PayloadType;

                //The first packet in a compound packet needs to be validated
                if (parsedPackets == 0 && !IsValidRtcpHeader(currentPacket.Header, currentPacket.Version)) yield break;
                else if (currentPacket.Version != version || skipUnknownTypes && RtcpPacket.GetImplementationForPayloadType((byte)currentPacket.PayloadType) == null) yield break;
                
                //Count the packets parsed
                ++parsedPackets;

                //if the packet is a SourceDescriptionReport ensure a CName is present.
                if (!hasCName && currentPacket.PayloadType == SourceDescriptionReport.PayloadType)
                {
                    //The packets contained a SourceDescription Report.
                    hasSourceDescription = true;

                    //if not already checked for a cname check now
                    if (!hasCName && currentPacket.BlockCount > 0) using (SourceDescriptionReport asReport = new SourceDescriptionReport(currentPacket, false)) if ((hasCName = asReport.HasCName)) break;
                }

                if (hasSourceDescription && !hasCName) Common.ExceptionExtensions.CreateAndRaiseException(currentPacket, "Invalid compound data, Source Description report did not have a CName SourceDescriptionItem.");

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
        public static int ReadPadding(Common.MemorySegment segment, int position)
        {


            int segmentCount = segment.Count;

            //If there are no more bytes to parse we cannot continue
            if (segmentCount == 0 || position > segmentCount) return 0;

            /*
              If the padding bit is set, the packet contains one or more
              additional padding octets at the end which are not part of the
              payload.  The last octet of the padding contains a count of how
              many padding octets should be ignored, including itself.  Padding
              may be needed by some encryption algorithms with fixed block sizes
              or for carrying several RTP packets in a lower-layer protocol data unit.
          */
            

            //Iterate forwards looking for padding ending at the count of bytes in the segment of memory given
            for (; position < segmentCount; ++position)
            {

                byte val = segment.Array[position];

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
        public static IEnumerable<byte> CreatePadding(int amount)
        {
            if (amount <= 0) return Enumerable.Empty<byte>();
            if (amount > byte.MaxValue) Common.Binary.CreateOverflowException("amount", amount, byte.MinValue.ToString(), byte.MinValue.ToString());
            return Enumerable.Concat(Enumerable.Repeat(default(byte), amount - 1), ((byte)amount).Yield());
        }

        //Random32 etc

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
        /// This instance only declares 2 fields which are value types and owns no other references.
        /// </remarks>
        public class CommonHeaderBits : BaseDisposable, IEnumerable<byte>
        {
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
            /// 1 SHL 7 produces a 8 bit value of 1000000 (127) Decimal
            /// </summary>
            public const int RtpMarkerMask = Binary.SevenBitMaxValue;

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

            public static byte PacketOctet(int version, byte remainingBits)
            {
                return (byte)((byte)(BitConverter.IsLittleEndian ? version << 6 : version >> 6) | (byte)remainingBits);
            }

            /// <summary>
            /// Composes an octet with the common bit fields utilized by both Rtp and Rtcp abstractions in the 2nd octet of the first word.
            /// </summary>
            /// <param name="marker"></param>
            /// <param name="payloadTypeBits"></param>
            /// <returns></returns>
            public static byte PackOctet(bool marker, int payloadTypeBits)
            {
                return ((byte)(marker ? Binary.Or(128, (byte)payloadTypeBits) : (byte)payloadTypeBits));
            }

            #endregion

            #region Fields

            /// <summary>
            /// If created from memory existing
            /// </summary>
            Common.MemorySegment m_Memory;

            /// <summary>
            /// The first and octets themselves, utilized by both Rtp and Rtcp.
            /// Seperated to prevent checks on endian.
            /// </summary>
            protected byte leastSignificant, mostSignificant;

            #endregion

            #region Properties

            internal byte First8Bits
            {
                get { return m_Memory != null ? m_Memory.Array[m_Memory.Offset] : leastSignificant; }
                set
                {
                    if (m_Memory != null)
                    {
                        m_Memory.Array[m_Memory.Offset] = value;
                    }
                    else
                    {
                        leastSignificant = value;
                    }
                }
            }

            internal byte Last8Bits
            {
                get { return m_Memory != null ? m_Memory.Array[m_Memory.Offset + 1] : mostSignificant; }
                set
                {
                    if (m_Memory != null)
                    {
                        m_Memory.Array[m_Memory.Offset + 1] = value;
                    }
                    else
                    {
                        mostSignificant = value;
                    }
                }
            }

            /// <summary>
            /// Gets or sets bits 0 and 1; from the lowest quartet of the first octet.
            /// Throws a Overflow exception if the value is less than 0 or greater than 3.
            /// </summary>
            public int Version
            {
                //Only 1 shift is required to read the version
                get { return BitConverter.IsLittleEndian ? First8Bits >> 6 : First8Bits << 6; }
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

            /// <summary>
            /// Gets or sets the Extension bit.
            /// </summary>
            public bool Extension
            {
                //There are 8 bits in a byte.
                //Where 3 is the amount of unnecessary bits preceeding the Extension bit
                //and 7 is amount of bits to discard to place the extension bit at the highest indicie of the octet (8)
                //get { return First8Bits > 0 && (Common.Binary.ReadBitsWithShift(First8Bits, 3, 7, !BitConverter.IsLittleEndian) & ExtensionMask) > 0; }
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
                set
                {
                    if (value > Binary.FiveBitMaxValue)
                        throw Binary.CreateOverflowException("RtcpBlockCount", value, byte.MinValue.ToString(), Binary.FiveBitMaxValue.ToString());

                    //129 - 10000001 Little Endian
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
                get { return BitConverter.IsLittleEndian ? Common.Binary.ReverseU8((byte)(First8Bits << 4)) : Common.Binary.ReverseU8((byte)(First8Bits >> 4)); }
                internal set
                {

                    //If the value exceeds the highest value which can be stored in the bit field throw an overflow exception
                    if (value > Binary.FourBitMaxValue)
                        throw Binary.CreateOverflowException("RtpContributingSourceCount", value, byte.MinValue.ToString(), Binary.FourBitMaxValue.ToString());

                    //Get a unsigned copy to prevent two checks, the value is only 4 bits and must be aligned to this boundary in the octet
                    //byte unsigned = BitConverter.IsLittleEndian ? (byte)(Common.Binary.ReverseU8((byte)(value)) >> 4) : (byte)(value << 4);

                    //re pack the octet
                    First8Bits = PackOctet(Version, Padding, Extension, BitConverter.IsLittleEndian ? (byte)(Common.Binary.ReverseU8((byte)(value)) >> 4) : (byte)(value << 4));
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

            /// <summary>
            /// Gets or sets the 7 bit value associated with the RtpPayloadType.
            /// </summary>
            public int RtpPayloadType
            {
                get { return Last8Bits > 0 ? (byte)((Last8Bits << 1)) >> 1 : 0; }
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
            public CommonHeaderBits(CommonHeaderBits other)
            {
                leastSignificant = other.leastSignificant;
                mostSignificant = other.mostSignificant;
            }

            /// <summary>
            /// Constructs a managed representation around a copy of the given two octets
            /// </summary>
            /// <param name="lsb">The least significant 8 bits</param>
            /// <param name="msb">The most significant 8 bits</param>
            public CommonHeaderBits(byte lsb, byte msb)
            {
                //Assign them

                leastSignificant = lsb;

                mostSignificant = msb;
            }

            public CommonHeaderBits(Common.MemorySegment memory, int additionalOffset = 0)
            {
                if (Math.Abs(memory.Count - additionalOffset) < 2) throw new InvalidOperationException("at least two octets are required in memory");

                m_Memory = new Common.MemorySegment(memory.Array, memory.Offset + additionalOffset, 2);
            }

            /// <summary>
            /// Constructs a new instance of the CommonHeaderBits with the given values packed into the bit fields.
            /// </summary>
            /// <param name="version">The version of the common header bits</param>
            /// <param name="padding">The value of the Padding bit</param>
            /// <param name="extension">The value of the Extension bit</param>
            public CommonHeaderBits(int version, bool padding, bool extension)
            {
                //Pack the bit fields in the first octet wich belong there
                leastSignificant = CommonHeaderBits.PackOctet(version, padding, extension);
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
            public CommonHeaderBits(int version, bool padding, bool extension, bool marker, int payloadTypeBits, byte otherBits)
                : this(version, padding, extension)
            {
                //Pack the bit fields in the second octet which belong there
                mostSignificant = CommonHeaderBits.PackOctet(marker, payloadTypeBits);

                if (otherBits > 0) RtcpBlockCount = otherBits;
            }

            #endregion

            #region IEnumerator Implementations

            public IEnumerator<byte> GetEnumerator()
            {
                if (m_Memory != null)
                {
                    Common.MemorySegment segment = m_Memory;

                    byte[] array = segment.Array;

                    int offset = segment.Offset;

                    yield return array[offset++];

                    yield return array[offset];
                }
                else
                {
                    yield return leastSignificant;

                    yield return mostSignificant;
                }
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            #endregion

            #region Overrides

            public override int GetHashCode() { return (short)this; }

            public override bool Equals(object obj)
            {
                if (!(obj is CommonHeaderBits)) return false;

                CommonHeaderBits bits = obj as CommonHeaderBits;

                if (bits.m_Memory != m_Memory) return false;

                return GetHashCode() == bits.GetHashCode();
            }

            #endregion

            #region Implicit Operators

            public static implicit operator short(CommonHeaderBits bits) { return Common.Binary.Read16(bits, 0, BitConverter.IsLittleEndian); }

            public static bool operator ==(CommonHeaderBits a, CommonHeaderBits b)
            {
                object boxA = a, boxB = b;
                return boxA == null ? boxB == null : a.Equals(b);
            }

            public static bool operator !=(CommonHeaderBits a, CommonHeaderBits b) { return !(a == b); }

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
        public sealed class SourceList : BaseDisposable, IEnumerator<uint>, IEnumerable<uint>, IReadOnlyCollection<uint>
        {
            #region Constants / Statics

            /// <summary>
            /// The size in octets of each element in the SourceList
            /// </summary>
            public const int ItemSize = 4;

            //Maybe choose to allow creation of a FixedSizedList

            #endregion

            #region Fields

            byte[] m_OwnedOctets;

            /// <summary>
            /// The memory which contains the SourceList
            /// </summary>
            Common.MemorySegment m_Binary;

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
            public SourceList(uint ssrc) : this(ssrc.Yield()) { }

            public SourceList(IEnumerable<uint> sources, int start = 0)
            {
                m_SourceCount = Math.Max(Common.Binary.FourBitMaxValue, sources.Count());

                IEnumerable<byte> binary = Utility.Empty;

                foreach (var ssrc in sources.Skip(start))
                {
                    if (BitConverter.IsLittleEndian)
                        binary = binary.Concat(BitConverter.GetBytes(ssrc).Reverse()).ToArray();
                    else
                        binary = binary.Concat(BitConverter.GetBytes(ssrc)).ToArray();
                }

                m_Binary = new Common.MemorySegment(binary.ToArray(), 0, m_SourceCount * 4);
            }

            /// <summary>
            /// Creates a new source list from the given parameters.
            /// The SourceList owns ownly it's own resources and always should be disposed immediately.
            /// </summary>
            /// <param name="header">The <see cref="RtpHeader"/> to read the <see cref="RtpHeader.ContributingSourceCount"/> from</param>
            /// <param name="buffer">The buffer (which is vector of 32 bit values e.g. it will be read in increments of 32 bits per read)</param>
            public SourceList(Media.Rtp.RtpHeader header, byte[] buffer)
            {
                if (header == null) throw new ArgumentNullException("header");

                //Assign the count (don't read it again)
                m_SourceCount = header.ContributingSourceCount;

                if (buffer == null) throw new ArgumentNullException("buffer");

                //Keep a reference to the buffer and the amount of bytes required
                if (m_SourceCount > 0)
                {
                    //Source lists are only inserted by a mixer and come directly after the header and would be present in the payload,
                    //before the RtpExtension (if present) and before the RtpPacket's actual binary data
                    m_Binary = new Common.MemorySegment(buffer, 0, Math.Min(buffer.Length, m_SourceCount * 4));
                }
            }

            /// <summary>
            /// Creates a new source list from the given parameters.
            /// The SourceList owns ownly it's own resources and always should be disposed immediately.
            /// </summary>
            /// <param name="packet">The <see cref="RtpPacket"/> to create a SourceList from</param>
            public SourceList(Media.Rtp.RtpPacket packet)
                : this(packet.Header, packet.Payload.Array)
            {

            }

            /// <summary>
            /// Creates a SourceList from the given data when the count of sources in the data is known in advance.
            /// </summary>
            /// <param name="sourceCount">The count of sources expected in the SourceList</param>
            /// <param name="data">The data contained in the SourceList.</param>
            public SourceList(int sourceCount)
            {
                m_SourceCount = sourceCount;
                int sourceListSize = 4 * sourceCount;
                m_OwnedOctets = new byte[sourceListSize];
                m_Binary = new Common.MemorySegment(m_OwnedOctets, 0, sourceListSize);
            }

            /// <summary>
            /// Creates a SourceList from the data contained in the GoodbyeReport
            /// </summary>
            /// <param name="goodbyeReport">The GoodbyeReport</param>
            public SourceList(GoodbyeReport goodbyeReport)
            {
                m_SourceCount = goodbyeReport.Header.BlockCount;
                m_Binary = goodbyeReport.Payload;
            }

            #endregion

            #region Properties

            /// <summary>
            /// Indicates if there is enough data in the given binary to read the complete source list.
            /// </summary>
            public bool IsComplete { get { return !IsDisposed && m_SourceCount * 4 == m_Binary.Count; } }

            uint IEnumerator<uint>.Current
            {
                get
                {
                    CheckDisposed();
                    if (m_CurrentOffset == m_Binary.Offset) throw new InvalidOperationException("Enumeration has not started yet.");
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
            //    Binary.WriteNetwork32(m_Binary.Array, m_CurrentOffset, !BitConverter.IsLittleEndian, m_CurrentSource);

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
                if (m_Read < m_SourceCount && m_CurrentOffset + 4 < m_Binary.Count)
                {
                    //Read the unsigned 16 bit value from the binary data
                    m_CurrentSource = Binary.ReadU16(m_Binary.Array, m_CurrentOffset, true);

                    //advance the offset
                    m_CurrentOffset += 4;

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
            /// Prepares a binary sequence of 'Size' containing all of the data in the SourceList.
            /// </summary>
            /// <returns>The sequence created.</returns>
            public IEnumerable<byte> AsBinaryEnumerable()
            {
                return m_Binary.Array.Skip(m_Binary.Offset).Take(m_Binary.Count);
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
                    Array.Copy(m_Binary.Array, m_Binary.Offset, other, offset, Math.Min(m_SourceCount * 4, m_Binary.Count));
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

                //Should always happen
                if (ShouldDispose)
                {
                    m_OwnedOctets = null;

                    m_Binary = null;
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
        }

        #endregion

        #endregion
    }
}
