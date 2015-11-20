#region Copyright
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
#endregion

namespace Media.Common.Extensions.Stream
{
    public static class StreamExtensions
    {
        #region BeginCopyTo / BeginCopyTransaction

        public static ITransactionResult /*System.IAsyncResult*/ BeginCopyTo(this System.IO.Stream source, System.IO.Stream dest, int bufferSize, System.Action<long, byte[], int, int> writeFunction)
        {
            //Todo allow a way to call SetLength on dest 
            return new StreamCopyTransaction(source, dest, bufferSize, writeFunction);
        }

        #endregion

        //Will eventually use HttpClient...

        #region DownloadAdapter / DownloadStream

        /// <summary>
        /// Provies a way to download a remote resource and optionally cancel the download.
        /// 
        /// <see cref="ITransactionResult"/> events are not implemented by default however the download itself is usually backed by a <see cref="StreamCopyTransaction"/> when utilized.
        /// This is easy enough to change but also could be exposed via the TransactionBase implementation, since this class is internal it doesn't matter.
        /// </summary>
        internal sealed class DownloadAdapter : LifetimeDisposable, ITransactionResult
        {
            public System.Uri Location { get; internal set; } 

            public System.IDisposable Request { get; internal set; } 
            
            public System.IDisposable Response { get; internal set; }

            public System.IO.Stream ResponseOutputStream { get; internal set; }

            internal long StartPosition = 0, MaximumLength; //Make public accessor

            //readonly DisposeSink ds = new DisposeSink();

            internal protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing || ResponseOutputStream == null || false == ResponseOutputStream.CanRead);

                if (false == ShouldDispose) return;

                //HandleDisposing(this, ds.Dispose);

                if (Location != null) Location = null;

                if (Response != null) { Response.Dispose(); Response = null; }

                if (Request != null) { Request.Dispose(); Request = null; }

                if (ResponseOutputStream != null)
                {
                    ResponseOutputStream.Dispose(); 
                    
                    ResponseOutputStream = null;
                }

                SemaphoreSlim.Dispose();
            }

            internal DownloadAdapter(System.Uri location)
                : base(false)
            {
                if ((Location = location) == null) throw new System.ArgumentNullException("location");

                //var request = (System.Net.HttpWebRequest)System.Net.HttpWebRequest.Create(Location);

                //Request = (System.IDisposable)request;

                //var response = request.GetResponse();

                //Response = response;

                //Result = response.GetResponseStream();
                
                //ds.Disposing += HandleDisposing;

                SemaphoreSlim.Wait();
            }

            public void Cancel()
            {
                Expire();  

                if (ResponseOutputStream != null && ResponseOutputStream.CanRead)
                {
                    Dispose();
                }
            }

            //void HandleDisposing(object sender, Action t)
            //{
            //    if (false == (sender == this)) return;

            //    ds.Dispose();

            //    ds.Disposing -= HandleDisposing;

            //    Expire();

            //    Dispose(true);
            //}

            //Calls the finalizer

            //~DownloadReference() { Dispose(); }

            //Allows a call to be made from a finalizer, creates an intentional memory leak...
            //internal class DisposeSink : BaseDisposable
            //{
            //    public event EventHandlerEx<Action> Disposing;

            //    //public DisposeSink() : base(false) { }

            //    protected override void Dispose(bool disposing)
            //    {
            //        if (disposing && Disposing != null) Disposing(this, Dispose);

            //        base.Dispose(disposing);
            //    }

            //    void HandleDisposing(object sender, Action t)
            //    {
            //        if (t != null) t();

            //        if (this == sender) Disposing -= HandleDisposing;
            //    }

            //    public override void Dispose()
            //    {
            //        HandleDisposing(this, base.Dispose);
            //    }
            //}

            #region Nested Types

            internal class DisposableHttpWebRequest : LifetimeDisposable
            {
                //internal readonly DisposeSink ds = new DisposeSink();

                //static DisposableHttpWebRequest Null

                internal System.Net.HttpWebRequest Request;

                internal System.Net.WebResponse Response;

                internal System.IO.Stream ResponseStream = System.IO.Stream.Null;

