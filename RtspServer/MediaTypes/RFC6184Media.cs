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

//http://tools.ietf.org/html/rfc6184

using System;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;

namespace Media.Rtsp.Server.MediaTypes
{

    /// <summary>
    /// Provides an implementation of <see href="https://tools.ietf.org/html/rfc6184">RFC6184</see> which is used for H.264 Encoded video.
    /// </summary>
    public class RFC6184Media : RFC2435Media //Todo use RtpSink not RFC2435Media
    {
        //Some MP4 Related stuff
        //https://github.com/fyhertz/libstreaming/blob/master/src/net/majorkernelpanic/streaming/mp4/MP4Parser.java

        //C# h264 elementary stream stuff
        //https://bitbucket.org/jerky/rtp-streaming-server

        //C# MP4 and H.264 ES Writer
        //http://iknowu.duckdns.org/files/public/MP4Maker/MP4Maker.htm

        /// <summary>
        /// Implements Packetization and Depacketization of packets defined in <see href="https://tools.ietf.org/html/rfc6184">RFC6184</see>.
        /// </summary>
        public class RFC6184Frame : Rtp.RtpFrame
        {
            #region Static

            public static byte[] FullStartSequence = new byte[] { 0x00, 0x00, 0x00, 0x01 };

            static readonly Common.MemorySegment FullStartSequenceSegment = new Common.MemorySegment(FullStartSequence, false);

            static readonly Common.MemorySegment ShortStartSequenceSegment = new Common.MemorySegment(FullStartSequence, 1, 3, false);

            public static byte[] CreateSingleTimeAggregationUnit(int? DON = null, params byte[][] nals)
            {

                if (nals == null || nals.Count() == 0) throw new InvalidOperationException("Must have at least one nal");

                //Get the data required which consists of the Length and the nal.
                IEnumerable<byte> data = nals.SelectMany(n => Common.Binary.GetBytes((short)n.Length, Common.Binary.IsLittleEndian).Concat(n));

                //STAP - B has DON at the very beginning
                if (DON.HasValue)
                {
                    data = Media.Common.Extensions.Linq.LinqExtensions.Yield(Media.Codecs.Video.H264.NalUnitType.SingleTimeAggregationB).Concat(Common.Binary.GetBytes((short)DON, Common.Binary.IsLittleEndian)).Concat(data);
                }//STAP - A
                else data = Media.Common.Extensions.Linq.LinqExtensions.Yield(Media.Codecs.Video.H264.NalUnitType.SingleTimeAggregationA).Concat(data);

                return data.ToArray();
            }

            public static byte[] CreateMultiTimeAggregationUnit(int DON, byte dond, int tsOffset, params byte[][] nals)
            {

                if (nals == null || nals.Count() == 0) throw new InvalidOperationException("Must have at least one nal");

                //Get the data required which consists of the Length and the nal.
                IEnumerable<byte> data = nals.SelectMany(n =>
                {
                    byte[] lengthBytes = new byte[2];
                    Common.Binary.Write16(lengthBytes, 0, Common.Binary.IsLittleEndian, (short)n.Length);

                    //GetBytes

                    //DOND
                    //TS OFFSET

                    byte[] tsOffsetBytes = new byte[3];

                    Common.Binary.Write24(tsOffsetBytes, 0, Common.Binary.IsLittleEndian, tsOffset);

                    return Media.Common.Extensions.Linq.LinqExtensions.Yield(dond).Concat(lengthBytes).Concat(n);
                });

                //MTAP has DON at the very beginning
                data = Media.Common.Extensions.Linq.LinqExtensions.Yield(Media.Codecs.Video.H264.NalUnitType.MultiTimeAggregation16).Concat(Media.Common.Binary.GetBytes((short)DON, Common.Binary.IsLittleEndian)).Concat(data);

                return data.ToArray();
            }

            public static byte[] CreateMultiTimeAggregationUnit(int DON, byte dond, short tsOffset, params byte[][] nals)
            {

                if (nals == null || nals.Count() == 0) throw new InvalidOperationException("Must have at least one nal");

                //Get the data required which consists of the Length and the nal.
                IEnumerable<byte> data = nals.SelectMany(n =>
                {
                    byte[] lengthBytes = new byte[2];
                    Common.Binary.Write16(lengthBytes, 0, Common.Binary.IsLittleEndian, (short)n.Length);

                    //Common.Binary.GetBytes((short)n.Length, Common.Binary.IsLittleEndian);

                    //DOND

                    //TS OFFSET

                    byte[] tsOffsetBytes = new byte[2];

                    Common.Binary.Write16(tsOffsetBytes, 0, Common.Binary.IsLittleEndian, tsOffset);
                    
                    return Media.Common.Extensions.Linq.LinqExtensions.Yield(dond).Concat(tsOffsetBytes).Concat(lengthBytes).Concat(n);
                });

                //MTAP has DON at the very beginning
                data = Media.Common.Extensions.Linq.LinqExtensions.Yield(Media.Codecs.Video.H264.NalUnitType.MultiTimeAggregation24).Concat(Media.Common.Binary.GetBytes((short)DON, Common.Binary.IsLittleEndian)).Concat(data);

                return data.ToArray();
            }

