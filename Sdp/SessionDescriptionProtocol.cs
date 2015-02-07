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
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.Sdp
{
    /// <summary>
    /// Media Types used in SessionMediaDescription
    /// </summary>
    public enum MediaType : byte
    {
        unknown = 0,
        audio,
        video,
        text,
        timing,
        application,
        message,
        //
        data,
        control,
        //http://tools.ietf.org/html/rfc3840#section-10
        //automata,
        //Class, (business, personal, busipersonal)
        //Duplex,
        //Extensions,
        //mobility,
        //description,
        whiteboard //never specified in 4566 but referenced 3 total times
    }

    /// <summary>
    /// Provides facilities for parsing and creating SessionDescription data
    /// http://en.wikipedia.org/wiki/Session_Description_Protocol
    /// http://tools.ietf.org/html/rfc4566
    /// </summary>
    /// 
    ///https://msdn.microsoft.com/en-us/library/bb758954(v=office.13).aspx
    public sealed class SessionDescription : Common.BaseDisposable
    {
        #region Statics

        public const string MimeType = "application/sdp";

        public const char AttributeType = 'a', BandwidthType = 'b', EqualsSign = (char)Common.ASCII.EqualsSign, HyphenSign = (char)Common.ASCII.HyphenSign, SemiColon = (char)Common.ASCII.SemiColon, Colon = (char)Common.ASCII.Colon, Space = (char)Common.ASCII.Space;

        internal static string LF = new string((char)Common.ASCII.LineFeed, 1), CR = new string((char)Common.ASCII.NewLine, 1), CRLF = CR + LF;

        internal static string[] ColonSplit = new string[] { Colon.ToString() }, CRLFSplit = new string[] { CRLF };

        internal static char[] SlashSplit = new char[] { (char)Common.ASCII.ForwardSlash };

        internal static string TrimLineValue(string value) { return string.IsNullOrWhiteSpace(value) ? value : value.Trim(); }

        /// <summary>
        /// Parse a range line.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="type"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public static bool TryParseRange(string value, out string type, out TimeSpan start, out TimeSpan end)
        {

            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentNullException("value");

            type = Utility.Unknown;
            start = TimeSpan.Zero;
            end = Utility.InfiniteTimeSpan;

            int offset = 0;

            try
            {
                //range: = 6 (may be present)
                string[] parts = value.Split(Media.Sdp.SessionDescription.Colon, Media.Sdp.SessionDescription.HyphenSign, Media.Sdp.SessionDescription.EqualsSign);

                int partsLength = parts.Length;

                type = parts[offset++]; //npt, etc

                if (type == "range") type = parts[offset++];

                double seconds = 0;

                switch (type)
                {
                    case "npt":
                        {
                            if (parts[offset].ToLowerInvariant() == "now") start = TimeSpan.Zero;
                            else if (partsLength == 3)
                            {
                                if (parts[offset].Contains(':'))
                                {
                                    start = TimeSpan.Parse(parts[offset++].Trim(), System.Globalization.CultureInfo.InvariantCulture);
                                }
                                else
                                {
                                    start = TimeSpan.FromSeconds(double.Parse(parts[offset++].Trim(), System.Globalization.CultureInfo.InvariantCulture));
                                }
                            }
                            else if (partsLength == 4)
                            {
                                if (parts[offset].Contains(':'))
                                {
                                    start = TimeSpan.Parse(parts[offset++].Trim(), System.Globalization.CultureInfo.InvariantCulture);
                                    end = TimeSpan.Parse(parts[offset++].Trim(), System.Globalization.CultureInfo.InvariantCulture);
                                }
                                else
                                {
                                    if (double.TryParse(parts[offset++].Trim(), out seconds)) start = TimeSpan.FromSeconds(seconds);
                                    if (double.TryParse(parts[offset++].Trim(), out seconds)) end = TimeSpan.FromSeconds(seconds);
                                    
                                }
                            }
                            else throw new InvalidOperationException("Invalid Range Header: " + value);

                            break;
                        }
                    case "clock":
                        {
                            //Check for live
                            if (parts[offset].ToLowerInvariant() == "now") start = TimeSpan.Zero;
                            //Check for start time only
                            else if (partsLength == 3)
                            {
                                DateTime now = DateTime.UtcNow, startDate;
                                ///Parse and determine the start time
                                if (DateTime.TryParse(parts[offset++].Trim(), out startDate))
                                {
                                    //Time in the past
                                    if (now > startDate) start = now - startDate;
                                    //Future?
                                    else start = startDate - now;
                                }
                                //Only start is live?
                                //m_Live = true;
                            }
                            else if (partsLength == 4)
                            {
                                DateTime now = DateTime.UtcNow, startDate, endDate;
                                ///Parse and determine the start time
                                if (DateTime.TryParse(parts[offset++].Trim(), out startDate))
                                {
                                    //Time in the past
                                    if (now > startDate) start = now - startDate;
                                    //Future?
                                    else start = startDate - now;
                                }

                                ///Parse and determine the end time
                                if (DateTime.TryParse(parts[offset++].Trim(), out endDate))
                                {
                                    //Time in the past
                                    if (now > endDate) end = now - endDate;
                                    //Future?
                                    else end = startDate - now;
                                }
                            }
                            else throw new InvalidOperationException("Invalid Range Header Received: " + value);
                            
                            break;
                        }
                    case "smpte":
                        {
                            //Get the times into the times array skipping the time from the server (order may be first so I explicitly did not use Substring overload with count)
                            if (parts[offset].ToLowerInvariant() == "now") start = TimeSpan.Zero;
                            else if (partsLength == 3)
                            {
                                start = TimeSpan.Parse(parts[offset++].Trim(), System.Globalization.CultureInfo.InvariantCulture);
                            }
                            else if (partsLength == 4)
                            {
                                start = TimeSpan.Parse(parts[offset++].Trim(), System.Globalization.CultureInfo.InvariantCulture);
                                end = TimeSpan.Parse(parts[offset++].Trim(), System.Globalization.CultureInfo.InvariantCulture);
                            }
                            else throw new InvalidOperationException("Invalid Range Header Received: " + value);
                            
                            break;
                        }
                    default:
                        {
                            if (partsLength > 0)
                            {
                                if (parts[offset] != "now" && double.TryParse(parts[offset++], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out seconds))
                                {
                                    start = TimeSpan.FromSeconds(seconds);
                                }
                            }

                            //If there is a start and end time
                            if (partsLength > 1)
                            {
                                if (!string.IsNullOrWhiteSpace(parts[offset]) && double.TryParse(parts[offset++], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out seconds))
                                {
                                    end = TimeSpan.FromSeconds(seconds);
                                }
                            }

                            break;
                        }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Fields

        //Should be done in constructor of a new 
        Media.Sdp.Lines.SessionVersionLine m_SessionVersion;
        Media.Sdp.Lines.SessionOriginatorLine m_Originator;
        Media.Sdp.Lines.SessionNameLine m_SessionName;

        List<MediaDescription> m_MediaDescriptions = new List<MediaDescription>();
        List<TimeDescription> m_TimeDescriptions = new List<TimeDescription>();
        List<SessionDescriptionLine> m_Lines = new List<SessionDescriptionLine>();

        #endregion

        #region Properties

        /// <summary>
        /// Gets or Sets the version as indicated on the `v=` line.
        /// </summary>
        public int SessionVersion
        {
            get { return m_SessionVersion.Version; }
            private set
            {
                if (m_SessionVersion == null || value != m_SessionVersion.Version)
                {
                    m_SessionVersion = new Lines.SessionVersionLine(value); ++m_Originator.SessionVersion;
                }
            }
        }

        /// <summary>
        /// Gets or sets the value of the 'o=' line.
        /// </summary>
        public string OriginatorAndSessionIdentifier
        {
            get { return m_Originator.ToString(); }
            set { m_Originator = new Media.Sdp.Lines.SessionOriginatorLine(value.ToString()); }
        }

        /// <summary>
        /// Gets or sets the value of the 's=' line.
        /// When setting the version of the document is changed.
        /// </summary>
        public string SessionName
        {
            get { return m_SessionName.SessionName; }
            set
            {
                if (m_SessionName == null || value != m_SessionName.SessionName)
                {
                    m_SessionName = new Lines.SessionNameLine(value); 
                    
                    ++m_Originator.SessionVersion;
                }
            }
        }

        /// <summary>
        /// Gets or sets the value assoicated with the SessionId of this SessionDescription as indicated in the 'o=' line.
        /// </summary>
        public string SessionId
        {
            get
            {
                return m_Originator == null ? string.Empty : m_Originator.SessionId;
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value)) throw new ArgumentNullException();

                m_Originator.SessionId = value;

                ++m_Originator.SessionVersion;
            }
        }

        public IEnumerable<TimeDescription> TimeDescriptions { get { return m_TimeDescriptions.AsReadOnly(); } set { m_TimeDescriptions = value.ToList(); ++m_Originator.SessionVersion; } }

        public IEnumerable<MediaDescription> MediaDescriptions { get { return m_MediaDescriptions.AsReadOnly(); } set { m_MediaDescriptions = value.ToList(); ++m_Originator.SessionVersion; } }

        /// <summary>
        /// Gets the lines assoicated with the Session level attributes which are lines other than the o, i or c lines.
        /// </summary>
        public IEnumerable<SessionDescriptionLine> Lines
        {
            get
            {
                //Should also return m_Originator, m_Version and m_SessionName lines before m_Lines?
                return (((SessionDescriptionLine)m_SessionVersion).Yield()).Concat(m_Originator.Yield()).Concat(m_SessionName.Yield()).Concat(m_Lines);
            }
            //set
            //{
            //    m_Lines = value.ToList(); 
            //    ++m_Originator.SessionVersion;
            //}
        }

        public SessionDescriptionLine ConnectionLine { get { return m_Lines.FirstOrDefault(l => l.Type == Sdp.Lines.SessionConnectionLine.ConnectionType); } }

        public SessionDescriptionLine RangeLine { get { return m_Lines.FirstOrDefault(l => l.Type == AttributeType && l.Parts[0].StartsWith("range:", StringComparison.InvariantCultureIgnoreCase)); } }

        public SessionDescriptionLine ControlLine { get { return m_Lines.FirstOrDefault(l => l.Type == AttributeType && l.Parts[0].StartsWith("control:", StringComparison.InvariantCultureIgnoreCase)); } }

        /// <summary>
        /// Calculates the length in bytes of this SessionDescription.
        /// </summary>
        public int Length
        {
            get
            {
                return (m_Originator == null ? 0 : m_Originator.Length) +
                    (m_SessionName == null ? 0 : m_SessionName.Length) +
                    (m_SessionVersion == null ? 0 : m_SessionVersion.Length) + 
                    m_Lines.Sum(l=>l.Length) + 
                    m_MediaDescriptions.Sum(md => md.Length) + 
                    m_TimeDescriptions.Sum(td => td.Length) +
                    2; //CRLF
            }
        }

        #endregion

        #region Constructor

        public SessionDescription(int version)
        {
            m_Originator = new Lines.SessionOriginatorLine();

            m_SessionName = new Sdp.Lines.SessionNameLine();

            SessionVersion = version;
        }

        public SessionDescription(string originatorString, string sessionName)
            :this(0)
        {
            OriginatorAndSessionIdentifier = originatorString;

            SessionName = sessionName;
        }

        /// <summary>
        /// Constructs a new Session Description
        /// </summary>
        /// <param name="protocolVersion">Usually 0</param>
        /// <param name="originatorAndSession">Compound string identifying origionator and session identifier</param>
        /// <param name="sessionName">name of the session</param>
        public SessionDescription(int protocolVersion, string originatorAndSession, string sessionName)
            : this(protocolVersion)
        {
            OriginatorAndSessionIdentifier = originatorAndSession;

            SessionName = sessionName;
        }

        /// <summary>
        /// Constructs a SessionDescription from the given contents of a Session Description Protocol message
        /// </summary>
        /// <param name="sdpContents">The Session Description Protocol usually recieved in the Describe request of a RtspClient</param>
        public SessionDescription(string sdpContents)
        {
            string[] lines = sdpContents.Split(SessionDescription.CRLFSplit, StringSplitOptions.RemoveEmptyEntries);

            if (lines.Length < 3) Common.ExceptionExtensions.RaiseTaggedException(this, "Invalid Session Description");

            //Parse remaining optional entries
            for (int lineIndex = 0, endIndex = lines.Length; lineIndex < endIndex; /*Advancement of the loop controlled by the corrsponding Lines via ref*/)
            {
                string line = lines[lineIndex].Trim();

                switch (line[0])
                {
                    case Media.Sdp.Lines.SessionVersionLine.VersionType:
                        {
                            m_SessionVersion = new Media.Sdp.Lines.SessionVersionLine(lines, ref lineIndex);
                            continue;
                        }
                    case Media.Sdp.Lines.SessionOriginatorLine.OriginatorType:
                        {
                            m_Originator = new Media.Sdp.Lines.SessionOriginatorLine(lines, ref lineIndex);
                            continue;
                        }
                    case Media.Sdp.Lines.SessionNameLine.NameType:
                        {
                            m_SessionName = new Media.Sdp.Lines.SessionNameLine(lines, ref lineIndex);
                            continue;
                        }
                    case TimeDescription.TimeDescriptionType:
                        {
                            m_TimeDescriptions.Add(new TimeDescription(lines, ref lineIndex));
                            continue;
                        }
                    case MediaDescription.MediaDescriptionType:
                        {
                            m_MediaDescriptions.Add(new MediaDescription(lines, ref lineIndex));
                            continue;
                        }
                    default:
                        {
                            SessionDescriptionLine parsed;

                            if(SessionDescriptionLine.TryParse(lines, ref lineIndex, out parsed)) m_Lines.Add(parsed);
                            else lineIndex++;//No advance was made on lineIndex by SessionDescriptionLine if parsed was null

                            continue;
                        }
                }
            }            
        }

        /// <summary>
        /// Creates a copy of another SessionDescription
        /// </summary>
        /// <param name="other">The SessionDescription to copy</param>
        public SessionDescription(SessionDescription other, bool reference = false)
        {
            SessionVersion = other.SessionVersion;

            OriginatorAndSessionIdentifier = other.OriginatorAndSessionIdentifier;

            SessionName = other.SessionName;

            if (reference)
            {
                m_TimeDescriptions = other.m_TimeDescriptions;

                m_MediaDescriptions = other.m_MediaDescriptions;

                m_Lines = other.m_Lines;
            }
            else
            {
                m_TimeDescriptions = new List<TimeDescription>(other.TimeDescriptions);

                m_MediaDescriptions = new List<MediaDescription>(other.m_MediaDescriptions);

                m_Lines = new List<SessionDescriptionLine>(other.Lines);
            }
        }

        #endregion

        #region Methods        

        public void Add(MediaDescription mediaDescription, bool updateVersion = true)
        {
            if (mediaDescription == null) return;
            m_MediaDescriptions.Add(mediaDescription);
            if (updateVersion) ++m_Originator.SessionVersion;
        }

        public void Add(TimeDescription timeDescription, bool updateVersion = true)
        {
            if (timeDescription == null) return;
            m_TimeDescriptions.Add(timeDescription);
            if (updateVersion) ++m_Originator.SessionVersion;
        }

        public void Add(SessionDescriptionLine line, bool updateVersion = true)
        {
            if (line == null) return;
            m_Lines.Add(line);
            if (updateVersion) ++m_Originator.SessionVersion;
        }

        public bool Remove(SessionDescriptionLine line, bool updateVersion = true)
        {
            bool result = m_Lines.Remove(line);
            if (updateVersion && result) ++m_Originator.SessionVersion;
            return result;
        }

        public bool Remove(TimeDescription timeDescription, bool updateVersion = true)
        {
            bool result = m_TimeDescriptions.Remove(timeDescription);
            if (updateVersion && result) ++m_Originator.SessionVersion;
            return result;
        }

        public bool Remove(MediaDescription mediaDescription, bool updateVersion = true)
        {
            bool result = m_MediaDescriptions.Remove(mediaDescription);
            if (updateVersion && result) ++m_Originator.SessionVersion;
            return result;
        }

        public void RemoveLine(int index, bool updateVersion = true)
        {
            m_Lines.RemoveAt(index);
            if (updateVersion) ++m_Originator.SessionVersion;
        }

        public void RemoveMediaDescription(int index, bool updateVersion = true)
        {
            m_MediaDescriptions.RemoveAt(index);
            if (updateVersion) ++m_Originator.SessionVersion;
        }

        public void RemoveTimeDescription(int index, bool updateVersion = true)
        {
            m_TimeDescriptions.RemoveAt(index);
            if(updateVersion) ++m_Originator.SessionVersion;
        }

        #endregion

        #region Overrides

        public override string ToString()
        {
            StringBuilder buffer = new StringBuilder();

            if(m_SessionVersion != null) buffer.Append(m_SessionVersion.ToString());

            if (m_Originator != null) buffer.Append(m_Originator.ToString());

            if (m_SessionName != null) buffer.Append(m_SessionName.ToString());

            foreach (SessionDescriptionLine l in m_Lines.Where(l => l.Type != Sdp.SessionDescription.BandwidthType && l.Type != Sdp.SessionDescription.AttributeType))
            {
                buffer.Append(l.ToString());
            }

            foreach (SessionDescriptionLine l in m_Lines.Where(l => l.Type == Sdp.SessionDescription.BandwidthType))
            {
                buffer.Append(l.ToString());
            }

            foreach (SessionDescriptionLine l in m_Lines.Where(l => l.Type == Sdp.SessionDescription.AttributeType))
            {
                buffer.Append(l.ToString());
            }

            m_TimeDescriptions.ForEach(td => buffer.Append(td.ToString(this)));

            m_MediaDescriptions.ForEach(md => buffer.Append(md.ToString(this)));

            //Strings in .Net are Unicode code points ( subsequently the characters only are addressable by their 16 bit code point representation).
            return buffer.ToString();
        }

        public override void Dispose()
        {

            if (IsDisposed) return;

            base.Dispose();

            m_SessionVersion = null;

            m_Originator = null;

            m_SessionName = null;

            m_MediaDescriptions.Clear();

            m_MediaDescriptions = null;

            m_TimeDescriptions.Clear();

            m_TimeDescriptions = null;

            m_Lines.Clear();

            m_Lines = null;
        }

        #endregion

    }

    public static class SessionDescriptionExtensions
    {
        public static bool SupportsAggregateControl(this SessionDescription sdp)
        {
            SessionDescriptionLine controlLine = sdp.ControlLine;

            //If there is a control line in the SDP it contains the URI used to setup and control the media
            if (controlLine == null) return false;
            
            //Get the control token
            string controlPart = controlLine.Parts.Where(p => p.Contains("control")).FirstOrDefault();

            //If there is a controlPart in the controlLine
            if (false == string.IsNullOrWhiteSpace(controlPart))
            {
                //Prepare the part
                controlPart = controlPart.Split(Media.Sdp.SessionDescription.ColonSplit, 2, StringSplitOptions.RemoveEmptyEntries).Last();

                //if unqualified then there is no aggregate control.
                if (controlPart == "*") return false;

                //Create a uri
                Uri controlUri = new Uri(controlPart, UriKind.RelativeOrAbsolute);

                //Determine if its a Absolute Uri
                if (controlUri.IsAbsoluteUri) return true;
            }

            //Another type of control line is present.
            return false;
        }
    }

    /// <summary>
    /// Represents the MediaDescription in a Session Description.
    /// Parses and Creates.
    /// </summary>
    public class MediaDescription
    {
        public const char MediaDescriptionType = 'm';

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
        /// The MediaProtocol of the MediaDescription
        /// </summary>
        public string MediaProtocol { get; set; }

        /// <summary>
        /// The MediaFormat of the MediaDescription
        /// </summary>
        public byte MediaFormat { get; set; }

        //Maybe add a few Computed properties such as SampleRate
        //OR
        //Maybe add methods for Get rtpmap, fmtp etc

        //LinesByType etc...

        //Keep in mind that adding/removing or changing lines should change the version of the parent SessionDescription
        internal List<SessionDescriptionLine> m_Lines = new List<SessionDescriptionLine>();

        #endregion

        public System.Collections.ObjectModel.ReadOnlyCollection<SessionDescriptionLine> Lines { get { return m_Lines.AsReadOnly(); } }


        /// <summary>
        /// Calculates the length in bytes of this MediaDescription.
        /// </summary>
        public int Length
        {
            get
            {
                //(m=)X(\r\n)
                return (4 + (string.Join(SessionDescription.Space.ToString(), MediaType, MediaPort.ToString(), MediaProtocol, MediaFormat)).Length) + m_Lines.Sum(l => l.Length);
            }
        }

        #region Constructor

        public MediaDescription(MediaType mediaType, int mediaPort, string mediaProtocol, byte mediaFormat)
        {
            MediaType = mediaType;
            MediaPort = mediaPort;
            MediaProtocol = mediaProtocol;
            MediaFormat = mediaFormat;
        }

        public MediaDescription(string[] sdpLines, ref int index)
        {
            string sdpLine = sdpLines[index++].Trim();

            if (!sdpLine.StartsWith("m=")) Common.ExceptionExtensions.RaiseTaggedException(this,"Invalid Media Description");
            else sdpLine = sdpLine.Replace("m=", string.Empty);

            string[] parts = sdpLine.Split(SessionDescription.Space);

            if (parts.Length != 4) Common.ExceptionExtensions.RaiseTaggedException(this,"Invalid Media Description");

            try
            {
                MediaType = (MediaType)Enum.Parse(typeof(MediaType), SessionDescription.TrimLineValue(parts[0].ToLowerInvariant()));
            }
            catch
            {
                MediaType = Sdp.MediaType.unknown;
            }

            MediaPort = int.Parse(SessionDescription.TrimLineValue(parts[1]), System.Globalization.CultureInfo.InvariantCulture);

            MediaProtocol = parts[2];

            //Notes that this can apparently be a * or something besides 3 digits, rightnow ("*" turns into 43)
            //Maybe should allow to be int or string?
            byte temp;

            if (byte.TryParse(SessionDescription.TrimLineValue(parts[3]), out temp)) MediaFormat = temp;
            else MediaFormat = (byte)parts[3].FirstOrDefault();

            //Parse remaining optional entries
            for (int e = sdpLines.Length; index < e;)
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

        internal void Add(SessionDescriptionLine line)
        {
            if (line == null) return;
            m_Lines.Add(line);
        }

        internal void RemoveLine(int index)
        {
            m_Lines.RemoveAt(index);
        }

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

                if (connectionLine != null)
                {
                    int? portSpecifier = connectionLine.Ports;

                    if (portSpecifier.HasValue)
                    {
                        //Note if Unassigned MediaFormat is used that this might have to be a 'char' to be exactly what was given
                        buffer.Append(MediaDescriptionType.ToString() + Sdp.SessionDescription.EqualsSign + string.Join(SessionDescription.Space.ToString(), MediaType, MediaPort.ToString() + '/' + portSpecifier, MediaProtocol, MediaFormat) + SessionDescription.CRLF);
                        goto LinesOnly;
                    }
                }
            }

            //Note if Unassigned MediaFormat is used that this might have to be a 'char' to be exactly what was given
            buffer.Append(MediaDescriptionType.ToString() + Sdp.SessionDescription.EqualsSign + string.Join(SessionDescription.Space.ToString(), MediaType, MediaPort.ToString(), MediaProtocol, MediaFormat) + SessionDescription.CRLF);

        LinesOnly:
            foreach (SessionDescriptionLine l in m_Lines.Where(l => l.Type != Sdp.SessionDescription.BandwidthType && l.Type != Sdp.SessionDescription.AttributeType))
                buffer.Append(l.ToString());

            foreach (SessionDescriptionLine l in m_Lines.Where(l => l.Type == Sdp.SessionDescription.BandwidthType))
                buffer.Append(l.ToString());

            foreach (SessionDescriptionLine l in m_Lines.Where(l => l.Type == Sdp.SessionDescription.AttributeType))
                buffer.Append(l.ToString());

            return buffer.ToString();
        }

        public SessionDescriptionLine ConnectionLine { get { return m_Lines.FirstOrDefault(l => l.Type == Sdp.Lines.SessionConnectionLine.ConnectionType); } }

        public SessionDescriptionLine RtpMapLine
        {
            get
            {
                return m_Lines.FirstOrDefault(l => l.Type == Sdp.SessionDescription.AttributeType && l.Parts[0].StartsWith("rtpmap:", StringComparison.InvariantCultureIgnoreCase));
            }
        }

        public SessionDescriptionLine FmtpLine
        {
            get
            {
                return m_Lines.FirstOrDefault(l => l.Type == Sdp.SessionDescription.AttributeType && l.Parts[0].StartsWith("fmtp:", StringComparison.InvariantCultureIgnoreCase));
            }
        }

        public SessionDescriptionLine RangeLine
        {
            get { return m_Lines.FirstOrDefault(l => l.Type == Sdp.SessionDescription.AttributeType && l.Parts[0].StartsWith("range:", StringComparison.InvariantCultureIgnoreCase)); }
        }

        public SessionDescriptionLine ControlLine
        {
            get
            {
                return m_Lines.FirstOrDefault(l => l.Type == Sdp.SessionDescription.AttributeType && l.Parts[0].StartsWith("control:", StringComparison.InvariantCultureIgnoreCase));
            }
        }

        public IEnumerable<SessionDescriptionLine> BandwidthLines
        {
            get
            {
                return m_Lines.Where(l => l.Type == Sdp.SessionDescription.BandwidthType);
            }
        }        
    }

    public static class MediaDescriptionExtensions
    {
        /// <summary>
        /// Parses the <see cref="MediaDescription.ControlLine"/> and if present
        /// </summary>
        /// <param name="mediaDescription"></param>
        /// <param name="source"></param>
        /// <returns></returns>
        public static Uri GetAbsoluteControlUri(this MediaDescription mediaDescription, Uri source)
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
                if (!string.IsNullOrWhiteSpace(controlPart))
                {
                    //Prepare the part
                    controlPart = controlPart.Split(Media.Sdp.SessionDescription.ColonSplit, 2, StringSplitOptions.RemoveEmptyEntries).Last();

                    //Create a uri
                    Uri controlUri = new Uri(controlPart, UriKind.RelativeOrAbsolute);

                    //Determine if its a Absolute Uri
                    if (controlUri.IsAbsoluteUri) return controlUri;

                    //hmm  - Determines the difference between two Uri instances.
                    //return controlUri.MakeRelativeUri(source);

                    //Return a new uri using the original string and the controlUri relative path.
                    return new Uri(source.OriginalString + '/' + controlUri.OriginalString);
                }
            }

            //There is no control line, just return the source.
            return source;
        }

        //Todo Seperate into GetReceiveBandwidth, GetSendBandwidth, GetApplicationSpecificBandwidth

        //public static bool IsRtcpEnabled(this MediaDescription mediaDescription)
        //{

        //    if (mediaDescription == null) return false;

        //    int reportSendingEvery = 0, reportReceivingEvery = 0;

        //    foreach (Media.Sdp.SessionDescriptionLine line in mediaDescription.BandwidthLines)
        //    {
        //        //Should be constant
        //        if (line.Parts[0].StartsWith("RR"))
        //        {
        //            reportReceivingEvery = int.Parse(line.Parts[0].Split(Media.Sdp.SessionDescription.ColonSplit, StringSplitOptions.RemoveEmptyEntries)[1]);
        //        }

        //        if (line.Parts[0].StartsWith("RS"))
        //        {
        //            reportSendingEvery = int.Parse(line.Parts[0].Split(Media.Sdp.SessionDescription.ColonSplit, StringSplitOptions.RemoveEmptyEntries)[1]);
        //        }

        //        //if (line.Parts[0].StartsWith("AS"))
        //        //{
        //        //    applicationSpecific = int.Parse(line.Parts[0].Split(Colon, StringSplitOptions.RemoveEmptyEntries)[1]);
        //        //}
        //    }

        //    //Determine if rtcp is disabled
        //    return reportReceivingEvery + reportSendingEvery > 0;
        //}
    }

    /// <summary>
    /// Represents a TimeDescription with optional Repeat times.
    /// Parses and Creates.
    /// </summary>
    public class TimeDescription
    {

        public const char TimeDescriptionType = 't', RepeatTimeType = 'r';

        public double SessionStartTime { get; private set; }

        public double SessionStopTime { get; private set; }

        //public bool IsLive { get { return SessionStartTime == 0 } }

        //public bool IsContinious { get { return SessionStopTime <= 0; } }

        //Ntp Timestamps from above, NOTE they do not wrap in 2036
        //public DateTime Start, Stop;

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

            if (sdpLine[0] != TimeDescriptionType) Common.ExceptionExtensions.RaiseTaggedException(this,"Invalid Time Description");

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
                    Common.ExceptionExtensions.RaiseTaggedException(this,"Invalid Repeat Time", ex);
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
            string result = TimeDescriptionType.ToString() + Sdp.SessionDescription.EqualsSign.ToString() + SessionStartTime.ToString() + SessionDescription.Space + SessionStopTime + SessionDescription.CRLF;
            foreach (string repeatTime in RepeatTimes)
                result += RepeatTimeType.ToString() + Sdp.SessionDescription.EqualsSign.ToString() + repeatTime + SessionDescription.CRLF;
            return result;
        }

        public override string ToString()
        {
            return ToString(null);
        }
    }

    public static class TimeDescriptionExtensions
    {

    }

    //public class SessionAnnouncement
    //{
    //    /*
    //     announcement =        proto-version
    //                           origin-field
    //                           session-name-field
    //                           information-field
    //                           uri-field
    //                           email-fields
    //                           phone-fields
    //                           connection-field
    //                           bandwidth-fields
    //                           time-fields
    //                           key-field
    //                           attribute-fields
    //                           media-descriptions
    //     */
    //}

    /// <summary>
    /// Low level class for dealing with Sdp lines with a format of 'X=V{st:sv0,sv1;svN}'    
    /// </summary>
    /// <remarks>Should use byte[] and should have Encoding as a property</remarks>
    public class SessionDescriptionLine
    {
        #region Statics

        public static SessionDescriptionLine Parse(string[] sdpLines, ref int index)
        {
            string sdpLine = sdpLines[index] = sdpLines[index].Trim();

            if (sdpLine.Length <= 2) return null;
            else if (sdpLine[1] != SessionDescription.EqualsSign) return null;

            char type = sdpLine[0];

            //Invalid Line
            if (type == default(char)) return null;

            try
            {
                switch (type)
                {
                    case Media.Sdp.Lines.SessionVersionLine.VersionType: return new Media.Sdp.Lines.SessionVersionLine(sdpLines, ref index);
                    case Media.Sdp.Lines.SessionOriginatorLine.OriginatorType: return new Media.Sdp.Lines.SessionOriginatorLine(sdpLines, ref index);
                    case Media.Sdp.Lines.SessionNameLine.NameType: return new Media.Sdp.Lines.SessionNameLine(sdpLines, ref index);
                    case Media.Sdp.Lines.SessionConnectionLine.ConnectionType: return new Media.Sdp.Lines.SessionConnectionLine(sdpLines, ref index);
                    case Media.Sdp.Lines.SessionUriLine.LocationType: return new Media.Sdp.Lines.SessionUriLine(sdpLines, ref index);
                    case Media.Sdp.Lines.SessionEmailLine.EmailType: return new Media.Sdp.Lines.SessionEmailLine(sdpLines, ref index);
                    case Media.Sdp.Lines.SessionPhoneLine.PhoneType: return new Media.Sdp.Lines.SessionPhoneLine(sdpLines, ref index);
                    case 'z': //TimeZone Information
                    case Sdp.SessionDescription.AttributeType: //Attribute
                    case Sdp.SessionDescription.BandwidthType: //Bandwidth
                    default:
                        {
                            ++index;
                            return new SessionDescriptionLine(sdpLine);
                        }
                }
            }
            catch
            {
                throw;
            }
        }

        public static bool TryParse(string[] sdpLines, ref int index, out SessionDescriptionLine result)
        {
            try
            {
                result = Parse(sdpLines, ref index);

                return result != null;
            }
            catch
            {
                result = null;

                return false;
            }
        }

        #endregion

        static char[] ValueSplit = new char[] { ';' };

        public readonly char Type;
        
        protected List<string> m_Parts;

        public System.Collections.ObjectModel.ReadOnlyCollection<string> Parts { get { return m_Parts.AsReadOnly(); } }

        /// <summary>
        /// Calculates the length in bytes of this line.
        /// </summary>
        public int Length
        {
            //Each part gets a type, =, all parts are joined with ';' and lines are ended with `\r\n\`.
            get { return 2 + m_Parts.Sum(p => p.Length) + ( m_Parts.Count > 0 ? m_Parts.Count - 1 : 0) + 2; }
        }

        internal string GetPart(int index) { return m_Parts.Count > index ? m_Parts[index] : string.Empty; }

        internal void SetPart(int index, string value) { if(m_Parts.Count > index) m_Parts[index] = value; }

        internal void EnsureParts(int count)
        {
            while (m_Parts.Count < count) m_Parts.Add(string.Empty);
        }

        public SessionDescriptionLine(SessionDescriptionLine other)
        {
            m_Parts = other.m_Parts;
            Type = other.Type;
        }

        /// <summary>
        /// Constructs a new SessionDescriptionLine with the given type
        /// <param name="type">The type of the line</param>
        public SessionDescriptionLine(char type, int partCount = 0)
        {
            m_Parts = new List<string>(partCount);
            EnsureParts(partCount);
            Type = type;
        }

        /// <summary>
        /// Parses and creates a SessionDescriptionLine from the given line
        /// </summary>
        /// <param name="line">The line from a SessionDescription</param>
        public SessionDescriptionLine(string line)
        {
            if (line.Length < 2 || line[1] != SessionDescription.EqualsSign) Common.ExceptionExtensions.RaiseTaggedException(this,"Invalid SessionDescriptionLine: \"" + line + "\"");

            Type = char.ToLower(line[0]);

            //Two types 
            //a=<flag>
            //a=<name>:<value> where value = {...,...,...;x;y;z}

            m_Parts = new List<string>(line.Remove(0, 2).Split(ValueSplit));
        }

        /// <summary>
        /// The string representation of the SessionDescriptionLine including the required new lines.
        /// </summary>
        /// <returns>The string representation of the SessionDescriptionLine including the required new lines.</returns>
        public override string ToString()
        {
            return Type.ToString() + SessionDescription.EqualsSign + string.Join(SessionDescription.SemiColon.ToString(), m_Parts.ToArray()) + SessionDescription.CRLF;
        }

      
    }

    //Public? TryRegisterLineImplementation, TypeCollection

    #region Internal Line Types
   
    namespace /*Media.Sdp.*/Lines
    {
        internal class SessionVersionLine : SessionDescriptionLine
        {
            internal const char VersionType = 'v';

            public SessionVersionLine(SessionDescriptionLine line)
                :base(line)
            {
                if (Type != VersionType) throw new InvalidOperationException("Not a version line");
            }

            public int Version
            {
                get
                {
                    return m_Parts.Count > 0 ? int.Parse(m_Parts[0], System.Globalization.CultureInfo.InvariantCulture) : 0;
                }
                set
                {
                    m_Parts.Clear(); 
                    m_Parts.Add(value.ToString());
                }
            }

            public SessionVersionLine(int version)
                : base(VersionType)
            {
                Version = version;
            }

            public SessionVersionLine(string[] sdpLines, ref int index)
                : base(VersionType)
            {
                try
                {
                    string sdpLine = sdpLines[index++].Trim();

                    if (sdpLine[0] != VersionType) Common.ExceptionExtensions.RaiseTaggedException(this, "Invalid Version Line");

                    sdpLine = SessionDescription.TrimLineValue(sdpLine.Substring(2));

                    m_Parts.Add(sdpLine);
                }
                catch
                {
                    throw;
                }
            }

        }

        internal class SessionOriginatorLine : SessionDescriptionLine
        {
            internal const char OriginatorType = 'o';

            public string Username { get { return GetPart(0); } set { SetPart(0, value); } }
            
            public string SessionId { get { return GetPart(1); } set { SetPart(1, value); } }

            public ulong SessionVersion
            {
                get
                {
                    string part = GetPart(2); 
                    return false == string.IsNullOrWhiteSpace(part) ? ulong.Parse(part, System.Globalization.CultureInfo.InvariantCulture) : 0;
                }
                set { SetPart(2, value.ToString()); }
            }
            
            public string NetworkType { get { return GetPart(3); } set { SetPart(3, value); } }
            
            public string AddressType { get { return GetPart(4); } set { SetPart(4, value); } }
            
            public string Address { get { return GetPart(5); } set { SetPart(5, value); } }

            public SessionOriginatorLine()
                : base(OriginatorType)
            {
                while (m_Parts.Count < 6) m_Parts.Add(string.Empty);
                Username = string.Empty;
                SessionVersion = 1;
            }

            public SessionOriginatorLine(SessionDescriptionLine line)
                :base(line)
            {
                if (Type != OriginatorType) throw new InvalidOperationException("Not a originator line");
            }

            public SessionOriginatorLine(string owner)
                : this()
            {

                if (string.IsNullOrWhiteSpace(owner)) m_Parts = new List<string>();
                else if (owner[0] != OriginatorType)
                {
                    m_Parts = new List<string>(owner.Split(SessionDescription.Space));
                }
                else m_Parts = new List<string>(owner.Substring(2).Replace(SessionDescription.CRLF, string.Empty).Split(SessionDescription.Space));

                if (m_Parts.Count < 6)
                {
                    EnsureParts(6);

                    //Make a new version if anything was added.
                    SessionVersion++;
                }
            }

            public SessionOriginatorLine(string[] sdpLines, ref int index)
                : this()
            {
                try
                {
                    string sdpLine = sdpLines[index++].Trim();

                    if (sdpLine[0] != OriginatorType) Common.ExceptionExtensions.RaiseTaggedException(this, "Invalid Owner");

                    sdpLine = SessionDescription.TrimLineValue(sdpLine.Substring(2));

                    m_Parts = new List<string>(sdpLine.Split(' '));

                    while (m_Parts.Count < 6) m_Parts.Add(string.Empty);
                }
                catch
                {
                    throw;
                }
            }

            public override string ToString()
            {
                return OriginatorType.ToString() + Media.Sdp.SessionDescription.EqualsSign + string.Join(SessionDescription.Space.ToString(), Username, SessionId, SessionVersion, NetworkType, AddressType, Address) + SessionDescription.CRLF;
            }

        }

        internal class SessionNameLine : SessionDescriptionLine
        {

            internal const char NameType = 's';

            public string SessionName { get { return m_Parts.Count > 0 ? m_Parts[0] : string.Empty; } set { m_Parts.Clear(); m_Parts.Add(value); } }

            public SessionNameLine()
                : base(NameType)
            {

            }

            public SessionNameLine(SessionDescriptionLine line)
                :base(line)
            {
                if (Type != NameType) throw new InvalidOperationException("Not a name line");
            }

            public SessionNameLine(string sessionName)
                : this()
            {
                SessionName = sessionName;
            }

            public SessionNameLine(string[] sdpLines, ref int index)
                : this()
            {
                try
                {
                    string sdpLine = sdpLines[index++].Trim();

                    if (sdpLine[0] != NameType) Common.ExceptionExtensions.RaiseTaggedException(this, "Invalid Session Name");

                    sdpLine = SessionDescription.TrimLineValue(sdpLine.Substring(2));

                    m_Parts.Add(sdpLine);
                }
                catch
                {
                    throw;
                }
            }

            public override string ToString()
            {
                return NameType.ToString() + Media.Sdp.SessionDescription.EqualsSign + (string.IsNullOrEmpty(SessionName) ? string.Empty : SessionName) + SessionDescription.CRLF;
            }
        }

        internal class SessionPhoneLine : SessionDescriptionLine
        {

            internal const char PhoneType = 'p';

            public string PhoneNumber { get { return m_Parts.Count > 0 ? m_Parts[0] : string.Empty; } set { m_Parts.Clear(); m_Parts.Add(value); } }

            public SessionPhoneLine()
                : base(PhoneType)
            {

            }

            public SessionPhoneLine(SessionDescriptionLine line)
                :base(line)
            {
                if (Type != PhoneType) throw new InvalidOperationException("Not a phone line");
            }

            public SessionPhoneLine(string sessionName)
                : this()
            {
                PhoneNumber = sessionName;
            }

            public SessionPhoneLine(string[] sdpLines, ref int index)
                : this()
            {
                try
                {
                    string sdpLine = sdpLines[index++].Trim();

                    if (sdpLine[0] != PhoneType) Common.ExceptionExtensions.RaiseTaggedException(this, "Invalid PhoneNumber");

                    sdpLine = SessionDescription.TrimLineValue(sdpLine.Substring(2));

                    m_Parts.Add(sdpLine);
                }
                catch
                {
                    throw;
                }
            }

            public override string ToString()
            {
                return PhoneType.ToString() + Media.Sdp.SessionDescription.EqualsSign + (string.IsNullOrEmpty(PhoneNumber) ? string.Empty : PhoneNumber) + SessionDescription.CRLF;
            }
        }

        internal class SessionEmailLine : SessionDescriptionLine
        {
            internal const char EmailType = 'e';

            public string Email { get { return m_Parts.Count > 0 ? m_Parts[0] : string.Empty; } set { m_Parts.Clear(); m_Parts.Add(value); } }

            public SessionEmailLine()
                : base(EmailType)
            {

            }

            public SessionEmailLine(SessionDescriptionLine line)
                :base(line)
            {
                if (Type != EmailType) throw new InvalidOperationException("Not a email line");
            }

            public SessionEmailLine(string sessionName)
                : this()
            {
                Email = sessionName;
            }

            public SessionEmailLine(string[] sdpLines, ref int index)
                : this()
            {
                try
                {
                    string sdpLine = sdpLines[index++].Trim();

                    if (sdpLine[0] != EmailType) Common.ExceptionExtensions.RaiseTaggedException(this, "Invalid Email");

                    sdpLine = SessionDescription.TrimLineValue(sdpLine.Substring(2));

                    m_Parts.Add(sdpLine);
                }
                catch
                {
                    throw;
                }
            }

            public override string ToString()
            {
                return EmailType.ToString() + Media.Sdp.SessionDescription.EqualsSign + (string.IsNullOrEmpty(Email) ? string.Empty : Email) + SessionDescription.CRLF;
            }
        }

        internal class SessionUriLine : SessionDescriptionLine
        {

            internal const char LocationType = 'u';

            public Uri Location
            {
                get
                {
                    Uri result;

                    //UriDecode?
                    Uri.TryCreate(m_Parts[0], UriKind.RelativeOrAbsolute, out result);

                    return result;
                }
                set { m_Parts.Clear(); m_Parts.Add(value.ToString()); }
            }

            public SessionUriLine()
                : base(LocationType)
            {
            }

            public SessionUriLine(SessionDescriptionLine line)
                :base(line)
            {
                if (Type != LocationType) throw new InvalidOperationException("Not a uri line");
            }

            public SessionUriLine(string uri)
                : this()
            {
                try
                {
                    Location = new Uri(uri);
                }
                catch
                {
                    throw;
                }
            }

            public SessionUriLine(Uri uri)
                : this()
            {
                Location = uri;
            }

            public SessionUriLine(string[] sdpLines, ref int index)
                : this()
            {
                try
                {
                    string sdpLine = sdpLines[index++].Trim();

                    if (sdpLine[0] != LocationType) Common.ExceptionExtensions.RaiseTaggedException(this, "Invalid Uri");

                    sdpLine = SessionDescription.TrimLineValue(sdpLine.Substring(2));

                    m_Parts.Add(sdpLine);
                }
                catch
                {
                    throw;
                }
            }

        }

        internal class SessionConnectionLine : SessionDescriptionLine
        {

            internal const char ConnectionType = 'c';

            internal string NetworkType { get { return GetPart(0); } set { SetPart(0, value); } }
            internal string AddressType { get { return GetPart(1); } set { SetPart(1, value); } }
            internal string Address { get { return GetPart(2); } set { SetPart(2, value); } }

            internal bool MultipleAddresses
            {
                get
                {
                    return Address.Contains('/');
                }
            }

            public string IPAddress
            {
                get
                {
                    if (false == string.IsNullOrWhiteSpace(Address))
                    {
                        var parts = Address.Split(SessionDescription.SlashSplit, 2);
                        return parts.First();
                    }
                    return null;
                }
            }

            

            public int? Hops
            {
                get
                {
                    if (!string.IsNullOrWhiteSpace(Address))
                    {
                        var parts = Address.Split(SessionDescription.SlashSplit, 3);
                        if (parts.Length > 2)
                        {
                            return int.Parse(parts.Skip(1).First(), System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture);
                        }
                    }
                    return null;
                }
            }

            public int? Ports
            {
                get
                {
                    if (!string.IsNullOrWhiteSpace(Address)) 
                    {
                        var parts = Address.Split(SessionDescription.SlashSplit, 3);
                        if (parts.Length > 2)
                        {
                            return int.Parse(parts.Last(), System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture);
                        }
                    }
                    return null;
                }
            }

            public SessionConnectionLine(SessionDescriptionLine line)
                :base(line)
            {
                if (Type != ConnectionType) throw new InvalidOperationException("Not a connection line");

                if (m_Parts.Count == 1) m_Parts = new List<string>(m_Parts[0].Split(SessionDescription.Space));
            }


            public SessionConnectionLine()
                : base(ConnectionType, 3)
            {

            }

            public SessionConnectionLine(string[] sdpLines, ref int index)
                : this()
            {
                try
                {
                    string sdpLine = sdpLines[index++].Trim();

                    if (sdpLine[0] != ConnectionType) Common.ExceptionExtensions.RaiseTaggedException(this, "Invalid Session Connection Line");

                    sdpLine = SessionDescription.TrimLineValue(sdpLine.Substring(2));

                    m_Parts.Add(sdpLine);

                    m_Parts = new List<string>(sdpLine.Split(SessionDescription.Space));
                }
                catch
                {
                    throw;
                }
            }

            public override string ToString()
            {
                return ConnectionType.ToString() + Media.Sdp.SessionDescription.EqualsSign + string.Join(SessionDescription.Space.ToString(), NetworkType, AddressType, Address) + SessionDescription.CRLF;
            }
        }
    }

    #endregion
}
