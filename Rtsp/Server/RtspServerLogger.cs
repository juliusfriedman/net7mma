using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.Rtsp
{
    public abstract class RtspServerLogger
    {
        internal abstract void LogRequest(RtspRequest request, RtspSession session);
        internal abstract void LogResponse(RtspResponse response, RtspSession session);
    }
}
