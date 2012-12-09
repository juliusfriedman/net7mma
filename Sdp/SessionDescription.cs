using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.Sdp
{
    /// <summary>
    /// http://en.wikipedia.org/wiki/Session_Description_Protocol
    /// </summary>
    public class SessionDescription
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

        #endregion

        #region Nested Types

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
        /// </summary>
        public class SessionTimeDescription
        {
            public SessionTimeDescription(string[] sdpLines, ref int index)
            {
                string sdpLine = sdpLines[index++];

                if (!sdpLine.StartsWith("t=")) throw new SessionDescriptionException("Invalid Time Description");

                sdpLine = CleanValue(sdpLine.Replace("t=", string.Empty));
                string[] parts = sdpLine.Split(' ');
                SessionStartTime = Convert.ToInt32(parts[0]);
                SessionStopTime = Convert.ToInt32(parts[1]);

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
                        RepeatTimes.Add(Convert.ToInt32(sdpLines));
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

            List<int> RepeatTimes = new List<int>();

            public override string ToString()
            {
                return "t=" + SessionStartTime.ToString() + " " + SessionStopTime.ToString() + CRLF;
            }
        }

        /// <summary>
        /// Represents the MediaDescription in a Session Description
        /// </summary>
        public class SessionMediaDescription
        {
            #region Fields

            public string MediaType;
            public int MediaPort;
            public string MediaProtocol;
            public string MediaFormat;

            Dictionary<string, string> m_Properties = new Dictionary<string, string>();

            Dictionary<string, string> m_BandwidthInformation = new Dictionary<string, string>();

            Dictionary<string, string> m_Attributes = new Dictionary<string, string>();

            #endregion

            //#region Properties

            //#endregion

            #region Methods

            public SessionMediaDescription(string[] sdpLines, ref int index)
            {
                string sdpLine = sdpLines[index++];

                if (!sdpLine.StartsWith("m=")) throw new SessionDescriptionException("Invalid Media Description");
                else sdpLine = sdpLine.Replace("m=", string.Empty);

                string[] parts = sdpLine.Split(' ');

                if(parts.Length != 4) throw new SessionDescriptionException("Invalid Media Name and Address");

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
                    else if (line.StartsWith("a"))
                    {
                        //Parse the attribute
                        line = line.Replace("a=", string.Empty);
                        parts = line.Split(':');
                        m_Attributes.Add(parts[0], CleanValue(parts[1]));
                    }
                    else if (line.StartsWith("b"))
                    {
                        //Parse the attribute
                        line = line.Replace("b=", string.Empty);
                        parts = line.Split(':');
                        m_BandwidthInformation.Add(parts[0], CleanValue(parts[1]));
                    }
                    else
                    {
                        //Parse the property
                        parts = line.Split('=');
                        if (parts.Length > 1) m_Properties.Add(parts[0], CleanValue(parts[1]));
                    }
                }            
            }

            public string GetProperty(string name)
            {
                if (m_Properties.ContainsKey(name)) return m_Properties[name];
                return null;
            }
            
            public void SetProperty(string name, string value)
            {
                if (m_Properties.ContainsKey(name)) m_Properties[name] = value;
                else m_Properties.Add(name, value);
            }

            public string GetAtttribute(string name)
            {
                if (m_Attributes.ContainsKey(name)) return m_Attributes[name];
                return null;
            }

            public void SetAttribute(string name, string value)
            {
                if (m_Attributes.ContainsKey(name)) m_Attributes[name] = value;
                else m_Attributes.Add(name, value);
            }

            public string GetBandwithInformation(string name)
            {
                if (m_BandwidthInformation.ContainsKey(name)) return m_BandwidthInformation[name];
                return null;
            }

            public void SetBandwithInformation(string name, string value)
            {
                if (m_BandwidthInformation.ContainsKey(name)) m_BandwidthInformation[name] = value;
                else m_BandwidthInformation.Add(name, value);
            }

            public override string ToString()
            {
                StringBuilder buffer = new StringBuilder();
                buffer.Append("m=" + MediaType + " " + MediaPort.ToString() + " " + MediaProtocol + " " + MediaFormat + CRLF);
                
                foreach (KeyValuePair<string, string> pair in m_Properties)
                    buffer.Append(pair.Key + '=' + pair.Value + CRLF);
             
                foreach (KeyValuePair<string, string> pair in m_BandwidthInformation)
                    buffer.Append("b=" + pair.Key + ':' + pair.Value + CRLF);

                foreach (KeyValuePair<string, string> pair in m_Attributes)
                    buffer.Append("a=" + pair.Key + ':' + pair.Value + CRLF);

                //End of Media Desc
                //buffer.Append(CRFL);                

                return buffer.ToString();
            }

            #endregion
        }

        #endregion

        #region Fields

        Dictionary<string, string> m_BandwidthInformation = new Dictionary<string, string>();

        Dictionary<string, string> m_Properties = new Dictionary<string, string>();

        Dictionary<string, string> m_Attributes = new Dictionary<string, string>();

        #endregion

        #region Properties

        public List<SessionTimeDescription> TimeDescriptions = new List<SessionTimeDescription>();

        public List<SessionMediaDescription> MediaDesciptions = new List<SessionMediaDescription>();

        public int Version { get { return Convert.ToInt32(GetProperty("v")); } set { SetProperty("v", value.ToString()); } }

        public string OriginatorAndSessionIdentifier { get { return GetProperty("o"); } set { SetProperty("o", value); } }

        public string SessionName { get { return GetProperty("s"); } set { SetProperty("s", value); } }

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
            string[] lines = sdpContents.Split('\n');
            int lineIndex = 0;

            if (lines.Length < 3) throw new SessionDescriptionException("Invalid Session Description");

            string[] parts = lines[lineIndex++].Split('=');

            if (parts[0] != "v") throw new SessionDescriptionException("Expected Protocol Version");
            else m_Properties.Add(parts[0], CleanValue(parts[1]));

            parts = lines[lineIndex++].Split('=');

            if (parts[0] != "o") throw new SessionDescriptionException("Expected Originator and Session Identifier");
            else m_Properties.Add(parts[0], CleanValue(parts[1]));

            parts = lines[lineIndex++].Split('=');

            if (parts[0] != "s") throw new SessionDescriptionException("Expected Session Name");
            else m_Properties.Add(parts[0], CleanValue(parts[1]));

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
                    m_Attributes.Add(parts[0], CleanValue(parts[1]));
                }
                else if (line.StartsWith("b="))
                {
                    //Parse the attribute
                    line = line.Replace("b=", string.Empty);
                    parts = line.Split(':');
                    SetBandwithInformation(parts[0], CleanValue(parts[1]));
                    //m_BandwidthInformation.Add(parts[0], CleanValue(parts[1]));
                }
                else
                {
                    //Parse the property
                    parts = line.Split('=');
                    if (parts.Length > 1) m_Properties.Add(parts[0], CleanValue(parts[1]));
                }
            }            

        }

        #endregion

        #region Methods

        public string GetProperty(string name)
        {
            if (m_Properties.ContainsKey(name)) return m_Properties[name];
            return null;
        }

        public void SetProperty(string name, string value)
        {
            if (m_Properties.ContainsKey(name)) m_Properties[name] = value;
            else m_Properties.Add(name, value);
        }

        public string GetAtttribute(string name)
        {
            if (m_Attributes.ContainsKey(name)) return m_Attributes[name];
            return null;
        }

        public void SetAttribute(string name, string value)
        {
            if (m_Attributes.ContainsKey(name)) m_Attributes[name] = value;
            else m_Attributes.Add(name, value);
        }

        public string GetBandwithInformation(string name)
        {
            if (m_BandwidthInformation.ContainsKey(name)) return m_BandwidthInformation[name];
            return null;
        }

        public void SetBandwithInformation(string name, string value)
        {
            if (m_BandwidthInformation.ContainsKey(name)) m_BandwidthInformation[name] = value;
            else m_BandwidthInformation.Add(name, value);
        }

        /// <summary>
        /// Copies all Attributes, Properties and Bandwidth inforamation to another SessionDescription.
        /// Will not copy the version, originator or session name.
        /// </summary>
        /// <param name="other">The SessionDescription to copy to</param>
        public void CopyTo(SessionDescription other)
        {
            if (Version != other.Version) throw new ArgumentException("Must be the same version", "other");

            foreach (KeyValuePair<string, string> pair in m_Properties)
            {
                //Dont copy all
                if (pair.Key == "v" || pair.Key == "o" || pair.Key == "s") continue;
                other.SetProperty(pair.Key, pair.Value);
            }

            foreach (KeyValuePair<string, string> pair in m_BandwidthInformation)
            {
                other.SetBandwithInformation(pair.Key, pair.Value);
            }

            if (TimeDescriptions.Count > 0)
            {
                foreach (SessionTimeDescription timeDescription in TimeDescriptions)
                    other.TimeDescriptions.Add(timeDescription);
            }

            foreach (KeyValuePair<string, string> pair in m_Attributes)
            {
                other.SetAttribute(pair.Key, pair.Value);
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
            foreach (KeyValuePair<string, string> pair in m_Properties)
                buffer.Append(pair.Key + '=' + pair.Value + CRLF);

            foreach (KeyValuePair<string, string> pair in m_BandwidthInformation)
                buffer.Append("b=" + pair.Key + ':' + pair.Value + CRLF);

            if (TimeDescriptions.Count > 0)
            {
                foreach (SessionTimeDescription timeDescription in TimeDescriptions)
                    buffer.Append(timeDescription.ToString());
            }

            foreach (KeyValuePair<string, string> pair in m_Attributes)
                buffer.Append("a=" + pair.Key + ':' + pair.Value + CRLF);

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
