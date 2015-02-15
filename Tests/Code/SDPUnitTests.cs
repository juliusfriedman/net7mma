#region Copyright
/*
This file came from Managed Media Aggregation, You can always find the latest version @ https://net7mma.codeplex.com/
  
 History:
 10 Feb 2015    Aaron Clauson aaron@sipsorcery.com  Created.
 11 Feb 2015    Julius Friedman juliusfriedman@gmail.com  Removed m_CRLF. Added additional unit tests to support existing logic.
 
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
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Defines a class which verifies the intended functionality by providing tests to the logical units of work related to the <see cref="Media.Sdp"/> namespace.
/// </summary>
public class SDPUnitTests
{
    /// <summary>
    /// Test the constructor
    /// </summary>
    public void ATestSessionDescriptionConstructor()
    {

        Console.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);

        System.Diagnostics.Debug.Assert(false == string.IsNullOrEmpty(Media.Sdp.SessionDescription.NewLine), "Media.Sdp.SessionDescription.NewLine Must not be Null or Empty.");

        //Get the characters which make the NewLine string.
        char[] newLineCharacters = Media.Sdp.SessionDescription.NewLine.ToArray();

        //Check for two characters
        System.Diagnostics.Debug.Assert(2 == newLineCharacters.Length, "Media.Sdp.SessionDescription.NewLine Must Have 2 Characters");

        //Check for '\r'
        System.Diagnostics.Debug.Assert('\r' == newLineCharacters[0], "Media.Sdp.SessionDescription.NewLine[0] Must Equal '\r'");

        //Check for '\n'
        System.Diagnostics.Debug.Assert('\n' == newLineCharacters[1], "Media.Sdp.SessionDescription.NewLine[0] Must Equal '\n'");
    }

    public void ParseMediaDescriptionUnitTest()
    {
        string testVector = @" m=audio 49230 RTP/AVP 96 97 98
            a=rtpmap:96 L8/8000
            a=rtpmap:97 L16/8000
            a=rtpmap:98 L16/11025/2";

        using (var md = new Media.Sdp.MediaDescription(testVector))
        {
            System.Diagnostics.Debug.Assert(md.Lines.Count() == 4, "MediaDescription must have 4 lines");

            //CLR not assert correctly with == ....
            //md.MediaDescriptionLine.ToString() == "m=audio 49230 RTP/AVP 96 97 98"

            System.Diagnostics.Debug.Assert(md.PayloadTypes.Count() == 3, "Could not read the Payload List");

            System.Diagnostics.Debug.Assert(md.PayloadTypes.First() == 96, "Could not read the Payload List");

            System.Diagnostics.Debug.Assert(md.PayloadTypes.ToArray()[1] == 97, "Could not read the Payload List");

            System.Diagnostics.Debug.Assert(md.PayloadTypes.Last() == 98, "Could not read the Payload List");

            System.Diagnostics.Debug.Assert(string.Compare(md.MediaDescriptionLine.ToString(), "m=audio 49230 RTP/AVP 96 97 98\r\n") == 0, "Did not handle Payload List Correct");

        }
    }

    public void ParseSDPUnitTest()
    {
        Console.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);

        string sdpStr =
            "v=0" + Media.Sdp.SessionDescription.NewLine +
            "o=root 3285 3285 IN IP4 10.0.0.4" + Media.Sdp.SessionDescription.NewLine +
            "s=session" + Media.Sdp.SessionDescription.NewLine +
            "c=IN IP4 10.0.0.4" + Media.Sdp.SessionDescription.NewLine +
            "t=0 0" + Media.Sdp.SessionDescription.NewLine +
            "m=audio 12228 RTP/AVP 0 101" + Media.Sdp.SessionDescription.NewLine +
            "a=rtpmap:0 PCMU/8000" + Media.Sdp.SessionDescription.NewLine +
            "a=rtpmap:101 telephone-event/8000" + Media.Sdp.SessionDescription.NewLine +
            "a=fmtp:101 0-16" + Media.Sdp.SessionDescription.NewLine +
            "a=silenceSupp:off - - - -" + Media.Sdp.SessionDescription.NewLine +
            "a=ptime:20" + Media.Sdp.SessionDescription.NewLine +
            "a=sendrecv";

        Media.Sdp.SessionDescription sdp = new Media.Sdp.SessionDescription(sdpStr);

        System.Diagnostics.Debug.WriteLine(sdp.ToString());

        System.Diagnostics.Debug.Assert("10.0.0.4" == sdp.ConnectionLine.Parts[2], "The connection address was not parsed  correctly.");  // ToDo: Be better if "Part[3]" was referred to by ConnectionAddress.
        System.Diagnostics.Debug.Assert(Media.Sdp.MediaType.audio == sdp.MediaDescriptions.First().MediaType, "The media type not parsed correctly.");
        System.Diagnostics.Debug.Assert(12228 == sdp.MediaDescriptions.First().MediaPort, "The connection port was not parsed correctly.");
        System.Diagnostics.Debug.Assert(0 == sdp.MediaDescriptions.First().PayloadTypes.First(), "The first media format was incorrect.");         // ToDo: Can't cope with multiple media formats?
        //Assert.IsTrue(sdp.Media[0].MediaFormats[0].FormatID == 0, "The highest priority media format ID was incorrect.");
        //Assert.IsTrue(sdp.Media[0].MediaFormats[0].Name == "PCMU", "The highest priority media format name was incorrect.");
        //Assert.IsTrue(sdp.Media[0].MediaFormats[0].ClockRate == 8000, "The highest priority media format clockrate was incorrect.");
        System.Diagnostics.Debug.Assert("rtpmap:0 PCMU/8000" == sdp.MediaDescriptions.First().RtpMapLine.Parts[0], "The rtpmap line for the PCM format was not parsed correctly.");  // ToDo "Parts" should be put into named properties where possible.  
    }

    public void CreateMediaDesciptionTest()
    {
        //RtpClient has the following property
        //Media.Rtp.RtpClient.AvpProfileIdentifier
        //I don't think it should be specified in the SDP Classes but I can figure out something else if desired.

        string profile = "RTP/AVP";

        Media.Sdp.MediaType mediaType = Media.Sdp.MediaType.audio;

        int mediaPort = 15000;

        //Iterate all possible byte values (should do a seperate test for the list of values?)
        for (int mediaFormat = 0; mediaFormat <= 999; ++mediaFormat)
        {
            //Create a MediaDescription
            using (var mediaDescription = new Media.Sdp.MediaDescription(mediaType, mediaPort, profile, mediaFormat))
            {
                System.Diagnostics.Debug.Assert(mediaDescription.MediaProtocol == profile, "Did not find MediaProtocol '" + profile + "'");

                System.Diagnostics.Debug.Assert(mediaDescription.PayloadTypes.Count() == 1, "Found more then 1 payload type in the PayloadTypes List");

                System.Diagnostics.Debug.Assert(mediaDescription.PayloadTypes.First() == mediaFormat, "Did not find correct MediaFormat");

                System.Diagnostics.Debug.Assert(mediaDescription.ToString() == string.Format("m={0} {1} RTP/AVP {2}\r\n", mediaType, mediaPort, mediaFormat), "Did not output correct result");
            }
        }
    }

    public void CreateSessionDescriptionUnitTest()
    {
        string originatorAndSession = String.Format("{0} {1} {2} {3} {4} {5}", "-", "62464", "0", "IN", "IP4", "10.1.1.2");

        string profile = "RTP/AVP";

        Media.Sdp.MediaType mediaType = Media.Sdp.MediaType.audio;

        int mediaPort = 15000;

        int mediaFormat = 0;

        string sessionName = "MySessionName";

        using (var audioDescription = new Media.Sdp.SessionDescription(mediaFormat, originatorAndSession, sessionName))
        {
            //Ensure the correct SessionDescriptionVersion was set
            System.Diagnostics.Debug.Assert(audioDescription.SessionDescriptionVersion == 0, "Did not find Correct SessionDescriptionVersion");

            //When created the version of the `o=` line should be 0 (or 1?)
            System.Diagnostics.Debug.Assert(audioDescription.DocumentVersion == 0, "Did not find Correct SessionVersion");

            //Add the MediaDescription
            audioDescription.Add(new Media.Sdp.MediaDescription(Media.Sdp.MediaType.audio, mediaPort, profile, 0), false);

            //update version was specified false so the verison of the document should not change
            System.Diagnostics.Debug.Assert(audioDescription.DocumentVersion == 0, "Did not find Correct SessionVersion");

            //Determine what the output should look like
            string expected = string.Format("v=0\r\no={0}\r\ns={1}\r\nm={2} {3} RTP/AVP {4}\r\n", originatorAndSession, sessionName, mediaType, mediaPort, mediaFormat);

            //Make a string from the instance
            string actual = audioDescription.ToString();

            //Check the result of the comparsion
            System.Diagnostics.Debug.Assert(string.Compare(expected, actual) == 0, "Did not output expected result");
        }
    }

    public void CreateSessionDescriptionAndSetConnectionLine()
    {
        using (Media.Sdp.SessionDescription sdp = new Media.Sdp.SessionDescription(0, "v√ƒ", "Bandit"))
        {
            sdp.Add(new Media.Sdp.Lines.SessionConnectionLine()
            {
                ConnectionNetworkType = "IN",
                ConnectionAddressType = "*",
                ConnectionAddress = "0.0.0.0"
            });

            System.Diagnostics.Debug.Assert(sdp.Lines.Count() == 4, "Did not have correct amount of Lines");

            string expected = "v=0\r\no=v√ƒ  -9223372036854775807   \r\ns=Bandit\r\nc=IN * 0.0.0.0\r\nc=IN * 0.0.0.0\r\n";

            System.Diagnostics.Debug.Assert(string.Compare(sdp.ToString(), expected) == 0, "Did not output correct result.");
        }
    }

    public void ParseBriaSDPUnitTest()
    {
        Console.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);

        string sdpStr = "v=0\r\no=- 5 2 IN IP4 10.1.1.2\r\ns=CounterPath Bria\r\nc=IN IP4 144.137.16.240\r\nt=0 0\r\nm=audio 34640 RTP/AVP 0 8 101\r\na=sendrecv\r\na=rtpmap:101 telephone-event/8000\r\na=fmtp:101 0-15\r\na=alt:1 1 : STu/ZtOu 7hiLQmUp 10.1.1.2 34640\r\n";

        Media.Sdp.SessionDescription sdp = new Media.Sdp.SessionDescription(sdpStr);

        System.Diagnostics.Debug.WriteLine(sdp.ToString());

        System.Diagnostics.Debug.Assert("144.137.16.240" == sdp.ConnectionLine.Parts[2], "The connection address was not parsed correctly.");
        System.Diagnostics.Debug.Assert(34640 == sdp.MediaDescriptions.First().MediaPort, "The connection port was not parsed correctly.");
        System.Diagnostics.Debug.Assert(0 == sdp.MediaDescriptions.First().PayloadTypes.First(), "The highest priority media format ID was incorrect.");
    }

    public void ParseICESessionAttributesUnitTest()
    {
        Console.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);

        string sdpStr =
          "v=0" + Media.Sdp.SessionDescription.NewLine +
          "o=jdoe 2890844526 2890842807 IN IP4 10.0.1.1" + Media.Sdp.SessionDescription.NewLine +
          "s=" + Media.Sdp.SessionDescription.NewLine +
          "c=IN IP4 192.0.2.3" + Media.Sdp.SessionDescription.NewLine +
          "t=0 0" + Media.Sdp.SessionDescription.NewLine +
          "a=ice-pwd:asd88fgpdd777uzjYhagZg" + Media.Sdp.SessionDescription.NewLine +
          "a=ice-ufrag:8hhY" + Media.Sdp.SessionDescription.NewLine +
          "m=audio 45664 RTP/AVP 0" + Media.Sdp.SessionDescription.NewLine +
          "b=RS:0" + Media.Sdp.SessionDescription.NewLine +
          "b=RR:0" + Media.Sdp.SessionDescription.NewLine +
          "a=rtpmap:0 PCMU/8000" + Media.Sdp.SessionDescription.NewLine +
          "a=candidate:1 1 UDP 2130706431 10.0.1.1 8998 typ host" + Media.Sdp.SessionDescription.NewLine +
          "a=candidate:2 1 UDP 1694498815 192.0.2.3 45664 typ srflx raddr 10.0.1.1 rport 8998";

        Media.Sdp.SessionDescription sdp = new Media.Sdp.SessionDescription(sdpStr);

        System.Diagnostics.Debug.WriteLine(sdp.ToString());

        //ToDo: Add ICE attributes.
        //System.Diagnostics.Debug.Assert("8hhY" == sdp.IceUfrag, "The ICE username was not parsed correctly.");
        //System.Diagnostics.Debug.Assert("asd88fgpdd777uzjYhagZg" == sdp.IcePwd, "The ICE password was not parsed correctly.");
    }

    /// <summary>
    /// Test that an SDP payload with multiple media announcements (in this test audio and video) are correctly
    /// parsed.
    /// </summary>
    public void ParseMultipleMediaAnnouncementsUnitTest()
    {
        Console.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);

        string sdpStr = "v=0" + Media.Sdp.SessionDescription.NewLine +
            "o=- 13064410510996677 3 IN IP4 10.1.1.2" + Media.Sdp.SessionDescription.NewLine +
            "s=Bria 4 release 4.1.1 stamp 74246" + Media.Sdp.SessionDescription.NewLine +
            "c=IN IP4 10.1.1.2" + Media.Sdp.SessionDescription.NewLine +
            "b=AS:2064" + Media.Sdp.SessionDescription.NewLine +
            "t=0 0" + Media.Sdp.SessionDescription.NewLine +
            "m=audio 49290 RTP/AVP 0" + Media.Sdp.SessionDescription.NewLine +
            "a=sendrecv" + Media.Sdp.SessionDescription.NewLine +
            "m=video 56674 RTP/AVP 96" + Media.Sdp.SessionDescription.NewLine +
            "b=TIAS:2000000" + Media.Sdp.SessionDescription.NewLine +
            "a=rtpmap:96 VP8/90000" + Media.Sdp.SessionDescription.NewLine +
            "a=sendrecv" + Media.Sdp.SessionDescription.NewLine +
            "a=rtcp-fb:* nack pli";

        Media.Sdp.SessionDescription sdp = new Media.Sdp.SessionDescription(sdpStr);

        System.Diagnostics.Debug.WriteLine(sdp.ToString());

        System.Diagnostics.Debug.Assert(2 == sdp.MediaDescriptions.Count());
        System.Diagnostics.Debug.Assert(49290 == sdp.MediaDescriptions.Where(x => x.MediaType == Media.Sdp.MediaType.audio).First().MediaPort);
        System.Diagnostics.Debug.Assert(56674 == sdp.MediaDescriptions.Where(x => x.MediaType == Media.Sdp.MediaType.video).First().MediaPort);
    }

    /// <summary>
    /// Tests various attributes of the <see cref="Media.Sdp.MediaDescription"/> class.
    /// </summary>
    public void TestMediaDescription()
    {
        Console.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);

        string testVector = @"v=0
o=- 1419841619185835 1 IN IP4 192.168.1.208
s=IP Camera Video
i=videoMain
a=tool:LIVE555 Streaming Media v2014.02.10
a=type:broadcast
a=control:*
a=range:npt=0-
a=x-qt-text-nam:IP Camera Video
a=x-qt-text-inf:videoMain
t=0
r=604800 3600 0 90000
r=7d 1h 0 25h
m=video 0 RTP/AVP 96
c=IN IP4 0.0.0.0
b=AS:96
a=rtpmap:96 H264/90000
a=fmtp:96 packetization-mode=1;profile-level-id=000000;sprop-parameter-sets=Z0IAHpWoKA9k,aM48gA==
a=control:track1
m=audio 0 RTP/AVP 0
c=IN IP4 0.0.0.0
b=AS:64
a=control:track2";

        Media.Sdp.SessionDescription sessionDescription = new Media.Sdp.SessionDescription(testVector);

        //Ensure 2 media descriptions were found
        System.Diagnostics.Debug.Assert(sessionDescription.MediaDescriptions.Count() == 2, "Did not find all Media Descriptions '2'");

        System.Diagnostics.Debug.Assert(sessionDescription.MediaDescriptions.First().MediaFormat == "96", "Did not find correct MediaFormat '96'");

        System.Diagnostics.Debug.Assert(sessionDescription.MediaDescriptions.First().MediaType == Media.Sdp.MediaType.video, "Did not find correct MediaType 'video'");

        System.Diagnostics.Debug.Assert(sessionDescription.MediaDescriptions.Last().MediaFormat == "0", "Did not find correct MediaFormat '0'");

        System.Diagnostics.Debug.Assert(sessionDescription.MediaDescriptions.Last().MediaType == Media.Sdp.MediaType.audio, "Did not find correct MediaType 'audio;");

        System.Diagnostics.Debug.Assert(false == sessionDescription.MediaDescriptions.Any(m => m.ControlLine == null), "All MediaDescriptons must have a ControlLine which is not null.");

        //Check time descriptions repeat times
        System.Diagnostics.Debug.Assert(sessionDescription.TimeDescriptions.Count() == 1, "Must have 1 TimeDescription");

        System.Diagnostics.Debug.Assert(sessionDescription.TimeDescriptions.First().SessionStartTime == 0, "Did not parse SessionStartTime");

        System.Diagnostics.Debug.Assert(sessionDescription.TimeDescriptions.First().SessionStopTime == 0, "Did not parse SessionStopTime");

        System.Diagnostics.Debug.Assert(sessionDescription.TimeDescriptions.First().RepeatTimes.Count == 2, "First TimeDescription must have 2 RepeatTime entries.");

        //Todo RepeatTimes should be an Object with the properties  (RepeatInterval, ActiveDuration, Offsets[start / stop])
        //r=<repeat interval> <active duration> <offsets from start-time>

        System.Diagnostics.Debug.Assert(sessionDescription.TimeDescriptions.First().RepeatTimes[0] == "604800 3600 0 90000", "Did not parse RepeatTimes");

        System.Diagnostics.Debug.Assert(sessionDescription.TimeDescriptions.First().RepeatTimes[1] == "7d 1h 0 25h", "Did not parse RepeatTimes");

        System.Diagnostics.Debug.Assert(sessionDescription.Length == sessionDescription.ToString().Length, "Did not calculate length correctly");

        System.Diagnostics.Debug.Assert(string.Compare(sessionDescription.ToString(), testVector) < 0, "Did not output exactly same string");
    }

    public void TestInitialObjectDescriptor()
    {
        Console.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);

        string testVector = @"v=0
o=- 1183588701 6 IN IP4 10.3.1.221
s=Elecard NWRenderer
i=Elecard streaming
u=http://www.elecard.com
e=tsup@elecard.net.ru
c=IN IP4 239.255.0.1/64
b=CT:0
a=ISMA-compliance:2,2.0,2
a=mpeg4-iod: ""data:application/mpeg4-iod;base64,AoE8AA8BHgEBAQOBDAABQG5kYXRhOmFwcGxpY2F0aW9uL21wZWc0LW9kLWF1O2Jhc2U2NCxBVGdCR3dVZkF4Y0F5U1FBWlFRTklCRUFGM0FBQVBvQUFBRERVQVlCQkE9PQEbAp8DFQBlBQQNQBUAB9AAAD6AAAA+gAYBAwQNAQUAAMgAAAAAAAAAAAYJAQAAAAAAAAAAA2EAAkA+ZGF0YTphcHBsaWNhdGlvbi9tcGVnNC1iaWZzLWF1O2Jhc2U2NCx3QkFTZ1RBcUJYSmhCSWhRUlFVL0FBPT0EEgINAAAUAAAAAAAAAAAFAwAAQAYJAQAAAAAAAAAA""
m=video 10202 RTP/AVP 98
a=rtpmap:98 H264/90000
a=control:trackID=1
a=fmtp:98 packetization-mode=1; profile-level-id=4D001E; sprop-parameter-sets=Z00AHp5SAWh7IA==,aOuPIAAA
a=mpeg4-esid:201
m=audio 10302 RTP/AVP 96
a=rtpmap:96 mpeg4-generic/48000/2
a=control:trackID=2
a=fmtp:96 streamtype=5; profile-level-id=255; mode=AAC-hbr; config=11900000000000000000; objectType=64; sizeLength=13; indexLength=3; indexDeltaLength=3
a=mpeg4-esid:101";

        Media.Sdp.SessionDescription sd = new Media.Sdp.SessionDescription(testVector);

        Console.WriteLine(sd.ToString());

        //Get the inital object descriptor line
        Media.Sdp.SessionDescriptionLine mpeg4IodLine = sd.Lines.Where(l => l.Type == 'a' && l.Parts.Any(p => p.Contains("mpeg4-iod"))).FirstOrDefault();

        System.Diagnostics.Debug.Assert(mpeg4IodLine != null, "Cannot find InitialObjectDescriptor Line");

        System.Diagnostics.Debug.Assert(mpeg4IodLine.Parts.Last() == "base64,AoE8AA8BHgEBAQOBDAABQG5kYXRhOmFwcGxpY2F0aW9uL21wZWc0LW9kLWF1O2Jhc2U2NCxBVGdCR3dVZkF4Y0F5U1FBWlFRTklCRUFGM0FBQVBvQUFBRERVQVlCQkE9PQEbAp8DFQBlBQQNQBUAB9AAAD6AAAA+gAYBAwQNAQUAAMgAAAAAAAAAAAYJAQAAAAAAAAAAA2EAAkA+ZGF0YTphcHBsaWNhdGlvbi9tcGVnNC1iaWZzLWF1O2Jhc2U2NCx3QkFTZ1RBcUJYSmhCSWhRUlFVL0FBPT0EEgINAAAUAAAAAAAAAAAFAwAAQAYJAQAAAAAAAAAA\"", "InitialObjectDescriptor Line Contents invalid.");
    }

}
