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
        application,
        data,
        control
    }

    /// <summary>
    /// Provides facilities for parsing and creating SessionDescription data
    /// http://en.wikipedia.org/wiki/Session_Description_Protocol
    /// http://tools.ietf.org/html/rfc4566
    /// </summary>
    public sealed class SessionDescription
    {
        #region Statics

        const string CR = "\r";
        const string LF = "\n";
        internal const string CRLF = CR + LF;

        internal static string CleanValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return value;
            return value.Replace(CR, string.Empty).Replace(LF, string.Empty);
        }

        #endregion

        #region Fields

        SessionVersionLine m_Version = new SessionVersionLine(0);
        SessionOriginatorLine m_Originator = new SessionOriginatorLine("Owner");
        SessionNameLine m_SessionName = new SessionNameLine("Session Name");

        List<MediaDescription> m_MediaDescriptions = new List<MediaDescription>();
        List<TimeDescription> m_TimeDescriptions = new List<TimeDescription>();
        List<SessionDescriptionLine> m_Lines = new List<SessionDescriptionLine>();

        #endregion

        #region Properties

        public int Version { get { return m_Version.Version; } set { m_Version.Version = value; ++m_Originator.Version; } }

        public string OriginatorAndSessionIdentifier { get { return m_Originator.ToString(); } set { m_Originator = new SessionOriginatorLine(value.ToString()); ++m_Originator.Version; } }

        public string SessionName { get { return m_SessionName.SessionName; } set { m_SessionName.SessionName = value; ++m_Originator.Version; } }

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

            if (lines.Length < 3) throw new SessionDescriptionException("Invalid Session Description");

            //Parse remaining optional entries
            for (int lineIndex = 0, endIndex = lines.Length; lineIndex < endIndex; )
            {
                string line = lines[lineIndex].Trim();

                if (line.StartsWith("v="))
                {
                    m_Version = new SessionVersionLine(lines, ref lineIndex);
                    continue;
                }
                else if (line.StartsWith("o="))
                {
                    m_Originator = new SessionOriginatorLine(lines, ref lineIndex);
                    continue;
                }
                else if (line.StartsWith("s="))
                {
                    m_SessionName = new SessionNameLine(lines, ref lineIndex);
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
                    m_Lines.Add(SessionDescriptionLine.Parse(lines, ref lineIndex));
                }
            }            
        }

        public SessionDescription(SessionDescription other)
        {
            Version = other.Version;

            TimeDescriptions = other.TimeDescriptions;

            MediaDescriptions = other.MediaDescriptions;

            Lines = other.Lines;
        }

        #endregion

        #region Methods        

        public override string ToString()
        {
            StringBuilder buffer = new StringBuilder();

            buffer.Append(m_Version.ToString());

            buffer.Append(m_Originator.ToString());

            buffer.Append(m_SessionName.ToString());

            m_Lines.Where(l => l.Type != 'b' && l.Type != 'a'/* && l.Type != 'v' && l.Type != 'o' && l.Type != 's'*/).ToList().ForEach(l => buffer.Append(l));

            m_Lines.Where(l => l.Type == 'b').ToList().ForEach(l => buffer.Append(l));

            m_Lines.Where(l => l.Type == 'a').ToList().ForEach(l => buffer.Append(l));

            m_TimeDescriptions.ForEach(l => buffer.Append(l));

            if (m_MediaDescriptions.Count > 0)
            {
                foreach (MediaDescription mediaDescription in m_MediaDescriptions)
                    buffer.Append(mediaDescription.ToString());
            }

            //End of SDP
            buffer.Append(CRLF); 

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

        public MediaType MediaType { get; private set; }
        public int MediaPort { get; private set; }
        public string MediaProtocol { get; private set; }
        public string MediaFormat { get; private set; }

        List<SessionDescriptionLine> m_Lines = new List<SessionDescriptionLine>();

        #endregion

        public System.Collections.ObjectModel.ReadOnlyCollection<SessionDescriptionLine> Lines { get { return m_Lines.AsReadOnly(); } }

        #region Constructor

        public MediaDescription(MediaType mediaType, int mediaPort, string mediaProtocol, string mediaFormat)
        {
            MediaType = mediaType;
            MediaPort = mediaPort;
            MediaProtocol = mediaProtocol;
            MediaFormat = mediaFormat;
        }

        public MediaDescription(string[] sdpLines, ref int index)
        {
            string sdpLine = sdpLines[index++].Trim();

            if (!sdpLine.StartsWith("m=")) throw new SessionDescriptionException("Invalid Media Description");
            else sdpLine = sdpLine.Replace("m=", string.Empty);

            string[] parts = sdpLine.Split(' ');

            if (parts.Length != 4) throw new SessionDescriptionException("Invalid Media Name and Address");

            MediaType = (MediaType)Enum.Parse(typeof(MediaType), parts[0].ToLowerInvariant());

            MediaPort = Convert.ToInt32(parts[1]); //Listener should probably verify ports with this

            MediaProtocol = parts[2]; //Listener should probably be using this to decide port

            MediaFormat = SessionDescription.CleanValue(parts[3]);

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
                    m_Lines.Add(SessionDescriptionLine.Parse(sdpLines, ref index));
                }
            }
        }

        #endregion

        public override string ToString()
        {
            StringBuilder buffer = new StringBuilder();

            buffer.Append("m=" + string.Join(" ", MediaType , MediaPort.ToString() , MediaProtocol , MediaFormat) + SessionDescription.CRLF);

            m_Lines.Where(l => l.Type != 'b' && l.Type != 'a').ToList().ForEach(l => buffer.Append(l));

            m_Lines.Where(l => l.Type == 'b').ToList().ForEach(l => buffer.Append(l));

            m_Lines.Where(l => l.Type == 'a').ToList().ForEach(l => buffer.Append(l));

            return buffer.ToString();
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

            if (!sdpLine.StartsWith("t=")) throw new SessionDescriptionException("Invalid Time Description");

            sdpLine = SessionDescription.CleanValue(sdpLine.Replace("t=", string.Empty));
            string[] parts = sdpLine.Split(' ');
            SessionStartTime = long.Parse(SessionDescription.CleanValue(parts[0]));
            SessionStopTime = long.Parse(SessionDescription.CleanValue(parts[1]));

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
                    RepeatTimes.Add(long.Parse(SessionDescription.CleanValue(sdpLine.Replace("r=", string.Empty))));
                }
                catch (Exception ex)
                {
                    throw new SessionDescriptionException("Invalid Repeat Time", ex);
                }
            }

        }

        public override string ToString()
        {
            string result = "t=" + SessionStartTime.ToString() + " " + SessionStopTime.ToString() + SessionDescription.CRLF;
            foreach (int repeatTime in RepeatTimes)
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
    public class SessionDescriptionException : Exception
    {
        public SessionDescriptionException(string message) : base(message) { }

        public SessionDescriptionException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Low level class for dealing with Sdp lines with a format of 'X=V{st:sv0,sv1;svN}'
    /// </summary>
    public class SessionDescriptionLine
    {
        public List<string> Parts;
        public char Type;
        
        public SessionDescriptionLine(string key, char type)
        {
            Parts = new List<string>();
            Type = type;
        }

        public SessionDescriptionLine(string line)
        {
            if (line[1] != '=') throw new SessionDescriptionException("Invalid SessionDescriptionLine: \"" + line + "\"");

            Type = char.ToLower(line[0]);

            //Two types 
            //a=<flag>
            //a=<name>:<value> where value = {...,...,...;x;y;z}

            Parts = new List<string>(line.Remove(0, 2)/*.Replace("\"", string.Empty)*/.Split(new char[] { ';' }));
        }

        public override string ToString()
        {
            return new String(Type, 1) + '=' + string.Join(";", Parts.ToArray()) + SessionDescription.CRLF;
        }

        internal static SessionDescriptionLine Parse(string[] sdpLines, ref int index)
        {
            string sdpLine = sdpLines[index] = sdpLines[index].Trim();

            char type = sdpLine[0];

            switch (type)
            {
                case 'v': return new SessionVersionLine(sdpLines, ref index);
                case 'o': return new SessionOriginatorLine(sdpLines, ref index);
                case 's': return new SessionNameLine(sdpLines, ref index);
                case 'c': return new SessionConnectionLine(sdpLines, ref index);
                case 'u': return new SessionUriLine(sdpLines, ref index);
                case 'e': return new SessionEmailLine(sdpLines, ref index);
                case 'p': return new SessionPhoneLine(sdpLines, ref index);
                case 'z': //Zone Information
                case 'a': //Attribute
                case 'b': //Bandwidth
                default:
                    {
                        ++index;
                        return new SessionDescriptionLine(sdpLine);
                    }
            }
        }

    }

    #region Internal Line Parsers Types

    internal class SessionVersionLine : SessionDescriptionLine
    {

        public int Version { get { return Parts.Count > 0 ? int.Parse(Parts[0]) : 0; } set { Parts.Clear(); Parts.Add(value.ToString()); } }

        public SessionVersionLine(int version)
            : base(null, 'v')
        {
            Version = version;
        }

        public SessionVersionLine(string[] sdpLines, ref int index)
            : base(null, 'v')
        {
            try
            {
                string sdpLine = sdpLines[index++].Trim();

                if (!sdpLine.StartsWith("v=")) throw new SessionDescriptionException("Invalid Version");

                sdpLine = SessionDescription.CleanValue(sdpLine.Replace("v=", string.Empty));

                Parts.Add(sdpLine);
            }
            catch
            {
                throw;
            }
        }

    }

    internal class SessionOriginatorLine : SessionDescriptionLine
    {
        public string Username;
        public string SessionId;
        public int Version;
        public string NetworkType;
        public string AddressType;
        public string Address;

        public SessionOriginatorLine()
            : base(null, 'o')
        {
            Version = 1;
        }

        public SessionOriginatorLine(string owner)
            : this()
        {
            Username = owner;
        }

        public SessionOriginatorLine(string[] sdpLines, ref int index)
            : this()
        {
            try
            {
                string sdpLine = sdpLines[index++].Trim();

                if (!sdpLine.StartsWith("o=")) throw new SessionDescriptionException("Invalid Owner");

                sdpLine = SessionDescription.CleanValue(sdpLine.Replace("o=", string.Empty));

                Parts.Add(sdpLine);

                string[] ownerFields = sdpLine.Split(' ');
                Username = ownerFields[0];
                SessionId = ownerFields[1];
                Int32.TryParse(ownerFields[2], out Version);
                NetworkType = ownerFields[3];
                AddressType = ownerFields[4];
                Address = ownerFields[5];

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
        public string SessionName { get { return Parts.Count > 0 ? Parts[0] : string.Empty; } set { Parts.Clear(); Parts.Add(value); } }

        public SessionNameLine()
            : base(null, 's')
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

                if (!sdpLine.StartsWith("s=")) throw new SessionDescriptionException("Invalid Session Name");

                sdpLine = SessionDescription.CleanValue(sdpLine.Replace("s=", string.Empty));

                Parts.Add(sdpLine);
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
        public string PhoneNumber { get { return Parts.Count > 0 ? Parts[0] : string.Empty; } set { Parts.Clear(); Parts.Add(value); } }

        public SessionPhoneLine()
            : base(null, 'p')
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

                if (!sdpLine.StartsWith("p=")) throw new SessionDescriptionException("Invalid PhoneNumber");

                sdpLine = SessionDescription.CleanValue(sdpLine.Replace("p=", string.Empty));

                Parts.Add(sdpLine);
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
        public string Email { get { return Parts.Count > 0 ? Parts[0] : string.Empty; } set { Parts.Clear(); Parts.Add(value); } }

        public SessionEmailLine()
            : base(null, 'e')
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

                if (!sdpLine.StartsWith("e=")) throw new SessionDescriptionException("Invalid Email");

                sdpLine = SessionDescription.CleanValue(sdpLine.Replace("e=", string.Empty));

                Parts.Add(sdpLine);
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

        public Uri Location { get { return Parts.Count > 0 ? (new Uri(Parts[0])) : null; } set { Parts.Clear(); Parts.Add(value.ToString()); } }

        public SessionUriLine()
            : base(null, 'u')
        {
        }

        public SessionUriLine(string uri)
            : this()
        {
            Location = new Uri(uri);
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

                if (!sdpLine.StartsWith("u=")) throw new SessionDescriptionException("Invalid Uri");

                sdpLine = SessionDescription.CleanValue(sdpLine.Replace("u=", string.Empty));

                Parts.Add(sdpLine);

                Location = new Uri(sdpLine);
            }
            catch
            {
                throw;
            }
        }

    }

    internal class SessionConnectionLine : SessionDescriptionLine
    {
        string NetworkType;
        string AddressType;
        string Address;

        public SessionConnectionLine()
            : base(null, 'c')
        {
        }

        public SessionConnectionLine(string[] sdpLines, ref int index)
            : this()
        {
            try
            {
                string sdpLine = sdpLines[index++].Trim();

                if (!sdpLine.StartsWith("c=")) throw new SessionDescriptionException("Invalid Media Connection Line");

                sdpLine = SessionDescription.CleanValue(sdpLine.Replace("c=", string.Empty));

                Parts.Add(sdpLine);

                string[] connectionFields = sdpLine.Split(' ');
                NetworkType = connectionFields[0].Trim();
                AddressType = connectionFields[1].Trim();
                Address = connectionFields[2].Trim();

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

    #endregion

    public static class Extensions
    {
        public static IEnumerable<SessionDescriptionLine> Where(this List<SessionDescriptionLine> list, char type, string value)
        {
            return list.Where(l => l.Type == type && l.Parts.Contains(value));
        }
    }
}
