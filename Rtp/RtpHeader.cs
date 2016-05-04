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

namespace Media.Rtp
{
    #region RtpHeader

    #region Reference

    /* Copied from http://tools.ietf.org/html/rfc3550#section-5
         
           Schulzrinne, et al.         Standards Track                    [Page 12]
           FFC 3550                          RTP                          July 2003
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
         */

    #endregion

    /// <summary>
    /// Provides a manged abstraction around the first 12 octets in any RtpPacket.
    /// Further information can be found at http://tools.ietf.org/html/rfc3550#section-5
    /// </summary>
    /// <remarks>
    /// Is IDisposeable because the class OWNS a reference to the CommonHeaderBits and a byte[].
    /// </remarks>
    public class RtpHeader : BaseDisposable, IEnumerable<byte>
    {
        #region Implementation Notes and Reference

        /*
              RTP data packets contain no length field or other delineation,
               therefore RTP relies on the underlying protocol(s) to provide a
               length indication.  The maximum length of RTP packets is limited only by the underlying protocols.
             * 
     ******  (Subsequently by the Maximum Transmittable Unit if applicable of the underlying network which implements the underlying protocol)  ******
             *
               If RTP packets are to be carried in an underlying protocol that
               provides the abstraction of a continuous octet stream rather than
               messages (packets), an encapsulation of the RTP packets MUST be
               defined to provide a framing mechanism.  
             * 
             * Framing is also needed if the underlying protocol may contain padding so that the extent of the
               RTP payload cannot be determined.  
               The framing mechanism is not defined here.
             * 
               http://tools.ietf.org/html/rfc4571 Defined a framining mechanism to which known Errata exists at the time of this implementation.
             * http://www.rfc-editor.org/errata_search.php?rfc=4571
             * 
             * RFC4571 Also states @  [Page 1] (Paragraph 1, the final sentence)
             * 
             * `However, earlier versions of RTP/AVP did define a framing method, and this method is in use in several implementations.`
             * 
             * For reference the earlier version of RTP can be found @ ftp://gaia.cs.umass.edu/pub/hgschulz/rtp/draft-ietf-avt-rtp-04.txt
             * 
             * RFC2336 defined a method similar to that which is used by RFC4571 and can be located @ http://tools.ietf.org/html/rfc2326#section-10.12
             *
             * Specifically the difference is that multiple packets may not be stored in a single interleaved frame 
             * Also that different packet types e.g. Rtp and Rtcp cannot appear in the same frame.
             * 
             */

        #endregion

        #region Constants and Statics

        /// <summary>
        /// The header length (and subsequently) the minimum size of any given RtpPacket.
        /// </summary>
        public const int Length = 12;

        #endregion

        #region Fields

        /// <summary>
        /// A managed abstraction of the first two octets, 16 bits of the RtpHeader.
        /// </summary>
        internal Media.RFC3550.CommonHeaderBits First16Bits;

        /// <summary>
        /// A the last 10 octets of the RtpHeader.
        /// </summary>
        internal byte[] Last10Bytes;

        internal Common.MemorySegment PointerToLast10Bytes;

        #endregion

        #region Properties

        #region Unsafe for Multi Thread Access without prior Synchronization

        /*
             * To provide synchronized access to these contention would be required.
             * It is assumed that consumers of this instance would synchronize their access to these properties during such development.
             * 
             * Arguments for this include the fact that is costs more to syncrhonize than to perform the binary operations themselves.
             * This is also why the propery getters and setters use manual shifting rather than the provided binary implementations when possible.
             * 
             * This class is IDisposable only to ensure there are no memory leaks, the cost of checking for disposition is usually the same as performing the binary opertions and reduces performance.
             * 
             * A Common base class which contains the CheckDispose method, Disposed property and the Dispose implementation may make this implemenation slightly more maintain able.
             * 
             */

        /// <summary>
        /// Gets the Version bit field of the RtpHeader
        /// </summary>
        public int Version
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]

