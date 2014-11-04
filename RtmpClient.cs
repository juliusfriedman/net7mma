using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Rtmp
{
    //http://en.wikipedia.org/wiki/Real_Time_Messaging_Protocol#Specification_document
    public class RtmpClient
    {

        //Todo Define a ITransport interface so RtpClient and RtmpClient look the same to the outside world.
        //E.g. IMediaTransport which is obtained from a TransportManager automatially through the RtspClient or RtspServer.
        //=> CreateClient
        //=> GetOpenPorts(ProtocolType)
        //=> CreateListener?

        //Determine if IMediaTransportContext will also be needed for changes in ClientSession et al.

        public RtmpClient()
        {
            throw new NotImplementedException();
        }
    }
}
