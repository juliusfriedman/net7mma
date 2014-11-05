using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Rtcp.Expansion
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
        
        //Define Constant?

        //Payload type (PT): 8 bits
        //This is the RTCP packet type that identifies the packet as being
        //an RTCP FB message.  Two values are defined by the IANA:

        //    Name   | Value | Brief Description
        //----------+-------+------------------------------------
        // RTPFB  |  205  | Transport layer FB message
        // PSFB   |  206  | Payload-specific FB message

        public class RtcpFeedbackPacket : RtcpReport
        {
            public RtcpFeedbackPacket(int version, int type, bool padding, int ssrc, byte[] feedbackControlInformation)
                : base(version, type, padding, ssrc, 0, 0, feedbackControlInformation != null ? feedbackControlInformation.Length : 0)
            {

                //type must be either RTPFB or PSFB ?

                if (feedbackControlInformation != null)
                    feedbackControlInformation.CopyTo(Payload.Array, Payload.Offset);
            }

            public IEnumerable<byte> FeedbackControlInformation
            {
                get { return ExtensionData; }
            }

        }
    }
}