                public DisposableHttpWebRequest(System.Net.HttpWebRequest request)
                    : base(false)
                {
                    if (request == null) throw new System.ArgumentNullException("request");

                    try
                    {
                        Request = request;

                        Response = Request.GetResponse();

                        ResponseStream = Response.GetResponseStream();
                    }
                    catch
                    {
                        //Dispose();
                    }
                    //ds.Disposing += HandleDisposing;
                }

                //void HandleDisposing(object sender, Action t)
                //{
                //    if (t != null) t();

                //    if (false == (sender == this)) return;

                //    ds.Disposing -= HandleDisposing;

                //    Expire();

                //    Dispose(true);
                //}

                public DisposableHttpWebRequest(System.Net.HttpWebRequest request, System.Net.WebProxy proxy, System.Net.NetworkCredential credentials, System.Collections.Specialized.NameValueCollection headers, System.Net.CookieCollection cookies)
                    : this(request)
                {
                    if(proxy != null) Request.Proxy = proxy;

                    if (headers != null) Request.Headers.Add(headers);

                    if (cookies != null)Request.CookieContainer.Add(cookies);

                    if (credentials != null) Request.Credentials = credentials;
                }

                public DisposableHttpWebRequest(System.Uri location, System.Net.WebProxy proxy, System.Net.NetworkCredential credentials, System.Collections.Specialized.NameValueCollection headers, System.Net.CookieCollection cookies) : 
                    this((System.Net.HttpWebRequest)System.Net.HttpWebRequest.Create(location), proxy, credentials, headers, cookies) { }

                internal protected override void Dispose(bool disposing)
                {
                    base.Dispose(disposing || false == (ResponseStream != null && ResponseStream.CanRead));

                    if (false == ShouldDispose) return;

                    //HandleDisposing(this, ds.Dispose);

                    if (Request != null)
                    {
                        Request.Abort();

                        Request = null;
                    }

                    if (ResponseStream != null)
                    {
                        ResponseStream.Dispose();

                        ResponseStream = null;
                    }

                    if (Response != null)
                    {
                        Response.Dispose();

                        Response = null;
                    }
                }
            }

            internal class WebClientEx : System.Net.WebClient, IDisposed
            {
                //internal readonly DisposeSink ds = new DisposeSink();

                readonly LifetimeDisposable Composed = new LifetimeDisposable(false);

                internal System.IO.Stream ResponseStream;

                public WebClientEx()
                    : base()
                {
                    //ds.Disposing += HandleDisposing;                
                }

                public WebClientEx(System.Uri location) 
                    : this()
                {
                    try
                    {
                        ResponseStream = OpenRead(location);
                    }
                    catch
                    {
                        ResponseStream = System.IO.Stream.Null;
                    }
                }

                //void HandleDisposing(object sender, Action t)
                //{
                //    if(t != null) t();

                //    ds.Disposing -= HandleDisposing;

                //    Dispose(true);
                //}

                protected override void Dispose(bool disposing)
                {
                    Composed.Dispose(disposing || false == (ResponseStream != null && ResponseStream.CanRead));

                    if (false == Composed.ShouldDispose) return;

                    //HandleDisposing(this, ds.Dispose);

                    base.Dispose(disposing);

                    if (ResponseStream != null)
                    {
                        ResponseStream.Dispose();

                        ResponseStream = null;
                    }
                }

                bool IDisposed.IsDisposed
                {
                    get { return Composed.IsDisposed; }
                }

                bool IDisposed.ShouldDispose
                {
                    get { return Composed.ShouldDispose; }
                }

                void System.IDisposable.Dispose()
                {
                    Composed.Dispose();
                }
            }

            #endregion
            //Credential
            public static DownloadAdapter HttpWebRequestDownload(System.Uri location, System.Net.WebProxy proxy = null, System.Net.NetworkCredential credentials = null, System.Collections.Specialized.NameValueCollection headers = null, System.Net.CookieCollection cookies = null)
            {
                if (location == null) throw new System.ArgumentNullException("location");
                else if (false == location.OriginalString.StartsWith(System.Uri.UriSchemeHttp)) throw new System.InvalidOperationException("location must start with System.Uri.UriSchemeHttp");

                using (var request = new DisposableHttpWebRequest(location, proxy, credentials, headers, cookies))
                {
                    return new DownloadAdapter(location)
                    {
                        Request = request,
                        Response = request.Response,
                        MaximumLength = request.Response == null ? 0 : request.Response.ContentLength,
                        ResponseOutputStream = request.ResponseStream
                    };
                }
            }

