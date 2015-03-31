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

//See
//http://tools.ietf.org/html/rfc5285#page-5

namespace Media.Rtp
{
    #region RtpExtension

    /// <summary>
    /// Provides a managed implementation around the RtpExtension information which would be present in a RtpPacket only if the Extension bit is set.
    /// Marked IDisposable for derived implementations and to indicate when the implementation is no longer required.
    /// </summary>
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
        public int Flags
        {
            get { return IsDisposed ? ushort.MinValue : Binary.ReadU16(m_MemorySegment.Array, m_MemorySegment.Offset, BitConverter.IsLittleEndian); }
            protected set { if (IsDisposed) return; Binary.Write16(m_MemorySegment.Array, m_MemorySegment.Offset, BitConverter.IsLittleEndian, (ushort)value); }
        }

        /// <summary>
        /// Gets a count of the amount of 32 bit words which are required completely read this extension.
        /// </summary>
        public int LengthInWords
        {
            get { return IsDisposed ? ushort.MinValue : Binary.ReadU16(m_MemorySegment.Array, m_MemorySegment.Offset + 2, BitConverter.IsLittleEndian); }
            protected set { if (IsDisposed) return; Binary.Write16(m_MemorySegment.Array, m_MemorySegment.Offset + 2, BitConverter.IsLittleEndian, (ushort)value); }
        }

        /// <summary>
        /// Gets the binary data of the RtpExtension.
        /// Note that the data may not be complete, <see cref="RtpExtension.IsComplete"/>
        /// </summary>
        public IEnumerable<byte> Data
        {
            get { return IsDisposed ? Media.Common.MemorySegment.EmptyBytes : m_MemorySegment.Skip(MinimumSize).Take(Binary.Min(LengthInWords * 4, m_MemorySegment.Count)); }
        }

        /// <summary>
        /// Gets a value indicating if there is enough binary data required for the length indicated in the `LengthInWords` property.
        /// </summary>
        public bool IsComplete { get { if (IsDisposed) return false; return m_MemorySegment.Count >= Size; } }

        /// <summary>
        /// Gets the size in bytes of this RtpExtension including the Flags and LengthInWords fields.
        /// </summary>
        public int Size { get { return IsDisposed ? 0 : (ushort)(MinimumSize + LengthInWords * 4); } }

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new RtpExtension from the given options and optional data.
        /// </summary>
        /// <param name="sizeInBytes">The known size of the RtpExtension in bytes. The LengthInWords property will reflect this value divided by 4.</param>
        /// <param name="data">The optional extension data itself not including the Flags or LengthInWords fields.</param>
        /// <param name="offset">The optional offset into data to being copying.</param>
        public RtpExtension(int sizeInBytes, short flags = 0, byte[] data = null, int offset = 0)
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
            return m_MemorySegment;//.Array.Skip(m_MemorySegment.Offset).Take(Size);
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

namespace Media.UnitTests
{
    /// <summary>
    /// Provides tests which ensure the logic of the RtpExtension class is correct
    /// </summary>
    internal class RtpExtensionUnitTests
    {
        /// <summary>
        /// O (  )
        /// </summary>
        public static void TestAConstructor_And_Reserialization()
        {
            using (Rtp.RtpExtension extension = new Rtp.RtpExtension(ushort.MinValue, unchecked((short)ushort.MaxValue)))
            {
                //Check Flags
                if (extension.Flags != ushort.MinValue) throw new Exception("Unexpected Flags");

                //Size
                if (extension.Size != Rtp.RtpExtension.MinimumSize) throw new Exception("Unexpected Size");

                using (Rtp.RtpExtension s = new Rtp.RtpExtension(extension.ToArray(), 0, Rtp.RtpExtension.MinimumSize))
                {
                    //Check Flags
                    if (s.Flags != ushort.MinValue) throw new Exception("Unexpected Flags");

                    //Size
                    if (s.Size != Rtp.RtpExtension.MinimumSize) throw new Exception("Unexpected Size");

                    if (false == s.SequenceEqual(extension)) throw new Exception("Unexpected Data");
                }
            }
        }

