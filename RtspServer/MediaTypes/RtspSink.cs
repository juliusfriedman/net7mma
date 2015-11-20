using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Rtsp.Server.MediaTypes
{
    public class RtspSink : RtspSource, IMediaSink
    {
        public RtspSink(string name, Uri source) : base(name, source) { }

        //public RtspSink(string name, Uri source, Rtp.RtpClient client, bool perPacket = false)
        //    : base(name, source, perPacket)
        //{
        //    //RtpClient = client;
        //}

        public void SendData(byte[] data)
        {
            //if (RtspClient != null)//...
        }

        public void EnqueData(byte[] data)
        {
            //if (RtspClient != null) //...
        }
    }
}
