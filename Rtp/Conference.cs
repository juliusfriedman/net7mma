using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Rtp
{
    /// <summary>
    /// http://tools.ietf.org/html/rfc3550#section-6.2.1
    /// Each RtpClient has it's own source table.
    /// 
    /// Since tables can potentially be very large. i.e. http://tools.ietf.org/html/rfc2762
    /// The state information could get very large and thus the table may not be able to hold all participants in communication...
    /// 
    /// </summary>
    public class Conference
    {

        internal HashSet<RtpClient> Clients = new HashSet<RtpClient>();

        internal System.Collections.Concurrent.ConcurrentDictionary<int, RtpClient.TransportContext> SourceTable = new System.Collections.Concurrent.ConcurrentDictionary<int, RtpClient.TransportContext>();

        /*
         
         * TODO
         * 
        A Session / Conference can be created to faciliate the reporting process and keep track of last active senders / receivers if required

        E.g. it will have a source table use the above algorithms and may be abstract or provide only a partial implementation
        
        Rtcp Scheduling should be implemented in the conference level see => http://tools.ietf.org/html/rfc3550#appendix-A.7
         
        It is up to the conference to handle SDP Announcement for new members and changes in the conference.
        
         */

        Conference() { throw new NotImplementedException("See comments"); }

    }
}
