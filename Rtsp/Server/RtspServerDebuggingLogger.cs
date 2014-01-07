using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.Rtsp.Server
{
    public class RtspServerDebuggingLogger : RtspServerLogger
    {
        public string Format = "{0} {1} {2} {3} {4}\r\n";

        internal override void LogRequest(RtspMessage request, ClientSession session)
        {
            System.Diagnostics.Debug.WriteLine(string.Format(Format, request.MessageType, request.Method, request.Location, session.Id, session.m_RtspSocket.RemoteEndPoint));
        }

        internal override void LogResponse(RtspMessage response, ClientSession session)
        {
            System.Diagnostics.Debug.WriteLine(string.Format(Format, response.MessageType, response.CSeq, response.StatusCode, session.Id, session.m_RtspSocket.RemoteEndPoint));
        }

        internal override void LogException(Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(string.Format(Format, ex.Message, Environment.NewLine, ex.StackTrace, Environment.NewLine, ex.InnerException != null ? ex.InnerException.ToString() : string.Empty));
        }
    }
}
