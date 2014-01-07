#region Copyright
/*
Copyright (c) 2013 juliusfriedman@gmail.com
  
 SR. Software Engineer ASTI Transportation Inc.

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
    public class RtcpHeader : Utility.BaseDisposable, IEnumerable<byte>
    {
        #region Constants and Statics

        /// <summary>
        /// The length of every RtcpHeader.
        /// </summary>
        public const int Length = 8;

        /// <summary>
        /// Based on the information given @ http://tools.ietf.org/html/rfc3550#appendix-A.
        /// Calculates a mask and value which can be used to validate a RtcpHeader of any version and any payloadType
        /// </summary>
        /// <param name="version">The version of the RtcpPacket</param>
        /// <param name="payloadType">The <see cref="RtcpPacket.PayloadType"/> of the RtcpPacket</param>
        /// <param name="blockCount">The <see cref="RtcpPacket.BlockCount"/> of the RtcpPacket</param>
        /// <param name="validValue">The place to store the valid value</param>
        /// <returns>Calculates a mask which can validate a RtcpHeader.</returns>
        public static int CreateRtcpValidMask(int version, int payloadType, out int validValue)
        {
            //Mask consists of the version, padding and payloadType
            int mask = (version << 14) | 0x2000 | payloadType;
            //Value consists of only the version and payloadType
            validValue = (version << 14) | (byte)payloadType;
            return mask;
        }

        #endregion

        #region Fields

        /// <summary>
        /// A managed abstraction of the first two octets, 16 bits of the RtcpHeader.
        /// </summary>
        CommonHeaderBits First16Bits;

        /// <summary>
        /// The last six octets of the RtcpHeader which contain the length in 32 bit words and the SSRC/CSRC of the sender of this RtcpHeader
        /// </summary>
        byte[] Last6Bytes;

        #endregion

        #region Properties

        /// <summary>
        /// Creates a 32 bit value which can be used to detect validity of the RtcpHeader when used in conjunction with the CreateRtcpValidMask function.
        /// </summary>
        /// <returns>The 32 bit value which is interpreted as a result of reading the RtcpHeader as a 32bit integer</returns>
        internal int ToInt32() { return First16Bits.ToInt32(); }

        /// <summary>
        /// Indicates if the RtcpPacket is valid by checking the header using a variation of the alorigthm given in http://tools.ietf.org/html/rfc3550#appendix-A.2
        /// </summary>
        public bool IsValid(int? version = 0, int? payloadType = 0)
        {
            //Store a value which is given from the CreateRtcpValidMask function.
            int validValue,
                mask = CreateRtcpValidMask(version ?? Version, payloadType ?? PayloadType, out validValue); //Create the mask based on the Version field in the RtcpHeader

            /* NOTE THIS IS NOT indicated in http://tools.ietf.org/html/rfc3550#appendix-A.2 because the version is given dynamically
             * 
              The following checks should be applied to RTCP packets.

               o  RTP version field must equal 2. ** In this implementation version is given as a parameter **

               o  The payload type field of the first RTCP packet in a compound
                  packet must be equal to SR or RR. *** This logic is performed in the FromCompoundBytes function

               o  The padding bit (P) should be zero for the first packet of a
                  compound RTCP packet because padding should only be applied, if it
                  is needed, to the last packet. *** This logic is performed in the FromCompoundBytes function

               o  The length fields of the individual RTCP packets must add up to
                  the overall length of the compound RTCP packet as received.  This
                  is a fairly strong check. *** This logic is performed in the FromCompoundBytes function
             */

            //The RtcpHeader is valid if the bits if the resulting mask are calulcated to match the valid value.
            return (ToInt32() & mask) == validValue;
        }

        /// <summary>
        /// Gets the Version bit field of the RtpHeader
        /// </summary>
        public int Version
        {
            get { CheckDisposed(); return First16Bits.Version; }
            set { CheckDisposed(); First16Bits.Version = value; }
        }

        /// <summary>
        /// Indicates if the Padding bit is set in the first octet.
        /// </summary>
        public bool Padding
        {
            get { CheckDisposed(); return First16Bits.Padding; }
            set { CheckDisposed(); First16Bits.Padding = value; }
        }

        /// <summary>
        /// Gets or sets the 5 bit value associated with the RtcpBlockCount field in the RtpHeader.
        /// </summary>
        public int BlockCount
        {
            get { CheckDisposed(); return First16Bits.RtcpBlockCount; }
            set { CheckDisposed(); First16Bits.RtcpBlockCount = value; }
        }

        /// <summary>
        /// Indicates the format of the data within the Payload
        /// </summary>
        public int PayloadType
        {
            //The value is revealed by clearing the 0th bit in the second octet.
            get { CheckDisposed(); return First16Bits.RtcpPayloadType; }
            set { CheckDisposed(); First16Bits.RtcpPayloadType = value; }
        }

        /// <summary>
        /// The length in 32 bit words (Minus One).
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
                CheckDisposed();

                //Read the value
                return Binary.ReadU16(Last6Bytes, 0, true);
            }
            //Set the value
            set
            {
                CheckDisposed();

                if (value > ushort.MaxValue) Binary.CreateOverflowException("LengthInWordsMinusOne", value, ushort.MinValue.ToString(), ushort.MaxValue.ToString());
                Binary.Write16(Last6Bytes, 0, BitConverter.IsLittleEndian, (ushort)value);
            }
        }

        /// <summary>
        /// The ID of the participant who sent this SendersInformation
        /// </summary>
        public int SendersSynchronizationSourceIdentifier
        {
            get { CheckDisposed(); return (int)Binary.ReadU32(Last6Bytes, 2, BitConverter.IsLittleEndian); }
            set { CheckDisposed(); Binary.Write32(Last6Bytes, 2, BitConverter.IsLittleEndian, (uint)value); }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Reads an instance of the RtpHeader class and copies 12 octets which make up the RtpHeader.
        /// </summary>
        /// <param name="octets">A reference to a byte array which contains at least 12 octets to copy.</param>
        public RtcpHeader(byte[] octets, int offset = 0)
        {
            //If the octets reference is null throw an exception
            if (octets == null) throw new ArgumentNullException("octets");

            //Determine the length of the array
            int octetsLength = octets.Length;

            //Check range
            if (offset > octetsLength) throw new ArgumentOutOfRangeException("offset", "Cannot be greater than the length of octets");

            //Check for the amount of octets required to build a RtcpHeader given by the delination of the offset
            if (octetsLength == 0 || octetsLength - offset < 4) throw new ArgumentException("octets must contain at least 4 elements given the deleniation of the offset parameter.", "octets");

            //Read a managed representation of the first two octets which are stored in Big Endian / Network Byte Order
            First16Bits = new CommonHeaderBits(octets[offset + 0], octets[offset + 1]);

            //Allocate space for the other 6 octets  which consist of the 
            //LengthInWordsMinusOne (16 bits)
            //SynchronizationSourceIdentifier (32 bits)
            Last6Bytes = new byte[6];

            //Copy the remaining bytes of the header which consist of the aformentioned properties
            Array.Copy(octets, offset + 2, Last6Bytes, 0, Math.Min(6, octetsLength - 2));
        }

        /// <summary>
        /// Creates an exact copy of the RtpHeader from the given RtpHeader
        /// </summary>
        /// <param name="other">The RtpHeader to copy</param>
        /// <param name="reference">A value indicating if the RtpHeader given should be referenced or copied.</param>
        public RtcpHeader(RtcpHeader other, bool reference)
        {
            if (reference)
            {
                First16Bits = other.First16Bits;
                Last6Bytes = other.Last6Bytes;
            }
            else
            {
                First16Bits = new CommonHeaderBits(other.First16Bits);
                Last6Bytes = new byte[6];
                other.Last6Bytes.CopyTo(Last6Bytes, 0);
            }
        }

        public RtcpHeader(int version, int payloadType, bool padding, int blockCount)
        {
            First16Bits = new CommonHeaderBits(version, padding, false, false, payloadType, (byte)blockCount);
            Last6Bytes = new byte[6];

            //The default value must be set into the LengthInWords field otherwise it will reflect 1
            LengthInWordsMinusOne = 0;
        }

        public RtcpHeader(int version, int payloadType, bool padding, int blockCount, int ssrc)
            : this(version, payloadType, padding, blockCount)
        {
            SendersSynchronizationSourceIdentifier = ssrc;
        }

        public RtcpHeader(int version, int payloadType, bool padding, int blockCount, int ssrc, int lengthInWords)
            : this(version, payloadType, padding, blockCount, ssrc)
        {
            LengthInWordsMinusOne = lengthInWords;
        }

        #endregion

        #region Instance Methods

        /// <summary>
        /// Creates a sequence containing  only the octets of the <see cref="SendersSynchronizationSourceIdentifier"/>.        
        /// </summary>
        /// <returns>The sequence created</returns>
        /// <remarks>
        /// Provided because the function is performed more than once and creating a seqeunce of the entire header when only the last 4 octets are requried is wasteful.
        /// </remarks>
        internal IEnumerable<byte> GetSendersSynchronizationSourceIdentifierSequence()
        {
            return Last6Bytes.Skip(2);
        }

        public override void Dispose()
        {

            if (Disposed) return;

            base.Dispose();

            //Dispose the instance
            First16Bits.Dispose();

            //Remove the reference to the CommonHeaderBits instance
            First16Bits = null;

            //Remove the reference to the allocated array.
            Last6Bytes = null;
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
             return Enumerable.Concat<byte>(First16Bits, Last6Bytes);
        }

        #endregion


        

        IEnumerator<Octet> IEnumerable<Octet>.GetEnumerator()
        {
            return GetEnumerableImplementation().GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerableImplementation().GetEnumerator();
        }
    }

    #endregion
}
