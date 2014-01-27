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
using System.Text;
using Octet = System.Byte;
using OctetSegment = System.ArraySegment<byte>;
using Media.Common;

#endregion

namespace Media.Rtp
{

    #region RtpPacket

    /// <summary>
    /// A managed implemenation of the Rtp abstraction found in RFC3550.
    /// <see cref="http://tools.ietf.org/html/rfc3550"> RFC3550 </see> for more information
    /// </summary>
    public class RtpPacket : BaseDisposable, IPacket
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
        /// The binary data of the RtpPacket which may contain a ContributingSourceList and RtpExtension.
        /// </summary>
        public OctetSegment Payload { get; protected set; }

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
        public int ContributingSourceListOctets { get { if (Disposed || Payload.Count == 0) return 0; return Math.Min(60, Math.Max(0, Header.ContributingSourceCount * 4)); } }

        /// <summary>
        /// Determines the amount of octets in the RtpExtension in this RtpPacket.
        /// The maximum value this property can return is 65535.
        /// <see cref="RtpExtension.LengthInWords"/> for more information.
        /// </summary>
        public int ExtensionOctets { get { if (Disposed || !Header.Extension || Payload.Count == 0) return 0; using (RtpExtension extension = GetExtension()) return extension != null ? extension.Size : 0; } }

        /// <summary>
        /// The amount of octets which belong either to the SourceList or the RtpExtension.
        /// This amount does not reflect any padding which may be present.
        /// </summary>
        internal int NonPayloadOctets { get { if (Disposed || Payload.Count == 0) return 0; return ContributingSourceListOctets + ExtensionOctets; } }

        /// <summary>
        /// Gets the amount of octets which are in the Payload property which are part of the padding if IsComplete is true.            
        /// This property WILL return the value of the last non 0 octet in the payload if Header.Padding is true, otherwise 0.
        /// <see cref="RFC3550.ReadPadding"/> for more information.
        /// </summary>
        public int PaddingOctets { get { if (Disposed || !Header.Padding) return 0; return RFC3550.ReadPadding(Payload, NonPayloadOctets); } }

        /// <summary>
        /// Indicates if the RtpPacket is formatted in a complaince to RFC3550 and that all data required to read the RtpPacket is available.
        /// This is dertermined by performing checks against the RtpHeader and data in the Payload to validate the SouceList and Extension if present.
        /// <see cref="SourceList"/> and <see cref="RtpExtension"/> for further information.
        /// </summary>
        public bool IsComplete
        {
            get
            {

                if (Header.IsCompressed) return true;

                //Invalidate certain conditions in an attempt to determine if the instance contains all data required.

                //if the instance is disposed then there is no data to verify
                int octetsContained = Payload.Count;

                //Check the ContributingSourceCount in the header, if set the payload must contain at least ContributingSourceListOctets
                octetsContained -= ContributingSourceListOctets;

                //If there are not enough octets return false
                if (octetsContained < 0) return false;

                //Check the Extension bit in the header, if set the RtpExtension must be complete
                if (Header.Extension) using (var extension = GetExtension())
                    {
                        if (extension == null || !extension.IsComplete) return false;

                        //Reduce the number of octets in the payload by the number of octets which make up the extension
                        octetsContained -= extension.Size;
                    }

                //If there is no padding there must be at least 0 octetsContained.
                if (!Header.Padding) return octetsContained >= 0;

                //Otherwise calulcate the amount of padding in the Payload
                int paddingOctets = PaddingOctets;

                //The result of completion is that the amount of paddingOctets found were >= 0
                return paddingOctets >= 0 && octetsContained >= paddingOctets;
            }
        }

        /// <summary>
        /// Indicates the length in bytes of this RtpPacket instance. (Including the RtpHeader as well as SourceList and Extension if present.)
        /// </summary>
        public int Length { get { if (Disposed) return 0; return RtpHeader.Length + Payload.Count; } }

