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
        IEnumerable<System.Net.Sockets.Socket> GetReferencedSockets();
    }

    /// <summary>
    /// Provides functions which help with configuration of <see cref="System.Net.Sockets"/> of a <see cref="ISocketReference"/>
    /// </summary>
    public static class ISocketReferenceExtensions
    {
        public static void SetReceiveBufferSize(this ISocketReference reference, int size)
        {
            foreach (System.Net.Sockets.Socket s in reference.GetReferencedSockets())
            {
                s.ReceiveBufferSize = Utility.Clamp(size, 0, int.MaxValue);
            }
        }

        public static void SetSendBufferSize(this ISocketReference reference, int size)
        {
            foreach (System.Net.Sockets.Socket s in reference.GetReferencedSockets())
            {
                s.SendBufferSize = Utility.Clamp(size, 0, int.MaxValue);
            }
        }


        public static void SetReceiveTimeout(this ISocketReference reference, int timeoutMsec)
        {
            foreach (System.Net.Sockets.Socket s in reference.GetReferencedSockets())
            {
                s.ReceiveTimeout = timeoutMsec;
            }
        }

        public static void SetSendTimeout(this ISocketReference reference, int timeoutMsec)
        {
            foreach (System.Net.Sockets.Socket s in reference.GetReferencedSockets())
            {
                s.SendTimeout = timeoutMsec;
            }
        }

        //

    }

}
