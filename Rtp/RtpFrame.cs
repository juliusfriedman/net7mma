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

using System;
using System.Collections.Generic;
using System.Linq;

namespace Media.Rtp
{
    /// <summary>
    /// A collection of RtpPackets
    /// </summary>
    public class RtpFrame : Media.Common.BaseDisposable, IEnumerable<RtpPacket>// IDictionary, IList, etc? IClonable
    {
        //Todo, should be Lifetime Disposable        (Where Lifetime is given by expected duration + connection time by default or 1 Minute)

        #region Static

        //Todo, could just as well be an extension to RtpFrame.
        //This also will appear in derived types for no reason.
        /// <summary>
        /// Assembles a single packet by skipping any ContributingSourceListOctets and optionally Extensions and a certain profile header. 
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="useExtensions"></param>
        /// <param name="profileHeaderSize"></param>
        /// <returns></returns>
        public static Common.MemorySegment AssemblePacket(RtpPacket packet, bool useExtensions = false, int profileHeaderSize = 0)
        {
            //Set to profileHeaderSize
            int localSize = profileHeaderSize;

            //Should be handled by derived implementation because it is not known if the flags are relevent to the data or how.
            if (packet.Extension)
            {
                //Use the extension
                using (RtpExtension extension = packet.GetExtension())
                {
                    //If present and complete
                    if (extension != null && extension.IsComplete)
                    {
                        //If the data should be included then include it
                        if (useExtensions)
                        {
                            localSize += packet.ContributingSourceListOctets + RtpExtension.MinimumSize;

                            return new Common.MemorySegment(packet.Payload.Array,
                                    packet.Payload.Offset + localSize,
                                    (packet.Payload.Count - localSize) - packet.PaddingOctets);
                        }

                        //Add the size to the localSize
                        localSize += extension.Size;
                    }
                }
            }

            //Include any csrc's prsent
            localSize += packet.ContributingSourceListOctets;

            return new Common.MemorySegment(packet.Payload.Array,
                packet.Payload.Offset + localSize,
                (packet.Payload.Count - localSize) - packet.PaddingOctets);
        }

        //Packetize static overload for byte[] given a payloadType and timestamp etc.

        #endregion

        #region Readonly

        //readonly ValueType
        /// <summary>
        /// The DateTime in which the instance was created.
        /// </summary>
        public readonly DateTime Created;

        //Should be readonly but would require parameter in constructor.

        /// <summary>
        /// The maximum amount of packets which can be contained in a frame
        /// </summary>
        internal protected int MaxPackets = 1024;

        //must handle in Depacketize
        //readonly ValueType
        /// <summary>
        /// Indicates if Add operations will ensure that all packets added have the same <see cref="RtpPacket.PayloadType"/>
        /// </summary>
        public readonly bool AllowsMultiplePayloadTypes;

        //must handle in Depacketize
        //readonly ValueType
        /// <summary>
        /// Indicates if duplicate packets will be allowed to be stored.
        /// </summary>
        public readonly bool AllowDuplicatePackets;

        //must handle in Depacketize
        //readonly ValueType
        /// <summary>
        /// Indicates if multiple marker packets will be allowed to be stored.
        /// </summary>
        public readonly bool AllowMultipleMarkerPackets;

        /// <summary>
        /// Updating the SequenceNumber of a contained packet can still cause unintended results.
        /// </summary>
        internal readonly protected List<RtpPacket> Packets;

        //Could use List if Add is replaced with Insert and index given by something like => Abs(Clamp(n, 0, Min(n - count) + Max(n - count))) or IndexOf(n)
        /// <summary>
        /// After a single RtpPacket is <see cref="Depacketize">depacketized</see> it will be placed into this list with the appropriate index.
        /// </summary>
        internal readonly SortedList<int, Common.MemorySegment> Depacketized;

        #endregion

        #region Fields        

        /// <summary>
        /// Timestamp, SynchronizationSourceIdentifier of all contained packets.
        /// </summary>
        internal int m_Timestamp = -1, m_Ssrc = -1;

        /// <summary>
        /// The PayloadType of all contained packets, If the Marker packet was added then the value is > 127, if the value has not been determined -1.
        /// </summary>
        internal int m_PayloadType = -1; //0x80;
        //When more than one marker packet is contained the count should be stored in the after the 8 bits related to the payload type (but then a function to get the markers packets would still have to search the offsets)
        //(0, 1) = Undefined PayloadType and reserved, (2) = Count of Markers,(3) = PayloadType
        
        //Marker index, Offset into Packets
        //0, 1
        //1, 3
        //2, 7
        //3, 9
        //Dictionary<int, int> MarkerPackets

        //public int MarkerCount => MarkerPackets.Count

        /// <summary>
        /// The Lowest and Highest SequenceNumber in the contained RtpPackets or -1 if no RtpPackets are contained
        /// </summary>
        internal int m_LowestSequenceNumber = -1, m_HighestSequenceNumber = -1;

        /// <summary>
        /// Useful for depacketization
        /// </summary>
        internal protected System.IO.MemoryStream m_Buffer;

        #endregion

        #region Properties

        //Could indicate if any have extensions

        /// <summary>
        /// Gets a value indicating if the <see cref="PayloadType"/> was specified.
        /// </summary>
        public bool SpecifiedPayloadType { get { return m_PayloadType >= 0; } }

        /// <summary>
        /// Gets the expected PayloadType of all contained packets or -1 if has not <see cref="SpecifiedPayloadType"/>
        /// </summary>
        public int PayloadType
        {
                //Callers should check DeterminedPayload first
            get { return /*m_PayloadType == -1 ? -1 : */m_PayloadType & Common.Binary.SevenBitMaxValue; }
            //internal protected set
            //{
            //    //When the value is less than 0 this means clear...
            //    if (value < 0)
            //    {
            //        m_PayloadType = -1;
                    
            //        Clear();

            //        return;
            //    }

            //                                                                      //value &= Common.Binary.SevenBitMaxValue;

            //    //Set all packets to the value (validates the value)
            //    foreach (RtpPacket packet in Packets) packet.PayloadType = value; //packet.Header.First16Bits.Last8Bits = (byte)value;

            //    //Set the byte depending on if the marker was previous set.
            //    m_PayloadType = (byte)(HasMarker ? value | RFC3550.CommonHeaderBits.RtpMarkerMask : value);
            //}
        }


        //SourceList which should be added to each packet int the frame?

        /// <summary>
        /// Indicates if there are any packets have been <see cref="Depacketize">depacketized</see>
        /// </summary>
        public bool HasDepacketized { get { return Depacketized.Count > 0; } }

        /// <summary>
        /// Indicates if the <see cref="Buffer"/> is not null.
        /// </summary>
        public bool HasBuffer { get { return false == (m_Buffer == null); } }

        //Public means this can be disposed. virtual is not necessary
        public System.IO.MemoryStream Buffer
        {
            //There may be multiple repeated calls to this property due to the way it was used previously.
            get
            {
                //If the buffer was not already prepared then Prepare it.
                if (m_Buffer == null)
                {
                    //Can only be prepared when Depacketized
                    if (false == HasDepacketized) return null;

                    //Prepare the buffer
                    PrepareBuffer();
                }

                //Return it
                return m_Buffer;
            }
            //get
            //{
            //    return new System.IO.MemoryStream(m_Buffer.GetBuffer(), 0, (int)m_Buffer.Length, true, true);
            //}
        }


