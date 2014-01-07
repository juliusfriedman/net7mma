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

#endregion
namespace Media.Common
{
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
    ///     3) You cannot set a remove manually references to a value type or set a structure to null which then causes the GC to maintin the pointer and refrerence count for more time and would lead to more memory leaks.
    ///     4) You can't inherit a struct and subsequently any derived implementation would need to redudantly store reference to something it can't be rid of manually.
    ///     
    /// public to allow derived implementation, hence not sealed.
    /// 
    /// This instance only declares 2 fields which are value types and owns no other references.
    /// </remarks>
    public class CommonHeaderBits : Utility.BaseDisposable, IEnumerable<byte>
    {
        #region Statics and Constants

        /// <summary>
        /// 3 << 6 produces a 8 bit value of 11000000
        /// </summary>
        public const byte VersionMask = 192;

        /// <summary>
        /// 1 << 7 produces a 8 bit value of 1000000 (127) Decimal
        /// </summary>
        public const int RtpMarkerMask = Binary.SevenBitMaxValue;

        /// <summary>
        /// 1 << 5 produces a 8 bit value of 00100000 (32 Decimal)
        /// </summary>
        public const byte PaddingMask = 32;

        /// <summary>
        /// 1 << 4 produces a 8 bit value of 00010000 (16) Decimal
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
        [CLSCompliant(false)]
        public static byte PackOctet(uint version, bool padding, bool extension, uint remainingBits = 0)
        {
            //Ensure the version is valid in a quarter bit
            if (version > 3) throw Binary.QuarterBitOverflow;

            //Check if the value can be packed into an octet.
            if (padding && extension)//Only 4 bits are available if padding and extensions are set
            {
                if (remainingBits > Binary.FiveBitMaxValue) throw new ArgumentException("Padding and Extensions cannot be set when remaining bits is greater than 31");
                remainingBits |= 48;
            }
            else if (padding)// 5 bits are available when Padding is set
            {
                if (remainingBits > Binary.FiveBitMaxValue) throw new ArgumentException("Padding cannot be set when remaining bits is greater than 31");
                remainingBits |= 32;
            }
            else if (extension)// Could be considered to be the sign bit if not utilized when packing (Occupies the 5 bit)
            {
                if (remainingBits > Binary.FourBitMaxValue) throw new ArgumentException("Extensions cannot be set when remaining bits is greater than 15");
                remainingBits |= 16;
            }

            //Pack the results into an octet which is stored the same in all endians.
            return (byte)(version << 6 | (byte)remainingBits);            
        }

