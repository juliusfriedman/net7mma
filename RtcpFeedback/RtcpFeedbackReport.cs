using System;
using System.Collections.Generic;
using Media.Rtcp;

namespace Media.Rtcp.Feedback
{
    public abstract class RtcpFeedbackReport : RtcpReport
    {
        public RtcpFeedbackReport(int version, int type, int format, int padding, int ssrc, int mssrc, byte[] feedbackControlInformation)
            : base(version, type, padding, ssrc, /*format*/0, 0,
            feedbackControlInformation != null ? Common.Binary.BytesPerInteger + feedbackControlInformation.Length : Common.Binary.BytesPerInteger)
        {
            //Set the FMT field
            BlockCount = format;

            //Write the MediaSynchronizationSourceIdentifier
            Common.Binary.Write32(Payload.Array, Payload.Offset, BitConverter.IsLittleEndian, (uint)mssrc);

            //Copy the FCI Data
            if (feedbackControlInformation != null)
                feedbackControlInformation.CopyTo(Payload.Array, Payload.Offset + Common.Binary.BytesPerInteger);
        }

        public RtcpFeedbackReport(RtcpPacket reference, bool shouldDispose = true) :
            base(reference.Header, reference.Payload, shouldDispose)
        {
            //Validate PayloadType
            if (Header.PayloadType < 205 || Header.PayloadType > 206) throw new ArgumentException("Header.PayloadType is not equal to the expected type.", "reference");

            //Validate Format
            switch (Format)
            {
                case Media.Rtcp.Feedback.RFC4585.FeedbackControlInformationType.Unassigned: throw new InvalidOperationException("PayloadType should be a known FeedbackControlInformationType other than Unassigned.");
            }
        }

        /// <summary>
        /// Gets or sets the value contained in the 'FMT' field which is also the <see cref="RtcpHeader.BlockCount"/>.
        /// </summary>
        public Media.Rtcp.Feedback.RFC4585.FeedbackControlInformationType Format
        {
            get
            {
                return (Media.Rtcp.Feedback.RFC4585.FeedbackControlInformationType)BlockCount;
            }
            set
            {
                BlockCount = (byte)value;
            }
        }

        /// <summary>
        /// Gets or sets the MediaSynchronizationSourceIdentifier field.
        /// </summary>
        public int MediaSynchronizationSourceIdentifier
        {
            get
            {

                return (int)Common.Binary.ReadU32(Payload.Array, Payload.Offset, BitConverter.IsLittleEndian);
            }
            set
            {
                Common.Binary.Write32(Payload.Array, Payload.Offset, BitConverter.IsLittleEndian, (uint)value);
            }
        }

        /// <summary>
        /// Gets the data which corresponds to the FeedbackReport without any padding.
        /// </summary>
        public IEnumerable<byte> FeedbackControlInformation
        {
            get { return new Common.MemorySegment(Payload.Array, Payload.Offset + Common.Binary.BytesPerInteger, Payload.Count - PaddingOctets - Common.Binary.BytesPerInteger); }
        }

        /// <summary>
        /// Calculates the amount of octets contained in the Payload which belong to any <see cref="FeedbackControlInformation"/>.
        /// </summary>
        public override int ReportBlockOctets
        {
            get
            {
                //The payload without padding and the MediaSynchronizationSourceIdentifier
                return Payload.Count - PaddingOctets - Common.Binary.BytesPerInteger;
            }
        }

        /// <summary>
        /// Retrieves the segment of data which corresponds to all <see cref="FeedbackControlInformation"/>. contained in the RtcpReport.
        /// </summary>
        public override IEnumerable<byte> ReportData
        {
            get
            {
                return FeedbackControlInformation;
            }
        }

        internal protected override IEnumerator<IReportBlock> GetEnumeratorInternal(int offset = 0)
        {
            //Add RtcpFeedbackReport's contain a MediaSynchronizationSourceIdentifier before any report data
            return base.GetEnumeratorInternal(offset + Common.Binary.BytesPerInteger);
        }

        //Should provide Feedback Report Blocks Enumerator?

        //Should override GetEnumeratorInternal to return the Feedback Report Blocks?
    }
}
