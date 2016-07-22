using System;
using System.Text;
namespace Media.Http
{
    /// <summary>
    /// Header Definitions from RFC7231
    /// https://tools.ietf.org/html/RFC7231
    /// </summary>
    public class HttpHeaders
    {

        public const char HyphenSign = (char)Common.ASCII.HyphenSign, SemiColon = (char)Common.ASCII.SemiColon, Comma = (char)Common.ASCII.Comma;

        internal protected static string[] TimeSplit = new string[] { HyphenSign.ToString(), SemiColon.ToString() };

        internal protected static char[] SpaceSplit = new char[] { (char)Common.ASCII.Space, Comma };

        internal protected static char[] ValueSplit = new char[] { (char)Common.ASCII.EqualsSign, SemiColon };

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
        public const string ContentDisposition = "Content-Disposition";
        public const string ContentType = "Content-Type";
        public const string Date = "Date";
        public const string Expires = "Expires";
        public const string From = "From";
        public const string Host = "Host";
        public const string LastModified = "Last-Modified";
        public const string IfMatch = "If-Match";
        public const string IfModifiedSince = "If-Modified-Since";
        public const string IfNoneMatch = "If-None-Match";
        public const string Location = "Location";
        public const string ETag = "MTag";
        public const string ProxyAuthenticate = "Proxy-Authenticate";
        public const string ProxyAuthenticationInfo = "Proxy-Authentication-Info";
        public const string ProxyAuthorization = "Proxy-Authorization";
        public const string ProxyRequire = "Proxy-Require";
        public const string ProxySupported = "Proxy-Supported";
        public const string Range = "Range";
        public const string Referrer = "Referrer";
        public const string RequestStatus = " Request-Status";
        public const string Session = "Session";
        public const string Server = "Server";
        
        //https://www.ietf.org/rfc/rfc2616.txt @ 14.39 TE
        public const string TE = "TE"; //Extension Transfer Encodings..

        public const string TerminateReason = "Terminate-Reason";
        public const string Transport = "Transport";
        public const string Trailer = "Trailer";
        public const string TransferEncoding = "Transfer-Encoding";
        public const string Unsupported = "Unsupported";
        public const string UserAgent = "User-Agent";
        public const string Via = "Via";
        public const string WWWAuthenticate = "WWW-Authenticate";

        internal protected HttpHeaders() { }

        //Could move to HeaderFields to avoid the duplicate name ...

        //https://www.w3.org/Protocols/rfc2616/rfc2616-sec13.html

        //                             was \u+3309, Combining Right Half Ring Below, works around CacheControl already being defined..
        // is now \u+0331, Combining Macron Below because it looks better
        public sealed class Cache̱Control   //Ξ,ϰ,ϫ or any other such character also works and it's slightly easier to type but I am not sure it `looks` better
        {
            internal const string only_if_cached = "only-if-cached";

            public const string OnlyIfCached = only_if_cached;

            //cache-extension

            //Response

            internal const string @public = "public";

            public const string Public = @public;

            internal const string @private = "private";

            public const string Private = @private;

            internal const string s_maxage = "s-maxage";

            public const string SMaxAge = s_maxage;

            internal const string min_fresh = "min-fresh";

            public const string MinFresh = min_fresh;

            internal const string min_stale = "min-stale";            

            public const string MinStale = min_stale;

            internal const string no_transform = "no_transform";

            public const string NoTransform = no_transform;

            internal const string must_revalidate = "must-revalidate";

            public const string MustRevalidate = must_revalidate;

            internal const string proxy_revalidate = "proxy-revalidate";

            public const string ProxyRevalidate = proxy_revalidate;

            //Both

            internal const string max_age = "max-age";

            public const string MaxAge = max_age;

            internal const string no_cache = "no-cache";

            public const string NoCache = no_cache;

            internal const string no_store = "no-store";

            public const string NoStore = no_store;
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
        public static string RangeHeader(string startPart, string endPart, string type = "bytes", string timePart = null, char? typeSeperator = null, char? valueSeperator = null)
        {
            return string.Concat(string.Concat(type, (typeSeperator ?? (char)Common.ASCII.EqualsSign).ToString()), string.Join((valueSeperator ?? (char)Common.ASCII.HyphenSign).ToString(), startPart, endPart));
        }

        public static string BasicAuthorizationHeader(Encoding encoding, System.Net.NetworkCredential credential) { return AuthorizationHeader(encoding, string.Empty, null, System.Net.AuthenticationSchemes.Basic, credential); }

        public static string DigestAuthorizationHeader(Encoding encoding, HttpMethod method, Uri location, System.Net.NetworkCredential credential, string qopPart = null, string ncPart = null, string nOncePart = null, string cnOncePart = null, string opaquePart = null, bool rfc2069 = false, string algorithmPart = null, string bodyPart = null) { return AuthorizationHeader(encoding, method.ToString(), location, System.Net.AuthenticationSchemes.Digest, credential, qopPart, ncPart, nOncePart, cnOncePart, opaquePart, rfc2069, algorithmPart, bodyPart); }