        /// <summary>
        /// Gets or sets the SynchronizationSourceIdentifier of All Packets Contained or -1 if not assigned.
        /// </summary>
        public int SynchronizationSourceIdentifier
        {
            get { return m_Ssrc; }
            set
            {
                m_Ssrc = value;

                foreach (RtpPacket p in Packets)
                {
                    p.SynchronizationSourceIdentifier = m_Ssrc;
                }
            }
        }

        /// <summary>
        /// Gets or sets the PayloadType of All Packets Contained or 128 if unassigned.
        /// </summary>
        //public byte PayloadTypeByte
        //{
        //    get { return m_PayloadByte; }
        //    set { m_PayloadByte = value; }
        //}

        /// <summary>
        /// Gets or sets the Timestamp of All Packets Contained or -1 if unassigned.
        /// </summary>
        public int Timestamp
        {
            get { return m_Timestamp; }
            set { m_Timestamp = value; }
        }

        /// <summary>
        /// Gets the packet at the given index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        internal protected RtpPacket this[int index]
        {
            get { return Packets[index]; }
            /*private*/
            set { Packets[index] = value; }
        }

        #region Readonly Properties

        /// <summary>
        /// Indicates if all contained RtpPacket instances have a Transferred Value otherwise false.
        /// </summary>
        public bool Transferred { get { return IsEmpty ? false : Packets.All(p => p.Transferred.HasValue); } }

        //Possible make an Action<bool> which represents the functionality to use here and remove virtual.
        /// <summary>
        /// Indicates if the RtpFrame is NotEmpty AND contained a RtpPacket which has the Marker Bit Set AND is not <see cref="IsMissingPackets"/>
        /// </summary>
        public virtual bool IsComplete
        {
            get { return false == IsDisposed && HasMarker && false == IsMissingPackets; }
        }

        //Todo, for Rtcp feedback one would need the sequence numbers of the missing packets...
        //Would be given any arbitrary RtpFrame and would implement that logic there.

        /// <summary>
        /// Indicates if all contained packets are sequential up the Highest Sequence Number contained in the RtpFrame. (If at least 2 packets are contained)
        /// If only 1 or 2 packets are contained then <see cref="HasMarker"/> is also checked.
        /// </summary>
        public bool IsMissingPackets
        {
            get
            {
                switch (Packets.Count)
                {
                    //No packets
                    case 0: return true;
                    //Single packet, only missing if there is no marker (Should not be checked)?
                    case 1: return false == HasMarker; //Could handle as case 0
                    //Skip the range check for 2 packets but ensure a marker is present.
                    case 2: if (false == ((short)(m_LowestSequenceNumber - m_HighestSequenceNumber) != -1)) return true; goto case 1; //Could skip the goto
                    //2 or more packets, cache the m_LowestSequenceNumber and check all packets to be sequential starting at offset 1
                    default: RtpPacket p; for (int nextSeq = m_LowestSequenceNumber == ushort.MaxValue ? ushort.MinValue : m_LowestSequenceNumber + 1, i = 1, e = Count; i < e; ++i)
                        {
                            //Scope the packet
                            p = Packets[i];

                            //obtain the sequence number to check if the packet is missing
                            if (p.SequenceNumber != nextSeq) return true;

                            //If the differece is not 0 then the packet is missing.
                            //if ((short)(p.SequenceNumber - nextSeq) != 0) return true;

                            //Determine the next sequence number
                            nextSeq = nextSeq == ushort.MaxValue ? ushort.MinValue : nextSeq + 1; //++nextSeq;
                        }

                        //Not missing any packets.
                        return false;
                        
                        //Verify HasMarker
                        //goto case 1;
                }
            }
        }

        //Possible change name to LastPacketIsMarker
        /// <summary>
        /// Indicates if a contained packet has the marker bit set. (Usually the last packet in a frame)
        /// </summary>
        public bool HasMarker
        {
            //get { return Packets.Count == 0 ? false : Packets[Packets.Count - 1].Marker; }
            //If multiple markers packets are stored this must be enforced here also
            //MarkerPackets.Count > 0
            get { return /*Packets.Count == 0 ? false : */ m_PayloadType > 127; }
        }

        /// <summary>
        /// The amount of Packets in the RtpFrame
        /// </summary>
        public int Count { get { return Packets.Count; } }

        /// <summary>
        /// Indicates if there are packets in the RtpFrame
        /// </summary>
        public bool IsEmpty { get { return Packets.Count == 0; } }
        
        /// <summary>
        /// Gets the 16 bit unsigned value which is associated with the highest sequence number contained or -1 if no RtpPackets are contained.
        /// Usually the packet at the highest offset
        /// </summary>
        public int HighestSequenceNumber { get { return m_HighestSequenceNumber; } }

        /// <summary>
        /// Gets the 16 bit unsigned value which is associated with the lowest sequence number contained or -1 if no RtpPackets are contained.
        /// Usually the packet at the lowest offset
        /// </summary>
        public int LowestSequenceNumber { get { return m_LowestSequenceNumber; } }

        #endregion

        #endregion

        #region Constructor

        /// <summary>
        /// Creates an instance which has no packets and an undetermined <see cref="PayloadType"/>
        /// </summary>
        /// <param name="shouldDispose">Indicates if the instance will <see cref="Clear"/> when <see cref="Dispose"/> is called.</param>
        public RtpFrame(bool shouldDispose)
            : base(shouldDispose)
        {
            //Indicate when this instance was created
            Created = DateTime.UtcNow;

            //Create the list
            Packets = new List<RtpPacket>();

            //Create the list
            Depacketized = new SortedList<int, Common.MemorySegment>();
        }

        /// <summary>
        /// Creates an instance which has no packets and an undetermined <see cref="PayloadType"/> and will dispose when <see cref="Dispose"/> is called.
        /// </summary>
        public RtpFrame() : this(true) { }

        /// <summary>
        /// Creates an instance which has no packets and and the given <see cref="PayloadType"/> and will dispose when <see cref="Dispose"/> is called.
        /// </summary>
        /// <param name="payloadType"></param>
        public RtpFrame(int payloadType) : this()
        {
            //Should be bound from 0 - 127 inclusive...
            if (payloadType > byte.MaxValue) throw Common.Binary.CreateOverflowException("payloadType", payloadType, byte.MinValue.ToString(), byte.MaxValue.ToString());

            //Assign the type of RtpFrame
            m_PayloadType = (byte)payloadType;
        }

        /// <summary>
        /// Creates an instance which has no packets and and the given <see cref="PayloadType"/>, <see cref="Timestamp"/> and <see cref="SynchronizationSourceIdentifier"/> and will dispose when <see cref="Dispose"/> is called.
        /// </summary>
        /// <param name="payloadType"></param>
        /// <param name="timeStamp"></param>
        /// <param name="ssrc"></param>
        public RtpFrame(int payloadType, int timeStamp, int ssrc) 
            :this(payloadType)
        {
            //Assign the Synconrization Source Identifier
            m_Ssrc = ssrc;

            //Assign the Timestamp
            m_Timestamp = timeStamp;    
        }

        //Todo, additional byte[] overloads to packetize data with

        //Could also provide delegated actions for use with logic to prevent having to subclass but type safetype is sacraficed somewhat as 
        //The semantics of each instance cannot easily be traced without knowing what to expect from each delegation
        //This requires the use of interfaces to properly 'do'.