            #endregion

            #region Constructor

            public RFC6184Frame(byte payloadType)
                : base(payloadType)
            {
                m_ContainedNalTypes = new List<byte>();
            }

            public RFC6184Frame(Rtp.RtpFrame existing)
                : base(existing)
            {
                m_ContainedNalTypes = new List<byte>();
            }

            public RFC6184Frame(RFC6184Frame existing)
                : base(existing, true, true)
            {
                m_ContainedNalTypes = existing.m_ContainedNalTypes;
            }

            //AllowMultipleMarkerPackets

            #endregion

            #region Fields

            //May be kept in a state or InformationClass eventually, would allow for other options to be kept also.

            //Should use HashSet? (would not allow counting of types but isn't really needed)
            internal protected readonly List<byte> m_ContainedNalTypes;

            #endregion

            #region Properties

            /// <summary>
            /// Indicates if a NalUnit which corresponds to a SupplementalEncoderInformation is contained.
            /// </summary>
            public bool ContainsSupplementalEncoderInformation
            {
                get
                {
                    return m_ContainedNalTypes.Any(t => t == Media.Codecs.Video.H264.NalUnitType.SupplementalEncoderInformation);
                }
            }

            /// <summary>
            /// Indicates if a NalUnit which corresponds to a SequenceParameterSet is contained.
            /// </summary>
            public bool ContainsSequenceParameterSet
            {
                get
                {
                    return m_ContainedNalTypes.Any(t => t == Media.Codecs.Video.H264.NalUnitType.SequenceParameterSet);
                }
            }

            /// <summary>
            /// Indicates if a NalUnit which corresponds to a PictureParameterSet is contained.
            /// </summary>
            public bool ContainsPictureParameterSet
            {
                get
                {
                    return m_ContainedNalTypes.Any(t => t == Media.Codecs.Video.H264.NalUnitType.PictureParameterSet);
                }
            }

            //bool ContainsInitializationSet return m_ContainedNalTypes.Any(t => t == Media.Codecs.Video.H264.NalUnitType.PictureParameterSet || t == Media.Codecs.Video.H264.NalUnitType.SequenceParameterSet);

            /// <summary>
            /// Indicates if a NalUnit which corresponds to a InstantaneousDecoderRefresh is contained.
            /// </summary>
            public bool ContainsInstantaneousDecoderRefresh
            {
                get
                {
                    return m_ContainedNalTypes.Any(t => t == Media.Codecs.Video.H264.NalUnitType.InstantaneousDecoderRefresh);
                }
            }

            /// <summary>
            /// Indicates if a NalUnit which corresponds to a CodedSlice is contained.
            /// </summary>
            public bool ContainsCodedSlice
            {
                get
                {
                    return m_ContainedNalTypes.Any(t => t == Media.Codecs.Video.H264.NalUnitType.CodedSlice);
                }
            }

            //This is not necessarily in the sorted order of the packets if packets were added out of order.

            /// <summary>
            /// After Packetization or Depacketization, will indicate the types of Nal units contained in the data of the frame.
            /// </summary>
            public IEnumerable<byte> ContainedUnitTypes
            {
                get
                {
                    return m_ContainedNalTypes;
                }
            }

            #endregion

