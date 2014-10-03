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
    public class Element : Common.BaseDisposable
    {
        readonly IMediaContainer Master;

        public readonly long Offset, Size;

        //Todo - Keep Size and Identifier both in bytes and use offsets and lengths to read values when required, so a ToByte() method can create the original data

        byte[] m_Data;

        //Should be a property created when accessed.
        public System.IO.MemoryStream Data
        {
            get
            {
                if (m_Data != null) return new System.IO.MemoryStream(m_Data);
                else if (Size <= 0 || Master.BaseStream == null) return null;

                //If data is larger then a certain amount then it may just make sense to return the data itself?

                //Slow, use from cached somehow
                long offsetPrevious = Master.BaseStream.Position;

                Master.BaseStream.Position = Offset;

                m_Data = new byte[Size];

                Master.BaseStream.Read(m_Data, 0, (int)Size);

                Master.BaseStream.Position = offsetPrevious;

                return new System.IO.MemoryStream(m_Data);
            }
        }

        //Indicates if element is complete
        public readonly bool IsComplete;

        public readonly byte[] Identifier;

        public Element(IMediaContainer master, byte[] identifier, long offset, long size, bool complete)
        {
            if (master == null) throw new ArgumentNullException("master");


            if (identifier == null) throw new ArgumentNullException("identifier");
            Master = master;
            Offset = offset;
            Identifier = identifier;
            Size = size;
            IsComplete = complete;
        }

        public override void Dispose()
        {
            if (Disposed) return;

            base.Dispose();

            m_Data = null;
        }

    }
}
