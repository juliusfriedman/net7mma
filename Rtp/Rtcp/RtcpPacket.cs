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
using Media.Common;

#endregion
namespace Media.Rtcp
{
    #region RtcpPacket

    /// <summary>
    /// A managed implemenation of the Rtcp abstraction found in RFC3550.
    /// <see cref="http://tools.ietf.org/html/rfc3550"> RFC3550 </see> for more information
    /// </summary>
    public class RtcpPacket : SuppressedFinalizerDisposable, IPacket, ICloneable
    {
        #region Statics

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
        public static IEnumerable<RtcpPacket> GetPackets(byte[] array, int offset, int count, int version = 2, int? payloadType = null, int? ssrc = null, bool shouldDispose = true)
        {

            //array.GetLowerBound(0) for VB, UpperBound(0) is then the index of the last element
            int lowerBound = 0, upperBound = array.Length; 

            if (offset < lowerBound || offset > upperBound) throw new ArgumentOutOfRangeException("index", "Must refer to an accessible position in the given array");

            if (count <= lowerBound) yield break;            

            if (count > upperBound) throw new ArgumentOutOfRangeException("count", "Must refer to an accessible position in the given array");

            //Would overflow the array
            if (count + offset > upperBound) throw new ArgumentOutOfRangeException("index", "Count must refer to an accessible position in the given array when deleniated by index");

            int remains = count;

            //While  a 32 bit value remains to be read in the vector
            while (remains >= RtcpHeader.Length)
            {
                //Get the header of the packet to verify if it is wanted or not
                using (var header = new RtcpHeader(new Common.MemorySegment(array, offset, remains)))
                {
                    //Determine how long the header was
                    int headerSize = header.Size;

                    //Determine the amount of bytes in the packet NOT INCLUDING the RtcpHeader (Which may be 0 or 65535)
                    //16384 is the maximum value which should occupy the LengthInWordsMinusOne in a single IP RTCP packet
                    //Values over this such as 65535 will be truncated to 0 when added with 1 when the result type is not bound to ushort

                    //When LengthInWordsMinusOne == 0 this means there should only be the header, 0 + 1 = 1 * 4 = 4

                    int lengthInBytes = headerSize > remains ? 0 : Binary.MachineWordsToBytes((ushort)(header.LengthInWordsMinusOne + 1)); 

                    //Create a packet using the existing header and the bytes left in the packet
                    using (RtcpPacket newPacket = new RtcpPacket(header, lengthInBytes == 0 ? MemorySegment.Empty : new MemorySegment(array, offset + headerSize, Binary.Clamp(lengthInBytes - headerSize, 0, remains - headerSize)), shouldDispose))
                    {
                        lengthInBytes = headerSize + newPacket.Payload.Count;

                        remains -= lengthInBytes;

                        offset += lengthInBytes;

                        //Check for the optional parameters before returning the packet
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
        /// <param name="header">The header to utilize.</param>
        /// <param name="octets">The octets to project</param>
        /// <param name="shouldDispose">Indicates if the header should be disposed.</param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public RtcpPacket(RtcpHeader header, IEnumerable<byte> octets, bool shouldDispose = true)
            : base(shouldDispose)
        {
            //Should probably keep header and just assign octets...  
            //The reason why this doesn't happen is that the amount of bytes to take from octets can only be determined from the header...

            //The instance owns the header
            m_OwnsHeader = shouldDispose;

            //Reference the header
            Header = header;

            //Project the sequence
            m_OwnedOctets = octets.ToArray();

            //Determine the amount of bytes in the header and packet
            int headerLength = RtcpHeader.Length, packetLength = Header.LengthInWordsMinusOne;

            //packetLength contains the LengthInWordsMinusOne
            switch (header.LengthInWordsMinusOne)
            {
                case ushort.MaxValue: // FFFF + 1 = 0
                    //case ushort.MinValue:// 0 + 1 = 1 * 4 = 4, header only.???
                    {
                        Payload = Common.MemorySegment.Empty;

                        m_OwnedOctets = Payload.Array;

                        return;
                    }
                default:
                    {
                        //Header has another word
                        headerLength = Header.Size; //Binary.BytesPerLong;

                        //Packet length is given by the LengthInWordsMinusOne + 1 * 4
                        //packetLength = ((ushort)((packetLength + 1) * 4));

                        //The header was consumed and the packet cannot be less than 0 bytes or more than the buffer's length - offset in parsing.
                        //packetLength = Binary.Clamp(packetLength - headerLength, 0, m_OwnedOctets.Length - headerLength);

                        packetLength = m_OwnedOctets.Length - headerLength;

                        //If there are no bytes then just handle as would for 0xFFFF
                        if (packetLength <= 0) goto case ushort.MaxValue;

                        //Assign the payload
                        Payload = new Common.MemorySegment(m_OwnedOctets, true);

                        return;
                    }
            }
        }        

        /// <summary>
        /// Creates a RtcpPacket instance from an existing RtcpHeader and payload.
        /// Check the IsValid property to see if the RtcpPacket is well formed.
        /// </summary>
        /// <param name="header">The existing RtpHeader (which is now owned by this instance)</param>
        /// <param name="payload">The data contained in the payload</param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public RtcpPacket(RtcpHeader header, MemorySegment payload, bool shouldDispose = true)
            : base(shouldDispose)
        {
            if (header == null) throw new ArgumentNullException("header");

            //The instance owns the header
            m_OwnsHeader = shouldDispose;

            Header = header;

            Payload = payload;            
        }

        /// <summary>
        /// Creates a RtcpPacket instance from the given parameters by copying data.
        /// </summary>
        /// <param name="buffer">The buffer which contains the binary RtcpPacket to decode</param>
        /// <param name="offset">The offset to start decoding</param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public RtcpPacket(byte[] buffer, int offset, int count, bool shouldDispose = true) : base(shouldDispose)
        {
            //The instance owns the header
            m_OwnsHeader = shouldDispose;

            //Create the header
            Header = new RtcpHeader(buffer, offset);

            //Calulate how many bytes the header used
            int headerSize = Header.Size;

            //Remove that many bytes from the count.
            //count -= headerSize;

            //packetLength contains the LengthInWordsMinusOne
            switch (count)
            {
                //case ushort.MaxValue: // FFFF + 1 = 0
                case ushort.MinValue:// 0 + 1 = 1 * 4 = 4, header only.???
                    {
                        Payload = new MemorySegment(0);
                        
                        m_OwnedOctets = Payload.Array;

                        return;
                    }
                default:
                    {
                        //only take up to the length of the packet or what remains available to copy.
                        int remains = count - headerSize;

                        //If there are no bytes then just handle as would for 0xFFFF
                        if (remains <= 0) goto case ushort.MinValue;

                        //Make the array
                        m_OwnedOctets = new byte[remains];

                        //Copy it
                        System.Array.Copy(buffer, offset + headerSize, m_OwnedOctets, 0, remains);

                        //Assign the payload
                        Payload = new Common.MemorySegment(m_OwnedOctets, 0, Binary.Clamp(0, Binary.MachineWordsToBytes(Header.LengthInWordsMinusOne + 1) - headerSize, remains));

                        return;
                    }
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public RtcpPacket(byte[] buffer, int offset, bool shouldDispose = true) 
            :this (buffer, offset, buffer.Length - offset, shouldDispose)
        {


        }

        /// <summary>
        /// Creates a new RtcpPacket with the given paramters.
        /// Throws a <see cref="OverflowException"/> if padding is less than 0 or greater than byte.MaxValue.
        /// </summary>
        /// <param name="version">Sets <see cref="Version"/></param>
        /// <param name="payloadType">Sets <see cref="PayloadType"/></param>
        /// <param name="padding">The amount of padding octets if greater than 0</param>
        /// <param name="ssrc">Sets <see cref="SendersSyncrhonizationSourceIdentifier"/></param>
        /// <param name="blockCount">Sets <see cref="BlockCount"/> </param>
        /// <param name="lengthInWords">Sets <see cref="RtcpHeader.LengthInWordsMinusOne"/></param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public RtcpPacket(int version, int payloadType, int padding, int ssrc, int blockCount, int lengthInWords, int blockSize, int extensionSize, bool shouldDispose = true)
            :base(shouldDispose)
        {
            //If the padding is greater than allow throw an overflow exception
            if (padding < 0 || padding > byte.MaxValue) Binary.CreateOverflowException("padding", padding, byte.MinValue.ToString(), byte.MaxValue.ToString());            

            extensionSize = Binary.MachineWordsToBytes(Binary.BytesToMachineWords(extensionSize));

            //Calulcate the size of the Payload Segment, padding SHOULD be included here although this is not enforced.
            int payloadSize = (blockSize * blockCount) + extensionSize;

            //Octet alignment should always be respected when creating the payload, this will avoid a few uncecessary resizes.
            int nullOctetsRequired = (extensionSize & 0x03);

            //262136 is really the maximum number of bytes which can appear in the payload...

            payloadSize += nullOctetsRequired + padding;

            int octetsLength = payloadSize + Common.Binary.BytesPerLong;

            //Allocate an array of byte equal to the size required
            m_OwnedOctets = new byte[octetsLength];

            //Header = new RtcpHeader(version, payloadType, padding > 0, blockCount, ssrc, lengthInWords);

            Header = new RtcpHeader(new Common.MemorySegment(m_OwnedOctets, 0, Common.Binary.BytesPerLong))
            {
                Version = version,
                PayloadType = payloadType,
                Padding = padding > 0,
                BlockCount = blockCount,
                LengthInWordsMinusOne = lengthInWords,
                SendersSynchronizationSourceIdentifier = ssrc
            };

            m_OwnsHeader = true;

            //int headerSize = Header.Size;

            //Segment the array to allow property access.
            Payload = new MemorySegment(m_OwnedOctets, Common.Binary.BytesPerLong, payloadSize); //, Common.Binary.Max(0, payloadSize - Header.Size));

            //If there was padding determine where to put it.
            if (padding > 0) m_OwnedOctets[Common.Binary.Min(octetsLength - 1, Common.Binary.BytesPerLong + (Payload.Offset + Payload.Count - 1))] = (byte)padding;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public RtcpPacket(int version, int payloadType, int padding, int ssrc, int lengthInWords, int blockCount, bool shouldDispose = true)
            :this(version, payloadType, padding, ssrc, blockCount, lengthInWords, 0, 0, shouldDispose)
        {
            
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public RtcpPacket(int version, int payloadType, int padding, int blockCount, int ssrc, int lengthInWords, byte[] payload, int index, int count, bool shouldDispose = true)
            :this(version, payloadType, padding, ssrc, blockCount, lengthInWords, shouldDispose)
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
        public MemorySegment Payload { get; protected set; } //should be backed field

        public bool IsReadOnly
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]

            get { return false == m_OwnsHeader; }
        }

        /// <summary>
        /// Indicates if there is data past the Payload which is not accounted for by the <see cref="Header"/>
        /// </summary>
        //public bool IsCompound { get { return Payload.Count > ((ushort)((Header.LengthInWordsMinusOne + 1) * 4)); } }

        /// <summary>
        /// Gets the amount of octets which are in the Payload property which are part of the padding if IsComplete is true.            
        /// This property WILL return the value of the last non 0 octet in the payload if Header.Padding is true, otherwise 0.
        /// <see cref="RFC3550.ReadPadding"/> for more information.
        /// </summary>
        public int PaddingOctets
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]

            get { return IsDisposed || Header.Padding == false ? 0 : Media.RFC3550.ReadPadding(Payload.Array, Payload.Offset + Payload.Count - 1, 1); }
        }

        //Todo Segment properties.

        /// <summary>
        /// The length in bytes of this RtcpPacket including the <see cref="Header">Rtcp Header</see> and any <see cref="PaddingOctets"/>.
        /// </summary>
        public int Length
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]

            get
            {
                //Proably don't have to check IsDisposed
                return IsDisposed ? 0 : Header.Size + Payload.Count;
            }
        }

        /// <summary>
        /// Indicates if the RtcpPacket needs any more data to be considered complete.
        /// </summary>
        public bool IsComplete
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]

            get { return IsDisposed || Header.IsDisposed ? false : Length >= ((ushort)((Header.LengthInWordsMinusOne + 1) * Binary.BytesPerInteger)) - Header.Size; }    //((ushort)((Header.LengthInWordsMinusOne + 1) * Binary.BytesPerInteger)) - RtcpHeader.Length; }
        }

