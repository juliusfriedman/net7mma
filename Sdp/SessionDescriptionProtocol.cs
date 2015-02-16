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
    public class SessionDescription : Common.BaseDisposable, IEnumerable<SessionDescriptionLine>
    {
        #region Statics

        public const string MimeType = "application/sdp";

        public const char EqualsSign = (char)Common.ASCII.EqualsSign, 
            HyphenSign = (char)Common.ASCII.HyphenSign, SemiColon = (char)Common.ASCII.SemiColon, 
            Colon = (char)Common.ASCII.Colon, Space = (char)Common.ASCII.Space;

        public static string 
            ForwardSlashString = new string((char)Common.ASCII.ForwardSlash, 1),
            SpaceString = new string((char)Common.ASCII.Space, 1),
            WildcardString = new string((char)Common.ASCII.Asterisk, 1),
            LineFeedString = new string((char)Common.ASCII.LineFeed, 1), 
            CarriageReturnString = new string((char)Common.ASCII.NewLine, 1), 
            NewLine = CarriageReturnString + LineFeedString;

        internal static string[] ColonSplit = new string[] { Colon.ToString() }, CRLFSplit = new string[] { NewLine };

        internal static char[] SpaceSplit = new char[] { (char)Common.ASCII.Space },
            SlashSplit = new char[] { (char)Common.ASCII.ForwardSlash };

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
        Media.Sdp.Lines.SessionVersionLine m_SessionVersionLine;
        Media.Sdp.Lines.SessionOriginatorLine m_OriginatorLine;
        Media.Sdp.Lines.SessionNameLine m_NameLine;
        
        List<MediaDescription> m_MediaDescriptions = new List<MediaDescription>();
        List<TimeDescription> m_TimeDescriptions = new List<TimeDescription>();
        List<SessionDescriptionLine> m_Lines = new List<SessionDescriptionLine>();

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

                m_OriginatorLine = new Media.Sdp.Lines.SessionOriginatorLine(value);

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

        public IEnumerable<TimeDescription> TimeDescriptions
        {
            get { return m_TimeDescriptions.AsReadOnly(); }
            set
            {
                if (UnderModification) return;

                var token = BeginUpdate();

                m_TimeDescriptions = value.ToList();

                EndUpdate(token, true);
            }
        }

        public IEnumerable<MediaDescription> MediaDescriptions
        {
            get { return m_MediaDescriptions.AsReadOnly(); }
            set
            {
                if (UnderModification) return;

                var token = BeginUpdate();

                m_MediaDescriptions = value.ToList();

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

        public Sdp.Lines.SessionOriginatorLine SessionOriginatorLine
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

        #endregion

        #region Properties

        /// <summary>
        /// Indicates if the Session Description is being modified
        /// </summary>
        public bool UnderModification
        {
            get { return false == m_Update.IsSet || m_UpdateTokenSource.IsCancellationRequested; }
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
                return Lines.FirstOrDefault(l => l.Type == Sdp.Lines.SessionAttributeLine.AttributeType && l.Parts[0].StartsWith("range:", StringComparison.InvariantCultureIgnoreCase));
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
                return Lines.FirstOrDefault(l => l.Type == Sdp.Lines.SessionAttributeLine.AttributeType && l.Parts[0].StartsWith("control:", StringComparison.InvariantCultureIgnoreCase));
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
                var connectionLine = ConnectionLine;
                return (m_OriginatorLine == null ? 0 : m_OriginatorLine.Length) +
                    (m_NameLine == null ? 0 : m_NameLine.Length) +
                    (m_SessionVersionLine == null ? 0 : m_SessionVersionLine.Length) +
                    (connectionLine == null ? 0 : connectionLine.Length) +
                    m_Lines.Sum(l => l.Length) +
                    m_MediaDescriptions.Sum(md => md.Length) +
                    m_TimeDescriptions.Sum(td => td.Length);
            }
        }

        #endregion

        #region Constructor

        public SessionDescription(int version)
        {
            m_OriginatorLine = new Lines.SessionOriginatorLine();

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

            if (lines.Length < 3) Common.ExceptionExtensions.RaiseTaggedException(this, "Invalid Session Description");

            //Parse remaining optional entries
            for (int lineIndex = 0, endIndex = lines.Length; lineIndex < endIndex; /*Advancement of the loop controlled by the corrsponding Lines via ref*/)
            {
                string line = lines[lineIndex].Trim();

                switch (line[0])
                {
                    case Media.Sdp.Lines.SessionVersionLine.VersionType:
                        {
                            m_SessionVersionLine = new Media.Sdp.Lines.SessionVersionLine(lines, ref lineIndex);
                            continue;
                        }
                    case Media.Sdp.Lines.SessionOriginatorLine.OriginatorType:
                        {
                            m_OriginatorLine = new Media.Sdp.Lines.SessionOriginatorLine(lines, ref lineIndex);
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
                    case MediaDescription.MediaDescriptionType:
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
                case Sdp.Lines.SessionOriginatorLine.OriginatorType:
                    m_OriginatorLine = new Sdp.Lines.SessionOriginatorLine(line);
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
                case Sdp.Lines.SessionOriginatorLine.OriginatorType:
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

        public void RemoveLine(int index, bool updateVersion = true)
        {

            if (UnderModification) return;

            //Should give backed lines virtual index?

            var token = BeginUpdate();

            m_Lines.RemoveAt(index);

            EndUpdate(token, updateVersion);
        }

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

        #region Events

        //I can see the benefit of making a SessionDescription have events for

        //VersionChanged
        //etc..
        //Remarks?

        #endregion

        #region Overrides

        public override string ToString()
        {
            StringBuilder buffer = new StringBuilder();

            if(m_SessionVersionLine != null) buffer.Append(m_SessionVersionLine.ToString());

            if (m_OriginatorLine != null) buffer.Append(m_OriginatorLine.ToString());

            if (m_NameLine != null) buffer.Append(m_NameLine.ToString());

            var connectionLine = ConnectionLine;

            if (connectionLine != null) buffer.Append(connectionLine.ToString());

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

            m_SessionVersionLine = null;

            m_OriginatorLine = null;

            m_NameLine = null;

            m_MediaDescriptions.Clear();

            m_MediaDescriptions = null;

            m_TimeDescriptions.Clear();

            m_TimeDescriptions = null;

            m_Lines.Clear();

            m_Lines = null;
        }

        #endregion

        public IEnumerator<SessionDescriptionLine> GetEnumerator()
        {
            if (m_SessionVersionLine != null) yield return m_SessionVersionLine;

            if (m_OriginatorLine != null) yield return m_OriginatorLine;

            if (m_NameLine != null) yield return m_NameLine;

            foreach (var mediaDescription in MediaDescriptions)
            {
                foreach (var line in mediaDescription)
                {
                    //Choose if the types which already appear should be skipped...

                    yield return line;
                }
            }

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

    }

    /// <summary>
    /// Represents the MediaDescription in a Session Description.
    /// Parses and Creates.
    /// </summary>
    public class MediaDescription : Common.BaseDisposable, IEnumerable<SessionDescriptionLine>
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
        /// The MediaFormat of the MediaDescription
        /// </summary>
        public virtual string MediaFormat
        {
            get
            {
                if (string.IsNullOrWhiteSpace(MediaFormatString)) return MediaFormat = string.Join(SessionDescription.SpaceString, m_PayloadList);
                return MediaFormatString;
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

        public MediaDescription(string mediaDescription) : this(mediaDescription.Split(SessionDescription.CRLFSplit, StringSplitOptions.RemoveEmptyEntries), 0)
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
            this(sdpLines, ref index){}

        [CLSCompliant(false)]
        public MediaDescription(string[] sdpLines, ref int index)
        {
            string sdpLine = sdpLines[index++].Trim();

            if (false == sdpLine.StartsWith("m=")) Common.ExceptionExtensions.RaiseTaggedException(this,"Invalid Media Description");
            
            sdpLine = sdpLine.Replace("m=", string.Empty);

            string[] parts = sdpLine.Split(SessionDescription.SpaceSplit, 4);

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

            MediaFormat = parts[3];            

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

        public void Add(SessionDescriptionLine line)
        {
            if (line == null) return;
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
                        buffer.Append(MediaDescriptionType.ToString() + Sdp.SessionDescription.EqualsSign + string.Join(SessionDescription.Space.ToString(), MediaType, MediaPort.ToString() + ((char)Common.ASCII.ForwardSlash).ToString() + portSpecifier, MediaProtocol, MediaFormat) + SessionDescription.NewLine);

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

        #region Named Lines

        internal protected SessionDescriptionLine MediaDescriptionLine
        {
            get
            {
                return new SessionDescriptionLine(MediaDescriptionType, ((char)Common.ASCII.Space).ToString())
                {
                    MediaType.ToString(),
                    MediaPort.ToString(),
                    MediaProtocol,
                    MediaFormat
                };
            }
        }

        public SessionDescriptionLine ConnectionLine { get { return m_Lines.FirstOrDefault(l => l.Type == Sdp.Lines.SessionConnectionLine.ConnectionType); } }

        public SessionDescriptionLine RtpMapLine
        {
            get
            {
                return m_Lines.FirstOrDefault(l => l.Type == Sdp.Lines.SessionAttributeLine.AttributeType && l.Parts[0].StartsWith("rtpmap:", StringComparison.InvariantCultureIgnoreCase));
            }
        }

        public SessionDescriptionLine FmtpLine
        {
            get
            {
                return m_Lines.FirstOrDefault(l => l.Type == Sdp.Lines.SessionAttributeLine.AttributeType && l.Parts[0].StartsWith("fmtp:", StringComparison.InvariantCultureIgnoreCase));
            }
        }

        public SessionDescriptionLine RangeLine
        {
            get { return m_Lines.FirstOrDefault(l => l.Type == Sdp.Lines.SessionAttributeLine.AttributeType && l.Parts[0].StartsWith("range:", StringComparison.InvariantCultureIgnoreCase)); }
        }

        public SessionDescriptionLine ControlLine
        {
            get
            {
                return m_Lines.FirstOrDefault(l => l.Type == Sdp.Lines.SessionAttributeLine.AttributeType && l.Parts[0].StartsWith("control:", StringComparison.InvariantCultureIgnoreCase));
            }
        }

        #endregion

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
                    return new Uri(string.Join(SessionDescription.ForwardSlashString, source.OriginalString, controlUri.OriginalString));                    

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
    }

    /// <summary>
    /// Represents a TimeDescription with optional Repeat times.
    /// Parses and Creates.
    /// </summary>
    public class TimeDescription : Common.BaseDisposable, IEnumerable<SessionDescriptionLine>
    {

        public const char TimeDescriptionType = 't', RepeatTimeType = 'r';

        public double SessionStartTime { get; private set; }

        public double SessionStopTime { get; private set; }

        //public bool IsLive { get { return SessionStartTime == 0 } }

        //public bool IsContinious { get { return SessionStopTime <= 0; } }

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
    public class SessionDescriptionLine : IEnumerable<String>
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
                    case Sdp.Lines.SessionAttributeLine.AttributeType: //Attribute
                    case Sdp.Lines.SessionBandwidthLine.BandwidthType: //Bandwidth
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

        protected string m_Seperator = SessionDescription.SemiColon.ToString();
        
        protected List<string> m_Parts;

        public System.Collections.ObjectModel.ReadOnlyCollection<string> Parts { get { return m_Parts.AsReadOnly(); } }

        /// <summary>
        /// Calculates the length in bytes of this line.
        /// </summary>
        public int Length
        {
            //Each part gets a type, =, all parts are joined with 'm_Seperator' and lines are ended with `\r\n\`.
            get { return 2 + m_Parts.Sum(p => p.Length) + ( m_Parts.Count > 0 ? m_Seperator.Length * m_Parts.Count - 1 : 0) + 2; }
        }

        internal string GetPart(int index) { return m_Parts.Count > index ? m_Parts[index] : string.Empty; }

        internal void SetPart(int index, string value) { if(m_Parts.Count > index) m_Parts[index] = value; }

        internal void EnsureParts(int count)
        {
            while (m_Parts.Count < count) m_Parts.Add(string.Empty);
        }

        internal void Add(string part)
        {
            m_Parts.Add(part);
        }

        public SessionDescriptionLine(SessionDescriptionLine other)
        {
            m_Parts = other.m_Parts;
            Type = other.Type;
        }

        /// <summary>
        /// Constructs a new SessionDescriptionLine with the given type
        /// </summary>
        /// <param name="type">The type of the line</param>
        public SessionDescriptionLine(char type, int partCount = 0)
        {
            m_Parts = new List<string>(partCount);
            EnsureParts(partCount);
            Type = type;
            
        }

        /// <summary>
        /// Constructs a new SessionDescriptionLine with the given type and seperator
        /// </summary>
        /// <param name="type"></param>
        /// <param name="seperator"></param>
        public SessionDescriptionLine(char type, string seperator)
            :this(type, 0)
        {
            if (seperator != null) m_Seperator = seperator;
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
            return Type.ToString() + SessionDescription.EqualsSign + string.Join(m_Seperator, m_Parts.ToArray()) + SessionDescription.NewLine;
        }


        public IEnumerator<string> GetEnumerator()
        {
            yield return Type.ToString();

            yield return SessionDescription.EqualsSign.ToString();

            foreach (string part in m_Parts)
            {
                yield return part;

                yield return m_Seperator;
            }

            yield return SessionDescription.NewLine;
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<string>)this).GetEnumerator();
        }
    }

    //Public? TryRegisterLineImplementation, TypeCollection

    namespace /*Media.Sdp.*/Lines
    {
        /// <summary>
        /// Represents an 'a=' <see cref="SessionDescriptionLine"/>
        /// </summary>
        public class SessionAttributeLine : SessionDescriptionLine
        {
            internal const char AttributeType = 'a';

            public SessionAttributeLine(SessionDescriptionLine line)
                : base(line)
            {
                if (Type != AttributeType) throw new InvalidOperationException("Not a SessionAttributeLine line");
            }

            public SessionAttributeLine(string value)
                :base(AttributeType, SessionDescription.SpaceString)
            {
                Add(value);
            }
        }

        /// <summary>
        /// Represents an 'b=' <see cref="SessionDescriptionLine"/>
        /// </summary>
        public class SessionBandwidthLine : SessionDescriptionLine
        {
            #region RFC3556 Bandwidth

            const string RecieveBandwidthToken = "RR", SendBandwdithToken = "RS", ApplicationSpecificBandwidthToken = "AS";

            internal static Sdp.SessionDescriptionLine DisabledReceiveLine = new Sdp.SessionDescriptionLine("b=RR:0");

            internal static Sdp.SessionDescriptionLine DisabledSendLine = new Sdp.SessionDescriptionLine("b=RS:0");

            internal static Sdp.SessionDescriptionLine DisabledApplicationSpecificLine = new Sdp.SessionDescriptionLine("b=AS:0");

            public static bool TryParseBandwidthLine(Media.Sdp.SessionDescriptionLine line, out int result)
            {
                string token;

                return TryParseBandwidthLine(line, out token, out result);
            }

            public static bool TryParseBandwidthLine(Media.Sdp.SessionDescriptionLine line, out string token, out int result)
            {
                token = string.Empty;

                result = -1;

                if (line == null || line.Type != Sdp.Lines.SessionBandwidthLine.BandwidthType) return false;

                string[] tokens = line.Parts[0].Split(Media.Sdp.SessionDescription.ColonSplit, StringSplitOptions.RemoveEmptyEntries);

                if (tokens.Length < 2) return false;

                token = tokens[0];

                return int.TryParse(tokens[1], out result);
            }

            public static bool TryParseRecieveBandwidth(Media.Sdp.SessionDescriptionLine line, out int result)
            {
                result = -1;

                if (line == null || line.Type != Sdp.Lines.SessionBandwidthLine.BandwidthType) return false;

                if (false == line.Parts[0].StartsWith(RecieveBandwidthToken, StringComparison.OrdinalIgnoreCase)) return false;

                return TryParseBandwidthLine(line, out result);
            }

            public static bool TryParseSendBandwidth(Media.Sdp.SessionDescriptionLine line, out int result)
            {
                result = -1;

                if (line == null || line.Type != Sdp.Lines.SessionBandwidthLine.BandwidthType) return false;

                if (false == line.Parts[0].StartsWith(SendBandwdithToken, StringComparison.OrdinalIgnoreCase)) return false;

                return TryParseBandwidthLine(line, out result);
            }

            public static bool TryParseGetApplicationSpecificBandwidth(Media.Sdp.SessionDescriptionLine line, out int result)
            {
                result = -1;

                if (line == null || line.Type != Sdp.Lines.SessionBandwidthLine.BandwidthType) return false;

                if (false == line.Parts[0].StartsWith(ApplicationSpecificBandwidthToken, StringComparison.OrdinalIgnoreCase)) return false;

                return TryParseBandwidthLine(line, out result);
            }

            public static bool TryParseBandwidthDirectives(Media.Sdp.MediaDescription mediaDescription, out int rrDirective, out int rsDirective, out int asDirective)
            {
                rrDirective = rsDirective = asDirective = -1;

                if (mediaDescription == null) return false;

                int parsed = -1;

                string token = string.Empty;

                foreach (Media.Sdp.SessionDescriptionLine line in mediaDescription.BandwidthLines)
                {
                    if (TryParseBandwidthLine(line, out token, out parsed))
                    {
                        switch (token)
                        {
                            case RecieveBandwidthToken:
                                rrDirective = parsed;
                                continue;
                            case SendBandwdithToken:
                                rsDirective = parsed;
                                continue;
                            case ApplicationSpecificBandwidthToken:
                                asDirective = parsed;
                                continue;

                        }
                    }
                }

                //Determine if rtcp is disabled
                return parsed >= 0;
            }

            #endregion

            internal const char BandwidthType = 'b';

            #region Properties

            #endregion

            public SessionBandwidthLine(SessionDescriptionLine line)
                : base(line)
            {
                if (Type != BandwidthType) throw new InvalidOperationException("Not a SessionBandwidthLine line");
            }

            public SessionBandwidthLine(string token, int value)
                : base(BandwidthType, SessionDescription.Colon.ToString())
            {
                Add(token);

                Add(value.ToString());
            }

            public override string ToString()
            {
                return base.ToString();
            }

        }

        /// <summary>
        /// Represents an 'c=' <see cref="SessionDescriptionLine"/>
        /// </summary>
        public class SessionConnectionLine : SessionDescriptionLine
        {

            public const string InConnectionToken = "IN";

            public const string IP6 = "IP6";

            public const string IP4 = "IP4";


            internal const char ConnectionType = 'c';

            internal string ConnectionNetworkType { get { return GetPart(0); } set { SetPart(0, value); } }

            internal string ConnectionAddressType { get { return GetPart(1); } set { SetPart(1, value); } }

            internal string ConnectionAddress { get { return GetPart(2); } set { SetPart(2, value); } }

            internal bool HasMultipleAddresses
            {
                get
                {
                    return ConnectionAddress.Contains((char)Common.ASCII.ForwardSlash);
                }
            }

            //Todo
            //Only split the ConnectionAddress one time and cache it
            string[] m_ConnectionParts;

            public string IPAddress
            {
                get
                {
                    if (string.IsNullOrWhiteSpace(ConnectionAddress)) return null;

                    if (m_ConnectionParts == null) m_ConnectionParts = ConnectionAddress.Split(SessionDescription.SlashSplit, 3);

                    return m_ConnectionParts.First();
                }
            }

            public int? Hops
            {
                get
                {
                    if (string.IsNullOrWhiteSpace(ConnectionAddress)) return null;

                    if (m_ConnectionParts == null) m_ConnectionParts = ConnectionAddress.Split(SessionDescription.SlashSplit, 3);

                    if (m_ConnectionParts.Length > 2)
                    {
                        return int.Parse(m_ConnectionParts.Skip(1).First(), System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture);
                    }

                    return null;
                }
            }

            public int? Ports
            {
                get
                {
                    if (string.IsNullOrWhiteSpace(ConnectionAddress)) return null;

                    if (m_ConnectionParts == null) m_ConnectionParts = ConnectionAddress.Split(SessionDescription.SlashSplit, 3);

                    if (m_ConnectionParts.Length > 2)
                    {
                        return int.Parse(m_ConnectionParts.Last(), System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture);
                    }

                    return null;
                }
            }

            public SessionConnectionLine(SessionDescriptionLine line)
                : base(line)
            {
                if (Type != ConnectionType) throw new InvalidOperationException("Not a SessionConnectionLine");

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

                    if (sdpLine[0] != ConnectionType) Common.ExceptionExtensions.RaiseTaggedException(this, "Invalid SessionConnectionLine");

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
                return ConnectionType.ToString() + Media.Sdp.SessionDescription.EqualsSign + string.Join(SessionDescription.Space.ToString(), ConnectionNetworkType, ConnectionAddressType, ConnectionAddress) + SessionDescription.NewLine;
            }
        }

        /// <summary>
        /// Represents an 'v=' <see cref="SessionDescriptionLine"/>
        /// </summary>
        public class SessionVersionLine : SessionDescriptionLine
        {
            internal const char VersionType = 'v';

            public SessionVersionLine(SessionDescriptionLine line)
                : base(line)
            {
                if (Type != VersionType) throw new InvalidOperationException("Not a SessionVersionLine line");
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

                    if (sdpLine[0] != VersionType) Common.ExceptionExtensions.RaiseTaggedException(this, "Invalid SessionVersionLine Line");

                    sdpLine = SessionDescription.TrimLineValue(sdpLine.Substring(2));

                    m_Parts.Add(sdpLine);
                }
                catch
                {
                    throw;
                }
            }

        }

        /// <summary>
        /// Represents an 'o=' <see cref="SessionDescriptionLine"/>
        /// </summary>
        public class SessionOriginatorLine : SessionDescriptionLine
        {
            internal const char OriginatorType = 'o';

            public string Username { get { return GetPart(0); } set { SetPart(0, value); } }

            public string SessionId { get { return GetPart(1); } set { SetPart(1, value); } }

            public long SessionVersion
            {
                get
                {
                    string part = GetPart(2);
                    if (string.IsNullOrWhiteSpace(part)) return 0;
                    return part[0] == Common.ASCII.HyphenSign ? long.Parse(part) : (long)ulong.Parse(part);
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
                //Username = string.Empty;
            }

            public SessionOriginatorLine(SessionDescriptionLine line)
                : base(line)
            {
                if (Type != OriginatorType) throw new InvalidOperationException("Not a SessionOriginatorLine line");
            }

            public SessionOriginatorLine(string owner)
                : this()
            {

                if (string.IsNullOrWhiteSpace(owner)) m_Parts = new List<string>();
                else if (owner[0] != OriginatorType)
                {
                    m_Parts = new List<string>(owner.Split(SessionDescription.Space));
                }
                else m_Parts = new List<string>(owner.Substring(2).Replace(SessionDescription.NewLine, string.Empty).Split(SessionDescription.Space));

                if (m_Parts.Count < 6)
                {
                    EnsureParts(6);

                    //Make a new version if anything was added.
                    //SessionVersion++;
                }
            }

            public SessionOriginatorLine(string[] sdpLines, ref int index)
                : this()
            {
                try
                {
                    string sdpLine = sdpLines[index++].Trim();

                    if (sdpLine[0] != OriginatorType) Common.ExceptionExtensions.RaiseTaggedException(this, "Invalid SessionOriginatorLine");

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
                return OriginatorType.ToString() + Media.Sdp.SessionDescription.EqualsSign + string.Join(SessionDescription.Space.ToString(), Username, SessionId, SessionVersion, NetworkType, AddressType, Address) + SessionDescription.NewLine;
            }

        }

        /// <summary>
        /// Represents an 's=' <see cref="SessionDescriptionLine"/>
        /// </summary>
        public class SessionNameLine : SessionDescriptionLine
        {

            internal const char NameType = 's';

            public string SessionName { get { return m_Parts.Count > 0 ? m_Parts[0] : string.Empty; } set { m_Parts.Clear(); m_Parts.Add(value); } }

            public SessionNameLine()
                : base(NameType)
            {

            }

            public SessionNameLine(SessionDescriptionLine line)
                : base(line)
            {
                if (Type != NameType) throw new InvalidOperationException("Not a SessionNameLine line");
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

                    if (sdpLine[0] != NameType) Common.ExceptionExtensions.RaiseTaggedException(this, "Invalid SessionNameLine");

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
                return NameType.ToString() + Media.Sdp.SessionDescription.EqualsSign + (string.IsNullOrEmpty(SessionName) ? string.Empty : SessionName) + SessionDescription.NewLine;
            }
        }

        /// <summary>
        /// Represents an 'p=' <see cref="SessionDescriptionLine"/>
        /// </summary>
        public class SessionPhoneLine : SessionDescriptionLine
        {

            internal const char PhoneType = 'p';

            public string PhoneNumber { get { return m_Parts.Count > 0 ? m_Parts[0] : string.Empty; } set { m_Parts.Clear(); m_Parts.Add(value); } }

            public SessionPhoneLine()
                : base(PhoneType)
            {

            }

            public SessionPhoneLine(SessionDescriptionLine line)
                : base(line)
            {
                if (Type != PhoneType) throw new InvalidOperationException("Not a SessionPhoneLine");
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

                    if (sdpLine[0] != PhoneType) Common.ExceptionExtensions.RaiseTaggedException(this, "Invalid SessionPhoneLine");

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
                return PhoneType.ToString() + Media.Sdp.SessionDescription.EqualsSign + (string.IsNullOrEmpty(PhoneNumber) ? string.Empty : PhoneNumber) + SessionDescription.NewLine;
            }
        }

        /// <summary>
        /// Represents an 'e=' <see cref="SessionDescriptionLine"/>
        /// </summary>
        public class SessionEmailLine : SessionDescriptionLine
        {
            internal const char EmailType = 'e';

            public string Email { get { return m_Parts.Count > 0 ? m_Parts[0] : string.Empty; } set { m_Parts.Clear(); m_Parts.Add(value); } }

            public SessionEmailLine()
                : base(EmailType)
            {

            }

            public SessionEmailLine(SessionDescriptionLine line)
                : base(line)
            {
                if (Type != EmailType) throw new InvalidOperationException("Not a SessionEmailLine line");
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

                    if (sdpLine[0] != EmailType) Common.ExceptionExtensions.RaiseTaggedException(this, "Invalid SessionEmailLine");

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
                return EmailType.ToString() + Media.Sdp.SessionDescription.EqualsSign + (string.IsNullOrEmpty(Email) ? string.Empty : Email) + SessionDescription.NewLine;
            }
        }

        /// <summary>
        /// Represents an 'u=' <see cref="SessionDescriptionLine"/>
        /// </summary>
        public class SessionUriLine : SessionDescriptionLine
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
                : base(line)
            {
                if (Type != LocationType) throw new InvalidOperationException("Not a SessionUriLine");
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

                    if (sdpLine[0] != LocationType) Common.ExceptionExtensions.RaiseTaggedException(this, "Invalid SessionUriLine");

                    sdpLine = SessionDescription.TrimLineValue(sdpLine.Substring(2));

                    m_Parts.Add(sdpLine);
                }
                catch
                {
                    throw;
                }
            }

        }

        //RtpMapAttributeLine : AttributeLine

        //FormatTypeLine : AttributeLine
    }
}