        /// <summary>
        /// Creates an instance and if the packet is not null assigns properties from the given packet and optionally adds the packet to the list of stored packets.
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="addPacket"></param>
        /// <param name="shouldDispose"></param>
        public RtpFrame(RtpPacket packet, bool addPacket = true, bool shouldDispose = true)
            :this(shouldDispose)
        {
            if(Common.IDisposedExtensions.IsNullOrDisposed(packet)) return;

            m_PayloadType = packet.PayloadType;
            
            m_Timestamp = packet.Timestamp;

            m_Ssrc = packet.SynchronizationSourceIdentifier;

            if(addPacket) Add(packet);
        }

        /// <summary>
        /// Clone and existing RtpFrame
        /// </summary>
        /// <param name="f">The frame to clonse</param>
        /// <param name="referencePackets">Indicate if contained packets should be referenced</param>
        public RtpFrame(RtpFrame f, bool referencePackets = false, bool referenceBuffer = false, bool shouldDispose = true)
            : base(shouldDispose) //If shouldDispose is true when referencePackets is true then Dispose will clear both lists.
        {
            if (Common.IDisposedExtensions.IsNullOrDisposed(f)) return;

            m_PayloadType = f.m_PayloadType;

            m_Ssrc = f.m_Ssrc; 
            
            m_Timestamp = f.m_Timestamp;

            //If this is a shallow clone then just use the reference
            if (referencePackets) Packets = f.Packets; //Assign the list from the packets in te frame (changes to list reflected in both instances)
            else Packets = new List<RtpPacket>(f); //Create the list from the packets in the frame (changes to list not reflected in both instances)

            //It should be that...
            //If you reference the packets you also reference the buffer...

            //If the buffer is referenced
            if (referenceBuffer)
            {
                //Assign it
                m_Buffer = f.m_Buffer;

                //The depacketized must also be then..
                Depacketized = f.Depacketized;
            }
            else
            {
                //Create the list
                Depacketized = new SortedList<int, Common.MemorySegment>();
                
                //Can't create a new one because of the implications
                m_Buffer = f.Buffer; 
            }

            /// See notes and determine if this is appropraite behavior
            //ShouldDispose = f.ShouldDispose;
        }

        ///// <summary>
        ///// Destructor.
        ///// </summary>
        //~RtpFrame() { Dispose(); } 

        #endregion

        #region Methods

        //Should provide a virtual Packetize method ...

        //Should provide own logic and not throw for new or removed packets? (if packets are added or removed this logic is interrupted)
        /// <summary>
        /// Gets an enumerator of All Contained Packets at the time of the call
        /// </summary>
        /// <returns>The enumerator of the contained packets</returns>
        public IEnumerator<RtpPacket> GetEnumerator() { return Packets.GetEnumerator(); }

        //public IEnumerator<Common.MemorySegment> GetEnumerator() { return Depacketized.Values.GetEnumerator(); }

        /// <summary>
        /// Adds a RtpPacket to the RtpFrame. The first packet added sets the SynchronizationSourceIdentifier and Timestamp if not already set.
        /// </summary>
        /// <param name="packet">The RtpPacket to Add</param>
        /// <param name="allowPacketsAfterMarker">Indicates if the packet shouldbe allowed even if the packet's sequence number is greater than or equal to <see cref="HighestSequenceNumber"/> and <see cref="IsComplete"/> is true.</param>
        public void Add(RtpPacket packet, bool allowPacketsAfterMarker = true, bool allowDuplicates = false)
        {
            if (Common.IDisposedExtensions.IsNullOrDisposed(packet)) return;

            int count = Count, ssrc = packet.SynchronizationSourceIdentifier, seq = packet.SequenceNumber, ts = packet.Timestamp, pt = packet.PayloadType;

            //No packets contained yet
            if (count == 0)
            {
                if (m_Ssrc == -1) m_Ssrc = ssrc;
                else if (ssrc != m_Ssrc) throw new ArgumentException("packet.SynchronizationSourceIdentifier must match frame SynchronizationSourceIdentifier", "packet");

                if (m_Timestamp == -1) m_Timestamp = ts;
                else if (ts != m_Timestamp) throw new ArgumentException("packet.Timestamp must match frame Timestamp", "packet");

                if (m_PayloadType == -1) m_PayloadType = pt;
                else if (AllowsMultiplePayloadTypes == false && pt != PayloadType) throw new ArgumentException("packet.PayloadType must match frame PayloadType", "packet");

                m_LowestSequenceNumber = m_HighestSequenceNumber = seq;

                Packets.Add(packet);

                //Set marker bit in m_PayloadTypeByte (Todo, if AllowMultipleMarkers this needs to be counted)
                if (packet.Marker) m_PayloadType |= RFC3550.CommonHeaderBits.RtpMarkerMask;

                return;
            }
            else //At least 1 packet is contained
            {
                //Check payload type if indicated
                if (AllowsMultiplePayloadTypes == false && pt != PayloadType) throw new ArgumentException("packet.PayloadType must match frame PayloadType", "packet");

                if (ssrc != m_Ssrc) throw new ArgumentException("packet.SynchronizationSourceIdentifier must match frame SynchronizationSourceIdentifier", "packet");

                if (ts != m_Timestamp) throw new ArgumentException("packet.Timestamp must match frame Timestamp", "packet");

                if (count >= MaxPackets) throw new InvalidOperationException(string.Format("The amount of packets contained in a RtpFrame cannot exceed: {0}", MaxPackets));
            }

            //Check for existing marker packet
            bool hasMarker = HasMarker;

            //Determine if the packet has a marker
            bool packetMarker = packet.Marker;

            //If the packet has the marker bit set
            if (packetMarker)
            {
                //If there was already a marker packet in the frame then this is an exception
                if (false == allowPacketsAfterMarker && hasMarker) throw new InvalidOperationException("Cannot have more than one marker packet in the same RtpFrame.");

                //Marker packet must always be last
                Packets.Add(packet);

                //It is the highest sequence number
                m_HighestSequenceNumber = seq;

                //Set marker bit in m_PayloadTypeByte (Todo, if AllowMultipleMarkers this needs to be counted)
                m_PayloadType |= RFC3550.CommonHeaderBits.RtpMarkerMask;

                return;
            }
            
            //Determine where to insert and what seq will be inserted
            int insert = 0, tempSeq = 0;

            //Search for insert point while the index < count and while roll over would not occur
            while (insert < count && (short)(seq - (tempSeq = Packets[insert].SequenceNumber)) >= 0)
            {
                //move the index
                ++insert;
            }

            //Ensure not a duplicate
            if (false == allowDuplicates && tempSeq == seq) throw new InvalidOperationException("Cannot have duplicate packets in the same frame.");

            //Handle prepend
            if (insert == 0)
            {
                Packets.Insert(0, packet);

                m_LowestSequenceNumber = seq;
            }
            else if (insert >= count) //Handle add
            {
                if (false == allowPacketsAfterMarker && hasMarker) throw new InvalidOperationException("Cannot add packets after the marker packet.");

                Packets.Add(packet);

                m_HighestSequenceNumber = seq;
            }
            else Packets.Insert(insert, packet); //Insert
        }

