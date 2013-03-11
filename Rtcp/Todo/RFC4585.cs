using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.Rtcp
{

    public enum FeedbackFormat
    {
        Unassigned = 0,
        PictureLossIndication = 1,
        SliceLossIndication = 2,
        ReferencePictureSelectionIndication = 3,
        ApplicationLayerFeedback = 15,
        Reserved = 31
    }

    //https://nmparsers.svn.codeplex.com/svn/Develop_Branch/NPL/common/rtcp.npl

    //http://www.faqs.org/rfcs/rfc6642.html

    //http://tools.ietf.org/rfc/rfc4585.txt
    /*
         
         6.4.  Application Layer Feedback Messages

   Application layer FB messages are a special case of payload-specific
   messages and are identified by PT=PSFB and FMT=15.  There MUST be
   exactly one application layer FB message contained in the FCI field,
   unless the application layer FB message structure itself allows for
   stacking (e.g., by means of a fixed size or explicit length
   indicator).

   These messages are used to transport application-defined data
   directly from the receiver's to the sender's application.  The data
   that is transported is not identified by the FB message.  Therefore,
   the application MUST be able to identify the message payload.

   Usually, applications define their own set of messages, e.g., NEWPRED
   messages in MPEG-4 [16] (carried in RTP packets according to RFC 3016
   [23]) or FB messages in H.263/Annex N, U [17] (packetized as per RFC
   2429 [14]).  These messages do not need any additional information
   from the RTCP message.  Thus, the application message is simply
   placed into the FCI field as follows and the length field is set
   accordingly.

   Application Message (FCI): variable length
      This field contains the original application message that should
      be transported from the receiver to the source.  The format is
      application dependent.  The length of this field is variable.  If
      the application data is not 32-bit aligned, padding bits and bytes
      MUST be added to achieve 32-bit alignment.  Identification of
      padding is up to the application layer and not defined in this
      specification.

   The application layer FB message specification MUST define whether or
   not the message needs to be interpreted specifically in the context
   of a certain codec (identified by the RTP payload type).  If a
   reference to the payload type is required for proper processing, the
   application layer FB message specification MUST define a way to
   communicate the payload type information as part of the application
   layer FB message itself.
         
         */

    public class RtcpFeedbackPacket
    {

         #region Properties

        public byte? Channel { get; set; }
        public DateTime? Created { get; set; }
        public DateTime? Sent { get; set; }
        public uint SenderSynchronizationSourceIdentifier { get; set; }
        public uint SourceSynchronizationSourceIdentifier { get; set; }
        public byte[] FeedbackControlInformation { get; set; }
        public byte Format { get; set; }
        public FeedbackFormat FeedbackFormat { get { return (FeedbackFormat)Format; } set { Format = (byte)value; } }

        #endregion

        #region Constructor

        public RtcpFeedbackPacket(uint senderSsrc, uint sourceSsrc, byte format = 0, byte[] data = null) { Created = DateTime.UtcNow; SenderSynchronizationSourceIdentifier = senderSsrc; SourceSynchronizationSourceIdentifier = sourceSsrc; FeedbackControlInformation = data; }

        public RtcpFeedbackPacket(RtcpPacket packet) : this(packet.Payload, 0)
        {
            Channel = packet.Channel;
            Created = packet.Created ?? DateTime.UtcNow;
            Format = (byte)packet.BlockCount;
            if (packet.PacketType != RtcpPacket.RtcpPacketType.TransportLayerFeedback || packet.PacketType != RtcpPacket.RtcpPacketType.PayloadSpecificFeedback) throw new Exception("Invalid Packet Type, Expected RTPFB or PSFB. Found: '" + (byte)packet.PacketType + '\'');
        }

        public RtcpFeedbackPacket(byte[] packet, int index)
        {
            SenderSynchronizationSourceIdentifier = Utility.ReverseUnsignedInt(BitConverter.ToUInt32(packet, index));
            index += 4;
            SourceSynchronizationSourceIdentifier = Utility.ReverseUnsignedInt(BitConverter.ToUInt32(packet, index));
            index += 4;

            int len = packet.Length - index;

            FeedbackControlInformation = new byte[len];
            System.Array.Copy(packet, index, FeedbackControlInformation, 0, len);

        }

        #endregion

        #region Methods

        public RtcpPacket ToPacket(byte? channel = null)
        {
            RtcpPacket output = new RtcpPacket(RtcpPacket.RtcpPacketType.ApplicationSpecific);
            output.Payload = ToBytes();
            output.BlockCount = Format;
            output.Channel = channel ?? Channel;
            return output;
        }

        public byte[] ToBytes()
        {
            List<byte> result = new List<byte>();
            //Should check endian before swapping
            result.AddRange(BitConverter.GetBytes(Utility.ReverseUnsignedInt(SenderSynchronizationSourceIdentifier)));
            result.AddRange(BitConverter.GetBytes(Utility.ReverseUnsignedInt(SourceSynchronizationSourceIdentifier)));
            if (FeedbackControlInformation != null) result.AddRange(FeedbackControlInformation);
            //Data is aligned to a multiple of 32 bits
            while (result.Count % 4 != 0) result.Add(0);
            return result.ToArray();
        }

        #endregion

        public static implicit operator RtcpPacket(RtcpFeedbackPacket fb) { return fb.ToPacket(fb.Channel); }

        public static implicit operator RtcpFeedbackPacket(RtcpPacket packet) { return new RtcpFeedbackPacket(packet); }

    }

    //RPSI

    //PLI

    //SLI

    //ALFB


}
