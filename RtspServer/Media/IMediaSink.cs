using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Rtsp.Server.Media
{
    public interface IMediaSink : IMediaStream
    {
        void SendData(byte[] data);

        void EnqueData(byte[] data);
    }
}
