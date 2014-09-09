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
            if (skip <=0 && reference) return new SourceDescriptionItemList(ChunkData)
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
}