            //Should be overriden
            /// <summary>
            /// Creates any <see cref="Rtp.RtpPacket"/>'s required for the given nal by copying the data to RtpPacket instances.
            /// </summary>
            /// <param name="nal">The nal</param>
            /// <param name="mtu">The mtu</param>
            public virtual void Packetize(byte[] nal, int mtu = 1500, int? DON = null) //sequenceNumber
            {
                if (nal == null) return;

                int nalLength = nal.Length;

                int offset = 0;

                if (nalLength >= mtu)
                {
                    //Consume the original header and move the offset into the data
                    byte nalHeader = nal[offset++],
                        nalFNRI = (byte)(nalHeader & 0xE0), //Extract the F and NRI bit fields
                        nalType = (byte)(nalHeader & Common.Binary.FiveBitMaxValue), //Extract the Type
                        fragmentType = (byte)(DON.HasValue ? Media.Codecs.Video.H264.NalUnitType.FragmentationUnitB : Media.Codecs.Video.H264.NalUnitType.FragmentationUnitA),
                        fragmentIndicator = (byte)(nalFNRI | fragmentType);//Create the Fragment Indicator Octet

                    //Store the nalType contained
                    m_ContainedNalTypes.Add(nalType);

                    //Determine if the marker bit should be set.
                    bool marker = false; //(nalType == Media.Codecs.Video.H264.NalUnitType.AccessUnitDelimiter);

                    //Get the highest sequence number
                    int highestSequenceNumber = HighestSequenceNumber;

                    //Consume the bytes left in the nal
                    while (offset < nalLength)
                    {
                        //Get the data required which consists of the fragmentIndicator, Constructed Header and the data.
                        IEnumerable<byte> data;

                        //Build the Fragmentation Header

                        //First Packet
                        if (offset == 1)
                        {
                            //FU (A/B) Indicator with F and NRI
                            //Start Bit Set with Original NalType

                            data = Enumerable.Concat(Media.Common.Extensions.Linq.LinqExtensions.Yield(fragmentIndicator), Media.Common.Extensions.Linq.LinqExtensions.Yield(((byte)(0x80 | nalType))));
                        }
                        else if (offset + mtu > nalLength)
                        {
                            //End Bit Set with Original NalType
                            data = Enumerable.Concat(Media.Common.Extensions.Linq.LinqExtensions.Yield(fragmentIndicator), Media.Common.Extensions.Linq.LinqExtensions.Yield(((byte)(0x40 | nalType))));

                            //This should not be set at the nal level for end of nal units.
                            //marker = true;

                        }
                        else//For packets other than the start or end
                        {
                            //No Start, No End
                            data = Enumerable.Concat(Media.Common.Extensions.Linq.LinqExtensions.Yield(fragmentIndicator), Media.Common.Extensions.Linq.LinqExtensions.Yield(nalType));
                        }

                        //FU - B has DON at the very beginning of each 
                        if (fragmentType == Media.Codecs.Video.H264.NalUnitType.FragmentationUnitB)// && Count == 0)// highestSequenceNumber == 0)
                        {
                            //byte[] DONBytes = new byte[2];
                            //Common.Binary.Write16(DONBytes, 0, Common.Binary.IsLittleEndian, (short)DON);

                            data = Enumerable.Concat(Common.Binary.GetBytes((short)DON, Common.Binary.IsLittleEndian), data);
                        }

                        //Add the data the fragment data from the original nal
                        data = Enumerable.Concat(data, nal.Skip(offset).Take(mtu));

                        //Add the packet using the next highest sequence number
                        Add(new Rtp.RtpPacket(2, false, false, marker, PayloadType, 0, SynchronizationSourceIdentifier, ++highestSequenceNumber, 0, data.ToArray()));

                        //Move the offset
                        offset += mtu;
                    }
                } //Should check for first byte to be 1 - 23?
                else Add(new Rtp.RtpPacket(2, false, false, false, PayloadType, 0, SynchronizationSourceIdentifier, HighestSequenceNumber + 1, 0, nal));
            }

            //Needs to ensure api is not confused with above. could also possibly handle in Packetize by searching for 0 0 1
            //public virtual void Packetize(byte[] accessUnit, int mtu = 1500, int? DON = null)
            //{
            //    throw new NotImplementedException();
            //    //Add all data and set marker packet on last packet.
            //    //Add AUD to next packet or the end of this one?
            //}

            //Not needed since ProcessPacket can do this.
            //public void Depacketize(bool ignoreForbiddenZeroBit = true, bool fullStartCodes = false)
            //{
            //    //base.Depacketize();

            //    DisposeBuffer();

            //    m_Buffer = new MemoryStream();

            //    var packets = Packets;

            //    //Todo, check if need to 
            //    //Order by DON / TSOFFSET (if any packets contains MTAP etc)

            //    //Get all packets in the frame and proces them
            //    foreach (Rtp.RtpPacket packet in packets)
            //        ProcessPacket(packet, ignoreForbiddenZeroBit, fullStartCodes);

            //    //Bring the buffer back the start. (This does not have a weird side effect of adding 0xa to the stream)
            //    m_Buffer.Seek(0, SeekOrigin.Begin);

            //    //This has a weird side effect of adding 0xa to the stream
            //    //m_Buffer.Position = 0;
            //}

            /// <summary>
            /// Depacketizes all contained packets and adds start sequences where necessary which can be though of as a H.264 RBSP 
            /// </summary>
            /// <param name="packet"></param>
            public override void Depacketize(Rtp.RtpPacket packet) { ProcessPacket(packet, false, false); }

