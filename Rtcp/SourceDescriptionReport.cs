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
    #region SourceDescriptionReport

    /// <summary>
    /// The SDES packet is a three-level structure composed of a header and zero or more chunks, each of which is composed of items describing the source identified in that chunk.  
    /// The items are described individually in subsequent sections of RFC3550 http://tools.ietf.org/html/rfc3550#section-6.5
    /// A SourceDescription is a 3 Tier Structure which contains an RtcpHeader and 0 or more <see cref="SourceDescriptionChunk"/>'s.
    /// The amount of <see cref="SourceDescriptionChunk"/>'s contained is indicated by the <see cref="RtcpPacket.BlockCount"/> property.
    /// </summary>
    public class SourceDescriptionReport : RtcpReport, IEnumerable<SourceDescriptionChunk>
    {
        #region Notes

        //Chunk lengths are indefinite, only definitely defined by the length of all subsequent items in the chunk.
        //Each chunk starts on a 32-bit boundary.
        //Each Item has a Type and Length, `ItemHeader` the `Length` does not include the Type and Length fields.
        //Item Length of 0 is valid.    Example (0x00, 0x00) - Type = 0 Length = 0 consists only of the 2 bytes which make up the `Item Header`
        //Items are contiguous, i.e., items are not individually padded to a 32-bit boundary. 

        /* Excerpt from Page 45 Paragraph 2 with an example.
         * 
      1) * Text is not null terminated because some multi-octet encodings include null octets.  The list of items in each chunk MUST be terminated by one or more null octets,
      2) * the first of which is interpreted as an item type of zero to denote the end of the list.
      3) * No length octet follows the null item type octet, but additional null octets MUST be included if needed to pad until the next 32-bit boundary.  
         * Note that this padding is separate from that indicated by the P bit in the RTCP header.  A chunk with zero items (four null octets) is valid but useless.
         *
         * * The following example illustrates how a Item may be formed and demonstrates the rules associated with aligning a SourceDescriptionChunk whose ItemData is contigous.
         * 
         * Example A SourceDescription RtcpPacket with (BlockCount)SC = 0
         * [RtcpHeader] In this example the chunk has an arbitary version with the BlockCount set to 0, and a LengthInWordsMinusOne property of 65535(0x00) This provides a (+4) offset to the values listed on the right hand column
         * -----------+Offset and Explination
         * x  x  x  x | 4 There are no more octets related to a SourceDescription past the RtcpHeader in such a case.
         * -----------+
         * --------------------------------------------------------------------
         * Example A SourceDescription RtcpPacket with BlockCount(SC) = 1
         * [RtcpHeader] In this example the chunk has an arbitary version with the BlockCount set to 1, and a LengthInWordsMinusOne property of 7(0x07) This provides a (+4) offset to the values listed on the right hand column
         * -----------+Offset and Explination
         * 1, 2, 3, 4 |0x04 (4 octet Chunk Identifier)
         * X, 255 (->)|0x05-0x105 (Type = X, Length = 255, Total 257 octets) [without a null terminator including the 2 bytes X and 255] (262 inlcuding the identifier)
         * ----------+106 (262) is evenly divisible by 4 and is aligned to a 32 bit boundary without additional null octets.
         * --------------------------------------------------------------------
         * Example A SourceDescription RtcpPacket with BlockCount(SC) = 2
         * [RtcpHeader] In this example the chunk has an arbitary version with the BlockCount set to 1, and a LengthInWordsMinusOne property of 13(0x0b) This provides a (+4) offset to the values listed on the right hand column
         * -----------+Offset and Explination
         * 1, 2, 3, 4 |0x04 (4 octet Chunk Identifier)
         * X, 255 (->)|0x05-0x105 (Type = X, Length = 255, Total 257 octets) [without a null terminator including the 2 bytes X and 255] (262 inlcuding the identifier)
         * 5, 6, 7, 8 |0x106 (4 octet Chunk Identifier)
         * X  251 ->  |0x107 - 0x203 (Type = X, Length = 251, Total 253 octets (+262 previous) ) [without a null terminator]
         *            |0x203 (515 total octets) 515 is not evenly divisible by 4 but since BlockCount(SC) = 2 no more octets are required.
         *------------+
         * 
         */

        #endregion

        #region Constants and Statics

        new public const int PayloadType = 202;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new SourceDescription with the given parameters
        /// </summary>
        /// <param name="version">The 2 - bit version of the SourceDescription</param>
        /// <param name="padding">The value indicating if the padding bit should be set in the header</param>
        /// <param name="reportBlocks">The value to store in the the high nybble of the first octet of the header</param>
        /// <param name="ssrc">The optional id of the participant who is sending this SourceDescriptionReport</param>
        public SourceDescriptionReport(int version, bool padding, int reportBlocks, int ssrc)
            : base(new RtcpHeader(version, PayloadType, padding, reportBlocks, ssrc), reportBlocks > 0 ? Enumerable.Repeat(SourceDescriptionItem.Null, 4) : Utility.Empty)
        {
            //[Page 45] Paragraph 2.
            //A chunk with zero items (four null octets) is valid but useless.
        }

        /// <summary>
        /// Constructs a SourceDescription from an existing RtcpPacket reference.
        /// </summary>
        /// <param name="reference">The existing RtcpPacket instance to create a SourceDescription instance from.</param>
        public SourceDescriptionReport(RtcpPacket reference, bool shouldDispose = true) :
            base(reference.Header, reference.Payload, shouldDispose)
        {
            if (Header.PayloadType != PayloadType) throw new ArgumentException("Header.PayloadType is not equal to the expected type of 202.", "reference");
        }

        #endregion

        #region Fields

        /// <summary>
        /// The cached Enumerable containing the pointer to the sequence of SourceDescriptionChunk contained in this instance.
        /// </summary>
        IEnumerable<SourceDescriptionChunk> m_Chunks;

        #endregion

        #region Properties

        /// <summary>
        /// Indicates the amount of SourceDescriptionChunks which are contained in this SourceDescription.
        /// </summary>
        public bool HasChunks { get { return Header.BlockCount > 0; } }

        /// <summary>
        /// Determines if ANY of the the contained SourceDescriptionChunks has a CName entry.
        /// </summary>
        /// <returns>True </returns>
        public bool HasCName
        {
            get
            {
                if (!HasChunks) return false;
                foreach (SourceDescriptionChunk chunk in GetChunkIterator()) foreach (SourceDescriptionItem item in chunk) if (item.ItemType == SourceDescriptionItem.SourceDescriptionItemType.CName) return true;
                return false;
            }
        }

        /// <summary>
        /// The binary data of the all <see cref="SourceDescriptionChunks"/>.
        /// Obtained by using <see cref="ReportBlockOctets"/>
        /// </summary>
        public override IEnumerable<byte> ReportData
        {
            get
            {
                if (!HasChunks) return Enumerable.Empty<byte>();
                return Enumerable.Concat(Header.GetSendersSynchronizationSourceIdentifierSequence(), Payload.Array.Skip(Payload.Offset).Take(ReportBlockOctets));                
            }
        }

        /// <summary>
        /// Calculates the summation of each contained <see cref="SourceDescriptionChunk"/> in this instance using <see cref="GetChunkEnumerator"/>
        /// </summary>
        public override int ReportBlockOctets { get { return GetChunkIterator().Sum(sc => sc.Size); } }

        #endregion

        public override void Add(IReportBlock reportBlock)
        {
            //Will throw an InvalidCastException is the given reportBlock is not a SourceDescriptionChunk
            if (reportBlock is SourceDescriptionChunk) Add(reportBlock as SourceDescriptionChunk);
            else base.Add(reportBlock);
        }

        public void Add(SourceDescriptionChunk chunk)
        {
            if (chunk == null) return;

            if (IsReadOnly) throw new InvalidOperationException("A SourceDescription Chunk cannot be added when IsReadOnly is true.");

            if (ReportBlocksRemaining == 0) throw new InvalidOperationException("A RtcpReport can only hold 31 ReportBlocks");

            //The octets which will be added to the payload consist of the ChunkData without the octets of the ChunkIdentifier in cases where BlockCount == 0
            IEnumerable<byte> chunkData = chunk.ChunkData;

            //Increase the BlockCount in any case
            ++BlockCount;

            //In the first SourceDescriptionChunk added to a SourceDescription the header contains the BlockIdentifier. 
            if (BlockCount == 1)
            {
                //Set the value in the header
                Header.SendersSynchronizationSourceIdentifier = chunk.ChunkIdentifer;

                //Build a seqeuence from the data in the ReportBlock without the chunk identifer
                chunkData = chunkData.Skip(SourceDescriptionChunk.IdentifierSize);

                //Remove any other data in the payload
                m_OwnedOctets = Utility.Empty;
            }
            
            //Add the bytes to the payload
            AddBytesToPayload(chunkData, 0, chunkData.Count());

            //http://tools.ietf.org/html/rfc3550#appendix-A.4
            int nullOctetsRequired = 4 - (Payload.Count & 0x03); 

            //Per RFC3550 @ Page 45 [Paragraph 2]
            //but additional null octets MUST be included if needed to pad until the next 32-bit boundary.
            if (nullOctetsRequired > 0)
            {
                //The amount of octets contained in the End Item is equal to the size of the chunk modulo 32.
                //This will allow the data to end on a 32 bit boundary.
                AddBytesToPayload(Enumerable.Concat(Enumerable.Repeat(SourceDescriptionItem.Null, nullOctetsRequired), chunkData), 0, nullOctetsRequired);
            }

            //Set the length in words minus one in the header
            SetLengthInWordsMinusOne();
        }

        public override bool Remove(IReportBlock reportBlock)
        {
            return Remove(reportBlock as SourceDescriptionChunk);
        }

        public bool Remove(SourceDescriptionChunk chunk)
        {
            if (chunk == null || IsReadOnly || BlockCount == 0) return false;

            //Determine where in the payload the chunk resides.
            int chunkOffset = 0, chunkIndex = 0;

            //Determine if the chunk is contained 
            bool contained = false;

            //Iterate the contained chunks
            foreach (SourceDescriptionChunk currentChunk in GetChunkIterator())
            {
                //If the chunk yielded in the iterator matches the chunk identifier a match is present.
                if (chunk.ChunkData == currentChunk.ChunkData)
                {
                    contained = true;
                    break;
                }

                //Move the offset
                chunkOffset += currentChunk.Size;

                //Increase the index
                ++chunkIndex;
            }
                

            //If there is no chunk matching by identifer indicate no chunk was removed.
            if (!contained) return false;

            //If the chunk is overlapped in the header
            if (chunkIndex == 0)
            {
                //Remove only the octets in the Payload which would correspond to the chunk which does no include the identifier
                m_OwnedOctets = m_OwnedOctets.Skip(chunk.Size - SourceDescriptionChunk.IdentifierSize).ToArray();
            }
            else //Otherwise the chunk and all items are removed,
            {
                //As well any null octets which proceed the chunk
                m_OwnedOctets = m_OwnedOctets.Skip(chunkOffset + chunk.Size).ToArray();
            }

            //Decrease the BlockCount
            --BlockCount;

            //Re allocate the segment based on the new array of owned octets.
            Payload = new Common.MemorySegment(m_OwnedOctets, 0, m_OwnedOctets.Length);

            SetLengthInWordsMinusOne();

            //Indicate a block was removed
            return true;
        }

        /// <summary>
        /// Gets a pointer to the IEnumerable implementation and caches that pointer for later use.
        /// </summary>
        /// <returns>The pointer to the enumerator implemenation.</returns>
        public IEnumerator<SourceDescriptionChunk> GetChunkEnumerator()
        {
            if (m_Chunks == null) m_Chunks = GetChunkIterator();
            return m_Chunks.GetEnumerator();
        }

        /// <summary>
        /// If the SourceDescription has any chunks they are returned using the Iterator logic here.
        /// The first Chunk is overlapped with the header
        /// </summary>
        /// <returns></returns>
        internal IEnumerable<SourceDescriptionChunk> GetChunkIterator()
        {
            //If no chunks are present break the iteration
            if (!HasChunks) yield break;

            //Label a chunk
            SourceDescriptionChunk current;

            int logicalChunkIndex = -1,
                offset = Payload.Offset;

            //Get the ReportData of the 
            IEnumerable<byte> ChunkData = Payload.Array;

            //While there is a chunk to iterate
            while (++logicalChunkIndex < Header.BlockCount && offset < Payload.Count)
            {
                //Instantiate the chunk and return the current item skipping previous data
                yield return current = new SourceDescriptionChunk(ChunkData.Skip(offset));
                offset += current.Size;
            }

            //Break the iterations all chunks have been iterated.
            yield break;
        }

        public override IEnumerator<IReportBlock> GetEnumerator()
        {
            return GetChunkEnumerator();
        }

        IEnumerator<SourceDescriptionChunk> IEnumerable<SourceDescriptionChunk>.GetEnumerator()
        {
            return GetChunkEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetChunkEnumerator();
        }        
    }

    #endregion
}