        /// <summary>
        /// Calls <see cref="Add"/> and indicates if the operations was a success
        /// </summary>
        public bool TryAdd(RtpPacket packet, bool allowPacketsAfterMarker = true, bool allowDuplicates = false)
        {
            try { Add(packet, allowPacketsAfterMarker, allowDuplicates); return true; }
            catch { return false; }            
        }

        //TryAddOrUpdate

        //Update

        /// <summary>
        /// Indicates if the RtpFrame contains a RtpPacket
        /// </summary>
        /// <param name="packet">The RtpPacket to check</param>
        /// <returns>True if the packet is contained, otherwise false.</returns>
        public bool Contains(RtpPacket packet) { return Packets.Contains(packet); }

        /// <summary>
        /// Indicates if the RtpFrame contains a RtpPacket
        /// </summary>
        /// <param name="sequenceNumber">The RtpPacket to check</param>
        /// <returns>True if the packet is contained, otherwise false.</returns>
        public bool Contains(int sequenceNumber) { return IndexOf(sequenceNumber) >= 0; }

        /// <summary>
        /// Indicates if the RtpFrame contains a RtpPacket based on the given sequence number.
        /// </summary>
        /// <param name="sequenceNumber">The sequence number to check</param>
        /// <returns>The index of the packet is contained, otherwise -1.</returns>
        internal protected int IndexOf(int sequenceNumber)
        {
            int count = Count;
            switch (count)
            {
                case 0: return -1;
                case 1:
                    {
                        return m_HighestSequenceNumber == sequenceNumber ? 0 : -1;
                    }
                case 2:
                    {
                        if (m_LowestSequenceNumber == sequenceNumber) return 0;

                        return m_HighestSequenceNumber == sequenceNumber ? 1 : -1;
                    }
                //Only optimal if sequenceNumber is @ 1 otherwise default cases may be faster, this saves 2 additional range changes
                case 3:
                    {
                        if (Packets[1].SequenceNumber == sequenceNumber) return 1;

                        goto case 2;
                    }
                    //Still really only just saves 2 checks, still not optimals
                //case 4:
                //    {
                //        if (Packets[2].SequenceNumber == sequenceNumber) return 2;

                //        goto case 3;
                //    }
                //case 5:
                //    {
                //        if (Packets[3].SequenceNumber == sequenceNumber) return 3;

                //        goto case 4;
                //    }
                default:
                    {
                        //Fast path when no roll over occur, e.g. m_Packets[0].SequenceNumber > m_Packets.Last().SequenceNumber
                        //if (m_HighestSequenceNumber > m_LowestSequenceNumber && (sequenceNumber <= m_HighestSequenceNumber && sequenceNumber >= m_LowestSequenceNumber)) return true;

                        RtpPacket p;

                                    //Not really necessary to Max, could just start at 0, but this potentially saves some array access
                        for (int i = Common.Binary.Max(0, sequenceNumber - m_LowestSequenceNumber), e = count; i < e; ++i)
                        {
                            p = Packets[i];

                            if (p.SequenceNumber == sequenceNumber) return i; // i

                            p = Packets[--e];

                            if (p.SequenceNumber == sequenceNumber) return e; // e
                        }

                        return -1;
                    }
            }
        }

        //bool Remove(int seq, out RtpPacket packet)
        //bool Remove(int seq, out RtpPacket packet, out int index)

        /// <summary>
        /// Removes a RtpPacket from the RtpFrame by the given Sequence Number.
        /// </summary>
        /// <param name="sequenceNumber">The sequence number of the RtpPacket to remove</param>
        /// <returns>A RtpPacket with the sequence number if removed, otherwise null</returns>
        public RtpPacket Remove(int sequenceNumber)
        {
            int count = Count;

            if (count == 0) return null;

            int i = IndexOf(sequenceNumber);

            if (i < 0) return null;

            //Get the packet
            RtpPacket p = Packets[i];

            //if (p.SequenceNumber != sequenceNumber) throw new Exception();

            //Remove it
            Packets.RemoveAt(i);

            //Determine if the sequence number effects the lowest or highest fields
            switch (--count)
            {
                case 0: m_LowestSequenceNumber = m_HighestSequenceNumber = -1; goto CheckMarker;
                    //Only 1 packet remains, saves a count - 1 and an array access.
                case 1:
                    {
                        //m_LowestSequenceNumber = m_HighestSequenceNumber = Packets[0].SequenceNumber; 
                        
                        //If this was at 0 then remap 0 to 1
                        if (sequenceNumber == m_LowestSequenceNumber) m_LowestSequenceNumber = m_HighestSequenceNumber;

                        //Remap 1 to 0
                        m_HighestSequenceNumber = m_LowestSequenceNumber;

                        goto CheckMarker;

                    }
                //only 2 packets is really default also but this saves a count - 1 instruction
                    //It also saves one access to the array when possible.
                case 2:
                    {
                        switch (i)
                        {
                            case 0://(sequenceNumber == m_LowestSequenceNumber)
                                {
                                    m_LowestSequenceNumber = Packets[0].SequenceNumber;
                                    break;
                                }
                            case 1: break; //Index 1 when there was 3 packets cannot effect the lowest or highest but may have a marker if multiple marker packets are stored.
                            case 2://(sequenceNumber == m_HighestSequenceNumber)
                                {
                                    m_HighestSequenceNumber = Packets[1].SequenceNumber;
                                    break;
                                }
                        }

                        goto CheckMarker;
                    }
                default:
                    {
                        //Skip the access of the array for all cases but when the sequence was == to the m_LowestSequenceNumber (i == 0)
                        if (sequenceNumber == m_LowestSequenceNumber)
                        {
                            m_LowestSequenceNumber = Packets[0].SequenceNumber; //First
                        }
                        else if (sequenceNumber == m_HighestSequenceNumber)//Otherise if was == to the m_HighestSequenceNumber (i >= count)
                        {
                            m_HighestSequenceNumber = Packets[count - 1].SequenceNumber; //Last
                        }

                        goto CheckMarker;
                    }
            }

        CheckMarker:
            //Check for marker when i >= count and unset marker bit if present. (Todo, if AllowMultipleMarkers this needs to be counted)
            if (i >= count && p.Marker) m_PayloadType &= Common.Binary.SevenBitMaxValue;

            //Just remove
            //MarkerPackets.Remove(i);

            return p;            //Notes, i contains the offset where p was stored.
        }

        
        ////More realistically this doesn't need to be done at all.
        ////Once a packet is removed it's removed, if the buffer is not diposed who cares?
        ////Who can access RemoveAt anyway? the protected or derived members...
        ////No one else is calling RemoveAt, not even this implementation.
        ////Inline
        ////disposeBuffer could determined by ShouldDispose or more correclty a readonly field which derived types can easily set in the construtor.
        //internal protected virtual void RemoveAt(int index, bool disposeBuffer = false)
        //{
        //    Packets.RemoveAt(index);

        //    if (disposeBuffer) DisposeBuffer();
        //}

        /// <summary>
        /// Empties the RtpFrame by clearing the underlying List of contained RtpPackets
        /// </summary>
        internal protected void RemoveAllPackets() //bool disposeBuffer
        {
            Packets.Clear();

            Depacketized.Clear();

            m_HighestSequenceNumber = m_LowestSequenceNumber = -1;
        }

        /// <summary>
        /// Disposes all contained packets.
        /// Disposes the buffer
        /// Clears the contained packets.
        /// </summary>
        public void Clear()
        {
            //Different than most collections
            DisposeAllPackets();

            //Disposes the buffer also
            DisposeBuffer();

            //Finally clears the collection and resets sequence numbers
            RemoveAllPackets(); 
        }