        /// <summary>
        /// <see cref="RtpHeader.Version"/>
        /// </summary>
        public int Version
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]

            get { return Header.Version; }
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]

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
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]

            get { return Header.Padding; }
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]

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
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]

            get { return Header.PayloadType; }
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]

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
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]

            get { return Header.BlockCount; }
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]

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
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]

            get { return Header.SendersSynchronizationSourceIdentifier; }
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]

            internal protected set
            {
                if (IsReadOnly) throw new InvalidOperationException("SynchronizationSourceIdentifier can only be set when IsReadOnly is false.");
                Header.SendersSynchronizationSourceIdentifier = value;
            }
        }

        #endregion

        #region Methods

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
        /// Sets the <see cref="RtcpHeader.LengthInWordsMinusOne"/> property based on the Length property.
        /// Throws a <see cref="InvalidOperationException"/> if <see cref="IsReadOnly"/> is true.
        /// </summary>
        internal protected void SetLengthInWordsMinusOne()
        {
            if (IsReadOnly) throw new InvalidOperationException("An RtcpPacket cannot be modifed when IsReadOnly is true.");

            //Set the LengthInWords property to the lengthInWords minus one
            //Header.LengthInWordsMinusOne = (ushort)(Binary.BytesToMachineWords(Length)); //Binary.BytesToMachineWords(Length - 1); // (ushort)(Length * Binary.BitsPerByte / Binary.BitsPerInteger) - 1;

            //Since there is the possibility for the Length of a RtcpPacket to exceed 65535 bytes do not cast outside the getter / setter.
            Header.LengthInWordsMinusOne = Binary.BytesToMachineWords(Length - RtcpHeader.Length);
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
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        internal protected virtual void AddBytesToPayload(IEnumerable<byte> octets, int offset = 0, int count = int.MaxValue, bool setLength = true) //overload for padd if necessary?
        {
            if (IsReadOnly) throw new InvalidOperationException("Can only set the AddBytesToPayload when IsReadOnly is false.");

            //Should also handle inSetLengthInWordsMinusOne
            //if (Common.Binary.BytesToMachineWords(Length + count) > ushort.MaxValue) throw new InvalidOperationException("RtcpPacket's cannot have more than 65535 bytes.");

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
                //Concat whatever we own with the new data concatenated with the existing padding and project it to an array
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

            //Set the length in words minus one in the header
            if(setLength) SetLengthInWordsMinusOne();
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        internal protected void Synchronize()
        {

            //Should check IsContiguous

            //Rather than allowing set could just have a Resize method.
            Payload.Update(ref m_OwnedOctets);

            Header.Synchronize(ref m_OwnedOctets);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public bool IsContiguous()
        {
            return Header.IsContiguous() && Header.SegmentToLast6Bytes.Array == Payload.Array && Header.SegmentToLast6Bytes.Offset + Header.SegmentToLast6Bytes.Count == Payload.Offset;
        }

        /// <summary>
        /// Generates a sequence of bytes containing the RtcpHeader and any data including Padding contained in the Payload.
        /// </summary>
        /// <returns>The sequence created.</returns>
        public IEnumerable<byte> Prepare()
        {
            //foreach (byte b in Header) yield return b; foreach (byte b in Payload ?? MemorySegment.Empty) yield return b;

            return Enumerable.Concat(Header, Payload ?? MemorySegment.Empty);
        }

        public IEnumerable<byte> Prepare(bool includeHeader, bool includeReportData, bool includePadding)
        {

            IEnumerable<byte> sequence = Enumerable.Empty<byte>();

            //if(includeHeader) foreach (byte b in Header) yield return b;

            if (includeHeader) sequence = Enumerable.Concat(sequence, Header);

            //if (includeReportData) foreach (byte b in RtcpData) yield return b;

            if (includeReportData) sequence = Enumerable.Concat(sequence, RtcpData);

            //if (includePadding) foreach (byte b in PaddingData) yield return b;

            if (includePadding) sequence = Enumerable.Concat(sequence, PaddingData);

            return sequence;
        }

        /// <summary>
        /// Gets the data in the <see cref="Payload"/> of the RtcpPacket inluding any <see cref="ExtensionData"/> bot not any <see cref="PaddingOctets"/> if the packet <see cref="IsComplete"/>.
        /// </summary>
         public IEnumerable<byte> RtcpData
         {
             get
             {
                 if (IsDisposed || Payload.Count == 0) return Media.Common.MemorySegment.Empty;

                 //return Payload.Take(Payload.Count - PaddingOctets);

                 return new Common.MemorySegment(Payload.Array, Payload.Offset, Payload.Count - PaddingOctets);
             }
         }

        /// <summary>
         /// The amount of data as specified by RFC3550 if <see cref="Padding"/> is true, otherwise an Empty Sequnce
        /// </summary>
        public IEnumerable<byte> PaddingData
        {
            get
            {
                if (IsDisposed || false == Padding || false == IsComplete || Payload.Count == 0) return Media.Common.MemorySegment.Empty;

                //return Payload.Skip(Payload.Count - PaddingOctets);

                ////return Payload.Reverse().Take(PaddingOctets).Reverse();

                int padding = PaddingOctets;

                if (padding == 0) return Common.MemorySegment.Empty;

                return new Common.MemorySegment(Payload.Array, (Payload.Offset + Payload.Count) - padding, padding);
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
            IEnumerable<byte> binarySequence = Media.Common.MemorySegment.EmptyBytes;

            try
            {
                //If the sourcelist and extensions are to be included and selfReference is true then return the new instance using the a reference to the data already contained.
                if (padding && reportBlocks) return selfReference ? new RtcpPacket(Header.Clone(selfReference), Payload) { Transferred = Transferred } : new RtcpPacket(Prepare().ToArray(), 0, Length) { Transferred = Transferred };
                else if (reportBlocks) binarySequence = binarySequence.Concat(RtcpData); //Add the binary data to the packet except any padding
                else if (padding) binarySequence = binarySequence.Concat(Payload.Array.Skip(Payload.Count - PaddingOctets)); //Add only the padding

                //Return the result of creating the new instance with the given binary
                return new RtcpPacket(new RtcpHeader(Header.Version, Header.PayloadType, padding ? Header.Padding : false, reportBlocks ? Header.BlockCount : 0, Header.SendersSynchronizationSourceIdentifier), binarySequence) { Transferred = Transferred };
                
            }
            catch { throw; } //If anything goes wrong deliver the exception
            finally { binarySequence = null; } //When the stack is cleaned up remove the reference to the binarySequence
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

            //Needs to account for buffer or socket.

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
                
                //Re-allocate the segment around the received data. //length + received
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
        /// Builds the <see cref="ImplementationMap"/> from all loaded types
        /// </summary>
        static RtcpPacket() { MapDerivedImplementations(); }

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
                ? Media.Common.Extensions.Generic.Dictionary.DictionaryExtensions.TryAdd(ImplementationMap, payloadType, implementation, out any) : false;
        }

        internal static bool TryUnMapImplementation(byte payloadType, out Type implementation) { implementation = null; Exception any; return payloadType > default(byte) && Media.Common.Extensions.Generic.Dictionary.DictionaryExtensions.TryRemove(ImplementationMap, payloadType, out implementation, out any); }

        #endregion

        #region Overrides

        /// <summary>
        /// Disposes of any private data this instance utilized.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (false == disposing || false == ShouldDispose) return;

            base.Dispose(ShouldDispose);

            //If there is a referenced RtpHeader
            if (m_OwnsHeader && false == Common.IDisposedExtensions.IsNullOrDisposed(Header))
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

            if (false == (obj is RtcpPacket)) return false;

            RtcpPacket other = obj as RtcpPacket;

            return other.Length == Length
                &&
                other.Payload == Payload //SequenceEqual...
                && 
                other.GetHashCode() == GetHashCode();
        }

        //Packet equals...

        public override int GetHashCode() { return Created.GetHashCode() ^ Header.GetHashCode(); }

        #endregion

        #region Operators
        
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(RtcpPacket a, RtcpPacket b)
        {
            object boxA = a, boxB = b;
            return boxA == null ? boxB == null : a.Equals(b);
        }
        
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(RtcpPacket a, RtcpPacket b) { return false == (a == b); }

        #endregion

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        object ICloneable.Clone()
        {
            return this.Clone(true, true, false);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public bool TryGetBuffers(out System.Collections.Generic.IList<System.ArraySegment<byte>> buffer)
        {

            if (IsDisposed)
            {
                buffer = default(System.Collections.Generic.IList<System.ArraySegment<byte>>);

                return false;
            }

            buffer = new System.Collections.Generic.List<ArraySegment<byte>>()
            {
                Common.MemorySegmentExtensions.ToByteArraySegment(Header.First16Bits.m_Memory),
                Common.MemorySegmentExtensions.ToByteArraySegment(Header.SegmentToLast6Bytes),
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
    /// Provides tests which ensure the logic of the RtcpPacket class is correct
    /// </summary>
    internal class RtcpPacketUnitTests
    {
        /// <summary>
        /// O (  )
        /// </summary>
        public static void TestAConstructor_And_Reserialization()
        {
            //Cache a bitValue
            bool bitValue = false;

            unchecked
            {
                //Test every possible bit packed value that can be valid in the first and second octet
                for (int ibitValue = 0; ibitValue < 2; ++ibitValue)
                {
                    //Make a bitValue after the 0th iteration
                    if (ibitValue > 0) bitValue = Convert.ToBoolean(ibitValue);

                    //Permute every possible value within the 2 bit Version
                    for (byte VersionCounter = 0; VersionCounter <= Media.Common.Binary.TwoBitMaxValue; ++VersionCounter)
                    {
                        //Permute every possible value in the 7 bit PayloadCounter
                        for (int PayloadCounter = 0; PayloadCounter <= byte.MaxValue; ++PayloadCounter)
                        {
                            //Permute every possible value in the 5 bit BlockCount
                            for (byte ReportBlockCounter = byte.MinValue; ReportBlockCounter <= Media.Common.Binary.FiveBitMaxValue; ++ReportBlockCounter)
                            {
                                //Permute every possible value in the Padding field.
                                for (byte PaddingCounter = byte.MinValue; PaddingCounter <= Media.Common.Binary.FiveBitMaxValue; ++PaddingCounter)
                                {
                                    //Create a RtpPacket instance using the specified options
                                    using (Media.Rtcp.RtcpPacket p = new Rtcp.RtcpPacket(VersionCounter, PayloadCounter, PaddingCounter, 7, ushort.MaxValue, ReportBlockCounter))
                                    {
                                        //Check the Version
                                        System.Diagnostics.Debug.Assert(p.Version == VersionCounter, "Unexpected Version");

                                        //Check the Padding
                                        System.Diagnostics.Debug.Assert(p.Padding == PaddingCounter > 0, "Unexpected Padding");

                                        //Check the BlockCount
                                        System.Diagnostics.Debug.Assert(p.BlockCount == ReportBlockCounter, "Unexpected BlockCount");

                                        //Check the SynchronizationSourceIdentifier
                                        System.Diagnostics.Debug.Assert(p.SynchronizationSourceIdentifier == 0 || p.SynchronizationSourceIdentifier == 7, "Unexpected SynchronizationSourceIdentifier");

                                        //Check the LengthInWordsMinusOne, should not be 0 when padding is used...
                                        System.Diagnostics.Debug.Assert(p.Header.LengthInWordsMinusOne == ushort.MaxValue, "Unexpected LengthInWordsMinusOne");

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
                                            System.Diagnostics.Debug.Assert(p.PaddingOctets == s.PaddingOctets, "Unexpected Padding");

                                            //Check the BlockCount
                                            System.Diagnostics.Debug.Assert(s.BlockCount == ReportBlockCounter, "Unexpected BlockCount");

                                            //Check the SynchronizationSourceIdentifier, we specified 0 for the length in words...
                                            //s.SynchronizationSourceIdentifier == 0 || 
                                            System.Diagnostics.Debug.Assert(s.SynchronizationSourceIdentifier == p.SynchronizationSourceIdentifier, "Unexpected SynchronizationSourceIdentifier");

                                            //Check the LengthInWordsMinusOne
                                            System.Diagnostics.Debug.Assert(s.Header.LengthInWordsMinusOne == p.Header.LengthInWordsMinusOne, "Unexpected LengthInWordsMinusOne");

                                            //Check the Length
                                            System.Diagnostics.Debug.Assert(s.Length == p.Length, "Unexpected Length");

                                            //Check the data projects is equal to the data provided
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

        /// <summary>
        /// O ( )
        /// </summary>
        public static void TestInvalidPadding()
        {
            //Set padding when bit when there is no padding

            //Inverse

            //...
        }

        /// <summary>
        /// 
        /// </summary>
        public static void TestInvalidLengthInWords()
        {

        }
    }
}