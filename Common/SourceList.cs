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
using Media.Rtp;
using Media.Rtcp;

#endregion

namespace Media.Common
{
    #region SourceList

    /// <summary>
    /// Provides a managed implementation around reading the SourceList from the binary present in a RtpPacket.
    /// Marked IDisposable incase derived and to indicate when the implementation is no longer required.
    /// 
    /// Note a mixer which is making a new SourceList should account for the fact that only 15 sources per RtpPacket can be indicated in a single RtpPacket,
    /// for more information see
    /// <see href="http://tools.ietf.org/html/rfc3550">Page 15, paragraph `CSRC list`</see>
    /// </summary>
    public sealed class SourceList : BaseDisposable, IEnumerator<uint>, IEnumerable<uint>, IReadOnlyCollection<uint>
    {
        #region Constants / Statics

        /// <summary>
        /// The size in octets of each element in the SourceList
        /// </summary>
        public const int ItemSize = 4;

        //Maybe choose to allow creation of a FixedSizedList

        #endregion

        #region Fields

        byte[] m_OwnedOctets;

        /// <summary>
        /// The memory which contains the SourceList
        /// </summary>
        Common.MemorySegment m_Binary;

        int m_CurrentOffset, //The current offset in parsing the binary
            m_SourceCount, //The amount of ContributingSources to read given from the CC nybble in a RtpHeader
            m_Read;//The amount of ContributingSources read so far.

        /// <summary>
        /// The current source item.
        /// </summary>
        uint m_CurrentSource;

        #endregion

        #region Constructor

        [CLSCompliant(false)]
        public SourceList(uint ssrc) : this(ssrc.Yield()) { }

        public SourceList(IEnumerable<uint> sources, int start = 0)
        {
            m_SourceCount = Math.Max(15, sources.Count());

            IEnumerable<byte> binary = Utility.Empty;

            foreach (var ssrc in sources.Skip(start))
            {
                if (BitConverter.IsLittleEndian)
                    binary = binary.Concat(BitConverter.GetBytes(ssrc).Reverse()).ToArray();
                else
                    binary = binary.Concat(BitConverter.GetBytes(ssrc)).ToArray();
            }

            m_Binary = new Common.MemorySegment(binary.ToArray(), 0, m_SourceCount * 4);
        }

        /// <summary>
        /// Creates a new source list from the given parameters.
        /// The SourceList owns ownly it's own resources and always should be disposed immediately.
        /// </summary>
        /// <param name="header">The <see cref="RtpHeader"/> to read the <see cref="RtpHeader.ContributingSourceCount"/> from</param>
        /// <param name="buffer">The buffer (which is vector of 32 bit values e.g. it will be read in increments of 32 bits per read)</param>
        public SourceList(RtpHeader header, byte[] buffer)
        {
            if (header == null) throw new ArgumentNullException("header");            

            //Assign the count (don't read it again)
            m_SourceCount = header.ContributingSourceCount;

            if (buffer == null) throw new ArgumentNullException("buffer");

            //Keep a reference to the buffer and the amount of bytes required
            if (m_SourceCount > 0)
            {
                //Source lists are only inserted by a mixer and come directly after the header and would be present in the payload,
                //before the RtpExtension (if present) and before the RtpPacket's actual binary data
                m_Binary = new Common.MemorySegment(buffer, 0, Math.Min(buffer.Length, m_SourceCount * 4));
            }
        }

        /// <summary>
        /// Creates a new source list from the given parameters.
        /// The SourceList owns ownly it's own resources and always should be disposed immediately.
        /// </summary>
        /// <param name="packet">The <see cref="RtpPacket"/> to create a SourceList from</param>
        public SourceList(RtpPacket packet)
            : this(packet.Header, packet.Payload.Array)
        {

        }

        /// <summary>
        /// Creates a SourceList from the given data when the count of sources in the data is known in advance.
        /// </summary>
        /// <param name="sourceCount">The count of sources expected in the SourceList</param>
        /// <param name="data">The data contained in the SourceList.</param>
        public SourceList(int sourceCount)
        {
            m_SourceCount = sourceCount;
            int sourceListSize = 4 * sourceCount;
            m_OwnedOctets = new byte[sourceListSize];
            m_Binary = new Common.MemorySegment(m_OwnedOctets, 0, sourceListSize);
        }

