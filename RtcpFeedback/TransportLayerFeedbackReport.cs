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

        #region Constructors

        public TransportLayerFeedbackReport(int version, TransportLayerFormat tlf, int padding, int ssrc, int mssrc, byte[] fci)
            : base(version, TransportLayerFeedbackReport.PayloadType, (int)tlf, padding, ssrc, mssrc, fci)
        {

        }

        /// <summary>
        /// Constructs a new TransportLayerFeedbackReport from the given <see cref="RtcpHeader"/> and payload.
        /// Changes to the header are immediately reflected in this instance.
        /// Changes to the payload are not immediately reflected in this instance.
        /// </summary>
        /// <param name="header">The header</param>
        /// <param name="payload">The payload</param>
        public TransportLayerFeedbackReport(RtcpHeader header, IEnumerable<byte> payload, bool shouldDispose = true)
            : base(header, payload, shouldDispose)
        {
            if (Header.PayloadType != PayloadType) throw new ArgumentException("Header.PayloadType is not equal to the expected type of 205.", "reference");
        }

        /// <summary>
        /// Constructs a new TransportLayerFeedbackReport from the given <see cref="RtcpHeader"/> and payload.
        /// Changes to the header and payload are immediately reflected in this instance.
        /// </summary>
        /// <param name="header"></param>
        /// <param name="payload"></param>
        public TransportLayerFeedbackReport(RtcpHeader header, Common.MemorySegment payload, bool shouldDipose = true)
            : base(header, payload, shouldDipose)
        {
            if (Header.PayloadType != PayloadType) throw new ArgumentException("Header.PayloadType is not equal to the expected type of 205.", "reference");
        }

        /// <summary>
        /// Constructs a TransportLayerFeedbackReport instance from an existing RtcpPacket reference.
        /// Throws a ArgumentNullException if reference is null.
        /// Throws an ArgumentException if the <see cref="RtcpHeader.PayloadType"/> is not TransportLayerFeedbackReport (205)
        /// </summary>
        /// <param name="reference">The packet containing the GoodbyeReport</param>
        public TransportLayerFeedbackReport(RtcpPacket reference, bool shouldDispose = true)
            : base(reference.Header, reference.Payload, shouldDispose)
        {
            if (Header.PayloadType != PayloadType) throw new ArgumentException("Header.PayloadType is not equal to the expected type of 205.", "reference");
        }

        #endregion
    }
}