        /// <summary>
        /// Provided for CLS Complaince.
        /// <see cref="CommonHeaderBits.PackOctet"/>
        /// </summary>
        /// <param name="version">a 2 bit value, 0, 1, 2 or 3.</param>
        /// <param name="padding">Indicates the value of the 2nd Bit</param>
        /// <param name="extension">Indicates the value of the 3rd Bit</param>
        /// <param name="remainingBits">Bits 4, 5, 6, 7 and 8</param>
        /// <returns>The octet which has been composed as a result of packing the bit fields</returns>
        public static byte PacketOctetCompliant(int version, bool padding, bool extension, int remainingBits)
        {
            return PackOctet((uint)version, padding, extension, (uint)remainingBits);
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
        /// The first and octets themselves, utilized by both Rtp and Rtcp.
        /// Seperated to prevent checks on endian.
        /// </summary>
        protected byte leastSignificant, mostSignificant;

        #endregion

        #region Properties

        /// <summary>
        /// Converts the 16 bits utilized in this implemention into a 32 bit integer
        /// </summary>
        /// <returns>The 32 bit value created as a result of interpreting the 16 bits as a 32 bit value</returns>
        internal int ToInt32()
        {
            //Not using Binary.ReadInteger to save call overhead.
            return Binary.ReadU16(this, 0, BitConverter.IsLittleEndian);
        }

        /// <summary>
        /// Gets or sets bits 0 and 1; from the lowest quartet of the first octet.
        /// Throws a Overflow exception if the value is less than 0 or greater than 3.
        /// </summary>
        public int Version
        {
            //Only 8 bits in a byte, moved 6 right yeilds a base 2 value of 0, 1, 2 or 3
            get { return (byte)(leastSignificant > 0 ? (leastSignificant >> 6) : 0); }
            set
            {
                //Unsigned to prevent 2 checks
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
                leastSignificant = PackOctet(unsigned, Padding, Extension, (byte)RtpContributingSourceCount);
            }
        }

        /// <summary>
        /// Gets or sets the Padding bit.
        /// </summary>
        public bool Padding
        {
            get
            {
                //Example 223 & 32 == 0
                //Where 32 == PaddingMask and 223 == (11011111) Binary and would indicate a version 3 header with no padding, extension set and 15 CC
                return leastSignificant > 0 && (leastSignificant & PaddingMask) > 0;
            }
            internal set
            {
                //Comprise an octet with the required Version, Padding and Extension bit set
                //Where 6 is the amount of unnecessary bits which preceeded the reqired value in the byte
                //leastSignificant = (byte)((byte)Version << 6 | (value ? (PaddingMask) : 0) | (Extension ? (byte)(ExtensionMask) : 0) | RtpContributingSourceCount);
                leastSignificant = PackOctet((byte)Version, value, Extension, (byte)RtpContributingSourceCount);
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
            get { return leastSignificant > 0 && ((byte)(leastSignificant << 3) >> 7) > 0; }
            set
            {
                leastSignificant = PackOctet((byte)Version, Padding, value, (byte)RtpContributingSourceCount);
            }
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
            get { return (byte)((leastSignificant << 3)) >> 3; }
            set
            {
                //Get a unsigned copy to prevent two checks
                byte unsigned = (byte)value;

                if (unsigned > Binary.FiveBitMaxValue)
                    throw Binary.CreateOverflowException("RtcpBlockCount", unsigned, byte.MinValue.ToString(), Binary.FiveBitMaxValue.ToString());

                byte blockCount = (byte)RtcpBlockCount;

                if (blockCount > 0) leastSignificant ^= blockCount;

                leastSignificant |= unsigned;
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
            get { return leastSignificant > 0 ? leastSignificant & 0x0f : 0; }
            internal set
            {
                byte unsigned = (byte)value;

                //If the value exceeds the highest value which can be stored in the bit field throw an overflow exception
                if (unsigned > Binary.FourBitMaxValue)
                    throw Binary.CreateOverflowException("RtpContributingSourceCount", unsigned, byte.MinValue.ToString(), Binary.FourBitMaxValue.ToString());

                leastSignificant = PackOctet((uint)Version, Padding, Extension, unsigned);
            }
        }

        /// <summary>
        /// Gets or sets the RtpMarker bit.
        /// </summary>
        public bool RtpMarker
        {
            get { return mostSignificant > 0 && (mostSignificant >> 7) > 0; }
            set { mostSignificant = PackOctet(value, (byte)RtpPayloadType); }
        }

        /// <summary>
        /// Gets or sets the 7 bit value associated with the RtpPayloadType.
        /// </summary>
        public int RtpPayloadType
        {
            get { return mostSignificant > 0 ? (byte)((mostSignificant << 1)) >> 1 : 0; }
            set
            {
                //Get an unsigned copy of the value to prevent 2 checks 
                byte unsigned = (byte)value;

                //If the value exceeds the highest value which can be stored in the bit field throw an overflow exception
                if (unsigned > Binary.SevenBitMaxValue)
                    throw Binary.CreateOverflowException("RtpPayloadType", unsigned, byte.MinValue.ToString(), sbyte.MaxValue.ToString());

                mostSignificant = PackOctet(RtpMarker, (byte)unsigned);
            }
        }

        /// <summary>
        /// Gets or sets the 8 bit value associated with the RtcpPayloadType.
        /// Note that in RtpPackets that this field is shared with the Marker bit and if the value has Bit 0 set then the RtpMarker property will be true.
        /// </summary>
        public int RtcpPayloadType
        {
            get { return mostSignificant; }
            set { mostSignificant = (byte)value; } //Check Marker before setting?
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
        /// Constructs a managed representation around the given two octets
        /// </summary>
        /// <param name="lsb">The least significant 8 bits</param>
        /// <param name="msb">The most significant 8 bits</param>
        public CommonHeaderBits(byte lsb, byte msb)
        {
            //Assign them

            leastSignificant = lsb;

            mostSignificant = msb;
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
            leastSignificant = CommonHeaderBits.PackOctet((uint)version, padding, extension);
        }

        /// <summary>
        /// Constructs a new instance of the CommonHeaderBits with the given values packed into the bit fields.
        /// The <paramref name="payloadTypeBits"/> usually refer to count of Contributing Sources and will be stored in the managed <propertyref name="RtpContributingSourceCount"/> property.
        /// </summary>
        /// <param name="version">The version of the common header bits</param>
        /// <param name="padding">The value of the Padding bit</param>
        /// <param name="extension">The value of the Extension bit</param>
        /// <param name="marker">The value of the Marker bit</param>
        /// <param name="payloadTypeBits">The value of the remaning bits which are not utilized. (4 bits)</param>
        public CommonHeaderBits(int version, bool padding, bool extension, bool marker, int payloadTypeBits, byte otherBits)
            : this(version, padding, extension)
        {
            //Pack the bit fields in the second octet which belong there
            mostSignificant = CommonHeaderBits.PackOctet(marker, (payloadTypeBits | otherBits));
        }

        #endregion

        #region IEnumerator Implementations

        public IEnumerator<byte> GetEnumerator()
        {
            return Enumerable.Concat<byte>(leastSignificant.Yield(), mostSignificant.Yield()).GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            //Duplicated logic here to save casting
            return Enumerable.Concat<byte>(leastSignificant.Yield(), mostSignificant.Yield()).GetEnumerator();
        }

        #endregion

        #region Implicit Operators
        //(u)short / byte[] ?
        #endregion
    }

    #endregion
}
