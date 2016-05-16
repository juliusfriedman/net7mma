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

using System;
using System.Collections.Generic;
using System.Linq;
using Media.Common;

#endregion

namespace Media.Rtp
{

    #region RtpPacket

    /// <summary>
    /// A managed implemenation of the Rtp abstraction found in RFC3550.
    /// <see cref="http://tools.ietf.org/html/rfc3550"> RFC3550 </see> for more information
    /// </summary>
    public class RtpPacket : BaseDisposable, IPacket, ICloneable
    {
        #region Fields

        /// <summary>
        /// Provides a storage location for bytes which are owned by this instance.
        /// </summary>
        byte[] m_OwnedOctets;

        /// <summary>
        /// The RtpHeader assoicated with this RtpPacket instance.
        /// </summary>
        /// <remarks>
        /// readonly attempts to ensure no race conditions when accessing this field e.g. during property access when using the Dispose method.
        /// </remarks>
        public readonly RtpHeader Header;

        bool m_OwnsHeader;

        #endregion

        #region Properties

        /// <summary>
        /// The binary data of the RtpPacket which may contain a ContributingSourceList, RtpExtension and Padding.
        /// </summary>
        public MemorySegment Payload { get; protected set; }

        /// <summary>
        /// Determines the amount of unsigned integers which must be contained in the ContributingSourcesList to make the payload complete.
        /// <see cref="RtpHeader.ContributingSourceCount"/>
        /// </summary>
        /// <remarks>
        /// Obtained by performing a Multiply against 4 from the high quartet in the first octet of the RtpHeader.
        /// This number can never be larger than 60 given by the mask `0x0f` (15) used to obtain the ContributingSourceCount.
        /// Subsequently >15 * 4  = 60
        /// Clamped with Min(60, Max(0, N)) where N = ContributingSourceCount * 4;
        /// </remarks>
        public int ContributingSourceListOctets
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]

