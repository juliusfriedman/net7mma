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

    //public class SessionDescriptionLineTests {

    /// <summary>
    /// Test SessionDescriptionLine
    /// </summary>
    public void TestSessionDescriptionLine()
    {
        Media.Sdp.SessionDescriptionLine line = new Media.Sdp.SessionDescriptionLine('x', string.Empty)
        {
            "a", "b", "c"
        };

        //Verify the Type
        System.Diagnostics.Debug.Assert(line.Type == 'x', "Did not expected type");

        //Verify the seperator
        System.Diagnostics.Debug.Assert(line.m_Seperator == Media.Sdp.SessionDescription.SpaceString, "Did not have expected seperator");

        System.Diagnostics.Debug.Assert(line.m_Parts.Count == 3, "Did not have correct Part Count");

        //Create a exact copy but in a new instance
        Media.Sdp.SessionDescriptionLine linf = new Media.Sdp.SessionDescriptionLine('x', string.Empty)
        {
            "a", "b", "c"
        };

        //Should be equal
        System.Diagnostics.Debug.Assert(line == linf, "Did not find an equal line.");

        //Same line with different case in the values.

        linf = new Media.Sdp.SessionDescriptionLine('x', string.Empty)
        {
            "a", "B", "C"
        };

        //Should be equal for now.
        System.Diagnostics.Debug.Assert(line == linf, "Did not find an equal line.");

        //Test a not equal scenario
        linf = new Media.Sdp.SessionDescriptionLine('x', string.Empty)
        {
            "1", "2", "3"
        };

        System.Diagnostics.Debug.Assert(line != linf, "Found an equal line.");
    }

    public void TestSessionBandwidthLine()
    {
        string testVector = "b=RS:0";

        Media.Sdp.SessionDescriptionLine line = new Media.Sdp.SessionDescriptionLine(testVector);

        Media.Sdp.Lines.SessionBandwidthLine Line = new Media.Sdp.Lines.SessionBandwidthLine(line);

        //Media.Sdp.Lines.SessionBandwidthLine TBLineFromString = new Media.Sdp.Lines.SessionBandwidthLine(testVector);

        System.Diagnostics.Debug.Assert(line.ToString() == Line.ToString(), "Not String Equal");

        System.Diagnostics.Debug.Assert(line == Line, "Not Type Equal");

        System.Diagnostics.Debug.Assert(line.m_Parts.Count == Line.m_Parts.Count, "Parts Count Not Equal");
    }

    public void TestSessionConnectionLine()
    {
        string testVector = "c=IN IP4 10.0.0.4";

        Media.Sdp.SessionDescriptionLine line = new Media.Sdp.SessionDescriptionLine(testVector);

        Media.Sdp.Lines.SessionConnectionLine Line = new Media.Sdp.Lines.SessionConnectionLine(line);

        System.Diagnostics.Debug.Assert(line.ToString() == Line.ToString(), "Not String Equal");

        //Seperator different
        System.Diagnostics.Debug.Assert(line != Line, "Not Type Equal");

        System.Diagnostics.Debug.Assert(line.m_Parts.Count != Line.m_Parts.Count, "Parts Count Not Equal");
    }

    public void TestSessionOriginLine()
    {
        string testVector = "o=jdoe 2890844526 2890842807 IN IP4 www.com";

        Media.Sdp.SessionDescriptionLine line = new Media.Sdp.SessionDescriptionLine(testVector);

        Media.Sdp.Lines.SessionOriginLine Line = new Media.Sdp.Lines.SessionOriginLine(line);

        System.Diagnostics.Debug.Assert(Line.SessionId == "2890844526", "Unexpected SessionId");

        System.Diagnostics.Debug.Assert(Line.SessionVersion == 2890842807, "Unexpected SessionVersion");

        System.Diagnostics.Debug.Assert(line.ToString() == Line.ToString(), "Not String Equal");

        //Seperator different
        System.Diagnostics.Debug.Assert(line != Line, "Not Type Equal");

        System.Diagnostics.Debug.Assert(line.m_Parts.Count != Line.m_Parts.Count, "Parts Count Not Equal");
    }    

    //}


    public void TestTimeZoneLine()
    {
        string testVector = "z=2882844526 -1h 2898848070 0 0 -3600 0";

        Media.Sdp.Lines.SessionTimeZoneLine line = new Media.Sdp.Lines.SessionTimeZoneLine(new Media.Sdp.SessionDescriptionLine(testVector));

        Media.Sdp.Lines.SessionTimeZoneLine Line = new Media.Sdp.Lines.SessionTimeZoneLine(line);

        //7 Total parts
        //0 - 2882844526 -1h //Start
        //1 - 2898848070 0   //End
        //2 - 0 -3600        //Start
        //3 - 0 (0)          //End ,Missing the offset

        System.Diagnostics.Debug.Assert(line.AdjustmentTimesCount == 4, "Unexpected AdjustmentTimesCount");

        System.Diagnostics.Debug.Assert(Line.AdjustmentTimesCount == 4, "Unexpected AdjustmentTimesCount");

        //Test the AdjustmentValues property

        double[] values = line.AdjustmentValues.ToArray();

        System.Diagnostics.Debug.Assert(values[0] == 2882844526, "Unexpected value (Start Adjustment Time)");

        System.Diagnostics.Debug.Assert(values[1] == -3600, "Unexpected value (Start Adjustment Offset)");

        System.Diagnostics.Debug.Assert(values[2] == 2898848070, "Unexpected value (End Adjustment Time)");

        System.Diagnostics.Debug.Assert(values[3] == 0, "Unexpected value (End Adjustment Offset)");

        System.Diagnostics.Debug.Assert(values[4] == 0, "Unexpected value (Start Adjustment Time)");

        System.Diagnostics.Debug.Assert(values[5] == -3600, "Unexpected value (End Adjustment Offset)");

        System.Diagnostics.Debug.Assert(values[6] == 0, "Unexpected value (Start Adjustment Time)");
        
        //Loop the AdjustmentTimes
        for (int i = 0, e = line.AdjustmentTimesCount; i < e; ++i)
        {
            DateTime startDate = line.GetAdjustmentDate(i);

            TimeSpan startOffset = line.GetAdjustmentOffset(i);

            System.Diagnostics.Debug.Assert(startOffset.Hours == -1, "Unexpected AdjustmentOffset");

            System.Diagnostics.Debug.Assert(startDate == Line.GetAdjustmentDate(i), "Unexpected AdjustmentDate");

            System.Diagnostics.Debug.Assert(startOffset == Line.GetAdjustmentOffset(i), "Unexpected AdjustmentOffset");

            //Advance to the End Offsets
            if(++i > e) break;

            DateTime endDate = line.GetAdjustmentDate(i);

            TimeSpan endOffset = line.GetAdjustmentOffset(i);

            System.Diagnostics.Debug.Assert(endOffset.Hours == 0, "Unexpected AdjustmentOffset");

            System.Diagnostics.Debug.Assert(endDate == Line.GetAdjustmentDate(i), "Unexpected AdjustmentDate");

            System.Diagnostics.Debug.Assert(endOffset == Line.GetAdjustmentOffset(i), "Unexpected AdjustmentOffset");
        }

        //Use the setter to set all the values
        line.AdjustmentValues = values;

        Line.AdjustmentValues = line.AdjustmentValues;

        System.Diagnostics.Debug.Assert(line.AdjustmentValues.SequenceEqual(values) && Line.AdjustmentValues.SequenceEqual(values), "Unexpected AdjustmentValues");
    }

    public void TestTimeDescriptionType()
    {
        /*
         
         5.10.  Repeat Times ("r=")

      r=<repeat interval> <active duration> <offsets from start-time>

   "r=" fields specify repeat times for a session.  For example, if a
   session is active at 10am on Monday and 11am on Tuesday for one hour
   each week for three months, then the <start-time> in the
   corresponding "t=" field would be the NTP representation of 10am on
   the first Monday, the <repeat interval> would be 1 week, the <active
   duration> would be 1 hour, and the offsets would be zero and 25
   hours.  The corresponding "t=" field stop time would be the NTP
   representation of the end of the last session three months later.  By
   default, all fields are in seconds, so the "r=" and "t=" fields might
   be the following:

      t=3034423619 3042462419
      r=604800 3600 0 90000

   To make description more compact, times may also be given in units of
   days, hours, or minutes.  The syntax for these is a number
   immediately followed by a single case-sensitive character.
   Fractional units are not allowed -- a smaller unit should be used
   instead.  The following unit specification characters are allowed:

      d - days (86400 seconds)
      h - hours (3600 seconds)
      m - minutes (60 seconds)
      s - seconds (allowed for completeness)

   Thus, the above session announcement could also have been written:

      r=7d 1h 0 25h

   Monthly and yearly repeats cannot be directly specified with a single
   SDP repeat time; instead, separate "t=" fields should be used to
   explicitly list the session times.

         t=0
        r=604800 3600 0 90000
        r=7d 1h 0 25h
         
         */

        string testVector = @"t=3034423619 3042462419
r=604800 3600 0 90000
r=7d 1h 0 25h";

        //t=now 0 should be Permanent and Unbounded?

        string[] vector = testVector.Split(Media.Sdp.SessionDescription.NewLine, Media.Sdp.SessionDescription.LineFeed);

        int index = 0;

        Media.Sdp.TimeDescription line = new Media.Sdp.TimeDescription(vector, ref index);

        System.Diagnostics.Debug.Assert(line.StartTime == 3034423619, "Expected StartTime not found");

        System.Diagnostics.Debug.Assert(line.StopTime == 3042462419, "Expected StopTime not found");

        System.Diagnostics.Debug.Assert(line.RepeatLines.Count == 2, "Expected RepeatTimes Count not found");

        TimeSpan expected = new TimeSpan(8, 2, 0, 0);

        foreach (var repeatTime in line.RepeatLines)
        {
            TimeSpan found = repeatTime.RepeatTimeSpan;

            System.Diagnostics.Debug.Assert(found == expected, "Found unexpected repeatTime");
        }

        double[] expectedParts = new double[] { 604800, 3600, 0, 90000 };

        System.Diagnostics.Debug.Assert(line.RepeatLines.All(r => r.RepeatTimeSpan == expected && r.RepeatTimeValue == expected.TotalSeconds && r.RepeatValues.SequenceEqual(expectedParts)), "Found unexpected RepeatLine.RepeatValues");

        //3 months later 31 days per month
        System.Diagnostics.Debug.Assert((line.NtpStopDateTime - line.NtpStartDateTime).Days == 93, "Expected Days not found");

        //Verify repeat times.

        Media.Sdp.TimeDescription Line = new Media.Sdp.TimeDescription(line.NtpStartDateTime, line.NtpStopDateTime);

        System.Diagnostics.Debug.Assert(line.NtpStartDateTime == Line.NtpStartDateTime, "Expected NtpStartDateTime not found");

        System.Diagnostics.Debug.Assert(line.NtpStopDateTime == Line.NtpStopDateTime, "Expected NtpStopDateTime not found");

        Line.NtpStartDateTime = line.NtpStartDateTime;

        System.Diagnostics.Debug.Assert(line.NtpStartDateTime == Line.NtpStartDateTime, "Expected NtpStartDateTime not found");

        Line.NtpStopDateTime = line.NtpStopDateTime;

        System.Diagnostics.Debug.Assert(line.NtpStopDateTime == Line.NtpStopDateTime, "Expected NtpStopDateTime not found");

        //Needs repeat times.
        //System.Diagnostics.Debug.Assert(line == Line, "Not Type Equal");

        //System.Diagnostics.Debug.Assert(line.ToString() == Line.ToString(), "Not String Equal");

        DateTime now = DateTime.UtcNow, then = now.AddSeconds(60);

        //Can't use the timestamp directly in the line value without conversion to unix time.
        //To test pass the DateTime to the TimeDescription constructor and allow it to convert to unix time as necessary
        testVector = "t=" + Media.Ntp.NetworkTimeProtocol.DateTimeToNptTimestamp(now) + " " + Media.Ntp.NetworkTimeProtocol.DateTimeToNptTimestamp(then);
        vector = testVector.Split(Media.Sdp.SessionDescription.NewLine, Media.Sdp.SessionDescription.LineFeed);

        index = 0;

        line = new Media.Sdp.TimeDescription(vector, ref index);

        try
        {
            //Otherwise this overflows because the line.SessionStartTime is out of range for DateTime type when converting.
            System.Diagnostics.Debug.Assert((int)(line.NtpStopDateTime - line.NtpStartDateTime).TotalSeconds == 60, "Expected TotalSeconds not found");
        }
        catch
        {
            //Expected

            //Check them to be in Ntp Format..
            System.Diagnostics.Debug.Assert(line.StartTimeToken == Media.Ntp.NetworkTimeProtocol.DateTimeToNptTimestamp(now).ToString(), "Expected SessionStartTime not found");
            
            System.Diagnostics.Debug.Assert(line.StopTimeToken == Media.Ntp.NetworkTimeProtocol.DateTimeToNptTimestamp(then).ToString(), "Expected SessionStopTime not found");
        }
    }

    public void TestMediaDescriptionType()
    {
        ///Todo, seperate tests.
    }

    public void TestTryParseRange()
    {
        //https://www.ietf.org/mail-archive/web/mmusic/current/msg01854.html
        //This attribute is defined in ABNF [14] as:
        //a-range-def = "a" "=" "range" ":" ranges-specifier CRLF 
        //a=range:ranges-specifierCRLF

        //https://www.ietf.org/rfc/rfc2326.txt @  Page 16

        /* SMPTE Relative Timestamps
           smpte-range  =   smpte-type "=" smpte-time "-" [ smpte-time ]
           smpte-type   =   "smpte" | "smpte-30-drop" | "smpte-25"; other timecodes may be added
           smpte-time   =   1*2DIGIT ":" 1*2DIGIT ":" 1*2DIGIT [ ":" 1*2DIGIT ] [ "." 1*2DIGIT ]
        
         Examples:
             smpte=10:12:33:20-
             smpte=10:07:33-
             smpte=10:07:00-10:07:33:05.01
             smpte-25=10:07:00-10:07:33:05.01
         */

        //Should also test ToString.

        Tuple<string, TimeSpan, TimeSpan>[] testVectors = new[] 
        { 
            new Tuple<string, TimeSpan, TimeSpan>("smpte=10:12:33:20-", TimeSpan.Parse("10:12:33:20"), Media.Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan),
            new Tuple<string, TimeSpan, TimeSpan>("smpte=10:07:33-", TimeSpan.Parse("10:07:33"), Media.Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan),
            new Tuple<string, TimeSpan, TimeSpan>("smpte=10:07:00-10:07:33:05.01", TimeSpan.Parse("10:07:00"), TimeSpan.Parse("10:07:33:05.01")),
            new Tuple<string, TimeSpan, TimeSpan>("smpte-25=10:07:00-10:07:33:05.01", TimeSpan.Parse("10:07:00"), TimeSpan.Parse("10:07:33:05.01")),
        };

        foreach (var test in testVectors)
        {
            string type;

            System.TimeSpan start, end;

            if (false == Media.Sdp.SessionDescription.TryParseRange(test.Item1, out type, out start, out end)) throw new System.Exception("TryParseRange");

            if (false == type.StartsWith("smpte", StringComparison.OrdinalIgnoreCase)) throw new System.Exception("TryParseRange -> Type");

            if (start != test.Item2) throw new System.Exception("TryParseRange -> Start");

            if (end != test.Item3) throw new System.Exception("TryParseRange -> End");
        }

        /* Normal Play Time
        npt-range    =   ( npt-time "-" [ npt-time ] ) | ( "-" npt-time )
        npt-time     =   "now" | npt-sec | npt-hhmmss
        npt-sec      =   1*DIGIT [ "." *DIGIT ]
        npt-hhmmss   =   npt-hh ":" npt-mm ":" npt-ss [ "." *DIGIT ]
        npt-hh       =   1*DIGIT     ; any positive number
        npt-mm       =   1*2DIGIT    ; 0-59
        npt-ss       =   1*2DIGIT    ; 0-59

        Examples:
            npt=123.45-125
            npt=12:05:35.3-
            npt=now-
         
         */

        testVectors = new[] 
        { 
            new Tuple<string, TimeSpan, TimeSpan>("npt=123.45-125", TimeSpan.FromSeconds(123.45), TimeSpan.FromSeconds(125)),
            new Tuple<string, TimeSpan, TimeSpan>("npt=12:05:35.3-", TimeSpan.Parse("12:05:35.3"), Media.Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan),
            new Tuple<string, TimeSpan, TimeSpan>("npt=now-", TimeSpan.Zero, Media.Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan),
            new Tuple<string, TimeSpan, TimeSpan>("npt=now- now", TimeSpan.Zero, TimeSpan.Zero),
            new Tuple<string, TimeSpan, TimeSpan>("npt= 0 - 1", TimeSpan.Zero, Media.Common.Extensions.TimeSpan.TimeSpanExtensions.OneSecond),
            new Tuple<string, TimeSpan, TimeSpan>("npt=0.-", TimeSpan.Zero, Media.Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan),
            new Tuple<string, TimeSpan, TimeSpan>("npt=0.0-", TimeSpan.Zero, Media.Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan),
            new Tuple<string, TimeSpan, TimeSpan>("npt=0.00-", TimeSpan.Zero, Media.Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan),
            new Tuple<string, TimeSpan, TimeSpan>(" npt=0.000-", TimeSpan.Zero, Media.Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan),
            new Tuple<string, TimeSpan, TimeSpan>("npt=0.0000-", TimeSpan.Zero, Media.Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan),
            new Tuple<string, TimeSpan, TimeSpan>("npt=0.00000-", TimeSpan.Zero, Media.Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan),
            new Tuple<string, TimeSpan, TimeSpan>("npt=00.000-", TimeSpan.Zero, Media.Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan),
            new Tuple<string, TimeSpan, TimeSpan>("npt=000.000-", TimeSpan.Zero, Media.Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan),
            new Tuple<string, TimeSpan, TimeSpan>(" npt= 0.0 -", TimeSpan.Zero, Media.Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan),
            new Tuple<string, TimeSpan, TimeSpan>(" npt=0.00000-", TimeSpan.Zero, Media.Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan),
            new Tuple<string, TimeSpan, TimeSpan>(" range:npt=0.00000-", TimeSpan.Zero, Media.Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan),
            new Tuple<string, TimeSpan, TimeSpan>(" range: npt=0.00000-", TimeSpan.Zero, Media.Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan),
            new Tuple<string, TimeSpan, TimeSpan>("range : npt=0.00000-", TimeSpan.Zero, Media.Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan),
            new Tuple<string, TimeSpan, TimeSpan>(" range : npt = 1 - 1", Media.Common.Extensions.TimeSpan.TimeSpanExtensions.OneSecond, Media.Common.Extensions.TimeSpan.TimeSpanExtensions.OneSecond),
            new Tuple<string, TimeSpan, TimeSpan>(" : range : range : npt = 1 - 1", Media.Common.Extensions.TimeSpan.TimeSpanExtensions.OneSecond, Media.Common.Extensions.TimeSpan.TimeSpanExtensions.OneSecond),
        };

        foreach (var test in testVectors)
        {
            string type;

            System.TimeSpan start, end;

            if (false == Media.Sdp.SessionDescription.TryParseRange(test.Item1, out type, out start, out end)) throw new System.Exception("TryParseRange");

            if (type != "npt") throw new System.Exception("TryParseRange -> Type");

            if (start != test.Item2) throw new System.Exception("TryParseRange -> Start");

            if (end != test.Item3) throw new System.Exception("TryParseRange -> End");
        }

        /* Absolute Time
         utc-range    =   "clock" "=" utc-time "-" [ utc-time ]
         utc-time     =   utc-date "T" utc-time "Z"
         utc-date     =   8DIGIT                    ; < YYYYMMDD >
         utc-time     =   6DIGIT [ "." fraction ]   ; < HHMMSS.fraction >

        Example for November 8, 1996 at 14h37 and 20 and a quarter seconds UTC:

        19961108T143720.25Z
         */

        testVectors = new[] 
        { 
            new Tuple<string, TimeSpan, TimeSpan>("clock=19961108T143720.25", DateTime.UtcNow - DateTime.SpecifyKind(new DateTime(1996, 11, 8, 14, 37, 20, 250), DateTimeKind.Utc), Media.Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan),
        };

        foreach (var test in testVectors)
        {
            string type;

            System.TimeSpan start, end;

            if (false == Media.Sdp.SessionDescription.TryParseRange(test.Item1, out type, out start, out end)) throw new System.Exception("TryParseRange");

            if (type != "clock") throw new System.Exception("TryParseRange -> Type");

            if (start != test.Item2) throw new System.Exception("TryParseRange -> Start");

            if (end != test.Item3) throw new System.Exception("TryParseRange -> End");
        }
    }

    public void TestAttributeLine()
    {
        string testVector = "a=range : npt = 1 - 1\r\n";

        Media.Sdp.SessionDescriptionLine line = new Media.Sdp.SessionDescriptionLine(testVector);

        if (string.Compare(line.ToString(), testVector) != 0) throw new System.Exception("ToString");

        line = Media.Sdp.SessionDescriptionLine.Parse(testVector);

        if (string.Compare(line.ToString(), testVector) != 0) throw new System.Exception("ToString");

        Media.Sdp.Lines.SessionAttributeLine attributeLine = new Media.Sdp.Lines.SessionAttributeLine(line);

        if (string.Compare(attributeLine.ToString(), testVector) != 0) throw new System.Exception("ToString");

        if (string.Compare(attributeLine.AttributeName, "range") != 0) throw new System.Exception("AttributeName");

        if (string.Compare(attributeLine.AttributeValue, "npt = 1 - 1") != 0) throw new System.Exception("AttributeValue");

        testVector = "a=fmtp:97 packetization-mode=1;profile-level-id=42C01E;sprop-parameter-sets=Z0LAHtkDxWhAAAADAEAAAAwDxYuS,aMuMsg==\r\n";

        line = new Media.Sdp.SessionDescriptionLine(testVector);

        if (string.Compare(line.ToString(), testVector) != 0) throw new System.Exception("ToString");

        line = Media.Sdp.SessionDescriptionLine.Parse(testVector);

        if (string.Compare(line.ToString(), testVector) != 0) throw new System.Exception("ToString");

        attributeLine = new Media.Sdp.Lines.SessionAttributeLine(line);

        if (string.Compare(attributeLine.ToString(), testVector) != 0) throw new System.Exception("ToString");

        if (string.Compare(attributeLine.AttributeName, "fmtp") != 0) throw new System.Exception("AttributeName");

        if (string.Compare(attributeLine.AttributeValue, "97 packetization-mode=1;profile-level-id=42C01E;sprop-parameter-sets=Z0LAHtkDxWhAAAADAEAAAAwDxYuS,aMuMsg==") != 0) throw new System.Exception("AttributeValue");

        testVector = "a=control:trackId=1\r\n";

        line = new Media.Sdp.SessionDescriptionLine(testVector);

        if (string.Compare(line.ToString(), testVector) != 0) throw new System.Exception("ToString");

        line = Media.Sdp.SessionDescriptionLine.Parse(testVector);

        if (string.Compare(line.ToString(), testVector) != 0) throw new System.Exception("ToString");

        attributeLine = new Media.Sdp.Lines.SessionAttributeLine(line);

        if (string.Compare(attributeLine.ToString(), testVector) != 0) throw new System.Exception("ToString");

        if (string.Compare(attributeLine.AttributeName, "control") != 0) throw new System.Exception("AttributeName");

        if (string.Compare(attributeLine.AttributeValue, "trackId=1") != 0) throw new System.Exception("AttributeName");
    }


    public void TestFormatType()
    {
        Media.Sdp.Lines.FormatTypeLine fmtp = fmtp = new Media.Sdp.Lines.FormatTypeLine(" ");

        if (string.Compare(fmtp.ToString(), "a=fmtp:  \r\n") != 0)
            throw new System.Exception("ToString");

        fmtp = new Media.Sdp.Lines.FormatTypeLine(string.Empty);

        if (string.Compare(fmtp.ToString(), "a=fmtp: \r\n") != 0)
            throw new System.Exception("ToString");

        fmtp = new Media.Sdp.Lines.FormatTypeLine("-", "packetization-mode=1;profile-level-id=42C01E;sprop-parameter-sets=Z0LAHtkDxWhAAAADAEAAAAwDxYuS,aMuMsg==");

        if (string.Compare(fmtp.ToString(), "a=fmtp:- packetization-mode=1;profile-level-id=42C01E;sprop-parameter-sets=Z0LAHtkDxWhAAAADAEAAAAwDxYuS,aMuMsg==\r\n") != 0)
            throw new System.Exception("ToString");

        if (fmtp.FormatSpecificParametersCount != 3) throw new System.Exception("FormatSpecificParametersCount");

        if (fmtp.FormatSpecificParameterToken != "packetization-mode=1;profile-level-id=42C01E;sprop-parameter-sets=Z0LAHtkDxWhAAAADAEAAAAwDxYuS,aMuMsg==") throw new System.Exception("FormatSpecificParameterToken");

        if (false == fmtp.HasFormatSpecificParameters) throw new System.Exception("HasFormatSpecificParameters is false");

        fmtp = new Media.Sdp.Lines.FormatTypeLine(97, "packetization-mode=1;profile-level-id=42C01E;sprop-parameter-sets=Z0LAHtkDxWhAAAADAEAAAAwDxYuS,aMuMsg==");

        if (string.Compare(fmtp.ToString(), "a=fmtp:97 packetization-mode=1;profile-level-id=42C01E;sprop-parameter-sets=Z0LAHtkDxWhAAAADAEAAAAwDxYuS,aMuMsg==\r\n") != 0)
            throw new System.Exception("ToString");

        if (fmtp.FormatToken != "97") throw new System.Exception("FormatToken");

        if (fmtp.FormatValue != 97) throw new System.Exception("FormatValue");

        if (fmtp.FormatSpecificParametersCount != 3) throw new System.Exception("FormatSpecificParametersCount");

        if (fmtp.FormatSpecificParameterToken != "packetization-mode=1;profile-level-id=42C01E;sprop-parameter-sets=Z0LAHtkDxWhAAAADAEAAAAwDxYuS,aMuMsg==") throw new System.Exception("FormatSpecificParameterToken");

        if (false == fmtp.HasFormatSpecificParameters) throw new System.Exception("HasFormatSpecificParameters is false");

        //We will extract the sps and pps from that line.
        byte[] sps = null, pps = null;

        //If there was a fmtp line then iterate the parts contained.
        foreach (string p in fmtp.FormatSpecificParameters)
        {
            //Determine where in the string the desired token in.
            string token = Media.Common.Extensions.String.StringExtensions.Substring(p, "sprop-parameter-sets=");

            //If present extract it.
            if (false == string.IsNullOrWhiteSpace(token))
            {
                //Get the strings which corresponds to the data without the datum split by ','
                string[] data = token.Split(',');

                //If there is any data then assign it

                if (data.Length > 0) sps = System.Convert.FromBase64String(data[0]);

                if (data.Length > 1) pps = System.Convert.FromBase64String(data[1]);

                //Done
                break;
            }
        }

        //Prepend the SPS if it was found
        if (Media.Common.Extensions.Array.ArrayExtensions.IsNullOrEmpty(sps)) throw new System.Exception("SequenceParameterSet not found");

        //Prepend the PPS if it was found.
        if (Media.Common.Extensions.Array.ArrayExtensions.IsNullOrEmpty(pps)) throw new System.Exception("PicutureParameterSet not found");
    }

    ///// <summary>
    ///// Test the constructor
    ///// </summary>
    //public void ATestSessionDescriptionConstructor()
    //{

    //    Console.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);

    //    System.Diagnostics.Debug.Assert(false == string.IsNullOrEmpty(Media.Sdp.SessionDescription.NewLineString), "Media.Sdp.SessionDescription.NewLine Must not be Null or Empty.");

    //    //Get the characters which make the NewLine string.
    //    char[] newLineCharacters = Media.Sdp.SessionDescription.NewLineString.ToArray();

    //    //Check for two characters
    //    System.Diagnostics.Debug.Assert(2 == newLineCharacters.Length, "Media.Sdp.SessionDescription.NewLine Must Have 2 Characters");

    //    //Check for '\r'
    //    System.Diagnostics.Debug.Assert(Media.Sdp.SessionDescription.NewLine == newLineCharacters[0], "Media.Sdp.SessionDescription.NewLine[0] Must Equal '\r'");

    //    //Check for '\n'
    //    System.Diagnostics.Debug.Assert(Media.Sdp.SessionDescription.LineFeed == newLineCharacters[1], "Media.Sdp.SessionDescription.NewLine[0] Must Equal '\n'");
    //}

    public void ParseMediaDescriptionUnitTest()
    {
        string testVector = @" m=audio 49230 RTP/AVP 96 97 98
            a=rtpmap:96 L8/8000
            a=rtpmap:97 L16/8000
            a=rtpmap:98 L16/11025/2";

        //c=IN IP4 224.2.1.1/127/2
         //m=video 49170/2 RTP/AVP 31

        using (var md = new Media.Sdp.MediaDescription(testVector))
        {
            System.Diagnostics.Debug.Assert(md.Lines.Count() == 4, "MediaDescription must have 4 lines");//Including itself

            //CLR not assert correctly with == ....
            //md.MediaDescriptionLine.ToString() == "m=audio 49230 RTP/AVP 96 97 98"

            System.Diagnostics.Debug.Assert(md.MediaType == Media.Sdp.MediaType.audio, "Unexpected MediaType");

            System.Diagnostics.Debug.Assert(md.MediaPort == 49230, "Unexpected MediaPort");

            System.Diagnostics.Debug.Assert(md.MediaProtocol == "RTP/AVP", "Unexpected MediaProtocol");

            System.Diagnostics.Debug.Assert(md.PayloadTypes.Count() == 3, "Could not read the Payload List");

            System.Diagnostics.Debug.Assert(md.PayloadTypes.First() == 96, "Could not read the Payload List");

            System.Diagnostics.Debug.Assert(md.PayloadTypes.ToArray()[1] == 97, "Could not read the Payload List");

            System.Diagnostics.Debug.Assert(md.PayloadTypes.Last() == 98, "Could not read the Payload List");

            System.Diagnostics.Debug.Assert(string.Compare(md.MediaDescriptionLine.ToString(), "m=audio 49230 RTP/AVP 96 97 98\r\n") == 0, "Did not handle Payload List Correct");

            System.Diagnostics.Debug.Assert(md.AttributeLines.Count() == 3, "Unexpected number of AttributeLines");

            System.Diagnostics.Debug.Assert(md.m_Lines.Count == 3, "Unexpected m_Lines.Count");

            Media.Sdp.Lines.SessionMediaDescriptionLine line = new Media.Sdp.Lines.SessionMediaDescriptionLine("m=audio 49230 RTP/AVP 96 97 98\r\n");

            System.Diagnostics.Debug.Assert(line.PayloadTypes.Count() == md.PayloadTypes.Count(), "PayloadTypes Count doesn't match");

            System.Diagnostics.Debug.Assert(line.PayloadTypes.SequenceEqual(md.PayloadTypes), "Could not read the Payload List");

            System.Diagnostics.Debug.Assert(line.MediaType == Media.Sdp.MediaType.audio, "Unexpected MediaType");

            System.Diagnostics.Debug.Assert(line.MediaPort == 49230, "Unexpected MediaPort");

            System.Diagnostics.Debug.Assert(line.MediaProtocol == "RTP/AVP", "Unexpected MediaProtocol");

            System.Diagnostics.Debug.Assert(false == line.HasMultiplePorts, "Unexpected HasPortRange");

            //System.Diagnostics.Debug.Assert(line.PortRange == 0, "Unexpected PortRange");

            System.Diagnostics.Debug.Assert(string.Compare(md.MediaDescriptionLine.ToString(), line.ToString()) == 0, "Not string equal");

            System.Diagnostics.Debug.Assert(md.MediaDescriptionLine == line, "Not Type Equals");
            
        }
    }

    public void ParseSDPUnitTest()
    {
        Console.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);

        string sdpStr =
            "v=0" + Media.Sdp.SessionDescription.NewLineString +
            "o=root 3285 3285 IN IP4 10.0.0.4" + Media.Sdp.SessionDescription.NewLineString +
            "s=session" + Media.Sdp.SessionDescription.NewLineString +
            "c=IN IP4 10.0.0.4" + Media.Sdp.SessionDescription.NewLineString +
            "t=0 0" + Media.Sdp.SessionDescription.NewLineString +
            "m=audio 12228 RTP/AVP 0 101" + Media.Sdp.SessionDescription.NewLineString +
            "a=rtpmap:0 PCMU/8000" + Media.Sdp.SessionDescription.NewLineString +
            "a=rtpmap:101 telephone-event/8000" + Media.Sdp.SessionDescription.NewLineString +
            "a=fmtp:101 0-16" + Media.Sdp.SessionDescription.NewLineString +
            "a=silenceSupp:off - - - -" + Media.Sdp.SessionDescription.NewLineString +
            "a=ptime:20" + Media.Sdp.SessionDescription.NewLineString +
            "a=sendrecv";

        Media.Sdp.SessionDescription sdp = new Media.Sdp.SessionDescription(sdpStr);

        System.Diagnostics.Debug.WriteLine(sdp.ToString());

        System.Diagnostics.Debug.Assert("10.0.0.4" == sdp.ConnectionLine.m_Parts[2], "The connection address was not parsed  correctly.");  // ToDo: Be better if "Part[3]" was referred to by ConnectionAddress.
        System.Diagnostics.Debug.Assert(Media.Sdp.MediaType.audio == sdp.MediaDescriptions.First().MediaType, "The media type not parsed correctly.");
        System.Diagnostics.Debug.Assert(12228 == sdp.MediaDescriptions.First().MediaPort, "The connection port was not parsed correctly.");
        System.Diagnostics.Debug.Assert(0 == sdp.MediaDescriptions.First().PayloadTypes.First(), "The first media format was incorrect.");         // ToDo: Can't cope with multiple media formats?
        //Assert.IsTrue(sdp.Media[0].MediaFormats[0].FormatID == 0, "The highest priority media format ID was incorrect.");
        //Assert.IsTrue(sdp.Media[0].MediaFormats[0].Name == "PCMU", "The highest priority media format name was incorrect.");
        //Assert.IsTrue(sdp.Media[0].MediaFormats[0].ClockRate == 8000, "The highest priority media format clockrate was incorrect.");
        System.Diagnostics.Debug.Assert("a=rtpmap:0 PCMU/8000\r\n" == sdp.MediaDescriptions.First().RtpMapLine.ToString(), "The rtpmap line for the PCM format was not parsed correctly.");  // ToDo "Parts" should be put into named properties where possible.  
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

    public void ParseMulticastSDPUnitTest()
    {
        string testVector = @"v=0
o=- 1459753417444873 1 IN IP4 10.0.57.24
s=Session streamed by ""testMPEG4VideoStreamer""
i=test.m4e
t=0 0
a=tool:LIVE555 Streaming Media v2016.04.01
a=type:broadcast
a=control:*
a=source-filter: incl IN IP4 * 10.0.57.24
a=rtcp-unicast: reflection
a=range:npt=0-
a=x-qt-text-nam:Session streamed by ""testMPEG4VideoStreamer""
a=x-qt-text-inf:test.m4e
m=video 18888 RTP/AVP 96
c=IN IP4 232.248.50.1/255
b=AS:500
a=rtpmap:96 MP4V-ES/90000
a=fmtp:96 profile-level-id=3;config=000001B003000001B509000001000000012000C8888007D05040F14103
a=control:track1
";

        using (Media.Sdp.SessionDescription sdp = new Media.Sdp.SessionDescription(testVector))
        {
            //This string should be equal exactly, but because the class corrects of the lines the order it's not exactly the same.
            //T= is before the media description in the example..
            //System.Diagnostics.Debug.Assert(string.Compare(sdp.ToString(), testVector) == 0, "Unexpected sdp.ToString");

            foreach (Media.Sdp.MediaDescription md in sdp.MediaDescriptions)
            {
                Media.Sdp.Lines.SessionConnectionLine cLine = new Media.Sdp.Lines.SessionConnectionLine(md.ConnectionLine);

                System.Diagnostics.Debug.Assert(string.Compare(cLine.ConnectionNetworkType, Media.Sdp.Lines.SessionConnectionLine.InConnectionToken) == 0, "Unexpected ConnectionNetworkType");

                System.Diagnostics.Debug.Assert(string.Compare(cLine.ConnectionAddressType, Media.Sdp.Lines.SessionConnectionLine.IP4) == 0, "Unexpected ConnectionAddressType");

                System.Diagnostics.Debug.Assert(string.Compare(cLine.ConnectionAddress, "232.248.50.1/255") == 0, "Unexpected ConnectionAddress");

                System.Diagnostics.Debug.Assert(string.Compare(cLine.Host, "232.248.50.1") == 0, "Unexpected Host");

                System.Diagnostics.Debug.Assert(Media.Common.Extensions.IPAddress.IPAddressExtensions.IsMulticast(System.Net.IPAddress.Parse(cLine.Host)), "Must be a IsMulticast");

                System.Diagnostics.Debug.Assert(cLine.HasMultipleAddresses == false, "Unexpected HasMultipleAddresses");

                System.Diagnostics.Debug.Assert(cLine.HasTimeToLive, "Unexpected HasTimeToLive");

                System.Diagnostics.Debug.Assert(cLine.TimeToLive == 255, "Unexpected TimeToLive value");

                System.Diagnostics.Debug.Assert(cLine.ConnectionParts.First() == "232.248.50.1", "Unexpected ConnectionParts");

                System.Diagnostics.Debug.Assert(cLine.ConnectionParts.Last() == "255", "Unexpected ConnectionParts");

                System.Diagnostics.Debug.Assert(cLine.ToString() == "c=IN IP4 232.248.50.1/255\r\n", "Unexpected cLine ToString");
            }

        }

    }

    public void ParseMulticastConnectionLineWithPortRangeAndTtl()
    {
        string testVector = "c=IN IP4 232.248.50.1/255/2";

        Media.Sdp.Lines.SessionConnectionLine cLine = new Media.Sdp.Lines.SessionConnectionLine(testVector);

        System.Diagnostics.Debug.Assert(string.Compare(cLine.ConnectionNetworkType, Media.Sdp.Lines.SessionConnectionLine.InConnectionToken) == 0, "Unexpected ConnectionNetworkType");

        System.Diagnostics.Debug.Assert(string.Compare(cLine.ConnectionAddressType, Media.Sdp.Lines.SessionConnectionLine.IP4) == 0, "Unexpected ConnectionAddressType");

        //Todo
        //IPAddress name of property is wrong. (ConnectionAddress)
        System.Diagnostics.Debug.Assert(cLine.Host == "232.248.50.1", "Unexpected Host");

        System.Diagnostics.Debug.Assert(Media.Common.Extensions.IPAddress.IPAddressExtensions.IsMulticast(System.Net.IPAddress.Parse(cLine.Host)), "Must be a IsMulticast");

        System.Diagnostics.Debug.Assert(cLine.ConnectionAddress == "232.248.50.1/255/2", "Unexpected ConnectionAddress");

        //Must be parsed from ConnectionParts

        System.Diagnostics.Debug.Assert(cLine.HasTimeToLive, "Unexpected HasTimeToLive");

        System.Diagnostics.Debug.Assert(cLine.TimeToLive == 255, "Unexpected TimeToLive value");

        System.Diagnostics.Debug.Assert(cLine.HasMultipleAddresses, "Unexpected HasMultipleAddresses");

        System.Diagnostics.Debug.Assert(cLine.NumberOfAddresses == 2, "Unexpected NumberOfAddresses value");

        System.Diagnostics.Debug.Assert(cLine.ConnectionParts.First() == "232.248.50.1", "Unexpected ConnectionParts");

        System.Diagnostics.Debug.Assert(cLine.ConnectionParts.Last() == "2", "Unexpected ConnectionParts");

        System.Diagnostics.Debug.Assert(cLine.ToString() == "c=IN IP4 232.248.50.1/255/2\r\n", "Unexpected cLine ToString");

    }

    public void CreateSessionDescriptionUnitTest()
    {
        Console.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);

        //Should have o=?
        string originatorAndSession = String.Format("{0} {1} {2} {3} {4} {5}", "-", "62464", "0", "IN", "IP4", "10.1.1.2");

        string profile = "RTP/AVP";

        Media.Sdp.MediaType mediaType = Media.Sdp.MediaType.audio;

        int mediaPort = 15000;

        int mediaFormat = 0;

        string sessionName = "MySessionName";

        //The document will have a DocumentVersion of 0 by default.
        //Version will be set (v=) which will try to update the version but there is no originator.
        //Originator is set which causes an update in version but because there was no originator the version is 0
        //Once the originator is set (which maintains the DocumentVersion)
        //It can then be increased, the constructor does not do this for you.
        using (var audioDescription = new Media.Sdp.SessionDescription(mediaFormat, originatorAndSession, sessionName))
        {
            //Ensure the correct SessionDescriptionVersion was set
            System.Diagnostics.Debug.Assert(audioDescription.SessionDescriptionVersion == 0, "Did not find Correct SessionDescriptionVersion");

            //When created the version of the `o=` line should be 1.
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

    public void CreateSessionDescriptionModifySessionVersionUnitTest()
    {
        Console.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);

        using (Media.Sdp.SessionDescription sdp = new Media.Sdp.SessionDescription(0, "v√ƒ", "Bandit"))
        {

            //update version was specified false so the verison of the document should have updated.
            System.Diagnostics.Debug.Assert(sdp.DocumentVersion == 0, "Did not find Correct SessionVersion");

            //Add a connection line, updating the version 
            sdp.Add(new Media.Sdp.Lines.SessionConnectionLine()
            {
                ConnectionNetworkType = "IN",
                ConnectionAddressType = "*",
                ConnectionAddress = "0.0.0.0"
            });

            System.Diagnostics.Debug.Assert(sdp.Lines.Count() == 4, "Did not have correct amount of Lines");

            long sessionVersion = 9223372036802072014;

            //update version was specified false so the verison of the document should have updated.
            System.Diagnostics.Debug.Assert(sdp.DocumentVersion == 1, "Did not find Correct SessionVersion");

            sdp.DocumentVersion = sessionVersion;

            string expected = "v=0\r\no=v√ƒ  9223372036802072014   \r\ns=Bandit\r\nc=IN * 0.0.0.0\r\n";

            System.Diagnostics.Debug.Assert(string.Compare(sdp.ToString(), expected) == 0, "Did not output correct result.");

            //Try to get a token to update the document
            var token = sdp.BeginUpdate();

            //Do another update to test modification doesn't freeze?
            ++sdp.DocumentVersion;

            //End the update
            sdp.EndUpdate(token, true);

            //update version was specified false so the verison of the document should have updated.
            System.Diagnostics.Debug.Assert(sdp.DocumentVersion == sessionVersion + 1, "Did not find Correct SessionVersion");

            //Do another update
            ++sdp.DocumentVersion;

            System.Diagnostics.Debug.Assert(sdp.DocumentVersion == sessionVersion + 2, "Did not find Correct SessionVersion");
        }
    }

    public void ParseBriaSDPUnitTest()
    {
        Console.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);

        string sdpStr = "v=0\r\no=- 5 2 IN IP4 10.1.1.2\r\ns=CounterPath Bria\r\nc=IN IP4 144.137.16.240\r\nt=0 0\r\nm=audio 34640 RTP/AVP 0 8 101\r\na=sendrecv\r\na=rtpmap:101 telephone-event/8000\r\na=fmtp:101 0-15\r\na=alt:1 1 : STu/ZtOu 7hiLQmUp 10.1.1.2 34640\r\n";

        Media.Sdp.SessionDescription sdp = new Media.Sdp.SessionDescription(sdpStr);

        System.Diagnostics.Debug.WriteLine(sdp.ToString());

        System.Diagnostics.Debug.Assert("144.137.16.240" == sdp.ConnectionLine.m_Parts[2], "The connection address was not parsed correctly.");
        System.Diagnostics.Debug.Assert(34640 == sdp.MediaDescriptions.First().MediaPort, "The connection port was not parsed correctly.");
        System.Diagnostics.Debug.Assert(0 == sdp.MediaDescriptions.First().PayloadTypes.First(), "The highest priority media format ID was incorrect.");
    }

    public void ParseICESessionAttributesUnitTest()
    {
        Console.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);

        string sdpStr =
          "v=0" + Media.Sdp.SessionDescription.NewLineString +
          "o=jdoe 2890844526 2890842807 IN IP4 10.0.1.1" + Media.Sdp.SessionDescription.NewLineString +
          "s=" + Media.Sdp.SessionDescription.NewLineString +
          "c=IN IP4 192.0.2.3" + Media.Sdp.SessionDescription.NewLineString +
          "t=0 0" + Media.Sdp.SessionDescription.NewLineString +
          "a=ice-pwd:asd88fgpdd777uzjYhagZg" + Media.Sdp.SessionDescription.NewLineString +
          "a=ice-ufrag:8hhY" + Media.Sdp.SessionDescription.NewLineString +
          "m=audio 45664 RTP/AVP 0" + Media.Sdp.SessionDescription.NewLineString +
          "b=RS:0" + Media.Sdp.SessionDescription.NewLineString +
          "b=RR:0" + Media.Sdp.SessionDescription.NewLineString +
          "a=rtpmap:0 PCMU/8000" + Media.Sdp.SessionDescription.NewLineString +
          "a=candidate:1 1 UDP 2130706431 10.0.1.1 8998 typ host" + Media.Sdp.SessionDescription.NewLineString +
          "a=candidate:2 1 UDP 1694498815 192.0.2.3 45664 typ srflx raddr 10.0.1.1 rport 8998";

        Media.Sdp.SessionDescription sdp = new Media.Sdp.SessionDescription(sdpStr);

        System.Diagnostics.Debug.WriteLine(sdp.ToString());

        //ToDo: Add ICE attributes. (https://tools.ietf.org/html/rfc5245#page-26, https://tools.ietf.org/html/rfc6336)
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

        string sdpStr = "v=0" + Media.Sdp.SessionDescription.NewLineString +
            "o=- 13064410510996677 3 IN IP4 10.1.1.2" + Media.Sdp.SessionDescription.NewLineString +
            "s=Bria 4 release 4.1.1 stamp 74246" + Media.Sdp.SessionDescription.NewLineString +
            "c=IN IP4 10.1.1.2" + Media.Sdp.SessionDescription.NewLineString +
            "b=AS:2064" + Media.Sdp.SessionDescription.NewLineString +
            "t=0 0" + Media.Sdp.SessionDescription.NewLineString +
            "m=audio 49290 RTP/AVP 0" + Media.Sdp.SessionDescription.NewLineString +
            "a=sendrecv" + Media.Sdp.SessionDescription.NewLineString +
            "m=video 56674 RTP/AVP 96" + Media.Sdp.SessionDescription.NewLineString +
            "b=TIAS:2000000" + Media.Sdp.SessionDescription.NewLineString +
            "a=rtpmap:96 VP8/90000" + Media.Sdp.SessionDescription.NewLineString +
            "a=sendrecv" + Media.Sdp.SessionDescription.NewLineString +
            "a=rtcp-fb:* nack pli";

        Media.Sdp.SessionDescription sdp = new Media.Sdp.SessionDescription(sdpStr);

        System.Diagnostics.Debug.WriteLine(sdp.ToString());

        System.Diagnostics.Debug.Assert(2 == sdp.MediaDescriptions.Count());
        System.Diagnostics.Debug.Assert(49290 == sdp.MediaDescriptions.Where(x => x.MediaType == Media.Sdp.MediaType.audio).First().MediaPort);
        System.Diagnostics.Debug.Assert(56674 == sdp.MediaDescriptions.Where(x => x.MediaType == Media.Sdp.MediaType.video).First().MediaPort);
    }

    public void ParseMultiplePayloadUnitTest()
    {
        Console.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);

        string sdpStr = "v=0" + Media.Sdp.SessionDescription.NewLineString +
            "o=root 1220363117 1220363117 IN IP4 192.168.1.109" + Media.Sdp.SessionDescription.NewLineString +
            "s=Asterisk PBX GIT-master-60a15fe" + Media.Sdp.SessionDescription.NewLineString +
            "c=IN IP4 192.168.1.109" + Media.Sdp.SessionDescription.NewLineString +
            "b=CT:384" + Media.Sdp.SessionDescription.NewLineString +
            "t=0 0" + Media.Sdp.SessionDescription.NewLineString +
            "m=audio 18544 RTP/AVP 0 8" + Media.Sdp.SessionDescription.NewLineString +
            "a=rtpmap:0 PCMU/8000" + Media.Sdp.SessionDescription.NewLineString +
            "a=rtpmap:8 PCMA/8000" + Media.Sdp.SessionDescription.NewLineString +
            "a=maxptime:150" + Media.Sdp.SessionDescription.NewLineString +
            "a=sendrecv" + Media.Sdp.SessionDescription.NewLineString +
            "m=video 24314 RTP/AVP 99 34" + Media.Sdp.SessionDescription.NewLineString +
            "a=rtpmap:99 H264/90000" + Media.Sdp.SessionDescription.NewLineString +
            "a=fmtp:99 sprop-parameter-sets=Z0KADJWgUH5A,aM4Ecg==" + Media.Sdp.SessionDescription.NewLineString +
            "a=rtpmap:34 H263/90000" + Media.Sdp.SessionDescription.NewLineString +
            "a=sendrecv" + Media.Sdp.SessionDescription.NewLineString;

        System.Diagnostics.Debug.WriteLine(sdpStr);

        using (Media.Sdp.SessionDescription sdp = new Media.Sdp.SessionDescription(sdpStr))
        {
            System.Diagnostics.Debug.WriteLine(sdp.ToString());

            System.Diagnostics.Debug.Assert(0 == string.Compare(sdpStr, sdp.ToString()));

            System.Diagnostics.Debug.Assert(2 == sdp.MediaDescriptions.Count());

            System.Diagnostics.Debug.Assert(18544 == sdp.MediaDescriptions.Where(x => x.MediaType == Media.Sdp.MediaType.audio).First().MediaPort);

            System.Diagnostics.Debug.Assert(24314 == sdp.MediaDescriptions.Where(x => x.MediaType == Media.Sdp.MediaType.video).First().MediaPort);

            System.Diagnostics.Debug.Assert(2 == sdp.MediaDescriptions.Where(x => x.MediaType == Media.Sdp.MediaType.video).First().PayloadTypes.Count());

            System.Diagnostics.Debug.Assert(2 == sdp.MediaDescriptions.Where(x => x.MediaType == Media.Sdp.MediaType.audio).First().PayloadTypes.Count());

            System.Diagnostics.Debug.Assert(99 == sdp.MediaDescriptions.Where(x => x.MediaType == Media.Sdp.MediaType.video).First().PayloadTypes.First());

            System.Diagnostics.Debug.Assert(34 == sdp.MediaDescriptions.Where(x => x.MediaType == Media.Sdp.MediaType.video).First().PayloadTypes.Last());

            System.Diagnostics.Debug.Assert(0 == sdp.MediaDescriptions.Where(x => x.MediaType == Media.Sdp.MediaType.audio).First().PayloadTypes.First());

            System.Diagnostics.Debug.Assert(8 == sdp.MediaDescriptions.Where(x => x.MediaType == Media.Sdp.MediaType.audio).First().PayloadTypes.Last());

            Media.Sdp.Lines.SessionMediaDescriptionLine line = new Media.Sdp.Lines.SessionMediaDescriptionLine("m=audio 18544 RTP/AVP 0 8");

            System.Diagnostics.Debug.Assert(line.MediaType == Media.Sdp.MediaType.audio);

            System.Diagnostics.Debug.Assert((line.MediaType = Media.Sdp.MediaType.audio) == Media.Sdp.MediaType.audio);

            System.Diagnostics.Debug.Assert(18544 == line.MediaPort);

            System.Diagnostics.Debug.Assert((line.MediaPort = 18544) == line.MediaPort);

            System.Diagnostics.Debug.Assert(line.PayloadTypes.Count() == 2);

            System.Diagnostics.Debug.Assert(line.PayloadTypes.First() == 0);

            System.Diagnostics.Debug.Assert(line.PayloadTypes.Last() == 8);

            System.Diagnostics.Debug.Assert((line.PayloadTypes = new int[] { 0, 8 }).SequenceEqual(new int[] { 0, 8 }));

            System.Diagnostics.Debug.Assert(line.ToString() == "m=audio 18544 RTP/AVP 0 8" + Environment.NewLine);

            line = new Media.Sdp.Lines.SessionMediaDescriptionLine("m=video 24314 RTP/AVP 99 34");

            System.Diagnostics.Debug.Assert(line.MediaType == Media.Sdp.MediaType.video);

            System.Diagnostics.Debug.Assert((line.MediaType = Media.Sdp.MediaType.video) == Media.Sdp.MediaType.video);           

            System.Diagnostics.Debug.Assert(24314 == line.MediaPort);

            System.Diagnostics.Debug.Assert((line.MediaPort = 24314) == line.MediaPort);

            System.Diagnostics.Debug.Assert(line.PayloadTypes.Count() == 2);

            System.Diagnostics.Debug.Assert(line.PayloadTypes.First() == 99);

            System.Diagnostics.Debug.Assert(line.PayloadTypes.Last() == 34);

            System.Diagnostics.Debug.Assert((line.PayloadTypes = new int[] { 99, 34 }).SequenceEqual(new int[] { 99, 34 }));

            System.Diagnostics.Debug.Assert(line.ToString() == "m=video 24314 RTP/AVP 99 34" + Environment.NewLine);

        }
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

        System.Diagnostics.Debug.Assert(sessionDescription.TimeDescriptions.First().StartTime == 0, "Did not parse SessionStartTime");

        System.Diagnostics.Debug.Assert(sessionDescription.TimeDescriptions.First().StopTime == 0, "Did not parse SessionStopTime");

        System.Diagnostics.Debug.Assert(sessionDescription.TimeDescriptions.First().RepeatLines.Count == 2, "First TimeDescription must have 2 RepeatTime entries.");

        //Todo RepeatTimes should be an Object with the properties  (RepeatInterval, ActiveDuration, Offsets[start / stop])
        //r=<repeat interval> <active duration> <offsets from start-time>

        System.Diagnostics.Debug.Assert(sessionDescription.TimeDescriptions.First().RepeatLines[0].ToString() == "r=604800 3600 0 90000\r\n", "Did not parse RepeatTimes");

        System.Diagnostics.Debug.Assert(sessionDescription.TimeDescriptions.First().RepeatLines[1].ToString() == "r=7d 1h 0 25h\r\n", "Did not parse RepeatTimes");

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

        System.Diagnostics.Debug.Assert(mpeg4IodLine.Parts.Last() == "\"data:application/mpeg4-iod;base64,AoE8AA8BHgEBAQOBDAABQG5kYXRhOmFwcGxpY2F0aW9uL21wZWc0LW9kLWF1O2Jhc2U2NCxBVGdCR3dVZkF4Y0F5U1FBWlFRTklCRUFGM0FBQVBvQUFBRERVQVlCQkE9PQEbAp8DFQBlBQQNQBUAB9AAAD6AAAA+gAYBAwQNAQUAAMgAAAAAAAAAAAYJAQAAAAAAAAAAA2EAAkA+ZGF0YTphcHBsaWNhdGlvbi9tcGVnNC1iaWZzLWF1O2Jhc2U2NCx3QkFTZ1RBcUJYSmhCSWhRUlFVL0FBPT0EEgINAAAUAAAAAAAAAAAFAwAAQAYJAQAAAAAAAAAA\"", "InitialObjectDescriptor Line Contents invalid.");
    }

    public void IssueSessionDescriptionWithMediaDescriptionWithPortRange()
    {
        Console.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);

        string testVector = @"v=0
