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
    public class ContainerElement : Common.BaseDisposable
    {
        readonly ulong Offset, Size;
        public readonly string FourCC; //Needs a better name, E.g. ElementName
        readonly System.IO.MemoryStream Data;

        public override void Dispose()
        {
            if (Disposed) return;

            base.Dispose();

            Data.Dispose();
        }

    }
}
