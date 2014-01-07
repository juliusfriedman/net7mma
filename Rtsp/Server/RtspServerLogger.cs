using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.Rtsp.Server
{
    public abstract class RtspServerLogger
    {

        //Also make delegates and use them in the methods so the pattern in set both ways

        internal abstract void LogRequest(RtspMessage request, ClientSession session);
        internal abstract void LogResponse(RtspMessage response, ClientSession session);
        internal abstract void LogException(Exception ex);
    }
}
