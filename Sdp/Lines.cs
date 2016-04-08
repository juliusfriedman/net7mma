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

            //When GetPart(0) contains ':' the AttributeName does not include the ':' or any characters after it.

            //public string AttributeName
            //{
            //    get { return GetPart(0).Split(SessionDescription.Colon)[0]; }
            //    set { SetPart(0, string.Join(SessionDescription.Colon.ToString(), value)); }
            //}

            public SessionAttributeLine(SessionDescriptionLine line)
                : base(line)
            {
                if (m_Type != AttributeType) throw new InvalidOperationException("Not a SessionAttributeLine line");
            }

            //Assuming given a value NOT a line text to parse...
            public SessionAttributeLine(string value)
                : base(AttributeType, SessionDescription.SpaceString)
            {
                Add(value);
            }

            //Should have params values overload with optional seperator

            public SessionAttributeLine(string[] sdpLines, ref int index)
                : base(sdpLines, ref index, SessionDescription.SpaceString, AttributeType) { }

            //If going to use SpaceString then should offer a property to get parts for dervived types which wish to use ':'
            //E.g. KeyValuePair<string, string> GetValues

            //Should check or charset or sdpland attribute and switch currentEncoding. ( would be a compositive line then)
        }

        //SdpLangAttribute a=sdplang:<language tag>

        //CharsetAttribute  a=charset:<character set>

        //LandAttribute a=lang:<language tag>

        //https://tools.ietf.org/html/rfc4566 @ 5.8.  Bandwidth ("b=")
        /// <summary>
        /// Represents an 'b=' <see cref="SessionDescriptionLine"/>
        /// </summary>
        public class SessionBandwidthLine : SessionDescriptionLine
        {
            #region Statics

            /// <summary>
            /// Used to indicate disabled.
            /// </summary>
            const int DisabledValue = 0;

            //Types registered with IANA
            const string RecieveBandwidthToken = "RR", SendBandwdithToken = "RS", ApplicationSpecificBandwidthToken = "AS", ConferenceTotalBandwidthToken = "CT",
                IndependentApplicationSpecificBandwidthToken = "TIAS"; //http://tools.ietf.org/html/rfc3890

            //const string BandwidthFormat = "b={0}:{1}";

            //Calling Add will change the values of these lines. (Add is internal)
            //Must enforce with IUpdateable which is always under modification.

            public static readonly Sdp.SessionDescriptionLine DisabledReceiveLine = SessionBandwidthLine.Disabled(RecieveBandwidthToken);//new Sdp.SessionDescriptionLine(string.Format(BandwidthFormat, RecieveBandwidthToken));

            public static readonly Sdp.SessionDescriptionLine DisabledSendLine = SessionBandwidthLine.Disabled(SendBandwdithToken);//new Sdp.SessionDescriptionLine(string.Format(BandwidthFormat, SendBandwdithToken));

            public static readonly Sdp.SessionDescriptionLine DisabledApplicationSpecificLine = SessionBandwidthLine.Disabled(ApplicationSpecificBandwidthToken); //new Sdp.SessionDescriptionLine(string.Format(BandwidthFormat, ApplicationSpecificBandwidthToken));

            public static readonly Sdp.SessionDescriptionLine DisabledConferenceTotalLine = SessionBandwidthLine.Disabled(ConferenceTotalBandwidthToken); //new Sdp.SessionDescriptionLine(string.Format(BandwidthFormat, ConferenceTotalBandwidthToken));

            public static readonly Sdp.SessionDescriptionLine DisabledIndependentApplicationSpecificLine = SessionBandwidthLine.Disabled(IndependentApplicationSpecificBandwidthToken);//new Sdp.SessionDescriptionLine(string.Format(BandwidthFormat, IndependentApplicationSpecificBandwidthToken));

            public static SessionBandwidthLine Disabled(string token)
            {
                return new SessionBandwidthLine(token, DisabledValue);
            }

            public static bool TryParseBandwidthLine(Media.Sdp.SessionDescriptionLine line, out int result)
            {
                string token;

                return TryParseBandwidthLine(line, out token, out result);
            }

            public static bool TryParseBandwidthLine(Media.Sdp.SessionDescriptionLine line, out string token, out int result)
            {
                token = string.Empty;

                result = -1;

                if (line == null || line.m_Type != Sdp.Lines.SessionBandwidthLine.BandwidthType || line.m_Parts.Count <= 0) return false;

                string[] tokens = line.m_Parts[0].Split(Media.Sdp.SessionDescription.ColonSplit, StringSplitOptions.RemoveEmptyEntries);

                if (tokens.Length < 2) return false;

                token = tokens[0];

                return int.TryParse(tokens[1], out result);
            }

            //Todo, this should not be required or should be an extension method of the MediaDescription Type
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
            //Because there can also be a Bandwidth attribute at a the session level.

            #endregion

            internal const char BandwidthType = 'b';

            #region Properties

            /// <summary>
            /// Gets the bwtype token.
            /// </summary>
            public string BandwidthTypeString
            {
                get { return GetPart(0); }
                //set { SetPart(0, value); }
            }

            /// <summary>
            /// Gets the bandwidth token
            /// </summary>
            public string BandwidthValueString
            {
                get { return GetPart(1); }
                //set { SetPart(1, value); }
            }

            /// <summary>
            /// Parses the <see cref="BandwidthValueString"/>
            /// </summary>
            public long BandwidthValue
            {
                get { return long.Parse(GetPart(1)); }
                //set { SetPart(1, value.ToString()); }
            }

            /// <summary>
            /// Indicates if the <see cref="BandwidthValue"/> is equal to 0
            /// </summary>
            public bool IsDisabled { get { return BandwidthValue == DisabledValue; } }

            #endregion

            #region Constructor

            //b=X-YZ:128

            public SessionBandwidthLine(SessionDescriptionLine line)
                : base(line)
            {
                if (m_Type != BandwidthType) throw new InvalidOperationException("Not a SessionBandwidthLine line");

                //Parts will probably not be parsed on ':'
                //Must reset and use the first line parts...

                //m_Parts.Clear();
            }

            public SessionBandwidthLine(string token, int value)
                : base(BandwidthType, SessionDescription.Colon.ToString())
            {
                Add(token);

                Add(value.ToString());
            }

            public SessionBandwidthLine(string[] sdpLines, ref int index)
                : base(sdpLines, ref index, SessionDescription.Colon.ToString(), BandwidthType) { }

            #endregion

            //KeyValuePair string t,int b ^
        }

        //Todo, Redo
        /// <summary>
        /// Represents an 'c=' <see cref="SessionDescriptionLine"/>
        /// </summary>
        public class SessionConnectionLine : SessionDescriptionLine
        {
            public const string InConnectionToken = "IN";

            //Should be moved when defined

            //IANA http://www.iana.org/assignments/sdp-parameters/sdp-parameters.xhtml

            //Proto

            //NetType

            //AddrType

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

            //This is not always an IP....(see notes above)
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

            //HasHops

            /// <summary>
            /// Todo, should not be nullable.
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

                    return null; //Default Ttl
                }
            }

            //Has(Multiple)Ports

            /// <summary>
            /// Todo, should not be nullable.
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

                    return null; //1
                }
            }

            #region Constructor

            public SessionConnectionLine(SessionDescriptionLine line)
                : this()
            {
                if (line.m_Type != ConnectionType) throw new InvalidOperationException("Not a SessionConnectionLine");

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
                : base(sdpLines, ref index, SessionDescription.SpaceString, ConnectionType) //,3) //this()
            {
                //try
                //{
                //    string sdpLine = sdpLines[index++].Trim();

                //    if (string.IsNullOrWhiteSpace(sdpLine) 
                //        || 
                //        sdpLine[0] != ConnectionType) Media.Common.TaggedExceptionExtensions.RaiseTaggedException(this, "Invalid SessionConnectionLine");

                //    sdpLine = SessionDescription.TrimLineValue(sdpLine.Substring(2));

                //    //m_Parts.Add(sdpLine);

                //    //Could loop split results and call SetPart(i, splits[i])

                //    m_Parts.Clear();

                //    m_Parts.AddRange(sdpLine.Split(SessionDescription.Space));

                //    EnsureParts(3);
                //}
                //catch
                //{
                //    throw;
                //}
            }

            #endregion

            //ToString should be implemented by GetEnumerator and String.Join(m_Seperator

            //public override string ToString()
            //{
            //    return ConnectionType.ToString() + Media.Sdp.SessionDescription.EqualsSign + 
            //        (m_Parts.Count == 1 ? ConnectionNetworkType : string.Join(SessionDescription.SpaceString, ConnectionNetworkType, ConnectionAddressType, ConnectionAddress))
            //        + SessionDescription.NewLineString;
            //}

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
                if (m_Type != VersionType) throw new InvalidOperationException("Not a SessionVersionLine line");
            }

            /// <summary>
            /// The string value of the token on the version line.
            /// </summary>
            public string VersionToken
            {
                get { return GetPart(0); }
                set { SetPart(0, value); }
            }

            /// <summary>
            /// Parses <see cref="VersionToken"/> as an integer
            /// </summary>
            public int Version
            {
                get
                {
                    int result;

                    int.TryParse(VersionToken, out result);

                    return result;

                    //return m_Parts.Count > 0 ? int.Parse(m_Parts[0], System.Globalization.CultureInfo.InvariantCulture) : 0;
                }
                set
                {
                    //m_Parts.Clear();

                    //m_Parts.Add(value.ToString());

                    VersionToken = value.ToString();
                }
            }

            public SessionVersionLine(int version)
                : base(VersionType)
            {
                Version = version;
            }

            public SessionVersionLine(string[] sdpLines, ref int index)
                :  base(sdpLines, ref index, SessionDescription.SpaceString, VersionType) //base(VersionType)
            {
                //try
                //{
                //    string sdpLine = sdpLines[index++].Trim();

                //    if (sdpLine[0] != VersionType) Media.Common.TaggedExceptionExtensions.RaiseTaggedException(this, "Invalid SessionVersionLine Line");

                //    sdpLine = SessionDescription.TrimLineValue(sdpLine.Substring(2));

                //    m_Parts.Add(sdpLine);
                //}
                //catch
                //{
                //    throw;
                //}
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

            public string VersionToken
            {
                get { return GetPart(2); }
                set { SetPart(2, value); }
            }

            public long SessionVersion
            {
                get
                {
                    string part = VersionToken;

                    if (string.IsNullOrWhiteSpace(part)) return 0;

                    //Range ...
                    //return (long)double.Parse(part);

                    return part[0] == Common.ASCII.HyphenSign ? long.Parse(part) : (long)ulong.Parse(part);
                }
                //set { VersionToken = value.ToString(); }
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
                if (line.m_Type != OriginType) throw new InvalidOperationException("Not a SessionOriginLine");

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

            //Conflicts with base's concept of string constructor
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
                : base(sdpLines, ref index, SessionDescription.SpaceString, OriginType) //,6
            {
                //try
                //{
                //    string sdpLine = sdpLines[index++].Trim();

                //    if (sdpLine[0] != OriginType) Media.Common.TaggedExceptionExtensions.RaiseTaggedException(this, "Invalid SessionOriginatorLine");

                //    sdpLine = SessionDescription.TrimLineValue(sdpLine.Substring(2));

                //    m_Parts.Clear();

                //    m_Parts.AddRange(sdpLine.Split(SessionDescription.Space));
                //}
                //catch
                //{
                //    throw;
                //}
            }

            //public override string ToString()
            //{
            //    return OriginType.ToString() + Media.Sdp.SessionDescription.EqualsSign + 
            //        string.Join(SessionDescription.SpaceString, Username, SessionId, SessionVersion, NetworkType, AddressType, Address) 
            //        + SessionDescription.NewLineString;
            //}

        }

        /// <summary>
        /// Represents an 's=' <see cref="SessionDescriptionLine"/>
        /// </summary>
        public class SessionNameLine : SessionDescriptionLine
        {

            internal const char NameType = 's';

            public string SessionName
            {
                get { return GetPart(0); }
                set { SetPart(0, value); }
            }

            public SessionNameLine()
                : base(NameType)
            {

            }

            public SessionNameLine(SessionDescriptionLine line)
                : base(line)
            {
                if (m_Type != NameType) throw new InvalidOperationException("Not a SessionNameLine line");
            }

            //Conflicts with base's concept of string constructor
            public SessionNameLine(string sessionName)
                : this()
            {
                //Should contain a single space if null or empty.
                if (string.IsNullOrEmpty(sessionName)) sessionName = SessionDescription.SpaceString;

                SessionName = sessionName;
            }

            //public SessionNameLine(string text)
            //    : base(text, SessionDescription.SpaceString)
            //{
            //    if (Type != NameType) throw new InvalidOperationException("Not a SessionNameLine line");
            //}

            public SessionNameLine(string[] sdpLines, ref int index)
                : base(sdpLines, ref index, SessionDescription.SpaceString, NameType) //this()
            {
                //try
                //{
                //    string sdpLine = sdpLines[index++].Trim();

                //    if (sdpLine[0] != NameType) Media.Common.TaggedExceptionExtensions.RaiseTaggedException(this, "Invalid SessionNameLine");

                //    sdpLine = SessionDescription.TrimLineValue(sdpLine.Substring(2));

                //    m_Parts.Add(sdpLine);
                //}
                //catch
                //{
                //    throw;
                //}
            }

            //public override string ToString()
            //{
            //    return NameType.ToString() + Media.Sdp.SessionDescription.EqualsSign + (string.IsNullOrEmpty(SessionName) ? string.Empty : SessionName) + SessionDescription.NewLineString;
            //}
        }

        /// <summary>
        /// Represents an 'i=' <see cref="SessionDescriptionLine"/>
        /// </summary>
        public class SessionInformationLine : SessionDescriptionLine
        {

            internal const char InformationType = 'i';

            public string SessionName
            {
                get { return GetPart(0);}
                set { SetPart(0, value); }
            }

            public SessionInformationLine()
                : base(InformationType)
            {

            }

            public SessionInformationLine(SessionDescriptionLine line)
                : base(line)
            {
                if (m_Type != InformationType) throw new InvalidOperationException("Not a SessionInformationLine line");
            }

            public SessionInformationLine(string sessionName)
                : this()
            {
                SessionName = sessionName;
            }

            public SessionInformationLine(string[] sdpLines, ref int index)
                : base(sdpLines, ref index, SessionDescription.SpaceString, InformationType) //this()
            {
                //try
                //{
                //    string sdpLine = sdpLines[index++].Trim();

                //    if (sdpLine[0] != InformationType) Media.Common.TaggedExceptionExtensions.RaiseTaggedException(this, "Invalid SessionInformationLine");

                //    sdpLine = SessionDescription.TrimLineValue(sdpLine.Substring(2));

                //    m_Parts.Add(sdpLine);
                //}
                //catch
                //{
                //    throw;
                //}
            }

            //public override string ToString()
            //{
            //    return InformationType.ToString() + Media.Sdp.SessionDescription.EqualsSign + (string.IsNullOrEmpty(SessionName) ? string.Empty : SessionName) + SessionDescription.NewLineString;
            //}
        }

        /// <summary>
        /// Represents an 'p=' <see cref="SessionDescriptionLine"/>
        /// </summary>
        public class SessionPhoneNumberLine : SessionDescriptionLine
        {

            internal const char PhoneType = 'p';

            public string PhoneNumber
            {
                get { return GetPart(0); }
                set { SetPart(0, value); }
            }

            public SessionPhoneNumberLine()
                : base(PhoneType)
            {

            }

            public SessionPhoneNumberLine(SessionDescriptionLine line)
                : base(line)
            {
                if (m_Type != PhoneType) throw new InvalidOperationException("Not a SessionPhoneNumberLine");
            }

            public SessionPhoneNumberLine(string sessionName)
                : this()
            {
                PhoneNumber = sessionName;
            }

            public SessionPhoneNumberLine(string[] sdpLines, ref int index)
                : base(sdpLines, ref index, SessionDescription.SpaceString, PhoneType) //this()
            {
                //try
                //{
                //    string sdpLine = sdpLines[index++].Trim();

                //    if (sdpLine[0] != PhoneType) Media.Common.TaggedExceptionExtensions.RaiseTaggedException(this, "Invalid SessionPhoneNumberLine");

                //    sdpLine = SessionDescription.TrimLineValue(sdpLine.Substring(2));

                //    m_Parts.Add(sdpLine);
                //}
                //catch
                //{
                //    throw;
                //}
            }

            //public override string ToString()
            //{
            //    return PhoneType.ToString() + Media.Sdp.SessionDescription.EqualsSign + (string.IsNullOrEmpty(PhoneNumber) ? string.Empty : PhoneNumber) + SessionDescription.NewLineString;
            //}
        }

        /// <summary>
        /// Represents an 'e=' <see cref="SessionDescriptionLine"/>
        /// </summary>
        public class SessionEmailLine : SessionDescriptionLine
        {
            internal const char EmailType = 'e';

            public string Email
            {
                get { return GetPart(0); }
                set { SetPart(0, value); }
            }
            
            #region Constructor

            public SessionEmailLine()
                : base(EmailType)
            {

            }

            public SessionEmailLine(SessionDescriptionLine line)
                : base(line)
            {
                if (m_Type != EmailType) throw new InvalidOperationException("Not a SessionEmailLine line");
            }

            public SessionEmailLine(string sessionName)
                : this()
            {
                Email = sessionName;
            }

            public SessionEmailLine(string[] sdpLines, ref int index)
                : base(sdpLines, ref index, SessionDescription.SpaceString, EmailType)
            {
                //try
                //{
                //    string sdpLine = sdpLines[index++].Trim();

                //    if (sdpLine[0] != EmailType) Media.Common.TaggedExceptionExtensions.RaiseTaggedException(this, "Invalid SessionEmailLine");

                //    sdpLine = SessionDescription.TrimLineValue(sdpLine.Substring(2));

                //    m_Parts.Add(sdpLine);
                //}
                //catch
                //{
                //    throw;
                //}
            }

            #endregion

            //public override string ToString()
            //{
            //    return EmailType.ToString() + Media.Sdp.SessionDescription.EqualsSign + (string.IsNullOrEmpty(Email) ? string.Empty : Email) + SessionDescription.NewLineString;
            //}
        }

        /// <summary>
        /// Represents an 'u=' <see cref="SessionDescriptionLine"/>
        /// </summary>
        public class SessionUriLine : SessionDescriptionLine
        {
            internal const char UriType = 'u';

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
                : base(UriType)
            {
            }

            public SessionUriLine(SessionDescriptionLine line)
                : base(line)
            {
                if (m_Type != UriType) throw new InvalidOperationException("Not a SessionUriLine");
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
                : base(sdpLines, ref index, SessionDescription.SpaceString, UriType)
            {
                
            }
        }

        /// <summary>
        /// Represents an 'z=' <see cref="SessionDescriptionLine"/>
        /// </summary>
        public class SessionTimeZoneLine : SessionDescriptionLine
        {
            internal const char TimeZoneType = 'z';

            #region Properties

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

            #endregion

            #region Constructor

            public SessionTimeZoneLine()
                : base(TimeZoneType, SessionDescription.SpaceString)
            {

            }

            public SessionTimeZoneLine(SessionDescriptionLine line)
                : this()
            {
                if (line.m_Type != TimeZoneType) throw new InvalidOperationException("Not a SessionTimeZoneLine line");

                if (line.m_Parts.Count == 1)
                {
                    string temp = line.m_Parts[0];

                    //No parts, called this() not base()
                    //m_Parts.Clear();

                    m_Parts.AddRange(temp.Split(SessionDescription.Space));
                }
                else m_Parts.AddRange(line.m_Parts);
            }

            //Todo
            public SessionTimeZoneLine(string[] sdpLines, ref int index)
                : this() // base(sdpLines, ref index,  SessionDescription.SpaceString, TimeZoneType) (throws when valadating line in base)
            {
                try
                {
                    string sdpLine = sdpLines[index++].Trim();

                    if (string.IsNullOrWhiteSpace(sdpLine) 
                        ||
                        sdpLine[0] != TimeZoneType) Media.Common.TaggedExceptionExtensions.RaiseTaggedException(this, "Invalid SessionTimeZoneLine");

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
            //Changing existing values may be more suitable..

            #endregion

            //public override string ToString()
            //{
            //    return TimeZoneType.ToString() + SessionDescription.EqualsSign + string.Join(SessionDescription.SpaceString, m_Parts) + SessionDescription.NewLine;
            //}
        }

        /// <summary>
        /// Represents an 'm=' <see cref="SessionDescriptionLine"/>
        /// </summary>
        public class SessionMediaDescriptionLine : SessionDescriptionLine
        {
            internal const char MediaDescriptionType = 'm';

            //internal const int PartCount = 4;

            #region Properties

            /// <summary>
            /// Gets the string assoicated with the media token.
            /// </summary>
            public string MediaToken
            {
                get { return GetPart(0); }
                internal protected set { SetPart(0, value); }
            }

            /// <summary>
            /// Parses <see cref="MediaToken"/>
            /// </summary>
            public MediaType MediaType
            {
                                                                                      //Unbounded split
                get { return (MediaType)Enum.Parse(typeof(MediaType), MediaToken.Split(SessionDescription.Space).First(), true); }
                set { SetPart(0, value.ToString()); }
            }

            /// <summary>
            /// Gets the string assoicated with the port token.
            /// </summary>
            public string PortToken
            {
                get { return GetPart(1); }
                set { SetPart(1, value); }
            }

            /// <summary>
            /// Parses <see cref="PortToken"/>
            /// </summary>
            public int MediaPort
            {
                get { return int.Parse(PortToken, System.Globalization.CultureInfo.InvariantCulture); }
                set
                {
                    if (value < ushort.MinValue || value > ushort.MaxValue) throw new ArgumentOutOfRangeException("The port value cannot be less than 0 or exceed 65535");

                    SetPart(1, value.ToString());
                }
            }
            
            //Could be extension method or static method of the Attribute class, this logic could then be reused in other places such as the ConnectionLine.

            /// <summary>
            /// Gets a value indicating if the <see cref="PortToken"/> has the <see cref="SessionDescription.ForwardSlashString"/>
            /// </summary>
            public bool HasPortRange
            {
                get { return PortToken.IndexOf(SessionDescription.ForwardSlashString) >= 0; }
            }

            /// <summary>
            /// Parses the <see cref="PortToken"/> to obtain the number of ports.
            /// Return 1 if no value was found.
            /// </summary>
            public int NumberOfPorts
            {
                get
                {
                    foreach (string part in PortToken.Split(SessionDescription.ForwardSlash).Skip(1))
                    {
                        if (string.IsNullOrWhiteSpace(part)) continue;

                        return int.Parse(part);
                    }

                    return 1;
                }
                set
                {
                    if (value < ushort.MinValue || value > ushort.MaxValue) throw new ArgumentOutOfRangeException("A value less than 0 or greater than 65535 is not valid.");

                    //Set the first part to the result of joining the existing MediaPort + / + value
                    SetPart(1, string.Join(SessionDescription.ForwardSlash.ToString(), MediaPort, value));

                    ////Take the first value which is the MediaPort and then add / and the new values
                    //if (HasPortRange) SetPart(1, string.Join(SessionDescription.ForwardSlash.ToString(), GetPart(1).Split(SessionDescription.ForwardSlash).First(), value));
                    //else SetPart(1, string.Join(SessionDescription.ForwardSlash.ToString(), GetPart(1), value));
                }
            }

            //public IEnumerable<int> MediaPorts
            //{
            //    get
            //    {
            //        int mediaPort = MediaPort;

            //        yield return mediaPort;

            //        //Iterate from ports in range
            //        for (int i = 0, e = PortRange; i < e; ++i) yield return mediaPort + i;
            //    }
            //    set
            //    {
            //        MediaPort = value.First();

            //        PortRange = value.Count() - 1;
            //    }
            //}

            /// <summary>
            ///  Gets or Sets the transport protocol, (proto token)
            /// </summary>
            public string MediaProtocol
            {
                get { return GetPart(2); }
                set { SetPart(2, value); }
            }

            /// <summary>
            /// MediaFormat or <fmt> is a media format description.  
            /// The fourth and any subsequent sub-fields describe the format of the media.  
            /// The interpretation of the media format depends on the value of the <see cref="MediaProtocol"/>.
            /// </summary>
            public string MediaFormat
            {
                get { return GetPart(3); }
                set { SetPart(3, value); }
            }

            /// <summary>
            /// Parses the port tokens out of the MediaFormat field, this can probably be an extension method.
            /// </summary>
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
                    ////Proto token index
                    //int start = 3;

                    //foreach (var port in value)
                    //{
                    //    SetPart(start++, port.ToString());
                    //}

                    SetPart(3, string.Join(SessionDescription.SpaceString, value));
                }
            }

            #endregion

            #region Constructor

            public SessionMediaDescriptionLine()
                : base(MediaDescriptionType, SessionDescription.SpaceString, 4)
            {

            }

            public SessionMediaDescriptionLine(SessionDescriptionLine line)
                : base(line)
            {
                if (m_Type != MediaDescriptionType) throw new InvalidOperationException("Not a SessionMediaDescriptionLine line");

                ////Not really needed unless the line was in an incorrect format?
                //if (line.m_Parts.Count == 1)
                //{
                //    string temp = line.m_Parts[0];

                //    m_Parts.Clear();

                //    m_Parts.AddRange(temp.Split(SessionDescription.Space));
                //}
            }

            public SessionMediaDescriptionLine(string[] sdpLines, ref int index)
                : base(sdpLines, ref index, SessionDescription.SpaceString, MediaDescriptionType, 4)
            {
                
            }

            //internal...
            public SessionMediaDescriptionLine(string text)
                : base(text, SessionDescription.SpaceString)
            {
                if (m_Type != MediaDescriptionType) throw new InvalidOperationException("Not a SessionMediaDescriptionLine line");
            }

            #endregion

            void Add(int payloadType)
            {
                //Add the part
                //Add(payloadType.ToString());

                //Join in the given type.
                SetPart(3, string.Join(SessionDescription.SpaceString, payloadType.ToString()));
            }
        }

        /// <summary>
        /// Represents an 't=' <see cref="SessionDescriptionLine"/>
        /// </summary>
        public class SessionTimeDescriptionLine : SessionDescriptionLine
        {
            internal const char TimeType = 't';
            
            //internal protected for setters or this can be changed on accident.
            public static SessionTimeDescriptionLine Permanent = new SessionTimeDescriptionLine(0, 0);

            //Method for Permanent or Unbounded?

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

            public SessionTimeDescriptionLine(string[] sdpLines, ref int index)
                : base(sdpLines, ref index, SessionDescription.SpaceString, TimeType, 4)
            {
                
            }

            //internal...
            public SessionTimeDescriptionLine(string text)
                : base(text, SessionDescription.SpaceString)
            {
                if (m_Type != TimeType) throw new InvalidOperationException("Not a SessionTimeDescriptionLine line");
            }

            #endregion

            #region Properties

            public string StartTimeString
            {
                get { return GetPart(0); }
                set { SetPart(0, value); }
            }

            /// <summary>
            /// Gets or sets the start time.
            /// If seto to 0 then the session is not bounded,  though it will not become active until after the <see cref="StartTime"/>.  
            /// </summary>
            /// <remarks>These values are the decimal representation of Network Time Protocol (NTP) time values in seconds since 1900 </remarks>
            public long StartTime
            {
                get { return (long)StartTimeSpan.TotalSeconds; }
                internal set { SetPart(0, value.ToString()); }
            }

            /// <summary>
            /// TimeSpan representation of <see cref="StartTime"/>
            /// </summary>
            public TimeSpan StartTimeSpan
            {
                get { return SessionDescription.ParseTime(GetPart(0)); }
                internal set { SetPart(0, value.ToString()); }//Format
            }

            public string StopTimeString
            {
                get { return GetPart(1); }
                set { SetPart(1, value); }
            }

            /// <summary>
            /// Gets or sets the stop time.
            /// If set to 0 and the <see cref="StartTime"/> is also zero, the session is regarded as permanent.
            /// </summary>
            /// <remarks>These values are the decimal representation of Network Time Protocol (NTP) time values in seconds since 1900 </remarks>
            public long StopTime
            {
                get { return (long)StopTimeSpan.TotalSeconds; }
                internal set { SetPart(1, value.ToString()); }
            }

            /// <summary>
            /// TimeSpan representation of <see cref="StopTime"/>
            /// </summary>
            public TimeSpan StopTimeSpan
            {
                get { return SessionDescription.ParseTime(GetPart(1)); }
                internal set { SetPart(1, value.ToString()); }//Format
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

                internal set
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

                internal set
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

            #region Properties

            //Parts already gives the strings

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
            /// The sum of <see cref="RepeatTimes"/>
            /// </summary>
            public TimeSpan RepeatTimeSpan
            {
                get { return TimeSpan.FromSeconds(RepeatTimeValue); }
                //set { } //[Clear parts and] Format value.

                //"8d 2h 0m 0s"
                //RepeatTimeSpan.ToString(@"d\d\ h\h\ m\m\ s\s")

                //Could also do check Days > 0, Hours > 0 and build a smaller string?
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

            /// <summary>
            /// Gets or sets the sum of <see cref="RepeatValues"/>
            /// </summary>
            public double RepeatTimeValue
            {
                get { return RepeatValues.Sum(); }
                //set { RepeatTimeSpan = TimeSpan.FromSeconds(value); }
            }

            #endregion

            #region Constructor

            public SessionRepeatTimeLine()
                : base(RepeatType, SessionDescription.SpaceString)
            {

            }

            //Use base constructor...

            public SessionRepeatTimeLine(SessionDescriptionLine line)
                : this()
            {
                if (line.m_Type != RepeatType) throw new InvalidOperationException("Not a SessionRepeatTimeLine line");

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
                        sdpLine[0] != RepeatType) Media.Common.TaggedExceptionExtensions.RaiseTaggedException(this, "Invalid SessionRepeatTimeLine");

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
                    if (string.IsNullOrWhiteSpace(text)) Media.Common.TaggedExceptionExtensions.RaiseTaggedException(this, "Invalid SessionRepeatTimeLine");
                        
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

            public void Add(double repeatValue)
            {
                Add(repeatValue.ToString());
            }

            public void Add(params double[] repeatValues)
            {
                foreach (double repeatValue in repeatValues)
                    Add(repeatValue.ToString());
            }

            public void Add(TimeSpan repeatTime)
            {
                Add(repeatTime.ToString());
            }

            public void Add(params TimeSpan[] repeatTimes)
            {
                foreach (TimeSpan repeatTime in repeatTimes)
                    Add(repeatTime.ToString());//Format
            }
        }

        //a=rtpmap:98 H264/90000
        //RtpMapAttributeLine : AttributeLine

        /*
          a=fmtp:98 profile-level-id=42A01E;
                packetization-mode=1;
                sprop-parameter-sets=<parameter sets data>
         */

        public class FormatTypeLine : SessionAttributeLine
        {
            public string FormatToken
            {
                get { return GetPart(0); }
                set { SetPart(0, value); }
            }

            public bool HasFormatValue { get { return FormatToken.IndexOf(SessionDescription.Colon) >= 0; } }

            public int FormatValue
            {
                get
                {
                    int index = FormatToken.IndexOf(SessionDescription.Colon);

                    if (index == -1) return index;

                    return int.Parse(FormatToken.Substring(index));
                }
            }

            public bool HasFormatSpecificParameters { get { return m_Parts.Count > 1; } }

            public string FormatSpecificParameterToken
            {
                get { return GetPart(1); }
                set { SetPart(1, value); }
            }

            public IEnumerable<string> FormatSpecificParameterParameters
            {
                get { return m_Parts.Skip(1); }
            }

            public int FormatSpecificParametersCount
            {
                get { return PartsCount - 1; }
            }

            //Could be verified ina common class given a start type.
            public FormatTypeLine(SessionDescriptionLine line)
                : base(line)
            {
                if (m_Parts.Count == 0
                    ||//Should not ignore case.
                    false == GetPart(0).StartsWith(AttributeFields.FormatType, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Not a FormatTypeLine line");
            }

            public FormatTypeLine(string value)
                : base(string.Join(SessionDescription.Colon.ToString(), AttributeFields.FormatType, value))
            {

            }

            public FormatTypeLine(string value, string formatSpecificParameters)
                : this(value)
            {
                Add(formatSpecificParameters);
            }

            public FormatTypeLine(string[] sdpLines, ref int index)
                : base(sdpLines, ref index)
            {
                if (m_Parts.Count == 0
                    ||//Should not ignore case.
                    false == GetPart(0).StartsWith(AttributeFields.FormatType, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Not a FormatTypeLine line");
            }
        }
    }
}