        /// <summary>
        /// Gets the data in the Payload which does not belong to the ContributingSourceList or RtpExtension or Padding.
        /// The data if present usually contains data related to signal codification,
        /// the coding of which can be determined by a combination of the PayloadType and SDP information which was used to being the participation 
        /// which resulted in the transfer of this RtpPacket instance.
        /// </summary>
        public IEnumerable<byte> Coefficients
        {
            get
            {
                if (Disposed || !IsComplete) return Utility.Empty;
                int nonPayloadOctets = NonPayloadOctets;
                return Payload.Array.Skip(Payload.Offset + nonPayloadOctets).Take(Payload.Count - nonPayloadOctets - PaddingOctets);
            }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a RtpPacket instance by projecting the given sequence to an array which is subsequently owned by the instance.
        /// </summary>
        /// <param name="header">The header to utilize. When Dispose is called this header will be diposed.</param>
        /// <param name="octets">The octets to project</param>
        public RtpPacket(RtpHeader header, IEnumerable<byte> octets, bool ownsHeader = true)
        {
            if (header == null) throw new ArgumentNullException("header");

            //Assign the header (maybe referenced elsewhere, when dispose is called the given header will be disposed.)
            Header = header;

            m_OwnsHeader = ownsHeader;

            //Project the octets in the sequence
            m_OwnedOctets = octets.ToArray();

            //The Payload property must be assigned otherwise the properties will not function in the instance.
            Payload = new OctetSegment(m_OwnedOctets, 0, m_OwnedOctets.Length);
        }

        /// <summary>
        /// Creates a RtpPacket instance from an existing RtpHeader and payload.
        /// Check the IsValid property to see if the RtpPacket is well formed.
        /// </summary>
        /// <param name="header">The existing RtpHeader</param>
        /// <param name="payload">The data contained in the payload</param>
        public RtpPacket(RtpHeader header, OctetSegment payload, bool ownsHeader = true)
        {
            if (header == null) throw new ArgumentNullException("header");

            Header = header;

            m_OwnsHeader = ownsHeader;

            Payload = payload;
        }

        /// <summary>
        /// Creates a RtpPacket instance by copying data from the given buffer at the given offset.
        /// </summary>
        /// <param name="buffer">The buffer which contains the binary RtpPacket to decode</param>
        /// <param name="offset">The offset to start copying</param>
        public RtpPacket(byte[] buffer, int offset)
        {
            if (buffer == null || buffer.Length == 0) throw new ArgumentException("Must have data in a RtpPacket");

            int bufferLength = buffer.Length;

            //Read the header
            Header = new RtpHeader(buffer, offset);
            
            m_OwnsHeader = true;

            if (bufferLength > RtpHeader.Length && !Header.IsCompressed)
            {
                //Advance the pointer
                offset += RtpHeader.Length;

                int ownedOctets = Math.Abs(buffer.Length - offset);
                m_OwnedOctets = new byte[ownedOctets];
                Array.Copy(buffer, offset, m_OwnedOctets, 0, ownedOctets);

                //Create a segment to the payload deleniated by the given offset and the constant Length of the RtpHeader.
                Payload = new OctetSegment(m_OwnedOctets, 0, ownedOctets);
            }
            else
            {                
                m_OwnedOctets = Utility.Empty; //IsReadOnly should be false
                Payload = new OctetSegment(m_OwnedOctets, 0, 0);
            }
        }

        /// <summary>
        /// Creates a RtpPacket instance from the given segment of memory.
        /// The instance will depend on the memory in the given buffer.
        /// </summary>
        /// <param name="buffer">The segment containing the binary data to decode.</param>
        public RtpPacket(OctetSegment buffer) : this(buffer.Array, buffer.Offset) { }

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

        public bool IsReadOnly { get { return !m_OwnsHeader || m_OwnedOctets == null; } }

        #endregion

        #region Methods

        /// <summary>
        /// Gets an Enumerator which can be used to read the contribuing sources contained in this RtpPacket.
        /// <see cref="SourceList"/> for more information.
        /// </summary>
        public SourceList GetSourceList() { if (Disposed) return null; return new SourceList(this); }

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
            //Get the bytes which correspond to the header
            IEnumerable<byte> binarySequence = Enumerable.Empty<byte>();

            //If the sourcelist and extensions are to be included and selfReference is true then return the new instance using the a reference to the data already contained.
            if (includeSourceList && includeExtension && selfReference) return new RtpPacket(Header.Clone(true), Payload);

            bool hasSourceList = ContributingSourceCount > 0;

            //If the source list is included then include it.
            if (includeSourceList && hasSourceList) binarySequence = GetSourceList().AsBinaryEnumerable();

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
            if (includeCoeffecients) binarySequence = binarySequence.Concat(Coefficients); //Add the binary data to the packet except any padding

            //Determine if padding is present
            bool hasPadding = Header.Padding;

            //if padding is to be included in the clone then obtain the original padding directly from the packet
            if (includePadding) binarySequence = binarySequence.Concat(Payload.Array.Skip(Payload.Offset + Payload.Count - PaddingOctets)); //If just the padding is required the skip the Coefficients

            //Return the result of creating the new instance with the given binary
            return new RtpPacket(new RtpHeader(Header.Version, includePadding && hasPadding, includeExtension && hasExtension)
            {
                ContributingSourceCount = includeSourceList ? Header.ContributingSourceCount : 0
            }.Concat(binarySequence).ToArray(), 0);
        }

