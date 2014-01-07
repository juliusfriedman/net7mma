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
    #region SourceDescriptionChunk

    /// <summary>
    /// Provides a implementation around the SourceDescriptionChunk abstraction observed in RFC3550 http://tools.ietf.org/html/rfc3550#section-6.5.
    /// </summary>
    /// <remarks>
    /// A SourceDescriptionChunk is a [variable length] 2 Tier Structure which contains an Identifer and a List of <see cref="SourceDescriptionItem"/>.
    /// </remarks>
    public class SourceDescriptionChunk : IEnumerable<SourceDescriptionItem>, IReportBlock
    {
        #region Constants

        /// <summary>
        /// The size of the ChunkIdentifer (in octets) which identifies each SourceDescriptionChunk.
        /// </summary>
        internal const int IdentifierSize = 4;

        #endregion

        #region Fields

        protected IEnumerable<Octet> m_ChunkData;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a SourceDescriptionChunk instance from data already stored in memory.
        /// The data should contain the ChunkIdentifier as well as the data of any contained <see cref="SourceDescriptionItem"/>'s.
        /// </summary>
        /// <param name="reference">The existing <see cref="SourceDescriptionChunk"/> instance.</param>
        /// <param name="copyData">Indicates ifd the data should be copied to this instance</param>
        public SourceDescriptionChunk(SourceDescriptionChunk reference, bool copyData)
        {
            //If copying the data
            if (copyData)
            {

                //The segment of memory containing the data points to the owned octets.
                m_ChunkData = reference.m_ChunkData.ToArray();

                return;
            }

            //Create a copy of the pointer to the memory of the existing reference's chunk data.
            m_ChunkData = reference.m_ChunkData;
            
        }


        /// <summary>
        /// Creates a new SourceDescriptionChunk with the given chunkIdentifier and the given items
        /// //The size of the
        /// </summary>
        /// <param name="chunkIdentifier">The chunkIdentifier</param>
        /// <param name="items">The pointer to the items in the chunk</param>
        public SourceDescriptionChunk(int chunkIdentifier, IEnumerable<SourceDescriptionItem> items)
        {

            m_ChunkData = BitConverter.GetBytes(chunkIdentifier);

            if (BitConverter.IsLittleEndian)
            {
                m_ChunkData = m_ChunkData.Reverse();
            }


            m_ChunkData = Enumerable.Concat(m_ChunkData, items.SelectMany(i => i));            
        }

        public SourceDescriptionChunk(int chunkIdentifier, SourceDescriptionItem item) : this(chunkIdentifier, item.Yield()) { }

        public SourceDescriptionChunk(IEnumerable<Octet> ChunkData)
        {
            m_ChunkData = ChunkData;
        }

        #endregion

        #region Properties

        /// <summary>
        /// The Chunk Identifier preceeds any <see cref="SourceDescriptionReport.Item"/> contained in a chunk.
        /// </summary>
        public int ChunkIdentifer
        {
            get { return (int)Binary.ReadU32(m_ChunkData, 0, BitConverter.IsLittleEndian); }            
        }

        /// <summary>
        /// Indicates if the SourceDesriptionChunk has any <see cref="SourceDescriptionItem"/>'s present.
        /// The <see cref="SourceDescriptionItem"/>'s can be obtained with a <see cref="SourceDescriptionItemList"/>
        /// </summary>
        public bool HasItems { get { return m_ChunkData.Count() > IdentifierSize; } }

        /// <summary>
        /// The size in octets of this instance.
        /// </summary>
        public int Size { get { return m_ChunkData.Count(); } }

        /// <summary>
        /// Gets a sequence containing the binary data of the chunk. (Including the ChunkIdentifier)
        /// </summary>
        public IEnumerable<byte> ChunkData
        {
            get 
            {
                return m_ChunkData;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Creates a clone of this SourceDescriptionChunk.
        /// If reference is true changes to either instance will be visible in both.
        /// </summary>
        /// <param name="reference">A value indicating if the new instance references the data of this instance.</param>
        /// <returns>The new instance.</returns>
        public SourceDescriptionChunk Clone(bool reference)
        {
            //The new SourceDescriptionChunk instance is created as a result of not referencing the data.
            return new SourceDescriptionChunk(this, !reference);
        }

        #endregion

        #region IEnumerator Implementation

        IEnumerable<SourceDescriptionItem> GetEnumerableImplementation()
        {
            return new SourceDescriptionItemList(this);
        }

        IEnumerator<SourceDescriptionItem> IEnumerable<SourceDescriptionItem>.GetEnumerator()
        {
            return GetEnumerableImplementation().GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerableImplementation().GetEnumerator();
        }

        #endregion

        #region IReportBlock

        int IReportBlock.BlockIdentifier
        {
            get { return ChunkIdentifer; }
        }

        IEnumerable<Octet> IReportBlock.BlockData
        {
            get { return m_ChunkData; }
        }

        #endregion
    }

    #endregion
}