        //string method
        internal protected static string AuthorizationHeader(Encoding encoding, string methodString, Uri location, System.Net.AuthenticationSchemes scheme, System.Net.NetworkCredential credential, string qopPart = null, string ncPart = null, string nOncePart = null, string cnOncePart = null, string opaquePart = null, bool rfc2069 = false, string algorithmPart = null, string bodyPart = null)
        {
            string result = string.Empty;

            switch (scheme)
            {
                case System.Net.AuthenticationSchemes.None: break;
                case System.Net.AuthenticationSchemes.Basic:
                    {
                        //http://en.wikipedia.org/wiki/Basic_access_authentication
                        //Don't use the domain.

                        //Don't use `basic` because apparently case is REALLY important at this point...
                        //result = string.Concat(Http.HeaderFields.Authorization.Basic, new string((char)Common.ASCII.Space, 1), Convert.ToBase64String(encoding.GetBytes(credential.UserName + ':' + credential.Password)));

                        result = string.Concat("Basic", new string((char)Common.ASCII.Space, 1), Convert.ToBase64String(encoding.GetBytes(credential.UserName + ':' + credential.Password)));

                        break;
                    }
                case System.Net.AuthenticationSchemes.Digest:
                    {
                        //http://www.ietf.org/rfc/rfc2617.txt

                        //Example 
                        //Authorization: Digest username="admin", realm="GeoVision", nonce="b923b84614fc11c78c712fb0e88bc525", uri="rtsp://203.11.64.27:8554/CH001.sdp", response="d771e4e5956e3d409ce5747927db10af"\r\n

                        //Todo Check that Digest works with Options * or when uriPart is \

                        string usernamePart = credential.UserName,
                           realmPart = credential.Domain ?? "//",
                           uriPart = location != null && location.IsAbsoluteUri ? location.AbsoluteUri : new String((char)Common.ASCII.BackSlash, 1);

                        if (string.IsNullOrWhiteSpace(nOncePart))
                        {
                            //Contains two sequential 32 bit units from the Random generator for now
                            nOncePart = ((long)(Utility.Random.Next(int.MaxValue) << 32 | Utility.Random.Next(int.MaxValue))).ToString("X");
                        }

                        //Need to look at this again
                        if (false == string.IsNullOrWhiteSpace(qopPart))
                        {
                            if (false == string.IsNullOrWhiteSpace(ncPart)) ncPart = (int.Parse(ncPart) + 1).ToString();
                            else ncPart = "00000001";

                            if (string.IsNullOrWhiteSpace(cnOncePart)) cnOncePart = Utility.Random.Next(int.MaxValue).ToString("X");
                        }

                        //http://en.wikipedia.org/wiki/Digest_access_authentication
                        //The MD5 hash of the combined username, authentication realm and password is calculated. The result is referred to as HA1.
                        byte[] HA1 = Cryptography.MD5.GetHash(encoding.GetBytes(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}:{1}:{2}", credential.UserName, realmPart, credential.Password)));

                        //The MD5 hash of the combined method and digest URI is calculated, e.g. of "GET" and "/dir/index.html". The result is referred to as HA2.
                        byte[] HA2 = null;

                        //Need to format based on presence of fields qop...
                        byte[] ResponseHash;

                        //If there is a Quality of Protection
                        if (qopPart != null)
                        {
                            if (qopPart .Equals(Http.HeaderFields.Authorization.Attributes.qop, StringComparison.OrdinalIgnoreCase))
                            {
                                HA2 = Cryptography.MD5.GetHash(encoding.GetBytes(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}:{1}", methodString, uriPart)));
                                //The MD5 hash of the combined HA1 result, server nonce (nonce), request counter (nc), client nonce (cnonce), quality of protection code (qop) and HA2 result is calculated. The result is the "response" value provided by the client.
                                ResponseHash = Cryptography.MD5.GetHash(encoding.GetBytes(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}:{1}:{2}:{3}:{4}:{5}", BitConverter.ToString(HA1).Replace("-", string.Empty).ToLowerInvariant(), nOncePart, BitConverter.ToString(HA2).Replace("-", string.Empty).ToLowerInvariant(), ncPart, cnOncePart, qopPart)));
                                result = string.Format(System.Globalization.CultureInfo.InvariantCulture, "Digest username=\"{0}\", realm=\"{1}\", nonce=\"{2}\", uri=\"{3}\", qop=\"{4}\" nc=\"{5} cnonce=\"{6}\"", usernamePart, realmPart, nOncePart, uriPart, qopPart, ncPart, cnOncePart);
                                if (false == string.IsNullOrWhiteSpace(opaquePart)) result += "opaque=\"" + opaquePart + '"';
                            }
                            else if (qopPart.Equals(Http.HeaderFields.Authorization.Attributes.authint, StringComparison.OrdinalIgnoreCase))
                            {
                                HA2 = Cryptography.MD5.GetHash(encoding.GetBytes(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}:{1}:{2}", methodString, uriPart, Cryptography.MD5.GetHash(encoding.GetBytes(bodyPart)))));
                                //The MD5 hash of the combined HA1 result, server nonce (nonce), request counter (nc), client nonce (cnonce), quality of protection code (qop) and HA2 result is calculated. The result is the "response" value provided by the client.
                                ResponseHash = Cryptography.MD5.GetHash(encoding.GetBytes(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}:{1}:{2}:{3}:{4}:{5}", BitConverter.ToString(HA1).Replace("-", string.Empty).ToLowerInvariant(), nOncePart, BitConverter.ToString(HA2).Replace("-", string.Empty).ToLowerInvariant(), ncPart, cnOncePart, qopPart)));
                                result = string.Format(System.Globalization.CultureInfo.InvariantCulture, "Digest username=\"{0}\", realm=\"{1}\", nonce=\"{2}\", uri=\"{3}\", qop=\"{4}\" nc=\"{5} cnonce=\"{6}\"", usernamePart, realmPart, nOncePart, uriPart, qopPart, ncPart, cnOncePart);
                                if (string.IsNullOrWhiteSpace(opaquePart).Equals(false)) result += "opaque=\"" + opaquePart + '"';
                            }
                            else
                            {
                                throw new NotImplementedException("Quality Of Protection:" + qopPart);
                            }
                        }
                        else // No Quality of Protection
                        {
                            HA2 = Cryptography.MD5.GetHash(encoding.GetBytes(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}:{1}", methodString, uriPart)));
                            ResponseHash = Cryptography.MD5.GetHash(encoding.GetBytes(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}:{1}:{2}", BitConverter.ToString(HA1).Replace("-", string.Empty).ToLowerInvariant(), nOncePart, BitConverter.ToString(HA2).Replace("-", string.Empty).ToLowerInvariant())));
                            result = string.Format(System.Globalization.CultureInfo.InvariantCulture, "Digest username=\"{0}\", realm=\"{1}\", nonce=\"{2}\", uri=\"{3}\", response=\"{4}\"", usernamePart, realmPart, nOncePart, uriPart, BitConverter.ToString(ResponseHash).Replace("-", string.Empty).ToLowerInvariant());
                        }

                        break;
                    }
                default: throw new ArgumentException("Must be either None, Basic or Digest", "scheme");
            }

            return result;
        }

        //TryParseAuthorizationHeader

        //internal static string BuildHeader(string sep, params string[] values)
        //{
        //    return string.Join(sep, values, 0, values.Length - 1) + values.Last();
        //}


        //GetHeaderValues

        //Todo, Most parse header functions could be changed to use a ParseHeader function which gives an array of values which can then be parsed further.
        //Create header would do the reverse.

        //internal static string CreateHeader(string key, string value) => key + ':' + CreateHeaderValue(";", value);

        //internal static string CreateHeaderValue(string sep, params string[] values)
        //{
        //    return string.Join(sep, values);
        //}

        //Something like this for the reverse
        //string[] ParseHeaderValues(string delemit, string source) => source.Split(delemit);

        //string[] ParseHeaderValues(int count) => source.Split(delemit, count);

        //bound length = -1 default...

        public static string[] SplitSpace(string input) { return input.Split((char)Common.ASCII.Space); }

        public static string[] SplitComma(string input) { return input.Split(SemiColon); }

        public static string[] SplitEqual(string input) { return input.Split((char)Common.ASCII.EqualsSign); }

        //Media.Http.HttpHeaders.ParseHeader(@"Digest realm=""testrealm@host.com"",\r\n  \t qop=""auth,auth-int"",   nonce=""dcd98b7102dd2f0e8b11d0f600bfb0c093"", opaque=""5ccc069c403ebaf9f0171e9517f40e41""").Keys

        public static Common.Collections.Generic.ConcurrentThesaurus<string, string> ParseHeader(string input)
        {

            //Todo, instead of split use substring...

            Common.Collections.Generic.ConcurrentThesaurus<string, string> result = new Common.Collections.Generic.ConcurrentThesaurus<string, string>();

            //Digest realm="testrealm@host.com",\r\n  \t qop="auth,auth-int",   nonce="dcd98b7102dd2f0e8b11d0f600bfb0c093", opaque="5ccc069c403ebaf9f0171e9517f40e41"
            string[] majors = SplitSpace(input);

            foreach (string part in majors)
            {
                if (string.IsNullOrWhiteSpace(part)) continue;

                string[] parts = SplitEqual(part);

                if (parts.Length > 1)
                    result.Add(parts[0], parts[1]);
                else
                    result.Add(parts[0], string.Empty);
            }

            return result;
        }
    }
}