            get { /*CheckDisposed();*/ return First16Bits.Version; }
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]

            set { /*CheckDisposed();*/ First16Bits.Version = value; }
        }

        /// <summary>
        /// Indicates if the Padding bit is set in the first octet.
        /// </summary>
        public bool Padding
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]

            get { /*CheckDisposed();*/ return First16Bits.Padding; }
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]

            set { /*CheckDisposed();*/ First16Bits.Padding = value; }
        }

        /// <summary>
        /// Indicates if the Extension bit is set in the first octet.
        /// </summary>
        public bool Extension
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]

            get { /*CheckDisposed();*/ return First16Bits.Extension; }
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]

            set { /*CheckDisposed();*/ First16Bits.Extension = value; }
        }

        /// <summary>
        /// Gets or sets the nybble associated with the CC field in the RtpHeader.
        /// </summary>
        public int ContributingSourceCount
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]

            get { /*CheckDisposed();*/ return First16Bits.RtpContributingSourceCount; }
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]

            set { /*CheckDisposed();*/ First16Bits.RtpContributingSourceCount = value; }
        }

        /// <summary>
        /// Indicates if the Marker bit is set.
        /// </summary>
        public bool Marker
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]

            get { /*CheckDisposed();*/ return First16Bits.RtpMarker; }
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]

            set { /*CheckDisposed();*/ First16Bits.RtpMarker = value; }
        }

        /// <summary>
        /// Indicates the format of the data within the Payload
        /// </summary>
        public int PayloadType
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]

            //The value is revealed by clearing the 0th bit in the second octet.
            get { /*CheckDisposed();*/ return First16Bits.RtpPayloadType; }
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]

            set { /*CheckDisposed();*/ First16Bits.RtpPayloadType = value; }
        }

        #region Notes

        // NOTE The following fileds at offset by 2 from their absolute offset in the contagious allocation of memory for a given RtpHeader.
        // This is because they are octet aligned and contain no bit packed fields.

        #endregion

        public bool IsCompressed
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]

            get { return PointerToLast10Bytes.Count < 10; }
        }

        /// <summary>
        /// The amount of bytes this instance would occupy when serialized
        /// </summary>
        public int Size { get { return RFC3550.CommonHeaderBits.Size + PointerToLast10Bytes.Count; } }

        /// <summary>
        /// Gets or Sets the unsigned 16 bit SequenceNumber field in the RtpHeader.
        /// </summary>
        public int SequenceNumber
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]

            //The sequence number is stored in Netword Byte Order @ + 0x00 from the second octet (relative offset of 0x02 from the beginning of any header pointer)
            get { /*CheckDisposed();*/ return (ushort)Binary.ReadU16(PointerToLast10Bytes.Array, PointerToLast10Bytes.Offset, BitConverter.IsLittleEndian); }
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]

            set { /*CheckDisposed();*/ Binary.Write16(PointerToLast10Bytes.Array, PointerToLast10Bytes.Offset, BitConverter.IsLittleEndian, (ushort)value); }
        }

        /// <summary>
        /// Gets or sets the unsigned 32 bit Timestamp field in the RtpHeader
        /// </summary>
        /// <remarks>
        /// The absolute offset of the Timestamp field is at @ 0x04 from the start of any RtpHeader.
        /// </remarks>
        public int Timestamp
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]

            //The sequence number is stored in Netword Byte Order  @ + 0x02 from the second octet (relative offset of 0x04 from the beginning of any header pointer)
            get { /*CheckDisposed();*/ return (int)Binary.ReadU32(PointerToLast10Bytes.Array, PointerToLast10Bytes.Offset + 2, BitConverter.IsLittleEndian); } //Always read in reverse
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]

            set { /*CheckDisposed();*/ Binary.Write32(PointerToLast10Bytes.Array, PointerToLast10Bytes.Offset + 2, BitConverter.IsLittleEndian, (uint)value); }
        }

        /// <summary>
        /// Gets or sets the unsigned 32 bit SSRC field in the RtpHeader
        /// </summary>
        public int SynchronizationSourceIdentifier
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]

            //The sequence number is stored in Netword Byte Order @ + 0x06 from the second octet (relative offset of 0x08 from the beginning of any header pointer)
            get { /*CheckDisposed();*/ return (int)Binary.ReadU32(PointerToLast10Bytes.Array, PointerToLast10Bytes.Offset + 6, BitConverter.IsLittleEndian); }
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]

            set { /*CheckDisposed();*/ Binary.Write32(PointerToLast10Bytes.Array, PointerToLast10Bytes.Offset + 6, BitConverter.IsLittleEndian, (uint)value); }
        }

        #endregion

        #endregion

        #region Constructor

        /// <summary>
        /// Reads an instance of the RtpHeader class and copies 12 octets which make up the RtpHeader.
        /// </summary>
        /// <param name="octets">A reference to a byte array which contains at least 12 octets to copy.</param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public RtpHeader(byte[] octets, int offset = 0)
        {
            //Determine the length of the array
            long octetsLength;

            //If the octets reference is null throw an exception
            if (Common.Extensions.Array.ArrayExtensions.IsNullOrEmpty(octets, out octetsLength)) throw new ArgumentException("octets must not be null and must contain at least 1 element given the deleniation of the offset parameter.", "octets");

            //Check range
            if (offset > octetsLength) throw new ArgumentOutOfRangeException("offset", "Cannot be greater than the length of octets");

            //Todo, should not matter since this header instance would be allowed to be modified, saving the allocation here is not necessary.
            //The check is only relevent because octets my have less than 2 bytes which cannot be handled without exception in the CommonHeaderBits
            //Read a managed representation of the first two octets which are stored in Big ByteOrder / Network Byte Order
            First16Bits = octetsLength == 1 ? new Media.RFC3550.CommonHeaderBits(octets[offset], default(byte)) : new Media.RFC3550.CommonHeaderBits(octets, offset);

            //Allocate space for the other 10 octets
            Last10Bytes = new byte[10];

            //If there are octets for those bytes in the source array
            if (octetsLength > 2)
            {
                //Copy the remaining bytes of the header which consist of the 
                //SequenceNumber (2 octets / U16)
                //Timestamp (4 octets / U32)
                //SSRC (4 octets / U32)
                Array.Copy(octets, offset + 2, Last10Bytes, 0, Math.Min(10, octetsLength - 2));
            }

            //Assign the segment
            PointerToLast10Bytes = new MemorySegment(Last10Bytes, 0, Last10Bytes.Length);
        }

        /// <summary>
        /// Creates an exact copy of the RtpHeader from the given RtpHeader
        /// </summary>
        /// <param name="other">The RtpHeader to copy</param>
        /// <param name="reference">A value indicating if the RtpHeader given should be referenced or copied.</param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public RtpHeader(RtpHeader other, bool reference)
        {
            if (reference)
            {
                First16Bits = other.First16Bits;
                Last10Bytes = other.Last10Bytes;
                PointerToLast10Bytes = other.PointerToLast10Bytes;
            }
            else
            {
                First16Bits = new Media.RFC3550.CommonHeaderBits(other.First16Bits);
                Last10Bytes = new byte[10];
                PointerToLast10Bytes = new Common.MemorySegment(Last10Bytes, 0, 10);
                other.Last10Bytes.CopyTo(Last10Bytes, 0);
            }
        }
        
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public RtpHeader(Common.MemorySegment memory)
        {
            First16Bits = new Media.RFC3550.CommonHeaderBits(memory);

            PointerToLast10Bytes = new Common.MemorySegment(memory.Array, memory.Offset + RFC3550.CommonHeaderBits.Size, Binary.Clamp(memory.Count - RFC3550.CommonHeaderBits.Size, 0, 10));
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public RtpHeader(int version, bool padding, bool extension, bool shouldDispose = true)
            : base(shouldDispose)
        {
            First16Bits = new Media.RFC3550.CommonHeaderBits(version, padding, extension);

            //Allocate space for the other 10 octets
            Last10Bytes = new byte[10];

            PointerToLast10Bytes = new Common.MemorySegment(Last10Bytes, 0, 10);

            Version = version;

            Padding = padding;

            Extension = extension;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public RtpHeader(int version, bool padding, bool extension, bool marker, int payloadTypeBits, int contributingSourceCount, int ssrc, int sequenceNumber, int timestamp, bool shouldDispose = true)
            :this(version, padding, extension, shouldDispose)
        {
            //Set the marker bit
            Marker = marker;

            //Set the payloadType using the property for the same reason.
            PayloadType = payloadTypeBits;

            //Set the ContributingSourceCount through the property for the same reason.
            ContributingSourceCount = contributingSourceCount;

            //Set the id of the participant who sent this RtpHeader.
            SynchronizationSourceIdentifier = ssrc;

            //Set the sequence number of the packet
            SequenceNumber = sequenceNumber;

            //Set the Timestamp property
            Timestamp = timestamp;
        }

        #endregion

        #region Instance Methods        

        public int CopyTo(byte[] dest, int offset)
        {
            if (IsDisposed) return 0;

            int copied = 0;
            
            copied += First16Bits.CopyTo(dest, offset);

            offset += copied;

            Common.MemorySegmentExtensions.CopyTo(PointerToLast10Bytes, dest, offset);

            copied += PointerToLast10Bytes.Count;

            offset += PointerToLast10Bytes.Count;

            return copied;
        }

        /// <summary>
        /// Clones this RtpHeader instance.
        /// If reference is true any changes performed in either this instance or the new instance will be reflected in both instances.
        /// </summary>
        /// <param name="reference">indictes if the new instance should reference this instance.</param>
        /// <returns>The new instance</returns>
        public RtpHeader Clone(bool reference = false) { return new RtpHeader(this, reference); }

        internal IEnumerable<byte> GetEnumerableImplementation()
        {
            return Enumerable.Concat<byte>(First16Bits, PointerToLast10Bytes);
        }

        #endregion

        #region IEnumerable Implementations

        IEnumerator<byte> IEnumerable<byte>.GetEnumerator()
        {
            return GetEnumerableImplementation().GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerableImplementation().GetEnumerator();
        }

        #endregion

        #region Overrides

        public override void Dispose()
        {
            if (IsDisposed) return;

            base.Dispose();

            if (ShouldDispose)
            {
                //Call dispose
                First16Bits.Dispose();

                //Remove the reference to the CommonHeaderBits instance
                First16Bits = null;

                //Invalidate the pointer
                PointerToLast10Bytes.Dispose();
                PointerToLast10Bytes = null;

                //Remove the reference to the allocated array.
                Last10Bytes = null;
            }
        }

        /// <summary>
        /// Uses the first 2 bytes and the <see cref="SynchronizationSourceIdentifier"/> to create a HashCode which represents this header.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() { return First16Bits ^ SynchronizationSourceIdentifier; }

        public override bool Equals(object obj)
        {
            if (System.Object.ReferenceEquals(this, obj)) return true;

            if (!(obj is RtpHeader)) return false;

            RtpHeader other = obj as RtpHeader;

            return other.First16Bits == First16Bits
                &&
                other.SynchronizationSourceIdentifier == SynchronizationSourceIdentifier;
        }

        #endregion

        #region Operators

        public static bool operator ==(RtpHeader a, RtpHeader b)
        {
            object boxA = a, boxB = b;
            return boxA == null ? boxB == null : a.Equals(b);
        }

        public static bool operator !=(RtpHeader a, RtpHeader b) { return false == (a == b); }

        #endregion
    }

    #endregion
}

