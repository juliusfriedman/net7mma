﻿#region Copyright
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
using Media.Common;

#endregion
namespace Media.Rtcp
{
    #region RtcpPacket

    /// <summary>
    /// A managed implemenation of the Rtcp abstraction found in RFC3550.
    /// <see cref="http://tools.ietf.org/html/rfc3550"> RFC3550 </see> for more information
    /// </summary>
    public class RtcpPacket : BaseDisposable, IPacket, ICloneable
    {
        #region Constants and Statics

        static Type RtcpPacketType = typeof(RtcpPacket);

        /// <summary>
        /// The property which defines the name in which derivations of this type will utilize to specify their known PayloadType.
        /// The field must be static / const and must add it's type to the InstanceMap if it wishes to be known.
        /// </summary>
        const string PayloadTypeField = "PayloadType";

        /// <summary>
        /// Maps the PayloadType field to the implementation which best represents it.
        /// Derived instance which can be instantied are found in this collection after <see cref="MapDerivedImplementations"/> is called.
        /// </summary>
        internal protected static Dictionary<byte, Type> ImplementationMap = new Dictionary<byte, Type>();

        /// <summary>
        /// Provides a collection of abstractions which dervive from RtcpPacket, e.g. RtcpReport.
        /// </summary>
        internal protected static HashSet<Type> Abstractions = new HashSet<Type>();

        /// <summary>
        /// Parses all RtcpPackets contained in the array using the given paramters.
        /// </summary>
        /// <param name="array">The array to parse packets from.</param>
        /// <param name="offset">The index to start parsing</param>
        /// <param name="count">The amount of bytes to use in parsing</param>
        /// <param name="version">The optional <see cref="RtcpPacket.Version"/>version of the packets</param>
        /// <param name="payloadType">The optional <see cref="RtcpPacket.PayloadType"/> of all packets</param>
        /// <param name="ssrc">The optional <see cref="RtcpPacket.SynchronizationSourceIdentifier"/> of all packets</param>
        /// <returns>A pointer to each packet found</returns>
        public static IEnumerable<RtcpPacket> GetPackets(byte[] array, int offset, int count, int version = 2, int? payloadType = null, int? ssrc = null)
        {

            //array.GetLowerBound(0) for VB, UpperBound(0) is then the index of the last element
            int lowerBound = 0, upperBound = array.Length; 

            if (offset < lowerBound || offset > upperBound) throw new ArgumentOutOfRangeException("index", "Must refer to an accessible position in the given array");

            if (count <= lowerBound) yield break;            

            if (count > upperBound) throw new ArgumentOutOfRangeException("count", "Must refer to an accessible position in the given array");

            //Would overflow the array
            if (count + offset > upperBound) throw new ArgumentOutOfRangeException("index", "Count must refer to an accessible position in the given array when deleniated by index");

            //While  a 32 bit value remains to be read in the vector
            while (offset + RtcpHeader.Length < upperBound && count >= RtcpHeader.Length)
            {
                //Get the header of the packet to verify if it is wanted or not
                using (var header = new RtcpHeader(new Common.MemorySegment(array, offset, count)))
                {
                    //Get the lenth in words
                    int lengthInWords = header.LengthInWordsMinusOne;

                    //Determine how long the header was
                    int payloadOffset = header.Size;

                    //Determine the amount of bytes in the packet NOT INCLUDING the RtcpHeader (Which may be 0)
                    int lengthInBytes = ((lengthInWords + 1) * 4) - payloadOffset;

                    //Create a packet using the existing header and the bytes left in the packet
                    using (RtcpPacket newPacket = new RtcpPacket(header, new MemorySegment(array, offset + payloadOffset, Math.Min(lengthInBytes, count - payloadOffset))))
                    {
                        //Move the offset the length in bytes of the size of the last packet (including the header).
                        offset += newPacket.Length;

                        //Reduce the count
                        count -= newPacket.Length;

                        //Check for the optional parameters
                        if (payloadType.HasValue && payloadType.Value != header.PayloadType ||  // Check for the given payloadType if provided
                            ssrc.HasValue && ssrc.Value != header.SendersSynchronizationSourceIdentifier) //Check for the given ssrc if provided
                        {
                            //Skip the packet
                            continue;
                        }
                        
                        //Yield the packet, disposed afterwards
                        yield return newPacket;
                    }
                }
            }

            //Done parsing
            yield break;
        }