            //Could be called Depacketize
            //Virtual because the RFC6190 logic defers to this method for non SVC nal types.
            /// <summary>
            /// Depacketizes a single packet.
            /// </summary>
            /// <param name="packet"></param>
            /// <param name="containsSps"></param>
            /// <param name="containsPps"></param>
            /// <param name="containsSei"></param>
            /// <param name="containsSlice"></param>
            /// <param name="isIdr"></param>
            internal protected virtual void ProcessPacket(Rtp.RtpPacket packet, bool ignoreForbiddenZeroBit = true, bool fullStartCodes = false)
            {
                //If the packet is null or disposed then do not process it.
                if (Common.IDisposedExtensions.IsNullOrDisposed(packet)) return;
               
                //Just put the packets into Depacketized at the end for most cases.
                int packetKey = packet.SequenceNumber;

                //The packets are not stored by SequenceNumber in Depacketized, they are stored in whatever Decoder Order is necessary.
                if (Depacketized.ContainsKey(packetKey)) return;

                //(May need to handle re-ordering)
                //In such cases this step needs to place the packets into a seperate collection for sorting on DON / TSOFFSET before writing to the buffer.

                //From the beginning of the data in the actual payload
                int offset = packet.Payload.Offset,
                   headerOctets = packet.HeaderOctets,
                   padding = packet.PaddingOctets,
                   count = packet.Payload.Count - (padding + headerOctets);

                //Must have at least 2 bytes (When nalUnitType is a FragmentUnit.. 3)
                if (count <= 2) return;

                //Start after the headerOctets
                offset += headerOctets;

                //Obtain the data of the packet with respect to extensions and csrcs present.
                byte[] packetData = packet.Payload.Array;

                if (false.Equals(packet.PayloadType.Equals(PayloadType)))
                {
                    if (false.Equals(AllowsMultiplePayloadTypes)) return;

                    //This is probably a new sps pps set

                    //The order should be in seqeuence and there must be data for it to be used.

                    //(Stores the nalType) Write the start code
                    DepacketizeStartCode(ref packetKey, ref packetData[offset], fullStartCodes);

                    //Add the depacketized data
                    Depacketized.Add(packetKey++, new Common.MemorySegment(packetData, offset, count));

                    return;
                }

                //Determine if the forbidden bit is set and the type of nal from the first byte
                byte firstByte = packetData[offset];

                //Should never be set... (unless decoding errors are present)
                if (false.Equals(ignoreForbiddenZeroBit) && false.Equals(0.Equals(((firstByte & 0x80) >> 7))))
                {
                    //would need additional state to ensure all packets now have this bit.

                    return; //throw new Exception("forbiddenZeroBit");
                }

                byte nalUnitType = (byte)(firstByte & Common.Binary.FiveBitMaxValue);

                //RFC6184 @ Page 20
                //o  The F bit MUST be cleared if all F bits of the aggregated NAL units are zero; otherwise, it MUST be set.
                //if (forbiddenZeroBit && nalUnitType <= 23 && nalUnitType > 29) throw new InvalidOperationException("Forbidden Zero Bit is Set.");

                //Needs other state to check if previously F was set or not

                //Media.Codecs.Video.H264.NalUnitPriority priority = (Media.Codecs.Video.H264.NalUnitPriority)((firstByte & 0x60) >> 5);

                //Determine what to do
                switch (nalUnitType)
                {
                    //Reserved - Ignore
                    case Media.Codecs.Video.H264.NalUnitType.Unknown:
                    case Media.Codecs.Video.H264.NalUnitType.PayloadContentScalabilityInformation:
                    case Media.Codecs.Video.H264.NalUnitType.Reserved:
                        {
                            //May have 4 byte NAL header.
                            //Do not handle
                            return;
                        }
                    case Media.Codecs.Video.H264.NalUnitType.SingleTimeAggregationA: //STAP - A
                    case Media.Codecs.Video.H264.NalUnitType.SingleTimeAggregationB: //STAP - B
                    case Media.Codecs.Video.H264.NalUnitType.MultiTimeAggregation16: //MTAP - 16
                    case Media.Codecs.Video.H264.NalUnitType.MultiTimeAggregation24: //MTAP - 24
                        {
                            //Move to Nal Data
                            ++offset;

                            //Todo Determine if need to Order by DON first.
                            //EAT DON for ALL BUT STAP - A
                            if (nalUnitType != Media.Codecs.Video.H264.NalUnitType.SingleTimeAggregationA)
                            {
                                //Should check for 2 bytes.

                                //Read the DecoderOrderingNumber and add the value from the index.
                                packetKey = Common.Binary.ReadU16(packetData, ref offset, Common.Binary.IsLittleEndian);

                                //If the number was already observed skip this packet.
                                //if (Depacketized.ContainsKey(packetKey)) return;

                            }

                            //Should check for 2 bytes.

                            //Consume the rest of the data from the packet
                            while (offset < count) // + 2 <=
                            {
                                //Determine the nal unit size which does not include the nal header
                                int tmp_nal_size = Common.Binary.Read16(packetData, ref offset, Common.Binary.IsLittleEndian);

                                //Should check for tmp_nal_size > 0
                                //If the nal had data and that data is in this packet then write it
                                if (tmp_nal_size >= 0)
                                {
                                    //For DOND and TSOFFSET
                                    switch (nalUnitType)
                                    {
                                        case Media.Codecs.Video.H264.NalUnitType.MultiTimeAggregation16:// MTAP - 16 (May require re-ordering)
                                            {

                                                //Should check for 3 bytes.

                                                //DOND 1 byte

                                                //Read DOND and TSOFFSET, combine the values with the existing index
                                                packetKey = (int)Common.Binary.ReadU24(packetData, ref offset, Common.Binary.IsLittleEndian);

                                                //If the number was already observed skip this packet.
                                                //if (Depacketized.ContainsKey(packetKey)) return;

                                                goto default;
                                            }
                                        case Media.Codecs.Video.H264.NalUnitType.MultiTimeAggregation24:// MTAP - 24 (May require re-ordering)
                                            {
                                                //Should check for 4 bytes.

                                                //DOND 2 bytes

                                                //Read DOND and TSOFFSET , combine the values with the existing index
                                                packetKey = (int)Common.Binary.ReadU32(packetData, ref offset, Common.Binary.IsLittleEndian);

                                                //If the number was already observed skip this packet.
                                                //if (Depacketized.ContainsKey(packetKey)) return;

                                                goto default;
                                            }
                                        default:
                                            {

                                                //Should check for tmp_nal_size > 0

                                                //Could check for extra bytes or emulation prevention
                                                //https://github.com/raspberrypi/userland/blob/master/containers/rtp/rtp_h264.c

                                                //(Stores the nalType) Write the start code
                                                DepacketizeStartCode(ref packetKey, ref packetData[offset], fullStartCodes);

                                                //Add the depacketized data and increase the index.

                                                //Ensure the size is within the count.
                                                //When tmp_nal_size is 0 packetData which is referenced by this segment which will have a 0 count.
                                                Depacketized.Add(packetKey++, new Common.MemorySegment(packetData, offset, Common.Binary.Min(tmp_nal_size, count - offset)));

                                                //Move the offset past the nal
                                                offset += tmp_nal_size;

                                                continue;
                                            }
                                    }
                                }
                            }

                            //No more data in packet.
                            return;
                        }
                    case Media.Codecs.Video.H264.NalUnitType.FragmentationUnitA: //FU - A
                    case Media.Codecs.Video.H264.NalUnitType.FragmentationUnitB: //FU - B (May require re-ordering)
                        {

                            /*
                             Informative note: When an FU-A occurs in interleaved mode, it
                             always follows an FU-B, which sets its DON.
                             * Informative note: If a transmitter wants to encapsulate a single
                              NAL unit per packet and transmit packets out of their decoding
                              order, STAP-B packet type can be used.
                             */
                            //Needs atleast 2 bytes to reconstruct... 
                            //3 bytes for any valid data to follow after the header.
                            if (count >= 2)
                            {
                                //Offset still at the firstByte (FU Indicator) move to and read FU Header
                                byte FUHeader = packetData[++offset];

                                bool Start = ((FUHeader & 0x80) >> 7) > 0;

                                    //https://tools.ietf.org/html/rfc6184 page 31...
                                
                                   //A fragmented NAL unit MUST NOT be transmitted in one FU; that is, the
                                   //Start bit and End bit MUST NOT both be set to one in the same FU
                                   //header.

                                //bool End = ((FUHeader & 0x40) >> 6) > 0;

                                //ignoreReservedBit

                                //bool Reserved = (FUHeader & 0x20) != 0;

                                //Should not be set 
                                //if (Reserved) throw new InvalidOperationException("Reserved Bit Set");

                                //Move to data (Just read the FU Header)
                                ++offset;

                                            //packet.SequenceNumber - packet.Timestamp;

                                //Store the DecoderingOrderNumber we will derive from the timestamp and sequence number.
                                //int DecodingOrderNumber = packetKey;

                                //DON Present in FU - B, add the DON to the DecodingOrderNumber
                                if (nalUnitType == Media.Codecs.Video.H264.NalUnitType.FragmentationUnitB)
                                {
                                    //Needs 2 more bytes...
                                    Common.Binary.ReadU16(packetData, ref offset, Common.Binary.IsLittleEndian);//offset += 2;
                                }

                                //Should verify count... just consumed 1 - 3 bytes and only required 2.

                                //Determine the fragment size
                                int fragment_size = count - offset;

                                //Should be optional
                                //Don't emit empty fragments
                                //if (fragment_size == 0) return;

                                //If the start bit was set
                                if (Start)
                                {
                                    //ignoreEndBit
                                    //if (End) throw new InvalidOperationException("Start and End Bit Set in same FU");

                                    //Reconstruct the nal header
                                    //Use the first 3 bits of the first byte and last 5 bites of the FU Header
                                    byte nalHeader = (byte)((firstByte & 0xE0) | (FUHeader & Common.Binary.FiveBitMaxValue));

                                    //(Stores the nal) Write the start code
                                    DepacketizeStartCode(ref packetKey, ref nalHeader, fullStartCodes);

                                    //Wasteful but there is no other way to provide this byte since it is constructor from two values in the header
                                    //Unless of course a FragmentHeader : MemorySegment was created, which could have a NalType property ...
                                    //Could also just have an overload which writes the NalHeader
                                        //Would need a CreateNalSegment static method with option for full (4 + 1) or short code ( 3 + 1)/
                                    Depacketized.Add(packetKey++, new Common.MemorySegment(new byte[] { nalHeader }));
                                }

                                //Add the depacketized data
                                Depacketized.Add(packetKey, new Common.MemorySegment(packetData, offset, fragment_size));

                                //Allow If End to Write End Sequence?
                                //Should only be done if last byte is 0?
                                //if (End) Buffer.WriteByte(Media.Codecs.Video.H264.NalUnitType.EndOfSequence);
                            }

                            //No more data?
                            return;
                        }
                    default: //Any other type excluding PayloadContentScalabilityInformation(30) and Reserved(31)
                        {
                            //(Stores the nalUnitType) Write the start code
                            DepacketizeStartCode(ref packetKey, ref nalUnitType, fullStartCodes);

                            //Add the depacketized data
                            Depacketized.Add(packetKey, new Common.MemorySegment(packetData, offset, count - offset));

                            return;
                        }
                }
            }

