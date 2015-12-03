using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Rtcp.Feedback
{
    public static class RFC4585
    {
        public enum FeedbackControlInformationType : byte
        {
            Unassigned = 0,
            PictureLossIndication = 1,
            SliceLossIndication = 2,
            ReferencePictureSelectionIndication = 3,
            ApplicationLayerFeedback = 15,
            Reserved = 31
        }

        //Protocol should be here, attribute field can move to FeedbackReport
        //All others can be made to be integrated into the appropriate class for which it can be used.
        public const string Protocol = "RTP/AVPF", AttributeField = "rtcp-fb", Ack = "ack", Nack = "nack", TrrInt = "trr-int", App = "app",
            Sli = "sli", Pli = "pli", Rpsi = "rpsi";

        static RFC4585()
        {
            //Payload type (PT): 8 bits
            //This is the RTCP packet type that identifies the packet as being
            //an RTCP FB message.  Two values are defined by the IANA:

            //    Name   | Value | Brief Description
            //----------+-------+------------------------------------
            // RTPFB  |  205  | Transport layer FB message
            // PSFB   |  206  | Payload-specific FB message
            RtcpPacket.TryMapImplementation(Rtcp.Feedback.TransportLayerFeedbackReport.PayloadType, typeof(Rtcp.Feedback.TransportLayerFeedbackReport));
            RtcpPacket.TryMapImplementation(Rtcp.Feedback.PayloadSpecificFeedbackReport.PayloadType, typeof(Rtcp.Feedback.PayloadSpecificFeedbackReport));
        }

        //FeedbackControlInformationType Lookup?
    }
}