        /// <summary>
        /// Creates a SourceList from the data contained in the GoodbyeReport
        /// </summary>
        /// <param name="goodbyeReport">The GoodbyeReport</param>
        public SourceList(GoodbyeReport goodbyeReport)
        {
            m_SourceCount = goodbyeReport.Header.BlockCount;
            m_Binary = goodbyeReport.Payload;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Indicates if there is enough data in the given binary to read the complete source list.
        /// </summary>
        public bool IsComplete { get { return !Disposed && m_SourceCount * 4 == m_Binary.Count; } }

        uint IEnumerator<uint>.Current
        {
            get
            {
                CheckDisposed();
                if (m_CurrentOffset == m_Binary.Offset) throw new InvalidOperationException("Enumeration has not started yet.");
                return m_CurrentSource;
            }
        }

        /// <summary>
        /// Provides an 32 bit signed representation of the CurrentSource.
        /// </summary>
        public int CurrentSource
        {
            get
            {
                if (m_CurrentOffset == m_Binary.Offset) throw new InvalidOperationException("Enumeration has not started yet.");
                return (int)m_CurrentSource;
            }
        }

        /// <summary>
        /// The current contributing source item
        /// </summary>
        object System.Collections.IEnumerator.Current
        {
            get
            {
                //Check disposed
                CheckDisposed();

                //Indicate Enumeration has not yet started
                if (m_CurrentOffset == m_Binary.Offset) return null;

                //Return the current item
                return m_CurrentSource;
            }
        }

        /// <summary>
        /// Gets the amount of sources which can be read from the list.
        /// </summary>
        public int Count { get { return m_SourceCount; } }

        /// <summary>
        /// Gets the length in octets of the SourceList.
        /// </summary>
        public int Size { get { return m_SourceCount * 4; } }

        /// <summary>
        /// Indicates how many indexes are left in the SourceList based on the current index.
        /// </summary>
        public int Remaining { get { return Count - m_Read; } }
        
        /// <summary>
        /// The Capacity of this SourceList.
        /// </summary>
        public int Capacity { get { return m_SourceCount; } }

        #endregion

        #region Methods

        //Should also modify csrc
        ///// <summary>
        ///// Add the given id to this SourceList at the current position and sets the CurrentSource.
        ///// </summary>
        ///// <param name="id"></param>
        //public void Add(int id)
        //{
        //    //Check capacity
        //    if (Remaining <= 0) return;

        //    //Set the current item
        //    m_CurrentSource = (uint)id;

        //    //Write the given value to the correct position
        //    Binary.WriteNetwork32(m_Binary.Array, m_CurrentOffset, !BitConverter.IsLittleEndian, m_CurrentSource);

        //    //Move the offset
        //    m_CurrentOffset += 4;

        //    //Incremnt read
        //    ++m_Read;
        //}

        /// <summary>
        /// Moves to the next offset and parses the next contributing source.
        /// </summary>
        /// <returns>True if a value was read, otherwise false.</returns>
        public bool MoveNext()
        {
            if (Disposed) return false;

            //If there is a value to read and the binary data encompasses the required offset.
            if (m_Read < m_SourceCount && m_CurrentOffset + 4 < m_Binary.Count)
            {
                //Read the unsigned 16 bit value from the binary data
                m_CurrentSource = Binary.ReadU16(m_Binary.Array, m_CurrentOffset, true);

                //advance the offset
                m_CurrentOffset += 4;

                ++m_Read;

                //indicate success
                return true;
            }

            //indicate failure
            return false;
        }

        /// <summary>
        /// Resets the Enumerator
        /// </summary>
        public void Reset()
        {
            //Prevent unintended behvavior
            CheckDisposed();

            //Reset the current offset
            m_CurrentOffset = m_Binary.Offset;

            //Reset the amount of items read
            m_Read = 0;

            //Reset the current source
            m_CurrentSource = default(uint);
        }

        /// <summary>
        /// Prepares a binary sequence of 'Size' containing all of the data in the SourceList.
        /// </summary>
        /// <returns>The sequence created.</returns>
        public IEnumerable<byte> AsBinaryEnumerable()
        {
            return m_Binary.Array.Skip(m_Binary.Offset).Take(m_Binary.Count);
        }

        /// <summary>
        /// Tries to copies the ContribingSourceList to another vector.
        /// </summary>
        /// <param name="other">The vector to copy to</param>
        /// <param name="offset">The offset in the vector</param>
        /// <returns>True if the copy succeeded otherwise false.</returns>
        public bool TryCopyTo(byte[] other, int offset)
        {
            try
            {
                CheckDisposed();
                Array.Copy(m_Binary.Array, m_Binary.Offset, other, offset, Math.Min(m_SourceCount * 4, m_Binary.Count));
                return true;
            }
            catch { return false; }
        }

        /// <summary>
        /// Tries to copy the contained unsigned integers from the current in the enumeration to the given list.
        /// </summary>
        /// <param name="list">The list to add the items enumerated to.</param>
        /// <param name="index">The 0 based index of <paramref name="list"/> to start copying.</param>
        /// <returns>True if MoveNext was called, otherwise false.</returns>
        public bool TryCopyTo(IList<uint> list, int index = 0)
        {
            if (list == null || list.IsReadOnly) return false;
            try
            {
                CheckDisposed();

                while (MoveNext()) list.Insert(index++, m_CurrentSource);

                return true;
            }
            catch { return false; }
        }

        public sealed override void Dispose()
        {

            if (Disposed) return;

            base.Dispose();

            //Should always happen
            if (ShouldDispose) 
            {
                m_OwnedOctets = null;

                m_Binary = null;
            }
        }

        #endregion

        #region Interface Implementation Stubs

        IEnumerator<uint> IEnumerable<uint>.GetEnumerator()
        {
            return this;
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this;
        }

        #endregion 
    }

    #endregion
}