namespace Media.UnitTests
{
    /// <summary>
    /// Provides tests which ensure the logic of the RtpHeader class is correct
    /// </summary>
    internal class RtpHeaderUnitTests
    {
        public static void TestAConstructor_And_Reserialization()
        {
            unchecked
            {
                bool bitValue = false;

                //Test every possible bit packed value that can be valid in the first and second octet
                for (int ibitValue = 0; ibitValue < 2; ++ibitValue)
                {
                    //Make a bitValue after the 0th iteration
                    if (ibitValue > 0) bitValue = Convert.ToBoolean(ibitValue);

                    //Permute every possible value within the 2 bit Version
                    for (byte VersionCounter = 0; VersionCounter <= Media.Common.Binary.TwoBitMaxValue; ++VersionCounter)
                    {
                        //Permute every possible value in the 7 bit PayloadCounter
                        for (int PayloadCounter = 0; PayloadCounter <= sbyte.MaxValue; ++PayloadCounter)
                        {
                            //Permute every possible value in the 4 bit ContributingSourceCounter
                            for (byte ContributingSourceCounter = byte.MinValue; ContributingSourceCounter <= Media.Common.Binary.FourBitMaxValue; ++ContributingSourceCounter)
                            {
                                int RandomId = Utility.Random.Next(), RandomSequenceNumber = Utility.Random.Next(ushort.MinValue, ushort.MaxValue), RandomTimestamp = Utility.Random.Next();

                                using (Rtp.RtpHeader test = new Rtp.RtpHeader(VersionCounter,
                                    !bitValue, !bitValue, !bitValue,
                                    PayloadCounter,
                                    ContributingSourceCounter,
                                    RandomId, RandomSequenceNumber, RandomTimestamp))
                                {
                                    if (test.SynchronizationSourceIdentifier != RandomId) throw new Exception("Unexpected SynchronizationSourceIdentifier");

                                    if (test.SequenceNumber != RandomSequenceNumber) throw new Exception("Unexpected SequenceNumber");

                                    if (test.Timestamp != RandomTimestamp) throw new Exception("Unexpected Timestamp");

                                    if (test.Padding != !bitValue) throw new Exception("Unexpected Padding");

                                    if (test.Extension != !bitValue) throw new Exception("Unexpected Extension");

                                    if (test.Version != VersionCounter) throw new Exception("Unexpected Version");

                                    if (test.PayloadType != PayloadCounter) throw new Exception("Unexpected PayloadType");

                                    if (test.ContributingSourceCount != ContributingSourceCounter) throw new Exception("Unexpected ContributingSourceCounter");

                                    if (test.Count() != test.Size) throw new Exception("Invalid Size given Count");

                                    //Test Serialization from an array and Deserialization from the array

                                    using (Rtp.RtpHeader deserialized = new Rtp.RtpHeader(test.ToArray()))
                                    {
                                        if (test.Padding != deserialized.Padding) throw new Exception("Unexpected BlockCount");

                                        if (test.Version != deserialized.Version) throw new Exception("Unexpected Version");

                                        if (test.PayloadType != deserialized.PayloadType) throw new Exception("Unexpected PayloadType");

                                        if (test.ContributingSourceCount != deserialized.ContributingSourceCount) throw new Exception("Unexpected ContributingSourceCount");

                                        if (test.Timestamp != deserialized.Timestamp) throw new Exception("Invalid Timestamp");

                                        if (test.Size != deserialized.Size) throw new Exception("Unexpected Size");

                                        if (false == test.SequenceEqual(deserialized)) throw new Exception("Unexpected SequenceEqual");

                                        //The HashCode does not depend on the SequenceNumber or Timestamp right now.
                                        if (test.GetHashCode() != deserialized.GetHashCode()) throw new Exception("Unexpected GetHashCode");

                                        ////m_Memory is not == 
                                        if (test.Equals(deserialized)) throw new Exception("Unexpected Equals");

                                    }

                                    //Test IEnumerable constructor if added

                                }
                            }
                        }
                    }
                }
            }
        }
    }
}