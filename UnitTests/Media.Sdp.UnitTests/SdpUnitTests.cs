#region Copyright
/*
This file came from Managed Media Aggregation, You can always find the latest version @ https://net7mma.codeplex.com/
  
 History:
 10 Feb 2015    Aaron Clauson aaron@sipsorcery.com  Created.

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
#endregion

using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Media.Sdp;

namespace Media.Sdp.UnitTests
{
    [TestClass]
    public class SDPUnitTests
    {
        private static string m_CRLF = "\r\n";

        [TestMethod]
        public void ParseSDPUnitTest()
        {
            Console.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);

            string sdpStr =
                "v=0" + m_CRLF +
                "o=root 3285 3285 IN IP4 10.0.0.4" + m_CRLF +
                "s=session" + m_CRLF +
                "c=IN IP4 10.0.0.4" + m_CRLF +
                "t=0 0" + m_CRLF +
                "m=audio 12228 RTP/AVP 0 101" + m_CRLF +
                "a=rtpmap:0 PCMU/8000" + m_CRLF +
                "a=rtpmap:101 telephone-event/8000" + m_CRLF +
                "a=fmtp:101 0-16" + m_CRLF +
                "a=silenceSupp:off - - - -" + m_CRLF +
                "a=ptime:20" + m_CRLF +
                "a=sendrecv";

            SessionDescription sdp = new SessionDescription(sdpStr);

            Debug.WriteLine(sdp.ToString());

            Assert.AreEqual("10.0.0.4", sdp.ConnectionLine.Parts[3], "The connection address was not parsed  correctly.");  // ToDo: Be better if "Part[3]" was referred to by ConnectionAddress.
            Assert.AreEqual(MediaType.audio, sdp.MediaDescriptions.First().MediaType, "The media type not parsed correctly.");
            Assert.AreEqual(12228, sdp.MediaDescriptions.First().MediaPort, "The connection port was not parsed correctly.");
            Assert.AreEqual(0, sdp.MediaDescriptions.First().MediaFormat, "The first media format was incorrect.");         // ToDo: Can't cope with multiple media formats?
            //Assert.IsTrue(sdp.Media[0].MediaFormats[0].FormatID == 0, "The highest priority media format ID was incorrect.");
            //Assert.IsTrue(sdp.Media[0].MediaFormats[0].Name == "PCMU", "The highest priority media format name was incorrect.");
            //Assert.IsTrue(sdp.Media[0].MediaFormats[0].ClockRate == 8000, "The highest priority media format clockrate was incorrect.");
            Assert.AreEqual("PCMU/8000", sdp.MediaDescriptions.First().RtpMapLine.Parts[1], "The rtpmap line for the PCM format was not parsed correctly.");  // ToDo "Parts" should be put into named properties where possible.  
        }

        [TestMethod]
        public void ParseBriaSDPUnitTest()
        {
            Console.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);
            string sdpStr = "v=0\r\no=- 5 2 IN IP4 10.1.1.2\r\ns=CounterPath Bria\r\nc=IN IP4 144.137.16.240\r\nt=0 0\r\nm=audio 34640 RTP/AVP 0 8 101\r\na=sendrecv\r\na=rtpmap:101 telephone-event/8000\r\na=fmtp:101 0-15\r\na=alt:1 1 : STu/ZtOu 7hiLQmUp 10.1.1.2 34640\r\n";

            SessionDescription sdp = new SessionDescription(sdpStr);

            Debug.WriteLine(sdp.ToString());

            Assert.AreEqual("144.137.16.240", sdp.ConnectionLine.Parts[3], "The connection address was not parsed correctly.");
            Assert.AreEqual(34640, sdp.MediaDescriptions.First().MediaPort, "The connection port was not parsed correctly.");
            Assert.AreEqual(0, sdp.MediaDescriptions.First().MediaFormat, "The highest priority media format ID was incorrect.");
        }

        [TestMethod]
        public void ParseICESessionAttributesUnitTest()
        {
            Console.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);

            string sdpStr =
              "v=0" + m_CRLF +
              "o=jdoe 2890844526 2890842807 IN IP4 10.0.1.1" + m_CRLF +
              "s=" + m_CRLF +
              "c=IN IP4 192.0.2.3" + m_CRLF +
              "t=0 0" + m_CRLF +
              "a=ice-pwd:asd88fgpdd777uzjYhagZg" + m_CRLF +
              "a=ice-ufrag:8hhY" + m_CRLF +
              "m=audio 45664 RTP/AVP 0" + m_CRLF +
              "b=RS:0" + m_CRLF +
              "b=RR:0" + m_CRLF +
              "a=rtpmap:0 PCMU/8000" + m_CRLF +
              "a=candidate:1 1 UDP 2130706431 10.0.1.1 8998 typ host" + m_CRLF +
              "a=candidate:2 1 UDP 1694498815 192.0.2.3 45664 typ srflx raddr 10.0.1.1 rport 8998";

            SessionDescription sdp = new SessionDescription(sdpStr);

            Debug.WriteLine(sdp.ToString());

            //ToDo: Add ICE attributes.
            //Assert.AreEqual("8hhY", sdp.IceUfrag, "The ICE username was not parsed correctly.");    
            //Assert.AreEqual( "asd88fgpdd777uzjYhagZg", sdp.IcePwd, "The ICE password was not parsed correctly.");
        }


        /// <summary>
        /// Test that an SDP payload with multiple media announcements (in this test audio and video) are correctly
        /// parsed.
        /// </summary>
        [TestMethod]
        public void ParseMultipleMediaAnnouncementsUnitTest()
        {
            Console.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);

            string sdpStr = "v=0" + m_CRLF +
                "o=- 13064410510996677 3 IN IP4 10.1.1.2" + m_CRLF +
                "s=Bria 4 release 4.1.1 stamp 74246" + m_CRLF +
                "c=IN IP4 10.1.1.2" + m_CRLF +
                "b=AS:2064" + m_CRLF +
                "t=0 0" + m_CRLF +
                "m=audio 49290 RTP/AVP 0" + m_CRLF +
                "a=sendrecv" + m_CRLF +
                "m=video 56674 RTP/AVP 96" + m_CRLF +
                "b=TIAS:2000000" + m_CRLF +
                "a=rtpmap:96 VP8/90000" + m_CRLF +
                "a=sendrecv" + m_CRLF +
                "a=rtcp-fb:* nack pli";

            SessionDescription sdp = new SessionDescription(sdpStr);

            Debug.WriteLine(sdp.ToString());

            Assert.AreEqual(2, sdp.MediaDescriptions.Count());
            Assert.AreEqual(49290, sdp.MediaDescriptions.Where(x => x.MediaType == MediaType.audio).First().MediaPort);
            Assert.AreEqual(56674, sdp.MediaDescriptions.Where(x => x.MediaType == MediaType.video).First().MediaPort);
        }
    }
}
