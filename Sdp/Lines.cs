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
    namespace /*Media.Sdp.*/Lines
    {
        /// <summary>
        /// Represents an 'a=' <see cref="SessionDescriptionLine"/>
        /// </summary>
        public class SessionAttributeLine : SessionDescriptionLine
        {
            internal const char AttributeType = 'a';

            //Maybe better in a Parameters class...
            public sealed class AttributeFields
            {
                #region NestedTypes

                //Would be useful for meta programming where certain attributes can appear
                //[System.AttributeUsage(System.AttributeTargets.Class)]
                //public class AttributeType : System.Attribute
                //{
                //    [Flags]
                //    public enum Level
                //    {
                //        Session = 1,
                //        Media = 2,
                //    }

                //    public Level AllowedLevel;

                //    public AttributeType(Level level)
                //    {
                //        this.AllowedLevel = level;
                //    }
                //}

                #endregion

                #region RFC4566

                //Session and Media Level               

                public const string RecieveOnly = "recvonly";

                public const string SendReceive = "sendrecv";

                public const string SendOnly = "sendonly";

                public const string SdpLang = "sdplang";

                public const string Lang = "lang";

                #endregion

                #region RFC4145

                //Session and Media Level               

                public const string Setup = "setup";

                public const string Connection = "connection";

                #endregion

                #region RFC2326

                //Session and Media Level               

                //[RFC2326][RFC-ietf-mmusic-rfc2326bis-40]
                public const string Range = "range";

                public const string Control = "control";

                //RFC-ietf-mmusic-rfc2326bis-40]
                public const string MTag = "mtag";

                #endregion

                #region RFC2848

                //Session and Media Level               

                public const string PhoneContext = "phone-context";

                #endregion

                /* http://www.iana.org/assignments/sdp-parameters/sdp-parameters.xhtml#sdp-parameters-1
                att-field (both session and media level)	record	[RFC-ietf-siprec-protocol-18]
                att-field (both session and media level)	recordpref	[RFC-ietf-siprec-protocol-18]
                att-field (both session and media level)	rtcp-rgrp	[RFC-ietf-avtcore-rtp-multi-stream-optimisation-12]
                */
            }            

            public SessionAttributeLine(SessionDescriptionLine line)
                : base(line)
            {
                if (Type != AttributeType) throw new InvalidOperationException("Not a SessionAttributeLine line");
            }

            public SessionAttributeLine(string value)
                : base(AttributeType, SessionDescription.SpaceString)
            {
                Add(value);
            }
        }

        //https://tools.ietf.org/html/rfc4566 @ 5.8.  Bandwidth ("b=")
        /// <summary>
        /// Represents an 'b=' <see cref="SessionDescriptionLine"/>
        /// </summary>
        public class SessionBandwidthLine : SessionDescriptionLine
        {
            #region RFC3556 Bandwidth

            //X-YZ...

            //Max prefix..
            const string RecieveBandwidthToken = "RR", SendBandwdithToken = "RS", ApplicationSpecificBandwidthToken = "AS", ConferenceTotalBandwidthToken = "CT",
                IndependentApplicationSpecificBandwidthToken = "TIAS"; //http://tools.ietf.org/html/rfc3890

            const string BandwidthFormat = "b={0}:0";

            public static readonly Sdp.SessionDescriptionLine DisabledReceiveLine = new Sdp.SessionDescriptionLine(string.Format(BandwidthFormat, RecieveBandwidthToken));

            public static readonly Sdp.SessionDescriptionLine DisabledSendLine = new Sdp.SessionDescriptionLine(string.Format(BandwidthFormat, SendBandwdithToken));

            public static readonly Sdp.SessionDescriptionLine DisabledApplicationSpecificLine = new Sdp.SessionDescriptionLine(string.Format(BandwidthFormat, ApplicationSpecificBandwidthToken));

            public static readonly Sdp.SessionDescriptionLine DisabledIndependentApplicationSpecificLine = new Sdp.SessionDescriptionLine(string.Format(BandwidthFormat, IndependentApplicationSpecificBandwidthToken));

            public static readonly Sdp.SessionDescriptionLine DisabledConferenceTotalLine = new Sdp.SessionDescriptionLine(string.Format(BandwidthFormat, ConferenceTotalBandwidthToken));

            public static bool TryParseBandwidthLine(Media.Sdp.SessionDescriptionLine line, out int result)
            {
                string token;

                return TryParseBandwidthLine(line, out token, out result);
            }

            public static bool TryParseBandwidthLine(Media.Sdp.SessionDescriptionLine line, out string token, out int result)
            {
                token = string.Empty;

                result = -1;

                if (line == null || line.Type != Sdp.Lines.SessionBandwidthLine.BandwidthType || line.m_Parts.Count <= 0) return false;

                string[] tokens = line.m_Parts[0].Split(Media.Sdp.SessionDescription.ColonSplit, StringSplitOptions.RemoveEmptyEntries);

                if (tokens.Length < 2) return false;

                token = tokens[0];

                return int.TryParse(tokens[1], out result);
            }

            public static bool TryParseRecieveBandwidth(Media.Sdp.SessionDescriptionLine line, out int result)
            {
                result = -1;

                if (line == null || line.Type != Sdp.Lines.SessionBandwidthLine.BandwidthType) return false;

                if (line.m_Parts.Count <= 0 || false == line.m_Parts[0].StartsWith(RecieveBandwidthToken, StringComparison.OrdinalIgnoreCase)) return false;

                return TryParseBandwidthLine(line, out result);
            }

            public static bool TryParseSendBandwidth(Media.Sdp.SessionDescriptionLine line, out int result)
            {
                result = -1;

                if (line == null || line.Type != Sdp.Lines.SessionBandwidthLine.BandwidthType) return false;

                if (line.m_Parts.Count <= 0 || false == line.m_Parts[0].StartsWith(SendBandwdithToken, StringComparison.OrdinalIgnoreCase)) return false;

                return TryParseBandwidthLine(line, out result);
            }

            public static bool TryParseApplicationSpecificBandwidth(Media.Sdp.SessionDescriptionLine line, out int result)
            {
                result = -1;

                if (line == null || line.Type != Sdp.Lines.SessionBandwidthLine.BandwidthType) return false;

                if (false == line.m_Parts[0].StartsWith(ApplicationSpecificBandwidthToken, StringComparison.OrdinalIgnoreCase)) return false;

                return TryParseBandwidthLine(line, out result);
            }

            public static bool TryParseIndependentApplicationSpecificBandwidth(Media.Sdp.SessionDescriptionLine line, out int result)
            {
                result = -1;

                if (line == null || line.Type != Sdp.Lines.SessionBandwidthLine.BandwidthType) return false;

                if (false == line.m_Parts[0].StartsWith(IndependentApplicationSpecificBandwidthToken, StringComparison.OrdinalIgnoreCase)) return false;

                return TryParseBandwidthLine(line, out result);
            }

            public static bool TryParseBandwidthDirectives(Media.Sdp.MediaDescription mediaDescription, out int rrDirective, out int rsDirective, out int asDirective) //tiasDirective
            {
                rrDirective = rsDirective = asDirective = -1;

                if (mediaDescription == null) return false;

                int parsed = -1;

                string token = string.Empty;

                //Iterate all BandwidthLines
                foreach (Media.Sdp.SessionDescriptionLine line in mediaDescription.BandwidthLines)
                {
                    //If the line was parsed as a bandwidth line then determine the type of bandwidth line parsed and assign the value of the directive.
                    if (TryParseBandwidthLine(line, out token, out parsed))
                    {

                        //Anyparsed = true;

                        switch (token)
                        {
                            case RecieveBandwidthToken:
                                rrDirective = parsed;
                                continue;
                            case SendBandwdithToken:
                                rsDirective = parsed;
                                continue;
                            case IndependentApplicationSpecificBandwidthToken: //should have it's own out value...
                            case ApplicationSpecificBandwidthToken:
                                asDirective = parsed;
                                continue;

                        }
                    }
                }

                //should actually be returning true if any bandwidth tokens were parsed... Anyparsed...

                //Determine if rtcp is disabled
                return parsed >= 0;
            }

            //Overload for SessionDescription (which can fallback to use the MediaDescription overload above)

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

            //KeyValuePair string t,int b ^
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

            /// <summary>
            /// 
            /// </summary>
            internal string ConnectionNetworkType { get { return GetPart(0); } set { SetPart(0, value); } }

            /// <summary>
            /// 
            /// </summary>
            internal string ConnectionAddressType { get { return GetPart(1); } set { SetPart(1, value); } }

            /// <summary>
            /// See 5.7.  Connection Data ("c=")
            /// </summary>
            internal string ConnectionAddress { get { return GetPart(2); } set { SetPart(2, value); } }

            //HasOptionalSubFields...
            internal bool HasMultipleAddresses
            {
                get
                {
                    return ConnectionAddress.Contains((char)Common.ASCII.ForwardSlash);
                }
            }

            //Todo
            //Only split the ConnectionAddress one time and cache it
            //These are sub fields of a single datum
            string[] m_ConnectionParts;

            /// <summary>
            /// Gets the parts of the <see cref="ConnectionAddress"/>
            /// </summary>
            public IEnumerable<string> ConnectionParts
            {
                get
                {
                    if (m_ConnectionParts != null) return m_ConnectionParts;

                    if (string.IsNullOrWhiteSpace(ConnectionAddress)) return Enumerable.Empty<string>();

                    return m_ConnectionParts = ConnectionAddress.Split(SessionDescription.SlashSplit, 3);
                }
            }

            /// <summary>
            /// 
            /// </summary>
            public string IPAddress
            {
                get
                {
                    if (string.IsNullOrWhiteSpace(ConnectionAddress)) return null;

                    if (m_ConnectionParts == null) m_ConnectionParts = ConnectionAddress.Split(SessionDescription.SlashSplit, 3);

                    //Should verify that the string contains a . and is not shorter/longer than x, y...
                    return m_ConnectionParts[0];
                }
            }

            /// <summary>
            /// 
            /// </summary>
            public int? Hops
            {
                get
                {
                    if (string.IsNullOrWhiteSpace(ConnectionAddress)) return null;

                    if (m_ConnectionParts == null) m_ConnectionParts = ConnectionAddress.Split(SessionDescription.SlashSplit, 3);

                    if (m_ConnectionParts.Length > 2)
                    {
                        return int.Parse(m_ConnectionParts[1], System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture);
                    }

                    return null;
                }
            }

            /// <summary>
            /// 
            /// </summary>
            public int? Ports
            {
                get
                {
                    if (string.IsNullOrWhiteSpace(ConnectionAddress)) return null;

                    if (m_ConnectionParts == null) m_ConnectionParts = ConnectionAddress.Split(SessionDescription.SlashSplit, 3);

                    if (m_ConnectionParts.Length > 2) //Todo ensure not accidentally giving Hops... should proably be 3
                    {
                        return int.Parse(m_ConnectionParts[m_ConnectionParts.Length - 1], System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture);
                    }

                    return null;
                }
            }

            #region Constructor

            public SessionConnectionLine(SessionDescriptionLine line)
                : this()
            {
                if (line.Type != ConnectionType) throw new InvalidOperationException("Not a SessionConnectionLine");

                m_Parts.Clear();

                //Needs to parse with seperator (ToString is hacked up to prevent this from mattering)
                if (line.m_Parts.Count == 1)
                {
                    string temp = line.m_Parts[0];

                    m_Parts.AddRange(temp.Split(SessionDescription.Space));
                }
                else m_Parts.AddRange(line.m_Parts);

                EnsureParts(3);
            }


            public SessionConnectionLine()
                : base(ConnectionType, SessionDescription.SpaceString, 3)
            {

            }

            public SessionConnectionLine(string[] sdpLines, ref int index)
                : this()
            {
                try
                {
                    string sdpLine = sdpLines[index++].Trim();

                    if (string.IsNullOrWhiteSpace(sdpLine) 
                        || 
                        sdpLine[0] != ConnectionType) Media.Common.Extensions.Exception.ExceptionExtensions.RaiseTaggedException(this, "Invalid SessionConnectionLine");

                    sdpLine = SessionDescription.TrimLineValue(sdpLine.Substring(2));

                    //m_Parts.Add(sdpLine);

                    //Could loop split results and call SetPart(i, splits[i])

                    m_Parts.Clear();

                    m_Parts.AddRange(sdpLine.Split(SessionDescription.Space));

                    EnsureParts(3);
                }
                catch
                {
                    throw;
                }
            }

            #endregion

            //ToString should be implemented by GetEnumerator and String.Join(m_Seperator

            public override string ToString()
            {
                return ConnectionType.ToString() + Media.Sdp.SessionDescription.EqualsSign + 
                    (m_Parts.Count == 1 ? ConnectionNetworkType : string.Join(SessionDescription.SpaceString, ConnectionNetworkType, ConnectionAddressType, ConnectionAddress))
                    + SessionDescription.NewLineString;
            }

            //Should override GetEnumerator to include ConnectionParts.
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

                    if (sdpLine[0] != VersionType) Media.Common.Extensions.Exception.ExceptionExtensions.RaiseTaggedException(this, "Invalid SessionVersionLine Line");

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
        public class SessionOriginLine : SessionDescriptionLine
        {
            internal const char OriginType = 'o';

            public string Username
            {
                get { return GetPart(0); }
                set { SetPart(0, value); }
            }

            //Should not be a string?
            public string SessionId
            {
                get { return GetPart(1); }
                set { SetPart(1, value); }
            }

            //Should be double...
            //public long SessionId
            //{
            //    get
            //    {
            //        string part = GetPart(1);

            //        if (string.IsNullOrWhiteSpace(part)) return 0;

            //        return (long)double.Parse(part);

            //        //return part[0] == Common.ASCII.HyphenSign ? long.Parse(part) : (long)ulong.Parse(part);
            //    }
            //    set { SetPart(1, value.ToString()); }
            //}

            public long SessionVersion
            {
                get
                {
                    string part = GetPart(2);

                    if (string.IsNullOrWhiteSpace(part)) return 0;

                    //Range ...
                    //return (long)double.Parse(part);

                    return part[0] == Common.ASCII.HyphenSign ? long.Parse(part) : (long)ulong.Parse(part);
                }
                set { SetPart(2, value.ToString()); }
            }

            public string NetworkType
            {
                get { return GetPart(3); }
                set { SetPart(3, value); }
            }

            public string AddressType
            {
                get { return GetPart(4); }
                set { SetPart(4, value); }
            }

            //UnicastAddress is what it's called

            //Should have a Uri property?

            /// <summary>
            /// The address of the machine from which the session was created. 
            /// For an address type of IP4, this is either:
            /// The fully qualified domain name of the machine or the dotted-
            /// decimal representation of the IP version 4 address of the machine.
            /// For an address type of IP6, this is either:
            /// The fully qualified domain name of the machine or the compressed textual
            /// representation of the IP version 6 address of the machine. 
            /// For both IP4 and IP6, the fully qualified domain name is the form that
            /// SHOULD be given unless this is unavailable, in which case the
            /// globally unique address MAY be substituted.  A local IP address
            /// MUST NOT be used in any context where the SDP description might
            /// leave the scope in which the address is meaningful (for example, a
            /// local address MUST NOT be included in an application-level
            /// referral that might leave the scope).
            /// </summary>
            public string Address { get { return GetPart(5); } set { SetPart(5, value); } }

            public SessionOriginLine()
                : base(OriginType, SessionDescription.SpaceString, 6)
            {
                //while (m_Parts.Count < 6) m_Parts.Add(string.Empty);
                //Username = string.Empty;
            }

            public SessionOriginLine(SessionDescriptionLine line)
                : this()
            {
                if (line.Type != OriginType) throw new InvalidOperationException("Not a SessionOriginLine");

                m_Parts.Clear();

                //Needs to parse with seperator (ToString is hacked up to prevent this from mattering)
                if (line.m_Parts.Count == 1)
                {
                    string temp = line.m_Parts[0];

                    m_Parts.AddRange(temp.Split(SessionDescription.Space));
                }
                else m_Parts.AddRange(line.m_Parts);

                EnsureParts(6);
            }

            public SessionOriginLine(string owner)
                : base(OriginType)
            {
                if (string.IsNullOrWhiteSpace(owner)) goto EnsureParts;
                
                //Check for missing o=? (Adds overhead)
                if (owner[0] != OriginType)
                {
                    //Replace NewLine? TrimLineValue
                    m_Parts.AddRange(owner.Split(SessionDescription.Space));
                }
                else m_Parts.AddRange(owner.Substring(2).Replace(SessionDescription.NewLineString, string.Empty).Split(SessionDescription.Space));

            EnsureParts:
                EnsureParts(6);

                //if (m_Parts.Count < 6)
                //{
                //    //EnsureParts(6);

                //    //Make a new version if anything was added.
                //    //SessionVersion++;
                //}
            }

            public SessionOriginLine(string[] sdpLines, ref int index)
                : this()
            {
                try
                {
                    string sdpLine = sdpLines[index++].Trim();

                    if (sdpLine[0] != OriginType) Media.Common.Extensions.Exception.ExceptionExtensions.RaiseTaggedException(this, "Invalid SessionOriginatorLine");

                    sdpLine = SessionDescription.TrimLineValue(sdpLine.Substring(2));

                    m_Parts.Clear();

                    m_Parts.AddRange(sdpLine.Split(SessionDescription.Space));
                }
                catch
                {
                    throw;
                }
            }

            public override string ToString()
            {
                return OriginType.ToString() + Media.Sdp.SessionDescription.EqualsSign + 
                    string.Join(SessionDescription.SpaceString, Username, SessionId, SessionVersion, NetworkType, AddressType, Address) 
                    + SessionDescription.NewLineString;
            }

        }

        /// <summary>
        /// Represents an 's=' <see cref="SessionDescriptionLine"/>
        /// </summary>
        public class SessionNameLine : SessionDescriptionLine
        {

            internal const char NameType = 's';

            public string SessionName
            {
                get
                {
                    return m_Parts.Count > 0 ? m_Parts[0] : string.Empty;
                }

                set
                {
                    //if (m_Parts.Count > 0) m_Parts[0] = value;
                    //else m_Parts.Add(value);

                    //No branch

                    m_Parts.Clear(); 
                    
                    m_Parts.Add(value);
                }
            }

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

                    if (sdpLine[0] != NameType) Media.Common.Extensions.Exception.ExceptionExtensions.RaiseTaggedException(this, "Invalid SessionNameLine");

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
                return NameType.ToString() + Media.Sdp.SessionDescription.EqualsSign + (string.IsNullOrEmpty(SessionName) ? string.Empty : SessionName) + SessionDescription.NewLineString;
            }
        }

        /// <summary>
        /// Represents an 'i=' <see cref="SessionDescriptionLine"/>
        /// </summary>
        public class SessionInformationLine : SessionDescriptionLine
        {

            internal const char InformationType = 'i';

            public string SessionName
            {
                get { return m_Parts.Count > 0 ? m_Parts[0] : string.Empty; }
                set { m_Parts.Clear(); m_Parts.Add(value); }
            }

            public SessionInformationLine()
                : base(InformationType)
            {

            }

            public SessionInformationLine(SessionDescriptionLine line)
                : base(line)
            {
                if (Type != InformationType) throw new InvalidOperationException("Not a SessionInformationLine line");
            }

            public SessionInformationLine(string sessionName)
                : this()
            {
                SessionName = sessionName;
            }

            public SessionInformationLine(string[] sdpLines, ref int index)
                : this()
            {
                try
                {
                    string sdpLine = sdpLines[index++].Trim();

                    if (sdpLine[0] != InformationType) Media.Common.Extensions.Exception.ExceptionExtensions.RaiseTaggedException(this, "Invalid SessionInformationLine");

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
                return InformationType.ToString() + Media.Sdp.SessionDescription.EqualsSign + (string.IsNullOrEmpty(SessionName) ? string.Empty : SessionName) + SessionDescription.NewLineString;
            }
        }

        /// <summary>
        /// Represents an 'p=' <see cref="SessionDescriptionLine"/>
        /// </summary>
        public class SessionPhoneNumberLine : SessionDescriptionLine
        {

            internal const char PhoneType = 'p';

            public string PhoneNumber { get { return m_Parts.Count > 0 ? m_Parts[0] : string.Empty; } set { m_Parts.Clear(); m_Parts.Add(value); } }

            public SessionPhoneNumberLine()
                : base(PhoneType)
            {

            }

            public SessionPhoneNumberLine(SessionDescriptionLine line)
                : base(line)
            {
                if (Type != PhoneType) throw new InvalidOperationException("Not a SessionPhoneNumberLine");
            }

            public SessionPhoneNumberLine(string sessionName)
                : this()
            {
                PhoneNumber = sessionName;
            }

            public SessionPhoneNumberLine(string[] sdpLines, ref int index)
                : this()
            {
                try
                {
                    string sdpLine = sdpLines[index++].Trim();

                    if (sdpLine[0] != PhoneType) Media.Common.Extensions.Exception.ExceptionExtensions.RaiseTaggedException(this, "Invalid SessionPhoneNumberLine");

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
                return PhoneType.ToString() + Media.Sdp.SessionDescription.EqualsSign + (string.IsNullOrEmpty(PhoneNumber) ? string.Empty : PhoneNumber) + SessionDescription.NewLineString;
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

                    if (sdpLine[0] != EmailType) Media.Common.Extensions.Exception.ExceptionExtensions.RaiseTaggedException(this, "Invalid SessionEmailLine");

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
                return EmailType.ToString() + Media.Sdp.SessionDescription.EqualsSign + (string.IsNullOrEmpty(Email) ? string.Empty : Email) + SessionDescription.NewLineString;
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

                    if (sdpLine[0] != LocationType) Media.Common.Extensions.Exception.ExceptionExtensions.RaiseTaggedException(this, "Invalid SessionUriLine");

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
        /// Represents an 'z=' <see cref="SessionDescriptionLine"/>
        /// </summary>
        public class SessionTimeZoneLine : SessionDescriptionLine
        {
            internal const char TimeZoneType = 'z';

            //AdjustmentValuesCount?
            /// <summary>
            /// Gets the amount of adjustment times and offsets found in the parts of the line.
            /// </summary>
            /// <remarks>An adjustment time consists of a date and offset pair</remarks>
            public int AdjustmentTimesCount
            {
                get
                {
                    return m_Parts.Count + 1 >> 1;
                }
            }

            /// <summary>
            /// Gets the decimal representation (in seconds since the 1900 utc epoch) of the values stored on the line.
            /// </summary>
            public IEnumerable<double> AdjustmentValues
            {
                get
                {
                    foreach (var part in m_Parts)
                    {
                        yield return SessionDescription.ParseTime(part).TotalSeconds;
                    }
                }
                set
                {
                    //Remove existing entries
                    m_Parts.Clear();

                    //Enumerate the values selecting the ToString of the each value, add the range to m_Parts
                    m_Parts.AddRange(value.Select(v => v.ToString()));
                }
            }

            /// <summary>
            /// Gets a start or end date assoicated with the adjustmentIndex.
            /// Even values would retrieve a start date and Odd values would retrieve an end date.
            /// </summary>
            /// <param name="adjustmentIndex">The 0 based index of the adjustment date</param>
            /// <returns>The DateTime assoicated with the Adjustment Date at the specified index.</returns>
            public DateTime GetAdjustmentDate(int adjustmentIndex)
            {
                //Get the correct index
                adjustmentIndex <<= 1;// * 2

                if (adjustmentIndex >= m_Parts.Count) return Ntp.NetworkTimeProtocol.UtcEpoch1900;// DateTime.MinValue;

                return Ntp.NetworkTimeProtocol.UtcEpoch1900.Add(SessionDescription.ParseTime(m_Parts[adjustmentIndex]));
            }

            /// <summary>
            /// Returns the TimeSpan assoicated with the Adjustment Offset at the specified index.
            /// Even values would retrieve a start date and odd values would retrieve an end dat.
            /// </summary>
            /// <param name="adjustmentIndex">The Adjustment Offset index (Always 1 + the Adjustment Date Index)</param>
            /// <returns>The TimeSpan assoiciated with the Adjustment Offset at the specified index.</returns>
            public TimeSpan GetAdjustmentOffset(int adjustmentIndex)
            {
                //Get the correct index
                adjustmentIndex <<= 1;// * 2

                //Offsets are the value following the adjustmentIndex
                if (Common.Binary.IsEven(ref adjustmentIndex)) ++adjustmentIndex;
                
                //Ensure valid index
                if (adjustmentIndex >= m_Parts.Count) return TimeSpan.Zero;

                //Parse value as a time in seconds.
                return SessionDescription.ParseTime(m_Parts[adjustmentIndex]);
            }

            //public DateTimeOffset GetAdjustmentDateTimeOffset(int adjustmentIndex)
            //{
            //    return new DateTimeOffset(GetAdjustmentDate(adjustmentIndex).ToLocalTime(), GetAdjustmentOffset(adjustmentIndex));
            //}

            //Enumerator methods for DateTime or DateTimeOffset which calls the above with the index as necessary.

            #region Constructor

            public SessionTimeZoneLine()
                : base(TimeZoneType, SessionDescription.SpaceString)
            {

            }

            public SessionTimeZoneLine(SessionDescriptionLine line)
                : this()
            {
                if (line.Type != TimeZoneType) throw new InvalidOperationException("Not a SessionTimeZoneLine line");

                if (line.m_Parts.Count == 1)
                {
                    string temp = line.m_Parts[0];

                    m_Parts.AddRange(temp.Split(SessionDescription.Space));
                }
                else m_Parts.AddRange(line.m_Parts);
            }

            public SessionTimeZoneLine(string[] sdpLines, ref int index)
                : this()
            {
                try
                {
                    string sdpLine = sdpLines[index++].Trim();

                    if (string.IsNullOrWhiteSpace(sdpLine) 
                        ||
                        sdpLine[0] != TimeZoneType) Media.Common.Extensions.Exception.ExceptionExtensions.RaiseTaggedException(this, "Invalid SessionTimeZoneLine");

                    sdpLine = SessionDescription.TrimLineValue(sdpLine.Substring(2));

                    m_Parts.AddRange(sdpLine.Split(SessionDescription.Space));
                }
                catch
                {
                    throw;
                }
            }

            #endregion

            #region Add

            public void Add(DateTime dateTime, TimeSpan offset)
            {
                m_Parts.Add((dateTime - Ntp.NetworkTimeProtocol.UtcEpoch1900).TotalSeconds.ToString());

                m_Parts.Add(offset.TotalSeconds.ToString());
            }

            public void Add(DateTime startTime, TimeSpan startOffset, DateTime endTime, TimeSpan endOffset)
            {
                Add(startTime, startOffset);

                Add(endTime, endOffset);
            }

            public void Add(DateTimeOffset dateTime)
            {
                m_Parts.Add((dateTime - Ntp.NetworkTimeProtocol.UtcEpoch1900).TotalSeconds.ToString());

                m_Parts.Add(dateTime.Offset.TotalSeconds.ToString());
            }

            public void Add(DateTimeOffset start, DateTimeOffset end)
            {
                Add(start);

                Add(end);
            }

            #endregion

            #region Remove

            //Rather than a Date based api just allow the start or end time to be removed?
            //These dates don't have to be in any order and although the end time should occur after the start time who really knows...

            //Bigger question is what is the use case of removing a start or end time?
            //Chaning existing values may be more suitable..

            #endregion

            public override string ToString()
            {
                return TimeZoneType.ToString() + SessionDescription.EqualsSign + string.Join(SessionDescription.SpaceString, m_Parts) + SessionDescription.NewLine;
            }
        }

        //Waiting to redo composite type

        public class SessionMediaDescriptionLine : SessionDescriptionLine
        {
            internal const char MediaDescriptionType = 'm';

            public MediaType MediaType
            {
                get { return (MediaType)Enum.Parse(typeof(MediaType), GetPart(0).Split(SessionDescription.Space).First().ToLowerInvariant()); }
                set { SetPart(0, value.ToString()); }
            }

            public int MediaPort
            {
                get { return int.Parse(GetPart(1), System.Globalization.CultureInfo.InvariantCulture); }
                set
                {
                    if (value < 0 || value > ushort.MaxValue) throw new ArgumentOutOfRangeException("The port value cannot be less than 0 or exceed 65535");

                    SetPart(1, value.ToString());
                }
            }

            public bool HasPortRange
            {
                get { return GetPart(1).IndexOf(SessionDescription.ForwardSlashString) >= 0; }
            }

            public IEnumerable<int> PortRange
            {
                get
                {
                    if (false == HasPortRange) yield break;

                    foreach (var part in GetPart(1).Split(SessionDescription.ForwardSlash).Skip(1))
                    {
                        if (string.IsNullOrWhiteSpace(part)) continue;

                        yield return int.Parse(part);
                    }
                }
                set
                {
                    //Should verify value to have values or return

                    //Take the first value which is the MediaPort and then add / and the new values
                    if (HasPortRange) SetPart(1, string.Join(GetPart(1).Split(SessionDescription.ForwardSlash).First() + SessionDescription.ForwardSlash, value));
                    else SetPart(1, string.Join(GetPart(1) + SessionDescription.ForwardSlash, string.Join(SessionDescription.SpaceString, value)));
                }
            }

            public IEnumerable<int> MediaPorts
            {
                get
                {
                    yield return MediaPort;

                    foreach (var port in PortRange) yield return port;
                }
                set
                {
                    MediaPort = value.First();

                    PortRange = value.Skip(1);
                }
            }

            string MediaProtocol
            {
                get { return GetPart(2); }
                set { SetPart(2, value); }
            }

            string MediaFormat
            {
                get { return GetPart(3); }
                set { SetPart(3, value); }
            }

            public IEnumerable<int> PayloadTypes
            {
                get
                {
                    foreach (var part in MediaFormat.Split(SessionDescription.Space))
                    {
                        if (string.IsNullOrWhiteSpace(part)) continue;

                        yield return int.Parse(part);
                    }
                }
                set
                {

                    int start = 3;

                    foreach (var port in value)
                    {
                        SetPart(start++, port.ToString());
                    }
                }
            }

            public SessionMediaDescriptionLine()
                : base(MediaDescriptionType, SessionDescription.SpaceString, 4)
            {

            }

            public SessionMediaDescriptionLine(SessionDescriptionLine line)
                : base(line)
            {
                if (Type != MediaDescriptionType) throw new InvalidOperationException("Not a SessionMediaDescriptionLine line");

                if (line.m_Parts.Count == 1)
                {
                    string temp = line.m_Parts[0];

                    m_Parts.AddRange(temp.Split(SessionDescription.Space));
                }
                else m_Parts.AddRange(line.m_Parts);
            }

            public SessionMediaDescriptionLine(string[] sdpLines, ref int index)
                : base(sdpLines, ref index, SessionDescription.SpaceString, MediaDescriptionType, 4)
            {
                
            }

            //internal...
            public SessionMediaDescriptionLine(string text)
                : base(text, SessionDescription.SpaceString)
            {
                if (Type != MediaDescriptionType) throw new InvalidOperationException("Not a SessionMediaDescriptionLine line");
            }
        }

        /// <summary>
        /// Represents an 't=' <see cref="SessionDescriptionLine"/>
        /// </summary>
        public class SessionTimeDescriptionLine : SessionDescriptionLine
        {
            internal const char TimeType = 't';
            
            public static SessionTimeDescriptionLine Permanent = new SessionTimeDescriptionLine(0, 0);

            #region Constructor

            public SessionTimeDescriptionLine()
                : base(TimeType, SessionDescription.SpaceString)
            {

            }

            public SessionTimeDescriptionLine(long startTime, long stopTime)
                :this()
            {
                StartTime = startTime;

                StopTime = stopTime;
            }

            public SessionTimeDescriptionLine(TimeSpan startTime, TimeSpan stopTime)
                : this()
            {
                StartTimeSpan = startTime;

                StopTimeSpan = stopTime;
            }

            public SessionTimeDescriptionLine(DateTime startDate, DateTime stopDate)
                : this()
            {
                NtpStartDateTime = startDate;

                NtpStopDateTime = stopDate;
            }

            #endregion

            #region Properties

            /// <summary>
            /// Gets or sets the start time.
            /// If seto to 0 then the session is not bounded,  though it will not become active until after the <see cref="StartTime"/>.  
            /// </summary>
            /// <remarks>These values are the decimal representation of Network Time Protocol (NTP) time values in seconds since 1900 </remarks>
            public long StartTime
            {
                get { return (long)StartTimeSpan.TotalSeconds; }
                set { SetPart(0, value.ToString()); }
            }

            /// <summary>
            /// TimeSpan representation of <see cref="StartTime"/>
            /// </summary>
            public TimeSpan StartTimeSpan
            {
                get { return SessionDescription.ParseTime(GetPart(0)); }
                set { SetPart(0, value.ToString()); }//Format
            }

            /// <summary>
            /// Gets or sets the stop time.
            /// If set to 0 and the <see cref="StartTime"/> is also zero, the session is regarded as permanent.
            /// </summary>
            /// <remarks>These values are the decimal representation of Network Time Protocol (NTP) time values in seconds since 1900 </remarks>
            public long StopTime
            {
                get { return (long)StopTimeSpan.TotalSeconds; }
                set { SetPart(1, value.ToString()); }
            }

            /// <summary>
            /// TimeSpan representation of <see cref="StopTime"/>
            /// </summary>
            public TimeSpan StopTimeSpan
            {
                get { return SessionDescription.ParseTime(GetPart(1)); }
                set { SetPart(1, value.ToString()); }//Format
            }

            //StartDatTime should be enough..
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
            public bool IsPermanent { get { return StartTimeSpan == TimeSpan.Zero && StopTimeSpan == TimeSpan.Zero; } }

            #endregion
        }

        /// <summary>
        /// Represents an 'r=' <see cref="SessionDescriptionLine"/>
        /// </summary>
        public class SessionRepeatTimeLine : SessionDescriptionLine
        {
            internal const char RepeatType = 'r';

            /// <summary>
            /// Gets or sets the the TimeSpan representation of each part of the line
            /// </summary>
            public IEnumerable<TimeSpan> RepeatTimes
            {
                get
                {
                    foreach (var part in m_Parts)
                    {
                        yield return SessionDescription.ParseTime(part);
                    }
                }
                set
                {
                    RepeatValues = value.Select(v => v.TotalSeconds);
                }
            }

            /// <summary>
            /// Gets or sets the decimal representation (in seconds since the 1900 utc epoch) of the values stored on the line.
            /// </summary>
            public IEnumerable<double> RepeatValues
            {
                get
                {
                    foreach (var repeatTime in RepeatTimes)
                    {
                        yield return repeatTime.TotalSeconds;
                    }
                }
                set
                {
                    //Remove existing entries
                    m_Parts.Clear();

                    //Enumerate the values selecting the ToString of the each value, add the range to m_Parts
                    m_Parts.AddRange(value.Select(v => v.ToString()));
                }
            }

            #region Constructor

            public SessionRepeatTimeLine()
                : base(RepeatType, SessionDescription.SpaceString)
            {

            }

            public SessionRepeatTimeLine(SessionDescriptionLine line)
                : this()
            {
                if (line.Type != RepeatType) throw new InvalidOperationException("Not a SessionRepeatTimeLine line");

                if (line.m_Parts.Count == 1)
                {
                    string temp = line.m_Parts[0];

                    m_Parts.AddRange(temp.Split(SessionDescription.Space));
                }
                else m_Parts.AddRange(line.m_Parts);
            }

            public SessionRepeatTimeLine(string[] sdpLines, ref int index)
                : this()
            {
                try
                {
                    string sdpLine = sdpLines[index++].Trim();

                    if (string.IsNullOrWhiteSpace(sdpLine) 
                        ||
                        sdpLine[0] != RepeatType) Media.Common.Extensions.Exception.ExceptionExtensions.RaiseTaggedException(this, "Invalid SessionRepeatTimeLine");

                    sdpLine = SessionDescription.TrimLineValue(sdpLine.Substring(2));

                    m_Parts.AddRange(sdpLine.Split(SessionDescription.Space));
                }
                catch
                {
                    throw;
                }
            }

            //internal...
            public SessionRepeatTimeLine(string text)
                : this()
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(text)) Media.Common.Extensions.Exception.ExceptionExtensions.RaiseTaggedException(this, "Invalid SessionRepeatTimeLine");
                        
                    //sdpFormat
                    if(text[0] == RepeatType)
                    {

                        text = SessionDescription.TrimLineValue(text.Substring(2));
                    }
                    
                    m_Parts.AddRange(text.Split(SessionDescription.Space));
                }
                catch
                {
                    throw;
                }

            }

            #endregion
        }

        //RtpMapAttributeLine : AttributeLine

        //FormatTypeLine : AttributeLine
    }
}
