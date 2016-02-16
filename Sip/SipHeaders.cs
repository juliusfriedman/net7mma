
namespace Media.Sip
{
    public sealed class SipHeaders
    {
        //Don't duplicate HttpHeaders?
        //public const string ContentLength = "Content-Length";
        //public const string ContentType = "Content-Type";
        public const string CSeq = "CSeq";
        public const string Contact = "Contact";
        public const string Via = "Via";
        public const string To = "To";
        public const string From = "From";
        public const string UserAgent = "User-Agent";
        public const string Expires = "Expires";
        public const string MaxForwards = "Max-Forwards";
     
        public const string CallID = "Call-ID";
    }
}
