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

        /// <summary>
        /// Represents an 'b=' <see cref="SessionDescriptionLine"/>
        /// </summary>
        public class SessionBandwidthLine : SessionDescriptionLine
        {
            #region RFC3556 Bandwidth

            const string RecieveBandwidthToken = "RR", SendBandwdithToken = "RS", ApplicationSpecificBandwidthToken = "AS";

            public static readonly Sdp.SessionDescriptionLine DisabledReceiveLine = new Sdp.SessionDescriptionLine("b=RR:0");

            public static readonly Sdp.SessionDescriptionLine DisabledSendLine = new Sdp.SessionDescriptionLine("b=RS:0");

            public static readonly Sdp.SessionDescriptionLine DisabledApplicationSpecificLine = new Sdp.SessionDescriptionLine("b=AS:0");

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

                    if (m_ConnectionParts.Length > 2) //Todo ensure not accidentally giving Hops... should proably be 3
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

                    if (sdpLine[0] != ConnectionType) Media.Common.Extensions.Exception.ExceptionExtensions.RaiseTaggedException(this, "Invalid SessionConnectionLine");

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

                    if (sdpLine[0] != OriginatorType) Media.Common.Extensions.Exception.ExceptionExtensions.RaiseTaggedException(this, "Invalid SessionOriginatorLine");

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

                    if (sdpLine[0] != PhoneType) Media.Common.Extensions.Exception.ExceptionExtensions.RaiseTaggedException(this, "Invalid SessionPhoneLine");

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

        //RtpMapAttributeLine : AttributeLine

        //FormatTypeLine : AttributeLine
    }
}
