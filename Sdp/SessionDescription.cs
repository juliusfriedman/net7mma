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
    #region SessionDescription

    /// <summary>
    /// Provides facilities for parsing and creating SessionDescription data
    /// http://en.wikipedia.org/wiki/Session_Description_Protocol
    /// http://tools.ietf.org/html/rfc4566
    /// </summary>
    /// 
    ///https://msdn.microsoft.com/en-us/library/bb758954(v=office.13).aspx
    public class SessionDescription : Common.BaseDisposable, IEnumerable<SessionDescriptionLine>
    {
        #region Statics

        public const string MimeType = "application/sdp";

        internal const char EqualsSign = (char)Common.ASCII.EqualsSign,
            HyphenSign = (char)Common.ASCII.HyphenSign, SemiColon = (char)Common.ASCII.SemiColon,
            Colon = (char)Common.ASCII.Colon, Space = (char)Common.ASCII.Space,
            ForwardSlash = (char)Common.ASCII.ForwardSlash,
            Asterisk = (char)Common.ASCII.Asterisk,
            LineFeed = (char)Common.ASCII.LineFeed,
            NewLine = (char)Common.ASCII.NewLine;

        internal static string
            ForwardSlashString = new string(ForwardSlash, 1),
            SpaceString = new string(Space, 1),
            WildcardString = new string(Asterisk, 1),
            LineFeedString = new string(LineFeed, 1), 
            CarriageReturnString = new string(NewLine, 1), 
            NewLineString = CarriageReturnString + LineFeedString;

        internal static char[] SpaceSplit = new char[] { Space },
            SlashSplit = new char[] { ForwardSlash },
            SemiColonSplit = new char[] { SemiColon };
                             //CRSPlit, LFSplit...
        internal static string[] ColonSplit = new string[] { Colon.ToString() }, CRLFSplit = new string[] { NewLineString };

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

            type = Media.Common.Extensions.String.StringExtensions.UnknownString;
            start = TimeSpan.Zero;
            end = Media.Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan;

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

        //Typed Line would call ParseTime with each part

        public static TimeSpan ParseTime(string time)
        {
            TimeSpan result = TimeSpan.Zero;

            if (string.IsNullOrWhiteSpace(time)) return result;

            time = time.Trim();

            double temp;

            int tokenLength;

            foreach (string token in time.Split(Sdp.SessionDescription.Space))
            {
                //Don't process null tokens
                if (string.IsNullOrWhiteSpace(token)) continue;

                //Cache the token.Length
                tokenLength = token.Length - 1;

                //Determine if any specifier was used to convey the units.
                switch (char.ToLower(token[tokenLength]))
                {
                    //Todo, Type specifiers should be defined in some constant grammar
                    case 'd':
                        {
                            if (double.TryParse(token.Substring(0, tokenLength), out temp))
                            {
                                result = result.Add(TimeSpan.FromDays(temp));
                            }

                            continue;
                        }
                    case 'h':
                        {
                            if (double.TryParse(token.Substring(0, tokenLength), out temp))
                            {
                                result = result.Add(TimeSpan.FromHours(temp));
                            }

                            continue;
                        }
                    case 'm':
                        {
                            if (double.TryParse(token.Substring(0, tokenLength), out temp))
                            {
                                result = result.Add(TimeSpan.FromMinutes(temp));
                            }

                            continue;
                        }
                    case 's':
                        {
                            if (double.TryParse(token.Substring(0, tokenLength), out temp))
                            {
                                result = result.Add(TimeSpan.FromSeconds(temp));
                            }

                            continue;
                        }
                    default:
                        {
                            //Assume seconds
                            if (double.TryParse(token, out temp))
                            {
                                result = result.Add(TimeSpan.FromSeconds(temp));
                            }

                            continue;
                        }
                }
            }

            return result;
        }

        #endregion

        #region Fields

        //Should be done in constructor of a new 
            //Todo, could allow a local dictionary where certain types are cached.
        //Todo, check if readonly is applicable.
        internal protected Media.Sdp.Lines.SessionVersionLine m_SessionVersionLine;
        internal protected Media.Sdp.Lines.SessionOriginLine m_OriginatorLine;
        internal protected Media.Sdp.Lines.SessionNameLine m_NameLine;
        
        internal protected List<MediaDescription> m_MediaDescriptions = new List<MediaDescription>();
        internal protected List<TimeDescription> m_TimeDescriptions = new List<TimeDescription>();
        internal protected List<SessionDescriptionLine> m_Lines = new List<SessionDescriptionLine>();

        System.Threading.ManualResetEventSlim m_Update = new System.Threading.ManualResetEventSlim(true);

        System.Threading.CancellationTokenSource m_UpdateTokenSource = new System.Threading.CancellationTokenSource();

        #endregion

        #region Properties Backed With Fields

        /// <summary>
        /// Gets or Sets the version as indicated on the `v=` line. (-1 if not present)
        /// </summary>
        public int SessionDescriptionVersion
        {
            get { return m_SessionVersionLine == null ? -1 : m_SessionVersionLine.Version; }
            private set
            {
                if (UnderModification) return;

                if (m_SessionVersionLine == null || value != m_SessionVersionLine.Version)
                {
                    var token = BeginUpdate();

                    m_SessionVersionLine = new Lines.SessionVersionLine(value);

                    EndUpdate(token, DocumentVersion != 0);
                }
            }
        }

        /// <summary>
        /// Gets or sets the value of the 'o=' line.
        /// </summary>
        public string OriginatorAndSessionIdentifier
        {
            get { return m_OriginatorLine.ToString(); }
            set
            {
                if (UnderModification) return;

                if (string.IsNullOrWhiteSpace(value)) throw new InvalidOperationException("The SessionOriginatorLine is required to have a non-null and non-empty value.");

                bool hadValueWithSetVersion = m_OriginatorLine != null && m_OriginatorLine.SessionVersion != 0;

                if (hadValueWithSetVersion && string.Compare(value, m_OriginatorLine.ToString(), StringComparison.InvariantCultureIgnoreCase) == 0) return;

                var token = BeginUpdate();

                m_OriginatorLine = new Media.Sdp.Lines.SessionOriginLine(value);

                EndUpdate(token, hadValueWithSetVersion);
            }
        }

        /// <summary>
        /// Gets or sets the value of the 's=' line.
        /// When set the version is updated if the value is not equal to the existing value.
        /// </summary>
        public string SessionName
        {
            get { return m_NameLine.SessionName; }
            set
            {
                if (UnderModification) return;

                if (m_NameLine == null || string.Compare(value, m_NameLine.SessionName, StringComparison.InvariantCultureIgnoreCase) != 0)
                {
                    var token = BeginUpdate();

                    m_NameLine = new Lines.SessionNameLine(value);

                    EndUpdate(token, DocumentVersion != 0);
                }
            }
        }

        /// <summary>
        /// Gets or sets the value assoicated with the SessionId of this SessionDescription as indicated in the 'o=' line.
        /// When set the version is updated if the value is not equal to the existing value.
        /// </summary>
        public string SessionId
        {
            get
            {
                return m_OriginatorLine == null ? string.Empty : m_OriginatorLine.SessionId;
            }
            set
            {
                if (UnderModification) return;

                if (string.IsNullOrWhiteSpace(value)) throw new ArgumentNullException();

                if (string.Compare(value, m_OriginatorLine.SessionId, StringComparison.InvariantCultureIgnoreCase) == 0) return;

                var token = BeginUpdate();

                m_OriginatorLine.SessionId = value;

                EndUpdate(token, DocumentVersion != 0);
            }
        }

        public long DocumentVersion
        {
            get
            {
                return m_OriginatorLine == null ? 0 : m_OriginatorLine.SessionVersion;
            }
            set
            {
                if (UnderModification) return;

                var token = BeginUpdate();

                m_OriginatorLine.SessionVersion = value;

                EndUpdate(token, false);
            }
        }

        public int TimeDescriptionsCount { get { return m_TimeDescriptions.Count; } }

        public IEnumerable<TimeDescription> TimeDescriptions
        {
            get { return m_TimeDescriptions; }
            set
            {
                if (UnderModification) return;

                var token = BeginUpdate();

                m_TimeDescriptions.Clear();

                m_TimeDescriptions.AddRange(value);

                EndUpdate(token, true);
            }
        }

        public int MediaDescriptionsCount { get { return m_MediaDescriptions.Count; } }

        public IEnumerable<MediaDescription> MediaDescriptions
        {
            get { return m_MediaDescriptions; }
            set
            {
                if (UnderModification) return;

                var token = BeginUpdate();

                m_MediaDescriptions.Clear();

                m_MediaDescriptions.AddRange(value);

                EndUpdate(token, true);
            }
        }

        public Sdp.Lines.SessionVersionLine SessionVersionLine
        {
            get { return m_SessionVersionLine; }
            set
            {
                if (UnderModification) return;

                var token = BeginUpdate();

                m_SessionVersionLine = value;

                EndUpdate(token, true);
            }
        }

        public Sdp.Lines.SessionOriginLine SessionOriginatorLine
        {
            get { return m_OriginatorLine; }
            set
            {
                if (UnderModification) return;

                var token = BeginUpdate();

                m_OriginatorLine = value;

                EndUpdate(token, true);
            }
        }

        public Sdp.Lines.SessionNameLine SessionNameLine
        {
            get { return m_NameLine; }
            set
            {
                if (UnderModification) return;

                var token = BeginUpdate();
                
                m_NameLine = value;

                EndUpdate(token, true);

            }
        }

        /// <summary>
        /// Gets the lines assoicated with the Session level attributes which are lines other than the o, i or c lines.
        /// </summary>
        public IEnumerable<SessionDescriptionLine> Lines
        {
            get
            {
                return ((IEnumerable<SessionDescriptionLine>)this);
            }
            internal protected set
            {
                if (UnderModification) return;

                var token = BeginUpdate();

                m_Lines = value.ToList();

                EndUpdate(token, true);
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Indicates if the Session Description is being modified
        /// </summary>
        public bool UnderModification
        {
            get { return false == m_Update.IsSet || m_UpdateTokenSource.IsCancellationRequested; }
        }

        public SessionDescriptionLine ConnectionLine
        {
            get
            {
                return Lines.FirstOrDefault(l => l.Type == Sdp.Lines.SessionConnectionLine.ConnectionType);
            }
            set
            {
                if (value != null && value.Type != Sdp.Lines.SessionConnectionLine.ConnectionType)
                {
                    throw new InvalidOperationException("The ConnectionList must be a ConnectionLine");
                }
                
                if (UnderModification) return;

                Remove(ConnectionLine);

                var token = BeginUpdate();

                Add(value);

                EndUpdate(token, true);
            }
        }

        public SessionDescriptionLine RangeLine
        {
            get
            {
                return Lines.FirstOrDefault(l => l.Type == Sdp.Lines.SessionAttributeLine.AttributeType && l.m_Parts.Count > 0 && l.m_Parts[0].StartsWith("range:", StringComparison.InvariantCultureIgnoreCase));
            }
            set
            {
                if (UnderModification) return;

                Remove(RangeLine);

                var token = BeginUpdate();

                Add(value);

                EndUpdate(token, true);
            }
        }

        public SessionDescriptionLine ControlLine
        {
            get
            {
                return Lines.FirstOrDefault(l => l.Type == Sdp.Lines.SessionAttributeLine.AttributeType && l.m_Parts.Count > 0 && l.m_Parts[0].StartsWith("control:", StringComparison.InvariantCultureIgnoreCase));
            }
            set
            {
                if (UnderModification) return;

                Remove(ControlLine);

                var token = BeginUpdate();

                Add(value);

                EndUpdate(token, true);
            }
        }

        public IEnumerable<SessionDescriptionLine> AttributeLines
        {
            get
            {
                return Lines.Where(l => l.Type == Sdp.Lines.SessionAttributeLine.AttributeType);
            }
        }

        public IEnumerable<SessionDescriptionLine> BandwidthLines
        {
            get
            {
                return Lines.Where(l => l.Type == Sdp.Lines.SessionBandwidthLine.BandwidthType);
            }
        }

        /// <summary>
        /// Calculates the length in bytes of this SessionDescription.
        /// </summary>
        public int Length
        {
            get
            {
                return (m_OriginatorLine == null ? 0 : m_OriginatorLine.Length) +
                    (m_NameLine == null ? 0 : m_NameLine.Length) +
                    (m_SessionVersionLine == null ? 0 : m_SessionVersionLine.Length) +
                    m_Lines.Sum(l => l.Length) +
                    m_MediaDescriptions.Sum(md => md.Length) +
                    m_TimeDescriptions.Sum(td => td.Length);
            }
        }

        #endregion

        #region Constructor

        public SessionDescription(int version)
        {
            m_OriginatorLine = new Lines.SessionOriginLine();

            m_NameLine = new Sdp.Lines.SessionNameLine();

            SessionDescriptionVersion = version;
        }

        public SessionDescription(string originatorString, string sessionName)
            :this(0)
        {
            OriginatorAndSessionIdentifier = originatorString;

            m_NameLine = new Lines.SessionNameLine(sessionName); 
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

            m_NameLine = new Lines.SessionNameLine(sessionName); 
        }

        /// <summary>
        /// Constructs a SessionDescription from the given contents of a Session Description Protocol message
        /// </summary>
        /// <param name="sdpContents">The Session Description Protocol usually recieved in the Describe request of a RtspClient</param>
        public SessionDescription(string sdpContents)
        {
            string[] lines = sdpContents.Split(SessionDescription.CRLFSplit, StringSplitOptions.RemoveEmptyEntries);

            if (lines.Length < 3) Media.Common.Extensions.Exception.ExceptionExtensions.RaiseTaggedException(lines, "Invalid Session Description, At least 3 lines should be found.");

            //Parse remaining optional entries
            for (int lineIndex = 0, endIndex = lines.Length; lineIndex < endIndex; /*Advancement of the loop controlled by the corrsponding Lines via ref*/)
            {
                string line = lines[lineIndex].Trim();

                //Todo, use a Dictionary and allow registration.

                //Determine if there is a specialization, also performed in SessionDescriptionLine.TryParse
                switch (line[0])
                {
                    case Media.Sdp.Lines.SessionVersionLine.VersionType:
                        {
                            m_SessionVersionLine = new Media.Sdp.Lines.SessionVersionLine(lines, ref lineIndex);
                            continue;
                        }
                    case Media.Sdp.Lines.SessionOriginLine.OriginType:
                        {
                            m_OriginatorLine = new Media.Sdp.Lines.SessionOriginLine(lines, ref lineIndex);
                            continue;
                        }
                    case Media.Sdp.Lines.SessionNameLine.NameType:
                        {
                            m_NameLine = new Media.Sdp.Lines.SessionNameLine(lines, ref lineIndex);
                            continue;
                        }
                    case TimeDescription.TimeDescriptionType:
                        {
                            m_TimeDescriptions.Add(new TimeDescription(lines, ref lineIndex));
                            continue;
                        }
                    case MediaDescription.MediaDescriptionLineType:
                        {
                            m_MediaDescriptions.Add(new MediaDescription(lines, ref lineIndex));
                            continue;
                        }
                    //case Media.Sdp.Lines.SessionAttributeLine.AttributeType:
                    //    {
                    //        m_Lines.Add(new Media.Sdp.Lines.SessionAttributeLine(lines, ref lineIndex));
                    //        continue;
                    //    }
                    //case Media.Sdp.Lines.SessionBandwidthLine.BandwidthType:
                    //    {
                    //        m_Lines.Add(new Media.Sdp.Lines.SessionBandwidthLine(lines, ref lineIndex));
                    //        continue;
                    //    }
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
            SessionDescriptionVersion = other.SessionDescriptionVersion;

            OriginatorAndSessionIdentifier = other.OriginatorAndSessionIdentifier;

            m_NameLine = other.m_NameLine;

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

        public TimeDescription GetTimeDescription(int index) { return m_TimeDescriptions[index]; }

        public MediaDescription GetMediaDescription(int index) { return m_MediaDescriptions[index]; }       

        //public SessionDescriptionLine GetLine(int index)
        //{
        //    //Some lines are backed by properties
        //}

        public void Add(MediaDescription mediaDescription, bool updateVersion = true)
        {
            if (UnderModification || mediaDescription == null) return;

            var token = BeginUpdate();

            m_MediaDescriptions.Add(mediaDescription);

            EndUpdate(token, updateVersion);
        }

        public void Add(TimeDescription timeDescription, bool updateVersion = true)
        {
            if (UnderModification || timeDescription == null) return;

            var token = BeginUpdate();

            m_TimeDescriptions.Add(timeDescription);

            EndUpdate(token, updateVersion);
        }

        public void Add(SessionDescriptionLine line, bool updateVersion = true)
        {
            if (UnderModification || line == null) return;

            var token = BeginUpdate();

            switch (line.Type)
            {
                case Sdp.Lines.SessionVersionLine.VersionType:
                    m_SessionVersionLine = new Lines.SessionVersionLine(line);
                    break;
                case Sdp.Lines.SessionOriginLine.OriginType:
                    m_OriginatorLine = new Sdp.Lines.SessionOriginLine(line);
                    break;
                case Sdp.Lines.SessionNameLine.NameType:
                    m_NameLine = new Sdp.Lines.SessionNameLine(line);
                    break;
                default:
                    m_Lines.Add(line);
                    break;
            }
            
            EndUpdate(token, updateVersion);
        }

        public bool Remove(SessionDescriptionLine line, bool updateVersion = true)
        {
            if (UnderModification || line == null) return false;

            var token = BeginUpdate();

            bool result = false;

            switch (line.Type)
            {
                case Sdp.Lines.SessionVersionLine.VersionType:
                    if (line == m_SessionVersionLine)
                    {
                        m_SessionVersionLine = null;
                        result = true;
                    }
                    break;
                case Sdp.Lines.SessionOriginLine.OriginType:
                    if (line == m_OriginatorLine)
                    {
                        m_OriginatorLine = null;
                        result = true;
                    }
                    break;
                case Sdp.Lines.SessionNameLine.NameType:
                    if (line == m_OriginatorLine)
                    {
                        m_NameLine = null;
                        result = true;
                    }
                    break;
                    //Handle remove of Time Description and its constituents
                ////case Sdp.MediaDescription.MediaDescriptionType:
                ////    {
                    //Handle remove of Media Description and its constituents
                ////        foreach (MediaDescription md in MediaDescriptions) if (result = md.Remove(line)) break;
                ////    }                    
                ////    break;
                ////default:
                ////    {
                ////        result = m_Lines.Remove(line);
                ////    }
                ////    break;
            }

            if (false == result)
            {
                result = m_Lines.Remove(line);
            }

            if (false == result)
            {
                foreach (MediaDescription md in MediaDescriptions) if (result = md.Remove(line)) break;
            }

            EndUpdate(token, updateVersion && result);

            return result;
        }

        public bool Remove(TimeDescription timeDescription, bool updateVersion = true)
        {
            if (UnderModification || timeDescription == null) return false;

            var token = BeginUpdate();

            bool result = m_TimeDescriptions.Remove(timeDescription);

            EndUpdate(token, updateVersion && result);

            return result;
        }

        public bool Remove(MediaDescription mediaDescription, bool updateVersion = true)
        {
            if (UnderModification || mediaDescription == null) return false;

            var token = BeginUpdate();

            bool result = m_MediaDescriptions.Remove(mediaDescription);
            
            EndUpdate(token, updateVersion);

            return result;
        }

        //public void RemoveLine(int index, bool updateVersion = true)
        //{
        //    if (UnderModification) return;

        //    //Should give backed lines virtual/indirect index?

        //    var token = BeginUpdate();

        //    m_Lines.RemoveAt(index);

        //    EndUpdate(token, updateVersion);
        //}

        public void RemoveMediaDescription(int index, bool updateVersion = true)
        {
            if (UnderModification) return;

            var token = BeginUpdate();

            m_MediaDescriptions.RemoveAt(index);

            EndUpdate(token, updateVersion);
        }

        public void RemoveTimeDescription(int index, bool updateVersion = true)
        {
            if (UnderModification) return;

            var token = BeginUpdate();

            m_TimeDescriptions.RemoveAt(index);

            EndUpdate(token, updateVersion);
        }

        public void UpdateVersion(System.Threading.CancellationToken token)
        {

            if (token != null && token != m_UpdateTokenSource.Token) throw new InvalidOperationException("Must obtain the CancellationToken from a call to BeginUpdate.");

            if(false == token.IsCancellationRequested
                &&
            m_OriginatorLine != null)
            {
                ++m_OriginatorLine.SessionVersion;
            }
        }

        /// <summary>
        /// Allows stateful control of the modifications of the Session Description by blocking other updates.
        /// If called when <see cref="UnderModification"/> the call will block until the update can proceed.
        /// </summary>
        /// <returns>The <see cref="System.Threading.CancellationToken"/> which can be used to cancel the update started</returns>
        public System.Threading.CancellationToken BeginUpdate()
        {
            CheckDisposed();

            if (System.Threading.WaitHandle.SignalAndWait(m_UpdateTokenSource.Token.WaitHandle, m_Update.WaitHandle))
            {
                m_Update.Reset();
            }

            return m_UpdateTokenSource.Token;
        }

        //public System.Threading.CancellationToken BeginUpdate(int x, bool e)
        //{
        //    CheckDisposed();

        //    if (System.Threading.WaitHandle.SignalAndWait(m_UpdateTokenSource.Token.WaitHandle, m_Update.WaitHandle, x, e))
        //    {
        //        m_Update.Reset();
        //    }

        //    return m_UpdateTokenSource.Token;
        //}

        /// <summary>
        /// Ends a previous started update with <see cref="BeginUpdate"/>
        /// </summary>
        /// <param name="token">The token obtained to begin the update</param>
        public void EndUpdate(System.Threading.CancellationToken token, bool updateVersion)
        {
            CheckDisposed();

            //Ensure a token
            if (token == null) return;

            //That came from out cancellation source
            if (token != m_UpdateTokenSource.Token) throw new InvalidOperationException("Must obtain the CancellationToken from a call to BeginUpdate.");

            // check for manually removed state or a call without an update..
            //if(m_Update.Wait(1, token)) { would check that the event was manually cleared... }

            // acknowledge cancellation 
            if (token.IsCancellationRequested) throw new OperationCanceledException(token);

            //if the version should be updated, then do it now.
            if (updateVersion) UpdateVersion(token);

            //Allow threads to modify
            m_Update.Set(); //To unblocked
        }

        #endregion

        #region Overrides

        public override string ToString()
        {
            StringBuilder buffer = new StringBuilder();

            if(m_SessionVersionLine != null) buffer.Append(m_SessionVersionLine.ToString());

            if (m_OriginatorLine != null) buffer.Append(m_OriginatorLine.ToString());

            if (m_NameLine != null) buffer.Append(m_NameLine.ToString());

            foreach (SessionDescriptionLine l in m_Lines.Where(l => l.Type != Sdp.Lines.SessionBandwidthLine.BandwidthType && l.Type != Sdp.Lines.SessionAttributeLine.AttributeType))
            {
                buffer.Append(l.ToString());
            }

            foreach (SessionDescriptionLine l in m_Lines.Where(l => l.Type == Sdp.Lines.SessionBandwidthLine.BandwidthType))
            {
                buffer.Append(l.ToString());
            }

            foreach (SessionDescriptionLine l in m_Lines.Where(l => l.Type == Sdp.Lines.SessionAttributeLine.AttributeType))
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

            if (false == ShouldDispose) return;

            m_SessionVersionLine = null;

            m_OriginatorLine = null;

            m_NameLine = null;

            //Dispose all

            m_MediaDescriptions.Clear();

            m_MediaDescriptions = null;

            m_TimeDescriptions.Clear();

            m_TimeDescriptions = null;

            m_Lines.Clear();

            m_Lines = null;
        }

        #endregion

        //Todo, allow the Enumerator to be changed via a property rather then forcing a subclass.

        public IEnumerator<SessionDescriptionLine> GetEnumerator()
        {
            if (m_SessionVersionLine != null) yield return m_SessionVersionLine;

            if (m_OriginatorLine != null) yield return m_OriginatorLine;

            if (m_NameLine != null) yield return m_NameLine;

            foreach (var line in m_Lines)
            {
                if (line == null) continue;

                yield return line;
            }

            foreach (var mediaDescription in MediaDescriptions)
            {
                foreach (var line in mediaDescription)
                {
                    //Choose if the types which already appear should be skipped...

                    yield return line;
                }
            }

            foreach (var timeDescription in TimeDescriptions)
            {
                foreach (var line in timeDescription)
                {
                    //Choose if the types which already appear should be skipped...

                    yield return line;
                }
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<SessionDescriptionLine>)this).GetEnumerator();
        }
    }

    public static class SessionDescriptionExtensions
    {
        
        public static bool SupportsAggregateMediaControl(this SessionDescription sdp, Uri baseUri = null)
        {
            Uri result;
            return SupportsAggregateMediaControl(sdp, out result, baseUri);
        }

        /// <summary>
        /// <see fref="https://tools.ietf.org/html/rfc2326#page-80">Use of SDP for RTSP Session Descriptions</see>
        /// In brief an the given <see cref="SessionDescription"/> must contain a <see cref="Sdp.Lines.ConnectionLine"/> in the <see cref="Sdp.MediaDescription"/>
        /// </summary>
        /// <param name="sdp"></param>
        /// <param name="controlUri"></param>
        /// <param name="baseUri"></param>
        /// <returns></returns>
        public static bool SupportsAggregateMediaControl(this SessionDescription sdp, out Uri controlUri, Uri baseUri = null)
        {
            controlUri = null;

            SessionDescriptionLine controlLine = sdp.ControlLine;

            //If there is a control line in the SDP it contains the URI used to setup and control the media
            if (controlLine == null) return false;

            //Get the control token
            string controlPart = controlLine.Parts.Where(p => p.Contains("control")).FirstOrDefault();

            //If there is a controlPart in the controlLine
            if (false == string.IsNullOrWhiteSpace(controlPart))
            {
                /*
                    If this attribute contains only an asterisk (*), then the URL is
                    treated as if it were an empty embedded URL, and thus inherits the
                    entire base URL.
                 */
                controlPart = controlPart.Split(Media.Sdp.SessionDescription.ColonSplit, 2, StringSplitOptions.RemoveEmptyEntries).Last();

                //if unqualified then there is no aggregate control.
                if (controlPart == SessionDescription.WildcardString && baseUri == null) return false;

                //The control uri may be in the control part

                //Try to parse it
                if (Uri.TryCreate(controlPart, UriKind.RelativeOrAbsolute, out controlUri))
                {

                    //If parsing suceeded then the result is true only if the controlUri is absolute
                    if (controlUri.IsAbsoluteUri) return true;

                }

                //Try to create a uri relative to the base uri given
                if (Uri.TryCreate(baseUri, controlUri, out controlUri))
                {
                    //If the operation succeeded then the result is true.
                    return true;
                }
            }

            //Another type of control line is present.
            return false;
        }

        //Naming is weird, this returns the logical 0 based index of the given description within the sessionDescription's property of the same type.

        //E.g. This index can be used in GetMediaDescription(index)
        public static int GetIndexFor(this SessionDescription sdp, MediaDescription md)
        {
            if (sdp == null || md == null) return -1;

            return sdp.m_MediaDescriptions.IndexOf(md);
        }

        //E.g. This index can be used in GetTimeDescription(index)
        public static int GetIndexFor(this SessionDescription sdp, TimeDescription td)
        {
            if (sdp == null || td == null) return -1;

            return sdp.m_TimeDescriptions.IndexOf(td);
        }

        //GetMediaDescriptionFor

        //GetTimeDescriptionFor

    }

    #endregion

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

    

    //Public? TryRegisterLineImplementation, TypeCollection
}
