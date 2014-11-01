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
    public class SourceDescriptionReport : RtcpReport, IEnumerable<SourceDescriptionReport.SourceDescriptionChunk>
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

        #region Nested Types

        #region SourceDescriptionItem

        /// <summary>
        /// Provides an implementation of the SDES item abstraction observed in http://tools.ietf.org/html/rfc3550#section-6.5.
        /// And Item is a contigous allocation of memory with a 8 bit identifier which in certain cases is followed by an 8 bit length field.        
        /// </summary>
        /// <remarks>
        /// In cases where Type = 0 (End Of List) up to 4 null octets may be present in the Data of a SourceDescriptionItem. 
        /// </remarks>
        public class SourceDescriptionItem : IEnumerable<byte>
        {
            #region Enumerations and Constants

            /// <summary>
            /// Defines some of the known types of SourceDescriptionItems.
            /// <see cref="http://www.iana.org/assignments/rtp-parameters/rtp-parameters.xhtml">For a complete list</see>
            /// </summary>
            public enum SourceDescriptionItemType : byte
            {
                /// <summary>
                /// 
                /// </summary>
                End = 0,
                /// <summary>
                /// 
                /// </summary>
                CName = 1,
                /// <summary>
                /// 
                /// </summary>
                Name = 2,
                /// <summary>
                /// 
                /// </summary>
                Email = 3,
                /// <summary>
                /// 
                /// </summary>
                Phone = 4,
                /// <summary>
                /// 
                /// </summary>
                Location = 5,
                /// <summary>
                /// 
                /// </summary>
                Tool = 6,
                /// <summary>
                /// 
                /// </summary>
                Note = 7,
                /// <summary>
                /// 
                /// </summary>
                Private = 8,
                /// <summary>
                /// 
                /// </summary>
                H323CallableAddress = 9,
                /// <summary>
                /// 
                /// </summary>
                ApplicationSpecificIdentifier = 10
                //More @ 
                //http://tools.ietf.org/html/rfc6776
            }

            /// <summary>
            /// The size in octets of any SourceDescriptionItem besides the End Of List.
            /// </summary>
            public const int ItemHeaderSize = 2;

            #endregion

            #region Statics

            /// <summary>
            ///  The CNAME item SHOULD have the format "user@host
            /// </summary>
            public static SourceDescriptionItem CName = new SourceDescriptionItem(SourceDescriptionItem.SourceDescriptionItemType.CName, Encoding.UTF8.GetBytes(Environment.UserName + '@' + Environment.MachineName));

            /// <summary>
            /// The value representing null
            /// </summary>
            public static byte Null = default(byte);

            #endregion

            #region Fields

            /// <summary>
            /// Any octets which are owned by this instance when created
            /// </summary>
            readonly byte[] m_OwnedOctets;

            /// <summary>
            /// A reference to the octets which contain the data this instance.
            /// </summary>
            protected readonly IEnumerable<byte> ItemData;

            #endregion

            #region Constructor

            SourceDescriptionItem(SourceDescriptionItem existing, bool doNotCopy)
            {

                //Cache the length because it will be used more than once.
                byte length = (byte)existing.Length;

                //Generate the ItemHeader from the data existing in the memory of the existing references itemHeader.
                if (doNotCopy)
                {
                    ItemData = existing.ItemData;
                    return;
                }

                //Generate a seqence contaning the required memory and project it into the owned octets instance.
                ItemData = m_OwnedOctets = existing.ItemData.ToArray();

                //Generate a segment of data consisting of the octets owned.
                //ItemData = m_OwnedOctets;

            }

            /// <summary>
            /// Creates a new SourceDescriptionItem of the given type and length.
            /// </summary>
            /// <param name="itemType">The type of item to create</param>
            /// <param name="itemLength">The length in bytes of the item</param>
            public SourceDescriptionItem(SourceDescriptionItemType itemType, int itemLength)
            {

                if (itemLength > 255) throw Binary.CreateOverflowException("itemType", itemLength, byte.MinValue.ToString(), byte.MaxValue.ToString());

                //There is always at least 1 octets owned to represent the Type
                ItemData = m_OwnedOctets = new byte[ItemHeaderSize + itemLength];

                m_OwnedOctets[0] = (byte)itemType;

                //Only set the Length when the ItemType is not End.
                if (itemType != SourceDescriptionItemType.End) m_OwnedOctets[1] = (byte)itemLength;

                //Gererate a sequence from the owned octets.
                //ItemData = m_OwnedOctets;
            }

            /// <summary>
            /// Creates a new SourceDescriptionItem from the given itemType and length.
            /// If data is not null the octets which remain when deleniated by offset are subsequently copied to the item data.
            /// </summary>
            /// <param name="itemType">The type of item to create</param>
            /// <param name="length">The length in octets of the item</param>
            /// <param name="data">Any data which should be copied to the item</param>
            /// <param name="offset">The offset into data to begin copying</param>
            public SourceDescriptionItem(SourceDescriptionItemType itemType, int length, byte[] data, int offset)
                : this(itemType, length)
            {
                //If any data is given
                if (data != null)
                {
                    //Determine the length of the available data
                    int dataLength = data.Length;

                    //If the offset is less than 0 or out of the range of the vector throw an exception
                    if (offset < 0 || offset > dataLength) throw new ArgumentOutOfRangeException("offset", "Must reflect an acceisble index of the data vector");

                    //If there are any bytes to copy
                    if (dataLength > 0)
                    {
                        //Determine the amount of bytes to copy
                        int bytesToCopy = dataLength - offset;

                        //If there are any bytes to copy
                        if (bytesToCopy > 0)
                        {
                            //Copy any data present from the given offset into the octets owned by this instance to the correct destination offet depending on the Type
                            Array.Copy(data, offset, m_OwnedOctets, 0, bytesToCopy);
                        }
                    }
                }
            }

            /// <summary>
            /// Creates a SourceDescriptionItem from existing data.
            /// Any changes to data will be visible for the life of this instance.
            /// </summary>
            /// <param name="itemType">The type of SourceDescriptionItem to create</param>
            /// <param name="data">The data which cannot exceed 255 octets</param>
            public SourceDescriptionItem(IEnumerable<byte> data)
            {
                //Assign the segment of data to the item.
                ItemData = data;
            }

            /// <summary>
            /// Generates a new SourceDescriptionItem instance using the given values.
            /// Throws and OverflowException if <paramref name="octets"/> contains a seqeuence of octets greater than <see cref="byte.MaxValue"/>
            /// </summary>
            /// <param name="itemType">The <see cref="ItemType"/> of the instance</param>
            /// <param name="octets">The enumerable sequence of data which will be projected and owned by this instanced</param>
            public SourceDescriptionItem(SourceDescriptionItemType itemType, IEnumerable<byte> octets)
            {
                //Get the count of the sequence
                int octetCount = octets.Count();

                //If the amount of contained octets is greater than the maximum value allowed then throw an overflow exception.
                if (octetCount > byte.MaxValue) Binary.CreateOverflowException("octets", octetCount, byte.MinValue.ToString(), byte.MaxValue.ToString());

                //Project the sequence which must be less than Byte.MaxValue in count.
                ItemData = Enumerable.Concat(Enumerable.Repeat((byte)itemType, 1).Concat(Enumerable.Repeat((byte)octetCount, 1)), octets);

                //Use the count obtained previously to generate a segment.
                m_OwnedOctets = ItemData.ToArray();
            }

            #endregion

            #region Properties

            #region Header Properties

            /// <summary>
            /// Returns the 8 bit ItemType of the SourceDescriptionItem
            /// </summary>
            public SourceDescriptionItemType ItemType { get { return (SourceDescriptionItemType)ItemData.First(); } }

            /// <summary>
            /// Returns the 8 bit value of the Length field unless the Type is End Of list, then the amount of null octets is returned.
            /// </summary>
            public int Length { get { return ItemType == 0 ? Data.Count() : ItemData.Skip(1).First(); } }

            #endregion

            /// <summary>
            /// Calculates the size in octets of the SourceDescriptionItem.
            /// This value includes the Type and Length fields as well as any null octets which may be present.
            /// </summary>
            public int Size { get { return ItemHeaderSize + Length; } }

            /// <summary>
            /// Provides the binary data of the SourceDescriptionItem not including the Type and Length fields.
            /// If the Type is 0 (End Of List) the null octets remaining in the ItemData are returned.
            /// </summary>
            /// <remarks>
            /// This data is supposed to be text in UTF-8 Encoding however [Page 45] Paragraph 1 states:
            /// The presence of multi-octet encodings is indicated by setting the most significant bit of a character to a value of one.
            /// </remarks>
            public IEnumerable<byte> Data
            {
                get { return ItemType == default(byte) ? ItemData.TakeWhile(o => o == default(byte)) : ItemData.Skip(ItemHeaderSize).Take(Length); }
            }

            #endregion

            #region Instance Methods

            /// <summary>
            /// Creates a new SourceDescriptionItem from this instance.
            /// If reference is true changes to either instance will be reflected in both instances.
            /// </summary>
            /// <param name="reference">A value indicating if the new instance references the data contained in this instance.</param>
            /// <returns>The newly created instance.</returns>
            public SourceDescriptionItem Clone(bool reference)
            {
                //if reference is true return a new instance whose changes will be reflected in this instance also.
                /*if (reference)*/
                return new SourceDescriptionItem(this, reference);

                //Otherwise create a new instance which is an exact copy of this instance.
                //return new SourceDescriptionItem(ItemType, Length, Data.ToArray(), 0);
            }

            #endregion

            IEnumerator<byte> IEnumerable<byte>.GetEnumerator()
            {
                return ItemData.GetEnumerator();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return ItemData.GetEnumerator();
            }
        }

        #endregion

        #region SourceDescriptionItemList

        /// <summary>
        /// Provides a construct to enumerate <see cref="SourceDescriptionItem"/>'s from a contigous allocation of memory.
        /// Is effectively a fixed sized read only list.
        /// </summary>
        internal class SourceDescriptionItemList : BaseDisposable, IEnumerator<SourceDescriptionItem>, IEnumerable<SourceDescriptionItem>, IReadOnlyCollection<SourceDescriptionItem>
        {
            #region Fields

            /// <summary>
            /// Any octets which direclty belong to this SourceDescriptionItemList instance.
            /// </summary>
            /// <remarks>
            /// Not readonly incase a need for the methods Add, Insert or Remove are required.
            /// Would then need to Implement IList, not impossible but not required. Most of the work is done with the IEnumerator implemenation anyway.
            /// </remarks>
            byte[] m_OwnedOctets;

            int m_Count;

            //The OctetSegment from which the SourceDescriptionList is parsed.
            public readonly IEnumerable<byte> ChunkData;

            /// <summary>
            /// The offset in parsing the ChunkData.
            /// </summary>
            protected int ChunkDataOffset, ItemIndex;

            /// <summary>
            /// When parsed the last item parsed occupies this member
            /// </summary>
            protected SourceDescriptionItem CurrentItem;

            #endregion

            #region Constructor

            /// <summary>
            /// Constructs a new SourceDescriptionItemList from a <see cref="SourceDescriptionChunk" />
            /// </summary>
            /// <param name="parent">The SourceDescriptionChunk</param>
            public SourceDescriptionItemList(SourceDescriptionChunk parent)
            {
                if (parent == null) throw new ArgumentNullException("parent");

                ChunkData = parent.ChunkData;
            }

            /// <summary>
            /// Creates a new SourceDescriptionItemList from existing data
            /// </summary>
            /// <param name="chunkData">The data which corresponds to the items in the list</param>
            internal SourceDescriptionItemList(IEnumerable<byte> chunkData)
            {
                ChunkData = chunkData;
            }

            /// <summary>
            /// Creates a new SourceDescriptionItemList from the List of given items by copying their data.
            /// Once added to the list changes to the items outside of the list will not be reflected in this instance.
            /// If there is not an EndOfList item present one will be added if required.
            /// </summary>
            /// <param name="items">The items to add to the source list.</param>
            public SourceDescriptionItemList(IEnumerable<SourceDescriptionItem> items)
            {
                if (items == null) throw new ArgumentNullException("items");

                //using an enumerator on the items given
                using (IEnumerator<SourceDescriptionItem> enumerator = items.GetEnumerator())
                {
                    //Create a sequence to concatenate the bytes to
                    ChunkData = Enumerable.Empty<byte>();

                    //While there is an item
                    while (enumerator.MoveNext())
                    {
                        //concatenate the sequence representing the item to the existing sequence
                        ChunkData = ChunkData.Concat(enumerator.Current.Data);

                        ++m_Count;
                    }

                    //If the list did not end with an EndOfList item
                    if (enumerator.Current.ItemType != 0)
                    {
                        //Determine the amount of octets in the sequence
                        int count = ChunkData.Count();

                        //Determine how many null octets to add
                        //http://tools.ietf.org/html/rfc3550#appendix-A.4

                        int nullOctetsRequired = count % 32;

                        //if there are any to add, concatenate them to the sequence.
                        if (nullOctetsRequired > 0) ChunkData = ChunkData.Concat(Enumerable.Repeat(default(byte), nullOctetsRequired));
                    }

                    //Project the sequence
                    m_OwnedOctets = ChunkData.ToArray();
                }
            }

            #endregion

            #region Properties

            internal bool StartedEnumeration
            {
                get { return ItemIndex > 0; }
            }

            /// <summary>
            /// Indicates the last <see cref="SourceDescriptionItem"/> enumerated.
            /// </summary>
            public SourceDescriptionItem Current
            {
                get
                {
                    if (!StartedEnumeration) throw new InvalidOperationException("Enumeration has not started.");
                    return CurrentItem;
                }
            }

            /// <summary>
            /// Indicates the last <see cref="SourceDescriptionItem"/> enumerated.
            /// </summary>
            object System.Collections.IEnumerator.Current
            {
                get
                {
                    if (!StartedEnumeration) throw new InvalidOperationException("Enumeration has not started.");
                    return CurrentItem;
                }
            }

            /// <summary>
            /// Indicates if there are any more <see cref="SourceDescriptionItem"/>'s to enumerate.
            /// </summary>
            public bool AtEndOfList { get { return ChunkDataOffset > ChunkData.Count() || StartedEnumeration && CurrentItem.ItemType == 0; } }

            public int Size { get { return ChunkData.Count(); } }

            public int Count { get { return m_Count; } }

            #endregion

            #region Methods

            /// <summary>
            /// Disposes of any reference's obtained in parsing.
            /// After calling this method Disposed will be true and the CurrentItem as well as any owned octets will be null.
            /// </summary>
            public override void Dispose()
            {
                if (Disposed) return;

                base.Dispose();

                CurrentItem = null;

                m_OwnedOctets = null;

                m_Count = 0;
                //ChunkData still points to m_OwnedOctets but it is readonly
            }

            /// <summary>
            /// Attempts to parse to the next <see cref="SourceDescriptionItem"/> in the Parent SourceDescriptionChunk.
            /// </summary>
            /// <returns>True if an item was parsed, otherwise false.</returns>
            public bool MoveNext()
            {
                //If the enumerator is disposed or AtEndOfList is true
                if (!Disposed && !AtEndOfList && ChunkData.Count() - ChunkDataOffset >= SourceDescriptionItem.ItemHeaderSize)
                {

                    //Generate a sequence of data contained in the chunk
                    IEnumerable<byte> chunkData = ChunkData.Skip(ChunkDataOffset);

                    //itemType is determined by reading the 1st octet of the ItemHeader
                    SourceDescriptionItem.SourceDescriptionItemType itemType = (SourceDescriptionItem.SourceDescriptionItemType)chunkData.First();

                    //Determine the itemLength
                    int itemLength;

                    //If the itemType is not End Of List then the the itemLength is determined by reading the 2nd octet of the ItemHeader
                    if (itemType != SourceDescriptionItem.SourceDescriptionItemType.End)
                    {
                        itemLength = chunkData.Skip(1).First();
                        chunkData = chunkData.Skip(2).Take(itemLength);
                    }
                    else //Other wise it is determined by taking the null octets which proceed the previous data.
                    {
                        chunkData = chunkData.Skip(2).TakeWhile(o => o == SourceDescriptionItem.Null);
                        itemLength = chunkData.Count();
                    }

                    //Allocate an item
                    CurrentItem = new SourceDescriptionItem(itemType, chunkData);

                    //Move the offset in parsing
                    ChunkDataOffset += CurrentItem.Size;

                    //Increment ItemIndex
                    ++ItemIndex;

                    //Indicate success
                    return true;
                }

                //Indicate failure
                return false;
            }

            /// <summary>
            /// If not already disposed starts resets the position of the ParentChunkOffset to -1.
            /// </summary>
            public void Reset()
            {
                if (Disposed) return;
                ChunkDataOffset = 0;
            }

            /// <summary>
            /// Parses each SourceDescriptionItem in this SourceDescriptionItemList and adds each item to the destination
            /// </summary>
            /// <param name="destination"></param>
            /// <returns></returns>
            public bool TryCopyTo(IList<SourceDescriptionItem> destination)
            {
                if (destination == null) throw new ArgumentNullException("destination");

                if (Disposed) return false;

                try
                {
                    //While there is an item to add, Add it
                    while (MoveNext()) destination.Add(CurrentItem);

                    //Indicate success
                    return true;
                }
                catch { return false; } //Indicate Failure
            }

            /// <summary>
            /// Reads all contained items from the current position to a new IList.
            /// </summary>
            /// <returns>null if the instance has already been disposed otherwise a IList containing the SourceDescriptionItem contained in this SourceDescriptionItemList.</returns>
            public IList<SourceDescriptionItem> ToList()
            {
                if (Disposed) return null;

                IList<SourceDescriptionItem> result = new List<SourceDescriptionItem>();

                while (MoveNext()) result.Add(CurrentItem);

                return result;
            }

            /// <summary>
            /// Writes each SourceDescriptionItem in this SourceDescriptionList from the current position to the given memory at the given offset.
            /// </summary>
            /// <param name="memory">The memory to write the SourceDescriptionList to</param>
            /// <param name="offset">The offset to begin writing</param>
            /// <returns></returns>
            public bool TryWriteTo(byte[] memory, int offset)
            {
                try
                {
                    //Where there is an item
                    while (MoveNext())
                    {
                        //Prepare a sequence, project it and then copy it.
                        //CurrentItem.Prepare().ToArray().CopyTo(memory, offset);

                        //Move the offset
                        //offset += CurrentItem.Size;

                        //Write the items without projecting their sequence to a seperate array

                        //Write the Type
                        memory[offset++] = (byte)CurrentItem.ItemType;

                        //Write the length
                        memory[offset++] = (byte)CurrentItem.Length;

                        //For every other byte in the data write it to the memory at the offset, moving offset by 1 byte each time.
                        foreach (byte b in CurrentItem.Data) memory[offset++] = b;
                    }
                    //Indicate success
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            /// <summary>
            /// Clones this SourceDescriptionItemList.
            /// If reference is true changes to the items in the list as well as the list itself will be observed in both instances.
            /// </summary>
            /// <param name="reference">Indicates if the new instance should reference this instance</param>
            /// <returns>The newly created instance</returns>
            public SourceDescriptionItemList Clone(bool reference, int skip = 0)
            {
                //If changes are to be reflected in the new instance give a reference to the existing ChunkData
                if (skip <= 0 && reference) return new SourceDescriptionItemList(ChunkData)
                {
                    m_Count = m_Count
                };

                //Otherwise return a new instance created from the result of reading all contained items to a new list.
                return new SourceDescriptionItemList(ToList().Skip(skip));
            }

            #endregion

            IEnumerator<SourceDescriptionItem> IEnumerable<SourceDescriptionItem>.GetEnumerator()
            {
                return this;
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return this;
            }
        }


        #endregion

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

            protected IEnumerable<byte> m_ChunkData;

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

            public SourceDescriptionChunk(IEnumerable<byte> ChunkData)
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

            IEnumerable<byte> IReportBlock.BlockData
            {
                get { return m_ChunkData; }
            }

            #endregion
        }

        #endregion

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

        #region Methods

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

        #endregion
    }

    #endregion
}
