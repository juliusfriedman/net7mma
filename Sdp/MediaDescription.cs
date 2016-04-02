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
    #region MediaDescription

    /// <summary>
    /// Represents the MediaDescription in a Session Description.
    /// Parses and Creates.
    /// </summary>
    public class MediaDescription : Common.BaseDisposable, IEnumerable<SessionDescriptionLine>
    {
        //Nested type for MediaDescriptionLine?

        //Proto fields http://www.iana.org/assignments/sdp-parameters/sdp-parameters-2.csv

        //public sealed class ProtocolFields { const string RealTimeAudioVideoProfile = "RTP/AVP";  }

        public const char MediaDescriptionLineType = 'm';

        #region Fields

        //Created from the m= which is the first line, this is a computed line and not found in Lines.

        /// <summary>
        /// The MediaType of the MediaDescription
        /// </summary>
        public MediaType MediaType { get; set; }

        /// <summary>
        /// The MediaPort of the MediaDescription
        /// </summary>
        public int MediaPort { get; set; }

        /// <summary>
        /// Defines an optional value which is a port range that augments the MediaPort definition
        /// </summary>
        public int? PortRange { get; set; }

        /// <summary>
        /// The MediaProtocol of the MediaDescription
        /// </summary>
        public string MediaProtocol { get; set; }

        //Maybe add a few Computed properties such as SampleRate
        //OR
        //Maybe add methods for Get rtpmap, fmtp etc

        //LinesByType etc...

        //Keep in mind that adding/removing or changing lines should change the version of the parent SessionDescription
        List<SessionDescriptionLine> m_Lines = new List<SessionDescriptionLine>();

        List<int> m_PayloadList = new List<int>();

        /// <summary>
        /// The field which has been generated as a result of parsing or modifying the MediaFormat
        /// </summary>
        string MediaFormatString;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the string which is utilized in the Media Field to decribe the port range.
        /// </summary>
        internal protected string PortRangeString
        {
            get { return PortRange.HasValue ? string.Join(SessionDescription.ForwardSlashString, MediaPort.ToString(), PortRange.Value) : MediaPort.ToString(); }
        }

        /// <summary>
        /// The MediaFormat of the MediaDescription
        /// </summary>
        public string MediaFormat
        {
            get
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(MediaFormatString)) return MediaFormat = string.Join(SessionDescription.SpaceString, m_PayloadList);
                    return MediaFormatString;
                }
                catch (InvalidOperationException)
                {
                    return MediaFormat;
                }
                catch { throw; }
            }
            set
            {
                m_PayloadList.Clear();

                foreach (var payloadType in value.Split(SessionDescription.SpaceSplit, StringSplitOptions.RemoveEmptyEntries))
                {
                    int found;

                    if (int.TryParse(payloadType, out found))
                    {
                        m_PayloadList.Add(found);

                        continue;
                    }

                    MediaFormatString = string.Join(SessionDescription.SpaceString, payloadType);
                }
            }
        }

        /// <summary>
        /// Gets or sets the types of payloads which can be found in the MediaDescription
        /// </summary>
        public IEnumerable<int> PayloadTypes
        {
            get
            {
                return m_PayloadList;
            }
            set
            {
                m_PayloadList.Clear();
                MediaFormatString = null;
                m_PayloadList.AddRange(value);
            }
        }

        public IEnumerable<SessionDescriptionLine> Lines
        {
            get { return ((IEnumerable<SessionDescriptionLine>)this); }
        }

        /// <summary>
        /// Calculates the length in bytes of this MediaDescription.
        /// </summary>
        public int Length
        {
            get
            {
                return MediaDescriptionLine.Length + m_Lines.Sum(l => l.Length);
            }
        }

        #endregion

        #region Constructor

        public MediaDescription(string mediaDescription)
            : this(mediaDescription.Split(SessionDescription.CRLFSplit, StringSplitOptions.RemoveEmptyEntries), 0)
        {

        }

        public MediaDescription(MediaType mediaType, int mediaPort, string mediaProtocol, int mediaFormat)
            : this(mediaType, mediaPort, mediaProtocol, mediaFormat.ToString())
        {

        }

        public MediaDescription(MediaType mediaType, int mediaPort, string mediaProtocol, string mediaFormat)
        {
            MediaType = mediaType;
            MediaPort = mediaPort;
            MediaProtocol = mediaProtocol;
            MediaFormat = mediaFormat;
        }

        public MediaDescription(string[] sdpLines, int index) :
            this(sdpLines, ref index) { }

        [CLSCompliant(false)]
        public MediaDescription(string[] sdpLines, ref int index)
        {
            string sdpLine = sdpLines[index++].Trim();

            if (false == sdpLine.StartsWith("m=")) Media.Common.Extensions.Exception.ExceptionExtensions.RaiseTaggedException(this, "Invalid Media Description");

            sdpLine = sdpLine.Replace("m=", string.Empty);

            string[] parts = sdpLine.Split(SessionDescription.SpaceSplit, 4);

            if (parts.Length != 4) Media.Common.Extensions.Exception.ExceptionExtensions.RaiseTaggedException(this, "Invalid Media Description");

            try
            {
                MediaType = (MediaType)Enum.Parse(typeof(MediaType), SessionDescription.TrimLineValue(parts[0].ToLowerInvariant()));
            }
            catch
            {
                MediaType = Sdp.MediaType.unknown;
            }

            var mediaPorts = SessionDescription.TrimLineValue(parts[1]).Split(SessionDescription.SlashSplit, 2);

            MediaPort = int.Parse(SessionDescription.TrimLineValue(mediaPorts[0]), System.Globalization.CultureInfo.InvariantCulture);

            if (mediaPorts.Length > 1) PortRange = int.Parse(mediaPorts[1]);

            MediaProtocol = parts[2];

            MediaFormat = parts[3];

            //Parse remaining optional entries
            for (int e = sdpLines.Length; index < e; )
            {
                string line = sdpLines[index];

                if (line.StartsWith("m="))
                {
                    //Found the start of another MediaDescription
                    break;
                }
                else
                {
                    SessionDescriptionLine parsed;

                    if (SessionDescriptionLine.TryParse(sdpLines, ref index, out parsed)) m_Lines.Add(parsed);
                    else index++;
                }
            }
        }

        #endregion

        #region Methods

        public void Add(SessionDescriptionLine line)
        {
            if (line == null) return;

            //Should ensure that the line is allowed.

            m_Lines.Add(line);
        }

        public bool Remove(SessionDescriptionLine line)
        {
            return m_Lines.Remove(line);
        }

        public void RemoveLine(int index)
        {
            m_Lines.RemoveAt(index);
        }

        #endregion

        #region Overloads

        //Dispose

        public override string ToString()
        {
            return ToString(null);
        }

        public string ToString(SessionDescription sdp = null)
        {
            StringBuilder buffer = new StringBuilder();

            if (sdp != null)
            {
                /*
                If multiple addresses are specified in the "c=" field and multiple
                ports are specified in the "m=" field, a one-to-one mapping from
                port to the corresponding address is implied.  For example:
                
                  c=IN IP4 224.2.1.1/127/2
                  m=video 49170/2 RTP/AVP 31
                */

                Sdp.Lines.SessionConnectionLine connectionLine = sdp.Lines.OfType<Sdp.Lines.SessionConnectionLine>().FirstOrDefault();

                if (connectionLine != null && connectionLine.HasMultipleAddresses)
                {
                    int? portSpecifier = connectionLine.Ports;

                    if (portSpecifier.HasValue)
                    {
                        buffer.Append(MediaDescriptionLineType.ToString() + Sdp.SessionDescription.EqualsSign + string.Join(SessionDescription.Space.ToString(), MediaType, MediaPort.ToString() + ((char)Common.ASCII.ForwardSlash).ToString() + portSpecifier, MediaProtocol, MediaFormat) + SessionDescription.NewLineString);

                        goto LinesOnly;
                    }
                }
            }

            //Note if Unassigned MediaFormat is used that this might have to be a 'char' to be exactly what was given
            buffer.Append(MediaDescriptionLine.ToString());

        LinesOnly:
            foreach (SessionDescriptionLine l in m_Lines.Where(l => l.Type != Sdp.Lines.SessionBandwidthLine.BandwidthType && l.Type != Sdp.Lines.SessionAttributeLine.AttributeType))
                buffer.Append(l.ToString());

            foreach (SessionDescriptionLine l in m_Lines.Where(l => l.Type == Sdp.Lines.SessionBandwidthLine.BandwidthType))
                buffer.Append(l.ToString());

            foreach (SessionDescriptionLine l in m_Lines.Where(l => l.Type == Sdp.Lines.SessionAttributeLine.AttributeType))
                buffer.Append(l.ToString());

            return buffer.ToString();
        }

        #endregion

        #region Named Lines

        internal protected SessionDescriptionLine MediaDescriptionLine
        {
            get
            {
                return new SessionDescriptionLine(MediaDescriptionLineType, SessionDescription.SpaceString)
                {
                    MediaType.ToString(),
                    PortRangeString,
                    MediaProtocol,
                    MediaFormat
                };
            }
        }

        public IEnumerable<SessionDescriptionLine> AttributeLines
        {
            get
            {
                return m_Lines.Where(l => l.Type == Sdp.Lines.SessionAttributeLine.AttributeType);
            }
        }

        public IEnumerable<SessionDescriptionLine> BandwidthLines
        {
            get
            {
                return m_Lines.Where(l => l.Type == Sdp.Lines.SessionBandwidthLine.BandwidthType);
            }
        }

        public SessionDescriptionLine ConnectionLine { get { return m_Lines.FirstOrDefault(l => l.Type == Sdp.Lines.SessionConnectionLine.ConnectionType); } }

        public SessionDescriptionLine RtpMapLine
        {
            get
            {
                return m_Lines.FirstOrDefault(l => l.Type == Sdp.Lines.SessionAttributeLine.AttributeType && l.m_Parts.Count > 0 && l.m_Parts[0].StartsWith("rtpmap:", StringComparison.InvariantCultureIgnoreCase));
            }
        }
       
        public SessionDescriptionLine FmtpLine
        {
            get
            {
                return m_Lines.FirstOrDefault(l => l.Type == Sdp.Lines.SessionAttributeLine.AttributeType && l.m_Parts.Count > 0 && l.m_Parts[0].StartsWith("fmtp:", StringComparison.InvariantCultureIgnoreCase));
            }
        }

        public SessionDescriptionLine RangeLine
        {
            get { return m_Lines.FirstOrDefault(l => l.Type == Sdp.Lines.SessionAttributeLine.AttributeType && l.m_Parts.Count > 0 && l.m_Parts[0].StartsWith("range:", StringComparison.InvariantCultureIgnoreCase)); }
        }

        public SessionDescriptionLine ControlLine
        {
            get
            {
                return m_Lines.FirstOrDefault(l => l.Type == Sdp.Lines.SessionAttributeLine.AttributeType && l.m_Parts.Count > 0 && l.m_Parts[0].StartsWith("control:", StringComparison.InvariantCultureIgnoreCase));
            }
        }

        //Needs tool lines

        #endregion

        #region Lines

        public IEnumerator<SessionDescriptionLine> GetEnumerator()
        {
            yield return MediaDescriptionLine;

            foreach (var line in m_Lines)
            {
                if (line == null) continue;

                yield return line;
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<SessionDescriptionLine>)this).GetEnumerator();
        }

        #endregion
    }

    public static class MediaDescriptionExtensions
    {
        /// <summary>
        /// Parses the <see cref="MediaDescription.ControlLine"/> and if present
        /// </summary>
        /// <param name="mediaDescription"></param>
        /// <param name="source"></param>
        /// <returns></returns>
        public static Uri GetAbsoluteControlUri(this MediaDescription mediaDescription, Uri source, SessionDescription sessionDescription = null)
        {
            if (source == null) throw new ArgumentNullException("source");

            if (mediaDescription == null) return source;

            if (false == source.IsAbsoluteUri) throw new InvalidOperationException("source.IsAbsoluteUri must be true.");

            SessionDescriptionLine controlLine = mediaDescription.ControlLine;

            //If there is a control line in the SDP it contains the URI used to setup and control the media
            if (controlLine != null)
            {
                string controlPart = controlLine.Parts.Where(p => p.Contains("control")).FirstOrDefault();

                //If there is a controlPart in the controlLine
                if (false == string.IsNullOrWhiteSpace(controlPart))
                {
                    //Prepare the part
                    controlPart = controlPart.Split(Media.Sdp.SessionDescription.ColonSplit, 2, StringSplitOptions.RemoveEmptyEntries).Last();

                    //Create a uri
                    Uri controlUri = new Uri(controlPart, UriKind.RelativeOrAbsolute);

                    //Determine if its a Absolute Uri
                    if (controlUri.IsAbsoluteUri) return controlUri;

                    //Return a new uri using the original string and the controlUri relative path.
                    //Hopefully the direction of the braces matched

                                                                                                    //string.Join(source.OriginalString, controlUri.OriginalString);

                    return new Uri(source.OriginalString.EndsWith(SessionDescription.ForwardSlashString) ? source.OriginalString + controlUri.OriginalString : string.Join(SessionDescription.ForwardSlashString, source.OriginalString, controlUri.OriginalString));

                    #region Explination
                    //I wonder if Mr./(Dr) Fielding is happy...
                    //Let source = 
                    //rtsp://alt1.v7.cache3.c.youtube.com/CigLENy73wIaHwmddh2T-s8niRMYDSANFEgGUgx1c2VyX3VwbG9hZHMM/0/0/0/1/video.3gp/trackID=0
                    //Call
                    //return new Uri(source, controlUri);
                    //Result = 
                    //rtsp://alt1.v7.cache3.c.youtube.com/CigLENy73wIaHwmddh2T-s8niRMYDSANFEgGUgx1c2VyX3VwbG9hZHMM/0/0/0/1/trackID=0
                    #endregion
                }
            }

            //Try to take the session level control uri
            Uri sessionControlUri;

            //If there was a session description given and it supports aggregate media control then return that uri
            if (sessionDescription != null && sessionDescription.SupportsAggregateMediaControl(out sessionControlUri, source)) return sessionControlUri;

            //There is no control line, just return the source.
            return source;
        }

        public static TimeDescription GetTimeDescription(this MediaDescription mediaDescription, SessionDescription sessionDescription)
        {
            if (mediaDescription == null || sessionDescription == null) return null;

            //Get index of mediaDescription

            //Needs a better way to get the index of the media description
            int index = sessionDescription.GetIndexFor(mediaDescription);  //Array.IndexOf(sessionDescription.MediaDescriptions.ToArray(), mediaDescription);

            if (index == -1) return null;

            return sessionDescription.GetTimeDescription(index);
        }

        //Should have a date when or should return the date playable, which would then be used by another method to compare against a time.
        public static bool IsPlayable(this MediaDescription mediaDescription, SessionDescription sessionDescription) //, DateTime? check = null) ,TimeSpan within = TimeSpan.Zero
        {
            if (mediaDescription == null || sessionDescription == null) return false;

            //Get index of mediaDesription
            
            //Check TimeDescription @ index.

            TimeDescription td = GetTimeDescription(mediaDescription, sessionDescription);

            if (td == null) return true;

            //Unbound start and end ?
            if (td.StartTime == 0
                &&
                td.StopTime == 0) return true;


            //Notes multiple calls to UtcNow... (avoid with a within parameter)?
            try
            {
                //Ensure not a bounded end and that the end time is less than now
                if (td.StopTime != 0
                    &&
                    td.NtpStopDateTime >= DateTime.UtcNow) return false;

                //Ensure start time is not bounded and that the start time is greater than now
                if (td.StartTime != 0
                    &&
                    td.NtpStartDateTime > DateTime.UtcNow) return false;

                //Check repeat times.

                //td.RepeatTimes;
            }
            catch
            {
                //Out of range values for conversion, assume true if end is unbounded
                if (td.StopTime != 0) return false;
            }
            finally
            {
                td = null;
            }
            
            return true;
        }
    }

    #endregion
}
