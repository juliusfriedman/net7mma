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

        static RFC4585()
        {
            //Register Payload Types
            //Define Constant?

            //Payload type (PT): 8 bits
            //This is the RTCP packet type that identifies the packet as being
            //an RTCP FB message.  Two values are defined by the IANA:

            //    Name   | Value | Brief Description
            //----------+-------+------------------------------------
            // RTPFB  |  205  | Transport layer FB message
            // PSFB   |  206  | Payload-specific FB message
            //RtcpPacket.TryMapImplementation(205, typeof(RtcpFeedbackPacket));
            //RtcpPacket.TryMapImplementation(206, typeof(RtcpFeedbackPacket));

        }
        
        //Could be named RtcpFeedbackReport
        public class RtcpFeedbackPacket : RtcpReport
        {
            public RtcpFeedbackPacket(int version, int type, int format, int padding, int ssrc, int mssrc, byte[] feedbackControlInformation)
                : base(version, type, padding, ssrc, /*format*/0, 0, feedbackControlInformation != null ? 4 + feedbackControlInformation.Length : 4)
            {
                //Set the FMT field
                BlockCount = format;

                //Write the MediaSynchronizationSourceIdentifier
                Common.Binary.Write32(Payload.Array, Payload.Offset, BitConverter.IsLittleEndian, (uint)mssrc);

                //Copy the FCI Data
                if (feedbackControlInformation != null)
                    feedbackControlInformation.CopyTo(Payload.Array, Payload.Offset + Common.Binary.BytesPerInteger);
            }

            public FeedbackControlInformationType Format
            {
                get
                {
                    return (FeedbackControlInformationType)BlockCount;
                }
                set
                {
                    BlockCount = (byte)value;
                }
            }

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

            public IEnumerable<byte> FeedbackControlInformation
            {
                get { return Payload.Skip(Common.Binary.BytesPerInteger); }
            }

            public override IEnumerable<byte> ReportData
            {
                get
                {
                    return FeedbackControlInformation;
                }
            }

        }

        //public class NackPacket : RtcpFeedbackPacket
        //{
        //    public const int Format = 1;

        //    //public NackPacket(IEnumerable<int> lostPackets)
        //    //    :base(2, 1, 0, 0, null)
        //    //{

        //    //}
        //}
    }
}
