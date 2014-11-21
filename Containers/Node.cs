using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Container
{
    /// <summary>
    /// Represents a superset of binary data which usually comes from a Media File.
    /// </summary>
    public class Node : Common.BaseDisposable
    {
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
        /// The Total amount of bytes in the Node including the <see cref="Identifer"/> and <see cref="LengthSize"/>
        /// </summary>
        public long TotalSize { get { return DataSize + IdentifierSize + LengthSize; } }

        /// <summary>
        /// The offset at which the node occurs in the <see cref="Master"/>
        /// </summary>
        public long Offset { get { return DataOffset - (IdentifierSize + LengthSize); } } //Allow negitive or Max(0, ())?

        byte[] m_Data;

        /// <summary>
        /// The binary data of the contained in the Node (without (<see cref="Identifier"/> and (<see cref="LengthSize"/>))
        /// </summary>
        public byte[] Data
        {
            get
            {
                if (m_Data != null) return m_Data;
                else if (DataSize <= 0 || Master.BaseStream == null) return Utility.Empty;

                //If data is larger then a certain amount then it may just make sense to return the data itself?
                m_Data = new byte[DataSize];

                if (Master.BaseStream is MediaFileStream)
                {
                    ((MediaFileStream)Master.BaseStream).AbsoluteRead(DataOffset, m_Data, 0, (int) DataSize);
                    return m_Data;
                }

                long offsetPrevious = Master.BaseStream.Position;

                Master.BaseStream.Position = DataOffset;

                Master.BaseStream.Read(m_Data, 0, (int)DataSize);

                Master.BaseStream.Position = offsetPrevious;

                return m_Data;
            }
            //set
            //{
            //    if (DataSize > 0 && value != null) Array.Copy(value, 0, RawData, 0, Math.Min(value.Length, (int)DataSize));
            //}
        }

        /// <summary>
        /// Provides a <see cref="System.IO.MemoryStream"/> to <see cref="Data"/>
        /// </summary>
        public System.IO.MemoryStream DataStream
        {
            get
            {
                return new System.IO.MemoryStream(Data);
            }
        }

        /// <summary>
        /// Indicates if this Node instance contains all requried data. (could calculate)
        /// </summary>
        public readonly bool IsComplete;

        /// <summary>
        /// Identifies this Node instance.
        /// </summary>
        public readonly byte[] Identifier;

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
            IdentifierSize = identifier.Length;
            LengthSize = lengthSize;
            DataSize = size;
            IsComplete = complete; //Should calulcate here?
        }

        /// <summary>
        /// Writes all <see cref="Data"/> if <see cref="DataSize"/> is > 0.
        /// </summary>
        public void UpdateData()
        {
            if (DataSize > 0 && m_Data != null)
            {
                if (Master.BaseStream is MediaFileStream)
                {
                    ((MediaFileStream)Master.BaseStream).AbsoluteWrite(DataOffset, m_Data, 0, (int)DataSize);
                    return;
                }

                long offsetPrevious = Master.BaseStream.Position;

                Master.BaseStream.Position = DataOffset;

                Master.BaseStream.Write(m_Data, 0, (int)DataSize);

                Master.BaseStream.Position = offsetPrevious;
            }
        }

        /// <summary>
        /// Disposes of the resources used by the Node
        /// </summary>
        public override void Dispose()
        {
            if (Disposed) return;

            base.Dispose();

            m_Data = null;
        }

        //ToString?

    }
}
