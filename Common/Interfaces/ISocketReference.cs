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
        /// Should never contain a 'null' socket.
        /// </summary>
        IEnumerable<System.Net.Sockets.Socket> GetReferencedSockets();

        /// <summary>
        /// Gets the function which can set any required socket options on the given socket.
        /// </summary>
        /// <param name="socket">The socket to configure</param>
        Action<System.Net.Sockets.Socket/*, Common.ILogging*/> ConfigureSocket { get; set; } //ApplyConfiguration        
    }

    //delegate AggregateException ConfigureSocket(System.Net.Sockets.Socket socket);

    /// <summary>
    /// Provides functions which help with configuration of <see cref="System.Net.Sockets"/> of a <see cref="ISocketReference"/>
    /// </summary>
    public static class ISocketReferenceExtensions
    {
        public static void SetReceiveBufferSize(this ISocketReference reference, int size)
        {
            foreach (System.Net.Sockets.Socket s in reference.GetReferencedSockets())
            {
                s.ReceiveBufferSize = Media.Common.Binary.Clamp(size, 0, int.MaxValue);
            }
        }

        public static void SetSendBufferSize(this ISocketReference reference, int size)
        {
            foreach (System.Net.Sockets.Socket s in reference.GetReferencedSockets())
            {
                s.SendBufferSize = Media.Common.Binary.Clamp(size, 0, int.MaxValue);
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

        public static IEnumerable<System.Net.NetworkInformation.NetworkInterface> GetNetworkInterfaces(this ISocketReference reference)
        {
            foreach (System.Net.Sockets.Socket s in reference.GetReferencedSockets())
            {
                yield return Media.Common.Extensions.NetworkInterface.NetworkInterfaceExtensions.GetNetworkInterface(s);
            }
        }

    }

}
