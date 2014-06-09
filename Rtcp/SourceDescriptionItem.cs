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
using Octet = System.Byte;
using OctetSegment = System.ArraySegment<byte>;
using Media.Common;

#endregion
namespace Media.Rtcp
{
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
        public static Octet Null = default(Octet);

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
            if(octetCount > byte.MaxValue) Binary.CreateOverflowException("octets", octetCount, byte.MinValue.ToString(), byte.MaxValue.ToString());
            
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
            get { return ItemType == 0 ? ItemData.TakeWhile(o => o == 0) : ItemData.Skip(ItemHeaderSize).Take(Length); }
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
            if (reference) return new SourceDescriptionItem(this, reference);

            //Otherwise create a new instance which is an exact copy of this instance.
            return new SourceDescriptionItem(ItemType, Length, Data.ToArray(), 0);
        }

        #endregion

        IEnumerator<Octet> IEnumerable<Octet>.GetEnumerator()
        {
            return ItemData.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return ItemData.GetEnumerator();
        }
    }

    #endregion
}