        public static void TestExampleExtensions()
        {
            //Console.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);

            //Test TestOnvifRTPExtensionHeader (Thanks to Wuffles@codeplex)

            byte[] m_SamplePacketBytes = new byte[]
                                      {
                                          0x90, 0x60, 0x94, 0x63, // RTP Header
                                          0x0D, 0x19, 0x60, 0xC9, // .
                                          0xA6, 0x20, 0x13, 0x44, // .

                                          0xAB, 0xAC, 0x00, 0x03, // Extension Header   
                                          0xD4, 0xBB, 0x8A, 0x43, // Extension Data     
                                          0xFE, 0x7A, 0xC8, 0x1E, // Extension Data     
                                          0x00, 0xD3, 0x00, 0x00, // Extension Data     

                                          0x5C, 0x81, 0x9B, 0xC0, // RTP Payload start
                                          0x1C, 0x02, 0x38, 0x8E, // .
                                          0x2B, 0xC0, 0x01, 0x09, // .
                                          0x55, 0x77, 0x49, 0x99, // .
                                          0x62, 0xFF, 0xBA, 0xC9, // .
                                          0x8E, 0xCE, 0x23, 0x96, // .
                                          0x6A, 0xCC, 0xF5, 0x5F, // .
                                          0xA0, 0x08, 0xD9, 0x37, // .
                                          0xCF, 0xFA, 0xA5, 0x4D, // .
                                          0x16, 0x6C, 0x78, 0x61, // .
                                          0xFA, 0x7F, 0xC8, 0x7E, // .
                                          0xA1, 0x15, 0xF6, 0x5F, // .
                                          0xA3, 0x2F, 0x82, 0xC7, // .
                                          0x45, 0x0A, 0x87, 0x75, // .
                                          0xEC, 0x5B, 0x7D, 0xDE, // .
                                          0x82, 0x31, 0xD0, 0xE9, // .
                                          0xBE, 0xE5, 0x39, 0x8D, // etc. 
                                      };

            Media.Rtp.RtpPacket testPacket = new Media.Rtp.RtpPacket(m_SamplePacketBytes, 0);

            if (false == testPacket.Extension) throw new Exception("Unexpected Header.Extension");

            using (Media.Rtp.RtpExtension rtpExtension = testPacket.GetExtension())
            {

                if (rtpExtension == null) throw new Exception("Extension is null");

                if (false == rtpExtension.IsComplete) throw new Exception("Extension is not complete");

                // The extension data length is (3 words / 12 bytes) 
                // This property exposes the length of the ExtensionData in bytes including the flags and length bytes themselves
                //In cases where the ExtensionLength = 4 the ExtensionFlags should contain the only needed information
                if (rtpExtension.Size != 16) throw new Exception("Expected ExtensionLength not found");
                //else Console.WriteLine("Found LengthInWords: " + rtpExtension.LengthInWords);

                // Check extension values are what we expected.
                if (rtpExtension.Flags != 0xABAC) throw new Exception("Expected Extension Flags Not Found");
                //else Console.WriteLine("Found ExtensionFlags: " + rtpExtension.Flags.ToString("X"));

                // Test the extension data is correct
                byte[] expected = { 0xD4, 0xBB, 0x8A, 0x43, 0xFE, 0x7A, 0xC8, 0x1E, 0x00, 0xD3, 0x00, 0x00 };
                if (false == rtpExtension.Data.SequenceEqual(expected)) throw new Exception("Extension data was not as expected");
                //else Console.WriteLine("Found ExtensionData: " + BitConverter.ToString(rtpExtension.Data.ToArray()));
            }

            byte[] output = testPacket.Prepare().ToArray();

            if (output.Length != m_SamplePacketBytes.Length || false == output.SequenceEqual(m_SamplePacketBytes)) throw new Exception("Packet was not the same");

            //for (int i = 0, e = testPacket.Length; i < e; ++i) if (output[i] != m_SamplePacketBytes[i]) throw new Exception("Packet was not the same");

            //Console.WriteLine();

            m_SamplePacketBytes = new byte[]
                                      {
                                          //RTP Header
                                             0x90,0x1a,0x01,0x6d,0xf3,0xff,0x40,0x58,0xf0,0x00,0x9c,0x5b,
                                             //Extension FF FF, Length = 0
                                             0xff,0xff,0x00,0x00

                                            ,0x00,0x00,0x15,0x1c,0x01,0xff,0xa0,0x5a,0x13,0xd2,0xa9,0xf5,0xeb,0x49,0x52,0xdb
                                            ,0x65,0xa8,0xd8,0x40,0x36,0xa8,0x03,0x81,0x41,0xf7,0xa5,0x34,0x52,0x28,0x4f,0x7a
                                            ,0x29,0x79,0xf5,0xa4,0xa0,0x02,0x8a,0x5a,0x4a,0x00,0x28,0xed,0x45,0x14,0x0c,0x28
                                            ,0xef,0x45,0x1c,0xd0,0x20,0xa3,0x34,0x51,0x40,0xc4,0xcd,0x2d,0x14,0x7e,0x34,0x00
                                            ,0x7f,0x5a,0x4a,0x5a,0x29,0x00,0x94,0x52,0xd0,0x72,0x69,0x88,0x4e,0x68,0xa3,0x9a
                                            ,0x5a,0x43,0x12,0x8a,0x39,0xa0,0x73,0x4c,0x02,0x8a,0x5a,0x29,0x00,0x63,0x8e,0x28
                                            ,0xa5,0xcf,0x14,0x94,0x00,0x51,0x45,0x14,0x08,0x4a,0x51,0x9a,0x5c,0x52,0x00,0x68
                                            ,0x01,0x40,0xa4,0xa7,0x04,0x62,0x78,0xa9,0x52,0x06,0x6e,0xd4,0x01,0x12,0x8e,0x7d
                                            ,0x69,0x36,0x92,0x71,0x8a,0xd0,0x83,0x4f,0x91,0xfa,0x29,0x35,0x7a,0x1d,0x25,0xb3
                                            ,0xf3,0xfc,0xa3,0xde,0x9d,0x85,0x73,0x11,0x62,0x63,0x56,0x63,0xb3,0x76,0xc6,0x14
                                            ,0xd6,0xc9,0x86,0xca,0xd8,0x66,0x49,0x41,0x3e,0x82,0xaa,0x4f,0xad,0xdb,0xdb,0xf1
                                            ,0x12,0x8e,0x3b,0x9a,0x05,0x71,0xb1,0x69,0x72,0x1e,0x48,0xc0,0xf7,0xa9,0x8d,0xa5
                                            ,0xbc,0x03,0x32,0x48,0x3f,0x0a,0xc4,0xbd,0xf1,0x2c,0x8f,0x90,0x1f,0x03,0xd0,0x56
                                            ,0x35,0xce,0xaf,0x2c,0x87,0xef,0x53,0xb0,0x1d,0x54,0xd7,0xf6,0xb0,0x70,0x80,0x1f
                                            ,0x73,0x59,0xb7,0x9a,0xde,0x54,0x85,0x60,0x3e,0x95,0xcc,0x4d,0x79,0x23,0xf5,0x63
                                            ,0x55,0xda,0x56,0x27,0x92,0x69,0xf2,0xdc,0x2e,0x5f,0x97,0x54,0x91,0xdc,0xe5,0x89
                                            ,0xaa,0x57,0x77,0x4e,0xea,0x70,0x6a,0x0e,0x73,0x9a,0x8d,0xbe,0xb5,0x56,0x44,0xb6
                                            ,0x46,0x8e,0x59,0xdb,0x2d,0x9c,0x76,0xf4,0xa7,0xfe,0x95,0x1a,0x9c,0x12,0x79,0xa7
                                            ,0x82,0x4d,0x50,0x8f,0x41,0xa0,0x52,0xd2,0x71,0x59,0x1a,0x00,0x34,0xef,0x73,0x49
                                            ,0x9a,0x3e,0xa6,0x80,0x0a,0x28,0xe2,0x8e,0xf4,0x00,0x01,0x4b,0x41,0x22,0x90,0x30
                                            ,0xcf,0x51,0x45,0xc4,0x07,0x9a,0x51,0xd6,0x93,0x7a,0xff,0x00,0x78,0x7e,0x74,0x82
                                            ,0x44,0x1d,0x58,0x50,0x31,0xc3,0x39,0x39,0xa3,0x34,0x82,0x58,0xc7,0xf1,0x8f,0xce
                                            ,0x93,0xcf,0x8b,0xfb,0xe2,0x8b,0x88,0x7d,0x2e,0x0d,0x45,0xf6,0x88,0xfb,0xb8,0xfc
                                            ,0xe8,0xfb,0x54,0x3f,0xf3,0xd0,0x51,0x70,0x25,0xa2,0xa1,0xfb,0x5c,0x3d,0xdc,0x50
                                            ,0x2e,0xe1,0xcf,0xdf,0x14,0x5c,0x64,0xe0,0x51,0xc8,0xa4,0x57,0x57,0xe4,0x10,0x69
                                            ,0xc7,0x91,0x40,0x84,0xa5,0xa0,0x76,0x34,0x50,0x01,0x8c,0x9a,0x29,0x7a,0x52,0xfb
                                            ,0xd0,0x03,0x40,0xce,0x73,0x4b,0xb7,0x8a,0x5e,0x94,0x84,0xd0,0x03,0x31,0xcd,0x14
                                            ,0xe3,0x49,0x8c,0xd0,0x31,0x41,0xa7,0x0f,0x5a,0x8f,0xbd,0x3d,0x78,0x34,0x08,0x7f
                                            ,0x5e,0xb4,0x62,0x8c,0x64,0xd2,0xe3,0xd6,0x80,0x12,0x81,0x4b,0x8c,0xfb,0x51,0xc5
                                            ,0x00,0x14,0x87,0x91,0x4a,0x4d,0x1e,0xbc,0xd0,0x21,0xb9,0xcd,0x3b,0xde,0x8f,0xad
                                            ,0x28,0xa0,0x05,0xf4,0xa3,0xa9,0xa3,0x14,0x94,0x80,0x05,0x07,0xda,0x8a,0x5a,0x60
                                            ,0x27,0x7a,0x3b,0xe6,0x96,0x8e,0xb4,0x80,0x4e,0xb4,0x84,0x77,0xa5,0xe4,0x52,0x1a
                                            ,0x00,0x6e,0xda,0x8c,0x8e,0xb5,0x2d,0x30,0xe7,0xad,0x03,0x18,0x6a,0x39,0x33,0x52
                                            ,0x53,0x25,0xe9,0xd6,0x81,0x94,0xe5,0xe9,0x55,0x24,0xfb,0xc3,0xeb,0x56,0xe4,0xe9
                                            ,0x55,0x1b,0xef,0x0a,0x68,0x0b,0x30,0xf2,0xcb,0xf5,0xae,0x93,0x53,0xe3,0x4a,0x1f
                                            ,0x85,0x73,0x90,0x0f,0xde,0x2f,0xd6,0xba,0x4d,0x57,0x8d,0x2c,0x1f,0xa5,0x0f,0x63
                                            ,0x2a,0xbf,0x09,0x8d,0x6b,0x7d,0x2d,0xba,0xed,0x5c,0x11,0x5a,0xb6,0x9a,0xcb,0xa9
                                            ,0x1b,0x93,0x35,0x84,0xbe,0xb5,0x62,0x2e,0xbc,0x57,0x34,0x99,0xca,0xa7,0x25,0xb1
                                            ,0xdb,0xd8,0x6b,0xaa,0x17,0xee,0x10,0xd5,0xbb,0xa3,0x6a,0xcc,0x26,0xcc,0x8d,0x95
                                            ,0x6e,0xa2,0xbc,0xfe,0xd1,0xb6,0xe3,0x9a,0xdc,0xb2,0x9f,0x18,0xe6,0xb8,0xaa,0x26
                                            ,0x3f,0xac,0x4e,0xeb,0x53,0xd3,0x81,0x59,0x10,0x11,0x82,0xa6,0xa1,0x91,0x30,0xd8
                                            ,0x27,0x22,0xa9,0xf8,0x7e,0x7f,0x36,0xc8,0x6e,0x39,0xc1,0xa4,0xbe,0xbb,0x09,0x70
                                            ,0x57,0x3d,0x2a,0x66,0xd3,0x8a,0xee,0x74,0xd6,0xd6,0x0a,0x45,0xcf,0x2d,0x4d,0x21
                                            ,0x88,0x55,0x05,0xbd,0x1e,0xb5,0x28,0xbc,0x07,0xbd,0x61,0x76,0x71,0x68,0x5b,0xfb
                                            ,0x28,0x23,0x34,0xf8,0x20,0x11,0x12,0x7d,0x6a,0x18,0x2e,0xd4,0xf0,0xc6,0xae,0x03
                                            ,0x9e,0x6b,0xb7,0x0f,0x0a,0x73,0xdd,0xea,0x8e,0xca,0x50,0x83,0xf7,0x96,0xe4,0x77
                                            ,0x11,0xf9,0xb1,0x32,0xd6,0x4c,0x96,0x27,0xba,0x9a,0xda,0xa6,0x4b,0x22,0xc6,0x3e
                                            ,0x63,0xf8,0x56,0x98,0x9a,0x51,0x4d,0xce,0xe3,0xad,0x4a,0x32,0xd5,0xb3,0x9e,0x96
                                            ,0xcf,0xda,0xa8,0xdc,0x59,0xe3,0x27,0x15,0xd0,0x3b,0x2b,0x12,0x6a,0xad,0xc0,0x52
                                            ,0x0d,0x79,0xe9,0x9e,0x7b,0x8a,0x28,0xe9,0xb6,0xf8,0xb5,0x6e,0x3b,0xd5,0x5b,0xc4
                                            ,0xc6,0x45,0x6e,0x59,0x28,0xfb,0x23,0x1f,0x7a,0xc8,0xd4,0x78,0x26,0xbb,0xe9,0x3b
                                            ,0xa3,0xd8,0xa4,0xad,0x49,0x18,0x53,0x8e,0x6b,0x1b,0x59,0x1f,0xe8,0xad,0x5b,0x33
                                            ,0x9e,0x6b,0x1b,0x59,0x39,0xb5,0x35,0xd9,0xd0,0xc6,0xaf,0xc2,0xca,0x9e,0x1d,0x1c
                                            ,0xc9,0xf5,0xab,0x77,0x03,0x13,0xb7,0xd7,0xad,0x56,0xf0,0xef,0x59,0x3e,0xb5,0x6e
                                            ,0xe0,0x66,0x67,0xf5,0xcd,0x69,0x1d,0x8c,0xa1,0xb2,0x22,0x23,0xde,0x9b,0x4e,0xa3
                                            ,0x03,0xad,0x05,0x89,0x40,0xcf,0x7a,0x5e,0xd9,0xa4,0xef,0x40,0x06,0x38,0x34,0x63
                                            ,0x1e,0xf4,0x1e,0x07,0xd6,0x8f,0x4a,0x63,0x0f,0xeb,0x48,0x07,0xad,0x3b,0x81,0x4b
                                            ,0xd7,0x14,0x80,0x6f,0x4c,0xe6,0x93,0x8c,0x75,0xa5,0x23,0xae,0x68,0xc7,0x38,0xa6
                                            ,0x21,0x31,0xda,0x93,0x18,0xa7,0xe0,0x77,0x34,0x98,0xcd,0x00,0x34,0xf7,0xed,0x49
                                            ,0x8a,0x7e,0x33,0x48,0x45,0x00,0x34,0x77,0xa3,0x93,0x4b,0x4a,0x7d,0x28,0x01,0x33
                                            ,0x8a,0x30,0x71,0xcf,0x39,0xa2,0x97,0x1e,0xd4,0x00,0x2f,0xd2,0x9e,0x29,0x00,0xa7
                                            ,0x7b,0x66,0x81,0x09,0x8f,0x6a,0x43,0x9c,0x1e,0x29,0xe4,0x66,0x9a,0x72,0x38,0xa4
                                            ,0x52,0x2b,0xc8,0x0f,0x3d,0xaa,0xb4,0xb9,0x1f,0x4a,0xb5,0x27,0x7a,0xad,0x21,0xea
                                            ,0x2a,0x59,0x48,0xa8,0xfe,0xb5,0x03,0x70,0x6a,0xc4,0x83,0x15,0x5c,0xf5,0xa0,0xa1
                                            ,0x56,0xb6,0x3c,0x3d,0xff,0x00,0x1f,0xa9,0x58,0xe3,0xde,0xb6,0x7c,0x3d,0xff,0x00
                                            ,0x1f,0x89,0x42,0x14,0xb6,0x3b,0x1a,0x43,0x41,0x6e,0x29,0x33,0x56,0x66,0x14,0xbf
                                            ,0x53,0x4d,0xcd,0x26,0xf1,0xeb,0x40,0x0e,0x34,0xdf,0xad,0x30,0xca,0xa3,0xa9,0xa8
                                            ,0x24,0xbc,0x8d,0x7a,0x9a,0x57,0x0b,0x12,0xc9,0x55,0x49,0x1d,0xcd,0x55,0xbb,0xd5
                                            ,0x02,0xa9,0xd8,0xb9,0xfa,0xd6,0x3c,0xd7,0xf3,0x4d,0x9c,0xb6,0x07,0xa0,0xa3,0x98
                                            ,0x7c,0xac,0xda,0x9a,0xea,0x18,0xb3,0xb9,0xc7,0xe1,0x59,0xd3,0xea,0xc3,0x27,0xca
                                            ,0x4e,0x7d,0x4d,0x66,0x33,0x13,0xc9,0x24,0xd3,0x0f,0xeb,0x4b,0x99,0xb1,0xa8,0xa2
                                            ,0x69,0xee,0xe7,0x9b,0xef,0x39,0xc7,0xa0,0xaa,0xc7,0x9e,0xf4,0xa6,0x9a,0x69,0x17
                                            ,0x61,0x0d,0x25,0x2f,0xd6,0x83,0x40,0xc6,0x9a,0x28,0xeb,0x41,0x14,0x00,0x94,0x1a
                                            ,0x28,0xa0,0x04,0xf7,0xa5,0xa2,0x92,0x90,0x05,0x1d,0x28,0xa5,0xa0,0x62,0x73,0x45
                                            ,0x14,0x62,0x80,0x0a,0x3d,0x68,0xa2,0x80,0x0a,0x28,0xc5,0x2d,0x30,0x0e,0x94,0x7d
                                            ,0x69,0x69,0x29,0x00,0x94,0xb8,0xcf,0x5a,0x31,0x4b,0xed,0x40,0x08,0x0d,0x18,0xe2
                                            ,0x8a,0x28,0x10,0x9c,0xd1,0x4b,0x46,0x28,0x01,0x39,0xcd,0x14,0x51,0x8a,0x00,0x31
                                            ,0x49,0x9e,0x69,0xe4,0x52,0x00,0x73,0xcd,0x03,0x12,0x8a,0x90,0x46,0xc7,0xb5,0x4f
                                            ,0x1d,0xa3,0xb9,0xc0,0x52,0x68,0x11,0x57,0x06,0x9c,0xa8,0xc6,0xb5,0xe0,0xd1,0xe5
                                            ,0x6e,0x59,0x71,0xf5,0xab,0x8b,0x63,0x69,0x6e,0x33,0x34,0xcb,0xc7,0x61,0x4e,0xc2
                                            ,0xb9,0x84,0x96,0xce,0xc7,0xa1,0xab,0x90,0x69,0x72,0xbf,0x45,0x38,0xab,0xd2,0x6a
                                            ,0x76,0x36,0xdc,0x46,0x81,0x8f,0xa9,0xac,0xeb,0xbf,0x11,0xb6,0x08,0x56,0x0a,0x3d
                                            ,0xa9,0x0b,0x53,0x4a,0x3d,0x29,0x13,0xfd,0x6b,0xaa,0x8f,0xad,0x2b,0xcf,0xa7,0xda
                                            ,0xf7,0xde,0xc2,0xb9,0x1b,0x9d,0x71,0xdf,0x3f,0x39,0x3f,0x8d,0x66,0x4f,0xa8,0xca
                                            ,0xfd,0x0d,0x52,0xb8,0x1d,0xa5,0xc7,0x88,0xd2,0x20,0x44,0x4a,0xab,0x58,0x97,0x7e
                                            ,0x23,0x95,0xc9,0xc3,0x93,0x5c,0xdb,0xca,0xef,0xc9,0x63,0x4c,0xc9,0x34,0xf9,0x3b
                                            ,0x8a,0xe8,0xd0,0xb8,0xd5,0x66,0x94,0x9c,0xb1,0xaa,0x6f,0x71,0x23,0xe7,0x2c,0x6a
                                      };

            testPacket = new Media.Rtp.RtpPacket(m_SamplePacketBytes, 0);

            if (false == testPacket.Extension) throw new Exception("Unexpected Header.Extension");

            using (Media.Rtp.RtpExtension rtpExtension = testPacket.GetExtension())
            {

                if (rtpExtension == null) throw new Exception("Extension is null");

                if (false == rtpExtension.IsComplete) throw new Exception("Extension is not complete");

                // The extension data length is (3 words / 12 bytes) 
                // This property exposes the length of the ExtensionData in bytes including the flags and length bytes themselves
                //In cases where the ExtensionLength = 4 the ExtensionFlags should contain the only needed information
                if (rtpExtension.Size != Binary.BytesPerInteger) throw new Exception("Expected ExtensionLength not found");
                //else Console.WriteLine("Found LengthInWords: " + rtpExtension.LengthInWords);

                // Check extension values are what we expected.
                if (rtpExtension.Flags != ushort.MaxValue) throw new Exception("Expected Extension Flags Not Found");
                //else Console.WriteLine("Found ExtensionFlags: " + rtpExtension.Flags.ToString("X"));

                // Test the extension data is correct
                if (rtpExtension.Data.Any() || false == rtpExtension.Data.SequenceEqual(Media.Common.MemorySegment.EmptyBytes)) throw new Exception("Extension data was not as expected");
                //else Console.WriteLine("Found ExtensionData: " + BitConverter.ToString(rtpExtension.Data.ToArray()));
            }
        }

    }
}
