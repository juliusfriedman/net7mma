
namespace Media.Common
{
    //Will provide the base classes for any type of client
    public class TransportClient : Common.ISocketReference, Common.IThreadReference
    {
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
    }
}