        //(if packets are added or removed this logic is interrupted)
        /// <summary>
        /// Disposes all contained packets. 
        /// </summary>
        internal protected void DisposeAllPackets()
        {
            //Dispose all packets...
            foreach (RtpPacket p in Packets) p.Dispose();
        }

        //Notes, Assemble terminology is backwards, should be Disassemble
        //This also has no place in the API unless forcefully made up, e.g. ProcessPacket could be Assemble

        //The differences currently are related to the types of return which are hard to maintain and understand
        //Assemble a packet means to take a rtp packet and get the data which is needed for the decoder
        //sometimes the extensions are needed, most of the time there is only the need to skip the csrc list if present

        /// <summary>
        /// Calls <see cref="RtpFrame.AssemblePacket"/>
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="useExtensions"></param>
        /// <param name="profileHeaderSize"></param>
        /// <returns></returns>
        public virtual Common.MemorySegment Assemble(RtpPacket packet, bool useExtensions = false, int profileHeaderSize = 0)
        {
            return RtpFrame.AssemblePacket(packet, useExtensions, profileHeaderSize);
        }

        /// <summary>
        /// Assembles the RtpFrame into a IEnumerable by use of concatenation, the ExtensionBytes and Payload of all contained RtpPackets into a single sequence (excluding the RtpHeader)
        /// <see cref="RtpFrame.AssemblePacket"/>
        /// </summary>
        /// <returns>The byte array containing the assembled frame</returns>
        public IEnumerable<byte> Assemble(bool useExtensions = false, int profileHeaderSize = 0)
        {
            //The result
            IEnumerable<byte> sequence = Common.MemorySegment.Empty;

            //Iterate the packets (if packets are added or removed this logic is interrupted)
                                                            //Use the static functionality by default RtpFrame.AssemblePacket(packet, useExtensions, profileHeaderSize)
            foreach (RtpPacket packet in Packets) sequence = sequence.Concat(Assemble(packet, useExtensions, profileHeaderSize));
                 

            //Return the result
            return sequence;
        }

        //Todo, virtual here increases complexity and overhead.

        /// <summary>
        /// Depacketizes all contained packets ignoring <see cref="IsComplete"/>.
        /// </summary>
        public void Depacketize() { Depacketize(true); }

        //Same here but allows dervived types to specify

        /// <summary>
        /// Depacketizes all contained packets if possible.
        /// </summary>
        /// <param name="allowIncomplete">Determines if <see cref="IsComplete"/> must be true</param>
        public virtual void Depacketize(bool allowIncomplete) //bool needMarker, int markerCountNeeded
        {
            //May allow incomplete packets.
            if (false == allowIncomplete && false == IsComplete) return;

            foreach (RtpPacket packet in Packets) Depacketize(packet);

            //PrepareBuffer must be called to access the buffer.
        }
        
        //Virtual so dervied types can call their Depacketize method with any options they may require

        /// <summary>
        /// Depacketizes a single packet
        /// </summary>
        /// <param name="packet"></param>
        public virtual void Depacketize(RtpPacket packet)
        {
            if (Common.IDisposedExtensions.IsNullOrDisposed(packet)) return;

            //Add the data of the packet which is at the end of the extension and csrc until the padding.
            
            //Must / should also be able to determine if packet was previously depacketized.
                            //packet.SequenceNumber

            //This will fail if packets are added out of order, a standard function to generate the key should be used.
            //(packet.Timestamp - packet.SequenceNumber,
                                                 //Assemble(packet, false, 0));
            Depacketized.Add(Depacketized.Count, packet.PayloadDataSegment);//new Common.MemorySegment(packet.Payload.Array, (packet.Payload.Offset + headerOctets), packet.Payload.Count - (headerOctets + packet.PaddingOctets)));

            

            //Write the packet payload starting at the end of the extension and csrc until the padding
            //Buffer.Write(packet.Payload.Array, packet.Payload.Offset + headerOctets, packet.Payload.Count - (headerOctets + packet.PaddingOctets));
        }

        /// <summary>
        /// Takes all depacketized segments and writes them to the buffer.
        /// </summary>
        internal protected void PrepareBuffer() //action pre pre write, post write
        {
            //Ensure there is something to write to the buffer
            if (false == HasDepacketized) return;

            //If already exists then dispose
            DisposeBuffer();

            //Create a new buffer
            m_Buffer = new System.IO.MemoryStream();

            //Iterate ordered segments
            foreach (KeyValuePair<int, Common.MemorySegment> pair in Depacketized)
            {
                //Get the segment
                Common.MemorySegment value = pair.Value;

                //Write it to the Buffer
                Buffer.Write(value.Array, value.Offset, value.Count);
            }

            //Ensure at the begining
            m_Buffer.Seek(0, System.IO.SeekOrigin.Begin);
        }

        /// <summary>
        /// virtual so it's easy to keep the same API, not really needed though since Dispose is also overridable.
        /// </summary>
        internal virtual protected void DisposeBuffer()
        {
            if (m_Buffer != null)
            {
                m_Buffer.Dispose();

                m_Buffer = null;
            }
        }

        #endregion

        //Operators... and overloads

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override void Dispose()
        {
            base.Dispose();

            if (ShouldDispose)
            {
                Clear();
            }
        }

        //Registration ....
        //Otherwise there is no standard code for obtaining a Derived type besides using IsSubClassOf.
        //Then would still be unable to tell what profile it supports if using Dynamic..

    }

    #region Todo IRtpFrame

    //Where this is probably going
    //Would be something like the facade pattern.
    //public interface IRtpFrame
    //{

    //}

    #endregion

    #region To be implemented via RtpFrame

    //Doesn't need to inerhit but can although it looks weird.
    //Could just compose the frame in a member and add all the stuff related to packetization here.
    //RFC3550 could define a Framing (DePacketization) (Packetizer) which used the assemble methodology.
    //Would also be aware of various SDP parameters and PayloadType implementation..
    //public class Framing //Depacketization //Packetization //Packetizer //: RtpFrame
    //{
    //    #region Depacketization

    //    //Could be out or Ref on  Depacketize.
    //    internal protected RtpFrame Frame;

    //    //public virtual bool HasVariableSizeHeader { get; protected set; }

    //    //public virtual int HeaderSize {get; protected set; 

    //    //public virtual bool UsesExtensions { get; protected set; }

    //    public virtual int GetPacketKey(RtpPacket packet) { return packet.Timestamp - packet.SequenceNumber; }

    //    //public Func<RtpPacket, int> KeyGenerator;

    //    //delegate int KeyGenerator(RtpPacket packet);

    //    //public Func<bool> HasMarkerLogic;

    //    //delegate bool HasMarkerDelegate();

    //    //Could be out of ref on Depacketize
    //    //SortedMemory(should be keyed by sequence number by default but would require a Ushort comparer)
    //    internal protected SortedList<int, Common.MemorySegment> Segments; //Or Packetized // Packets

    //    //not needed if not stored.
    //    /// <summary>
    //    /// Indicates if there are any segments allocated
    //    /// </summary>
    //    public bool HasSegments { get { return Segments.Count > 0; } }

