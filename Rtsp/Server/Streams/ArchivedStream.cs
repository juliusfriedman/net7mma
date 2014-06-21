using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Media.Rtsp.Server.Streams
{
    internal class ArchivedStream : RtpSource
    {
        RtpTools.RtpPlay.Program program;

        public override Rtp.RtpClient RtpClient
        {
            get { return program.Client; }
        }

        public ArchivedStream(string name, Uri source, Guid? id)
            : base(name, source)
        {

            if (!System.IO.File.Exists(source.AbsolutePath))
            {
                //Throw exception
                return;
            }

            if (id.HasValue) m_Id = id.Value;

            program = new RtpTools.RtpPlay.Program();

            //Read sdp from source directory
            SessionDescription = new Sdp.SessionDescription(System.IO.File.ReadAllText(source.AbsolutePath + "/SessionDescription.sdp"));
        }
    }
}
