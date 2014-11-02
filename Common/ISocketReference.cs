using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Common
{
    /// <summary>
    /// Provides an interface which defines a way to obtain the <see cref="System.Net.Socket"/> instances used by a class.
    /// </summary>
    public interface ISocketReference
    {
        /// <summary>
        /// The <see cref="System.Net.Sockets"/> which belong to this instance.
        /// </summary>
        IEnumerable<System.Net.Sockets.Socket> ReferencedSockets { get; }
    }

    /// <summary>
    /// Provides functions which help with configuration of <see cref="System.Net.Sockets"/> of a <see cref="ISocketReference"/>
    /// </summary>
    public static class ISocketOwnerExtensions
    {
        public static void SetReceiveBufferSize(this ISocketReference owner, int size)
        {
            foreach (System.Net.Sockets.Socket s in owner.ReferencedSockets)
            {
                s.ReceiveBufferSize = size;
            }
        }

        public static void SetSendBufferSize(this ISocketReference owner, int size)
        {
            foreach (System.Net.Sockets.Socket s in owner.ReferencedSockets)
            {
                s.SendBufferSize = size;
            }
        }


        public static void SetReceiveTimeout(this ISocketReference owner, int timeoutMsec)
        {
            foreach (System.Net.Sockets.Socket s in owner.ReferencedSockets)
            {
                s.ReceiveTimeout = timeoutMsec;
            }
        }

        public static void SetSendTimeout(this ISocketReference owner, int timeoutMsec)
        {
            foreach (System.Net.Sockets.Socket s in owner.ReferencedSockets)
            {
                s.SendTimeout = timeoutMsec;
            }
        }

        //

    }

}