        /// <summary>
        /// Builds the <see cref="ImplementationMap"/> from all loaded types
        /// </summary>
        static RtcpPacket() { MapDerivedImplementations(); }

        #endregion

        #region Fields

        bool m_OwnsHeader;

        /// <summary>
        /// A reference to any octets owned in this RtcpPacket instance.
        /// </summary>
        protected byte[] m_OwnedOctets;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a RtcpPacket instance by projecting the given sequence to an array which is subsequently owned by the instance.
        /// The length of the sequence projected is determined by <paramref name="header"/>.
        /// Throws a ArgumentNullException if header is null
        /// </summary>
        /// <param name="header">The header to utilize. When Dispose is called this header will be diposed.</param>
        /// <param name="octets">The octets to project</param>
        public RtcpPacket(RtcpHeader header, IEnumerable<byte> octets, bool ownsHeader = true)
            :this(header.Concat(octets).ToArray(), 0, true)
        {
           
        }

        /// <summary>
        /// Creates a RtcpPacket instance from an existing RtcpHeader and payload.
        /// Check the IsValid property to see if the RtcpPacket is well formed.
        /// </summary>
        /// <param name="header">The existing RtpHeader (which is now owned by this instance)</param>
        /// <param name="payload">The data contained in the payload</param>
        public RtcpPacket(RtcpHeader header, MemorySegment payload, bool shouldDispose = true)
        {
            if (header == null) throw new ArgumentNullException("header");

            //The instance owns the header
            ShouldDispose = m_OwnsHeader = shouldDispose;

            Header = header;

            Payload = payload;            
        }

        /// <summary>
        /// Creates a RtcpPacket instance from the given parameters by copying data.
        /// </summary>
        /// <param name="buffer">The buffer which contains the binary RtcpPacket to decode</param>
        /// <param name="offset">The offset to start decoding</param>
        public RtcpPacket(byte[] buffer, int offset, bool shouldDispose = true)
        {
            //The instance owns the header
            ShouldDispose = m_OwnsHeader = shouldDispose;

            Header = new RtcpHeader(buffer, offset);

            //Determine the amount of bytes in the header and packet
            int headerLength = RtcpHeader.Length, packetLength = Header.LengthInWordsMinusOne;

            //If there is no words or only 1 word then there is nothing else in this packet.
            if (false == (packetLength == ushort.MaxValue || packetLength == 0))
            {
                //Header has another word
                headerLength += RtcpHeader.Length;

                //Packet length is given by the LengthInWordsMinusOne + 1 * 4
                packetLength = ((ushort)((packetLength + 1) * 4));
            }

            int nonHeaderBytes = packetLength - headerLength;

            //Project the octets in the sequence taking the minimum of the octets present and the octets required as indicated by the header.
            m_OwnedOctets = buffer.Skip(offset + headerLength).Take(nonHeaderBytes < 0 ? buffer.Length + nonHeaderBytes : nonHeaderBytes).ToArray();

            //The Payload property must be assigned otherwise the properties will not function in the instance.
            Payload = new Common.MemorySegment(m_OwnedOctets, shouldDispose);
        }

