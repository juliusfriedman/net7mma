using System;
using System.Collections.Generic;
using System.Linq;
using Media.Rtcp;

namespace Media.Rtcp.Feedback
{
    public class NackReport : TransportLayerFeedbackReport
    {
        public NackReport(int version, int padding, int ssrc, int mssrc, IEnumerable<int> lostPackets)
            : base(version, TransportLayerFeedbackReport.TransportLayerFormat.Nack, padding, ssrc, mssrc, lostPackets != null ? new byte[Common.Binary.BytesPerInteger * lostPackets.Count()] : null)
        {
            throw new NotImplementedException();
            //Write the lost packets in PID BLP form
        }

        //IEnumerable<int> LostPackets

        //AddLostPacket(int sequenceNumber)

        //RemoveLostPacket(int sequenceNumber)
    }
}