o=- 3 8 IN IP4 10.16.1.22
s=stream1
i=H264 session of stream1
u=http://10.16.1.22
c=IN IP4 239.1.1.22/64/1
t=0 0
m=video 5006/2 RTP/AVP 102
i=Video stream
c=IN IP4 239.1.1.22/64/1
a=fmtp:102 width=1280;height=720;depth=0;framerate=30000;fieldrate=30000;
a=framerate:30
a=rtpmap:102 H264/90000";

        using (Media.Sdp.SessionDescription sd = new Media.Sdp.SessionDescription(testVector))
        {
            Console.WriteLine(sd.ToString());

            //Verify the line count
            System.Diagnostics.Debug.Assert(sd.Lines.Count() == 13, "Did not find all lines");

            //Check for the MediaDescription
            System.Diagnostics.Debug.Assert(sd.MediaDescriptions.Count() == 1, "Cannot find MediaDescription");

            var md = sd.MediaDescriptions.First();

            System.Diagnostics.Debug.Assert(md != null, "Cannot find MediaDescription");

            //Count the line in the media description (including itself)
            System.Diagnostics.Debug.Assert(md.Lines.Count() == 6, "Cannot find corrent amount of lines in MediaDescription");

            var fmtp = md.FmtpLine;

            System.Diagnostics.Debug.Assert(fmtp != null, "Cannot find FmtpLine in MediaDescription");

            var rtpMap = md.RtpMapLine;

            System.Diagnostics.Debug.Assert(rtpMap != null, "Cannot find RtpMapLine in MediaDescription");

            //Verify and set the port range.
            System.Diagnostics.Debug.Assert(md.HasMultiplePorts, "HasMultiplePorts");

            System.Diagnostics.Debug.Assert(md.NumberOfPorts == 2, "NumberOfPorts");

            //Remove the port range.
            //When set to any value <= 1 the '/' is removed.

            md.NumberOfPorts = 1;

            System.Diagnostics.Debug.Assert(false == md.HasMultiplePorts, "HasMultiplePorts");

            System.Diagnostics.Debug.Assert(md.NumberOfPorts == 1, "Did not find the correct NumberOfPorts");

            string expectedMediaDescription = "m=video 5006 RTP/AVP 102\r\ni=Video stream\r\nc=IN IP4 239.1.1.22/64/1\r\na=fmtp:102 width=1280;height=720;depth=0;framerate=30000;fieldrate=30000;\r\na=framerate:30\r\na=rtpmap:102 H264/90000\r\n";

            string actualMediaDescription = md.ToString();

            //Check the result of the comparsion
            System.Diagnostics.Debug.Assert(string.Compare(expectedMediaDescription, actualMediaDescription) == 0, "Did not output expected result");

            md.NumberOfPorts = 0;

            System.Diagnostics.Debug.Assert(false == md.HasMultiplePorts, "HasMultiplePorts");

            System.Diagnostics.Debug.Assert(md.NumberOfPorts == 1, "Did not find the correct NumberOfPorts");

            actualMediaDescription = md.ToString();

            //Check the result of the comparsion
            System.Diagnostics.Debug.Assert(string.Compare(expectedMediaDescription, actualMediaDescription) == 0, "Did not output expected result");

        }

    }

    public void TestSessionDescriptionSpecifyingFeedback()
    {
        string testVector = @"v=0
o=- 1 1 IN IP4 127.0.0.1
s=Test
a=type:broadcast
t=0 0
c=IN IP4 0.0.0.0
m=video 0 RTP/AVP 96
a=rtpmap:96 H264/90000
a=fmtp:96 packetization-mode=1;profile-level-id=640028;sprop-parameter-sets=Z2QAKKy0BQHv+A0CAAAcIAACvyHsQPoAALQN3//x2IH0AAFoG7//4UA=,aM48bJCRjhwfHDgkEwlzioJgqFA1wx+cVBMFQoGuGPyCoYGjBx5gh+hEICRA48w79CIQEiBx5h38;
a=control:track0
a=rtcp-fb:96 nack";

        using (Media.Sdp.SessionDescription sd = new Media.Sdp.SessionDescription(testVector))
        {
            Console.WriteLine(sd.ToString());

            //Verify the line count
            System.Diagnostics.Debug.Assert(sd.Lines.Count() == 11, "Did not find all lines");

            //Check for the MediaDescription
            System.Diagnostics.Debug.Assert(sd.MediaDescriptions.Count() == 1, "Cannot find MediaDescription");

            var md = sd.MediaDescriptions.First();

            System.Diagnostics.Debug.Assert(md != null, "Cannot find MediaDescription");

            //Count the line in the media description (including itself)
            System.Diagnostics.Debug.Assert(md.Lines.Count() == 5, "Cannot find corrent amount of lines in MediaDescription");

            var fmtp = md.FmtpLine;

            System.Diagnostics.Debug.Assert(fmtp != null, "Cannot find FmtpLine in MediaDescription");

            string expected = "a=fmtp:96 packetization-mode=1;profile-level-id=640028;sprop-parameter-sets=Z2QAKKy0BQHv+A0CAAAcIAACvyHsQPoAALQN3//x2IH0AAFoG7//4UA=,aM48bJCRjhwfHDgkEwlzioJgqFA1wx+cVBMFQoGuGPyCoYGjBx5gh+hEICRA48w79CIQEiBx5h38;\r\n";

            System.Diagnostics.Debug.Assert(string.Compare(fmtp.ToString(), expected, StringComparison.InvariantCultureIgnoreCase) == 0, "Did not output correct FmtpLine line");

            var rtpMap = md.RtpMapLine;

            System.Diagnostics.Debug.Assert(rtpMap != null, "Cannot find RtpMapLine in MediaDescription");

            expected = "a=rtcp-fb:96 nack\r\n";

            System.Diagnostics.Debug.Assert(string.Compare(sd.AttributeLines.Last().ToString(), expected, StringComparison.InvariantCultureIgnoreCase) == 0, "Did not output correct feedback line");

            System.Diagnostics.Debug.Assert(sd.AttributeLines.Last() == md.AttributeLines.Last(), "Both last attribute lines should be equal to each other");
        }

    }    

}
