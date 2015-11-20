/*
This file came from Managed Media Aggregation, You can always find the latest version @ https://net7mma.codeplex.com/
  
 Julius.Friedman@gmail.com / (SR. Software Engineer ASTI Transportation Inc. http://www.asti-trans.com)

Permission is hereby granted, free of charge, 
 * to any person obtaining a copy of this software and associated documentation files (the "Software"), 
 * to deal in the Software without restriction, 
 * including without limitation the rights to :
 * use, 
 * copy, 
 * modify, 
 * merge, 
 * publish, 
 * distribute, 
 * sublicense, 
 * and/or sell copies of the Software, 
 * and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * 
 * 
 * JuliusFriedman@gmail.com should be contacted for further details.

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. 
 * 
 * IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, 
 * DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, 
 * TORT OR OTHERWISE, 
 * ARISING FROM, 
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 * 
 * v//
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.Rtsp.Server
{
    public class RtspServerDebuggingLogger : RtspServerLogger
    {
        public string Format = "{0} {1} {2} {3} {4}\r\n";

        readonly Common.ILogging Logger = new Common.Loggers.DebuggingLogger();

        internal override void LogRequest(RtspMessage request, ClientSession session)
        {
            try { Logger.Log(string.Format(Format, request.MessageType, request.RtspMethod, request.Location, session.Id, null)); }
            catch { throw; }
        }

        internal override void LogResponse(RtspMessage response, ClientSession session)
        {
            try { Logger.Log(string.Format(Format, response.MessageType, response.CSeq, response.RtspStatusCode, session.Id, null)); }
            catch { throw; }
        }

        public override void LogException(Exception ex)
        {
            try { Logger.Log(string.Format(Format, ex.Message, Environment.NewLine, ex.StackTrace, Environment.NewLine, ex.InnerException != null ? ex.InnerException.ToString() : string.Empty)); }
            catch { throw; }
        }

        public override void Log(string data)
        {
            try { Logger.Log(data); }
            catch { throw; }
        }
    }
}
