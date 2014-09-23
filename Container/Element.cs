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
        
        //Should be a property created when accessed.
        public System.IO.MemoryStream Data
        {
            get
            {

                if (Size <= 0 || Master.BaseStream == null) return null;

                System.IO.MemoryStream result = new System.IO.MemoryStream((int)Size);

                //Slow, use from cached somehow
                long offsetPrevious = Master.BaseStream.Position;

                if (Offset > offsetPrevious)
                {
                    //Master.Seek(-Offset, System.IO.SeekOrigin.Current);
                }
                else
                {
                    //Master.Seek(Offset, System.IO.SeekOrigin.Current);
                }

                //Master.CopyTo(result);

                return result;
            }
        }

        //Indicates if element is complete
        public readonly bool Complete;

        public readonly byte[] Identifier;

        public Element(IMediaContainer master, byte[] identifier, long offset, long size, bool complete)
        {
            if (master == null) throw new ArgumentNullException("master");


            if (identifier == null) throw new ArgumentNullException("identifier");
            Master = master;
            Offset = offset;
            Identifier = identifier;
            Size = size;
            Complete = complete;
        }

        public override void Dispose()
        {
            if (Disposed) return;

            base.Dispose();

            if (Size > 0 && Data != null) Data.Dispose();
        }

    }
}
