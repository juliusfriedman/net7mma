using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Rtsp.Server
{

    //Should also effectively be a ChildMedia of the ArchiveSource
    //Implies ArchiveSource should be RtpArchivedSource and Archiver should be RtpArchiver.

    public class ArchivedRtpMedia : Media.Rtsp.Server.MediaTypes.RtpSource
    {
        //Read the archived data and start playback, one thread to read and send packets
        //Each session should createan instance of this class and be given a unique url for playback and control.
        //e.g. play /archive/test/video should get a control url which doesn't effect /archive/test directly.

        public ArchivedRtpMedia(string name, Uri source)
            : base(name, source)
        {            
            //Create Session Description from file in archiver.

            //SessionDescription =

            //Make thread of RtpClient read packet data and raise events which will subsequently allow clients to receive data.

            //RtpClient = new Rtp.RtpClient();
        }
    }
}
