using System;
using System.Collections.Generic;

namespace Media.Rtcp.Feedback
{
    public class TransportLayerFeedbackReport : RtcpFeedbackReport
    {
        #region Constants and Statics

        public new const int PayloadType = 205;

        #endregion

        public enum TransportLayerFormat
        {
            Unassigned = 0,
            Nack = 1,
            Reserved = 31
        }

        public TransportLayerFeedbackReport(int version, TransportLayerFormat tlf, int padding, int ssrc, int mssrc, byte[] fci)
            : base(version, TransportLayerFeedbackReport.PayloadType, (int)tlf, padding, ssrc, mssrc, fci)
        {

        }
    }
}