            //internal protected void WriteStartCode(ref byte nalHeader, bool fullStartCodes = false)
            //{
            //    int addIndex = Depacketized.Count;

            //    DepacketizeStartCode(ref addIndex, ref nalHeader, fullStartCodes);
            //}

            //internal static Common.MemorySegment CreateStartCode(ref byte nalType)
            //{

            //}

            internal protected void DepacketizeStartCode(ref int addIndex, ref byte nalHeader, bool fullStartCodes = false)
            {
                //Determine the type of Nal
                byte nalType = (byte)(nalHeader & Common.Binary.FiveBitMaxValue);

                //Store the nalType contained (this is possibly not in sorted order of which they occur)
                m_ContainedNalTypes.Add(nalType);

                if (fullStartCodes)
                {
                    Depacketized.Add(addIndex++, FullStartSequenceSegment);

                    return;
                }

                //Determine the type of start code prefix required.
                switch (nalType)
                {
                    ////Should technically only be written for first iframe in au and only when not precceded by sps and pps
                    //case Media.Codecs.Video.H264.NalUnitType.InstantaneousDecoderRefresh://5
                    //case Media.Codecs.Video.H264.NalUnitType.SequenceParameterSetSubset:// 15 (6190)
                    //    {
                    //        //Check if first nal in Access Unit m_ContainedNalTypes[0] == Media.Codecs.Video.H264.NalUnitType.SequenceParameterSetSubset;
                    //        if (m_Buffer.Position == 0) goto case Media.Codecs.Video.H264.NalUnitType.SequenceParameterSet;

                    //        //Handle without extra byte
                    //        goto default;
                    //    }
                    case Media.Codecs.Video.H264.NalUnitType.SupplementalEncoderInformation://6:                    
                    case Media.Codecs.Video.H264.NalUnitType.SequenceParameterSet://7:
                    case Media.Codecs.Video.H264.NalUnitType.PictureParameterSet://8:
                    case Media.Codecs.Video.H264.NalUnitType.AccessUnitDelimiter://9                                        
                        {
                            //Use the FullStartSequence
                            Depacketized.Add(addIndex++, FullStartSequenceSegment);

                            return;
                        }
                    // See: [ITU-T H.264] 7.4.1.2.4 Detection of the first VCL NAL unit of a primary coded picture
                    //case Media.Codecs.Video.H264.NalUnitType.CodedSlice:1  (6190)
                    //case Media.Codecs.Video.H264.NalUnitType.SliceExtension:20  (6190)
                    //    {
                    //        //Write the extra 0 byte to the Buffer (could also check for a contained slice header to eliminate the possibility of needing to check?)
                    //        if (m_Buffer.Position == 0 && isFirstVclInPrimaryCodedPicture()) goto case Media.Codecs.Video.H264.NalUnitType.AccessUnitDelimiter;

                    //        //Handle as normal
                    //        goto default;
                    //    }
                    default:
                        {
                            #region Notes

                            /* 7.1 NAL Unit Semantics
                             
                            1) The first byte of the RBSP contains the (most significant, left-most) eight bits of the SODB; the next byte of
                            the RBSP shall contain the next eight bits of the SODB, etc., until fewer than eight bits of the SODB remain.
                            
                            2) rbsp_trailing_bits( ) are present after the SODB as follows:
                            i) The first (most significant, left-most) bits of the final RBSP byte contains the remaining bits of the SODB,
                            (if any)
                            ii) The next bit consists of a single rbsp_stop_one_bit equal to 1, and
                            iii) When the rbsp_stop_one_bit is not the last bit of a byte-aligned byte, one or more
                            rbsp_alignment_zero_bit is present to result in byte alignment.
                            
                            3) One or more cabac_zero_word 16-bit syntax elements equal to 0x0000 may be present in some RBSPs after
                            the rbsp_trailing_bits( ) at the end of the RBSP.
                            Syntax structures having these RBSP properties are denoted in the syntax tables using an "_rbsp" suffix. These
                            structures shall be carried within NAL units as the content of the rbsp_byte[ i ] data bytes. The association of the RBSP
                            syntax structures to the NAL units shall be as specified in Table 7-1.
                            NOTE - When the boundaries of the RBSP are known, the decoder can extract the SODB from the RBSP by concatenating the bits
                            of the bytes of the RBSP and discarding the rbsp_stop_one_bit, which is the last (least significant, right-most) bit equal to 1, and
                            discarding any following (less significant, farther to the right) bits that follow it, which are equal to 0. The data necessary for the
                            decoding process is contained in the SODB part of the RBSP.
                            emulation_prevention_three_byte is a byte equal to 0x03. When an emulation_prevention_three_byte is present in the
                            NAL unit, it shall be discarded by the decoding process.
                            The last byte of the NAL unit shall not be equal to 0x00.
                            Within the NAL unit, the following three-byte sequences shall not occur at any byte-aligned position:
                            – 0x000000
                            – 0x000001
                            – 0x000002
                            Within the NAL unit, any four-byte sequence that starts with 0x000003 other than the following sequences shall not
                            occur at any byte-aligned position:
                            – 0x00000300
                            – 0x00000301
                            – 0x00000302
                            – 0x00000303 
                             
                             */

                            //Could also check last byte(s) in buffer to ensure no 0...

                            //FFMPEG changed to always emit full start codes.
                            //https://ffmpeg.org/doxygen/trunk/rtpdec__h264_8c_source.html

                            #endregion

                            //Add the short start sequence
                            Depacketized.Add(addIndex++, ShortStartSequenceSegment);

                            //Done
                            return;
                        }
                }
            }