            get { if (IsDisposed || Payload.Count == 0) return 0; return Binary.Clamp(Header.ContributingSourceCount * 4, 0, 60)/* Math.Min(60, Math.Max(0, Header.ContributingSourceCount * 4))*/; }
        }

        /// <summary>
        /// Determines the amount of octets in the RtpExtension in this RtpPacket.
        /// The maximum value this property can return is 65535.
        /// <see cref="RtpExtension.LengthInWords"/> for more information.
        /// </summary>
        public int ExtensionOctets
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]

            get { if (IsDisposed || false == Header.Extension || Payload.Count == 0) return 0; using (RtpExtension extension = GetExtension()) return extension != null ? extension.Size : 0; }
        }

        /// <summary>
        /// The amount of octets which should exist in the payload and belong either to the SourceList and or the RtpExtension.
        /// This amount does not reflect any padding which may be present because the padding is at the end of the payload.
        /// </summary>
        public int HeaderOctets
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]

            get { if (IsDisposed || Payload.Count == 0) return 0; return ContributingSourceListOctets + ExtensionOctets; }
        }

        /// <summary>
        /// Gets the amount of octets which are in the Payload property which are part of the padding if IsComplete is true.            
        /// This property WILL return the value of the last non 0 octet in the payload if Header.Padding is true, otherwise 0.
        /// <see cref="RFC3550.ReadPadding"/> for more information.
        /// </summary>
        public int PaddingOctets
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]

            get { if (IsDisposed || false == Header.Padding) return 0; return Media.RFC3550.ReadPadding(Payload.Array, Payload.Offset + Payload.Count - 1, 1); }
        }

        /// <summary>
        /// Indicates if the RtpPacket is formatted in a complaince to RFC3550 and that all data required to read the RtpPacket is available.
        /// This is dertermined by performing checks against the RtpHeader and data in the Payload to validate the SouceList and Extension if present.
        /// <see cref="SourceList"/> and <see cref="RtpExtension"/> for further information.
        /// </summary>
        public bool IsComplete
        {
            get
            {

                if (IsDisposed) return false;

                if (Header.IsCompressed) return true;

                //Invalidate certain conditions in an attempt to determine if the instance contains all data required.

                //if the instance is disposed then there is no data to verify
                int octetsContained = Payload.Count;

                //Check the ContributingSourceCount in the header, if set the payload must contain at least ContributingSourceListOctets
                octetsContained -= ContributingSourceListOctets;

                //If there are not enough octets return false
                if (octetsContained < 0) return false;

                //Check the Extension bit in the header, if set the RtpExtension must be complete
                if (Header.Extension) using (RtpExtension extension = GetExtension())
                    {
                        if (extension == null || false == extension.IsComplete) return false;

                        //Reduce the number of octets in the payload by the number of octets which make up the extension
                        octetsContained -= extension.Size;
                    }

                //If there is no padding there must be at least 0 octetsContained.
                if (false == Header.Padding) return octetsContained >= 0;

                //Otherwise calulcate the amount of padding in the Payload
                int paddingOctets = PaddingOctets;

                //The result of completion is that the amount of paddingOctets found were >= 0
                return octetsContained >= paddingOctets;
            }
        }

        /// <summary>
        /// Indicates the length in bytes of this RtpPacket instance. (Including the RtpHeader as well as SourceList and Extension if present.)
        /// </summary>
        public int Length
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]

            get { return IsDisposed ? 0 : RtpHeader.Length + Payload.Count; }
        }

        /// <summary>
        /// <see cref="PayloadData"/>
        /// </summary>
        internal protected Common.MemorySegment PayloadDataSegment
        {
            get
            {
                //Proably don't have to check...
                if (IsDisposed || Payload.Count == 0) return Media.Common.MemorySegment.Empty;

                int nonPayloadOctets = HeaderOctets, padding = PaddingOctets;

                //return Payload.Skip(nonPayloadOctets).Take(IsComplete ? Payload.Count - (nonPayloadOctets + PaddingOctets) : -1);

                return nonPayloadOctets > Payload.Count ? Payload : new Common.MemorySegment(Payload.Array, (Payload.Offset + nonPayloadOctets), Payload.Count - (nonPayloadOctets + padding));
            }
        }

        /// <summary>
        /// Gets the data in the Payload which does not belong to the ContributingSourceList or RtpExtension or Padding.
        /// The data if present usually contains data related to signal codification,
        /// the coding of which can be determined by a combination of the PayloadType and SDP information which was used to being the participation 
        /// which resulted in the transfer of this RtpPacket instance.
        /// </summary>
        public IEnumerable<byte> PayloadData
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]

            get { return PayloadDataSegment; }
        }

        internal protected Common.MemorySegment PaddingDataSegment 
        {
            get
            {
                //Maybe should provide the incomplete data...
                if (IsDisposed || false == IsComplete) return Media.Common.MemorySegment.Empty;

                //return Payload.Reverse().Take(PaddingOctets).Reverse();

                int padding = PaddingOctets;

                if (padding == 0) return Common.MemorySegment.Empty;

                return new Common.MemorySegment(Payload.Array, (Payload.Offset + Payload.Count) - padding, padding);

                //for (int  p = PaddingOctets, e = Payload.Count, i = e - p; i < e; ++i) yield return Payload[i];
            }
        }

        public IEnumerable<byte> PaddingData
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]

            get { return PaddingDataSegment; }
        }

        /// <summary>
        /// Copies the given octets to the Payload before any Padding and calls <see cref="SetLengthInWordsMinusOne"/>.
        /// </summary>
        /// <param name="octets">The octets to add</param>
        /// <param name="offset">The offset to start copying</param>
        /// <param name="count">The amount of bytes to copy</param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        internal protected virtual void AddBytesToPayload(IEnumerable<byte> octets, int offset = 0, int count = int.MaxValue)
        {
            if (IsReadOnly) throw new InvalidOperationException("Can only set the AddBytesToPayload when IsReadOnly is false.");

            //Build a seqeuence from the existing octets and the data in the ReportBlock

            int newBytes = count - offset;

            if (newBytes <= 0) return;

            //If there are existing owned octets (which may include padding)
            if (Padding)
            {
                //Determine the amount of bytes in the payload
                int payloadCount = Payload.Count,
                    //Determine the padding octets offset
                    paddingOctets = PaddingOctets,
                    //Determine the amount of octets in the payload
                    payloadOctets = payloadCount - paddingOctets;

                //The owned octets is a projection of the Payload existing, without the padding combined with the given octets from offset to count and subsequently the paddingOctets after the payload
                m_OwnedOctets = Enumerable.Concat(m_OwnedOctets.Take(m_OwnedOctets.Length - paddingOctets), Enumerable.Concat(octets.Skip(offset).Take(newBytes), Payload.Skip(payloadOctets).Take(paddingOctets))).ToArray();

                Synchronize();

                Payload.IncreaseLength(newBytes);

            }
            else if (m_OwnedOctets == null)
            {
                m_OwnedOctets = octets.Skip(offset).Take(newBytes).ToArray();

                Payload = new MemorySegment(m_OwnedOctets);

                //Header must not be null
            }
            else
            {
                m_OwnedOctets = Enumerable.Concat(m_OwnedOctets, octets.Skip(offset).Take(newBytes)).ToArray();

                Synchronize();

                Payload.IncreaseLength(newBytes);
            }

            //Return
            return;
        }

        #endregion

        #region Constructor

        //~RtpPacket() { Dispose(); }

        //Padding...

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public RtpPacket(int version, bool padding, bool extension, byte[] payload)
            : this(new RtpHeader(version, padding, extension), payload)
        {

        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public RtpPacket(int version, bool padding, bool extension, bool marker, int payloadType, int csc, int ssrc, int seq, int timestamp, byte[] payload = null)
            : this(new RtpHeader(version, padding, extension, marker, payloadType, csc, ssrc, seq, timestamp), payload)
        {
            
        }

        /// <summary>
        /// Creates a RtpPacket instance by projecting the given sequence to an array which is subsequently owned by the instance.
        /// </summary>
        /// <param name="header">The header to utilize. When Dispose is called this header will be diposed if ownsHeader is true.</param>
        /// <param name="octets">The octets to project</param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public RtpPacket(RtpHeader header, IEnumerable<byte> octets, bool shouldDispose = true) : base(shouldDispose)
        {
            if (header == null) throw new ArgumentNullException("header");

            //Assign the header (maybe referenced elsewhere, when dispose is called the given header will be disposed.)
            Header = header;

            m_OwnsHeader = shouldDispose;

            //Project the octets in the sequence of use the empty array
            m_OwnedOctets = (octets ?? Common.MemorySegment.Empty).ToArray();

            //The Payload property must be assigned otherwise the properties will not function in the instance.
            Payload = new MemorySegment(m_OwnedOctets, 0, m_OwnedOctets.Length);
        }

        /// <summary>
        /// Creates a RtpPacket instance from an existing RtpHeader and payload.
        /// Check the IsValid property to see if the RtpPacket is well formed.
        /// </summary>
        /// <param name="header">The existing RtpHeader</param>
        /// <param name="payload">The data contained in the payload</param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public RtpPacket(RtpHeader header, MemorySegment payload, bool shouldDispose = true) : base(shouldDispose)
        {
            if (header == null) throw new ArgumentNullException("header");

            Header = header;

            m_OwnsHeader = shouldDispose;

            Payload = payload;
        }

        /// <summary>
        /// Creates a RtpPacket instance by copying data from the given buffer at the given offset.
        /// </summary>
        /// <param name="buffer">The buffer which contains the binary RtpPacket to decode</param>
        /// <param name="offset">The offset to start copying</param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public RtpPacket(byte[] buffer, int offset, int count, bool shouldDispose = true) : base(shouldDispose)
        {
            if (buffer == null || buffer.Length == 0 || count <= 0) throw new ArgumentException("Must have data in a RtpPacket");

            m_OwnedOctets = new byte[count];

            Array.Copy(buffer, offset, m_OwnedOctets, 0, count);

            //Read the header
            Header = new RtpHeader(new Common.MemorySegment(m_OwnedOctets, offset, count));

            m_OwnsHeader = true;

            if (count > RtpHeader.Length && false == Header.IsCompressed)
            {
                //Create a segment to the payload deleniated by the given offset and the constant Length of the RtpHeader.
                Payload = new MemorySegment(m_OwnedOctets, RtpHeader.Length, count - RtpHeader.Length);
            }
            else
            {                
                //m_OwnedOctets = Media.Common.MemorySegment.EmptyBytes; //IsReadOnly should be false
                //Payload = new MemoryReference(m_OwnedOctets, 0, 0, m_OwnsHeader);
                Payload = MemorySegment.Empty;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public RtpPacket(byte[] buffer, int offset) : this(buffer, offset, buffer.Length - offset) { }

        /// <summary>
        /// Creates a packet instance of the given size.
        /// </summary>
        /// <param name="size"></param>
        /// <param name="shouldDispose"></param>
        public RtpPacket(int size, bool shouldDispose = true) : base(shouldDispose)
        {
            size = Common.Binary.Max(0, size);

            m_OwnedOctets = new byte[size];

            Header = new RtpHeader(new Common.MemorySegment(m_OwnedOctets, 0, Common.Binary.Min(size, RtpHeader.Length)));

            m_OwnsHeader = true;

            Payload = new MemorySegment(m_OwnedOctets, RtpHeader.Length, size - Header.Size);
        }
            
        #endregion

        #region Properties

        /// <summary>
        /// <see cref="RtpHeader.Version"/>
        /// </summary>
        public int Version
        {
            get { return Header.Version; }
            internal protected set
            {
                if (IsReadOnly) throw new InvalidOperationException("Version can only be set when IsReadOnly is false.");
                Header.Version = value;
            }
        }

        /// <summary>
        /// <see cref="RtpHeader.Padding"/>
        /// </summary>
        public bool Padding
        {
            get { return Header.Padding; }
            internal protected set
            {
                if (IsReadOnly) throw new InvalidOperationException("Padding can only be set when IsReadOnly is false.");
                Header.Padding = value;
            }
        }

        /// <summary>
        /// <see cref="RtpHeader.Extension"/>
        /// </summary>
        public bool Extension
        {
            get { return Header.Extension; }
            internal protected set
            {
                if (IsReadOnly) throw new InvalidOperationException("Extension can only be set when IsReadOnly is false.");
                Header.Extension = value;
            }
        }

        /// <summary>
        /// <see cref="RtpHeader.Marker"/>
        /// </summary>
        public bool Marker
        {
            get { return Header.Marker; }
            internal protected set
            {
                if (IsReadOnly) throw new InvalidOperationException("Marker can only be set when IsReadOnly is false.");
                Header.Marker = value;
            }
        }

        /// <summary>
        /// <see cref="RtpHeader.ContributingSourceCount"/>
        /// </summary>
        public int ContributingSourceCount
        {
            get { return Header.ContributingSourceCount; }
            internal protected set
            {
                if (IsReadOnly) throw new InvalidOperationException("ContributingSourceCount can only be set when IsReadOnly is false.");
                Header.ContributingSourceCount = value;
            }
        }

        /// <summary>
        /// <see cref="RtpHeader.PayloadType"/>
        /// </summary>
        public int PayloadType
        {
            get { return Header.PayloadType; }
            internal protected set
            {
                if (IsReadOnly) throw new InvalidOperationException("PayloadType can only be set when IsReadOnly is false.");
                Header.PayloadType = value;
            }
        }

        /// <summary>
        /// <see cref="RtpHeader.SequenceNumber"/>
        /// </summary>
        public int SequenceNumber
        {
            get { return Header.SequenceNumber; }
            internal protected set
            {
                if (IsReadOnly) throw new InvalidOperationException("SequenceNumber can only be set when IsReadOnly is false.");
                Header.SequenceNumber = value;
            }
        }

        /// <summary>
        /// <see cref="RtpHeader.Timestamp"/>
        /// </summary>
        public int Timestamp 
        {
            get { return Header.Timestamp; }
            internal protected set
            {
                if (IsReadOnly) throw new InvalidOperationException("Timestamp can only be set when IsReadOnly is false.");
                Header.Timestamp = value;
            }
        }

        /// <summary>
        /// <see cref="RtpHeader.SynchronizationSourceIdentifier"/>
        /// </summary>
        public int SynchronizationSourceIdentifier
        {
            get { return Header.SynchronizationSourceIdentifier; }
            internal protected set
            {
                if (IsReadOnly) throw new InvalidOperationException("SynchronizationSourceIdentifier can only be set when IsReadOnly is false.");
                Header.SynchronizationSourceIdentifier = value;
            }
        }

        public bool IsReadOnly { get { return false == m_OwnsHeader || m_OwnedOctets == null; } }

        #endregion

        #region Methods

        /// <summary>
        /// Gets an Enumerator which can be used to read the contribuing sources contained in this RtpPacket.
        /// <see cref="SourceList"/> for more information.
        /// </summary>
        public Media.RFC3550.SourceList GetSourceList() { if (IsDisposed) return null; return new Media.RFC3550.SourceList(this); }

        /// <summary>
        /// Gets the RtpExtension which would be created as a result of reading the data from the RtpPacket's payload which would be contained after any contained ContributingSourceList.
        /// If the RtpHeader does not have the Extension bit set then null will be returned.
        /// <see cref="RtpHeader.Extension"/> for more information.
        /// </summary>
        [CLSCompliant(false)]
        public RtpExtension GetExtension()
        {
            return Header.Extension && (Payload.Count - ContributingSourceListOctets) > RtpExtension.MinimumSize ? new RtpExtension(this) : null;
        }

        /// <summary>
        /// Provides the logic for cloning a RtpPacket instance.
        /// The RtpPacket class does not have a Copy Constructor because of the variations in which a RtpPacket can be cloned.
        /// </summary>
        /// <param name="includeSourceList">Indicates if the SourceList should be copied.</param>
        /// <param name="includeExtension">Indicates if the Extension should be copied.</param>
        /// <param name="includePadding">Indicates if the Padding should be copied.</param>
        /// <param name="selfReference">Indicates if the new instance should reference the data contained in this instance.</param>
        /// <returns>The RtpPacket cloned as result of calling this function</returns>
        public RtpPacket Clone(bool includeSourceList, bool includeExtension, bool includePadding, bool includeCoeffecients, bool selfReference)
        {
            //If the sourcelist and extensions are to be included and selfReference is true then return the new instance using the a reference to the data already contained.
            if (includeSourceList && includeExtension && includePadding && includeCoeffecients) return selfReference ? new RtpPacket(Header, Payload, false) { Transferred = Transferred } : new RtpPacket(Prepare().ToArray(), 0) { Transferred = Transferred };

            IEnumerable<byte> binarySequence = Media.Common.MemorySegment.EmptyBytes;

            bool hasSourceList = ContributingSourceCount > 0;

            //If the source list is included then include it.
            if (includeSourceList && hasSourceList)
            {
                var sourceList = GetSourceList();
                if (sourceList != null)
                {
                    binarySequence = GetSourceList().GetBinaryEnumerable();
                }
                else binarySequence = Media.Common.MemorySegment.EmptyBytes;
            }

            //Determine if the clone should have extenison
            bool hasExtension = Header.Extension;

            //If there is a header extension to be included in the clone
            if (hasExtension && includeExtension)
            {
                //Get the Extension
                using (RtpExtension extension = GetExtension())
                {
                    //If an extension could be obtained include it
                    if (extension != null) binarySequence = binarySequence.Concat(extension);
                }
            }

            //if the video data is required in the clone then include it
            if (includeCoeffecients) binarySequence = binarySequence.Concat(PayloadData); //Add the binary data to the packet except any padding

            //Determine if padding is present
            bool hasPadding = Header.Padding;

            //if padding is to be included in the clone then obtain the original padding directly from the packet
            if (hasPadding && includePadding) binarySequence = binarySequence.Concat(Payload.Array.Skip(Payload.Offset + Payload.Count - PaddingOctets)); //If just the padding is required the skip the Coefficients

            //Return the result of creating the new instance with the given binary
            return new RtpPacket(new RtpHeader(Header.Version, includePadding && hasPadding, includeExtension && hasExtension)
            {
                Timestamp = Header.Timestamp,
                SequenceNumber = Header.SequenceNumber,
                SynchronizationSourceIdentifier = Header.SynchronizationSourceIdentifier,
                PayloadType = Header.PayloadType,
                ContributingSourceCount = includeSourceList ? Header.ContributingSourceCount : 0
            }.Concat(binarySequence).ToArray(), 0) { Transferred = Transferred };
        }

        /// <summary>
        /// Generates a sequence of bytes containing the RtpHeader and any data contained in Payload.
        /// (Including the SourceList and RtpExtension if present)
        /// </summary>
        /// <param name="other">The optional other RtpHeader to utilize in the preperation</param>
        /// <returns>The sequence created.</returns>
        public IEnumerable<byte> Prepare(RtpHeader other = null)
        {
            return Enumerable.Concat<byte>(other ?? Header, Payload ?? Media.Common.MemorySegment.Empty);
        }

        public IEnumerable<byte> Prepare() { return Prepare(null); }

        /// <summary>
        /// Generates a sequence of bytes containing the RtpHeader with the provided parameters and any data contained in the Payload.
        /// The sequence generated includes the SourceList and RtpExtension if present.
        /// </summary>
        /// <param name="payloadType">The optional payloadType to use</param>
        /// <param name="ssrc">The optional identifier to use</param>
        /// <param name="timestamp">The optional Timestamp to use</param>
        /// <returns>The binary seqeuence created.</returns>
        /// <remarks>
        /// To create the sequence a new RtpHeader is generated and eventually disposed.
        /// </remarks>
        public IEnumerable<byte> Prepare(int? payloadType, int? ssrc, int? sequenceNumber = null, int? timestamp = null, bool? marker = null) //includeHeader, includePayload, includePadding
        {
            try
            {
                //when all are null the header is the same... could use own header...

                return Prepare(new RtpHeader(Version, Padding, Extension, marker ?? Marker, payloadType ?? PayloadType, ContributingSourceCount, ssrc ?? SynchronizationSourceIdentifier, sequenceNumber ?? SequenceNumber, timestamp ?? Timestamp));
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Provides a sample implementation of what would be required to complete a RtpPacket that has the IsComplete property False.
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public virtual int CompleteFrom(System.Net.Sockets.Socket socket, Common.MemorySegment buffer)
        {
            if (IsReadOnly) throw new InvalidOperationException("Cannot modify a RtpPacket when IsReadOnly is false.");

            //If the packet is complete then return
            if (IsDisposed || IsComplete) return 0;

            // Cache the size of the original payload
            int payloadCount = Payload.Count,
                octetsRemaining = payloadCount, //Cache how many octets remain in the payload
                offset = Payload.Offset,//Cache the offset in parsing 
                sourceListOctets = ContributingSourceListOctets,//Cache the amount of octets required in the ContributingSourceList.
                extensionSize = Header.Extension ? RtpExtension.MinimumSize : 0, //Cache the amount of octets required to read the ExtensionHeader
                recieved = 0;

            //If the ContributingSourceList is not complete
            if (payloadCount < sourceListOctets)
            {
                //Calulcate the amount of octets to receive, ABS is weird and not required since paycount is checked to be less
                octetsRemaining = sourceListOctets - payloadCount; //Binary.Abs(payloadCount - sourceListOctets);

                //octetsRemaining = Binary.Min(payloadCount, sourceListOctets);

                //Allocte the memory for the required data
                if (m_OwnedOctets == null) m_OwnedOctets = new byte[octetsRemaining];
                else m_OwnedOctets = m_OwnedOctets.Concat(new byte[octetsRemaining]).ToArray();

                System.Net.Sockets.SocketError error;

                //Read from the stream, decrementing from octetsRemaining what was read.
                while (octetsRemaining > 0)
                {
                    //Receive octetsRemaining or less
                    int justReceived = Media.Common.Extensions.Socket.SocketExtensions.AlignedReceive(m_OwnedOctets, offset, octetsRemaining, socket, out error);

                    //Move the offset
                    offset += justReceived;

                    //Decrement how many octets were receieved
                    octetsRemaining -= justReceived;

                    recieved += justReceived;
                }
            }

            //At the end of the sourceList
            offset = sourceListOctets;

            //ContribuingSourceList is now Complete

            //If there is a RtpExtension indicated by the RtpHeader
            if (Header.Extension)
            {
                //Determine if the extension header was read
                octetsRemaining = RtpExtension.MinimumSize - (payloadCount - offset);

                //If the extension header is not yet read
                if (octetsRemaining > 0) 
                {
                    //Allocte the memory for the extension header
                    if (m_OwnedOctets == null) m_OwnedOctets = new byte[octetsRemaining];
                    else m_OwnedOctets = m_OwnedOctets.Concat(new byte[octetsRemaining]).ToArray();

                    System.Net.Sockets.SocketError error;

                    //Read from the socket, decrementing from octetsRemaining what was read.
                    while (octetsRemaining > 0)
                    {
                        //Receive octetsRemaining or less
                        int justReceived = Media.Common.Extensions.Socket.SocketExtensions.AlignedReceive(m_OwnedOctets, offset, octetsRemaining, socket, out error);

                        //Move the offset
                        offset += justReceived;

                        //Decrement how many octets were receieved
                        octetsRemaining -= justReceived;

                        recieved += justReceived;
                    }
                }

                //at least 4 octets are now present in Payload @ Payload.Offset

                //Use a RtpExtension instance to read the Extension Header and data.
                using (RtpExtension extension = GetExtension())
                {
                    if (extension != null && false == extension.IsComplete)
                    {
                        //Cache the size of the RtpExtension (not including the Flags and LengthInWords [The Extension Header])
                        extensionSize = extension.Size - RtpExtension.MinimumSize;

                        //The amount of octets required for for completion are indicated by the Size property of the RtpExtension.
                        //Calulcate the amount of octets to receive
                        octetsRemaining = (payloadCount - offset) - RtpExtension.MinimumSize;

                        if (octetsRemaining > 0 && octetsRemaining < extensionSize)
                        {
                            //Allocte the memory for the required data
                            if (m_OwnedOctets == null) m_OwnedOctets = new byte[octetsRemaining];
                            else m_OwnedOctets = m_OwnedOctets.Concat(new byte[octetsRemaining]).ToArray();

                            System.Net.Sockets.SocketError error;

                            //Read from the stream, decrementing from octetsRemaining what was read.
                            while (octetsRemaining > 0)
                            {
                                //Receive octetsRemaining or less
                                int justReceived = Media.Common.Extensions.Socket.SocketExtensions.AlignedReceive(m_OwnedOctets, offset, octetsRemaining, socket, out error);

                                //Move the offset
                                offset += justReceived;

                                //Decrement how many octets were receieved
                                octetsRemaining -= justReceived;

                                recieved += justReceived;
                            }
                        }
                    }
                }
            }

            //RtpExtension is now Complete

            //If the header indicates the payload has padding
            if (Header.Padding)
            {
                //Double check this math
                octetsRemaining = PaddingOctets - payloadCount;

                if (octetsRemaining > 0)
                {
                    //Allocte the memory for the required data
                    if (m_OwnedOctets == null) m_OwnedOctets = new byte[octetsRemaining];
                    else m_OwnedOctets = m_OwnedOctets.Concat(new byte[octetsRemaining]).ToArray();

                    offset = payloadCount;

                    //If the amount of bytes read in the padding is NOT equal to the last byte in the segment the RtpPacket is NOT complete
                    while (octetsRemaining > 0)
                    {
                        System.Net.Sockets.SocketError error;

                        //Receive 1 byte
                        //Receive octetsRemaining or less
                        int justReceived = Media.Common.Extensions.Socket.SocketExtensions.AlignedReceive(m_OwnedOctets, offset, octetsRemaining, socket, out error);

                        //Move the offset
                        offset += justReceived;

                        recieved += justReceived;

                        octetsRemaining -= justReceived;
                    }
                }
            }

            //Padding is now complete

            //Re allocate the payload segment to include any completed data
            Payload = new Common.MemorySegment(m_OwnedOctets, Payload.Offset, m_OwnedOctets.Length);

            //RtpPacket is complete

            return recieved;
        }      

        #endregion

        #region IPacket

        public readonly DateTime Created = DateTime.UtcNow;

        DateTime IPacket.Created { get { return Created; } }

        public DateTime? Transferred { get; set; }

        long Common.IPacket.Length { get { return (long)Length; } }

        public virtual bool IsCompressed { get { return Header.IsCompressed; } }

        #endregion

        #region Overrides

        /// <summary>
        /// Disposes of any private data this instance utilized.
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();

            if (false == ShouldDispose) return;

            //If there is a referenced RtpHeader
            if (m_OwnsHeader)
            {
                //Dispose it
                Header.Dispose();
            }

            if (false == Common.IDisposedExtensions.IsNullOrDisposed(Payload))
            {
                //Payload goes away when Disposing
                Payload.Dispose();

                Payload = null;
            }

            //The private data goes away after calling Dispose
            m_OwnedOctets = null;
        }

        public override bool Equals(object obj)
        {
            if (System.Object.ReferenceEquals(this, obj)) return true;

            if (false == (obj is RtpPacket)) return false;

            RtpPacket other = obj as RtpPacket;

            return other.Length == Length
                 &&
                 other.Payload == Payload //SequenceEqual...
                 &&
                 other.GetHashCode() == GetHashCode();
        }

        //Packet equals...

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() { return Created.GetHashCode() ^ Header.GetHashCode(); }        

        /// <summary>
        /// Copies all of the data in the packet to the given destination. The amount of bytes copied is given by <see cref="Length"/>
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="offset"></param>
        public void CopyTo(byte[] destination, int offset)
        {
            offset += Header.CopyTo(destination, offset);

            Common.MemorySegmentExtensions.CopyTo(Payload, destination, offset);
        }

        /// <summary>
        /// Calls <see cref="Update"/> on the <see cref="Payload"/> and <see cref="Synchronize"/> on the <see cref="Header"/>
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        internal protected void Synchronize()
        {
            //Should check IsContiguous

            //Rather than allowing set could just have a Resize method.
            Payload.Update(ref m_OwnedOctets);

            Header.Synchronize(ref m_OwnedOctets);
        }

        /// <summary>
        /// Indicates if the <see cref="Header"/> and <see cref="Payload"/> belong to the same array.
        /// </summary>
        /// <returns></returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public bool IsContiguous()
        {
            return Header.IsContiguous() && Header.PointerToLast10Bytes.Array == Payload.Array && Header.PointerToLast10Bytes.Offset + Header.PointerToLast10Bytes.Count == Payload.Offset;
        }

        #endregion

        #region Operators

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(RtpPacket a, RtpPacket b)
        {
            object boxA = a, boxB = b;
            return boxA == null ? boxB == null : a.Equals(b);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(RtpPacket a, RtpPacket b) { return false == (a == b); }

        #endregion

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        object ICloneable.Clone()
        {
            return this.Clone(true, true, true, true, false);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public bool TryGetBuffers(out System.Collections.Generic.IList<System.ArraySegment<byte>> buffer)
        {
            buffer  = new System.Collections.Generic.List<ArraySegment<byte>>()
            {
                Common.MemorySegmentExtensions.ToByteArraySegment(Header.First16Bits.m_Memory),
                Common.MemorySegmentExtensions.ToByteArraySegment(Header.PointerToLast10Bytes),
                Common.MemorySegmentExtensions.ToByteArraySegment(Payload),
            };

            return true;
        }

    }

    #endregion
}

namespace Media.UnitTests
{
    /// <summary>
    /// Provides tests which ensure the logic of the RtpPacket class is correct
    /// </summary>
    internal class RtpPacketUnitTests
    {
        public static void TestAConstructor_And_Reserialization()
        {
            //Cache a bitValue
            bool bitValue = false;

            //Permute every possible bit packed value that can be valid in the first and second octet
            for (int ibitValue = 0; ibitValue < 2; ++ibitValue)
            {
                //Make a bitValue after the 0th iteration
                if (ibitValue > 0) bitValue = Convert.ToBoolean(bitValue);

                //Permute every possible value within the 2 bit Version
                for (int VersionCounter = 0; VersionCounter <= Media.Common.Binary.TwoBitMaxValue; ++VersionCounter)
                {
                    //Permute every possible value in the 7 bit PayloadCounter
                    for (int PayloadCounter = 0; PayloadCounter <= sbyte.MaxValue; ++PayloadCounter)
                    {
                        //Permute every possible value in the 4 bit ContributingSourceCounter
                        for (byte ContributingSourceCounter = byte.MinValue; ContributingSourceCounter <= Media.Common.Binary.FourBitMaxValue; ++ContributingSourceCounter)
                        {
                            int RandomId = Utility.Random.Next(), RandomSequenceNumber = Utility.Random.Next(ushort.MinValue, ushort.MaxValue), RandomTimestamp = Utility.Random.Next();

                            //Create a RtpPacket instance using the specified options
                            using (Media.Rtp.RtpPacket p = new Rtp.RtpPacket(VersionCounter, 
                                bitValue, !bitValue, bitValue, 
                                PayloadCounter, 
                                ContributingSourceCounter, 
                                RandomId, 
                                RandomSequenceNumber,
                                RandomTimestamp))
                            {
                                //Check the Version
                                System.Diagnostics.Debug.Assert(p.Version == VersionCounter, "Unexpected Version");

                                //Check the Padding
                                System.Diagnostics.Debug.Assert(p.Padding == bitValue, "Unexpected Padding");

                                //Check the Extension
                                System.Diagnostics.Debug.Assert(p.Extension == !bitValue, "Unexpected Extension");

                                //Check the PayloadType
                                System.Diagnostics.Debug.Assert(p.PayloadType == PayloadCounter, "Unexpected PayloadType");

                                //Check the ContributingSourceCount
                                System.Diagnostics.Debug.Assert(p.ContributingSourceCount == ContributingSourceCounter, "Unexpected ContributingSourceCounter");

                                //Check the Length
                                System.Diagnostics.Debug.Assert(p.Length == Media.Rtp.RtpHeader.Length, "Unexpected Length");

                                //Serialize, Deserialize and verify again
                                using (Media.Rtp.RtpPacket s = new Rtp.RtpPacket(p.Prepare().ToArray(), 0))
                                {
                                    if (false == s.Prepare().SequenceEqual(p.Prepare())) throw new Exception("Unexpected Data");

                                    //The HashCode of the header uses only the first 2 bytes of the header and the ssrc
                                    System.Diagnostics.Debug.Assert(s.Header.GetHashCode() == p.Header.GetHashCode(), "Unexpected GetHashCode");

                                    //This implies that a RtcpPacket Header can have the same HashCode as a RtpPacketHeader if their values are the same.
                                    //This may or may not be desireable depding on what one is trying to do with the HashCode.
                                    //E.g. if your string both types of packet in a single collection using the the HasCode,
                                    //it's possible a collision will occur for some RtpPackets and RtcpPackets with the current GetHashCode implementation.

                                    //HashCode's of an RtpPacket use the time they were created at as well as the underlying HashCode of their header.
                                    //If two packets were created with the same DateTime then their HashCode will be equal.
                                    //System.Diagnostics.Debug.Assert(s.GetHashCode() == p.GetHashCode(), "Unexpected GetHashCode");

                                    //This implies that two packets created at the same exact time with the same values should have the same HashCode.

                                    //The packet should be equal.. It's content's and values are the same as the original packet but:
                                    //the created time is different
                                    //The values in the header and data are equal but not the same reference
                                    //Maybe should have a DataEqual overload... etc.

                                    //Two packets will never be equal unless their Payload points to the same original object.

                                    //System.Diagnostics.Debug.Assert((s.Equals(p) ? false : true), "Unexpected Equals");

                                }
                            }
                        }
                    }
                }
            }
        }
    }
}