        /// <summary>
        /// Creates a new RtcpPacket with the given paramters.
        /// Throws a <see cref="OverflowException"/> if padding is less than 0 or greater than byte.MaxValue.
        /// </summary>
        /// <param name="version">Sets <see cref="Version"/></param>
        /// <param name="payloadType">Sets <see cref="PayloadType"/></param>
        /// <param name="padding">The amount of padding octets if greater than 0</param>
        /// <param name="blockCount">Sets <see cref="BlockCount"/> </param>
        /// <param name="ssrc">Sets <see cref="SendersSyncrhonizationSourceIdentifier"/></param>
        /// <param name="lengthInWords">Sets <see cref="RtcpHeader.LengthInWordsMinusOne"/></param>
        public RtcpPacket(int version, int payloadType, int padding, int blockCount, int ssrc, int lengthInWords)
        {
            //If the padding is greater than allow throw an overflow exception
            if (padding < 0 || padding > byte.MaxValue) Binary.CreateOverflowException("padding", padding, byte.MinValue.ToString(), byte.MaxValue.ToString());
            
            Header = new RtcpHeader(version, payloadType, padding > 0, blockCount, ssrc, lengthInWords);

            m_OwnsHeader = true;

            //If there is no padding return
            if (padding == 0)
            {
                Payload = Common.MemorySegment.Empty;
                return;
            }
            
            //Create the owned octets by projecting the sequence and any padding
            m_OwnedOctets = RFC3550.CreatePadding(padding).ToArray();

            Payload = new Common.MemorySegment(m_OwnedOctets, 0, padding);
        }

        public RtcpPacket(int version, int payloadType, int padding, int blockCount, int ssrc, int lengthInWords, byte[] payload, int index, int count)
            :this(version, payloadType, padding, blockCount, ssrc, lengthInWords)
        {
            if (count == 0) return;

            int lowerBound = payload.GetLowerBound(0), upperBound = payload.GetUpperBound(0);

            if (index < lowerBound || index > upperBound) throw new ArgumentOutOfRangeException("index", "Must refer to an accessible position in the given array");

            if (count > upperBound) throw new ArgumentOutOfRangeException("count", "Must refer to an accessible position in the given array");

            if (count + index > upperBound) throw new ArgumentOutOfRangeException("index", "Count must refer to an accessible position in the given array when deleniated by index");

            AddBytesToPayload(payload, index, count);
        }

        #endregion

        #region Properties

        /// <summary>
        /// The RtcpHeader assoicated with this RtcpPacket instance.
        /// </summary>
        /// <remarks>
        /// readonly attempts to ensure no race conditions when accessing this field e.g. during property access when using the Dispose method.
        /// </remarks>
        public readonly RtcpHeader Header;

        /// <summary>
        /// The binary data of the RtcpPacket which may contain ReportBlocks and or ExtensionData.
        /// </summary>
        public MemorySegment Payload { get; protected set; } //should be backe dfield

        public bool IsReadOnly { get { return false == m_OwnsHeader; } }

        /// <summary>
        /// Indicates if there is data past the Payload which is not accounted for by the <see cref="Header"/>
        /// </summary>
        //public bool IsCompound { get { return Payload.Count > ((ushort)((Header.LengthInWordsMinusOne + 1) * 4)); } }

        /// <summary>
        /// Gets the amount of octets which are in the Payload property which are part of the padding if IsComplete is true.            
        /// This property WILL return the value of the last non 0 octet in the payload if Header.Padding is true, otherwise 0.
        /// <see cref="RFC3550.ReadPadding"/> for more information.
        /// </summary>
        public int PaddingOctets { get { if (IsDisposed || false == Header.Padding || Payload.Count == 0) return 0; return Media.RFC3550.ReadPadding(Payload, Payload.Count - 1); } }

        /// <summary>
        /// The length in bytes of this RtcpPacket including the <see cref="Header">Rtcp Header</see> and any <see cref="PaddingOctets"/>.
        /// </summary>
        public int Length
        {
            get
            {
                return Header.Size + (Payload ?? Common.MemorySegment.Empty).Count;
            }
        }

