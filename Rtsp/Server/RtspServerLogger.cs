using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.Rtsp
{
    public sealed class RtspServerLogger
    {
        static RtspServerLogger() { }
        internal void LogRequest(RtspRequest request, RtspSession session) { }
        internal void LogResponse(RtspResponse response, RtspSession session) { }
    }
}
