using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Rtsp.Server
{
    public interface IMediaSink : IMedia
    {
        void SendData(byte[] data);

        void EnqueData(byte[] data);
    }
}