    //    /// <summary>
    //    /// Creates a MemoryStream from the <see cref="Segments"/> of data depacketized.
    //    /// </summary>
    //    /// <returns></returns>
    //    public System.IO.MemoryStream PrepareBuffer()
    //    {
    //        System.IO.MemoryStream result = new System.IO.MemoryStream(Segments.Values.Sum(d => d.Count));

    //        foreach (var pair in Segments)
    //        {
    //            Common.MemorySegment value = pair.Value;

    //            result.Write(value.Array, value.Offset, value.Count);
    //        }

    //        return result;
    //    }

    //    /// <summary>
    //    /// Depacketize's the frame with the default options.
    //    /// </summary>
    //    public virtual void Depacketize() { Depacketize(true); } //RtpFrame frame (what), SortedList<int, Common.MemorySegment> Segments (where)

    //    /// <summary>
    //    /// Depacketizes the payload segment of Frame
    //    /// </summary>
    //    public void Depacketize(bool allowIncomplete) //RtpFrame Frame (what), SortedList<int, Common.MemorySegment> Segments (where)
    //    {
    //        //Ensure the Frame is not null
    //        if (Common.IDisposedExtensions.IsNullOrDisposed(Frame)) return;

    //        //Check IsComplete if required
    //        if (false == allowIncomplete && false == Frame.IsComplete) return;

    //        //Iterate packet's in Frame
    //        foreach (RtpPacket packet in Frame)
    //        {
    //            //Depacketize the packet
    //            Depacketize(packet);
    //        }
    //    }

    //    /// <summary>
    //    /// Calls <see cref="Depacketize"/> in parallel
    //    /// </summary>
    //    /// <param name="allowIncomplete"></param>
    //    public void ParallelDepacketize(bool allowIncomplete) //RtpFrame frame (what), SortedList<int, Common.MemorySegment> Segments (where)
    //    {
    //        //Ensure the Frame is not null
    //        if (Common.IDisposedExtensions.IsNullOrDisposed(Frame)) return;

    //        //Check IsComplete if required
    //        if (false == allowIncomplete && false == Frame.IsComplete) return;

    //        //In parallel Depacketize each packet.
    //        ParallelEnumerable.ForAll(Frame.AsParallel(), Depacketize);
    //    }

    //    /// <summary>
    //    /// Depacketize a single packet
    //    /// </summary>
    //    /// <param name="packet"></param>
    //    public virtual void Depacketize(RtpPacket packet) //SortedList<int, Common.MemorySegment> Segments (where)
    //    {
    //        //Calulcate the key
    //        int key = GetPacketKey(packet);

    //        //Save for previously Depacketized packets
    //        if (Segments.ContainsKey(key)) return;

    //        //Add the data in the PayloadDataSegment.
    //        Segments.Add(key, packet.PayloadDataSegment);
    //    }

    //    #endregion

    //    #region Packetization

    //    /// <summary>
    //    /// Packetizes the sourceData to a RtpFrame.
    //    /// </summary>
    //    public virtual RtpFrame Packetize(byte[] sourceData, int offset, int count, int bytesPerPayload, int sequenceNumber, int timeStamp, int ssrc, int payloadType, bool setMarker = true)
    //    {
    //        RtpFrame result = new RtpFrame();

    //        bool marker = false;

    //        while (count > 0)
    //        {
    //            //Subtract for consumed bytes and compare to bytesPerPayload
    //            if ((count -= bytesPerPayload) <= bytesPerPayload)
    //            {
    //                bytesPerPayload = count;

    //                marker = setMarker;
    //            }

    //            //Move the offset
    //            offset += bytesPerPayload;

    //            //Add the packet created from the sourceData at the offset, increase the sequence number.
    //            result.Add(new RtpPacket(new RtpHeader(2, false, false, marker, payloadType, 0, ssrc, sequenceNumber++, timeStamp), 
    //                new Common.MemorySegment(sourceData, offset, bytesPerPayload)));
    //        }

    //        return result;
    //    }

    //    #endregion

    //    #region Repacketization

    //    //Should return frame, this would imply that both frames would exist for ashort period of time.
    //    //Otherwise have an inplace option.
    //    /// <summary>
    //    /// Repacketizes the payload segment of Frame according to the given options.
    //    /// </summary>
    //    /// <param name="bytesPerPayload"></param>
    //    public virtual void Repacketize(RtpFrame frame, int bytesPerPayload) // bool inPlace
    //    {
    //        RtpPacket current = null;

    //        foreach (RtpPacket packet in frame)
    //        {
    //            if (packet.Length > bytesPerPayload)
    //            {
    //                //split
    //            }
    //            else
    //            {
    //                //join

    //                if (current == null) current = packet;
    //                else
    //                {

    //                    if (current.Length + packet.Length > bytesPerPayload)
    //                    {
    //                        //split
    //                    }
    //                    else
    //                    {
    //                        //join
    //                    }

    //                }
    //            }
    //        }

    //        //hard to modify frame in place...

    //        //Better to take a new frame and populate and swap and replace.

    //        int currentSize = 0;

    //        for (int i = 0, e = frame.Count; i < e; ++i)
    //        {
    //            current = frame[i];

    //            int currentLength = current.Length;

    //            if (currentSize + currentLength > bytesPerPayload)
    //            {
    //                //Split

    //                //Add a packet with bytesPerPayload from current 

    //                //Add a packet with currentLength - bytesPerPayload

    //                //Increas index 
    //                ++i;
    //            }
    //            else
    //            {
    //                //Remove current (reset for index again)
    //                Frame.Packets.RemoveAt(i--);

    //                //Join
    //                currentSize += current.Length;

    //                //Make a new packet and combine.

    //                continue;
    //            }
    //        }


    //        return;
    //    }

    //    #endregion

    //    //Dispose();
    //}

    #endregion

    #region To be used outside of the RtpClient

    //Useful for holding onto frame for longer than one cycle.
    //Could be used from the application during the FrameChangedEvent when 'final' is set to true.
    //E.g. when final == true, =>
    //Common.BaseDisposable.SetShouldDispose(frame, false, false);
    //JitterBuffer.Add(frame);

    //public class RtpJitterBuffer : Common.BaseDisposable
    //{
    //    //PayloadType, Frames for PayloadType
    //    readonly Common.Collections.Generic.ConcurrentThesaurus<int, RtpFrame> Frames = new Common.Collections.Generic.ConcurrentThesaurus<int, RtpFrame>();

    //    public RtpJitterBuffer(bool shouldDispose) : base(shouldDispose) { }

    //    public void Add(RtpFrame frame) { Add(frame.PayloadType, frame); }

    //    public void Add(int payloadType, RtpFrame frame) { Frames.Add(payloadType, frame); }

    //    public void Clear()
    //    {
    //        //Enumerate an array of contained keys
    //        foreach (int Key in Frames.Keys.ToArray())
    //        {
    //            //Store the frames at the key
    //            IEnumerable<RtpFrame> frames;

    //            //if removed from the ConcurrentThesaurus
    //            if (Frames.Remove(Key, out frames))
    //            {
    //                //Loop the frames contined at the key
    //                foreach (RtpFrame frame in frames)
    //                {
    //                    //Ensure the frame should be disposed.

    //                    //Can't set the property if not derived
    //                    //frame.ShouldDispose = true;

    //                    //Set ShouldDispose through the base class.
    //                    Common.BaseDisposable.SetShouldDispose(frame, true, true);

    //                    //Dispose the frame (already done with above call)
    //                    frame.Dispose();