        /// <summary>
        /// Generates a sequence of bytes containing the RtpHeader and any data contained in Payload.
        /// (Including the SourceList and RtpExtension if present)
        /// </summary>
        /// <param name="other">The optional other RtpHeader to utilize in the preperation</param>
        /// <returns>The sequence created.</returns>
        public IEnumerable<byte> Prepare(RtpHeader other = null) { return Enumerable.Concat<byte>(other ?? Header, Payload.Array.Skip(Payload.Offset).Take(Payload.Count)); }

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
        public IEnumerable<byte> Prepare(int? payloadType, int? ssrc, int? sequenceNumber = null, int? timestamp = null)
        {
            try
            {
                return Prepare(new RtpHeader(Version, Padding, Extension, Marker, payloadType ?? PayloadType, ContributingSourceCount, ssrc ?? SynchronizationSourceIdentifier, sequenceNumber ?? SequenceNumber, timestamp ?? Timestamp));
            }
            catch
            {
                throw;
            }
        }



        /// <summary>
        /// Disposes of any private data this instance utilized.
        /// </summary>
        public override void Dispose()
        {
            //If the instance was previously disposed return
            if (Disposed) return;

            //Call base's Dispose method first to set Diposed = true just incase another thread tries to finalze the object or access any properties
            base.Dispose();

            //If there is a referenced RtpHeader
            if (m_OwnsHeader && Header != null && !Header.Disposed)
            {
                //Dispose it
                Header.Dispose();
            }

            //Payload goes away when Disposing
            Payload = default(OctetSegment);

            //The private data goes away after calling Dispose
            m_OwnedOctets = null;
        }

