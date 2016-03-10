using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Http
{

    //https://msdn.microsoft.com/en-us/library/system.net.http.httpcontent(v=vs.110).aspx

    //Should be added to HttpMessage ?
    public abstract class HttpContent : Common.BaseDisposable
    {
        public DateTime? Transferred;

        public readonly string ContentName;

        public readonly Dictionary<string, string> Headers = new Dictionary<string, string>();

        //public virtual long TryCalculateLength() { return 0; }

        public HttpContent() : this(string.Empty) { }

        public HttpContent(string name)
        {
            if (name == null) throw new ArgumentNullException("name");

            ContentName = name;
        }
    }

    public class BinaryContent : HttpContent
    {
        public readonly Common.MemorySegment Data;

        public BinaryContent(byte[] data, string name = null) : this(new Common.MemorySegment(data), name) { }
        
        public BinaryContent(Common.MemorySegment data, string name = null) : base(name)
        {
            if (data == null || data.Count == 0) throw new ArgumentException("data cannot be null or empty");

            Data = data;
        }
    }

    public class StringContent : BinaryContent
    {
        public StringContent(string content, System.Text.Encoding encoding, string mediaType)
            : base((encoding ?? HttpMessage.DefaultEncoding).GetBytes(content ?? string.Empty), mediaType)
        {

        }

        public StringContent(string content, System.Text.Encoding encoding = null) : this(content, encoding, null) { }
    }

    public class StreamContent : HttpContent
    {
        public readonly System.IO.Stream Stream;

        public bool LeaveOpen;

        public StreamContent(System.IO.Stream stream)
        {
            if (stream == null) throw new ArgumentNullException();

            Stream = stream;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (IsDisposed && LeaveOpen) return;

            Stream.Dispose();
        }
    }

    /// <summary>
    /// Represents Multipart Http Content.
    /// </summary>
    public class MultipartContent : HttpContent, IEnumerable<HttpContent>
    {

        //DirectoryInfo Storage


        //Should store in response ?
        static MultipartContent Read(HttpClient client, HttpMessage message)
        {
            string boundary = message.GetHeader(HttpHeaders.ContentType).Split(';').FirstOrDefault(s => s.StartsWith("boundary")).Substring(9);

            MultipartContent result = new MultipartContent(boundary, message.ContentEncoding);

        Receive:
            int received = client.HttpSocket.Receive(client.Buffer.Array);

            if (received > 0)
            {

                //search for boundary and recycle unused bytes

                //Empty line itself
                //if (received <= 2) return result;

                goto Receive;
            }


            return result;
        }

        static StreamContent Stream(HttpClient client, HttpMessage request, System.IO.Stream backing)
        {
            StreamContent result = new StreamContent(backing ?? new System.IO.MemoryStream(client.Buffer.Array));

            return result;
        }

        public MultipartContent(string boundary, System.Text.Encoding encoding = null) : this((encoding ?? HttpMessage.DefaultEncoding).GetBytes(boundary ?? string.Empty)) { }

        public MultipartContent(byte[] boundary) : this(new Common.MemorySegment(boundary)) { }

        public MultipartContent(Common.MemorySegment boundary)
        {
            if (boundary == null || boundary.Count == 0) throw new ArgumentException("boundary cannot be null or empty");

            Boundary = boundary;
        }

        public readonly Common.MemorySegment Boundary;

        public readonly List<HttpContent> Contents = new List<HttpContent>();

        public void Add(HttpContent content)
        {
            if (Common.IDisposedExtensions.IsNullOrDisposed(content)) throw new ArgumentException("content cannot be null or disposed");
            Contents.Add(content);
        }

        IEnumerator<HttpContent> IEnumerable<HttpContent>.GetEnumerator()
        {
            return Contents.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return Contents.GetEnumerator();
        }
    }

    //public class test
    //{
    //    void tests()
    //    {
    //        MultipartContent mc = new MultipartContent("test");

    //        mc.Contents.Add(new MultipartContent("sub") { { new MultipartContent("subsub") } });
    //    }
    //}
}
