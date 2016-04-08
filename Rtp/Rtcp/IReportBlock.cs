using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.Rtcp
{
    #region IReportBlock 

    /// <summary>
    /// Represents a binary element which corresponds to a single <see cref="RtcpPacket.BlockCount">block</see>.
    /// </summary>
    public interface IReportBlock
    {
        /// <summary>
        /// The size in octets of this instance.
        /// </summary>
        int Size { get; }

        /// <summary>
        /// The value which identifies the <see cref="IReportBlock.BlockData"/>.
        /// </summary>
        int BlockIdentifier { get; }

        /// <summary>
        /// The octets [of Size] which correspond to the binary data contained in the instance.
        /// </summary>
        IEnumerable<byte> BlockData { get; }

        //Todo Segment properties.
    }

    #endregion
}
