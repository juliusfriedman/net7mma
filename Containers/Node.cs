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
using System.Threading.Tasks;

#endregion

namespace Media.Container
{
    /// <summary>
    /// Represents a superset of binary data within a <see cref="IMediaContainer"/>.
    /// </summary>
    public class Node : Common.BaseDisposable
    {
        #region Static API

        public static Node CreateNodeFrom(Node n)
        {
            return new Node(n); 
        }

        public static Node CreateNodeWithDataReference(Node n)
        {
            return new Node(n, true);
        }

        public static Node CreateNodeWithDataCopy(Node n)
        {
            return new Node(n, false);
        }

        public static bool ReferenceData(Node from, Node to)
        {
            if (from == to || false == from.DataAssigned) return false;

            if (false == to.DataAssigned || to.DataSize < from.DataSize) return false;

            if (DataReferenceEquals(from, to)) return true;

            to.m_Data = from.m_Data;

            return true;
        }

        public static bool CopyData(Node from, Node to, int offset = 0)
        {
            if (from == to || false == from.DataAssigned) return false;

            if (false == to.DataAssigned || to.DataSize < from.DataSize) return false;

            from.m_Data.CopyTo(to.m_Data, offset);

            return true;
        }

        public static bool TotalSizeEquals(Node a, Node b)
        {
            return a == b || a != null && b != null && a.TotalSize == b.TotalSize;
        }

        public static bool DataSizeEquals(Node a, Node b)
        {
            return a == b || a != null && b != null && a.DataSize == b.DataSize;
        }

        public static bool IdentifierEquals(Node a, Node b)
        {
            return a == b || a != null && b != null && a.Identifier == b.Identifier;
        }

        public static bool IdentifierSizeEquals(Node a, Node b)
        {
            return a == b || a != null && b != null && a.IdentifierSize == b.IdentifierSize;
        }

        public static bool MasterEquals(Node a, Node b)
        {
            return a == b || a != null && b != null && a.Master == b.Master;
        }

        public static bool IsCompleteEquals(Node a, Node b)
        {
            return a == b || a != null && b != null && a.IsComplete == b.IsComplete;
        }

        public static bool DataReferenceEquals(Node a, Node b)
        {
            return a == b || a != null && b != null && Object.ReferenceEquals(a.m_Data, b.m_Data);
        }

        public static bool DataEquals(Node a, Node b)
        {
            return a == b || a != null && b != null && a.DataAssigned && a.m_Data.SequenceEqual(b.m_Data);
        }

        #endregion

        #region Readonly Fields

        /// <summary>
        /// The <see cref="IMediaContainer"/> from which this instance was created.
        /// </summary>
        public readonly IMediaContainer Master;

        /// <summary>
        /// The Offset in which the <see cref="Data"/> occurs in the <see cref="Master"/>
        /// </summary>
        public readonly long DataOffset;
            
        /// <summary>
        /// The amount of bytes contained in the Node's <see cref="Data" />
        /// </summary>
        public readonly long DataSize;

        /// <summary>
        /// The amount of bytes used to describe the <see cref="Identifer"/> of the Node.
        /// </summary>
        public readonly int IdentifierSize;

        /// <summary>
        /// The amount of bytes used to describe the <see cref="DataSize"/> of the Node.
        /// </summary>
        public readonly int LengthSize;

        /// <summary>
        /// Indicates if this Node instance contains all requried data. (could calculate)
        /// </summary>
        public readonly bool IsComplete;

        /// <summary>
        /// Identifies this Node instance.
        /// </summary>
        public readonly byte[] Identifier;

        internal readonly Common.MemorySegment IdentifierPointer;

        #endregion

        #region Computed Properties

        /// <summary>
        /// The Total amount of bytes in the Node including the <see cref="Identifer"/> and <see cref="LengthSize"/>
        /// </summary>
        public long TotalSize { get { return DataSize + IdentifierSize + LengthSize; } }

        /// <summary>
        /// The offset at which the node occurs in the <see cref="Master"/>
        /// </summary>
        public long Offset { get { return DataOffset - (IdentifierSize + LengthSize); } } //Allow negitive or Max(0, ())?

        /// <summary>
        /// Indicates if the instance was disposed or if the <see cref="Master"/> Disposed AND <see cref="DataAssigned"/> is <see cref="False"/>
        /// </summary>
        public override bool IsDisposed { get { return base.IsDisposed || false == DataAssigned && Master != null && Master.IsDisposed; } }

        #endregion

        #region Data

        byte[] m_Data;