            //Removing a packet effects m_ContainedNalTypes

            //protected internal override void RemoveAt(int index, bool disposeBuffer = true)
            //{
            //    base.RemoveAt(index, disposeBuffer);
            //}
            
            //internal protected override void DisposeBuffer()
            //{
            //    //The nals are definetely still contained when the buffer is disposed...
            //    m_ContainedNalTypes.Clear();

            //    base.DisposeBuffer();
            //}


            //The references to the list control it's disposition.
            //The property is marked as readonly and is not exposed anyway so clearing it is more work than is necessary.
            //public override void Dispose()
            //{
            //    base.Dispose();

            //    if (ShouldDispose) m_ContainedNalTypes.Clear();
            //}
            

            //To go to an Image...
            //Look for a SliceHeader in the Buffer
            //Decode Macroblocks in Slice
            //Convert Yuv to Rgb
        }

        #region Fields

        //Should be created dynamically

        //http://www.cardinalpeak.com/blog/the-h-264-sequence-parameter-set/

        //TODO, Use a better starting point e.g. https://github.com/jordicenzano/h264simpleCoder/blob/master/src/CJOCh264encoder.h or the OpenH264 stuff @ https://github.com/cisco/openh264

        //http://stackoverflow.com/questions/6394874/fetching-the-dimensions-of-a-h264video-stream

