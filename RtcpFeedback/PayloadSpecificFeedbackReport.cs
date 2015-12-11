namespace Media.Rtcp.Feedback
{
    public class PayloadSpecificFeedbackReport : RtcpFeedbackReport
    {
        #region Constants and Statics

        public new const int PayloadType = 206;

        #endregion

        #region Constructors

        public PayloadSpecificFeedbackReport(int version, Media.Rtcp.Feedback.RFC4585.FeedbackControlInformationType fmt, int padding, int ssrc, int mssrc, byte[] fci)
            : base(version, PayloadSpecificFeedbackReport.PayloadType, (int)fmt, padding, ssrc, mssrc, fci)
        {

        }

        /// <summary>
        /// Constructs a new PayloadSpecificFeedbackReport from the given <see cref="RtcpHeader"/> and payload.
        /// Changes to the header are immediately reflected in this instance.
        /// Changes to the payload are not immediately reflected in this instance.
        /// </summary>
        /// <param name="header">The header</param>
        /// <param name="payload">The payload</param>
        public PayloadSpecificFeedbackReport(RtcpHeader header, System.Collections.Generic.IEnumerable<byte> payload, bool shouldDispose = true)
            : base(header, payload, shouldDispose)
        {
            if (Header.PayloadType != PayloadType) throw new System.ArgumentException("Header.PayloadType is not equal to the expected type of 206.", "reference");
        }

        /// <summary>
        /// Constructs a new PayloadSpecificFeedbackReport from the given <see cref="RtcpHeader"/> and payload.
        /// Changes to the header and payload are immediately reflected in this instance.
        /// </summary>
        /// <param name="header"></param>
        /// <param name="payload"></param>
        public PayloadSpecificFeedbackReport(RtcpHeader header, Common.MemorySegment payload, bool shouldDipose = true)
            : base(header, payload, shouldDipose)
        {
            if (Header.PayloadType != PayloadType) throw new System.ArgumentException("Header.PayloadType is not equal to the expected type of 206.", "reference");
        }

        /// <summary>
        /// Constructs a PayloadSpecificFeedbackReport instance from an existing RtcpPacket reference.
        /// Throws a ArgumentNullException if reference is null.
        /// Throws an ArgumentException if the <see cref="RtcpHeader.PayloadType"/> is not PayloadSpecificFeedbackReport (206)
        /// </summary>
        /// <param name="reference">The packet containing the PayloadSpecificFeedbackReport</param>
        public PayloadSpecificFeedbackReport(RtcpPacket reference, bool shouldDispose = true)
            : base(reference.Header, reference.Payload, shouldDispose)
        {
            if (Header.PayloadType != PayloadType) throw new System.ArgumentException("Header.PayloadType is not equal to the expected type of 206.", "reference");
        }

        #endregion

    }
}
