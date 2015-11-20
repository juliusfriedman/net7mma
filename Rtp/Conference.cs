/*
This file came from Managed Media Aggregation, You can always find the latest version @ https://net7mma.codeplex.com/
  
 Julius.Friedman@gmail.com / (SR. Software Engineer ASTI Transportation Inc. http://www.asti-trans.com)

Permission is hereby granted, free of charge, 
 * to any person obtaining a copy of this software and associated documentation files (the "Software"), 
 * to deal in the Software without restriction, 
 * including without limitation the rights to :
 * use, 
 * copy, 
 * modify, 
 * merge, 
 * publish, 
 * distribute, 
 * sublicense, 
 * and/or sell copies of the Software, 
 * and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * 
 * 
 * JuliusFriedman@gmail.com should be contacted for further details.

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. 
 * 
 * IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, 
 * DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, 
 * TORT OR OTHERWISE, 
 * ARISING FROM, 
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 * 
 * v//
 */
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

        public RtpClient Client { get; protected set; }

        //May a diction would be better
        internal System.Collections.Generic.HashSet<RtpClient> Clients = new System.Collections.Generic.HashSet<RtpClient>();

        /// <summary>
        /// One id may correspond to multiple context's, determine how context will find rtpClient if needed.
        /// </summary>
        internal Common.Collections.Generic.ConcurrentThesaurus<int, RtpClient.TransportContext> SourceTable = new Common.Collections.Generic.ConcurrentThesaurus<int, RtpClient.TransportContext>();

        //Offer ; Answer 

        /*
         
         * TODO
         * 
        A Session / Conference can be created to faciliate the reporting process and keep track of last active senders / receivers if required

        E.g. it will have a source table use the above algorithms and may be abstract or provide only a partial implementation
        
        Rtcp Scheduling should be implemented in the conference level see => http://tools.ietf.org/html/rfc3550#appendix-A.7
         
        It is up to the conference to handle SDP Announcement for new members and changes in the conference.
        
         */

        Conference() { throw new System.NotImplementedException("See comments"); }

        //When adding a client iterate all SourceContext and set SendRtcpReports = false;
        //Attach event for RtcpPacket reception and then determine here what to send and to who

        //When removing a client iterate all SourceContext and set SendRtcpReports = true;
        //Remove event for RtcpPacket reception

        //On each packet reception Schedule a packet if the Client has not Sent a RtcpReport in the appropriate amount of time.

    }
}
