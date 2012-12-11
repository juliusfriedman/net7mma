using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.Sdp
{
    /// <summary>
    /// Provides facilities for parsing and creating SessionDescription data
    /// http://en.wikipedia.org/wiki/Session_Description_Protocol
    /// http://tools.ietf.org/html/rfc4566
    /// </summary>
    public sealed class SessionDescription
    {
        #region Statics

        static string CR = "\r";
        static string LF = "\n";
        static string CRLF = CR + LF;

        static string CleanValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return value;
            return value.Replace(CR, string.Empty).Replace(LF, string.Empty);
        }

        static string DictionaryToString(Dictionary<string, List<SessionDescriptionDescriptor>> dictionary, string prepend, string join)
        {
            string result = string.Empty;
            foreach (KeyValuePair<string, List<SessionDescriptionDescriptor>> place in dictionary)
            {
                foreach (SessionDescriptionDescriptor attribute in place.Value)
                {
                    result += prepend + attribute.Key + join + attribute.Value + CRLF;
                }
            }
            return result;
        }

        #endregion

        #region Nested Types

        /// <summary>
        /// Represents a property, attribute or bandwidth information line in a SessionDescription.
        /// </summary>
        public struct SessionDescriptionDescriptor
        {
            public string Key;
            public string Value;

            public SessionDescriptionDescriptor(string key, string value)
            {
                if (string.IsNullOrWhiteSpace(key)) throw new ArgumentNullException("Cannot be null or whitespace", "key");
                if (string.IsNullOrWhiteSpace(value)) throw new ArgumentNullException("Cannot be null or whitespace", "value");
                Key = CleanValue(key);
                Value = CleanValue(value);
            }
        }

        /// <summary>
        /// Thrown when a SessionDescription does not conform to the RFC 4566 outline
        /// </summary>
        public class SessionDescriptionException : Exception
        {
            public SessionDescriptionException(string message) : base(message) { }

            public SessionDescriptionException(string message, Exception innerException) : base(message, innerException) { }
        }

        /// <summary>
        /// Represents the TimeDescription in a Session Description
        /// Parses and Creates.
        /// </summary>
        public class SessionTimeDescription
        {
            public SessionTimeDescription(string[] sdpLines, ref int index)
            {
                string sdpLine = sdpLines[index++];

                if (!sdpLine.StartsWith("t=")) throw new SessionDescriptionException("Invalid Time Description");

                sdpLine = CleanValue(sdpLine.Replace("t=", string.Empty));
                string[] parts = sdpLine.Split(' ');
                SessionStartTime = Convert.ToInt32(CleanValue(parts[0]));
                SessionStopTime = Convert.ToInt32(CleanValue(parts[1]));

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
                        sdpLine = sdpLine.Replace("r=", string.Empty);
                        RepeatTimes.Add(Convert.ToInt32(CleanValue(sdpLine)));
                    }
                    catch (Exception ex)
                    {
                        throw new SessionDescriptionException("Invalid Repeat Time", ex);
                    }
                }

            }

            public SessionTimeDescription(int startTime, int stopTime)
            {
                SessionStartTime = startTime;
                SessionStopTime = stopTime;
            }

            public int SessionStartTime;
            public int SessionStopTime;

            public List<int> RepeatTimes = new List<int>();

            public override string ToString()
            {
                string result =  "t=" + SessionStartTime.ToString() + " " + SessionStopTime.ToString() + CRLF;
                foreach (int repeatTime in RepeatTimes)
                    result += "r=" + repeatTime + CRLF;
                return result;
            }
        }

        /// <summary>
        /// Represents the MediaDescription in a Session Description.
        /// Parses and Creates.
        /// </summary>
        public class SessionMediaDescription
        {
            #region Fields

            public string MediaType;
            public int MediaPort;
            public string MediaProtocol;
            public string MediaFormat;

            Dictionary<string, List<SessionDescriptionDescriptor>> m_Properties = new Dictionary<string, List<SessionDescriptionDescriptor>>();

            Dictionary<string, List<SessionDescriptionDescriptor>> m_BandwidthInformation = new Dictionary<string, List<SessionDescriptionDescriptor>>();

            Dictionary<string, List<SessionDescriptionDescriptor>> m_Attributes = new Dictionary<string, List<SessionDescriptionDescriptor>>();

            #endregion

            #region Propeties

            public string[] Properties { get { return m_Properties.Keys.ToArray(); } }

            public string[] Attributes { get { return m_Attributes.Keys.ToArray(); } }

            public string[] BandwidthInformation { get { return m_BandwidthInformation.Keys.ToArray(); } }

            #endregion

            #region Constructor

            public SessionMediaDescription(string[] sdpLines, ref int index)
            {
                string sdpLine = sdpLines[index++];

                if (!sdpLine.StartsWith("m=")) throw new SessionDescriptionException("Invalid Media Description");
                else sdpLine = sdpLine.Replace("m=", string.Empty);

                string[] parts = sdpLine.Split(' ');

                if (parts.Length != 4) throw new SessionDescriptionException("Invalid Media Name and Address");

                MediaType = parts[0];

                MediaPort = Convert.ToInt32(parts[1]); //Listener should probably verify ports with this

                MediaProtocol = parts[2]; //Listener should probably be using this to decide port

                MediaFormat = CleanValue(parts[3]);

                //Parse remaining optional entries
                for (int e = sdpLines.Length; index < e; ++index)
                {
                    string line = sdpLines[index];

                    if (line.StartsWith("m="))
                    {
                        //Found the start of another MediaDescription
                        break;
                    }
                    else if (line.StartsWith("a="))
                    {
                        //Parse the attribute
                        line = line.Replace("a=", string.Empty);
                        parts = line.Split(':');
                        SetAttribute(new SessionDescriptionDescriptor(parts[0], parts[1]));
                    }
                    else if (line.StartsWith("b="))
                    {
                        //Parse the attribute
                        line = line.Replace("b=", string.Empty);
                        parts = line.Split(':');
                        SetBandwithInformation(new SessionDescriptionDescriptor(parts[0], parts[1]));
                    }
                    else
                    {
                        //Parse the property
                        parts = line.Split('=');
                        if (parts.Length > 1)
                        {
                            SetProperty(new SessionDescriptionDescriptor(parts[0], parts[1]));
                        }
                    }
                }
            }

            #endregion

            #region Methods

            public List<SessionDescriptionDescriptor> GetProperty(string name)
            {
                if (m_Properties.ContainsKey(name)) return m_Properties[name];
                return null;
            }

            public void SetProperty(SessionDescriptionDescriptor attribute)
            {
                if (m_Properties.ContainsKey(attribute.Key))
                {
                    m_Properties[attribute.Key].Add(attribute);
                }
                else
                {
                    m_Properties.Add(attribute.Key, new List<SessionDescriptionDescriptor>() { attribute });
                }
            }

            public List<SessionDescriptionDescriptor> GetAttribute(string name)
            {
                if (m_Attributes.ContainsKey(name)) return m_Attributes[name];
                return null;
            }

            public void SetAttribute(SessionDescriptionDescriptor attribute)
            {
                if (m_Attributes.ContainsKey(attribute.Key))
                {
                    m_Attributes[attribute.Key].Add(attribute);
                }
                else
                {
                    m_Attributes.Add(attribute.Key, new List<SessionDescriptionDescriptor>() { attribute });
                }
            }

            public List<SessionDescriptionDescriptor> GetBandwithInformation(string name)
            {
                if (m_Attributes.ContainsKey(name)) return m_Properties[name];
                return null;
            }

            public void SetBandwithInformation(SessionDescriptionDescriptor attribute)
            {
                if (m_BandwidthInformation.ContainsKey(attribute.Key))
                {
                    m_BandwidthInformation[attribute.Key].Add(attribute);
                }
                else
                {
                    m_BandwidthInformation.Add(attribute.Key, new List<SessionDescriptionDescriptor>() { attribute });
                }
            }

            public override string ToString()
            {
                StringBuilder buffer = new StringBuilder();

                buffer.Append("m=" + MediaType + " " + MediaPort.ToString() + " " + MediaProtocol + " " + MediaFormat + CRLF);

                buffer.Append(SessionDescription.DictionaryToString(m_Properties, string.Empty, "="));

                buffer.Append(SessionDescription.DictionaryToString(m_BandwidthInformation, "b=", ":"));

                buffer.Append(SessionDescription.DictionaryToString(m_Attributes, "a=", ":"));

                return buffer.ToString();
            }

            #endregion
        }

        #endregion

        #region Fields

        Dictionary<string, List<SessionDescriptionDescriptor>> m_Properties = new Dictionary<string, List<SessionDescriptionDescriptor>>();

        Dictionary<string, List<SessionDescriptionDescriptor>> m_BandwidthInformation = new Dictionary<string, List<SessionDescriptionDescriptor>>();

        Dictionary<string, List<SessionDescriptionDescriptor>> m_Attributes = new Dictionary<string, List<SessionDescriptionDescriptor>>();

        #endregion

        #region Properties

        public List<SessionTimeDescription> TimeDescriptions = new List<SessionTimeDescription>();

        public List<SessionMediaDescription> MediaDesciptions = new List<SessionMediaDescription>();

        public int Version { get { return Convert.ToInt32(GetProperty("v")[0].Value); } set { SetProperty(new SessionDescriptionDescriptor("v", value.ToString())); } }

        public string OriginatorAndSessionIdentifier { get { return GetProperty("o")[0].Value; } set { SetProperty(new SessionDescriptionDescriptor("o", value)); } }

        public string SessionName { get { return GetProperty("s")[0].Value; } set { SetProperty(new SessionDescriptionDescriptor("s", value)); } }

        public string[] Properties { get { return m_Properties.Keys.ToArray(); } }

        public string[] Attributes { get { return m_Attributes.Keys.ToArray(); } }

        public string[] BandwidthInformation { get { return m_BandwidthInformation.Keys.ToArray(); } }

        #endregion

        #region Constructor

        static SessionDescription() { }

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
            string[] lines = sdpContents.Split('\n');
            int lineIndex = 0;

            if (lines.Length < 3) throw new SessionDescriptionException("Invalid Session Description");

            string[] parts = lines[lineIndex++].Split('=');

            if (parts[0] != "v") throw new SessionDescriptionException("Expected Protocol Version");
            else SetProperty(new SessionDescriptionDescriptor(parts[0], parts[1]));

            parts = lines[lineIndex++].Split('=');

            if (parts[0] != "o") throw new SessionDescriptionException("Expected Originator and Session Identifier");
            else SetProperty(new SessionDescriptionDescriptor(parts[0], parts[1]));

            parts = lines[lineIndex++].Split('=');

            if (parts[0] != "s") SetProperty(new SessionDescriptionDescriptor("s", "Archive Session Name"));
            else SetProperty(new SessionDescriptionDescriptor(parts[0], parts[1]));


            //Parse remaining optional entries
            for (int e = lines.Length; lineIndex < e; ++lineIndex)
            {
                string line = lines[lineIndex];

                //Check for TimeDescription
                if (line.StartsWith("t="))
                {
                    TimeDescriptions.Add(new SessionTimeDescription(lines, ref lineIndex));
                    continue;
                }

                //Check for MediaDescription
                if (line.StartsWith("m="))
                {
                    MediaDesciptions.Add(new SessionMediaDescription(lines, ref lineIndex));
                    continue;
                }

                if (line.StartsWith("a="))
                {
                    //Parse the attribute
                    line = line.Replace("a=", string.Empty);
                    parts = line.Split(':');
                    SetAttribute(new SessionDescriptionDescriptor(parts[0], parts[1]));
                }
                else if (line.StartsWith("b="))
                {
                    //Parse the attribute
                    line = line.Replace("b=", string.Empty);
                    parts = line.Split(':');
                    SetBandwithInformation(new SessionDescriptionDescriptor(parts[0], parts[1]));
                }
                else
                {
                    //Parse the property
                    parts = line.Split('=');
                    if (parts.Length > 1) SetProperty(new SessionDescriptionDescriptor(parts[0], parts[1]));
                }
            }            

        }

        #endregion

        #region Methods

        public List<SessionDescriptionDescriptor> GetProperty(string name)
        {
            if (m_Properties.ContainsKey(name)) return m_Properties[name];
            return null;
        }

        public void SetProperty(SessionDescriptionDescriptor attribute)
        {           
            if (m_Properties.ContainsKey(attribute.Key))
            {
                //There can only be one version, originator and session identifier
                if (attribute.Key == "v" || attribute.Key == "o" || attribute.Key == "s")
                {
                    m_Properties[attribute.Key][0] = attribute;
                }
                else
                {
                    m_Properties[attribute.Key].Add(attribute);
                }                
            }
            else
            {
                m_Properties.Add(attribute.Key, new List<SessionDescriptionDescriptor>() { attribute });
            }
        }

        public List<SessionDescriptionDescriptor> GetAttribute(string name)
        {
            if (m_Attributes.ContainsKey(name)) return m_Properties[name];
            return null;
        }

        public void SetAttribute(SessionDescriptionDescriptor attribute)
        {
            if (m_Attributes.ContainsKey(attribute.Key))
            {
                m_Attributes[attribute.Key].Add(attribute);
            }
            else
            {
                m_Attributes.Add(attribute.Key, new List<SessionDescriptionDescriptor>() { attribute });
            }
        }

        public List<SessionDescriptionDescriptor> GetBandwithInformation(string name)
        {
            if (m_Attributes.ContainsKey(name)) return m_Properties[name];
            return null;
        }

        public void SetBandwithInformation(SessionDescriptionDescriptor attribute)
        {
            if (m_BandwidthInformation.ContainsKey(attribute.Key))
            {
                m_BandwidthInformation[attribute.Key].Add(attribute);
            }
            else
            {
                m_BandwidthInformation.Add(attribute.Key, new List<SessionDescriptionDescriptor>() { attribute });
            }
        }

        /// <summary>
        /// Copies all Attributes, Properties and Bandwidth inforamation to another SessionDescription.
        /// Will not copy the version, originator or session name.
        /// </summary>
        /// <param name="other">The SessionDescription to copy to</param>
        public void CopyTo(SessionDescription other)
        {
            if (Version != other.Version) throw new ArgumentException("Must be the same version", "other");

            foreach (KeyValuePair<string, List<SessionDescriptionDescriptor>> place in m_Properties/*.Where(a=> a.Key != "v" && a.Key != "o" && a.Key != "s")*/)
            {
                foreach (SessionDescriptionDescriptor attribute in place.Value)
                {
                    //Dont copy all
                    if (attribute.Key == "v" || attribute.Key == "o" || attribute.Key == "s") continue;
                    other.SetProperty(attribute);
                }
            }

            foreach (KeyValuePair<string, List<SessionDescriptionDescriptor>> place in m_BandwidthInformation)
            {
                foreach (SessionDescriptionDescriptor attribute in place.Value)
                {
                    other.SetBandwithInformation(attribute);
                }
            }

            if (TimeDescriptions.Count > 0)
            {
                foreach (SessionTimeDescription timeDescription in TimeDescriptions)
                    other.TimeDescriptions.Add(timeDescription);
            }

            foreach (KeyValuePair<string, List<SessionDescriptionDescriptor>> place in m_Attributes)
            {
                foreach (SessionDescriptionDescriptor attribute in place.Value)
                {
                    other.SetAttribute(attribute);
                }
            }          

            if (MediaDesciptions.Count > 0)
            {
                foreach (SessionMediaDescription mediaDescription in MediaDesciptions)
                    other.MediaDesciptions.Add(mediaDescription);
            }
        }

        public override string ToString()
        {
            StringBuilder buffer = new StringBuilder();

            buffer.Append(SessionDescription.DictionaryToString(m_Properties, string.Empty, "="));

            buffer.Append(SessionDescription.DictionaryToString(m_BandwidthInformation, "b=", ":"));

            if (TimeDescriptions.Count > 0)
            {
                foreach (SessionTimeDescription timeDescription in TimeDescriptions)
                    buffer.Append(timeDescription.ToString());
            }

            buffer.Append(SessionDescription.DictionaryToString(m_Attributes, "a=", ":"));

            if (MediaDesciptions.Count > 0)
            {
                foreach (SessionMediaDescription mediaDescription in MediaDesciptions)
                    buffer.Append(mediaDescription.ToString());
            }

            //End of SDP
            buffer.Append(CRLF); 

            return buffer.ToString();
        }

        #endregion
    }
}
