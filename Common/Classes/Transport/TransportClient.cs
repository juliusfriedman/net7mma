
namespace Media.Common
{
    //Will provide the base classes for any type of client

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

    public class TransportClient : Common.BaseDisposable, Common.ISocketReference, Common.IThreadReference
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

        #endregion

        #region Constructor / Destructor

        ~TransportClient() { Dispose(); }

        #endregion
    }
}
