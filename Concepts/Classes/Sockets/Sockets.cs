using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Concepts.Classes.Sockets
{
    /// <summary>
    /// Contains methods for setting SocketFlags via stored values.
    /// Overlaps 2 <see cref="System.Net.Sockets.SocketFlags"/> structures in one using the same amount of memory.
    /// </summary>
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Explicit)]
    public struct SocketFlagsInformation
    {
        #region Fields

        /// <summary>
        /// 4 bytes which are used to store various <see cref="System.Net.Sockets.SocketFlags"/>
        /// </summary>
        [System.Runtime.InteropServices.FieldOffset(0)]
        internal int SocketFlags;

        /// <summary>
        /// <see cref="System.Net.Sockets.SocketFlags"/> which are used for Send
        /// </summary>
        /// <remarks>
        /// 4 bytes, of which only 2 are used.
        /// </remarks>
        [System.Runtime.InteropServices.FieldOffset(0)]
        public System.Net.Sockets.SocketFlags SendFlags;

        /// <summary>
        /// <see cref="System.Net.Sockets.SocketFlags"/> which are used for Recieve
        /// </summary>
        [System.Runtime.InteropServices.FieldOffset(2)]
        public System.Net.Sockets.SocketFlags ReceiveFlags;

        /// <summary>
        /// 4 bytes which are used to store various <see cref="System.Net.Sockets.SocketError"/>
        /// </summary>
        [System.Runtime.InteropServices.FieldOffset(4)]
        internal int SocketErrors;

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// 4 bytes, of which only 2 are used.
        /// </remarks>
        [System.Runtime.InteropServices.FieldOffset(4)]
        public System.Net.Sockets.SocketError SendError;

        /// <summary>
        /// 
        /// </summary>
        [System.Runtime.InteropServices.FieldOffset(6)]
        public System.Net.Sockets.SocketError ReceiveError;

        #endregion

        #region Methods

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public int Send(System.Net.Sockets.Socket socket, byte[] data, ref int offset, ref int count, ref System.Net.Sockets.SocketFlags flags, out System.Net.Sockets.SocketError error)
        {
            return socket.Send(data, offset, count, flags, out error);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public int Receive(System.Net.Sockets.Socket socket, byte[] data, ref int offset, ref int count, ref System.Net.Sockets.SocketFlags flags, out System.Net.Sockets.SocketError error)
        {
            return socket.Receive(data, offset, count, flags, out error);
        }        


        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public int IsFatalError(ref System.Net.Sockets.SocketError error, System.Net.Sockets.Socket socket)
        {
            switch (error)
            {
                case System.Net.Sockets.SocketError.ConnectionAborted:
                case System.Net.Sockets.SocketError.ConnectionReset:
                case System.Net.Sockets.SocketError.Shutdown:
                    return 1;
                case System.Net.Sockets.SocketError.Success:
                    return 0;
                case System.Net.Sockets.SocketError.SocketError:
                default: return (int)error;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public int ReceiveAll(System.Net.Sockets.Socket socket, byte[] data, ref int offset, ref int count, ref System.Net.Sockets.SocketFlags flags, out System.Net.Sockets.SocketError error)
        {
            int total = 0, local = 0;

            do
            {
                local= Receive(socket, data, ref offset, ref count, ref flags, out error);

                total += local;

                offset += local;

                count -= local;

                if (IsFatalError(ref error, socket) > 0) break;

            } while (total < count);

            return total;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public int SendAll(System.Net.Sockets.Socket socket, byte[] data, ref int offset, ref int count, ref System.Net.Sockets.SocketFlags flags, out System.Net.Sockets.SocketError error)
        {
            int total = 0, local = 0;

            do
            {
                local = Send(socket, data, ref offset, ref count, ref flags, out error);

                total += local;

                offset += local;

                if (IsFatalError(ref error, socket) > 0) break;

            } while (total < count);

            return total;
        }

        //Send and Receive without flags and error which use the appropraite flags and store the appropriate error

        public int SendAll(System.Net.Sockets.Socket socket, byte[] data, ref int offset, ref int count)
        {
            return SendAll(socket, data, ref offset, ref count, ref SendFlags, out SendError);
        }

        public int ReceiveAll(System.Net.Sockets.Socket socket, byte[] data, ref int offset, ref int count)
        {
            return ReceiveAll(socket, data, ref offset, ref count, ref ReceiveFlags, out ReceiveError);
        }

        #endregion

        #region Information

        //http://www.cubrid.org/blog/dev-platform/understanding-tcp-ip-network-stack/

        //On windows its possible to interface directly with the NET_BUFFER which contains the inbound and outbound data.
        //https://msdn.microsoft.com/en-us/library/windows/hardware/ff543696(v=vs.85).aspx
        //https://msdn.microsoft.com/en-us/library/windows/hardware/ff568376(v=vs.85).aspx

        //gstreamer-netbuffer-0.10 
        //https://www.freedesktop.org/software/gstreamer-sdk/data/docs/latest/gst-plugins-base-libs-0.10/gst-plugins-base-libs-gstnetbuffer.html

        //GstNetBuffer is a subclass of a normal GstBuffer that contains two additional metadata fields of type GstNetAddress named 'to' and 'from'. The buffer can be used to store additional information about the origin of the buffer data and is used in various network elements to track the to and from addresses.

        #endregion
    }
}