        /// <summary>
        /// Indicates if the RtcpPacket needs any more data to be considered complete.
        /// </summary>
        public bool IsComplete { get { return IsDisposed || Header.IsDisposed ? false : Length >= ((ushort)((Header.LengthInWordsMinusOne + 1) * 4)) - RtcpHeader.Length; } }

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
        /// <see cref="RtcpHeader.PayloadType"/>
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
        /// <see cref="RtcpHeader.BlockCount"/>
        /// </summary>
        public int BlockCount
        {
            get { return Header.BlockCount; }
            internal protected set
            {
                if (IsReadOnly) throw new InvalidOperationException("BlockCount can only be set when IsReadOnly is false.");
                Header.BlockCount = value;
            }
        }

        /// <summary>
        /// <see cref="RtcpHeader.SynchronizationSourceIdentifier"/>
        /// </summary>
        public int SynchronizationSourceIdentifier
        {
            get { return Header.SendersSynchronizationSourceIdentifier; }
            internal protected set
            {
                if (IsReadOnly) throw new InvalidOperationException("SynchronizationSourceIdentifier can only be set when IsReadOnly is false.");
                Header.SendersSynchronizationSourceIdentifier = value;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Sets the <see cref="RtcpHeader.LengthInWordsMinusOne"/> property based on the Length property.
        /// Throws a <see cref="InvalidOperationException"/> if <see cref="IsReadOnly"/> is true.
        /// </summary>
        internal protected void SetLengthInWordsMinusOne()
        {
            if (IsReadOnly) throw new InvalidOperationException("An RtcpPacket cannot be modifed when IsReadOnly is true.");

            //Start with the Length in Octets which is the amount of 8 bit components
            int lengthInOctets = Length;

            //If there are no bytes in the payload then ensure LengthInWords is 0
            if (lengthInOctets <= 4)
            {
                Header.LengthInWordsMinusOne = 1;
                return;
            }

            //The length in 32 bit words is equal to Length * 8 / 4 (Might have to round uP?)
            ushort lengthInWords = (ushort)(lengthInOctets * Binary.BitsPerByte / Binary.BitsPerInteger);

            //Set the LengthInWords property to the lengthInWords minus one
            Header.LengthInWordsMinusOne = lengthInWords - 1;
        }

        /// <summary>
        /// Sets the Padding bit and creates the required amount of padding taking existing Padding into account.
        /// Existing padding data will be left in place if present but not the octet which counts the padding itself.
        /// If more then 255 octets of padding would be present as a result of this call then no padding is added.
        /// </summary>
        /// <param name="paddingAmount">The amount of padding create</param>
        internal protected virtual void SetPadding(int paddingAmount) 
        {
            throw new NotImplementedException();

            //Would have to check existing Padding, if the same then return,
            //If less padding would have to be removed && if == 0 then Padding = false.
            //If greater then padding would have to be added up to 255 respecting what is already present
        }

        /// <summary>
        /// Copies the given octets to the Payload before any Padding and calls <see cref="SetLengthInWordsMinusOne"/>.
        /// </summary>
        /// <param name="octets">The octets to add</param>
        /// <param name="offset">The offset to start copying</param>
        /// <param name="count">The amount of bytes to copy</param>
        internal protected virtual void AddBytesToPayload(IEnumerable<byte> octets, int offset = 0, int count = int.MaxValue, bool setLength = true) //overload for padd if necessary?
        {
            if (IsReadOnly) throw new InvalidOperationException("Can only set the AddBytesToPayload when IsReadOnly is false.");

            //Build a seqeuence from the existing octets and the data in the ReportBlock

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
                m_OwnedOctets = Enumerable.Concat(Payload.Take(payloadOctets), octets.Skip(offset).Take(count - offset))
                    .Concat(Payload.Skip(payloadOctets).Take(paddingOctets)).ToArray();
            }
            else if (m_OwnedOctets == null) m_OwnedOctets = octets.Skip(offset).Take(count - offset).ToArray();
            else m_OwnedOctets = Enumerable.Concat(m_OwnedOctets, octets.Skip(offset).Take(count - offset)).ToArray();

            //Create a pointer to the owned octets.
            Payload = new Common.MemorySegment(m_OwnedOctets);

            //Set the length in words minus one in the header
            if(setLength) SetLengthInWordsMinusOne();
        }

        /// <summary>
        /// Generates a sequence of bytes containing the RtcpHeader and any data including Padding contained in the Payload.
        /// </summary>
        /// <returns>The sequence created.</returns>
        public IEnumerable<byte> Prepare() { return IsDisposed ? Common.MemorySegment.Empty : Enumerable.Concat<byte>(Header, Payload); }

        public IEnumerable<byte> Prepare(bool includeHeader, bool includeReportData, bool includePadding)
        {
            if(includeHeader) foreach (byte b in Header) yield return b;

            if (includeReportData) foreach (byte b in RtcpData) yield return b;

            if (includePadding) foreach (byte b in PaddingData) yield return b;
        }

        /// <summary>
        /// Gets the data in the <see cref="Payload"/> of the RtcpPacket inluding any <see cref="ExtensionData"/> without any <see cref="PaddingOctets"/> if the packet <see cref="IsComplete"/>.
        /// </summary>
         public IEnumerable<byte> RtcpData
         {
             get
             {
                 if (IsDisposed || Payload.Count == 0) return Media.Common.MemorySegment.Empty;

                 return Payload.Take(IsComplete ? Payload.Count - PaddingOctets  : int.MaxValue);
             }
         }

        /// <summary>
        /// The amount of data as specified by RFC3550 if <see cref="IsComplete"/> is true, otherwise an Empty Sequnce
        /// </summary>
        public IEnumerable<byte> PaddingData
        {
            get
            {
                if (IsDisposed || false == IsComplete || Payload.Count == 0 || false == Padding) return Media.Common.MemorySegment.EmptyBytes;

                return Payload.Reverse().Take(PaddingOctets).Reverse();
            }
        }

        /// <summary>
        /// Provides the logic for cloning a RtcpPacket instance.
        /// The RtcpPacket class does not have a Copy Constructor because of the variations in which a RtcpPacket can be cloned.
        /// </summary>            
        /// <param name="padding">Indicates if the Padding should be copied.</param>
        /// <param name="selfReference">Indicates if the new instance should reference the data contained in this instance.</param>
        /// <returns>The RtcpPacket cloned as result of calling this function</returns>
        public RtcpPacket Clone(bool reportBlocks, bool padding, bool selfReference)
        {
            //Get the bytes which correspond to the header
            IEnumerable<byte> binarySequence = Media.Common.MemorySegment.EmptyBytes;

            try
            {
                //If the sourcelist and extensions are to be included and selfReference is true then return the new instance using the a reference to the data already contained.
                if ((padding && reportBlocks) && (selfReference && Payload.Count == 0)) return new RtcpPacket(Header.Clone(), Payload);

                if (padding && reportBlocks) binarySequence = binarySequence.Concat(Payload.Array.Skip(Payload.Offset).Take(Payload.Count));//Take everything that is left if padding and coeffecients are included.
                else if (reportBlocks) binarySequence = binarySequence.Concat(RtcpData); //Add the binary data to the packet except any padding
                else if (padding) binarySequence = binarySequence.Concat(Payload.Array.Skip(Payload.Count - PaddingOctets)); //Add only the padding

                //Return the result of creating the new instance with the given binary
                return new RtcpPacket(new RtcpHeader(Header.Version, Header.PayloadType, padding ? Header.Padding : false, reportBlocks ? Header.BlockCount : 0, Header.SendersSynchronizationSourceIdentifier), binarySequence) { Transferred = this.Transferred };
                
            }
            catch { throw; } //If anything goes wrong deliver the exception
            finally { binarySequence = null; } //When the stack is cleaned up remove the refernce to the binarySequence
        }

        /// <summary>
        /// Provides a sample implementation of what would be required to complete a RtpPacket that has the IsComplete property False.
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public virtual int CompleteFrom(System.Net.Sockets.Socket socket, Common.MemorySegment buffer)
        {
            if (IsReadOnly) throw new InvalidOperationException("Cannot modify a RtcpPacket when IsReadOnly is false.");

            //If the packet is complete then return
            if (IsDisposed || IsComplete) return 0;

            //Calulcate the amount of octets remaining in the RtcpPacket including the header
            int octetsRemaining = ((ushort)(Header.LengthInWordsMinusOne + 1)) * 4 - Length, offset = Payload != null ? Payload.Count : 0;

            if (octetsRemaining > 0)
            {
                //There is not enough room in the array to finish the packet
                if (Payload.Count < octetsRemaining)
                {
                    //Allocte the memory for the required data
                    if (m_OwnedOctets == null) m_OwnedOctets = new byte[octetsRemaining];
                    else m_OwnedOctets = m_OwnedOctets.Concat(new byte[octetsRemaining]).ToArray();
                }

                System.Net.Sockets.SocketError error;

                int recieved = 0;

                //Read from the stream, decrementing from octetsRemaining what was read.
                while (octetsRemaining > 0)
                {
                    int rec = Media.Common.Extensions.Socket.SocketExtensions.AlignedReceive(m_OwnedOctets, offset, octetsRemaining, socket, out error);
                    offset += rec;
                    octetsRemaining -= rec;
                    recieved += rec;
                }
                
                //Re-allocate the segment around the received data.
                Payload = new Common.MemorySegment(m_OwnedOctets, 0, m_OwnedOctets.Length);

                return recieved;
            }

            return 0;
        }

        #endregion

        #region IPacket

        public virtual bool IsCompressed { get { return Header.IsCompressed; } }

        public readonly DateTime Created = DateTime.UtcNow;

        DateTime IPacket.Created { get { return Created; } }

        public DateTime? Transferred { get; set; }

        long Common.IPacket.Length { get { return (long)Length; } }

        #endregion
        
        #region Expansion

        /// <summary>
        /// Returns all derived types of RtcpPacket in which the types are Abstract.
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<Type> GetImplementedAbstractions() { return Abstractions; }

        /// <summary>
        /// Return all Implementations of RtcpPacket in which the types are not Abstract.
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<KeyValuePair<byte, Type>> GetImplementations() { return ImplementationMap; }

        /// <summary>
        /// Returns the PayloadTypes which are associated to derived implementations of RtcpPacket.
        /// </summary>
        /// <returns></returns>
        // <remarks>
        //Replaced code like "payload >= (byte)Rtcp.SendersReport.PayloadType && payload <= (byte)Rtcp.ApplicationSpecificReport.PayloadType || payload >= 72 && payload <= 76"
        // </remarks>
        public static IEnumerable<byte> GetImplementedPayloadTypes() { return ImplementationMap.Keys; }

        /// <summary>
        /// Returns the implementation which best represents the given <paramref name="payloadType"/> if found.
        /// </summary>
        /// <param name="payloadType">The payloadType to find an implemention of</param>
        /// <returns></returns>
        public static Type GetImplementationForPayloadType(byte payloadType)
        {
            Type result = null;
            if (ImplementationMap.TryGetValue(payloadType, out result)) return result;
            return null;
        }

        public static Type GetImplementationForPayloadType(int payloadType) { return GetImplementationForPayloadType((byte)payloadType); }

        /// <summary>
        /// Finds all types in all loaded assemblies which are a subclass of RtcpPacket and adds those types to either the InstanceMap or the AbstractionBag
        /// </summary>
        internal protected static void MapDerivedImplementations(AppDomain domain = null)
        {            

            //Get all loaded assemblies in the current application domain
            foreach (var assembly in (domain ?? AppDomain.CurrentDomain).GetAssemblies())
            {
                //Iterate each derived type which is a SubClassOf RtcpPacket.
                foreach (var derivedType in assembly.GetTypes().Where(t => t.IsSubclassOf(RtcpPacketType)))
                {
                    //If the derivedType is an abstraction then add to the AbstractionBag and continue
                    if(derivedType.IsAbstract)
                    {
                        Abstractions.Add(derivedType);                            
                        continue;
                    }

                    //Obtain the field mapped to the derviedType which corresponds to the PayloadTypeField defined by the RtcpPacket implementation.
                    System.Reflection.FieldInfo payloadTypeField = derivedType.GetField(PayloadTypeField);

                    //If the field exists then try to map it, the field should be instance and have the attributes Static and Literal
                    if (payloadTypeField != null)
                    {
                        //Unbox the payloadType from an integer to a byte
                        byte payloadType = (byte)((int)payloadTypeField.GetValue(derivedType));

                        //if the mapping was not successful and the debbuger is attached break.
                        if (false == TryMapImplementation(payloadType, derivedType) && System.Diagnostics.Debugger.IsAttached)
                        {
                            //Another type was already mapped to the given payloadTypeField
                            System.Diagnostics.Debugger.Break();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Tries to map the given implemented type to the given payloadType
        /// </summary>
        /// <param name="payloadType">Any byte value other than 0</param>
        /// <param name="implementation">Any type which derives from <see cref="RtcpPacket"/></param>
        /// <returns>The result of adding the implemention to the InstanceMap</returns>
        internal static bool TryMapImplementation(byte payloadType, Type implementation)
        {
            Exception any;
            return payloadType > default(byte) &&
            implementation != null &&
            false == implementation.IsAbstract &&
            implementation.IsSubclassOf(RtcpPacketType)
                ? Media.Common.Collections.DictionaryExtensions.TryAdd(ImplementationMap, payloadType, implementation, out any) : false;
        }

        internal static bool TryUnMapImplementation(byte payloadType, out Type implementation) { implementation = null; Exception any; return payloadType > default(byte) && Media.Common.Collections.DictionaryExtensions.TryRemove(ImplementationMap, payloadType, out implementation, out any); }

        #endregion

        #region Overrides

        /// <summary>
        /// Disposes of any private data this instance utilized.
        /// </summary>
        public override void Dispose()
        {
            //If the instance was previously disposed return
            if (IsDisposed) return;

            //Call base's Dispose method first to set Diposed = true just incase another thread tries to finalze the object or access any properties
            base.Dispose();

            if (false == ShouldDispose) return;

            //If there is a referenced RtpHeader
            if (m_OwnsHeader && Header != null && false == Header.IsDisposed)
            {
                //Dispose it
                Header.Dispose();
                //Remove of the reference
                //Header = null;
            }

            if (Payload != null)
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

            if (false == (obj is RtcpPacket)) return false;

            RtcpPacket other = obj as RtcpPacket;

            return other.Length == Length
                &&
                other.Payload == Payload
                && 
                other.GetHashCode() == GetHashCode();
        }

        public override int GetHashCode() { return Created.GetHashCode() ^ Header.GetHashCode(); }

        #endregion

        #region Operators

        public static bool operator ==(RtcpPacket a, RtcpPacket b)
        {
            object boxA = a, boxB = b;
            return boxA == null ? boxB == null : a.Equals(b);
        }

        public static bool operator !=(RtcpPacket a, RtcpPacket b) { return !(a == b); }

        #endregion

        object ICloneable.Clone()
        {
            return this.Clone(true, true, false);
        }
    }

    #endregion
}


namespace Media.UnitTests
{
    /// <summary>
    /// Provides tests which ensure the logic of the RtcpPacket class is correct
    /// </summary>
    internal class RtcpPacketUnitTests
    {
        public static void TestAConstructor_And_Reserialization()
        {
            //Cache a bitValue
            bool bitValue = false;
            
            //Test every possible bit packed value that can be valid in the first and second octet
            for (int ibitValue = 0; ibitValue < 2; ++ibitValue)
            {
                //Make a bitValue after the 0th iteration
                if (ibitValue > 0) bitValue = Convert.ToBoolean(ibitValue);

                //Permute every possible value within the 2 bit Version
                for (int VersionCounter = 0; VersionCounter <= Media.Common.Binary.TwoBitMaxValue; ++VersionCounter)
                {
                    //Permute every possible value in the 7 bit PayloadCounter
                    for (int PayloadCounter = 0; PayloadCounter <= byte.MaxValue; ++PayloadCounter)
                    {
                        for (int ReportBlockCounter = byte.MinValue; ReportBlockCounter <= Media.Common.Binary.FiveBitMaxValue; ++ReportBlockCounter)
                        {
                            for (int PaddingCounter = byte.MinValue; PaddingCounter <= byte.MaxValue; ++PaddingCounter)
                            {
                                //Create a RtpPacket instance using the specified options
                                using (Media.Rtcp.RtcpPacket p = new Rtcp.RtcpPacket(VersionCounter, PayloadCounter, PaddingCounter, ReportBlockCounter, 7, 0))
                                {
                                    //Check the Version
                                    System.Diagnostics.Debug.Assert(p.Version == VersionCounter, "Unexpected Version");

                                    //Check the Padding
                                    System.Diagnostics.Debug.Assert(p.Padding == PaddingCounter > 0, "Unexpected Padding");

                                    //Check the BlockCount
                                    System.Diagnostics.Debug.Assert(p.BlockCount == ReportBlockCounter, "Unexpected BlockCount");

                                    //Check the SynchronizationSourceIdentifier
                                    System.Diagnostics.Debug.Assert(p.SynchronizationSourceIdentifier == 0, "Unexpected SynchronizationSourceIdentifier");

                                    //Check the LengthInWordsMinusOne
                                    System.Diagnostics.Debug.Assert(p.Header.LengthInWordsMinusOne == 0, "Unexpected LengthInWordsMinusOne");

                                    //Check the Length
                                    System.Diagnostics.Debug.Assert(p.Length == Media.Rtcp.RtcpHeader.Length + PaddingCounter, "Unexpected Length");

                                    //Check the IsComplete
                                    System.Diagnostics.Debug.Assert(p.IsComplete, "Not Complete");

                                    //Check the result of serialization using padding.

                                    //Another test would be to permute every possible value for the LengthInWords field :)
                                    //Set the LengthInWordsMinusOne so the correct amount of bytes are serialzed, we specified 0 in the constructor
                                    //p.SetLengthInWordsMinusOne();

                                    byte[] serialized = p.Prepare().ToArray();

                                    System.Diagnostics.Debug.Assert(serialized.Length == p.Length, "Unexpected Binary Data Serialized");

                                    //Make a managed packet from the serialized data and re-verify
                                    using (Media.Rtcp.RtcpPacket s = new Rtcp.RtcpPacket(serialized, 0))
                                    {
                                        //Check the IsComplete
                                        System.Diagnostics.Debug.Assert(s.IsComplete, "Not Complete");

                                        //Check the Version
                                        System.Diagnostics.Debug.Assert(s.Version == VersionCounter, "Unexpected Version");

                                        //Check the Padding
                                        System.Diagnostics.Debug.Assert(s.Padding == PaddingCounter > 0, "Unexpected Padding");

                                        //Check the BlockCount
                                        System.Diagnostics.Debug.Assert(s.BlockCount == ReportBlockCounter, "Unexpected BlockCount");

                                        //Check the SynchronizationSourceIdentifier (not serialized)
                                        System.Diagnostics.Debug.Assert(s.SynchronizationSourceIdentifier == 0, "Unexpected SynchronizationSourceIdentifier");

                                        //Check the LengthInWordsMinusOne
                                        System.Diagnostics.Debug.Assert(s.Header.LengthInWordsMinusOne == p.Header.LengthInWordsMinusOne, "Unexpected LengthInWordsMinusOne");

                                        //Check the Length
                                        System.Diagnostics.Debug.Assert(s.Length == p.Length, "Unexpected Length");

                                        System.Diagnostics.Debug.Assert(s.Prepare().SequenceEqual(serialized), "Unexpected Binary Data Serialized");
                                    }

                                }
                            }
                        }
                    }
                }
            }
        }
    }
}