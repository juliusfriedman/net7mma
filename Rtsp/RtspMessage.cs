﻿/*
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

namespace Media.Rtsp
{

    /// <summary>
    /// Header Definitions from RFC2326
    /// http://www.ietf.org/rfc/rfc2326.txt
    /// </summary>
    public sealed class RtspHeaders
    {

        internal const char HyphenSign = (char)Common.ASCII.HyphenSign, SemiColon = (char)Common.ASCII.SemiColon, Comma = (char)Common.ASCII.Comma;

        internal static string [] TimeSplit = new string[] { HyphenSign.ToString(), SemiColon.ToString() };

        internal static char[] SpaceSplit = new char[] { (char)Common.ASCII.Space, Comma };

        internal static char[] ValueSplit = new char[] { (char)Common.ASCII.EqualsSign, SemiColon };        

        public const string Allow = "Allow";
        public const string Accept = "Accept";
        public const string AcceptCredentials = "Accept-Credentials";
        public const string AcceptEncoding = "Accept-Encoding";
        public const string AcceptLanguage = "Accept-Language";
        public const string AcceptRanges = "Accept-Ranges";
        public const string Authorization = "Authorization";
        public const string AuthenticationInfo = "Authentication-Info";
        public const string Bandwidth = "Bandwidth";
        public const string Blocksize = "Blocksize";
        public const string CacheControl = "Cache-Control";
        public const string Conference = "Conference";
        public const string Connection = "Connection";
        public const string ConnectionCredentials = "Connection-Credentials";
        public const string ContentBase = "Content-Base";
        public const string ContentEncoding = "Content-Encoding";
        public const string ContentLanguage = "Content-Language";
        public const string ContentLength = "Content-Length";
        public const string ContentLocation = "Content-Location";
        public const string ContentType = "Content-Type";
        public const string CSeq = "CSeq";
        public const string Date = "Date";
        public const string RTSPDate = "RTSP-Date";
        public const string From = "From";
        public const string Expires = "Expires";
        public const string LastModified = "Last-Modified";
        public const string IfMatch = "If-Match";
        public const string IfModifiedSince = "If-Modified-Since";
        public const string IfNoneMatch = "If-None-Match";
        public const string Location = "Location";
        public const string MTag = "MTag";
        public const string MediaProperties = "Media-Properties";
        public const string MediaRange = "Media-Range";
        public const string PipelinedRequests = "Pipelined-Requests";
        public const string ProxyAuthenticate = "Proxy-Authenticate";
        public const string ProxyAuthenticationInfo = "Proxy-Authentication-Info";
        public const string ProxyAuthorization = "Proxy-Authorization";
        public const string ProxyRequire = "Proxy-Require";
        public const string ProxySupported = "Proxy-Supported";
        public const string Public = "Public";
        //Private / Prividgled?
        public const string Range = "Range";
        public const string Referrer = "Referrer";
        public const string Require = "Require";
        public const string RequestStatus = " Request-Status";
        public const string RetryAfter = "Retry-After";
        public const string RtpInfo = "RTP-Info";
        public const string Scale = "Scale";
        public const string Session = "Session";
        public const string Server = "Server";
        public const string Speed = "Speed";
        public const string Supported = "Supported";
        public const string TerminateReason = "Terminate-Reason";
        public const string Timestamp = "Timestamp";
        public const string Transport = "Transport";
        public const string Unsupported = "Unsupported";
        public const string UserAgent = "User-Agent";
        public const string Via = "Via";
        public const string WWWAuthenticate = "WWW-Authenticate";

        private RtspHeaders() { }

        /// <summary>
        /// Parses a RFCXXXX range string often used in SDP to describe start and end times.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="type"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public static bool TryParseRange(string value, out string type, out TimeSpan start, out TimeSpan end)
        {
            return Media.Sdp.SessionDescription.TryParseRange(value, out type, out start, out end);
        }

        /// <summary>
        /// Creates a RFCXXX range string
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="type"></param>
        /// <param name="format"></param>
        /// <param name="timePart">optional time= utc-time. `;time=19970123T143720Z`</param>
        /// <returns></returns>
        public static string RangeHeader(TimeSpan? start, TimeSpan? end, string type = "npt", string timePart = null)
        {
            return type + 
                ((char)Common.ASCII.EqualsSign).ToString() + 
                (start.HasValue && end.HasValue && end.Value != Utility.InfiniteTimeSpan ? 
                start.Value.TotalSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture) : "now") + 
                '-' + 
                (end.HasValue && end.Value > TimeSpan.Zero ? 
                end.Value.TotalSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture) : string.Empty) +
                (false == string.IsNullOrWhiteSpace(timePart) ? (((char)Common.ASCII.SemiColon).ToString() + timePart) : string.Empty);
        }

        public static string BasicAuthorizationHeader(Encoding encoding, System.Net.NetworkCredential credential) { return AuthorizationHeader(encoding, RtspMethod.UNKNOWN, null, System.Net.AuthenticationSchemes.Basic, credential); }

        public static string DigestAuthorizationHeader(Encoding encoding, RtspMethod method, Uri location, System.Net.NetworkCredential credential, string qopPart = null, string ncPart = null, string nOncePart = null, string cnOncePart = null, string opaquePart = null, bool rfc2069 = false, string algorithmPart = null, string bodyPart = null) { return AuthorizationHeader(encoding, method, location, System.Net.AuthenticationSchemes.Digest, credential, qopPart, ncPart, nOncePart, cnOncePart, opaquePart, rfc2069, algorithmPart, bodyPart); }

        internal static string AuthorizationHeader(Encoding encoding, RtspMethod method, Uri location, System.Net.AuthenticationSchemes scheme, System.Net.NetworkCredential credential, string qopPart = null, string ncPart = null, string nOncePart = null, string cnOncePart = null, string opaquePart = null, bool rfc2069 = false, string algorithmPart = null, string bodyPart = null)
        {
            if (scheme != System.Net.AuthenticationSchemes.Basic && scheme != System.Net.AuthenticationSchemes.Digest) throw new ArgumentException("Must be either None, Basic or Digest", "scheme");
            string result = string.Empty;

            //Basic 
            if (scheme == System.Net.AuthenticationSchemes.Basic)
            {
                //http://en.wikipedia.org/wiki/Basic_access_authentication
                //Don't use the domain.
                result = "Basic " + Convert.ToBase64String(encoding.GetBytes(credential.UserName + ':' + credential.Password));
            }
            else if (scheme == System.Net.AuthenticationSchemes.Digest) //Digest
            {
                //http://www.ietf.org/rfc/rfc2617.txt

                //Example 
                //Authorization: Digest username="admin", realm="GeoVision", nonce="b923b84614fc11c78c712fb0e88bc525", uri="rtsp://203.11.64.27:8554/CH001.sdp", response="d771e4e5956e3d409ce5747927db10af"\r\n

                //Todo Check that Digest works with Options * or when uriPart is \

                using (var md5 = Utility.CreateMD5HashAlgorithm())
                {
                    string usernamePart = credential.UserName,
                    realmPart = credential.Domain ?? "//",
                    uriPart = location != null ? location.AbsoluteUri : new String((char)Common.ASCII.BackSlash, 1);

                    if (string.IsNullOrWhiteSpace(nOncePart))
                    {
                        //Contains two sequential 32 bit units from the Random generator for now
                        nOncePart = ((long)(Utility.Random.Next(int.MaxValue) << 32 | Utility.Random.Next(int.MaxValue))).ToString("X");
                    }
                   
                    //Need to look at this again
                    if (!string.IsNullOrWhiteSpace(qopPart))
                    {
                        if (!string.IsNullOrWhiteSpace(ncPart)) ncPart = (int.Parse(ncPart) + 1).ToString();
                        else ncPart = "00000001";
                        if (string.IsNullOrWhiteSpace(cnOncePart)) cnOncePart = Utility.Random.Next(int.MaxValue).ToString("X");
                    }

                    //http://en.wikipedia.org/wiki/Digest_access_authentication
                    //The MD5 hash of the combined username, authentication realm and password is calculated. The result is referred to as HA1.
                    byte[] HA1 = md5.ComputeHash(encoding.GetBytes(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}:{1}:{2}", credential.UserName, realmPart, credential.Password)));

                    //The MD5 hash of the combined method and digest URI is calculated, e.g. of "GET" and "/dir/index.html". The result is referred to as HA2.
                    byte[] HA2 = null;

                    //Need to format based on presence of fields qop...
                    byte[] ResponseHash;

                    //If there is a Quality of Protection
                    if (qopPart != null)
                    {
                        if (qopPart == "auth")
                        {
                            HA2 = md5.ComputeHash(encoding.GetBytes(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}:{1}", method, uriPart)));
                            //The MD5 hash of the combined HA1 result, server nonce (nonce), request counter (nc), client nonce (cnonce), quality of protection code (qop) and HA2 result is calculated. The result is the "response" value provided by the client.
                            ResponseHash = md5.ComputeHash(encoding.GetBytes(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}:{1}:{2}:{3}:{4}:{5}", BitConverter.ToString(HA1).Replace("-", string.Empty).ToLowerInvariant(), nOncePart, BitConverter.ToString(HA2).Replace("-", string.Empty).ToLowerInvariant(), ncPart, cnOncePart, qopPart)));
                            result = string.Format(System.Globalization.CultureInfo.InvariantCulture, "Digest username=\"{0}\", realm=\"{1}\", nonce=\"{2}\", uri=\"{3}\", qop=\"{4}\" nc=\"{5} cnonce=\"{6}\"", usernamePart, realmPart, nOncePart, uriPart, qopPart, ncPart, cnOncePart);
                            if (!string.IsNullOrWhiteSpace(opaquePart)) result += "opaque=\"" + opaquePart + '"';
                        }
                        else if (qopPart == "auth-int")
                        {
                            HA2 = md5.ComputeHash(encoding.GetBytes(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}:{1}:{2}", method, uriPart, md5.ComputeHash(encoding.GetBytes(bodyPart)))));
                            //The MD5 hash of the combined HA1 result, server nonce (nonce), request counter (nc), client nonce (cnonce), quality of protection code (qop) and HA2 result is calculated. The result is the "response" value provided by the client.
                            ResponseHash = md5.ComputeHash(encoding.GetBytes(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}:{1}:{2}:{3}:{4}:{5}", BitConverter.ToString(HA1).Replace("-", string.Empty).ToLowerInvariant(), nOncePart, BitConverter.ToString(HA2).Replace("-", string.Empty).ToLowerInvariant(), ncPart, cnOncePart, qopPart)));
                            result = string.Format(System.Globalization.CultureInfo.InvariantCulture, "Digest username=\"{0}\", realm=\"{1}\", nonce=\"{2}\", uri=\"{3}\", qop=\"{4}\" nc=\"{5} cnonce=\"{6}\"", usernamePart, realmPart, nOncePart, uriPart, qopPart, ncPart, cnOncePart);
                            if (!string.IsNullOrWhiteSpace(opaquePart)) result += "opaque=\"" + opaquePart + '"';
                        }
                        else
                        {
                            throw new NotImplementedException();
                        }
                    }
                    else // No Quality of Protection
                    {
                        HA2 = md5.ComputeHash(encoding.GetBytes(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}:{1}", method, uriPart)));
                        ResponseHash = md5.ComputeHash(encoding.GetBytes(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}:{1}:{2}", BitConverter.ToString(HA1).Replace("-", string.Empty).ToLowerInvariant(), nOncePart, BitConverter.ToString(HA2).Replace("-", string.Empty).ToLowerInvariant())));
                        result = string.Format(System.Globalization.CultureInfo.InvariantCulture, "Digest username=\"{0}\", realm=\"{1}\", nonce=\"{2}\", uri=\"{3}\", response=\"{4}\"", usernamePart, realmPart, nOncePart, uriPart, BitConverter.ToString(ResponseHash).Replace("-", string.Empty).ToLowerInvariant());
                    }
                }
            }
            return result;
        }

        //TryParseAuthorizationHeader

        //Values should be nullable...?
        public static bool TryParseTransportHeader(string value, out int ssrc, out System.Net.IPAddress source, out int serverRtpPort, out int serverRtcpPort, out int clientRtpPort, out int clientRtcpPort, out bool interleaved, out byte dataChannel, out byte controlChannel, out string mode, out bool unicast, out bool multicast)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new InvalidOperationException("value cannot be null or whitespace.");

            //layers = / Hops Ttl

            //Should also given tokens for profile e.g. SAVP or RAW etc.

            ssrc = 0;
            source = System.Net.IPAddress.Any;
            serverRtpPort = serverRtcpPort = clientRtpPort = clientRtcpPort = 0;
            dataChannel = 0;
            controlChannel = 1;
            interleaved = unicast = multicast = false;
            mode = string.Empty;
            try
            {
                //Get the recognized parts of information from the transportHeader
                string[] parts = value.Split(SemiColon);

                for (int i = 0, e = parts.Length; i < e; ++i)
                {
                    string[] subParts = parts[i].Split((char)Common.ASCII.EqualsSign);

                    switch (subParts[0])
                    {
                        case "unicast":
                            {
                                if (multicast) throw new InvalidOperationException("Cannot be unicast and multicast");
                                unicast = true;
                                continue;
                            }
                        case "multicast":
                            {
                                if (unicast) throw new InvalidOperationException("Cannot be unicast and multicast");
                                multicast = true;
                                continue;
                            }
                        case "mode":
                            {
                                mode = subParts[1];
                                continue;
                            }
                        case "source":
                            {
                                string sourcePart = subParts[1];

                                source = System.Net.IPAddress.Parse(sourcePart);
                                continue;
                            }
                        case "ssrc":
                            {
                                string ssrcPart = subParts[1];

                                if (false == int.TryParse(ssrcPart, out ssrc)) //plain int                        
                                    ssrc = int.Parse(ssrcPart, System.Globalization.NumberStyles.HexNumber); //hex
                                
                                continue;
                            }
                        case "client_port":
                            {
                                string[] clientPorts = subParts[1].Split(HyphenSign);

                                int clientPortsLength = clientPorts.Length;

                                if (clientPortsLength > 0)
                                {
                                    clientRtpPort = int.Parse(clientPorts[0], System.Globalization.CultureInfo.InvariantCulture);
                                    if (clientPortsLength > 1) clientRtcpPort = int.Parse(clientPorts[1], System.Globalization.CultureInfo.InvariantCulture);
                                }

                                continue;
                            }
                        case "interleaved":
                            {
                                interleaved = true;

                                //Should only be for Tcp
                                string[] channels = subParts[1].Split(TimeSplit, StringSplitOptions.RemoveEmptyEntries);

                                int channelsLength = channels.Length;

                                if (channelsLength > 1)
                                {
                                    //DataChannel
                                    dataChannel = byte.Parse(channels[0], System.Globalization.CultureInfo.InvariantCulture);
                                    //Control Channel
                                    if(channelsLength > 1) controlChannel = byte.Parse(channels[1], System.Globalization.CultureInfo.InvariantCulture);
                                }

                                continue;
                            }
                        case "server_port":
                            {
                                string[] serverPorts = subParts[1].Split(HyphenSign);

                                int serverPortsLength = serverPorts.Length;

                                if (serverPortsLength > 0)
                                {
                                    serverRtpPort = int.Parse(serverPorts[0], System.Globalization.CultureInfo.InvariantCulture);
                                    if (serverPortsLength > 1) serverRtcpPort = int.Parse(serverPorts[1], System.Globalization.CultureInfo.InvariantCulture);
                                }

                                continue;
                            }
                        default: continue;
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static string TransportHeader(string connectionType, int? ssrc, System.Net.IPAddress source, int? clientRtpPort, int? clientRtcpPort, int? serverRtpPort, int? serverRtcpPort, bool? unicast, bool? multicast, int? ttl, bool? interleaved, byte? dataChannel, byte? controlChannel)
        {
            if (string.IsNullOrWhiteSpace(connectionType)) throw new ArgumentNullException("connectionType");

            if (unicast.HasValue && multicast.HasValue && unicast.Value == multicast.Value) throw new InvalidOperationException("unicast and multicast cannot have the same value.");

            StringBuilder builder = null;

            try
            {
                builder = new StringBuilder();

                builder.Append(connectionType);

                if (source != null)
                {
                    builder.Append(SemiColon);

                    builder.Append("source=");
                    builder.Append(source);
                }

                if (unicast.HasValue && unicast.Value == true)
                {
                    builder.Append(SemiColon);

                    builder.Append("unicast");
                }
                else if (multicast.HasValue && multicast.Value == true)
                {
                    builder.Append(SemiColon);

                    builder.Append("unicast");
                }

                //Should eventually also allow for rtcp only?

                if (clientRtpPort.HasValue)
                {
                    builder.Append(SemiColon);

                    builder.Append("client_port=");
                    builder.Append(clientRtpPort.Value);

                    if (clientRtcpPort.HasValue)
                    {
                        builder.Append(HyphenSign);
                        builder.Append(clientRtcpPort);
                    }
                } //else if

                if (serverRtpPort.HasValue)
                {
                    builder.Append(SemiColon);

                    builder.Append("server_port=");
                    builder.Append(serverRtpPort.Value);

                    if (serverRtcpPort.HasValue)
                    {
                        builder.Append(HyphenSign);
                        builder.Append(serverRtcpPort);
                    }
                } //else if

                //

                if (interleaved.HasValue && interleaved.Value == true)
                {
                    builder.Append(SemiColon);

                    builder.Append("interleaved=");

                    if (dataChannel.HasValue)
                    {
                        builder.Append(dataChannel);
                    }

                    if (controlChannel.HasValue)
                    {
                        if (dataChannel.HasValue) builder.Append(HyphenSign);

                        builder.Append(controlChannel);
                    }
                }

                if (ttl.HasValue)
                {
                    builder.Append(SemiColon);

                    builder.Append("ttl=");

                    builder.Append(ttl);
                }

                if (ssrc.HasValue)
                {
                    builder.Append(SemiColon);

                    builder.Append("ssrc=");

                    builder.Append(ssrc.Value.ToString("X"));
                }

                return builder.ToString();
            }
            catch { throw; }
            finally { builder = null; }
        }

        //internal static string BuildHeader(string sep, params string[] values)
        //{
        //    return string.Join(sep, values, 0, values.Length - 1) + values.Last();
        //}
    
      

        /// <summary>
        /// Parses a RFC2326 Rtp-Info header, if two sub headers are present only the values from the last header are returned.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="url"></param>
        /// <param name="seq"></param>
        /// <param name="rtpTime"></param>
        /// <returns></returns>
        ///NEEDS TO RETURN []s instead of single values out Uri[], etc.
        ///Or make en enumerator...
        public static bool TryParseRtpInfo(string value, out Uri url, out int? seq, out int? rtpTime, out int? ssrc)
        {
            url = null;

            seq = rtpTime = ssrc = null;

            if (string.IsNullOrWhiteSpace(value)) return false;
            
            try
            {
                string[] allParts = value.Split(Comma);

                for (int i = 0, e = allParts.Length; i < e; ++i)
                {

                    string part = allParts[i];

                    if (string.IsNullOrWhiteSpace(part)) continue;                    

                    foreach (var token in part.Split(SemiColon))
                    {
                        string[] subParts = token.Split(ValueSplit, 2);

                        if (subParts.Length < 2) continue;

                        switch (subParts[0].Trim())
                        {
                            case "url":
                                {
                                    url = new Uri(subParts[1].Trim(), UriKind.RelativeOrAbsolute);

                                    continue;
                                }
                            case "seq":
                                {
                                    int parsed;

                                    if (int.TryParse(subParts[1].Trim(), out parsed))
                                    {
                                        seq = parsed;
                                    }

                                    continue;
                                }
                            case "rtptime":
                                {

                                    int parsed;

                                    if (int.TryParse(subParts[1].Trim(), out parsed))
                                    {
                                        rtpTime = parsed;
                                    }

                                    continue;
                                }
                            case "ssrc":
                                {
                                    string ssrcPart = subParts[1].Trim();

                                    int id;

                                    if (false == int.TryParse(ssrcPart, out id)) //plain int                        
                                        id = int.Parse(ssrcPart, System.Globalization.NumberStyles.HexNumber); //hex

                                    ssrc = (int)id;

                                    continue;
                                }
                            default: continue;
                        }
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool TryParseRtpInfo(string value, out string[] values)
        {
            values = null;

            if (string.IsNullOrWhiteSpace(value)) return false;

            values = value.Split(Comma);

            return true;
        }

        /// <summary>
        /// Creates a RFC2326 Rtp-Info header
        /// </summary>
        /// <param name="url"></param>
        /// <param name="seq"></param>
        /// <param name="rtpTime"></param>
        /// <returns></returns>
        public static string RtpInfoHeader(Uri url, int? seq, int? rtpTime, int? ssrc)
        {
            return (
                (url != null ? "url=" + url.ToString() + SemiColon.ToString() : string.Empty)
                + (seq.HasValue ? "seq=" + seq.Value + SemiColon.ToString() : string.Empty)
                + (rtpTime.HasValue ? "rtptime=" + rtpTime.Value + SemiColon.ToString() : string.Empty)
                + (ssrc.HasValue ? "ssrc=" + ssrc.Value.ToString("X") : string.Empty)
                );
        }

        public bool TryParseScale(out double result) { throw new NotImplementedException(); }

        public string ScaleHeader(double scale) { return scale.ToString(RtspMessage.VersionFormat, System.Globalization.CultureInfo.InvariantCulture); }

        public bool TryParseSpeed(out double result) { throw new NotImplementedException(); }

        public string SpeedHeader(double speed) { return speed.ToString(RtspMessage.VersionFormat, System.Globalization.CultureInfo.InvariantCulture); }

        public bool TryParseBlockSize(out int result) { throw new NotImplementedException(); }

        public string BlockSizeHeader(int blockSize) { return blockSize.ToString(); }

        //internal const string OnDemand = "On-demand";

        //internal const string DynamicOnDemand = "Dynamic On-demand";

        //internal const string Live = "Live";

        //internal const string LiveWithRecording = "Live with Recording";

        internal const string RandomAccess = "Random-Access";

        internal const string BeginningOnly = "Beginning-Only";

        internal const string NoSeeking = "No-seeking";

        public static string MediaPropertiesHeader() { throw new NotImplementedException(); }

        public bool TryParseMediaProperties() { throw new NotImplementedException(); }

        internal const string Unlimited = "Unlimited";

        internal const string TimeLimited = "Time-Limited";

        internal const string TimeDuration = "Time-Duration";

        public static string Retention() { throw new NotImplementedException(); }

        internal const string Immutable = "Immutable";

        internal const string Dynamic = "Dynamic";

        internal const string TimeProgressing = "Time-Progressing";

        public static string ContentModifcations() { throw new NotImplementedException(); }      
    }

    #region Reference

    //    11 Status Code Definitions

    //   Where applicable, HTTP status [H10] codes are reused. Status codes
    //   that have the same meaning are not repeated here. See Table 1 for a
    //   listing of which status codes may be returned by which requests.

    //11.1 Success 2xx

    //11.1.1 250 Low on Storage Space

    //   The server returns this warning after receiving a RECORD request that
    //   it may not be able to fulfill completely due to insufficient storage
    //   space. If possible, the server should use the Range header to
    //   indicate what time period it may still be able to record. Since other
    //   processes on the server may be consuming storage space
    //   simultaneously, a client should take this only as an estimate.

    //11.2 Redirection 3xx

    //   See [H10.3].

    //   Within RTSP, redirection may be used for load balancing or
    //   redirecting stream requests to a server topologically closer to the
    //   client.  Mechanisms to determine topological proximity are beyond the
    //   scope of this specification.









    //Schulzrinne, et. al.        Standards Track                    [Page 41]
 
    //RFC 2326              Real Time Streaming Protocol            April 1998


    //11.3 Client Error 4xx

    //11.3.1 405 Method Not Allowed

    //   The method specified in the request is not allowed for the resource
    //   identified by the request URI. The response MUST include an Allow
    //   header containing a list of valid methods for the requested resource.
    //   This status code is also to be used if a request attempts to use a
    //   method not indicated during SETUP, e.g., if a RECORD request is
    //   issued even though the mode parameter in the Transport header only
    //   specified PLAY.

    //11.3.2 451 Parameter Not Understood

    //   The recipient of the request does not support one or more parameters
    //   contained in the request.

    //11.3.3 452 Conference Not Found

    //   The conference indicated by a Conference header field is unknown to
    //   the media server.

    //11.3.4 453 Not Enough Bandwidth

    //   The request was refused because there was insufficient bandwidth.
    //   This may, for example, be the result of a resource reservation
    //   failure.

    //11.3.5 454 Session Not Found

    //   The RTSP session identifier in the Session header is missing,
    //   invalid, or has timed out.

    //11.3.6 455 Method Not Valid in This State

    //   The client or server cannot process this request in its current
    //   state.  The response SHOULD contain an Allow header to make error
    //   recovery easier.

    //11.3.7 456 Header Field Not Valid for Resource

    //   The server could not act on a required request header. For example,
    //   if PLAY contains the Range header field but the stream does not allow
    //   seeking.







    //Schulzrinne, et. al.        Standards Track                    [Page 42]
 
    //RFC 2326              Real Time Streaming Protocol            April 1998


    //11.3.8 457 Invalid Range

    //   The Range value given is out of bounds, e.g., beyond the end of the
    //   presentation.

    //11.3.9 458 Parameter Is Read-Only

    //   The parameter to be set by SET_PARAMETER can be read but not
    //   modified.

    //11.3.10 459 Aggregate Operation Not Allowed

    //   The requested method may not be applied on the URL in question since
    //   it is an aggregate (presentation) URL. The method may be applied on a
    //   stream URL.

    //11.3.11 460 Only Aggregate Operation Allowed

    //   The requested method may not be applied on the URL in question since
    //   it is not an aggregate (presentation) URL. The method may be applied
    //   on the presentation URL.

    //11.3.12 461 Unsupported Transport

    //   The Transport field did not contain a supported transport
    //   specification.

    //11.3.13 462 Destination Unreachable

    //   The data transmission channel could not be established because the
    //   client address could not be reached. This error will most likely be
    //   the result of a client attempt to place an invalid Destination
    //   parameter in the Transport field.

    //11.3.14 551 Option not supported

    //   An option given in the Require or the Proxy-Require fields was not
    //   supported. The Unsupported header should be returned stating the
    //   option for which there is no support.

    #endregion

    /// <summary>
    /// The status codes utilized in RFC2326 Messages given in response to a request
    /// </summary>
    public enum RtspStatusCode
    {
        Unknown = 0,
        // 1xx Informational.
        Continue = 100,

        // 2xx Success.
        OK = 200,
        Created = 201,
        LowOnStorageSpace = 250,

        // 3xx Redirection.
        MultipleChoices = 300,
        MovedPermanently = 301,
        Found = 302,
        SeeOther = 303,
        NotModified = 304,
        UseProxy = 305,

        // 4xx Client Error.
        BadRequest = 400,
        Unauthorized = 401,
        PaymentRequired = 402,
        Forbidden = 403,
        NotFound = 404,
        MethodNotAllowed = 405,
        NotAcceptable = 406,
        ProxyAuthenticationRequired = 407,
        RequestTimeOut = 408,
        Gone = 410,
        LengthRequired = 411,
        PreconditionFailed = 412,
        RequestMessageBodyTooLarge = 413,
        RequestUriTooLarge = 414,
        UnsupportedMediaType = 415,
        ParameterNotUnderstood = 451,
        Reserved = 452,
        NotEnoughBandwidth = 453,
        SessionNotFound = 454,
        MethodNotValidInThisState = 455,
        HeaderFieldNotValidForResource = 456,
        InvalidRange = 457,
        ParameterIsReadOnly = 458,
        AggregateOpperationNotAllowed = 459,
        OnlyAggregateOpperationAllowed = 460,
        UnsupportedTransport = 461,
        DestinationUnreachable = 462,
        DestinationProhibited = 463,
        DataTransportNotReadyYet = 464,
        NotificationReasonUnknown = 465,
        KeyManagementError = 466,

        ConnectionAuthorizationRequired = 470,
        ConnectionCredentialsNotAcception = 471,
        FaulireToEstablishSecureConnection = 472,
        
        // 5xx Server Error.
        InternalServerError = 500,
        NotImplemented = 501,
        BadGateway = 502,
        ServiceUnavailable = 503,
        GatewayTimeOut = 504,
        RtspVersionNotSupported = 505,
        OptionNotSupported = 551,
    }

    /// <summary>
    /// Enumeration to describe the available Rtsp Methods, used in responses
    /// </summary>
    public enum RtspMethod
    {
        UNKNOWN,
        ANNOUNCE,
        DESCRIBE,
        REDIRECT,
        OPTIONS,
        SETUP,
        GET_PARAMETER,
        SET_PARAMETER,
        PLAY,
        PLAY_NOTIFY,
        PAUSE,
        RECORD,
        TEARDOWN
    }

    /// <summary>
    /// Enumeration to indicate the type of RtspMessage
    /// </summary>
    public enum RtspMessageType
    {
        Invalid = 0,
        Request = 1,
        Response = 2,
    }

    /// <summary>
    /// Base class of RtspRequest and RtspResponse
    /// </summary>
    public class RtspMessage : Common.BaseDisposable, Common.IPacket
    {
        #region Statics

        //Used to format the version string
        public static string VersionFormat = "0.0";

        //System encoded 'Carriage Return' => \r and 'New Line' => \n
        internal const string CRLF = "\r\n";

        //The scheme of Uri's of RtspMessage's
        public const string ReliableTransport = "rtsp";
        
        //The scheme of Uri's of RtspMessage's which are usually being transported via udp
        public const string UnreliableTransport = "rtspu";

        //`Secure` RTSP...
        public const string SecureTransport = "rtps";

        //The maximum amount of bytes any RtspMessage can contain.
        public const int MaximumLength = 4096;

        //String which identifies a Rtsp Request or Response
        internal const string MessageIdentifier = "RTSP";
        
        //String which can be used to delimit a RtspMessage for preprocessing
        internal static string[] HeaderLineSplit = new string[] { CRLF };
        
        //String which is used to split Header values of the RtspMessage
        internal static char[] HeaderValueSplit = new char[] { ':' };

        // \r\n in the encoding of the request (Network Order)
        internal static byte[] LineEnds = (BitConverter.IsLittleEndian ? Common.ASCII.NewLine.Yield().Concat(Common.ASCII.LineFeed.Yield()) : Common.ASCII.LineFeed.Yield().Concat(Common.ASCII.NewLine.Yield())).ToArray();

        internal static int MinimumStatusLineSize = 9; //'RTSP/X.X ' 

        public static readonly Encoding DefaultEncoding = System.Text.Encoding.UTF8;

        public static byte[] ToHttpBytes(RtspMessage message, int majorVersion = 1, int minorVersion = 0, string sessionCookie = null, System.Net.HttpStatusCode statusCode = System.Net.HttpStatusCode.Unused)
        {

            if (message.MessageType == RtspMessageType.Invalid) return null;

            //Our result in a List
            List<byte> result = new List<byte>();

            //Our RtspMessage base64 encoded
            byte[] messageBytes;

            //Either a RtspRequest or a RtspResponse
            if (message.MessageType == RtspMessageType.Request)
            {
                //Get the body of the HttpRequest
                messageBytes = message.Encoding.GetBytes(System.Convert.ToBase64String(message.ToBytes()));
                
                //Form the HttpRequest Should allow POST and MultiPart
                result.AddRange(message.Encoding.GetBytes("GET " + message.Location + " HTTP " + majorVersion.ToString() + "." + minorVersion.ToString() + CRLF));
                result.AddRange(message.Encoding.GetBytes("Accept:application/x-rtsp-tunnelled" + CRLF));
                result.AddRange(message.Encoding.GetBytes("Pragma:no-cache" + CRLF));
                result.AddRange(message.Encoding.GetBytes("Cache-Control:no-cache" + CRLF));
                result.AddRange(message.Encoding.GetBytes("Content-Length:" + messageBytes.Length + CRLF));
                
                if (!string.IsNullOrWhiteSpace(sessionCookie))
                {
                    result.AddRange(message.Encoding.GetBytes("x-sessioncookie: " + System.Convert.ToBase64String(message.Encoding.GetBytes(sessionCookie)) + CRLF));
                }

                result.AddRange(message.Encoding.GetBytes(CRLF));
                result.AddRange(message.Encoding.GetBytes(CRLF));

                result.AddRange(messageBytes);
            }
            else
            {
                //Get the body of the HttpResponse
                messageBytes = message.Encoding.GetBytes(System.Convert.ToBase64String(message.ToBytes()));

                //Form the HttpResponse
                result.AddRange(message.Encoding.GetBytes("HTTP/1." + minorVersion.ToString() + " " + (int)statusCode + " " + statusCode + CRLF));
                result.AddRange(message.Encoding.GetBytes("Accept:application/x-rtsp-tunnelled" + CRLF));
                result.AddRange(message.Encoding.GetBytes("Pragma:no-cache" + CRLF));
                result.AddRange(message.Encoding.GetBytes("Cache-Control:no-cache" + CRLF));
                result.AddRange(message.Encoding.GetBytes("Content-Length:" + messageBytes.Length + CRLF));
                result.AddRange(message.Encoding.GetBytes("Expires:Sun, 9 Jan 1972 00:00:00 GMT" + CRLF));
                result.AddRange(message.Encoding.GetBytes(CRLF));
                result.AddRange(message.Encoding.GetBytes(CRLF));

                result.AddRange(messageBytes);
            }

            return result.ToArray();
        }

        public static RtspMessage FromHttpBytes(byte[] message, int offset, Encoding encoding = null)
        {
            //Sanity
            if (message == null) return null;
            if (offset > message.Length) throw new ArgumentOutOfRangeException("offset");

            //Use a default encoding if none was given
            if (encoding == null) encoding = RtspMessage.DefaultEncoding;

            //Parse the HTTP 
            string Message = encoding.GetString(message, offset, message.Length - offset);

            //Find the end of all the headers
            int headerEnd = Message.IndexOf(CRLF + CRLF);

            //Get the Http Body, It occurs after all the headers which ends with \r\n\r\n and is Base64 Encoded.
            string Body = Message.Substring(headerEnd);

            //Might want to provide the headers as an out param /.

            //Get the bytes of the underlying RtspMessage by decoding the Http Body which was encoded in base64
            byte[] rtspMessage = System.Convert.FromBase64String(Body);

            //Done
            return new RtspMessage(rtspMessage);
        }

        public static RtspMessage FromString(string data, System.Text.Encoding encoding = null)
        {
            if (string.IsNullOrWhiteSpace(data)) throw new InvalidOperationException("data cannot be null or whitespace.");

            if(encoding == null) encoding = RtspMessage.DefaultEncoding;

            return new RtspMessage(encoding.GetBytes(data), 0, encoding);
        }

        #endregion

        #region Fields

        double m_Version;

        int m_StatusCode;

        /// <summary>
        /// The firstline of the RtspMessage and the Body
        /// </summary>
        internal string m_Body = string.Empty;

        /// <summary>
        /// Dictionary containing the headers of the RtspMessage
        /// </summary>
        Dictionary<string, string> m_Headers = new Dictionary<string, string>();

        /// <summary>
        /// The Date and Time the message was created.
        /// </summary>
        public readonly DateTime Created = DateTime.UtcNow;

        /// <summary>
        /// The method of the message
        /// </summary>
        public string MethodString = string.Empty;

        /// <summary>
        /// Gets the <see cref="RtspMethod"/> which can be parsed from the <see cref="MethodString"/>
        /// </summary>
        public RtspMethod Method
        {
            get { RtspMethod parsed = RtspMethod.UNKNOWN; Enum.TryParse<RtspMethod>(MethodString, true, out parsed); return parsed; }
            set { MethodString = value.ToString(); }
        }

        /// <summary>
        /// The location of the message which is not usually utilized in responses.
        /// </summary>
        public Uri Location;

        Encoding m_Encoding = Encoding.UTF8;

        //Buffer to place data which is not complete
        System.IO.MemoryStream m_Buffer;

        //Set when parsing the first line if not already parsed, indicates the position of the beginning of the header data in m_Buffer.
        int m_HeaderOffset = 0;

        #endregion

        #region Properties            

        /// <summary>
        /// Indicates the UserAgent of this RtspRquest
        /// </summary>
        public String UserAgent { get { return GetHeader(RtspHeaders.UserAgent); } set { if (string.IsNullOrWhiteSpace(value)) throw new ArgumentNullException(); SetHeader(RtspHeaders.UserAgent, value); } }

        /// <summary>
        /// Indicates the StatusCode of the RtspResponse.
        ///  A value of 200 or less usually indicates success.
        /// </summary>
        public RtspStatusCode StatusCode { get { return (RtspStatusCode)m_StatusCode; } set { m_StatusCode = (int)value; } }

        public double Version { get { return m_Version; } set { m_Version = value; } }

        /// <summary>
        /// The length of the RtspMessage in bytes.
        /// (Calculated from the values parsed so some whitespace may be omitted, usually within +/- 4 bytes of the actual length)
        /// </summary>
        public int Length
        {
            get
            {
                int length = 0;

                if (MessageType == RtspMessageType.Request)
                    length += Encoding.GetByteCount(MethodString + " " + (Location != null ? Location.ToString() : string.Empty) + " " + RtspMessage.MessageIdentifier + '/' + Version.ToString(VersionFormat, System.Globalization.CultureInfo.InvariantCulture) + CRLF);
                else if (MessageType == RtspMessageType.Response)
                    length += Encoding.GetByteCount(MessageIdentifier + '/' + Version.ToString(RtspMessage.VersionFormat, System.Globalization.CultureInfo.InvariantCulture) + " " + ((int)StatusCode).ToString() + " " + StatusCode.ToString() + CRLF);

                return length + (string.IsNullOrEmpty(m_Body) ? 0 : 2 + m_Encoding.GetByteCount(m_Body)) + ( m_Headers.Count > 0 ? 2 + m_Headers.Sum(s => m_Encoding.GetByteCount(s.Key) + m_Encoding.GetByteCount(s.Value) + 3) : 0);
            }
        }

        /// <summary>
        /// The body of the RtspMessage
        /// </summary>
        public string Body
        {
            get { return m_Body; }
            set
            {
                m_Body = value;
                if (string.IsNullOrWhiteSpace(m_Body)) RemoveHeader(RtspHeaders.ContentLength);
                else
                {
                    //Ensure all requests end with a CRLF
                    if (false == m_Body.EndsWith(CRLF)) m_Body += CRLF;
                    SetHeader(RtspHeaders.ContentLength, this.Encoding.GetByteCount(m_Body).ToString());
                }
            }
        }       

        /// <summary>
        /// Indicates if this RtspMessage is a request or a response
        /// </summary>
        public RtspMessageType MessageType { get; internal set; }

        /// <summary>
        /// Gets or Sets the CSeq of this RtspMessage, if found and parsed; otherwise -1.
        /// </summary>
        public int CSeq
        {
            get
            {
                try
                {
                    string sequenceNumber = GetHeader(RtspHeaders.CSeq);
                    if (string.IsNullOrWhiteSpace(sequenceNumber)) return -1;
                    return Convert.ToInt32(sequenceNumber);
                }
                catch { return -1; }                
            }
            set
            {
                //Use the unsigned representation
                SetHeader(RtspHeaders.CSeq, ((uint)value).ToString());
            }
        }

        public int HeaderCount { get { return m_Headers.Count; } }

        /// <summary>
        /// Accesses the header value 
        /// </summary>
        /// <param name="header">The header name</param>
        /// <returns>The header value</returns>
        public string this[string header]
        {
            get { return GetHeader(header); }
            set { SetHeader(header, value); }
        }

        /// <summary>
        /// Gets or Sets the encoding of this RtspMessage. (Defaults to UTF-8)
        /// When set the `Content-Encoding` header is also set to the 'WebName' of the given Encoding.
        /// </summary>
        public Encoding Encoding
        {
            get { return m_Encoding; }
            set
            {
                m_Encoding = value;
                SetHeader(RtspHeaders.ContentEncoding, Encoding.WebName);
            }
        }

        /// <summary>
        /// Indicates when the RtspMessage was transferred if sent.
        /// </summary>
        public DateTime? Transferred { get; set; }

        /// <summary>
        /// Indicates if the RtspMessage is complete
        /// </summary>
        public bool IsComplete
        {
            get
            {
                //Disposed is complete 
                if (IsDisposed) return IsDisposed;

                //All requests must have a StatusLine
                if (MessageType == RtspMessageType.Invalid || m_Buffer != null && m_Buffer.Length <= MinimumStatusLineSize) return false;

                //No headers mean there is not a CSeq
                //All requests MUST contain a CSeq header.
                if (m_Headers.Count == 0 || CSeq == -1) return false;

                //See if there is a Content-Length header
                string contentLength = GetHeader(RtspHeaders.ContentLength);

                //Messages without a contentLength are complete
                if (string.IsNullOrWhiteSpace(contentLength)) return true;

                //If the content-length header cannot be parsed or the length > the data in the body the message is invalid
                int supposedCount;

                if (false == int.TryParse(contentLength, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out supposedCount)) return false;
                
                //Messages with ContentLength but no Body are not.
                if (supposedCount > 0 && string.IsNullOrWhiteSpace(m_Body)) return false; 

                //Determine if the count of the octets in the body is equal to the supposed amount
                return supposedCount == 0 || (Encoding.GetByteCount(m_Body) >= supposedCount);
            }
        }

        #endregion

        #region Constructor        

        /// <summary>
        /// Reserved
        /// </summary>
        internal RtspMessage() { }

        /// <summary>
        /// Constructs a RtspMessage
        /// </summary>
        /// <param name="messageType">The type of message to construct</param>
        public RtspMessage(RtspMessageType messageType, double? version = 1.0, Encoding encoding = null) { MessageType = messageType; Version = version ?? 1.0; Encoding = encoding ?? Encoding.UTF8; }

        /// <summary>
        /// Creates a RtspMessage from the given bytes
        /// </summary>
        /// <param name="bytes">The byte array to create the RtspMessage from</param>
        /// <param name="offset">The offset within the bytes to start creating the message</param>
        public RtspMessage(byte[] bytes, int offset = 0, Encoding encoding = null) : this(bytes, offset, bytes.Length - offset, encoding) { }

        public RtspMessage(Common.MemorySegment data, Encoding encoding = null) : this(data.Array, data.Offset, data.Count, encoding) { }
            
        /// <summary>
        /// Creates a managed representation of an abstract RtspMessage concept from RFC2326.
        /// </summary>
        /// <param name="packet">The array segment which contains the packet in whole at the offset of the segment. The Count of the segment may not contain more bytes than a RFC2326 message may contain.</param>
        /// <reference>
        /// RFC2326 - http://tools.ietf.org/html/rfc2326 - [Page 19]
        /// 4.4 Message Length
        ///When a message body is included with a message, the length of that
        ///body is determined by one of the following (in order of precedence):
        ///1.     Any response message which MUST NOT include a message body
        ///        (such as the 1xx, 204, and 304 responses) is always terminated
        ///        by the first empty line after the header fields, regardless of
        ///        the entity-header fields present in the message. (Note: An
        ///        empty line consists of only CRLF.)
        ///2.     If a Content-Length header field (section 12.14) is present,
        ///        its value in bytes represents the length of the message-body.
        ///        If this header field is not present, a value of zero is
        ///        assumed.
        ///3.     By the server closing the connection. (Closing the connection
        ///        cannot be used to indicate the end of a request body, since
        ///        that would leave no possibility for the server to send back a
        ///        response.)
        ///Note that RTSP does not (at present) support the HTTP/1.1 "chunked"
        ///transfer coding(see [H3.6]) and requires the presence of the
        ///Content-Length header field.
        ///    Given the moderate length of presentation descriptions returned,
        ///    the server should always be able to determine its length, even if
        ///    it is generated dynamically, making the chunked transfer encoding
        ///    unnecessary. Even though Content-Length must be present if there is
        ///    any entity body, the rules ensure reasonable behavior even if the
        ///    length is not given explicitly.
        /// </reference>        
        public RtspMessage(byte[] data, int offset, int length, Encoding encoding = null)
        {
            //Sanely
            //length could be > data.Length or offset could be allowed to be negitive...
            if (data == null || offset < 0 || length == 0) 
            {
                return;
            }

            //Use the static line ends (\r\n)
            byte[] lineEnds = LineEnds;

            //use the supplied encoding if present.
            if (encoding != null && encoding != Encoding)
            {
                Encoding = encoding;

                lineEnds = Encoding.Convert(RtspMessage.DefaultEncoding, Encoding, lineEnds);
            }

            //Syntax, what syntax? there is no syntax ;)

            int start = offset, count = length, firstLineLength = -1;

            //RTSP in the encoding of the request
            //byte[] encodedIdentifier = Encoding.GetBytes(MessageIdentifier); int encodedIdentifierLength = encodedIdentifier.Length;

            int requiredEndLength = 1; //2.0 specifies that CR and LF must be present

            //Skip any non character data.
            while (false == char.IsLetter((char)data[start]))
            {
                if (--count <= 0) return;
                ++start;
            }

            //Find the end of the first line first,
            //If it cannot be found then the message does not contain the end line
            firstLineLength = Utility.ContainsBytes(data, ref start, ref count, lineEnds, 0, requiredEndLength);

            //Assume everything belongs to the first line.
            if (firstLineLength - offset > count || firstLineLength == -1 || firstLineLength < 9)
            {
                start = offset;
                firstLineLength = length;
            }
            else
            {
                //The length of the first line is given by the difference of start
                firstLineLength -= start;
            }


            //Create the buffer
            m_Buffer = new System.IO.MemoryStream(firstLineLength);

            //Write the data to the buffer
            m_Buffer.Write(data, start, firstLineLength);

            //Attempt to parse the data given as a StatusLine.
            if (false == ParseStatusLine()) goto DisposeBuffer;

            //A valid looking first line has been found...
            //Parse the headers and body if present

            if (m_HeaderOffset < count)
            {
                //The count of how many bytes are used to take up the header is given by
                //The amount of bytes (after the first line PLUS the length of CRLF in the encoding of the message) minus the count of the bytes in the packet
                int headerStart = m_HeaderOffset,
                remainingBytes = count - headerStart;

                //If the scalar is valid
                if (remainingBytes > 0 && headerStart + remainingBytes <= count)
                {
                    //Position the buffer, indicate no headers remain in the buffer
                    m_Buffer.Position = m_HeaderOffset = 0;

                    //Write that data
                    m_Buffer.Write(data, start + headerStart, remainingBytes);

                    //Ensure the length is set
                    m_Buffer.SetLength(remainingBytes);

                    //Position the buffer
                    m_Buffer.Position = 0;

                    //Parse the headers and body
                    ParseBody();
                }                
            } //All messages must have at least a CSeq header.
            //else MessageType = RtspMessageType.Invalid;

        DisposeBuffer:
            DisposeBuffer();
        }

        /// <summary>
        /// Creates a RtspMessage by copying the properties of another.
        /// </summary>
        /// <param name="other">The other RtspMessage</param>
        public RtspMessage(RtspMessage other)
        {
            m_Body = other.m_Body;
            m_Headers = other.m_Headers;
            m_StatusCode = other.m_StatusCode;
            m_Version = other.m_Version;
        }

        ~RtspMessage() { Dispose(); }

        #endregion

        #region Methods

        void DisposeBuffer()
        {
            if (m_Buffer != null && m_Buffer.CanWrite) m_Buffer.Dispose();

            m_Buffer = null;
        }

        virtual protected bool ParseStatusLine()
        {

            if (IsDisposed) return false;

            //Dont rely on the message type obtained previously
            //if (MessageType != RtspMessageType.Invalid) return true;

            //Dont rely on the offset
            //if (m_HeaderOffset > 0) return true;

            //Check if any headers have been parsed already.
            if (m_Headers.Count > 0) return true;

            //Determine how much data is present.
            int count = (int)m_Buffer.Length, index = -1;

            //Ensure enough data is availble to parse.
            if (count <= MinimumStatusLineSize) return false;

            //Always from the beginning of the buffer.
            m_Buffer.Seek(0, System.IO.SeekOrigin.Begin);

            //Get what we believe to be the first line
            //... containing the method to be applied to the resource,the identifier of the resource, and the protocol version in use;
            string StatusLine = Utility.ReadLine(m_Buffer, Encoding);

            //If it was not present then do not parse further
            if (StatusLine.Length < MinimumStatusLineSize)
            {
                MessageType = RtspMessageType.Invalid;

                return false;
            }

            //Cache the length of what we read.
            
            //m_HeaderOffset is still set when parsing fails but that shouldn't be an issue because it's reset when called again.
            m_HeaderOffset = StatusLine.Length;

            //Trim any whitespace
            StatusLine = StatusLine.TrimStart();

            //Determine where `RTSP` occurs.
            index = StatusLine.IndexOf(MessageIdentifier);

            //If it was not present then do not parse further
            if (index == -1)
            {
                MessageType = RtspMessageType.Invalid;

                return false;
            }

            //Determine the message type by there the identifier occurs.
            MessageType = index == 0 ? RtspMessageType.Response : RtspMessageType.Request;

            //Make an array of sub strings delemited by ' '
            string[] parts = StatusLine.Split((char)Common.ASCII.Space);

            //There must be 3 parts or parsing will not occur.
            if (parts.Length < 3) MessageType = RtspMessageType.Invalid;

            //Could assign version, then assign Method and Location
            if (MessageType == RtspMessageType.Request)
            {
                //C->S[0]SETUP[1]rtsp://example.com/media.mp4/streamid=0[2]RTSP/1.0()                  

                MethodString = parts[0].Trim();

                if (string.IsNullOrWhiteSpace(MethodString) ||
                    false == Uri.TryCreate(parts[1], UriKind.RelativeOrAbsolute, out Location) ||
                    parts[2].Length <= 5 ||
                    false == double.TryParse(Utility.ExtractPrecisionNumber(parts[2].Substring(5)), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out m_Version))
                {
                    return false;
                }
            }
            else if (MessageType == RtspMessageType.Response)
            {
                //S->C[0]RTSP/1.0[1]200[2]OK()

                if (false == int.TryParse(parts[1], System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out m_StatusCode) ||
                    parts[0].Length <= 5 ||
                    false == double.TryParse(Utility.ExtractPrecisionNumber(parts[0].Substring(5)), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out m_Version))
                {
                    return false;
                }
            }
            else return false; //This is an invalid message

            //The status line was parsed.
            return true;
        }

        virtual protected bool ParseHeaders()
        {
            if (IsDisposed) return false;

            //Headers were parsed if there is already a body.
            if (false == string.IsNullOrWhiteSpace(m_Body)) return true;

            //Need 2 empty lines to end the header section
            int emptyLine = 0;

            //Keep track of the position
            long position = m_Buffer.Position, max = m_Buffer.Length;

            //Ensure at the beginning of the buffer.
            m_Buffer.Seek(m_HeaderOffset, System.IO.SeekOrigin.Begin);

            //While we didn't find the end of the header section
            while (position < max)
            {
                //Read a line using the encoding determined so far.
                string rawLine = Utility.ReadLine(m_Buffer, Encoding);

                //Check for the empty line
                if (string.IsNullOrWhiteSpace(rawLine))
                {
                    //Can't have 3 consecutive emptyLines (counted by individualtity of \r \n occurances)
                    if (++emptyLine < 3) continue;

                    break;
                }

                //We only want the first 2 sub strings to allow for headers which have a ':' in the data
                //E.g. Rtp-Info: rtsp://....
                string[] parts = rawLine.Split(HeaderValueSplit, 2);

                //If this be a valid header set it
                if (parts.Length > 1)
                {
                    //Get the 'name' of the header 
                    string headerName = parts[0];

                    //If the name is not null or empty and begins with a Letter
                    if (false == string.IsNullOrEmpty(headerName) && char.IsLetter(headerName[0]))
                    {
                        //This is valid header
                        SetHeader(headerName, parts[1]);

                        //Empty line count must be reset
                        emptyLine = 0;

                        //Move the position
                        position += rawLine.Length;

                        //Do another loop
                        continue;
                    }
                }
            }

            //If there is more data in the headers and the header section was 'incorrectly terminated' by an invalid header or otherwise
            //Then there will be more data in m_Buffer, specifically Content-Length if there is a body and it has not yet been parsed.

            //Headers were parsed if there were 1 empty lines.
            try { return CSeq >= 0 && emptyLine > 0; }
            catch { return false; }
        }

        virtual protected bool ParseBody()
        {
            if (IsDisposed) return false;

            if (IsComplete) return true;

            if (false == ParseHeaders()) return false;

            //Get the content encoding required by the headers for the body
            string contentEncoding = GetHeader(RtspHeaders.ContentEncoding);

            System.Text.Encoding contentDecoder = Encoding;

            //If there was a content-Encoding header then set it now;
            if (false == string.IsNullOrWhiteSpace(contentEncoding))
            {
                //Check for the requested encoding
                contentEncoding = contentEncoding.Trim();
                System.Text.EncodingInfo requested = System.Text.Encoding.GetEncodings().FirstOrDefault(e => string.Compare(e.Name, contentEncoding, false, System.Globalization.CultureInfo.InvariantCulture) == 0);

                //If the encoding could not be found then throw an exception giving the required information.
                if (requested == null) Common.ExceptionExtensions.CreateAndRaiseException(contentEncoding, "The given message was encoded in a Encoding which is not present on this system and no fallback encoding was acceptible to decode the message. The tag has been set the value of the requested encoding");
                else contentDecoder = requested.GetEncoding();
            }

            //See if there is a Content-Length header
            string contentLength = GetHeader(RtspHeaders.ContentLength);

            //No content length means the body is parsed.
            if (string.IsNullOrWhiteSpace(contentLength)) return true;

            //Quite possibly should be long
            int supposedCount, max = (int)m_Buffer.Length;

            //If there is a header check its value, it will the only way to validate the body
            if (int.TryParse(contentLength, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out supposedCount))
            {
                //Empty body
                if (supposedCount == 0) m_Body = string.Empty;
                else
                {
                    int position = (int)m_Buffer.Position;

                    //Get the body of the message
                    m_Body = contentDecoder.GetString(m_Buffer.GetBuffer(), position, max - position);

                    //Ensure it is not larger than indicated.
                    if (m_Body.Length > supposedCount) m_Body = m_Body.Substring(0, supposedCount);
                }

            }//There was no Content-Length header or it was invalid
            else
            {
                MessageType = RtspMessageType.Invalid;

                //No longer needed.
                DisposeBuffer();

                return false;
            }

            //No longer needed.
            DisposeBuffer();

            //Body was parsed or started to be parsed.
            return true;
        }

        /// <summary>
        /// Creates a 'string' representation of the RtspMessage including all binary data contained therein.
        /// </summary>
        /// <returns>A string which contains the entire message itself in the encoding of the RtspMessage.</returns>
        public override string ToString()
        {
            return Encoding.GetString(ToBytes());
        }

        /// <summary>
        /// Gets an array of all headers present in the RtspMessage
        /// </summary>
        /// <returns>The array containing all present headers</returns>
        public virtual IEnumerable<string> GetHeaders() { return m_Headers.Keys.ToList(); }

        /// <summary>
        /// Gets a header value with cases insensitivity
        /// </summary>
        /// <param name="name">The name of the header</param>
        /// <returns>The header value if found, otherwise null.</returns>
        internal string GetHeader(string name, out string actualName)
        {
            actualName = null;
            if (string.IsNullOrWhiteSpace(name)) return null;
            foreach (string headerName in GetHeaders())
                if (string.Compare(name, headerName, true) == 0)
                {
                    actualName = headerName;
                    return m_Headers[headerName];
                }
            return null;
        }

        public virtual string GetHeader(string name)
        {
            return GetHeader(name, out name);
        }

        /// <summary>
        /// Sets or adds a header value
        /// </summary>
        /// <param name="name">The name of the header</param>
        /// <param name="value">The value of the header</param>
        public virtual void SetHeader(string name, string value)
        {
            //If the name is not valid then return without adding it
            if (string.IsNullOrWhiteSpace(name) || !char.IsLetter(name[0])) return;

            //If there is no value for the header then return without adding it
            if (string.IsNullOrWhiteSpace(value)) return;
            
            string actualName = null;
            
            //If the header with the same name has already been matched add it
            if (ContainsHeader(name, out actualName)) m_Headers[actualName] = value; 
            else m_Headers.Add(name, value);
        }

        public virtual void AppendOrSetHeader(string name, string value)
        {
            if (string.IsNullOrWhiteSpace(name)) return;

            string containedHeader = null;

            if (!ContainsHeader(name, out containedHeader)) SetHeader(name, value);
            else m_Headers[containedHeader] += ';' + value;
        }

        /// <summary>
        /// Indicates of the RtspMessage contains a header with the given name.
        /// </summary>
        /// <param name="name">The name of the header to find</param>
        /// <param name="headerName">The value which is actually the name of the header searched for</param>
        /// <returns>True if contained, otherwise false</returns>
        internal bool ContainsHeader(string name, out string headerName)
        {
            headerName = null;
            if (string.IsNullOrWhiteSpace(name)) return false;
            return GetHeader(name, out headerName) != null;
        }

        public virtual bool ContainsHeader(string name)
        {
            return ContainsHeader(name, out name);
        }

        /// <summary>
        /// Removes a header from the RtspMessage
        /// </summary>
        /// <param name="name">The name of the header to remove</param>
        /// <returns>True if removed, false otherwise</returns>
        public virtual bool RemoveHeader(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            else
            {
                GetHeader(name, out name);
                if (false == string.IsNullOrWhiteSpace(name)) return m_Headers.Remove(name);
                return false;
            }
        }

        /// <summary>
        /// Creates a Packet from the RtspMessage which can be sent on the network
        /// </summary>
        /// <returns>The packet which represents this RtspMessage</returns>
        public virtual byte[] ToBytes()
        {
            List<byte> result = new List<byte>(RtspMessage.MaximumLength / 7);

            if (MessageType == RtspMessageType.Request || MessageType == RtspMessageType.Invalid)
                result.AddRange(Encoding.GetBytes(Method.ToString() + " " + ( Location == null ? "*" : Location.ToString()) + " " + RtspMessage.MessageIdentifier + '/' + Version.ToString(VersionFormat, System.Globalization.CultureInfo.InvariantCulture) + CRLF));
            else if (MessageType == RtspMessageType.Response)
                result.AddRange(Encoding.GetBytes(MessageIdentifier + '/' + Version.ToString(RtspMessage.VersionFormat, System.Globalization.CultureInfo.InvariantCulture) + " " + ((int)StatusCode).ToString() + " " + StatusCode.ToString() + CRLF));

            byte[] encodedCRLF = Encoding.GetBytes(CRLF);

            //Write headers
            foreach (KeyValuePair<string, string> header in m_Headers/*.OrderBy((key) => key.Key).Reverse()*/)
            {
                result.AddRange(Encoding.GetBytes(header.Key + ": " + header.Value));
                result.AddRange(encodedCRLF);
            }

            //End Header
            result.AddRange(encodedCRLF);

            //Write body if required 
            if (!string.IsNullOrWhiteSpace(m_Body)) result.AddRange(Encoding.GetBytes(m_Body));

            //if (result.Count > RtspMessage.MaximumLength) throw new RtspMessageException("The message cannot be larger than '" + RtspMessage.MaximumLength + "' bytes. The Tag property contains the resulting binary data,", null, this, result);

            return result.ToArray();
        }

        public virtual IEnumerable<byte> Prepare() { return ToBytes(); }

        #endregion

        #region Overrides

        /// <summary>
        /// Disposes of all resourced used by the RtspMessage
        /// </summary>
        public override void Dispose()
        {
            if (IsDisposed) return;            

            //Call the base implementation
            base.Dispose();

            //No longer needed.
            DisposeBuffer();

            //Clear local references (Will change output of ToString())
            //m_Body = null;

            //m_Headers.Clear();
        }

        public override int GetHashCode()
        {
            return (int)((int)MessageType | (int)Method ^ (int)StatusCode) ^ (string.IsNullOrWhiteSpace(m_Body) ? Length : m_Body.GetHashCode()) ^ (m_Headers.Count ^ CSeq);
        }

        public override bool Equals(object obj)
        {
            if (System.Object.ReferenceEquals(this, obj)) return true;

            if (false == (obj is RtspMessage)) return false;

            RtspMessage other = obj as RtspMessage;

            //Fast path doesn't show true equality.
            //other.Created != Created
            return other.MessageType != MessageType 
                ||
                other.Version != Version
                ||
                other.Method != Method
                ||
                other.m_Headers.Count != m_Headers.Count
                ||
                other.CSeq != CSeq
                ||
                other.Body != Body
                || 
                other.Length != Length;
        }

        #endregion

        #region Operators

        public static bool operator ==(RtspMessage a, RtspMessage b)
        {
            object boxA = a, boxB = b;
            return boxA == null ? boxB == null : a.Equals(b);
        }

        public static bool operator !=(RtspMessage a, RtspMessage b) { return !(a == b); }

        #endregion

        #region IPacket

        DateTime Common.IPacket.Created
        {
            get { return Created; }
        }


        bool Common.IPacket.IsReadOnly
        {
            get { return false; }
        }

        long Common.IPacket.Length 
        {
            get { return (long)Length; }
        }

        /// <summary>
        /// Completes the RtspMessage from either the buffer or the socket.
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public virtual int CompleteFrom(System.Net.Sockets.Socket socket, Common.MemorySegment buffer)
        {

            if (socket == null && buffer.IsDisposed || buffer.Count == 0)
            {
                //goto InvalidMessage;
                return 0;
            }

            //Don't check IsComplete because of the notion of how a RtspMessage can be received.

            int received = 0;

            //Create the buffer if it was null
            if (m_Buffer == null || false == m_Buffer.CanWrite) m_Buffer = new System.IO.MemoryStream();
            else //Otherwise prepare to append the buffer
            {
                m_Buffer.Seek(0, System.IO.SeekOrigin.End);

                m_Buffer.SetLength(m_Buffer.Length + buffer.Count);
            }
            
            //If there was a buffer
            if (buffer != null)
            {
                //Write the new data
                m_Buffer.Write(buffer.Array, buffer.Offset, received += buffer.Count);

                //Go to the beginning
                m_Buffer.Seek(0, System.IO.SeekOrigin.Begin);
            }

            //If the status line was not parsed return the number of bytes written
            if (false == ParseStatusLine())
            {
                goto DisposeBuffer;
            } 

            //Should first check for cSeq header...
            string sCseq = GetHeader(RtspHeaders.CSeq);

            //See if there is a Content-Length header
            string contentLength = GetHeader(RtspHeaders.ContentLength);

            //Messages without a contentLength are not complete
            if (string.IsNullOrWhiteSpace(sCseq) || string.IsNullOrWhiteSpace(contentLength))
            {
                //Seek to the headerOffset (set in ParseStatusLine())
                m_Buffer.Seek(m_HeaderOffset, System.IO.SeekOrigin.Begin);

                //If the header section was not parsed indicate how much was written
                if (false == ParseHeaders())
                {
                    goto DisposeBuffer;
                }
            }

            //If the body is now parsed then we are done.
            if (null == m_Body && ParseBody()) goto DisposeBuffer;

            //Calulcate the amount of bytes in the body
            int encodedBodyCount = Encoding.GetByteCount(m_Body), supposedCount;

            //If the content-length header cannot be parsed or the length > the data in the body the message is invalid
            if (false == int.TryParse(contentLength, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out supposedCount)) return 0;

            //Determine how much remaing
            int remaining = supposedCount - encodedBodyCount;

            //If there are remaining octetes then complete the RtspMessage
            if (remaining > 0)
            {
                //Store the error
                System.Net.Sockets.SocketError error = System.Net.Sockets.SocketError.SocketError;

                //Keep track of whats received as of yet and where
                int justReceived = 0, offset = buffer.Offset;

                //While there is something to receive.
                while (remaining > 0)
                {
                    //Receive max more if there is a socket
                    justReceived = socket == null ? remaining : Utility.AlignedReceive(buffer.Array, offset, remaining, socket, out error);

                    //If anything was present then add it to the body.
                    if (justReceived > 0)
                    {
                        //Concatenate the result into the body
                        m_Body += Encoding.GetString(buffer.Array, offset,  Math.Min(remaining, justReceived));

                        remaining -= justReceived;

                        received += justReceived;
                    }

                    //If any socket error occured besides a timeout or a block then stop trying to receive.
                    if (error != System.Net.Sockets.SocketError.TimedOut && error != System.Net.Sockets.SocketError.TryAgain) break;
                }
            }
            
            // We don't want the buffer around anymore
        DisposeBuffer:
            DisposeBuffer();

            //Return the amount of bytes consumed.
            return received;

        }

        #endregion
    }
}
