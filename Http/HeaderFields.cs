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

namespace Media.Http
{

    //A Grammar should atleast contain the definitions of what is considered a token and then further what can modify the tokens
    //public class Grammar
    //{
    //    //System.Collections.Generic.List<string> Tokens;

    //    //System.Collections.Generic.List<string> Modifiers;
    //}

    /// <summary>
    /// Commonly used values within <see cref="HttpHeaders"/>
    /// </summary>
    public class HeaderFields
    {

        #region General

        internal const string type = "type";

        internal const string identifier = "identifier";

        internal const string thread = "thread";

        internal const string count = "count";

        #endregion

        internal protected HeaderFields() { }

        #region Connection

        public sealed class Connection
        {
            internal const string close = "close";

            //its possible to use the unicode character modifiers to make the variable name (e.g when generating or otherwise) pseudo hypenated but typing them would be a pain...
            internal const string keepΞalive = "keep-alive";

            public const string Close = close;

            //public versions would be a way to work around that.
            public const string KeepAlive = keepΞalive;

        }

        #endregion

        #region Authorization

        //WWW-Authenticate

        public sealed class Authorization
        {
            internal const string basic = "basic";

            internal const string digest = "digest";

            internal const string realm = "realm";

            public const string Realm = realm;

            public const string Basic = basic;

            public const string Digest = digest;

            //DigestGrammar
            public sealed class Attributes
            {
                internal const string nonce = "nonce";

                internal const string cnonce = "cnonce";

                internal const string nc = "nc";

                internal const string realm = "realm";

                internal const string opaque = "opaque";

                internal const string uri = "uri";

                internal const string response = "response";

                internal const string qop = "qop";

                internal const string stale = "stale";

                public const string Nonce = nonce;

                public const string Cnonce = cnonce;

                public const string Nc = nc;

                public const string Realm = realm;

                public const string Opaque = opaque;

                public const string Uri = uri;

                public const string Response = response;

                public const string QualityOfProtection = qop;

                public const string Stale = stale;

            }

        }

        #endregion

        #region Session

        public sealed class SessionId
        {
            public const string SID = "SID";

            public const string ANON = "ANON";
        }

        #endregion

    }
}