    //                    //could also just call frame.Clear();
    //                }
    //            }
    //        }
    //    }

    //    public override void Dispose()
    //    {
    //        base.Dispose();

    //        if (ShouldDispose)
    //        {
    //            Clear();
    //        }
    //    }
    //}

    #endregion

    #region Other concepts thought up but not used.

    //Since it does not inherit the frame this does not work very well.
    //Could have MultiMarkerFrame be dervived but Add is not virtual / overloadable.
    //public class MultiPayloadRtpFrame
    //{
    //    internal readonly protected Dictionary<int, RtpFrame> Frames = new Dictionary<int, RtpFrame>();

    //    public bool TryRemove(int payloadType, out RtpFrame frame) { return Common.Extensions.Generic.Dictionary.DictionaryExtensions.TryRemove(Frames, ref payloadType, out frame); }

    //    public bool ContainsPayloadType(int payloadType) { return Frames.ContainsKey(payloadType); }

    //    public bool TryGetFrame(int payloadType, out RtpFrame result) { return Frames.TryGetValue(payloadType, out result); }
    //}

    //public class DynamicRtpFrame : RtpFrame
    //{
    //    //void GetDepacketizer(int payloadType);
    //    //readonly Action<RtpPacket> Depacketize;
    //}

    //public class RtpFrameExtensions
    //{
    //    //public static RtpFrame CreateTypedFrame => Depacketize
    //}

    #endregion

}


namespace Media.UnitTests
{
    /// <summary>
    /// Provides tests which ensure the logic of the RtpFrame class is correct
    /// </summary>
    internal class RtpFrameUnitTests
    {

        //Todo, Randomize (Range class..)
        public void TestAddingRandomPackets()
        {
            //Create a frame
            using (Media.Rtp.RtpFrame frame = new Media.Rtp.RtpFrame(0))
            {
                //1 2 3 4 5

                //Add marker packet with seq = 2
                frame.Add(new Media.Rtp.RtpPacket(2, false, false, Media.Common.MemorySegment.EmptyBytes)
                {
                    SequenceNumber = 2
                });

                if (frame.Count != 1) throw new Exception("Frame must have 1 packet");

                //The frame must be missing packets
                if (false == frame.IsMissingPackets) throw new Exception("Frame is not missing packets");

                //Add marker packet with seq = 4
                frame.Add(new Media.Rtp.RtpPacket(2, false, false, Media.Common.MemorySegment.EmptyBytes)
                {
                    SequenceNumber = 4
                });

                if (frame.Count != 2) throw new Exception("Frame must have 2 packets");

                //The frame must be missing packets
                if (false == frame.IsMissingPackets) throw new Exception("Frame is not missing packets");

                //Add marker packet with seq = 3
                frame.Add(new Media.Rtp.RtpPacket(2, false, false, Media.Common.MemorySegment.EmptyBytes)
                {
                    SequenceNumber = 3
                });

                if (frame.Count != 3) throw new Exception("Frame must have 3 packets");

                //The frame must NOT be missing packets (Unknown 1 is missing)
                if (frame.IsMissingPackets) throw new Exception("Frame is missing packets");

                //Add marker packet with seq = 5
                frame.Add(new Media.Rtp.RtpPacket(2, false, false, Media.Common.MemorySegment.EmptyBytes)
                {
                    SequenceNumber = 5
                });

                //The frame must NOT be missing packets
                if (frame.IsMissingPackets) throw new Exception("Frame is qmissing packets");

                if (frame.Count != 4) throw new Exception("Frame must have 4 packets");

                //Add marker packet with seq = 1
                frame.Add(new Media.Rtp.RtpPacket(2, false, false, Media.Common.MemorySegment.EmptyBytes)
                {
                    SequenceNumber = 1
                });

                if (frame.Count != 5) throw new Exception("Frame must have 5 packets");

                //The frame must NOT be missing packets
                if (frame.IsMissingPackets) throw new Exception("Frame is missing packets");

                /// 6, 7, 8 9, 10Marker

                //Add marker packet with seq = 10 marker
                frame.Add(new Media.Rtp.RtpPacket(2, false, false, Media.Common.MemorySegment.EmptyBytes)
                {
                    SequenceNumber = 10,
                    Marker = true
                });

                if (frame.Count != 6) throw new Exception("Frame must have 6 packets");

                //The frame must have a marker
                if (false == frame.HasMarker) throw new Exception("Frame must have marker");

                //The frame must be missing packets
                if (false == frame.IsMissingPackets) throw new Exception("Frame is not missing packets");

                //Add marker packet with seq = 2
                frame.Add(new Media.Rtp.RtpPacket(2, false, false, Media.Common.MemorySegment.EmptyBytes)
                {
                    SequenceNumber = 7
                });

                if (frame.Count != 7) throw new Exception("Frame must have 7 packets");

                //The frame must have a marker
                if (false == frame.HasMarker) throw new Exception("Frame must have marker");

                //The frame must be missing packets
                if (false == frame.IsMissingPackets) throw new Exception("Frame is not missing packets");

                //Add marker packet with seq = 4
                frame.Add(new Media.Rtp.RtpPacket(2, false, false, Media.Common.MemorySegment.EmptyBytes)
                {
                    SequenceNumber = 9
                });

                if (frame.Count != 8) throw new Exception("Frame must have 8 packets");

                //The frame must have a marker
                if (false == frame.HasMarker) throw new Exception("Frame must have marker");

                //The frame must be missing packets
                if (false == frame.IsMissingPackets) throw new Exception("Frame is not missing packets");

                //Add marker packet with seq = 3
                frame.Add(new Media.Rtp.RtpPacket(2, false, false, Media.Common.MemorySegment.EmptyBytes)
                {
                    SequenceNumber = 8
                });

                if (frame.Count != 9) throw new Exception("Frame must have 9 packets");

                //The frame must have a marker
                if (false == frame.HasMarker) throw new Exception("Frame must have marker");

                //The frame must be missing packets
                if (false == frame.IsMissingPackets) throw new Exception("Frame is not missing packets");

                //Add marker packet with seq = 5
                frame.Add(new Media.Rtp.RtpPacket(2, false, false, Media.Common.MemorySegment.EmptyBytes)
                {
                    SequenceNumber = 6
                });

                //Verify count
                if (frame.Count != 10) throw new Exception("Frame must have 10 packets");

                //The frame must have a marker
                if (false == frame.HasMarker) throw new Exception("Frame must have marker");

                //The frame must be missing packets
                if (frame.IsMissingPackets) throw new Exception("Frame should not be missing packets");

                //Write out
                //Console.WriteLine(string.Join(",", frame.Select(p => p.SequenceNumber).ToArray()));

                //Verify the order
                for (int i = 0; i < 10; ++i)
                {
                    if (frame.Packets[i].SequenceNumber != i + 1) throw new Exception("Invalid order");
                }

                //The frame cannot be missing packets.
                if (frame.IsMissingPackets) throw new Exception("Frame is missing packets");
            }
        }

