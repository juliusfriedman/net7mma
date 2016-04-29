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
    #region RtcpHeader

    #region Reference

    /* Copied from http://tools.ietf.org/html/rfc3550#section-6.4
         
           Schulzrinne, et al.         Standards Track                    [Page 35]
           FFC 3550                          RTP                          July 2003
         
         6.4.1 SR: Sender Report RTCP Packet

                  0                   1                   2                   3
                0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
               +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
               |V=2|P|    RC   |   PT=SR=200   |             length            |
               +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
         */

    #endregion

    /// <summary>
    /// Provides a managed abstraction around the first 4 octets of any RtcpPacket.
    /// Futher information can be found at http://tools.ietf.org/html/rfc3550#section-6.4.1
    /// Note in certain situations the <see cref="CommonHeaderBits.RtcpBlockCount"/> is used for application specific purposes.
    /// </summary>
    public class RtcpHeader : BaseDisposable, IEnumerable<byte>
    {
        #region Constants and Statics

        /// <summary>
        /// The length of every RtcpHeader. (In bytes) 
        /// </summary>
        public const int Length = Common.Binary.BytesPerInteger; //Notes Size and Length could be swapped because Length is more instance orientated.

        /// <summary>
        /// The value which is placed into the <see cref="RtcpHeader.LengthInWordsMinusOne"/> by default when creating a RtcpReport.
        /// Note that a value of 0 = 4 bytes and a value of 1 = 8 bytes.
        /// </summary>
        public const int DefaultLengthInWords = 1;

        /// <summary>
        /// The maximum value which can appear in the <see cref="RtcpHeader.LengthInWordsMinusOne"/>
        /// Usually indicates a header only, 65535 + 1 = 0, 4 bytes
        /// </summary>
        public const int MaximumLengthInWords = ushort.MaxValue;

        /// <summary>
        /// The minimum value which can appear in the <see cref="RtcpHeader.LengthInWordsMinusOne"/>
        /// Usually indicates header and a ssrc but is technically equal to <see cref="MaximumLengthInWords"/>
        /// </summary>
        internal const int MinimumLengthInWords = ushort.MinValue;

        #endregion

        #region Fields

        /// <summary>
        /// A managed abstraction of the first two octets, 16 bits of the RtcpHeader.
        /// </summary>
        internal Media.RFC3550.CommonHeaderBits First16Bits;

        /// <summary>
        /// The last six octets of the RtcpHeader which contain the length in 32 bit words and the SSRC/CSRC of the sender of this RtcpHeader
        /// </summary>
        internal byte[] Last6Bytes;

        //Better name, the pointer is like + IntPtr.Size * 2 from there into the Rtti
        internal Common.MemorySegment PointerToLast6Bytes;

        #endregion

        #region Properties

        public bool IsCompressed
        {
            get { return Size < Length; } //E.g. Size < Length would look better as Length < Size ... :p
        } 

        /// <summary>
        /// Creates a 32 bit value which can be used to detect validity of the RtcpHeader when used in conjunction with the CreateRtcpValidMask function.
        /// </summary>
        /// <returns>The 32 bit value which is interpreted as a result of reading the RtcpHeader as a 32bit integer</returns>
        internal int ToInt32()
        {
            //Create a 32 bit system endian value
            return (int)Common.Binary.ReadU32(this, 0, BitConverter.IsLittleEndian);
        }

        /// <summary>
        /// Indicates if the RtcpPacket is valid by checking the header for the given parameters.
        /// </summary>
        public bool IsValid(int? version = 0, int? payloadType = 0, bool? padding = false)
        {
            if (version.HasValue && version != Version) return false;

            if (payloadType.HasValue && payloadType != PayloadType) return false;

            if (padding.HasValue && Padding != padding) return false;

            return true;
        }

        /// <summary>
        /// Gets the Version bit field of the RtpHeader
        /// </summary>
        public int Version
        {
            get { /*CheckDisposed();*/ return First16Bits.Version; }
            set { /*CheckDisposed();*/ First16Bits.Version = value; }
        }

        /// <summary>
        /// Indicates if the Padding bit is set in the first octet.
        /// </summary>
        public bool Padding
        {
            get { /*CheckDisposed();*/ return First16Bits.Padding; }
            set { /*CheckDisposed();*/ First16Bits.Padding = value; }
        }

        /// <summary>
        /// Gets or sets the 5 bit value associated with the RtcpBlockCount field in the RtpHeader.
        /// </summary>
        public int BlockCount
        {
            get { /*CheckDisposed();*/ return First16Bits.RtcpBlockCount; }
            set { /*CheckDisposed();*/ First16Bits.RtcpBlockCount = value; }
        }

        /// <summary>
        /// Indicates the format of the data within the Payload
        /// </summary>
        public int PayloadType
        {
            //The value is revealed by clearing the 0th bit in the second octet.
            get { /*CheckDisposed();*/ return First16Bits.RtcpPayloadType; }
            set { /*CheckDisposed();*/ First16Bits.RtcpPayloadType = value; }
        }

        /// <summary>
        /// The length in 32 bit words (Minus One). Including any padding. 
        /// When equal to ushort.MaxValue or ushort.MinValue there is no payload.
        /// </summary>
        public int LengthInWordsMinusOne
        {
            /* Copied from http://tools.ietf.org/html/rfc3550#section-6.4.1
             
        length: 16 bits
            
          The length of this RTCP packet in 32-bit words minus one,
          including the header and any padding.  (The offset of one makes
          zero a valid length and avoids a possible infinite loop in
          scanning a compound RTCP packet, while counting 32-bit words
          avoids a validity check for a multiple of 4.)             
         */

            get
            {
                /*CheckDisposed();*/

                //Read the value
                return Binary.ReadU16(PointerToLast6Bytes.Array, PointerToLast6Bytes.Offset, BitConverter.IsLittleEndian);
            }
            //Set the value
            set
            {
                /*CheckDisposed();*/

                //Write the value
                if (value > RtcpHeader.MinimumLengthInWords) Binary.CreateOverflowException("LengthInWordsMinusOne", value, ushort.MinValue.ToString(), ushort.MaxValue.ToString());

                Binary.Write16(PointerToLast6Bytes.Array, PointerToLast6Bytes.Offset, BitConverter.IsLittleEndian, (ushort)value);
            }
        }

        //Todo should be on rtcpPacket.
        /// <summary>
        /// The ID of the participant who sent this SendersInformation if <see cref="LengthInWordsMinusOne"/> is not <see cref="ushort.MaxValue"/> and at least 6 bytes are contained in the header.
        /// </summary>
        /// <notes><see cref="PointerToLast6Bytes"/>.Count MUST be >= 6 for a SSRC to occur in the header.</notes>
        public int SendersSynchronizationSourceIdentifier
        {
            get
            { 

                /*CheckDisposed();*/

                switch (LengthInWordsMinusOne)
                {
                    //case RtcpHeader.MinimumLengthInWords:
                    case RtcpHeader.MaximumLengthInWords: // -
                        return 0;
                    default: return (int)Binary.ReadU32(PointerToLast6Bytes.Array, PointerToLast6Bytes.Offset + 2, BitConverter.IsLittleEndian);
                }

               //return (int)Binary.ReadU32(PointerToLast6Bytes.Array, PointerToLast6Bytes.Offset + 2, BitConverter.IsLittleEndian);
            }
            set
            { 
                /*CheckDisposed();*/

                Binary.Write32(PointerToLast6Bytes.Array, PointerToLast6Bytes.Offset + 2, BitConverter.IsLittleEndian, (uint)value);

                //If there was no words in the packet (other than the header itself) than indicate another word is present.
                switch (LengthInWordsMinusOne)
                {
                    case RtcpHeader.MinimumLengthInWords://Was 0 + 1
                    case RtcpHeader.MaximumLengthInWords://Was FFFF + 0
                        LengthInWordsMinusOne = RtcpHeader.DefaultLengthInWords;
                        return;
                }
            }
        }

        /// <summary>
        /// The amount of bytes this instance would occupy when serialized
        /// </summary>
        public int Size
        {
            get
            {
                if (IsDisposed) return 0;

                switch (LengthInWordsMinusOne)
                {
                    case RtcpHeader.MinimumLengthInWords:
                    case RtcpHeader.MaximumLengthInWords:
                        return RtcpHeader.Length;
                    default: return Binary.BytesPerLong; //return RFC3550.CommonHeaderBits.Size + PointerToLast6Bytes.Count;
                }
            }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Reads an instance of the RtcpHeader class and copies octets which make up the RtcpHeader.
        /// </summary>
        /// <param name="octets">A reference to a byte array which contains at least 4 octets to copy.</param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public RtcpHeader(byte[] octets, int offset = 0)
        {
            //If the octets reference is null throw an exception
            if (octets == null) throw new ArgumentNullException("octets");

            //Determine the length of the array
            int octetsLength = octets.Length, availableOctets = octetsLength - offset;

            //Check range
            if (offset > octetsLength) throw new ArgumentOutOfRangeException("offset", "Cannot be greater than the length of octets");

            //Check for the amount of octets required to build a RtcpHeader given by the delination of the offset
            if (octetsLength == 0 || availableOctets < RtcpHeader.Length) throw new ArgumentException("octets must contain at least 4 elements given the deleniation of the offset parameter.", "octets");

            //Read a managed representation of the first two octets which are stored in Big ByteOrder / Network Byte Order
            First16Bits = new Media.RFC3550.CommonHeaderBits(octets[offset + 0], octets[offset + 1]);

            //Allocate space for the other 6 octets which consist of the 
            //LengthInWordsMinusOne (16 bits)
            //SynchronizationSourceIdentifier (32 bits)
            // 48 Bits = 6 bytes
            Last6Bytes = new byte[6];
            //Copy the remaining bytes of the header which consist of the aformentioned properties

            //If the LengthInWords is FFFF then this is extreanous and probably belongs to any padding...

            Array.Copy(octets, offset + RFC3550.CommonHeaderBits.Size, Last6Bytes, 0, Binary.Min(6, availableOctets - RFC3550.CommonHeaderBits.Size));

            //Make a pointer to the last 6 bytes
            PointerToLast6Bytes = new Common.MemorySegment(Last6Bytes, 0, 6);
        }

        /// <summary>
        /// Creates an exact copy of the RtpHeader from the given RtpHeader
        /// </summary>
        /// <param name="other">The RtpHeader to copy</param>
        /// <param name="reference">A value indicating if the RtpHeader given should be referenced or copied.</param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public RtcpHeader(RtcpHeader other, bool reference)
        {
            if (reference)
            {
                First16Bits = other.First16Bits;
                
                Last6Bytes = other.Last6Bytes;
                
                PointerToLast6Bytes = other.PointerToLast6Bytes;
            }
            else
            {
                First16Bits = new Media.RFC3550.CommonHeaderBits(other.First16Bits);
                
                Last6Bytes = new byte[6];
                
                PointerToLast6Bytes = new Common.MemorySegment(Last6Bytes, 0, 6);

                if (other.Last6Bytes != null)
                {
                    other.Last6Bytes.CopyTo(Last6Bytes, 0);
                }
                else
                {
                    System.Array.Copy(other.PointerToLast6Bytes.Array, other.PointerToLast6Bytes.Offset, Last6Bytes, 0, 6);
                }
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public RtcpHeader(Common.MemorySegment memory)//, int additionalOffset = 0) 
        {
            First16Bits = new Media.RFC3550.CommonHeaderBits(memory);//, additionalOffset);

            PointerToLast6Bytes = new Common.MemorySegment(memory.Array, memory.Offset + RFC3550.CommonHeaderBits.Size, Binary.Clamp(memory.Count - RFC3550.CommonHeaderBits.Size, 0, 6));
        }

        //Todo overloads when CommonHeaderBits exist...

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public RtcpHeader(int version, int payloadType, bool padding, int blockCount)
        {
            First16Bits = new Media.RFC3550.CommonHeaderBits(version, padding, false, false, payloadType, (byte)blockCount);
            
            Last6Bytes = new byte[6];
            
            PointerToLast6Bytes = new Common.MemorySegment(Last6Bytes, 0, 6);
            
            //The default value must be set into the LengthInWords field otherwise it will reflect 0
            if(blockCount == 0) LengthInWordsMinusOne = RtcpHeader.MaximumLengthInWords; // ushort (0 - 1)
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public RtcpHeader(int version, int payloadType, bool padding, int blockCount, int ssrc)
            : this(version, payloadType, padding, blockCount)
        {
            SendersSynchronizationSourceIdentifier = ssrc;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public RtcpHeader(int version, int payloadType, bool padding, int blockCount, int ssrc, int lengthInWordsMinusOne)
            : this(version, payloadType, padding, blockCount, ssrc)
        {
            LengthInWordsMinusOne = lengthInWordsMinusOne;
        }

        #endregion

        #region Instance Methods

        public int CopyTo(byte[] dest, int offset)
        {
            if (IsDisposed) return 0;

            int copied = First16Bits.CopyTo(dest, offset);

            offset += copied;

            Common.MemorySegmentExtensions.CopyTo(PointerToLast6Bytes, dest, offset);

            copied += PointerToLast6Bytes.Count;

            return copied;
        }

        /// <summary>
        /// Creates a sequence containing  only the octets of the <see cref="SendersSynchronizationSourceIdentifier"/>. 
        /// </summary>
        /// <returns>The sequence created</returns>
        internal IEnumerable<byte> GetSendersSynchronizationSourceIdentifierSequence()
        {
            switch (LengthInWordsMinusOne)
            {
                case RtcpHeader.MinimumLengthInWords:
                case RtcpHeader.MaximumLengthInWords:
                    return Common.MemorySegment.EmptyBytes;
                default: return PointerToLast6Bytes.Skip(RFC3550.CommonHeaderBits.Size);
            }
            //int lengthInWords = LengthInWordsMinusOne;
            //return PointerToLast6Bytes.Count >= 6 && lengthInWords != 0 && lengthInWords != ushort.MaxValue ? PointerToLast6Bytes.Skip(RFC3550.CommonHeaderBits.Size) : Media.Common.MemorySegment.EmptyBytes;
        }

        /// <summary>
        /// Clones this RtcpHeader instance.
        /// If reference is true any changes performed in either this instance or the new instance will be reflected in both instances.
        /// </summary>
        /// <param name="reference">indictes if the new instance should reference this instance.</param>
        /// <returns>The new instance</returns>
        public RtcpHeader Clone(bool reference = false) { return new RtcpHeader(this, reference); }

        internal IEnumerable<byte> GetEnumerableImplementation()
        {
            switch (LengthInWordsMinusOne)
            {
                    //Value 0 means there is 65535 words.... this should return any values present... (as default does)
                case RtcpHeader.MinimumLengthInWords:
                case RtcpHeader.MaximumLengthInWords:
                    return Enumerable.Concat<byte>(First16Bits, PointerToLast6Bytes.Take(RFC3550.CommonHeaderBits.Size));
                default:
                     return Enumerable.Concat<byte>(First16Bits, PointerToLast6Bytes);
            }
        }

        #endregion

        IEnumerator<byte> IEnumerable<byte>.GetEnumerator()
        {
            return GetEnumerableImplementation().GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerableImplementation().GetEnumerator();
        }

        #region Overrides

        public override void Dispose()
        {

            base.Dispose();

            if (ShouldDispose)
            {
                //Dispose the instance
                First16Bits.Dispose();

                //Remove the reference to the CommonHeaderBits instance
                First16Bits = null;

                //Invalidate the pointer
                PointerToLast6Bytes.Dispose();

                PointerToLast6Bytes = null;

                //Remove the reference to the allocated array.
                Last6Bytes = null;
            }
        }

        public override int GetHashCode() { return First16Bits ^ SendersSynchronizationSourceIdentifier; }

        public override bool Equals(object obj)
        {
            if(System.Object.ReferenceEquals(this, obj)) return true;

            if (false == (obj is RtcpHeader)) return false;

            RtcpHeader other = obj as RtcpHeader;

            return other.First16Bits == First16Bits
                &&
                other.SendersSynchronizationSourceIdentifier == SendersSynchronizationSourceIdentifier;
        }

        #endregion

        #region Operators

        public static bool operator ==(RtcpHeader a, RtcpHeader b)
        {
            object boxA = a, boxB = b;
            return boxA == null ? boxB == null : a.Equals(b);
        }

        public static bool operator !=(RtcpHeader a, RtcpHeader b) { return false == (a == b); }

        #endregion

    }

    #endregion
}

namespace Media.UnitTests
{
    /// <summary>
    /// Provides tests which ensure the logic of the RtcpHeader class is correct
    /// </summary>
    internal class RtcpHeaderUnitTests
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
                        for (int PayloadCounter = 0; PayloadCounter <= byte.MaxValue; ++PayloadCounter)
                        {
                            //Permute every possible value in the 5 bit BlockCount
                            for (byte ReportBlockCounter = byte.MinValue; ReportBlockCounter <= Media.Common.Binary.FiveBitMaxValue; ++ReportBlockCounter)
                            {
                                //Permute every necessary value in the 16 bit LengthInWordsMinusOne 65535, 0 -> 8
                                for (ushort lengthIn32BitWords = ushort.MaxValue; lengthIn32BitWords == ushort.MaxValue || lengthIn32BitWords <= Media.Common.Binary.BitsPerByte; ++lengthIn32BitWords)
                                {
                                    //Always specify a value for the ssrc, if the length is 65535 this means there is no ssrc...
                                    using (Rtcp.RtcpHeader test = new Rtcp.RtcpHeader(VersionCounter, PayloadCounter, bitValue, ReportBlockCounter, 7, lengthIn32BitWords))
                                    {
                                        //Should possibly allow for this ...
                                        if (lengthIn32BitWords != ushort.MaxValue && test.SendersSynchronizationSourceIdentifier != 7) throw new Exception("Unexpected SendersSynchronizationSourceIdentifier");

                                        if (test.Padding != bitValue) throw new Exception("Unexpected BlockCount");

                                        if (test.Version != VersionCounter) throw new Exception("Unexpected Version");

                                        if (test.PayloadType != PayloadCounter) throw new Exception("Unexpected PayloadType");

                                        if (test.BlockCount != ReportBlockCounter) throw new Exception("Unexpected BlockCount");

                                        if (lengthIn32BitWords != test.LengthInWordsMinusOne ||
                                            lengthIn32BitWords + 1 * 4 != test.LengthInWordsMinusOne + 1 * 4) throw new Exception("Invalid LengthInWordsMinusOne");

                                        if (test.Count() != test.Size) throw new Exception("Invalid Size given Count");

                                        //Test Serialization from an array and Deserialization from the array

                                        using (Rtcp.RtcpHeader deserialized = new Rtcp.RtcpHeader(test.ToArray()))
                                        {
                                            if (test.SendersSynchronizationSourceIdentifier != 0 &&
                                                test.Size > Rtcp.RtcpHeader.Length &&
                                                test.SendersSynchronizationSourceIdentifier != deserialized.SendersSynchronizationSourceIdentifier) throw new Exception("Unexpected SendersSynchronizationSourceIdentifier");

                                            if (test.Padding != deserialized.Padding) throw new Exception("Unexpected BlockCount");

                                            if (test.Version != deserialized.Version) throw new Exception("Unexpected Version");

                                            if (test.PayloadType != deserialized.PayloadType) throw new Exception("Unexpected PayloadType");

                                            if (test.BlockCount != deserialized.BlockCount) throw new Exception("Unexpected BlockCount");

                                            if (test.LengthInWordsMinusOne != deserialized.LengthInWordsMinusOne) throw new Exception("Invalid LengthInWordsMinusOne");

                                            if (test.Size != deserialized.Size) throw new Exception("Unexpected Size");

                                            //Should possibly allow for this ...
                                            if (test.SendersSynchronizationSourceIdentifier == deserialized.SendersSynchronizationSourceIdentifier
                                                &&
                                                test.GetHashCode() != deserialized.GetHashCode()) throw new Exception("Unexpected GetHashCode");
                                            else if (deserialized.Size != test.Size) throw new Exception("Unexpected Size");
                                            
                                            //m_Memory is not == 
                                            if (test.Equals(deserialized)) throw new Exception("Unexpected Equals");
                                        }

                                        //Test IEnumerable constructor if added

                                        //using (Rtcp.RtcpHeader deserialized = new Rtcp.RtcpHeader(test.GetEnumerableImplementation()))
                                        //{
                                        //    if (test.Padding != deserialized.Padding) throw new Exception("Unexpected BlockCount");

                                        //    if (test.Version != deserialized.Version) throw new Exception("Unexpected Version");

                                        //    if (test.PayloadType != deserialized.PayloadType) throw new Exception("Unexpected PayloadType");

                                        //    if (test.BlockCount != deserialized.BlockCount) throw new Exception("Unexpected BlockCount");

                                        //    if (test.LengthInWordsMinusOne != deserialized.LengthInWordsMinusOne) throw new Exception("Invalid LengthInWordsMinusOne");

                                        //    if (test.Count() != deserialized.Size) throw new Exception("Invalid Size");
                                        //}

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