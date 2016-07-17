
namespace Media.Common
{
    //TransportSession? => Id 

    //TransportContext => Thin wrapper. (GetChannelBinding, GetTlsTokenBindings)

    // Holds a cached Endpoint binding to be reused
    //internal class CachedTransportContext : System.Net.TransportContext
    //{
    //    internal CachedTransportContext(System.Security.Authentication.ExtendedProtection.ChannelBinding binding)
    //    {
    //        this.binding = binding;
    //    }

    //    public override System.Security.Authentication.ExtendedProtection.ChannelBinding GetChannelBinding(System.Security.Authentication.ExtendedProtection.ChannelBindingKind kind)
    //    {
    //        if (kind != System.Security.Authentication.ExtendedProtection.ChannelBindingKind.Endpoint)
    //            return null;

    //        return binding;
    //    }

    //    private System.Security.Authentication.ExtendedProtection.ChannelBinding binding;
    //}

    /// Will eventually provide the base classes for any type of client
    public class TransportClient : Common.SuppressedFinalizerDisposable, Common.ISocketReference, Common.IThreadReference
    {
        #region ISocketReference

        System.Collections.Generic.IEnumerable<System.Net.Sockets.Socket> ISocketReference.GetReferencedSockets()
        {
            throw new System.NotImplementedException();
        }

        System.Action<System.Net.Sockets.Socket> ISocketReference.ConfigureSocket
        {
            get
            {
                throw new System.NotImplementedException();
            }
            set
            {
                throw new System.NotImplementedException();
            }
        }

        #endregion

        #region IThreadReference

        System.Collections.Generic.IEnumerable<System.Threading.Thread> IThreadReference.GetReferencedThreads()
        {
            throw new System.NotImplementedException();
        }

        System.Action<System.Threading.Thread> IThreadReference.ConfigureThread
        {
            get
            {
                throw new System.NotImplementedException();
            }
            set
            {
                throw new System.NotImplementedException();
            }
        }

        #endregion

        #region Fields

        //readonly ValueType...
        public readonly System.DateTime Created = System.DateTime.UtcNow;

        //ConnectionTime

        /// <summary>
        /// Gets the <see cref="System.Net.NetworkInformation.NetworkInterface"/> which was used to create the TransportClient.
        /// </summary>
        public readonly System.Net.NetworkInformation.NetworkInterface NetworkInterface;

        //Media.Sockets.Connection

        //public readonly System.Collections.Generic.List<Common.IPacket> Outbound;

        //SendThreads, ReceiveThreads

        #endregion

        #region Properties

        //TransportSessions

        //ConnectionTime

        //ConnectionSocket

        //Accept?

        //SendTimeout, RecieveTimeout

        #endregion

        #region Methods

        public virtual void Connect() { }

        public virtual void Disconnect() { }

        //EnqueMessage

        //SendMessge

        #endregion

        #region Events

        public delegate void TransportClientAction(TransportClient sender, object args);

        //Todo, ITransportMessage
        public delegate void RequestHandler(TransportClient sender, TransportMessageBase request);

        //Todo, ITransportMessage
        public delegate void ResponseHandler(TransportClient sender, TransportMessageBase request, TransportMessageBase response);

        //Todo, should be generic and allow variance for implementations which handle multiple types of protocols.
        public event TransportClientAction OnConnect;

        public event TransportClientAction OnDisconnect;

        #endregion

        #region Constructor / Destructor

        public TransportClient(System.Net.NetworkInformation.NetworkInterface networkInterface, bool shouldDispose = true)
            : base(shouldDispose)
        {
            if (object.ReferenceEquals(networkInterface, null)) throw new System.ArgumentNullException();

            NetworkInterface = networkInterface;
        }

        #endregion

        protected internal override void Dispose(bool disposing)
        {
            if (false.Equals(disposing) || false.Equals(ShouldDispose)) return;

            base.Dispose(ShouldDispose);

            if (false.Equals(IsDisposed)) return;
        }
    }
}