        public void TestAddingAndRemovingPackets()
        {
            //Create a frame
            using (Media.Rtp.RtpFrame frame = new Media.Rtp.RtpFrame(0))
            {

                //Add marker packet with seq = 0
                frame.Add(new Media.Rtp.RtpPacket(2, false, false, Media.Common.MemorySegment.EmptyBytes)
                   {
                       SequenceNumber = ushort.MinValue,
                       Marker = true
                   });


                //Add a lower order packet which MAY belong to the frame, 65535 could be a previous packet or a large jump, it would depend on the timestamp.
                frame.Add(new Media.Rtp.RtpPacket(2, false, false, Media.Common.MemorySegment.EmptyBytes)
                {
                    SequenceNumber = ushort.MaxValue
                });

                if (false == frame.HasMarker) throw new Exception("Frame does not have marker");

                //Add a lower order packet which MAY belong to the frame, 65534
                frame.Add(new Media.Rtp.RtpPacket(2, false, false, Media.Common.MemorySegment.EmptyBytes)
                {
                    SequenceNumber = ushort.MaxValue - 1
                });

                if (false == frame.HasMarker) throw new Exception("Frame does not have marker");

                //Add a higher order packet which does not belong to the frame because the marker packet was set on packet 0, the timestamp should also be different.
                try
                {
                    frame.Add(new Media.Rtp.RtpPacket(2, false, false, Media.Common.MemorySegment.EmptyBytes)
                    {
                        SequenceNumber = 1
                    }, false);

                    throw new Exception("Should not be allowed to add packet");
                }
                catch(InvalidOperationException)
                {
                    //Expected
                }

                if (frame.HighestSequenceNumber != ushort.MinValue) throw new Exception("Unexpected HighestSequenceNumber");

                if (frame.LowestSequenceNumber != ushort.MaxValue - 1) throw new Exception("Unexpected LowestSequenceNumber");

                //Remove a non existing packet
                using (Media.Rtp.RtpPacket packet = frame.Remove(1))
                {
                    if (packet != null) throw new Exception("Packet is not null");
                }

                //Remove three existing packets
                using (Media.Rtp.RtpPacket packet = frame.Remove(ushort.MaxValue))
                {
                    if(packet == null || packet.SequenceNumber != ushort.MaxValue) throw new Exception("Packet is null");
                }

                using (Media.Rtp.RtpPacket packet = frame.Remove(ushort.MinValue))
                {
                    if (packet == null || packet.SequenceNumber != ushort.MinValue) throw new Exception("Packet is null");
                }

                using (Media.Rtp.RtpPacket packet = frame.Remove(ushort.MaxValue - 1))
                {
                    if (packet == null || packet.SequenceNumber != ushort.MaxValue - 1) throw new Exception("Packet is null");
                }

                if (false == frame.IsEmpty) throw new Exception("Frame is not empty");


                //Add marker packet with seq = 0
                frame.Add(new Media.Rtp.RtpPacket(2, false, false, Media.Common.MemorySegment.EmptyBytes)
                {
                    SequenceNumber = ushort.MinValue
                });

                frame.Add(new Media.Rtp.RtpPacket(2, false, false, Media.Common.MemorySegment.EmptyBytes)
                {
                    SequenceNumber = 1
                });

                //Remove 1
                using(frame.Remove(1)) ;

                if (frame.HighestSequenceNumber != 0) throw new Exception("HighestSequenceNumber Incorrect");

                if (frame.LowestSequenceNumber != 0) throw new Exception("LowestSequenceNumber Incorrect");

                using (frame.Remove(0)) ;
            }
        }

        public void TestContains()
        {
            //Create a frame
            using (Media.Rtp.RtpFrame frame = new Media.Rtp.RtpFrame(0))
            {
                frame.Add(new Media.Rtp.RtpPacket(2, false, false, Media.Common.MemorySegment.EmptyBytes)
                {
                    SequenceNumber = ushort.MaxValue - 1
                });

                frame.Add(new Media.Rtp.RtpPacket(2, false, false, Media.Common.MemorySegment.EmptyBytes)
                {
                    SequenceNumber = ushort.MaxValue
                });

                frame.Add(new Media.Rtp.RtpPacket(2, false, false, Media.Common.MemorySegment.EmptyBytes)
                {
                    SequenceNumber = ushort.MinValue,
                    Marker = true
                });

                if (frame.IsEmpty) throw new Exception("Frame is empty");

                if (false == frame.IsComplete) throw new Exception("Frame is not complete");

                if (false == frame.Contains(ushort.MaxValue)) throw new Exception("Does not contain expected sequence number");

                if (false == frame.Contains(ushort.MaxValue - 1)) throw new Exception("Does not contain expected sequence number");

                if (false == frame.Contains(ushort.MinValue)) throw new Exception("Does not contain expected sequence number");
            }
        }

        public void TestIsMissingPackets()
        {
            //Create a frame
            using (Media.Rtp.RtpFrame frame = new Media.Rtp.RtpFrame(0))
            {
                //Add packets to the frame
                for (int i = 0; i < 15; ++i)
                {

                    frame.Add(new Media.Rtp.RtpPacket(2, false, false, Media.Common.MemorySegment.EmptyBytes)
                    {
                        SequenceNumber = i,
                        Marker = i == 14
                    });
                }

                if (frame.IsMissingPackets) throw new Exception("Frame is missing packets");

                if (false == frame.IsComplete) throw new Exception("Frame is not complete");

                if (false == frame.HasMarker) throw new Exception("Frame does not have marker");

                //Remove the marker packet
                using (frame.Remove(14))
                {
                    //Frame doesn't have a marker packet anymore

                    //Check for IsComplete
                    if (frame.IsComplete) throw new Exception("Frame is complete");

                    //Check for HasMarker
                    if (frame.HasMarker) throw new Exception("Frame has marker");

                    //The frame IS NOT missing packets because the frame contains 0 - 13, but has no marker packet.
                    if (frame.IsMissingPackets) throw new Exception("Frame is missing packets");
                }


                //Remove the first packet
                using (frame.Remove(1))
                {
                    if (false == frame.IsMissingPackets) throw new Exception("Frame is not missing packets");
                }

                if (frame.Count != 13) throw new Exception("Frame Count is incorrect");
            }
        }

        public void TestSequenceNumberRollover()
        {
            //Create a frame
            unchecked
            {
                using (Media.Rtp.RtpFrame frame = new Media.Rtp.RtpFrame(0))
                {
                    //Add 15 packets to the frame
                    for (ushort i = ushort.MaxValue - 5; i != 10; ++i)
                    {

                        frame.Add(new Media.Rtp.RtpPacket(2, false, false, Media.Common.MemorySegment.EmptyBytes)
                        {
                            SequenceNumber = i,
                            Marker = i == 9
                        });
                    }

                    if (frame.IsMissingPackets) throw new Exception("Frame is missing packets");

                    if (false == frame.IsComplete) throw new Exception("Frame is not complete");

                    if (false == frame.HasMarker) throw new Exception("Frame does not have marker");

                    //Remove the packet with sequence number 0
                    using (frame.Remove(0))
                    {
                        if (false == frame.IsMissingPackets) throw new Exception("Frame is not missing packets");
                    }

                    //Remove the packet with sequence number 2
                    using (frame.Remove(2))
                    {
                        if (false == frame.IsMissingPackets) throw new Exception("Frame is not missing packets");
                    }

                    //Remove the marker packet
                    using (frame.Remove(9))
                    {
                        if (frame.IsComplete) throw new Exception("Frame is complete");

                        if (frame.HasMarker) throw new Exception("Frame has marker");

                        if (false == frame.IsMissingPackets) throw new Exception("Frame is not missing packets");                        
                    }
                }
            }
        }
    }
}
