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

//See
//http://tools.ietf.org/html/rfc5285#page-5

namespace Media.Rtp
{
    #region RtpExtension

    /// <summary>
    /// Provides a managed implementation around the RtpExtension information which would be present in a RtpPacket only if the Extension bit is set.
    /// Marked IDisposable for derived implementations and to indicate when the implementation is no longer required.
    /// </summary>
    [CLSCompliant(false)]
    public class RtpExtension : BaseDisposable, IEnumerable<byte>
    {
        #region Constants And Statics

        public const int MinimumSize = 4;

        public static InvalidOperationException InvalidExtension = new InvalidOperationException(string.Format("The given array does not contain the required amount of elements ({0}) to create a RtpExtension.", MinimumSize));

        #endregion

        #region Fields

        /// <summary>
        /// Reference to the binary data which is thought to contain the RtpExtension.
        /// </summary>
        readonly Common.MemorySegment m_MemorySegment;

        #endregion

        #region Properties

        /// <summary>
        /// Gets a the 16 bit field which is able to be stored in any RtpPacketPayload if the Extension bit is set.
        /// </summary>
        public ushort Flags
        {
            get { if (Disposed) return 0; return Binary.ReadU16(m_MemorySegment.Array, m_MemorySegment.Offset, BitConverter.IsLittleEndian); }
            protected set { if (Disposed) return; Binary.WriteNetwork16(m_MemorySegment.Array, m_MemorySegment.Offset, BitConverter.IsLittleEndian, value); }
        }

        /// <summary>
        /// Gets a count of the amount of 32 bit words which are required completely read this extension.
        /// </summary>
        public ushort LengthInWords
        {
            get { if (Disposed) return 0; return Binary.ReadU16(m_MemorySegment.Array, m_MemorySegment.Offset + 2, BitConverter.IsLittleEndian); }
            protected set { if (Disposed) return; Binary.WriteNetwork16(m_MemorySegment.Array, m_MemorySegment.Offset + 2, BitConverter.IsLittleEndian, value); }
        }

        /// <summary>
        /// Gets the binary data of the RtpExtension.
        /// Note that the data may not be complete, <see cref="RtpExtension.IsComplete"/>
        /// </summary>
        public IEnumerable<byte> Data
        {
            get { if (Disposed) return Utility.Empty; return m_MemorySegment.Array.Skip(m_MemorySegment.Offset + MinimumSize).Take(Math.Min(LengthInWords * 4, m_MemorySegment.Count)); }
        }

        /// <summary>
        /// Gets a value indicating if there is enough binary data required for the length indicated in the `LengthInWords` property.
        /// </summary>
        public bool IsComplete { get { if (Disposed) return false; return m_MemorySegment.Count == Size; } }

        /// <summary>
        /// Gets the size in bytes of this RtpExtension including the Flags and LengthInWords fields.
        /// </summary>
        public int Size { get { if (Disposed) return 0; return (ushort)(MinimumSize + LengthInWords * 4); } }

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new RtpExtension from the given options and optional data.
        /// </summary>
        /// <param name="sizeInBytes">The known size of the RtpExtension in bytes. The LengthInWords property will reflect this value divided by 4.</param>
        /// <param name="data">The optional extension data itself not including the Flags or LengthInWords fields.</param>
        /// <param name="offset">The optional offset into data to being copying.</param>
        public RtpExtension(int sizeInBytes, ushort flags = 0, byte[] data = null, int offset = 0)
        {
            //Allocate memory for the binary
            m_MemorySegment = new Common.MemorySegment(new byte[MinimumSize + sizeInBytes], 0, MinimumSize + sizeInBytes);

            //If there are any flags set them
            if (flags > 0) Flags = flags;

            //If there is any Extension data then set the LengthInWords field
            if (sizeInBytes > 0) LengthInWords = (ushort)(sizeInBytes / MinimumSize); //10 = 2.5 becomes (3 words => 12 bytes)

            //If the data is not null and the size in bytes a positive value
            if (data != null && sizeInBytes > 0)
            {
                //Copy the data from to the binary taking only the amount of bytes which can be read within the bounds of the vector.
                Array.Copy(data, offset, m_MemorySegment.Array, 0, Math.Min(data.Length, sizeInBytes));
            }
        }

        /// <summary>
        /// Creates an RtpExtension from the given binary data which must include the Flags and LengthInWords at the propper offsets.
        /// Data is copied from offset to count.
        /// </summary>
        /// <param name="binary">The binary data of the extensions</param>
        /// <param name="offset">The amount of bytes to skip in binary</param>
        /// <param name="count">The amount of bytes to copy from binary</param>
        public RtpExtension(byte[] binary, int offset, int count)
        {
            if (binary == null) throw new ArgumentNullException("binary");
            else if (binary.Length < MinimumSize) throw InvalidExtension;

            //Atleast 4 octets are contained in binary
            m_MemorySegment = new Common.MemorySegment(binary, offset, count);
        }

        /// <summary>
        /// Creates a RtpExtension from the given RtpPacket by taking a reference to the Payload of the given RtpPacket. (No data is copied)
        /// Throws an ArgumentException if the given <paramref name="rtpPacket"/> does not have the <see cref="RtpHeader.Extension"/> bit set.
        /// </summary>
        /// <param name="rtpPacket">The RtpPacket</param>
        public RtpExtension(RtpPacket rtpPacket)
        {

            if (rtpPacket == null) throw new ArgumentNullException("rtpPacket");

            //Calulcate the amount of ContributingSourceListOctets
            int sourceListOctets = rtpPacket.ContributingSourceListOctets;

            if (!rtpPacket.Header.Extension)
                throw new ArgumentException("rtpPacket", "Does not have the Extension bit set in the RtpHeader.");
            else if (rtpPacket.Payload.Count - sourceListOctets < MinimumSize)
                throw InvalidExtension;
            else
                m_MemorySegment = new MemorySegment(rtpPacket.Payload.Array, rtpPacket.Payload.Offset + rtpPacket.ContributingSourceListOctets, rtpPacket.Payload.Count - sourceListOctets);
        }

        #endregion

        IEnumerable<byte> GetEnumerableImplementation()
        {
            return m_MemorySegment.Array.Skip(m_MemorySegment.Offset).Take(Size);
        }

        IEnumerator<byte> IEnumerable<byte>.GetEnumerator()
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
