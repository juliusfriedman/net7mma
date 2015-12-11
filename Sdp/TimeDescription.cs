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

using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace Media.Sdp
{
    #region TimeDescription

    /// <summary>
    /// Represents a TimeDescription with optional Repeat times.
    /// Parses and Creates.
    /// </summary>
    public class TimeDescription : Common.BaseDisposable, IEnumerable<SessionDescriptionLine>
    {

        public const char TimeDescriptionType = 't', RepeatTimeType = 'r';

        /* https://tools.ietf.org/html/rfc4566#page-17
         The first and second sub-fields give the start and stop times,
           respectively, for the session.  These values are the decimal
           representation of Network Time Protocol (NTP) time values in seconds
           since 1900 [13].  To convert these values to UNIX time, subtract
           decimal 2208988800.

         */

        //Should be DateTime?

        public double SessionStartTime { get; private set; }

        public double SessionStopTime { get; private set; }

        //public bool IsLive { get { return SessionStartTime == 0 } }

        //public bool IsContinious { get { return SessionStopTime <= 0; } }

        //public bool IsPermanent { get { return SessionStopTime == SessionStartTime == 0; } }

        //Also note that there must be no RepeatTimes for the above to be true.

        //Ntp Timestamps from above, NOTE they do not wrap in 2036
        //public DateTime Start, Stop;

        internal protected SessionDescriptionLine TimeDescriptionLine
        {
            get
            {
                return new SessionDescriptionLine(TimeDescriptionType, ((char)Common.ASCII.Space).ToString()){
                    SessionStartTime.ToString(),
                    SessionStopTime.ToString()
                };
            }
        }

        public List<string> RepeatTimes { get; private set; }

        /// <summary>
        /// Calculates the length in bytes of this TimeDescription.
        /// </summary>
        public int Length
        {
            get
            {

                //(t=)X()Y(\r\n)                      //(r=)X(\r\n)
                return 5 + (SessionStartTime.ToString().Length + SessionStopTime.ToString().Length) + RepeatTimes.Sum(p => p.Length + 4);
            }
        }

        public TimeDescription()
        {
            RepeatTimes = new List<string>();
        }

        public TimeDescription(int startTime, int stopTime)
            : this()
        {
            SessionStartTime = startTime;
            SessionStopTime = stopTime;
        }

        public TimeDescription(string[] sdpLines, ref int index)
            : this()
        {
            string sdpLine = sdpLines[index++].Trim();

            if (string.IsNullOrWhiteSpace(sdpLine) || sdpLine[0] != TimeDescriptionType) Media.Common.Extensions.Exception.ExceptionExtensions.RaiseTaggedException(this, "Invalid Time Description");

            //Char 1 must always be '='...

            sdpLine = SessionDescription.TrimLineValue(sdpLine.Substring(2));

            //https://net7mma.codeplex.com/workitem/17032

            //The OP advised he was recieving a SDP with "now" ... not sure why this is not standard.

            //Additionally he might have been talking about the Range header in which case "now" is handled propertly.

            //TODO Use constants...


            /*
                         5.9.  Timing ("t=")

                  t=<start-time> <stop-time>

               The "t=" lines specify the start and stop times for a session.
               Multiple "t=" lines MAY be used if a session is active at multiple
               irregularly spaced times; each additional "t=" line specifies an
               additional period of time for which the session will be active.  If
               the session is active at regular times, an "r=" line (see below)
               should be used in addition to, and following, a "t=" line -- in which
               case the "t=" line specifies the start and stop times of the repeat
               sequence.

               The first and second sub-fields give the start and stop times,
               respectively, for the session.  These values are the decimal
               representation of Network Time Protocol (NTP) time values in seconds
               since 1900 [13].  To convert these values to UNIX time, subtract
               decimal 2208988800.

               NTP timestamps are elsewhere represented by 64-bit values, which wrap
               sometime in the year 2036.  Since SDP uses an arbitrary length
               decimal representation, this should not cause an issue (SDP
               timestamps MUST continue counting seconds since 1900, NTP will use
               the value modulo the 64-bit limit).

               If the <stop-time> is set to zero, then the session is not bounded,
               though it will not become active until after the <start-time>.  If
               the <start-time> is also zero, the session is regarded as permanent.

               User interfaces SHOULD strongly discourage the creation of unbounded
               and permanent sessions as they give no information about when the
               session is actually going to terminate, and so make scheduling
               difficult.

               The general assumption may be made, when displaying unbounded
               sessions that have not timed out to the user, that an unbounded
               session will only be active until half an hour from the current time
               or the session start time, whichever is the later.  If behaviour
               other than this is required, an end-time SHOULD be given and modified
               as appropriate when new information becomes available about when the
               session should really end.

               Permanent sessions may be shown to the user as never being active
               unless there are associated repeat times that state precisely when
               the session will be active.
             */

            string[] parts = sdpLine.Split(SessionDescription.Space);

            int partsLength = parts.Length;

            if (partsLength > 0)
            {
                double time;

                if (parts[0] != "now")
                {
                    if (double.TryParse(SessionDescription.TrimLineValue(parts[0]), out time)) SessionStartTime = time;
                }

                if (partsLength > 1)
                {
                    if (double.TryParse(SessionDescription.TrimLineValue(parts[1]), out time)) SessionStopTime = time;
                }
            }

            //Iterate remaining lines
            for (; index < sdpLines.Length; ++index)
            {
                //Scope a line
                sdpLine = sdpLines[index];

                //If we are not extracing repeat times then there is no more TimeDescription to parse
                if (sdpLine[0] != RepeatTimeType) break;

                //Parse and add the repeat time
                try
                {
                    //r=<repeat interval> <active duration> <offsets from start-time>
                    RepeatTimes.Add(SessionDescription.TrimLineValue(sdpLine.Substring(2)));
                }
                catch (Exception ex)
                {
                    Media.Common.Extensions.Exception.ExceptionExtensions.RaiseTaggedException(this, "Invalid Repeat Time", ex);
                }
            }

        }


        public TimeDescription(TimeDescription other, bool referenceRepeatTimes = false)
        {
            SessionStartTime = other.SessionStartTime;

            SessionStopTime = other.SessionStopTime;

            if (referenceRepeatTimes) RepeatTimes = other.RepeatTimes;
            else RepeatTimes.AddRange(other.RepeatTimes);
        }

        public string ToString(SessionDescription sdp = null)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(TimeDescriptionLine.ToString());
            foreach (string repeatTime in RepeatTimes)
                builder.Append(RepeatTimeType.ToString() + Sdp.SessionDescription.EqualsSign.ToString() + repeatTime + SessionDescription.NewLine);
            return builder.ToString();
        }

        public override string ToString()
        {
            return ToString(null);
        }

        public IEnumerator<SessionDescriptionLine> GetEnumerator()
        {
            yield return TimeDescriptionLine;

            foreach (string repeatTime in RepeatTimes)
            {
                yield return new SessionDescriptionLine(RepeatTimeType) { repeatTime };
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<SessionDescriptionLine>)this).GetEnumerator();
        }
    }

    public static class TimeDescriptionExtensions
    {

    }

    #endregion
}
