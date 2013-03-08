using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.Rtcp
{

    //https://nmparsers.svn.codeplex.com/svn/Develop_Branch/NPL/common/rtcp.npl

    //http://www.faqs.org/rfcs/rfc6642.html

    //http://tools.ietf.org/rfc/rfc4585.txt
    class PayloadSpecificFeedback
    {
        

    }

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

}
