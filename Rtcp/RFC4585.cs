using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.Rtcp
{

    //TODO Integrate Feedback into RtpClient and perform Rtcp checks for feedback enabled / disabled and finally send feedback with reports if required.
    
    //See if name is correct and possibly enumerate ApplicationLayer vs PayloadSpecific formats?
    public enum FeedbackControlInformationType : byte
    {
        Unassigned = 0,
        PictureLossIndication = 1,
        SliceLossIndication = 2,
        ReferencePictureSelectionIndication = 3,
        ApplicationLayerFeedback = 15,
        Reserved = 31
    }

    //RtpFeedback
    /*
     * References
     http://tools.ietf.org/rfc/rfc4585.txt
     http://tools.ietf.org/html/rfc6642
     * 
     * Existing format parser
     https://nmparsers.svn.codeplex.com/svn/Develop_Branch/NPL/common/rtcp.npl
     */

    public class RtcpFeedbackPacket
    {

        RtcpPacket.RtcpPacketType m_PacketType;

         #region Properties

        public byte? Channel { get; set; }
        public DateTime? Created { get; set; }
        public DateTime? Sent { get; set; }
        public uint SenderSynchronizationSourceIdentifier { get; set; }
        public uint SourceSynchronizationSourceIdentifier { get; set; }
        public byte[] FeedbackControlInformation { get; set; }
        public byte Format { get; set; }
        public FeedbackControlInformationType FeedbackFormat { get { return (FeedbackControlInformationType)Format; } set { Format = (byte)value; } }
        public RtcpPacket.RtcpPacketType PacketType { get { return m_PacketType; } set { if (value != RtcpPacket.RtcpPacketType.TransportLayerFeedback && value != RtcpPacket.RtcpPacketType.PayloadSpecificFeedback) throw new InvalidOperationException(); m_PacketType = value; } }

        #endregion

        #region Constructor

        public RtcpFeedbackPacket(uint senderSsrc, uint sourceSsrc, RtcpPacket.RtcpPacketType packetType = RtcpPacket.RtcpPacketType.TransportLayerFeedback, byte format = 0, byte[] data = null) { Created = DateTime.UtcNow; PacketType = packetType; SenderSynchronizationSourceIdentifier = senderSsrc; SourceSynchronizationSourceIdentifier = sourceSsrc; FeedbackControlInformation = data; }

        public RtcpFeedbackPacket(RtcpPacket packet) : this(packet.Payload, 0)
        {
            if (packet.PacketType != RtcpPacket.RtcpPacketType.TransportLayerFeedback || packet.PacketType != RtcpPacket.RtcpPacketType.PayloadSpecificFeedback) throw new Exception("Invalid Packet Type, Expected RTPFB or PSFB. Found: '" + (byte)packet.PacketType + '\'');
            else PacketType = packet.PacketType;
            Channel = packet.Channel;
            Created = packet.Created ?? DateTime.UtcNow;
            Format = (byte)packet.BlockCount;            
        }

        internal RtcpFeedbackPacket(byte[] packet, int index)
        {
            SenderSynchronizationSourceIdentifier = Utility.ReverseUnsignedInt(BitConverter.ToUInt32(packet, index));
            index += 4;
            SourceSynchronizationSourceIdentifier = Utility.ReverseUnsignedInt(BitConverter.ToUInt32(packet, index));
            index += 4;
            int len = packet.Length - index;
            FeedbackControlInformation = new byte[len];
            System.Array.Copy(packet, index, FeedbackControlInformation, 0, len);
        }

        public RtcpFeedbackPacket(FeedbackControlInformationType feedbackType, RtcpPacket.RtcpPacketType packetType, byte[] feedbackControlInfo = null)
        {
            Created = DateTime.Now;
            FeedbackFormat = feedbackType;
            m_PacketType = packetType;
        }

        #endregion

        #region Methods

        public RtcpPacket ToPacket(byte? channel = null)
        {
            RtcpPacket output = new RtcpPacket(PacketType);
            output.Payload = ToBytes();
            output.BlockCount = Format;
            output.Channel = channel ?? Channel;
            return output;
        }

        public virtual byte[] ToBytes()
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

    //NACK
    /*     
6.2.   Transport Layer Feedback Messages

   Transport layer FB messages are identified by the value RTPFB as RTCP
   message type.

   A single general purpose transport layer FB message is defined in
   this document: Generic NACK.  It is identified by means of the FMT
   parameter as follows:

   0:    unassigned
   1:    Generic NACK
   2-30: unassigned
   31:   reserved for future expansion of the identifier number space

   The following subsection defines the formats of the FCI field for
   this type of FB message.  Further generic feedback messages MAY be
   defined in the future.

6.2.1.  Generic NACK

   The Generic NACK message is identified by PT=RTPFB and FMT=1.

   The FCI field MUST contain at least one and MAY contain more than one
   Generic NACK.

   The Generic NACK is used to indicate the loss of one or more RTP
   packets.  The lost packet(s) are identified by the means of a packet
   identifier and a bit mask.

   Generic NACK feedback SHOULD NOT be used if the underlying transport
   protocol is capable of providing similar feedback information to the
   sender (as may be the case, e.g., with DCCP).

   The Feedback Control Information (FCI) field has the following Syntax
   (Figure 4):

    0                   1                   2                   3
    0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |            PID                |             BLP               |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+

               Figure 4: Syntax for the Generic NACK message

   Packet ID (PID): 16 bits
      The PID field is used to specify a lost packet.  The PID field
      refers to the RTP sequence number of the lost packet.
     
     */

    public class GenericNegativeACKnowledgement
    {
        

        public ushort PacketId { get; set; }

        public ushort BitmapLostPackets { get; set; }

        public byte[] ToBytes() { List<byte> result = new List<byte>(); result.AddRange(BitConverter.GetBytes(Utility.ReverseUnsignedShort(PacketId))); result.AddRange(BitConverter.GetBytes(Utility.ReverseUnsignedShort(BitmapLostPackets))); return result.ToArray(); }

        public RtcpPacket ToPacket(byte? channel = null)
        {
            return new RtcpFeedbackPacket((FeedbackControlInformationType)1, RtcpPacket.RtcpPacketType.TransportLayerFeedback, ToBytes())
            {
                Channel = channel ?? Channel,
                SenderSynchronizationSourceIdentifier = SenderSynchronizationSourceIdentifier,
                SourceSynchronizationSourceIdentifier = SourceSynchronizationSourceIdentifier
            };
        }

        public GenericNegativeACKnowledgement(RtcpFeedbackPacket packet)
            : this(packet.FeedbackControlInformation, 0)
        {
            Created = packet.Created;
            Channel = packet.Channel;
            Sent = packet.Sent;
            if (packet.PacketType != RtcpPacket.RtcpPacketType.TransportLayerFeedback || packet.Format != 1) throw new InvalidOperationException("Exptected: W=Z, Y=Z. Found: \"" + '"');
            SenderSynchronizationSourceIdentifier = packet.SenderSynchronizationSourceIdentifier;
            SourceSynchronizationSourceIdentifier = packet.SourceSynchronizationSourceIdentifier;
        }

        public GenericNegativeACKnowledgement(byte[] packet, int index)
        {
            PacketId = Utility.ReverseUnsignedShort(BitConverter.ToUInt16(packet, index));
            index += 2;
            BitmapLostPackets = Utility.ReverseUnsignedShort(BitConverter.ToUInt16(packet, index));
        }

        public byte? Channel { get; set; }
        public DateTime? Created { get; set; }
        public DateTime? Sent { get; set; }
        public uint SenderSynchronizationSourceIdentifier, SourceSynchronizationSourceIdentifier;

        public static implicit operator RtcpPacket(GenericNegativeACKnowledgement nack) { return nack.ToPacket(nack.Channel); }

        public static implicit operator GenericNegativeACKnowledgement(RtcpPacket packet) { return new GenericNegativeACKnowledgement(packet); }
    }

    //ALFB
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

    public class ApplicationLayerFeedback
    {
        public RtcpPacket ToPacket(byte? channel = null)
        {
            return new RtcpFeedbackPacket(FeedbackControlInformationType.ApplicationLayerFeedback, RtcpPacket.RtcpPacketType.PayloadSpecificFeedback, ToBytes())
            {
                Channel = channel ?? Channel,
                SenderSynchronizationSourceIdentifier = SenderSynchronizationSourceIdentifier,
                SourceSynchronizationSourceIdentifier = SourceSynchronizationSourceIdentifier
            };
        }

        public byte[] ToBytes() { return FeedbackData; }

        public ApplicationLayerFeedback(RtcpFeedbackPacket packet)
            : this(packet.FeedbackControlInformation, 0)
        {
            Created = packet.Created;
            Channel = packet.Channel;
            Sent = packet.Sent;
            if (packet.PacketType != RtcpPacket.RtcpPacketType.PayloadSpecificFeedback || packet.FeedbackFormat != FeedbackControlInformationType.ApplicationLayerFeedback) throw new InvalidOperationException("Exptected: W=Z, Y=Z. Found: \"" + '"');
            SenderSynchronizationSourceIdentifier = packet.SenderSynchronizationSourceIdentifier;
            SourceSynchronizationSourceIdentifier = packet.SourceSynchronizationSourceIdentifier;
        }

        public ApplicationLayerFeedback(byte[] packet, int index)
        {
            if (index < packet.Length)
            {
                int len = packet.Length - index;
                FeedbackData = new byte[len];
                Array.Copy(packet, index, FeedbackData, 0, len);
            }
        }

        public byte? Channel { get; set; }
        public DateTime? Created { get; set; }
        public DateTime? Sent { get; set; }
        public uint SenderSynchronizationSourceIdentifier, SourceSynchronizationSourceIdentifier;
        public byte[] FeedbackData;

        public static implicit operator RtcpPacket(ApplicationLayerFeedback alfb) { return alfb.ToPacket(alfb.Channel); }

        public static implicit operator ApplicationLayerFeedback(RtcpPacket packet) { return new ApplicationLayerFeedback(packet); }

    }

    //PLI
    /*
     6.3.1.  Picture Loss Indication (PLI)

   The PLI FB message is identified by PT=PSFB and FMT=1.

   There MUST be exactly one PLI contained in the FCI field.

6.3.1.1.  Semantics

   With the Picture Loss Indication message, a decoder informs the
   encoder about the loss of an undefined amount of coded video data
   belonging to one or more pictures.  When used in conjunction with any
   video coding scheme that is based on inter-picture prediction, an
   encoder that receives a PLI becomes aware that the prediction chain
   may be broken.  The sender MAY react to a PLI by transmitting an
   intra-picture to achieve resynchronization (making this message
   effectively similar to the FIR message as defined in [6]); however,
   the sender MUST consider congestion control as outlined in Section 7,
   which MAY restrict its ability to send an intra frame.

   Other RTP payload specifications such as RFC 2032 [6] already define
   a feedback mechanism for some for certain codecs.  An application
   supporting both schemes MUST use the feedback mechanism defined in
   this specification when sending feedback.  For backward compatibility
   reasons, such an application SHOULD also be capable to receive and
   react to the feedback scheme defined in the respective RTP payload
   format, if this is required by that payload format.

6.3.1.2.  Message Format

   PLI does not require parameters.  Therefore, the length field MUST be
   2, and there MUST NOT be any Feedback Control Information.

   The semantics of this FB message is independent of the payload type.

6.3.1.3.  Timing Rules

   The timing follows the rules outlined in Section 3.  In systems that
   employ both PLI and other types of feedback, it may be advisable to
   follow the Regular RTCP RR timing rules for PLI, since PLI is not as
   delay critical as other FB types.

6.3.1.4.  Remarks

   PLI messages typically trigger the sending of full intra-pictures.
   Intra-pictures are several times larger then predicted (inter-)
   pictures.  Their size is independent of the time they are generated.
   In most environments, especially when employing bandwidth-limited
   links, the use of an intra-picture implies an allowed delay that is a
   significant multitude of the typical frame duration.  An example: If
   the sending frame rate is 10 fps, and an intra-picture is assumed to
   be 10 times as big as an inter-picture, then a full second of latency
   has to be accepted.  In such an environment, there is no need for a
   particular short delay in sending the FB message.  Hence, waiting for
   the next possible time slot allowed by RTCP timing rules as per [2]
   with Tmin=0 does not have a negative impact on the system
   performance.
     */

    public class PictureLossIndication
    {
        public RtcpPacket ToPacket(byte? channel = null)
        {
            return new RtcpFeedbackPacket(FeedbackControlInformationType.PictureLossIndication, RtcpPacket.RtcpPacketType.PayloadSpecificFeedback, null)
            {
                Channel = channel ?? Channel,                
                SenderSynchronizationSourceIdentifier = SenderSynchronizationSourceIdentifier,
                SourceSynchronizationSourceIdentifier = SourceSynchronizationSourceIdentifier
            };
        }

        public PictureLossIndication(RtcpFeedbackPacket packet)
            : this(packet.FeedbackControlInformation, 0)
        {
            Created = packet.Created;
            Channel = packet.Channel;
            Sent = packet.Sent;
            if (packet.PacketType != RtcpPacket.RtcpPacketType.PayloadSpecificFeedback || packet.FeedbackFormat != FeedbackControlInformationType.PictureLossIndication) throw new InvalidOperationException("Exptected: W=Z, Y=Z. Found: \"" + '"');
            SenderSynchronizationSourceIdentifier = packet.SenderSynchronizationSourceIdentifier;
            SourceSynchronizationSourceIdentifier = packet.SourceSynchronizationSourceIdentifier;
        }

        public PictureLossIndication(byte[] packet, int index)
        {

        }

        public byte? Channel { get; set; }
        public DateTime? Created { get; set; }
        public DateTime? Sent { get; set; }
        uint SenderSynchronizationSourceIdentifier, SourceSynchronizationSourceIdentifier;

        public static implicit operator RtcpPacket(PictureLossIndication pli) { return pli.ToPacket(pli.Channel); }

        public static implicit operator PictureLossIndication(RtcpPacket packet) { return new PictureLossIndication(packet); }

    }

    //SLI
    /*
     6.3.2.2.  Format

   The Slice Loss Indication uses one additional FCI field, the content
   of which is depicted in Figure 6.  The length of the FB message MUST
   be set to 2+n, with n being the number of SLIs contained in the FCI
   field.

    0                   1                   2                   3
    0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |            First        |        Number           | PictureID |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+

            Figure 6: Syntax of the Slice Loss Indication (SLI)

   First: 13 bits
      The macroblock (MB) address of the first lost macroblock.  The MB
      numbering is done such that the macroblock in the upper left
      corner of the picture is considered macroblock number 1 and the
      number for each macroblock increases from left to right and then
      from top to bottom in raster-scan order (such that if there is a
      total of N macroblocks in a picture, the bottom right macroblock
      is considered macroblock number N).
     */

    public class SliceLossIndication
    {
        public RtcpPacket ToPacket(byte? channel = null)
        {
            return new RtcpFeedbackPacket(FeedbackControlInformationType.SliceLossIndication, RtcpPacket.RtcpPacketType.PayloadSpecificFeedback, ToBytes())
            {
                Channel = channel ?? Channel,                
                SenderSynchronizationSourceIdentifier = SenderSynchronizationSourceIdentifier,
                SourceSynchronizationSourceIdentifier = SourceSynchronizationSourceIdentifier
            };
        }

        public byte[] ToBytes()
        {
            List<byte> result = new List<byte>();
            result.AddRange(BitConverter.GetBytes(Utility.ReverseUnsignedShort(First) << 24 | Utility.ReverseUnsignedShort(Number) << 12 | PictureID));
            return result.ToArray();
        }

        public SliceLossIndication(RtcpFeedbackPacket packet)
            : this(packet.FeedbackControlInformation, 0)
        {
            Created = packet.Created;
            Channel = packet.Channel;
            Sent = packet.Sent;
            if (packet.PacketType != RtcpPacket.RtcpPacketType.PayloadSpecificFeedback || packet.FeedbackFormat != FeedbackControlInformationType.SliceLossIndication) throw new InvalidOperationException("Exptected: W=Z, Y=Z. Found: \"" + '"');
            SenderSynchronizationSourceIdentifier = packet.SenderSynchronizationSourceIdentifier;
            SourceSynchronizationSourceIdentifier = packet.SourceSynchronizationSourceIdentifier;
        }

        public SliceLossIndication(byte[] packet, int index)
        {
            uint composite = BitConverter.ToUInt32(packet, index);
            First = Utility.ReverseUnsignedShort((ushort)((composite & 0x7FFFFFF) >> 24));
            Number = Utility.ReverseUnsignedShort((ushort)((composite & 0xFFF00) >> 24));
            PictureID = (byte)(composite & 0x3F);
        }

        public byte? Channel { get; set; }
        public DateTime? Created { get; set; }
        public DateTime? Sent { get; set; }
        public uint SenderSynchronizationSourceIdentifier, SourceSynchronizationSourceIdentifier;

        public ushort First, Number; public byte PictureID;

        public static implicit operator RtcpPacket(SliceLossIndication sli) { return sli.ToPacket(sli.Channel); }

        public static implicit operator SliceLossIndication(RtcpPacket packet) { return new SliceLossIndication(packet); }

    }

    //RPSI
    /*
     6.3.3.2.  Format

   The FCI for the RPSI message follows the format depicted in Figure 7:

    0                   1                   2                   3
    0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |      PB       |0| Payload Type|    Native RPSI bit string     |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |   defined per codec          ...                | Padding (0) |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
     */

    public class ReferencePictureSelectionIndication
    {
        ReferencePictureSelectionIndication(byte[] packet, int index)
        {
            PaddedBits = packet[index++];
            PayloadType = packet[index++];
            int len = packet.Length - index;
            NativeRPSIBitString = new byte[len];
            System.Array.Copy(packet, 0, NativeRPSIBitString, 0, len);            
        }

        public ReferencePictureSelectionIndication(RtcpFeedbackPacket packet)
            : this(packet.FeedbackControlInformation, 0)
        {
            Created = packet.Created;
            Channel = packet.Channel;
            Sent = packet.Sent;
            if (packet.PacketType != RtcpPacket.RtcpPacketType.PayloadSpecificFeedback || packet.FeedbackFormat != FeedbackControlInformationType.ReferencePictureSelectionIndication) throw new InvalidOperationException("Exptected: W=Z, Y=Z. Found: \"" + '"');
            SenderSynchronizationSourceIdentifier = packet.SenderSynchronizationSourceIdentifier;
            SourceSynchronizationSourceIdentifier = packet.SourceSynchronizationSourceIdentifier;
        }

        byte PaddedBits { get; set; }
        byte PayloadType { get; set; }
        byte[] NativeRPSIBitString { get; set; }

        public byte? Channel { get; set; }
        public DateTime? Created { get; set; }
        public DateTime? Sent { get; set; }
        public uint SenderSynchronizationSourceIdentifier, SourceSynchronizationSourceIdentifier;

        public byte[] ToBytes() { List<byte> result = new List<byte>(); result.Add(PaddedBits); result.Add(PayloadType); result.AddRange(NativeRPSIBitString); return result.ToArray(); }

        public RtcpPacket ToPacket(byte? channel = null)
        {
            return new RtcpFeedbackPacket(FeedbackControlInformationType.ReferencePictureSelectionIndication, RtcpPacket.RtcpPacketType.PayloadSpecificFeedback, ToBytes())
            {
                Channel = channel ?? Channel,
                SenderSynchronizationSourceIdentifier = SenderSynchronizationSourceIdentifier,
                SourceSynchronizationSourceIdentifier = SourceSynchronizationSourceIdentifier
            };
        }

        public static implicit operator RtcpPacket(ReferencePictureSelectionIndication rpsi) { return rpsi.ToPacket(rpsi.Channel); }

        public static implicit operator ReferencePictureSelectionIndication(RtcpPacket packet) { return new ReferencePictureSelectionIndication(packet); }

    }

}
