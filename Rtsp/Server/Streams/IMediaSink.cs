using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Rtsp.Server.Streams
{
    public interface IMediaSink
    {
        void SendData(byte[] data);

	    void EnqueuData(byte[] data);
    }
}