        /// <summary>
        /// Provides a sample implementation of what would be required to complete a RtpPacket that has the IsComplete property False.
        /// </summary>
        public virtual void CompleteFrom(System.Net.Sockets.Socket socket)
        {
            if (IsReadOnly) throw new InvalidOperationException("Cannot modify a RtpPacket when IsReadOnly is false.");

            //If the packet is complete then return
            if (Disposed || IsComplete) return;

            // Cache the size of the original payload
            int payloadCount = Payload.Count,
                octetsRemaining = payloadCount, //Cache how many octets remain in the payload
                offset = Payload.Offset,//Cache the offset in parsing 
                sourceListOctets = ContributingSourceListOctets,//Cache the amount of octets required in the ContributingSourceList.
                extensionSize = Header.Extension ? 4 : 0; //Cache the amount of octets required to read the ExtensionHeader

            //If the ContributingSourceList is not complete
            if (payloadCount < sourceListOctets)
            {
                //Calulcate the amount of octets to receive
                octetsRemaining = Math.Abs(payloadCount - sourceListOctets);

                //Allocte the memory for the required data
                if (m_OwnedOctets == null) m_OwnedOctets = new byte[octetsRemaining];
                else m_OwnedOctets = m_OwnedOctets.Concat(new byte[octetsRemaining]).ToArray();

                System.Net.Sockets.SocketError error;

                //Read from the stream, decrementing from octetsRemaining what was read.
                while (octetsRemaining > 0)
                {
                    //Receive octetsRemaining or less
                    int justReceived = Utility.AlignedReceive(m_OwnedOctets, offset, octetsRemaining, socket, out error);

                    //Move the offset
                    offset += justReceived;

                    //Decrement how many octets were receieved
                    octetsRemaining -= justReceived;
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
                        int justReceived = Utility.AlignedReceive(m_OwnedOctets, offset, octetsRemaining, socket, out error);

                        //Move the offset
                        offset += justReceived;

                        //Decrement how many octets were receieved
                        octetsRemaining -= justReceived;
                    }
                }

                //at least 4 octets are now present in Payload @ Payload.Offset

                //Use a RtpExtension instance to read the Extension Header and data.
                using (RtpExtension extension = GetExtension())
                {
                    //Cache the size of the RtpExtension (not including the Flags and LengthInWords [The Extension Header])
                    extensionSize = extension.Size - RtpExtension.MinimumSize;

                    //The amount of octets required for for completion are indicated by the Size property of the RtpExtension.
                    //Calulcate the amount of octets to receive
                    octetsRemaining = Math.Abs(offset - (sourceListOctets + extensionSize));

                    //Allocte the memory for the required data
                    if (m_OwnedOctets == null) m_OwnedOctets = new byte[octetsRemaining];
                    else m_OwnedOctets = m_OwnedOctets.Concat(new byte[octetsRemaining]).ToArray();

                    System.Net.Sockets.SocketError error;

                    //Read from the stream, decrementing from octetsRemaining what was read.
                    while (octetsRemaining > 0)
                    {
                        //Receive octetsRemaining or less
                        int justReceived = Utility.AlignedReceive(m_OwnedOctets, offset, octetsRemaining, socket, out error);

                        //Move the offset
                        offset += justReceived;

                        //Decrement how many octets were receieved
                        octetsRemaining -= justReceived;
                    }
                }
            }

            //RtpExtension is now Complete

            //If the header indicates the payload has padding
            if (Header.Padding)
            {
                //If the amount of bytes read in the padding is NOT equal to the last byte in the segment the RtpPacket is NOT complete
                while (PaddingOctets == 0)
                {
                    //Allocte the memory for the required data
                    if (m_OwnedOctets == null) m_OwnedOctets = new byte[1];
                    else m_OwnedOctets = m_OwnedOctets.Concat(new byte[1]).ToArray();

                    System.Net.Sockets.SocketError error;

                    //Receive 1 byte
                    //Receive octetsRemaining or less
                    int justReceived = Utility.AlignedReceive(m_OwnedOctets, offset, 1, socket, out error);

                    //Move the offset
                    offset += justReceived;
                }
            }

            //Padding is now complete

            //Re allocate the payload segment to include any completed data
            Payload = new OctetSegment(m_OwnedOctets, Payload.Offset, m_OwnedOctets.Length);

            //RtpPacket is complete
        }      

        #endregion

        #region IPacket

        public readonly DateTime Created = DateTime.UtcNow;

        DateTime IPacket.Created { get { return Created; } }

        public DateTime? Transferred { get; set; }

        long Common.IPacket.Length { get { return (long)Length; } }

        #endregion
    }

    #endregion
}