        //The sps should be changed to reflect the correct amount of macro blocks for the width and height specified as well as color depth.

        //profile_idc, profile_iop, level_idc

        protected byte[] sps = { 0x00, 0x00, 0x00, 0x01, 0x67, 0x42, 0x00, 0x0a, 0xf8, 0x41, 0xa2 };

        protected byte[] pps = { 0x00, 0x00, 0x00, 0x01, 0x68, 0xce, 0x38, 0x80 };

        byte[] //slice_header = { 0x00, 0x00, 0x00, 0x01, 0x05, 0x88, 0x84, 0x21, 0xa0 },
            slice_header1 = { 0x00, 0x00, 0x00, 0x01, 0x65, 0x88, 0x84, 0x21, 0xa0 },
            slice_header2 = { 0x00, 0x00, 0x00, 0x01, 0x65, 0x88, 0x94, 0x21, 0xa0 };

        bool useSliceHeader1 = true;
        
        byte[] macroblock_header = { 0x0d, 0x00 };

        #endregion

        #region Constructor

        public RFC6184Media(int width, int height, string name, string directory = null, bool watch = true)
            : base(name, directory, watch, width, height, false, 99)
        {
            Width = width;
            Height = height;
            Width += Width % 8;
            Height += Height % 8;
            ClockRate = 90;
        }

        #endregion

        #region Methods

        public override void Start()
        {
            if (m_RtpClient != null) return;

            base.Start();

            //Remove JPEG Track
            SessionDescription.RemoveMediaDescription(0);
            m_RtpClient.TransportContexts.Clear();

            //Add a MediaDescription to our Sdp on any available port for RTP/AVP Transport using the given payload type            
            SessionDescription.Add(new Sdp.MediaDescription(Sdp.MediaType.video, 0, Rtp.RtpClient.RtpAvpProfileIdentifier, 96)); //This is the payload description, it is defined in the profile

            //Add the control line and media attributes to the Media Description
            SessionDescription.MediaDescriptions.First().Add(new Sdp.SessionDescriptionLine("a=control:trackID=video")); //<- this is the id for this track which playback control is required, if there is more than 1 video track this should be unique to it

            SessionDescription.MediaDescriptions.First().Add(new Sdp.SessionDescriptionLine("a=rtpmap:96 H264/90000")); //<- 96 must match the Payload description given above

            //Prepare the profile information which is useful for receivers decoding the data, in this profile the Id, Sps and Pps are given in a base64 string.
            SessionDescription.MediaDescriptions.First().Add(new Sdp.SessionDescriptionLine("a=fmtp:96 profile-level-id=" + Common.Binary.ReadU24(sps, 4, Media.Common.Binary.IsBigEndian).ToString("X2") + ";sprop-parameter-sets=" + Convert.ToBase64String(sps, 4, sps.Length - 4) + ',' + Convert.ToBase64String(pps, 4, pps.Length - 4)));            

            //Create a context
            m_RtpClient.TryAddContext(new Rtp.RtpClient.TransportContext(0, 1,  //data and control channel id's (can be any number and should not overlap but can...)
                sourceId, //A randomId which was alredy generated 
                SessionDescription.MediaDescriptions.First(), //This is the media description we just created.
                false, //Don't enable Rtcp reports because this source doesn't communicate with any clients
                1, // This context is not in discovery
                0)); //This context is always valid from the first rtp packet received
        }

        //Ported from https://cardinalpeak.com/downloads/hello264.c

        //Move to Codec/h264
        //H264Encoder

