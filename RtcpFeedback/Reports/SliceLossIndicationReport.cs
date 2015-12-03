namespace Media.Rtcp.Feedback
{
    public class SliceLossIndicationReport : PayloadSpecificFeedbackReport
    {
        public SliceLossIndicationReport(int version, int padding, int ssrc, int mssrc, int first, int number, int pictureId)
            : base(version, Media.Rtcp.Feedback.RFC4585.FeedbackControlInformationType.SliceLossIndication, padding, ssrc, mssrc, new byte[Common.Binary.BytesPerInteger * 3])
        {
            throw new System.NotImplementedException();

            //Needs WriteBits methods in Common.

            //using (var bw = new Common.BitWriter(new System.IO.MemoryStream(Payload.Array)))
            //{
            //    ///
            //}
        }
    }
}