            public static DownloadAdapter WebClientDownload(System.Uri location, System.Net.NetworkCredential credential = null)
            {
                if (location == null) throw new System.ArgumentNullException("location");

                using (WebClientEx webClient = new WebClientEx(location))
                {
                    if (credential != null) webClient.Credentials = credential;

                    using (var d = new DownloadAdapter(location)
                    {
                        Request = webClient,
                        ResponseOutputStream = webClient.ResponseStream
                    })
                    {
                        if (webClient.ResponseHeaders != null)
                        {
                            string contentLength = webClient.ResponseHeaders["Content-Length"];

                            if (false == string.IsNullOrWhiteSpace(contentLength)) long.TryParse(contentLength, out d.MaximumLength);

                            contentLength = null;
                        }

                        return d;
                    }
                }
            }

            bool ITransactionResult.SupportsCancellation
            {
                get { return true; }
            }

            void ITransactionResult.CancelTransaction()
            {                
                Cancel();
            }

            bool ITransactionResult.IsTransactionCanceled
            {
                get { return LifetimeElapsed; }
            }

            bool ITransactionResult.IsTransactionDone
            {
                get { return IsDisposed || LifetimeElapsed || ResponseOutputStream != null && false == ResponseOutputStream.CanRead; }
            }

            //Since there are no events it might make sense to have a percentage property and also a remaining property

            event EventHandlerEx<ITransactionResult> ITransactionResult.TransactionRead
            {
                add { throw new System.NotImplementedException(); }
                remove { throw new System.NotImplementedException(); }
            }

            event EventHandlerEx<ITransactionResult> ITransactionResult.TransactionWrite
            {
                add { throw new System.NotImplementedException(); }
                remove { throw new System.NotImplementedException(); }
            }

            event EventHandlerEx<ITransactionResult> ITransactionResult.TransactionCompleted
            {
                add { throw new System.NotImplementedException(); }
                remove { throw new System.NotImplementedException(); }
            }

            event EventHandlerEx<ITransactionResult> ITransactionResult.TransactionCancelled
            {
                add { throw new System.NotImplementedException(); }
                remove { throw new System.NotImplementedException(); }
            }

            object System.IAsyncResult.AsyncState
            {
                get { return this; }
            }

            public readonly System.Threading.SemaphoreSlim SemaphoreSlim = new System.Threading.SemaphoreSlim(1);

            System.Threading.WaitHandle System.IAsyncResult.AsyncWaitHandle
            {
                get { return SemaphoreSlim.AvailableWaitHandle; }
            }

            bool System.IAsyncResult.CompletedSynchronously
            {
                get { return IsDisposed; }
            }

