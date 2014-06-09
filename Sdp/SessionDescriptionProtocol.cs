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
    public sealed class SessionDescription
    {
        #region Statics
        
        public const string MimeType = "application/sdp";

        const string CR = "\r";
        const string LF = "\n";
        internal const string CRLF = CR + LF;

        internal static string CleanLineValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return value;
            //Trims the whitespace  removes CR, LF from everywhere in the value...
            return value.Trim();   // .Replace(CR, string.Empty).Replace(LF, string.Empty);
        }

        #endregion

        #region Fields

        Media.Sdp.Lines.SessionVersionLine m_Version = new Media.Sdp.Lines.SessionVersionLine(0);
        Media.Sdp.Lines.SessionOriginatorLine m_Originator = new Media.Sdp.Lines.SessionOriginatorLine("Owner");
        Media.Sdp.Lines.SessionNameLine m_SessionName = new Media.Sdp.Lines.SessionNameLine("Session Name");

        List<MediaDescription> m_MediaDescriptions = new List<MediaDescription>();
        List<TimeDescription> m_TimeDescriptions = new List<TimeDescription>();
        List<SessionDescriptionLine> m_Lines = new List<SessionDescriptionLine>();

        #endregion

        #region Properties

        public int Version { get { return m_Version.Version; } private set { if (value != m_Version.Version) { m_Version.Version = value; ++m_Originator.Version; } } }

        public string OriginatorAndSessionIdentifier { get { return m_Originator.ToString(); } set { m_Originator = new Media.Sdp.Lines.SessionOriginatorLine(value.ToString()); } }

        public string SessionName { get { return m_SessionName.SessionName; } set { if (value != m_SessionName.SessionName) { m_SessionName.SessionName = value; ++m_Originator.Version; } } }

        public System.Collections.ObjectModel.ReadOnlyCollection<TimeDescription> TimeDescriptions { get { return m_TimeDescriptions.AsReadOnly(); } set { m_TimeDescriptions = value.ToList(); ++m_Originator.Version; } }

        public System.Collections.ObjectModel.ReadOnlyCollection<MediaDescription> MediaDescriptions { get { return m_MediaDescriptions.AsReadOnly(); } set { m_MediaDescriptions = value.ToList(); ++m_Originator.Version; } }

        public System.Collections.ObjectModel.ReadOnlyCollection<SessionDescriptionLine> Lines { get { return m_Lines.AsReadOnly(); } set { m_Lines = value.ToList(); ++m_Originator.Version; } }

        #endregion

        #region Constructor

        public SessionDescription(int version)
        {
            Version = version;
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
        {
            Version = protocolVersion;

            OriginatorAndSessionIdentifier = originatorAndSession;

            SessionName = sessionName;
        }

        /// <summary>
        /// Constructs a SessionDescription from the given contents of a Session Description Protocol message
        /// </summary>
        /// <param name="sdpContents">The Session Description Protocol usually recieved in the Describe request of a RtspClient</param>
        public SessionDescription(string sdpContents)
        {
            string[] lines = sdpContents.Split(CRLF.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            if (lines.Length < 3) Common.ExceptionExtensions.CreateAndRaiseException(this, "Invalid Session Description");

            //Parse remaining optional entries
            for (int lineIndex = 0, endIndex = lines.Length; lineIndex < endIndex; /*Advancement of the loop controlled by the corrsponding Lines via ref*/)
            {
                string line = lines[lineIndex].Trim();

                if (line.StartsWith("v="))
                {
                    m_Version = new Media.Sdp.Lines.SessionVersionLine(lines, ref lineIndex);
                    continue;
                }
                else if (line.StartsWith("o="))
                {
                    m_Originator = new Media.Sdp.Lines.SessionOriginatorLine(lines, ref lineIndex);
                    continue;
                }
                else if (line.StartsWith("s="))
                {
                    m_SessionName = new Media.Sdp.Lines.SessionNameLine(lines, ref lineIndex);
                    continue;
                }
                else if (line.StartsWith("t=")) //Check for TimeDescription
                {
                    m_TimeDescriptions.Add(new TimeDescription(lines, ref lineIndex));
                    continue;
                }
                else if (line.StartsWith("m="))//Check for MediaDescription
                {
                    m_MediaDescriptions.Add(new MediaDescription(lines, ref lineIndex));
                    continue;
                }
                else
                {
                    SessionDescriptionLine parsed = SessionDescriptionLine.Parse(lines, ref lineIndex);
                    if (parsed != null) m_Lines.Add(parsed);
                    else lineIndex++;//No advance was made on lineIndex by SessionDescriptionLine if parsed was null
                }
            }            
        }

        /// <summary>
        /// Creates a copy of another SessionDescription
        /// </summary>
        /// <param name="other">The SessionDescription to copy</param>
        public SessionDescription(SessionDescription other)
        {
            Version = other.Version;

            OriginatorAndSessionIdentifier = other.OriginatorAndSessionIdentifier;

            SessionName = other.SessionName;

            m_TimeDescriptions = new List<TimeDescription>(other.TimeDescriptions);

            m_MediaDescriptions = new List<MediaDescription>(other.m_MediaDescriptions);

            m_Lines = new List<SessionDescriptionLine>(other.Lines);
        }

        #endregion

        #region Methods        

        public void Add(MediaDescription mediaDescription, bool updateVersion = true)
        {
            if (mediaDescription == null) return;
            m_MediaDescriptions.Add(mediaDescription);
            if (updateVersion) ++m_Originator.Version;
        }

        public void Add(TimeDescription timeDescription, bool updateVersion = true)
        {
            if (timeDescription == null) return;
            m_TimeDescriptions.Add(timeDescription);
            if (updateVersion) ++m_Originator.Version;
        }

        public void Add(SessionDescriptionLine line, bool updateVersion = true)
        {
            if (line == null) return;
            m_Lines.Add(line);
            if (updateVersion) ++m_Originator.Version;
        }

        public void RemoveLine(int index, bool updateVersion = true)
        {
            m_Lines.RemoveAt(index);
            if (updateVersion) ++m_Originator.Version;
        }

        public void RemoveMediaDescription(int index, bool updateVersion = true)
        {
            m_MediaDescriptions.RemoveAt(index);
            if (updateVersion) ++m_Originator.Version;
        }

        public void RemoveTimeDescription(int index, bool updateVersion = true)
        {
            m_TimeDescriptions.RemoveAt(index);
            if(updateVersion) ++m_Originator.Version;
        }

        public override string ToString()
        {
            StringBuilder buffer = new StringBuilder();

            buffer.Append(m_Version.ToString());

            buffer.Append(m_Originator.ToString());

            buffer.Append(m_SessionName.ToString());

            foreach (SessionDescriptionLine l in m_Lines.Where(l => l.Type != 'b' && l.Type != 'a'))
            {
                buffer.Append(l.ToString());
            }

            foreach (SessionDescriptionLine l in m_Lines.Where(l => l.Type == 'b'))
            {
                buffer.Append(l.ToString());
            }

            foreach (SessionDescriptionLine l in m_Lines.Where(l => l.Type == 'a'))
            {
                buffer.Append(l.ToString());
            }

            m_TimeDescriptions.ForEach(td => buffer.Append(td.ToString(this)));

            m_MediaDescriptions.ForEach(md => buffer.Append(md.ToString(this)));

            //End of SDP
            buffer.Append(default(char) + CRLF); 

            //Strings in .Net are Unicode code points ( subsequently the characters only are addressable by their 16 bit code point representation).
            return buffer.ToString();
        }

        #endregion
    }

    /// <summary>
    /// Represents the MediaDescription in a Session Description.
    /// Parses and Creates.
    /// </summary>
    public class MediaDescription
    {
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

            if (!sdpLine.StartsWith("m=")) Common.ExceptionExtensions.CreateAndRaiseException(this,"Invalid Media Description");
            else sdpLine = sdpLine.Replace("m=", string.Empty);

            string[] parts = sdpLine.Split(' ');

            if (parts.Length != 4) Common.ExceptionExtensions.CreateAndRaiseException(this,"Invalid Media Description");

            MediaType = (MediaType)Enum.Parse(typeof(MediaType), SessionDescription.CleanLineValue(parts[0].ToLowerInvariant()));

            MediaPort = int.Parse(SessionDescription.CleanLineValue(parts[1]), System.Globalization.CultureInfo.InvariantCulture); //Listener should probably verify ports with this

            MediaProtocol = parts[2]; //Listener should probably be using this to decide port

            MediaFormat = byte.Parse(SessionDescription.CleanLineValue(parts[3]), System.Globalization.CultureInfo.InvariantCulture);

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
                    SessionDescriptionLine parsed = SessionDescriptionLine.Parse(sdpLines, ref index);
                    if (parsed != null) m_Lines.Add(parsed);
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
                        buffer.Append("m=" + string.Join(" ", MediaType, MediaPort.ToString() + '/' + portSpecifier, MediaProtocol, MediaFormat) + SessionDescription.CRLF);
                        goto LinesOnly;
                    }
                }
            }
            
            buffer.Append("m=" + string.Join(" ", MediaType,  MediaPort.ToString(), MediaProtocol, MediaFormat) + SessionDescription.CRLF);

        LinesOnly:
            foreach (SessionDescriptionLine l in m_Lines.Where(l => l.Type != 'b' && l.Type != 'a'))
                buffer.Append(l.ToString());

            foreach (SessionDescriptionLine l in m_Lines.Where(l => l.Type == 'b'))
                buffer.Append(l.ToString());

            foreach (SessionDescriptionLine l in m_Lines.Where(l => l.Type == 'a'))
                buffer.Append(l.ToString());

            return buffer.ToString();
        }

        public SessionDescriptionLine RtpMap
        {
            get
            {
                return m_Lines.FirstOrDefault(l => l.Type == 'a' && string.Compare(l.Parts[0], "rtpmap", true) >= 0);
            }
        }

    }

    /// <summary>
    /// Represents a TimeDescription with optional Repeat times.
    /// Parses and Creates.
    /// </summary>
    public class TimeDescription
    {

        public long SessionStartTime { get; private set; }
        public long SessionStopTime { get; private set; }

        public List<long> RepeatTimes { get; private set; }

        public TimeDescription(int startTime, int stopTime)            
        {
            SessionStartTime = startTime;
            SessionStopTime = stopTime;
            RepeatTimes = new List<long>();
        }

        public TimeDescription(string[] sdpLines, ref int index)
        {
            string sdpLine = sdpLines[index++].Trim();

            if (!sdpLine.StartsWith("t=")) Common.ExceptionExtensions.CreateAndRaiseException(this,"Invalid Time Description");

            sdpLine = SessionDescription.CleanLineValue(sdpLine.Replace("t=", string.Empty));
            string[] parts = sdpLine.Split(' ');
            SessionStartTime = long.Parse(SessionDescription.CleanLineValue(parts[0]), System.Globalization.CultureInfo.InvariantCulture);
            SessionStopTime = long.Parse(SessionDescription.CleanLineValue(parts[1]), System.Globalization.CultureInfo.InvariantCulture);
            RepeatTimes = new List<long>();
            //Iterate remaining lines
            for (; index < sdpLines.Length; ++index)
            {
                //Scope a line
                sdpLine = sdpLines[index];

                //If we are not extracing repeat times then there is no more TimeDescription to parse
                if (!sdpLine.StartsWith("r=")) break;

                //Parse and add the repeat time
                try
                {
                    RepeatTimes.Add(long.Parse(SessionDescription.CleanLineValue(sdpLine.Replace("r=", string.Empty)), System.Globalization.CultureInfo.InvariantCulture));
                }
                catch (Exception ex)
                {
                    Common.ExceptionExtensions.CreateAndRaiseException(this,"Invalid Repeat Time", ex);
                }
            }

        }

        public string ToString(SessionDescription sdp = null)
        {
            string result = "t=" + SessionStartTime.ToString() + " " + SessionStopTime.ToString() + SessionDescription.CRLF;
            foreach (long repeatTime in RepeatTimes)
                result += "r=" + repeatTime + SessionDescription.CRLF;
            return result;
        }
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
    /// Thrown when a SessionDescription does not conform to the RFC 4566 outline
    /// </summary>
  

    /// <summary>
    /// Low level class for dealing with Sdp lines with a format of 'X=V{st:sv0,sv1;svN}'    
    /// </summary>
    /// <remarks>Should use byte[] and should have Encoding as a property</remarks>
    public class SessionDescriptionLine
    {
        static char[] ValueSplit = new char[] { ';' };

        public readonly char Type;
        
        protected List<string> m_Parts;

        public System.Collections.ObjectModel.ReadOnlyCollection<string> Parts { get { return m_Parts.AsReadOnly(); } }

        internal string GetPart(int index) { return m_Parts.Count > index ? m_Parts[index] : string.Empty; }

        internal void SetPart(int index, string value) { if(m_Parts.Count > index) m_Parts[index] = value; }

        internal void EnsureParts(int count)
        {
            while (m_Parts.Count < count) m_Parts.Add(string.Empty);
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
            if (line.Length < 2 || line[1] != '=') Common.ExceptionExtensions.CreateAndRaiseException(this,"Invalid SessionDescriptionLine: \"" + line + "\"");

            Type = char.ToLower(line[0]);

            //Two types 
            //a=<flag>
            //a=<name>:<value> where value = {...,...,...;x;y;z}

            m_Parts = new List<string>(line.Remove(0, 2)/*.Replace("\"", string.Empty)*/.Split(ValueSplit));
        }

        /// <summary>
        /// The string representation of the SessionDescriptionLine including the required new lines.
        /// </summary>
        /// <returns>The string representation of the SessionDescriptionLine including the required new lines.</returns>
        public override string ToString()
        {
            return new String(Type, 1) + '=' + string.Join(";", m_Parts.ToArray()) + SessionDescription.CRLF;
        }

        internal static SessionDescriptionLine Parse(string[] sdpLines, ref int index)
        {
            string sdpLine = sdpLines[index] = sdpLines[index].Trim();

            if (sdpLine.Length <= 2) return null;
            else if (sdpLine[1] != '=') return null;

            char type = sdpLine[0];

            //Invalid Line
            if (type == default(char)) return null;

            try
            {
                switch (type)
                {
                    case 'v': return new Media.Sdp.Lines.SessionVersionLine(sdpLines, ref index);
                    case 'o': return new Media.Sdp.Lines.SessionOriginatorLine(sdpLines, ref index);
                    case 's': return new Media.Sdp.Lines.SessionNameLine(sdpLines, ref index);
                    case 'c': return new Media.Sdp.Lines.SessionConnectionLine(sdpLines, ref index);
                    case 'u': return new Media.Sdp.Lines.SessionUriLine(sdpLines, ref index);
                    case 'e': return new Media.Sdp.Lines.SessionEmailLine(sdpLines, ref index);
                    case 'p': return new Media.Sdp.Lines.SessionPhoneLine(sdpLines, ref index);
                    case 'z': //TimeZone Information
                    case 'a': //Attribute
                    case 'b': //Bandwidth
                    default:
                        {
                            ++index;
                            return new SessionDescriptionLine(sdpLine);
                        }
                }
            }
            catch
            {
                return null;
            }

        }
    }

    #region Internal Line Types
   
    namespace /*Media.Sdp.*/Lines
    {
        internal class SessionVersionLine : SessionDescriptionLine
        {

            public int Version { get { return m_Parts.Count > 0 ? int.Parse(m_Parts[0], System.Globalization.CultureInfo.InvariantCulture) : 0; } set { m_Parts.Clear(); m_Parts.Add(value.ToString()); } }

            public SessionVersionLine(int version)
                : base('v')
            {
                Version = version;
            }

            public SessionVersionLine(string[] sdpLines, ref int index)
                : base('v')
            {
                try
                {
                    string sdpLine = sdpLines[index++].Trim();

                    if (!sdpLine.StartsWith("v=")) Common.ExceptionExtensions.CreateAndRaiseException(this,"Invalid Version");

                    sdpLine = SessionDescription.CleanLineValue(sdpLine.Replace("v=", string.Empty));

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

            public string Username { get { return GetPart(0); } set { SetPart(0, value); } }
            public string SessionId { get { return GetPart(1); } set { SetPart(1, value); } }
            public ulong Version { get { string part = GetPart(2); return !string.IsNullOrWhiteSpace(part) ? ulong.Parse(part, System.Globalization.CultureInfo.InvariantCulture) : 0; } set { SetPart(2, value.ToString()); } }
            public string NetworkType { get { return GetPart(3); } set { SetPart(3, value); } }
            public string AddressType { get { return GetPart(4); } set { SetPart(4, value); } }
            public string Address { get { return GetPart(5); } set { SetPart(5, value); } }

            public SessionOriginatorLine()
                : base('o')
            {
                while (m_Parts.Count < 6) m_Parts.Add(string.Empty);
                Username = string.Empty;
                Version = 1;
            }

            public SessionOriginatorLine(string owner)
                : this()
            {
                m_Parts = new List<string>(owner.Replace("o=", string.Empty).Replace(SessionDescription.CRLF, string.Empty).Split(' '));
                if (m_Parts.Count < 6)
                {
                    EnsureParts(6);
                    Version++;
                }
            }

            public SessionOriginatorLine(string[] sdpLines, ref int index)
                : this()
            {
                try
                {
                    string sdpLine = sdpLines[index++].Trim();

                    if (!sdpLine.StartsWith("o=")) Common.ExceptionExtensions.CreateAndRaiseException(this,"Invalid Owner");

                    sdpLine = SessionDescription.CleanLineValue(sdpLine.Replace("o=", string.Empty));

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
                return "o=" + string.Join(" ", Username, SessionId, Version, NetworkType, AddressType, Address) + SessionDescription.CRLF;
            }

        }

        internal class SessionNameLine : SessionDescriptionLine
        {
            public string SessionName { get { return m_Parts.Count > 0 ? m_Parts[0] : string.Empty; } set { m_Parts.Clear(); m_Parts.Add(value); } }

            public SessionNameLine()
                : base('s')
            {

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

                    if (!sdpLine.StartsWith("s=")) Common.ExceptionExtensions.CreateAndRaiseException(this,"Invalid Session Name");

                    sdpLine = SessionDescription.CleanLineValue(sdpLine.Replace("s=", string.Empty));

                    m_Parts.Add(sdpLine);
                }
                catch
                {
                    throw;
                }
            }

            public override string ToString()
            {
                return "s=" + (string.IsNullOrEmpty(SessionName) ? string.Empty : SessionName) + SessionDescription.CRLF;
            }
        }

        internal class SessionPhoneLine : SessionDescriptionLine
        {
            public string PhoneNumber { get { return m_Parts.Count > 0 ? m_Parts[0] : string.Empty; } set { m_Parts.Clear(); m_Parts.Add(value); } }

            public SessionPhoneLine()
                : base('p')
            {

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

                    if (!sdpLine.StartsWith("p=")) Common.ExceptionExtensions.CreateAndRaiseException(this,"Invalid PhoneNumber");

                    sdpLine = SessionDescription.CleanLineValue(sdpLine.Replace("p=", string.Empty));

                    m_Parts.Add(sdpLine);
                }
                catch
                {
                    throw;
                }
            }

            public override string ToString()
            {
                return "p=" + (string.IsNullOrEmpty(PhoneNumber) ? string.Empty : PhoneNumber) + SessionDescription.CRLF;
            }
        }

        internal class SessionEmailLine : SessionDescriptionLine
        {
            public string Email { get { return m_Parts.Count > 0 ? m_Parts[0] : string.Empty; } set { m_Parts.Clear(); m_Parts.Add(value); } }

            public SessionEmailLine()
                : base('e')
            {

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

                    if (!sdpLine.StartsWith("e=")) Common.ExceptionExtensions.CreateAndRaiseException(this,"Invalid Email");

                    sdpLine = SessionDescription.CleanLineValue(sdpLine.Replace("e=", string.Empty));

                    m_Parts.Add(sdpLine);
                }
                catch
                {
                    throw;
                }
            }

            public override string ToString()
            {
                return "e=" + (string.IsNullOrEmpty(Email) ? string.Empty : Email) + SessionDescription.CRLF;
            }
        }

        internal class SessionUriLine : SessionDescriptionLine
        {

            public Uri Location
            {
                get
                {
                    Uri result;
                    Uri.TryCreate(m_Parts[0], UriKind.RelativeOrAbsolute, out result);
                    return result;
                }
                set { m_Parts.Clear(); m_Parts.Add(value.ToString()); }
            }

            public SessionUriLine()
                : base('u')
            {
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

                    if (!sdpLine.StartsWith("u=")) Common.ExceptionExtensions.CreateAndRaiseException(this,"Invalid Uri");

                    sdpLine = SessionDescription.CleanLineValue(sdpLine.Replace("u=", string.Empty));

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
                    if (!string.IsNullOrWhiteSpace(Address))
                    {
                        var parts = Address.Split('/');
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
                        var parts = Address.Split('/');
                        if (parts.Length > 2)
                        {
                            return int.Parse(parts.Skip(1).Single(), System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture);
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
                        var parts = Address.Split('/');
                        if (parts.Length > 2)
                        {
                            return int.Parse(parts.Last(), System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture);
                        }
                    }
                    return null;
                }
            }

            public SessionConnectionLine()
                : base('c', 3)
            {

            }

            public SessionConnectionLine(string[] sdpLines, ref int index)
                : this()
            {
                try
                {
                    string sdpLine = sdpLines[index++].Trim();

                    if (!sdpLine.StartsWith("c=")) Common.ExceptionExtensions.CreateAndRaiseException(this,"Invalid Session Connection Line");

                    sdpLine = SessionDescription.CleanLineValue(sdpLine.Replace("c=", string.Empty));

                    m_Parts.Add(sdpLine);

                    m_Parts = new List<string>(sdpLine.Split(' '));
                }
                catch
                {
                    throw;
                }
            }

            public override string ToString()
            {
                return "c=" + string.Join(" ", NetworkType, AddressType, Address) + SessionDescription.CRLF;
            }
        }
    }

    #endregion
}
