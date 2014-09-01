using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Common
{
    public interface ISocketOwner
    {
        IEnumerable<System.Net.Sockets.Socket> OwnedSockets { get; }
    }

    public static class ISocketOwnerExtensions
    {
        public static void SetReceiveBufferSize(this ISocketOwner owner, int size)
        {
            foreach (System.Net.Sockets.Socket s in owner.OwnedSockets)
            {
                s.ReceiveBufferSize = size;
            }
        }

        public static void SetSendBufferSize(this ISocketOwner owner, int size)
        {
            foreach (System.Net.Sockets.Socket s in owner.OwnedSockets)
            {
                s.SendBufferSize = size;
            }
        }


        public static void SetReceiveTimeout(this ISocketOwner owner, int timeoutMsec)
        {
            foreach (System.Net.Sockets.Socket s in owner.OwnedSockets)
            {
                s.ReceiveTimeout = timeoutMsec;
            }
        }

        public static void SetSendTimeout(this ISocketOwner owner, int timeoutMsec)
        {
            foreach (System.Net.Sockets.Socket s in owner.OwnedSockets)
            {
                s.SendTimeout = timeoutMsec;
            }
        }

        //

    }

}
