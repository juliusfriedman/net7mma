using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace Media.Rtsp.Server.Streams
{
    /// <summary>
    /// A Source Stream which is a facade` to another.
    /// </summary>
    public class ChildStream : SourceStream
    {
        internal SourceStream m_Parent;

        public ChildStream(SourceStream source)
            :base(source.Name, source.Source)
        {
            if (!source.IsParent) throw new ArgumentException("Cannot make a Child of a Child.");
            m_Parent = source;
            m_Child = true;
        }

        public override Uri Source { get { return m_Parent.Source; } set { m_Parent.Source = value; } }
    }
}
