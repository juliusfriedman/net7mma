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
        /* Reference
         https://tools.ietf.org/html/rfc4566#page-17
         The first and second sub-fields give the start and stop times,
           respectively, for the session.  These values are the decimal
           representation of Network Time Protocol (NTP) time values in seconds
           since 1900 [13].  To convert these values to UNIX time, subtract
           decimal 2208988800.
         * 
          time =                POS-DIGIT 9*DIGIT
                         ; Decimal representation of NTP time in
                         ; seconds since 1900.  The representation
                         ; of NTP time is an unbounded length field
                         ; containing at least 10 digits.  Unlike the
                         ; 64-bit representation used elsewhere, time
                         ; in SDP does not wrap in the year 2036.
         
         */       

        #region Statics

        public const char TimeDescriptionType = 't', RepeatTimeType = 'r';

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the start time.
        /// If seto to 0 then the session is not bounded,  though it will not become active until after the <see cref="StartTime"/>.  
        /// </summary>
        /// <remarks>These values are the decimal representation of Network Time Protocol (NTP) time values in seconds since 1900 </remarks>
        public double StartTime { get; private set; }

        /// <summary>
        /// Gets or sets the stop time.
        /// If set to 0 and the <see cref="StartTime"/> is also zero, the session is regarded as permanent.
        /// </summary>
        /// <remarks>These values are the decimal representation of Network Time Protocol (NTP) time values in seconds since 1900 </remarks>
        public double StopTime { get; private set; }

        /// <summary>
        /// Gets the DateTime representation of <see cref="StarTime"/>
        /// Throws an ArgumentOutOfRangeException if SessionStartTime was out of range.
        /// </summary>
        public DateTime NtpStartDateTime
        {
            get
            {
                return Media.Ntp.NetworkTimeProtocol.UtcEpoch1900.AddSeconds(StartTime);// - Media.Ntp.NetworkTimeProtocol.NtpDifferenceUnix);
            }

            set
            {
                //Convert to SDP timestamp
                StartTime = (value.ToUniversalTime().Ticks - Ntp.NetworkTimeProtocol.UtcEpoch1900.Ticks) / TimeSpan.TicksPerSecond;

                //Ensure Ntp Difference
                //StartTime += Ntp.NetworkTimeProtocol.NtpDifferenceUnix;
            }
        }

        /// <summary>
        /// Gets the DateTime representation of <see cref="StopTime"/>
        /// Throws an ArgumentOutOfRangeException if SessionStopTime was out of range.
        /// </summary>
        public DateTime NtpStopDateTime
        {
            get
            {
                return Media.Ntp.NetworkTimeProtocol.UtcEpoch1900.AddSeconds(StopTime);// - Media.Ntp.NetworkTimeProtocol.NtpDifferenceUnix);
            }

            set
            {
                //Convert to SDP timestamp
                StopTime = (value.ToUniversalTime().Ticks - Ntp.NetworkTimeProtocol.UtcEpoch1900.Ticks) / TimeSpan.TicksPerSecond;

                //Ensure Ntp Difference
                //StopTime += Ntp.NetworkTimeProtocol.NtpDifferenceUnix;
            }
        }

        /// <summary>
        /// If the <see cref="StopTime"/> is set to zero, then the session is not bounded,  though it will not become active until after the <see cref="StartTime"/>.  
        /// If the <see cref="StartTime"/> is also zero, the session is regarded as permanent.
        /// </summary>
        public bool IsPermanent { get { return StartTime == 0 && StopTime == 0; } }

        internal protected SessionDescriptionLine TimeDescriptionLine
        {
            get
            {
                return new SessionDescriptionLine(TimeDescriptionType, SessionDescription.SpaceString){
                    ((ulong)StartTime).ToString(),
                    ((ulong)StopTime).ToString()
                };
            }
        }

        public IEnumerable<Lines.SessionRepeatTimeLine> RepeatLines
        {
            get
            {
                foreach (string repeatTime in RepeatTimes)
                {
                    yield return new Lines.SessionRepeatTimeLine(repeatTime);
                }
            }
        }

        /// <summary>
        /// Gets or sets any repeat descriptions assoicated with the TimeDescription.
        /// </summary>
        public List<string> RepeatTimes { get; private set; }

        /// <summary>
        /// Indicates if there are any repeat times.
        /// </summary>
        public bool HasRepeatTimes { get { return RepeatTimes.Count > 0; } }

        /// <summary>
        /// Calculates the length in bytes of this TimeDescription.
        /// </summary>
        public int Length
        {
            get
            {

                //(t=)X()Y(\r\n)                      //(r=)X(\r\n)
                return 5 + (StartTime.ToString().Length + StopTime.ToString().Length) + RepeatTimes.Sum(p => p.Length + 4);
            }
        }

        #endregion

        #region Constructor

        public TimeDescription()
        {
            RepeatTimes = new List<string>();
        }

        public TimeDescription(int startTime, int stopTime)
            : this()
        {
            StartTime = startTime;
            StopTime = stopTime;
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
                    if (double.TryParse(SessionDescription.TrimLineValue(parts[0]), out time)) StartTime = time;
                }

                if (partsLength > 1)
                {
                    if (double.TryParse(SessionDescription.TrimLineValue(parts[1]), out time)) StopTime = time;
                }
            }

            //Iterate remaining lines
            for (; index < sdpLines.Length; ++index)
            {
                //Scope a line
                sdpLine = sdpLines[index];

                if (string.IsNullOrWhiteSpace(sdpLine)) continue;

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
            StartTime = other.StartTime;

            StopTime = other.StopTime;

            if (referenceRepeatTimes) RepeatTimes = other.RepeatTimes;
            else RepeatTimes.AddRange(other.RepeatTimes);
        }

        public TimeDescription(long sessionStart, long sessionStop)
            : this()
        {
            StartTime = sessionStart;

            StopTime = sessionStop;
        }

        public TimeDescription(DateTime sessionStartUtc, DateTime sessionStopUtc)
            : this()
        {
            NtpStartDateTime = sessionStartUtc;

            NtpStopDateTime = sessionStopUtc;
        }

        #endregion

        public string ToString(SessionDescription sdp = null)
        {
            StringBuilder builder = new StringBuilder();
            
            builder.Append(TimeDescriptionLine.ToString());
            
            foreach (string repeatTime in RepeatTimes)
            {
                builder.Append(RepeatTimeType);
                builder.Append(Sdp.SessionDescription.EqualsSign);
                builder.Append(repeatTime);
                builder.Append(SessionDescription.NewLineString);
            }

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

    #endregion
    
    //Could be Extension methods

    //Should be has started?
    //public bool IsLive { get { return RepeatTimes.Count == 0 && StartTime == 0; } }

    //Maybe should not be for less than 0
    //public bool IsContinious { get { return RepeatTimes.Count == 0 && StopTime <= 0; } }
}