            bool System.IAsyncResult.IsCompleted
            {
                get { return IsDisposed; }
            }

        }

        #endregion

        #region Download / TryDownload

        //Logic really doesn't require seperate methods unless they verify the protocol...

        public static System.IO.Stream HttpWebRequestDownload(System.Uri location, System.Net.WebProxy proxy = null, System.Net.NetworkCredential credential = null)
        {
            if (false == location.Scheme.StartsWith(System.Uri.UriSchemeHttp, System.StringComparison.InvariantCultureIgnoreCase)) throw new System.ArgumentException("Must start with System.Uri.UriSchemeHttp", "location.Scheme");

            using (DownloadAdapter d = DownloadAdapter.HttpWebRequestDownload(location, proxy, credential))
                return d.ResponseOutputStream;
        }

        public static bool TryHttpWebRequestDownload(System.Uri location, out System.IO.Stream result, System.Net.WebProxy proxy = null, System.Net.NetworkCredential credential = null)
        {
            result = null;

            try
            {
                result = HttpWebRequestDownload(location, proxy, credential);

                return result != null;
            }
            catch (System.Exception ex)
            {
                Common.Extensions.Exception.ExceptionExtensions.TryRaiseTaggedException(result, ex.Message, ex); 
                
                return false;
            }
        }

        public static System.IO.Stream FileDownload(System.Uri location, System.Net.NetworkCredential credential = null)
        {
            if (false == location.Scheme.StartsWith(System.Uri.UriSchemeFile, System.StringComparison.InvariantCultureIgnoreCase)) throw new System.ArgumentException("Must start with System.Uri.UriSchemeFile", "location.Scheme");

            using (DownloadAdapter d = DownloadAdapter.WebClientDownload(location, credential))
                return d.ResponseOutputStream;
        }

        public static bool TryFileDownload(System.Uri location, out System.IO.Stream result, System.Net.NetworkCredential credential = null)
        {
            result = null;

            try
            {
                result = FileDownload(location, credential);

                return result != null;
            }
            catch (System.Exception ex)
            {
                Common.Extensions.Exception.ExceptionExtensions.TryRaiseTaggedException(result, ex.Message, ex);

                return false;
            }
        }

        public static System.IO.Stream WebClientDownload(System.Uri location, System.Net.NetworkCredential credential = null)
        {
            using (DownloadAdapter d = DownloadAdapter.WebClientDownload(location, credential))
                return d.ResponseOutputStream;
        }

        public static bool TryWebClientDownload(System.Uri location, out System.IO.Stream result, System.Net.NetworkCredential credential = null)
        {
            result = null;

            try
            {
                result = WebClientDownload(location, credential);

                return result != null;
            }
            catch (System.Exception ex)
            {
                Common.Extensions.Exception.ExceptionExtensions.TryRaiseTaggedException(result, ex.Message, ex);

                return false;
            }
        }

        #endregion

        #region ITransactionResult

        public interface ITransactionResult : System.IAsyncResult, IDisposed
        {
            bool SupportsCancellation { get; }

            void CancelTransaction();

            bool IsTransactionCanceled { get; }

            bool IsTransactionDone { get; }

            //long ExpectedLength { get; }

            //int LastTransactionAmount { get; }

            event EventHandlerEx<ITransactionResult> TransactionRead;

            event EventHandlerEx<ITransactionResult> TransactionWrite;

            event EventHandlerEx<ITransactionResult> TransactionCompleted;

            event EventHandlerEx<ITransactionResult> TransactionCancelled;
        }

        #endregion

        #region TransactionBase

        public abstract class TransactionBase : LifetimeDisposable, ITransactionResult
        {
            #region Statics

            static void HandleCompleted(object sender, ITransactionResult e)
            {
                if (e != null && e is TransactionBase)
                {
                    TransactionBase tb = e as TransactionBase;

                    tb.IsTransactionDone = tb.ShouldDispose = true;

                    tb.Dispose();

                    tb = null;
                }
            }

            static void HandleRead(object sender, ITransactionResult e)
            {
                //if (e == null && false == sct is TransactionBase) return;
            }

            static void HandleWrite(object sender, ITransactionResult e)
            {
                if (e != null && e is TransactionBase)
                {
                    TransactionBase tb = ((TransactionBase)e);

                    tb.TransactionOffset += tb.LastTransactionAmount;

                    tb = null;
                }
            }

            static void HandleCancelled(object sender, ITransactionResult e)
            {
                if (e != null && e is TransactionBase)
                {
                    TransactionBase tb = ((TransactionBase)e);//e as TransactionBase;

                    tb.TransactionCompleted(sender, e);

                    tb = null;
                }
            }

            internal protected static void RaiseTransactionCompleted(TransactionBase tb, object sender = null)
            {
                var evt = tb != null ? tb.TransactionCompleted : null;

                if (evt == null) return;
                
                evt(sender, tb);

                evt = null;

                tb = null;
            }

            internal protected static void RaiseTransactionWrite(TransactionBase tb, object sender = null)
            {
                var evt = tb != null ? tb.TransactionWrite : null;

                if (evt == null) return;

                evt(sender, tb);

                evt = null;

                tb = null;
            }

            internal protected static void RaiseTransactionRead(TransactionBase tb, object sender = null)
            {
                var evt = tb != null ? tb.TransactionRead : null;

                if (evt == null) return;

                evt(sender, tb);

                evt = null;

                tb = null;
            }

            internal protected static void RaiseTransactionCancelled(TransactionBase tb, object sender = null)
            {
                var evt = tb != null ? tb.TransactionCancelled : null;

                if (evt == null) return;

                evt(sender, tb);

                evt = null;

                tb = null;
            }

            #endregion

            #region Fields

            public readonly System.DateTimeOffset Created = System.DateTimeOffset.UtcNow;

            protected long TransactionOffset;

            protected readonly System.Threading.CancellationTokenSource CancelTokenSource;

            protected readonly System.Threading.CancellationToken CancelToken;

            #endregion

            #region Properties

            #endregion

            #region Virtual Properties

            public virtual bool IsTransactionDone { get; protected set; }

            public virtual int LastTransactionAmount { get; protected set; }

            public virtual long TotalTransactionBytesWritten { get { return TransactionOffset; } }

            public virtual System.IAsyncResult AsyncTransactionResult { get; internal set; }

            public System.IAsyncResult AsyncResult { get; internal set; }

            public virtual object AsyncState { get { return AsyncResult.AsyncState; } }

            public virtual System.Threading.WaitHandle AsyncWaitHandle { get { return AsyncResult.AsyncWaitHandle; } }

            public virtual bool CompletedSynchronously { get { return AsyncResult.CompletedSynchronously; } }

            public virtual bool SupportsCancellation { get { return CancelToken.CanBeCanceled; } }

            public virtual bool IsTransactionCanceled
            {
                get { return CancelTokenSource.IsCancellationRequested || CancelToken.IsCancellationRequested; }
            }
          
            public virtual bool IsCompleted
            {
                get { return IsTransactionCanceled || IsTransactionDone || AsyncTransactionResult != null && AsyncTransactionResult.IsCompleted; }
            }

            #endregion

            #region Constructor / Destructor

            public TransactionBase(bool shouldDispose = false)
                : base(shouldDispose)
            {
                CancelTokenSource = new System.Threading.CancellationTokenSource();

                CancelToken = CancelTokenSource.Token;

                TransactionCompleted += TransactionBase.HandleCompleted;

                TransactionCancelled += TransactionBase.HandleCancelled;

                TransactionRead += TransactionBase.HandleRead;

                TransactionWrite += TransactionBase.HandleWrite;
            }

            public TransactionBase(bool shouldDispose, System.AsyncCallback start, System.IAsyncResult result, object state)
                : this(shouldDispose)
            {
                AsyncResult = start.BeginInvoke(result, start, state ?? this);
            }

            //~TransactionBase() { Dispose(); }

            #endregion

            #region Overrides

            internal protected override void Dispose(bool disposing)
            {
                if (IsDisposed) return;

                base.Dispose(disposing);

                if (false == ShouldDispose) return;

                Cancel();

                if (AsyncResult != null) AsyncResult = null;

                if (TransactionRead != null)
                {
                    TransactionRead -= TransactionBase.HandleRead;

                    TransactionRead = null;
                }

                if (TransactionWrite != null)
                {
                    TransactionWrite -= TransactionBase.HandleWrite;

                    TransactionWrite = null;
                }

                //Could happen in handler
                if (TransactionCompleted != null)
                {
                    TransactionCompleted -= TransactionBase.HandleCompleted;

                    TransactionCompleted = null;
                }

                if (TransactionCancelled != null)
                {
                    TransactionCancelled -= TransactionBase.HandleCancelled;

                    TransactionCancelled = null;
                }
            }

            #endregion

            #region Virtual Methods

            protected virtual void Cancel()
            {
                if (false == IsCompleted && CancelTokenSource != null)
                {
                    CancelTokenSource.Cancel();

                    if (TransactionCancelled != null) TransactionCancelled(null, this);
                }
            }

            public virtual void CancelTransaction()
            {
                Cancel();
            }           

            #endregion

            #region Events

            public event EventHandlerEx<ITransactionResult> TransactionRead;

            public event EventHandlerEx<ITransactionResult> TransactionWrite;

            public event EventHandlerEx<ITransactionResult> TransactionCompleted;

            public event EventHandlerEx<ITransactionResult> TransactionCancelled;
            
            #endregion
        }

        #endregion

        #region ReadTransaction

        public interface IReadTransaction<T> : ITransactionResult
        {
            T Source { get; }
        }

        public abstract class ReadTransaction : TransactionBase
        {
            public ReadTransaction(System.IO.Stream s, byte[] dest, int off, int len)
                : base(true, new System.AsyncCallback((iar) => s.BeginRead(dest, off, len, new System.AsyncCallback((ia) => s.EndRead(ia)), null)), null, null)
            {

            }
        }

        #endregion

        #region WriteTransaction

        public interface IWriteTransaction<T> : ITransactionResult
        {
            T Destination { get; }
        }

        public abstract class WriteTransaction : TransactionBase
        {
            public WriteTransaction(System.IO.Stream s, byte[] dest, int off, int len)
                : base(true, new System.AsyncCallback((iar) => s.BeginWrite(dest, off, len, new System.AsyncCallback((ia) => s.EndWrite(ia)), null)), null, null)
            {

            }
        }

        #endregion

        #region StreamCopyTransaction

        public sealed class StreamCopyTransaction : TransactionBase, IReadTransaction<System.IO.Stream>, IWriteTransaction<System.IO.Stream>
        {
            #region Properties and Fields

            public System.IO.Stream Source { get; internal set; }

            public readonly bool DisposeSource;

            public System.IO.Stream Destination { get; internal set; }

            public readonly bool DisposeDestination;

            public Common.MemorySegment Memory { get; internal set; }

            public System.AsyncCallback ReadFunction { get; internal set; }

            public System.Action<long, byte[], int, int> WriteFunction { get; set; }

            #endregion

            public override bool IsCompleted
            {
                get
                {
                    return false == Source.CanRead || false == Destination.CanWrite || base.IsCompleted;
                }
            }

            #region Constructor / Destructor

            public StreamCopyTransaction(System.IO.Stream source, System.IO.Stream dest, int bufferSize, System.Action<long, byte[], int, int> writeFunction, int offset = 0)
            {
                Source = source;

                Destination = dest;

                Memory = new MemorySegment(bufferSize);

                ReadFunction = CopyLogic;

                WriteFunction = writeFunction;

                AsyncResult = source.BeginRead(Memory.Array, offset /*0 = Memory.Offset*/, bufferSize, ReadFunction, this);
            }

            public StreamCopyTransaction(System.IO.Stream source, bool disposeSource, System.IO.Stream dest, bool disposeDest, int bufferSize, System.Action<long, byte[], int, int> writeFunction)
                :this(source, dest, bufferSize, writeFunction)
            {
                DisposeSource = disposeSource;

                DisposeDestination = disposeDest;
            }

            //~StreamCopyTransaction() { Dispose(IsCompleted || LifetimeElapsed); }

            #endregion

            #region Methods

            void CopyLogic(System.IAsyncResult iar)
            {

                if(IsCompleted) return;

                try
                {
                    LastTransactionAmount = Source.EndRead(iar);

                    switch (LastTransactionAmount)
                    {
                        case 0:
                            {
                                int check = 0;

                                if (false == Source.CanRead || -1 == (check = Source.ReadByte()))
                                {
                                    TransactionBase.RaiseTransactionCompleted(this);

                                    goto Dispose;
                                }

                                Memory[Memory.Offset + LastTransactionAmount++] = (byte)check;

                                goto default;
                            }
                        default:
                            {
                                TransactionBase.RaiseTransactionRead(this);

                                WriteFunction(TransactionOffset, Memory.Array, Memory.Offset, LastTransactionAmount);

                                TransactionBase.RaiseTransactionWrite(this);

                                if (false == IsTransactionCanceled && false == IsDisposed) AsyncResult = Source.BeginRead(Memory.Array, Memory.Offset, Memory.Count, ReadFunction, this);

                                return;
                            }
                    }
                }
                catch
                {
                    LastTransactionAmount = -1;

                    Expire();
                }

            Dispose:
                Dispose(ShouldDispose = true);
            }

            #endregion
        }

        #endregion
    }
}
