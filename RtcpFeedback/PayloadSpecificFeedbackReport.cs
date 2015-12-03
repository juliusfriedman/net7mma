namespace Media.Rtcp.Feedback
{
    public class PayloadSpecificFeedbackReport : RtcpFeedbackReport
    {
        #region Constants and Statics

        public new const int PayloadType = 206;

        #endregion

        public PayloadSpecificFeedbackReport(int version, Media.Rtcp.Feedback.RFC4585.FeedbackControlInformationType fmt, int padding, int ssrc, int mssrc, byte[] fci)
            : base(version, PayloadSpecificFeedbackReport.PayloadType, (int)fmt, padding, ssrc, mssrc, fci)
        {

        }
    }
}