        /// <summary>
        /// The binary data of the contained in the Node (without (<see cref="Identifier"/> and (<see cref="LengthSize"/>))
        /// </summary>
        public byte[] Data
        {
            get
            {
                if (DataAssigned) return m_Data;
                else if (IsDisposed || DataSize <= 0 || Master.BaseStream == null) return Media.Common.MemorySegment.EmptyBytes;

                //If data is larger then a certain amount then it may just make sense to return the data itself?
                m_Data = new byte[DataSize];

                Master.ReadAt(DataOffset, m_Data, 0, (int)DataSize);

                return m_Data;
            }
            //set
            //{
            //    if (DataSize > 0 && value != null) Array.Copy(value, 0, RawData, 0, Math.Min(value.Length, (int)DataSize));
            //}
        }

        /// <summary>
        /// Indicates if <see cref="Data"/> has been assigned.
        /// </summary>
        public bool DataAssigned { get { return m_Data != null; } }

        /// <summary>
        /// Provides a <see cref="System.IO.MemoryStream"/> to <see cref="Data"/>
        /// </summary>
        public System.IO.MemoryStream DataStream
        {
            get
            {
                //Should not require the nodes data being read first. 
                //May need to create a new stream with a fixed length
                return new System.IO.MemoryStream(Data);
            }
        }

        #endregion

        #region Constructor / Destructor


        /// <summary>
        /// Constucts a Node instance from the given parameters
        /// </summary>
        /// <param name="master"></param>
        /// <param name="identifier"></param>
        /// <param name="offset"></param>
        /// <param name="size"></param>
        /// <param name="complete"></param>
        public Node(IMediaContainer master, byte[] identifier, int lengthSize, long offset, long size, bool complete)
        {
            if (master == null) throw new ArgumentNullException("master");
            if (identifier == null) throw new ArgumentNullException("identifier");
            Master = master;
            DataOffset = offset;
            Identifier = identifier;
            IdentifierPointer = new Common.MemorySegment(identifier);
            IdentifierSize = identifier.Length;
            LengthSize = lengthSize;
            DataSize = size;
            IsComplete = complete; //Should calulcate here?
        }

        public Node(IMediaContainer master, Common.MemorySegment identifierPointer, int identifierSize, int lengthSize, long offset, long size, bool complete)
        {
            Master = master;
            DataOffset = offset;

            IdentifierPointer = identifierPointer;
            Identifier = IdentifierPointer.Array;
            
            IdentifierSize = identifierSize;
            LengthSize = lengthSize;
            
            DataSize = size;
            IsComplete = complete; //Should calulcate here?
        }

        /// <summary>
        /// Creates a shallow copy of the node without the data
        /// </summary>
        /// <param name="n"></param>
        Node(Node n)
        {
            if (n == null) throw new ArgumentNullException();
            Master = n.Master;
            DataOffset = n.DataOffset;
            Identifier = n.Identifier;
            IdentifierSize = n.IdentifierSize;
            LengthSize = n.LengthSize;
            DataSize = n.DataSize;
            IsComplete = n.IsComplete;
        }

        /// <summary>
        /// Creates a copy of the node with the data if <paramref name="n"/> has a <see cref="DataSize"/> greater than 0 AND <see cref="DataAssigned"/> is <see cref="True"/>
        /// Throws a <see cref="NotImplementedException"/> if <paramref name="ndc"/> is not a known <see cref="NodeDataCopy"/>
        /// </summary>
        /// <param name="n">The source <see cref="Node"/></param>
        /// <param name="ndc">How to assign <see cref="Data"/></param>
        /// <param name="offset">The optional offset in <see cref="Data"/> to start the copy. (The length of the copy operation is given by <see cref="DataSize"/> minus this parameter) </param>
        Node(Node n, bool selfReference, int offset = 0) //ndc could just be a bool selfReference
            : this(n)
        {
            if(n != null && n.DataSize > 0 && n.DataAssigned)
            {
                if (selfReference) m_Data = n.m_Data;
                else
                {
                    m_Data = new byte[DataSize];

                    System.Array.Copy(n.m_Data, offset, m_Data, offset, DataSize - offset);
                }
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Writes all <see cref="Data"/> if <see cref="DataSize"/> is > 0.
        /// </summary>
        public void UpdateData()
        {
            if (!IsDisposed && DataSize > 0 && m_Data != null)
            {
                Master.WriteAt(DataOffset, m_Data, 0, (int)DataSize);
                return;
            }
        }

        /// <summary>
        /// Disposes of the resources used by the Node
        /// </summary>
        public override void Dispose()
        {
            if (IsDisposed) return;

            base.Dispose();

            m_Data = null;
        }

        public override string ToString()
        {
            return Master.ToTextualConvention(this);
        }

        #endregion
    }
}