        /// <summary>
        /// Packetize's an Image for Sending
        /// </summary>
        /// <param name="image">The Image to Encode and Send</param>
        public override void Packetize(System.Drawing.Image image)
        {
            try
            {
                //Make the width and height correct (Should not dispoe image if not resized... (https://net7mma.codeplex.com/discussions/652556)
                using (var thumb = image.Width != Width || image.Height != Height ? image.GetThumbnailImage(Width, Height, null, IntPtr.Zero) : image)
                {
                    //Ensure the transformation will work.
                    if (thumb.PixelFormat != System.Drawing.Imaging.PixelFormat.Format32bppArgb) throw new NotSupportedException("Only ARGB is currently supported.");

                    //Create a new frame
                    var newFrame = new RFC6184Frame(96); //should all payload type to come from the media description...

                    //Get RGB Stride
                    System.Drawing.Imaging.BitmapData data = ((System.Drawing.Bitmap)thumb).LockBits(new System.Drawing.Rectangle(0, 0, thumb.Width, thumb.Height),
                               System.Drawing.Imaging.ImageLockMode.ReadOnly, thumb.PixelFormat);

                    //MUST Convert the bitmap to yuv420
                    //switch on image.PixelFormat
                    //Utility.YUV2RGBManaged()
                    //Utility.ABGRA2YUV420Managed(image.Width, image.Height, data.Scan0);
                    //etc

                    //Todo use Media.Image.Transformations                   

                    byte[] yuv = Media.Codecs.Image.ColorConversions.ABGRA2YUV420Managed(thumb.Width, thumb.Height, data.Scan0);

                    ((System.Drawing.Bitmap)image).UnlockBits(data);

                    data = null;

                    List<IEnumerable<byte>> macroBlocks = new List<IEnumerable<byte>>();

                    //For each h264 Macroblock in the frame
                    for (int i = 0; i < Height / 16; i++)
                        for (int j = 0; j < Width / 16; j++)
                            macroBlocks.Add(EncodeMacroblock(i, j, yuv)); //Add an encoded macroblock to the list

                    macroBlocks.Add(new byte[] { 0x80 });//Stop bit (Wasteful by itself)

                    //Packetize the data with the slice header (omit start code)
                    newFrame.Packetize((useSliceHeader1 ? slice_header1 : slice_header2).Skip(4).Concat(macroBlocks.SelectMany(mb => mb)).ToArray());

                    //Change slice header next time.
                    useSliceHeader1 = !useSliceHeader1;

                    //Add the frame
                    AddFrame(newFrame);

                    yuv = null;

                    macroBlocks.Clear();

                    macroBlocks = null;
                }
            }
            catch { throw; }
        }

        IEnumerable<byte> EncodeMacroblock(int i, int j, byte[] yuvData)
        {

            IEnumerable<byte> result = Media.Common.MemorySegment.EmptyBytes;

            int frameSize = Width * Height;
            int chromasize = frameSize / 4;

            int yIndex = 0;
            int uIndex = frameSize;
            int vIndex = frameSize + chromasize;

            //If not the first macroblock in the slice
            if (!((i == 0) && (j == 0))) result = macroblock_header;
            else //There are offsets to the pixel values
            {
                int offset = i * Height + j * Width;

                if (offset > 0)
                {
                    yIndex += offset;
                    uIndex += offset;
                    vIndex += offset;
                }
            }

            //Take the Luma Values
            result = result.Concat(yuvData.Skip(yIndex ).Take(16 * 8));

            //Take the Chroma Values
            result = result.Concat(yuvData.Skip(uIndex ).Take(8 * 8));

            result = result.Concat(yuvData.Skip(vIndex ).Take(8 * 8));

            return result;
        }

        #endregion
    }

    public static class RFC6184FrameExtensions
    {
        public static bool Contains(this Media.Rtsp.Server.MediaTypes.RFC6184Media.RFC6184Frame frame, params byte[] nalTypes)
        {
            if (Common.IDisposedExtensions.IsNullOrDisposed(frame)) return false;

            if (Common.Extensions.Array.ArrayExtensions.IsNullOrEmpty(nalTypes)) return false;

            return frame.m_ContainedNalTypes.Any(n => nalTypes.Contains(n));
        }

        public static bool IsKeyFrame(this Media.Rtsp.Server.MediaTypes.RFC6184Media.RFC6184Frame frame)
        {
            if (Common.IDisposedExtensions.IsNullOrDisposed(frame)) return false;

            foreach (byte type in frame.m_ContainedNalTypes)
            {
                if (type == Media.Codecs.Video.H264.NalUnitType.InstantaneousDecoderRefresh) return true;

                byte nalType = type;

                //Check for the SliceType or IDR
                if (Media.Codecs.Video.H264.NalUnitType.IsSlice(ref nalType))
                {
                    //Todo, 
                    //Get type slice type from the slice header.

                    //This logic is also useful for reading the frame number which is needed to determine full or short start codes

                    /* https://code.mythtv.org/doxygen/H264Parser_8cpp_source.html
                    slice_type specifies the coding type of the slice according to
                    Table 7-6.   e.g. P, B, I, SP, SI
 
                    When nal_unit_type is equal to 5 (IDR picture), slice_type shall
                    be equal to 2, 4, 7, or 9 (I or SI)
                    */

                    //Should come from the payload

                    //FirstMbInSLice
                    //SliceType
                    //Pps ID

                    byte sliceType = nalType; // = get_ue_golomb_31(gb);

                    if (Media.Codecs.Video.H264.SliceType.IsIntra(ref sliceType)) return true;
                }
            }

            return false;
        }
    }
}