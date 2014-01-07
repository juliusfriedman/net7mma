﻿#region Copyright
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
    #region RtcpReport

    /// <summary>
    /// Provides a managed implementation around the data contained in most Rtcp Packets.
    /// </summary>
    public abstract class RtcpReport : RtcpPacket, IEnumerable<IReportBlock>
    {

        #region Static

        //TOdo add IConvertible stubs

        /// <summary>
        /// Prepares a sequence of octets containing the items given.
        /// </summary>
        /// <param name="items">The items to prepare a binary sequence of</param>
        /// <returns>The variable length sequence corresponding to the items given.</returns>
        public static IEnumerable<byte> PrepareBinarySequence(IEnumerable<IReportBlock> items) //Extension where IReportBlock = T, where T is IConvertible<byte[]>
        {
            //Create a sequence to concatenate the bytes to, 4 bytes to hold a place for each of the octets in the ChunkIdentifier
            //Note, the ChunkIdentifer is not written here to avoid checks on system endian.
            IEnumerable<byte> octetSequence = Enumerable.Empty<byte>();

            //using an enumerator on the items given
            using (IEnumerator<IReportBlock> enumerator = items.GetEnumerator())
            {
                //While there is an item
                while (enumerator.MoveNext())
                {
                    //concatenate the sequence repsenting the item to the existing sequence
                    octetSequence = octetSequence.Concat(enumerator.Current.BlockData);
                }
            }

            //Project the sequence
            return octetSequence;
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Constructs a new instance of a RtcpReport.
        /// </summary>
        /// <param name="version">The version of the report</param>
        /// <param name="payloadType">The payloadType of the report.</param>
        /// <param name="padding"><see cref="RtcpHeader.Padding"/></param>
        /// <param name="ssrc">The id of the sender of the report</param>
        /// <param name="blockCount"><see cref="RtcpHeader.BlockCount"/></param>
        /// <param name="blockSize">The size in bytes of each block in the RtcpReport</param>
        /// <param name="extensionSize">The size in bytes of any extension data contained in the report.</param>
        public RtcpReport(int version, int payloadType, bool padding, int ssrc, int blockCount, int blockSize, int extensionSize = 0)
            : base(new RtcpHeader(version, payloadType, padding, blockCount, ssrc), Enumerable.Empty<byte>())
        {
            //Calulcate the size of the Payload Segment
            int payloadSize = blockSize * blockCount + extensionSize;

            //Allocate an array of byte equal to the size required
            m_OwnedOctets = new byte[payloadSize];

            //Segment the array to allow property access.
            Payload = new OctetSegment(m_OwnedOctets, 0, payloadSize);

            //Set the SetLenthInWordsMinusOne property in the Header
            SetLengthInWordsMinusOne();
        }

        /// <summary>
        /// Constructs a new instance of a RtcpReport containing the given Payload.
        /// Any changes to the header are immediately visible in this instance.
        /// Any changes to the payload are not reflected in this instance.
        /// </summary>
        /// <param name="header">The <see cref="RtcpHeader"/> of the instance</param>
        /// <param name="payload">The <see cref="RtcpPacket.Payload"/> of the instance.</param>
        public RtcpReport(RtcpHeader header, IEnumerable<byte> payload, bool ownsHeader = true)
            : base(header, payload, ownsHeader)
        {

        }

        /// <summary>
        /// Constructs a new RtcpReport from the given <see cref="RtcpHeader"/> and payload
        /// Any changes to the header or Payload are immediately visible in this instance.
        /// </summary>
        /// <param name="header">The <see cref="RtcpHeader"/> of the instance</param>
        /// <param name="payload">The <see cref="RtcpPacket.Payload"/> of the instance.</param>
        public RtcpReport(RtcpHeader header, OctetSegment payload, bool ownsHeader = true)
            : base(header, payload, ownsHeader)
        {

        }

        #endregion

        #region Properties

        /// <summary>
        /// Indicates if there is any data contained in this implementation of RtcpReport.
        /// </summary>
        public bool HasExtensionData
        {
            get { return Disposed ? false : Header.BlockCount > 0 && Length > ReportBlock.ReportBlockSize * Header.BlockCount; }
        }

        /// <summary>
        /// Indicates if the RtcpReport has any Reports based on the RtcpHeader.
        /// <see cref="RtcpHeader.BlockCount"/>
        /// </summary>
        public virtual bool HasReports
        {
            get { return Disposed ? false : Header.BlockCount > 0; }
        }

        /// <summary>
        /// Calculates the amount of octets contained in the Payload which belong to any ReportBlock.
        /// The BlockCount is obtained from the Header.
        /// </summary>
        public virtual int ReportBlockOctets
        {
            get { return !Disposed && HasReports ? ReportBlock.ReportBlockSize * Header.BlockCount : 0; }
        }

        /// <summary>
        /// Retrieves the segment of data which corresponds to all ReportBlocks contained in the RtcpReport.
        /// </summary>
        public virtual IEnumerable<byte> ReportData
        {
            get { if (!HasReports || Disposed) return Enumerable.Empty<byte>(); return Payload.Array.Skip(Payload.Offset).Take(ReportBlockOctets); }
        }

        //Could provide an index based accessor for Reports.
        //Such information is known from the ReportBlockOctets given by ReportBlockOctets / Header.BlockCount
        //Valid in all cases except SDES which is not a RtcpReport.
        //TODO IList Enumerable<byte> / OctetSegment of said indexer? Derived implementations would have the ReportBlock etc.

        public IEnumerable<byte> ExtensionData
        {
            get
            {
                if (Disposed || !HasExtensionData) return Enumerable.Empty<byte>();

                return Payload.Array.Skip(Payload.Offset + ReportBlockOctets);
            }
        }

        /// <summary>
        /// Calulcates the amount of blocks remaining in the RtcpReport given the value in the <see cref="RtcpHeaer.BlockCount"/>
        /// </summary>
        public int ReportBlocksRemaining { get { return Disposed ? 0 : Binary.FiveBitMaxValue - Header.BlockCount; } }        

        #endregion

        #region Instance Methods       

        public virtual IEnumerator<IReportBlock> GetEnumerator()
        {
            if (!Disposed && HasReports)
            {
                for (int offset = Payload.Offset, count = ReportBlockOctets; offset < count; )
                {
                    ReportBlock current = new ReportBlock(new OctetSegment(Payload.Array, offset, Payload.Count));
                    offset += current.Size;
                    yield return current;
                }
            }
        }

        #endregion

        #region Implementation Methods

        /// <summary>
        /// Adds a ReportBlock to this RtcpReport.
        /// Once added the <paramref name="reportBlock"/> cannot be removed.
        /// If less octets are allocated then requried in the Payload they will be added and owned by this instance and the <see cref="RtcpHeader.LengthInWordsMinusOne"/> property will be set to reflect the new count of octets in the payload.
        /// </summary>
        /// <param name="reportBlock">The IReportBlock instance to add.</param>
        public virtual void Add(IReportBlock reportBlock)
        {
            if (IsReadOnly) throw new InvalidOperationException("The RtcpReport can only be modified when IsReadOnly is false.");

            if (BlockCount > Binary.FiveBitMaxValue) throw new ArgumentOutOfRangeException("reportBlock", "The BlockCount property of the RtcpReport is at the maximum value possible.");

            if (reportBlock == null) throw new ArgumentNullException("reportBlock");

            //Determine the size of the block
            int reportBlockSize = reportBlock.Size;

            //If there is nothing being added then there is nothing to do.
            if(reportBlockSize == 0) return;

            //Increase the BlockCount
            ++BlockCount;

            AddBytesToPayload(reportBlock.BlockData, 0, reportBlockSize);
        }       

        public virtual bool Remove(IReportBlock reportBlock)
        {
            if (IsReadOnly) throw new InvalidOperationException("The RtcpReport can only be modified when IsReadOnly is false.");

            if (BlockCount <= 0) throw new ArgumentOutOfRangeException("reportBlock", "The BlockCount property of the RtcpReport is at the lowest value possible.");

            if (reportBlock == null) throw new ArgumentNullException("reportBlock");

            //Preserve the state in the enumerator
            using (var enumerator = GetEnumerator())
            {               
                //keep track of the offset
                int offset = 0;

                //While the enumerator can enumerate
                while (enumerator.MoveNext())
                {
                    //Check for equality first in the BlockIdentifier
                    if (reportBlock.BlockIdentifier == enumerator.Current.BlockIdentifier && // And then
                        reportBlock.BlockData.GetHashCode() == enumerator.Current.BlockData.GetHashCode())//If the HashCode of given BlockData is equal to the HashCode of the enumerators current BlockData...
                    {
                        //Then a IReportBlock has been matched,
                        //Take all bytes up to the offset and skip the size of the reportBlock, taking any octets which remains in the sequence as well.
                        m_OwnedOctets = m_OwnedOctets.Take(offset).Skip(reportBlock.Size).ToArray();

                        //Re allocate the payload around the new owned octets
                        Payload = new OctetSegment(m_OwnedOctets, 0, m_OwnedOctets.Length);

                        //Decrease the block count
                        --BlockCount;

                        //Indicate success
                        return true;
                    }

                    //Move the offset pointer
                    offset += enumerator.Current.Size;

                    //Iterate again while there is another IReportBlock in the enumerator
                    continue;
                }

            }

            //Indicate failure
            return false;
        }

        public virtual int IndexOf(IReportBlock reportBlock, int index)
        {
            //indicate no result found yet
            int result = -1;
            
            //If the index is in bounds, use an enumerator on the data because each report block is potentially sized differently but must always contain an Identifier.
            if(index >= 0 &&  index <= BlockCount) using (IEnumerator<IReportBlock> blockEnumerator = GetEnumerator())
            {
                //While there is an item in the enumerator
                while (blockEnumerator.MoveNext())
                {
                    //If the index is not yet 0 decrement and continue enumeration.
                    if (index-- > 0) continue;

                    //If the current block being enumerated corresponds to the given reportBlock then break enumeration.
                    if (blockEnumerator.Current.BlockIdentifier == reportBlock.BlockIdentifier && reportBlock.BlockData == blockEnumerator.Current.BlockData) break;
                }
            }

            //Return the result which corresponds to the index of the matched reportBlock
            return result;
        }

        public virtual bool Contains(IReportBlock reportBlock) { return IndexOf(reportBlock, 0) >= 0; }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }

        #endregion
       
    }

    #endregion
}
