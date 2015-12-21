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

namespace Media.Common.Extensions.Socket
{
    public static class SocketExtensions
    {
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public static System.Net.Sockets.Socket ReservePort(System.Net.Sockets.SocketType socketType, System.Net.Sockets.ProtocolType protocol, System.Net.IPAddress localIp, int port)
        {
            System.Net.Sockets.Socket result = new System.Net.Sockets.Socket(localIp.AddressFamily, socketType, protocol);

            Media.Common.Extensions.Socket.SocketExtensions.EnableAddressReuse(result);

            result.Bind(new System.Net.IPEndPoint(localIp, port));

            return result;
        }

        public static int FindOpenPort(System.Net.Sockets.ProtocolType type, int start = 30000, bool even = true)
        {
            //As IP would imply either or Only Tcp or Udp please.
            if (type != System.Net.Sockets.ProtocolType.Udp && type != System.Net.Sockets.ProtocolType.Tcp) return -1;

            //Start at the given port number
            int port = start;

            //Get the IpGlobalProperties
            System.Net.NetworkInformation.IPGlobalProperties ipGlobalProperties = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties();

            //Can't get any information
            if (ipGlobalProperties == null) return port = -1;

            //We need endpoints to ensure the ports we want are not in use
            System.Collections.Generic.IEnumerable<System.Net.IPEndPoint> listeners = System.Linq.Enumerable.Empty<System.Net.IPEndPoint>();


            switch (type)
            {
                case System.Net.Sockets.ProtocolType.Udp: 
                    listeners = ipGlobalProperties.GetActiveUdpListeners();
                    break;
                case System.Net.Sockets.ProtocolType.Tcp:
                    listeners = ipGlobalProperties.GetActiveTcpListeners();
                    break;
                default: throw new System.NotSupportedException("The given ProtocolType is not supported");
            }

            //Enumerate the ones that are = or > then port and increase port along the way
            foreach (System.Net.IPEndPoint ep in listeners)
            {
                if (ep.Port <= port) continue;

                if (port == ep.Port) port++;
                else if (ep.Port == port + 1) port += 2;
            }

            //If we only want even ports and we found an even one return it
            if (even && Binary.IsEven(port) || false == even && Binary.IsOdd(port)) return port;

            //We found an even and we wanted odd or vice versa
            return ++port;
        }

        /// <summary>
        /// Determine the computers first Ipv4 Address 
        /// </summary>
        /// <returns>The First IPV4 Address Found on the Machine</returns>
        public static System.Net.IPAddress GetFirstV4IPAddress()
        {
            return GetFirstUnicastIPAddress(System.Net.Sockets.AddressFamily.InterNetwork);
        }

        public static System.Net.IPAddress GetFirstUnicastIPAddress(System.Net.Sockets.AddressFamily addressFamily)
        {
            //Check for a supported AddressFamily
            if (addressFamily != System.Net.Sockets.AddressFamily.InterNetwork &&
                addressFamily != System.Net.Sockets.AddressFamily.InterNetworkV6) throw new System.NotSupportedException("Only InterNetwork or InterNetworkV6 is supported.");

            //If there is no network available use the Loopback adapter.
            if (false == System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable()) return addressFamily == System.Net.Sockets.AddressFamily.InterNetwork ? System.Net.IPAddress.Loopback : System.Net.IPAddress.IPv6Loopback;

            //Iterate for each Network Interface available.
            foreach (System.Net.NetworkInformation.NetworkInterface networkInterface in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
            {
                System.Net.IPAddress result = Common.Extensions.NetworkInterface.NetworkInterfaceExtensions.GetFirstUnicastIPAddress(networkInterface, addressFamily);

                if (result != null) return result;
            }

            //Could not find an IP.
            return System.Net.IPAddress.None;
        }

        //Should also have a TrySetSocketOption

        //Should ensure that the correct options are being set, these are all verified as windows options but Linux or Mac may not have them

        internal static void SetTcpOption(System.Net.Sockets.Socket socket, System.Net.Sockets.SocketOptionName name, int value)
        {
            socket.SetSocketOption(System.Net.Sockets.SocketOptionLevel.Tcp, name, value);
        }

        #region Linger
        public static void DisableLinger(System.Net.Sockets.Socket socket) { socket.SetSocketOption(System.Net.Sockets.SocketOptionLevel.Socket, System.Net.Sockets.SocketOptionName.DontLinger, true); }

        public static void EnableLinger(System.Net.Sockets.Socket socket) { socket.SetSocketOption(System.Net.Sockets.SocketOptionLevel.Socket, System.Net.Sockets.SocketOptionName.DontLinger, false); }

        #endregion

        #region Retransmission

        public static void SetMaximumTcpRetransmissionTime(System.Net.Sockets.Socket socket, int amountInSeconds = 3)
        {
            //On windows this is TCP_MAXRT elsewhere USER_TIMEOUT
            System.Net.Sockets.SocketOptionName optionName = (System.Net.Sockets.SocketOptionName)(Common.Extensions.OperatingSystemExtensions.IsWindows ? 5 : 18);

            SetTcpOption(socket, optionName, amountInSeconds);
        }

        public static void DisableTcpRetransmissions(System.Net.Sockets.Socket socket)
        {
            SetMaximumTcpRetransmissionTime(socket, 0);
        }

        public static void EnableTcpRetransmissions(System.Net.Sockets.Socket socket)
        {
            SetMaximumTcpRetransmissionTime(socket);
        }

        #endregion

        #region MaximumSegmentSize

        //Should verify the value for the option is correct for the OS.

        public static void GetMaximumSegmentSize(System.Net.Sockets.Socket socket, out int result)
        {
            result = (int)socket.GetSocketOption(System.Net.Sockets.SocketOptionLevel.Tcp, (System.Net.Sockets.SocketOptionName)0x02);
        }

        public static void SetMaximumSegmentSize(System.Net.Sockets.Socket socket, int size)
        {
            SetTcpOption(socket, (System.Net.Sockets.SocketOptionName)0x02, size);
        }

        #endregion

        #region Urgent and Expedited

        //Windows

        //#define TCP_NOURG                   7
        const int NotUrgent = 7;

        //#define TCP_STDURG                  6
        const int StandardUrgency = 6;

        internal static void SetTcpExpedited(System.Net.Sockets.Socket socket, int amount = 1)
        {
            SetTcpOption(socket, System.Net.Sockets.SocketOptionName.Expedited, amount);
        }

        public static void SetTcpNotUrgent(System.Net.Sockets.Socket socket)
        {
            SetTcpOption(socket, (System.Net.Sockets.SocketOptionName)NotUrgent, 1);
        }

        public static void SetTcpStandardUrgent(System.Net.Sockets.Socket socket)
        {
            SetTcpOption(socket, (System.Net.Sockets.SocketOptionName)StandardUrgency, 1);
        }

        public static void EnableTcpExpedited(System.Net.Sockets.Socket socket)
        {
            SetTcpExpedited(socket);
        }

        //DisableTcpExpedited... Can technically be done on the socket just be using the opposite option value

        #endregion

        #region NoDelay / Nagle

        public static void EnableTcpNagelAlgorithm(System.Net.Sockets.Socket socket)
        {
            SetTcpNoDelay(socket, 0);
        }

        public static void DisableTcpNagelAlgorithm(System.Net.Sockets.Socket socket)
        {
            SetTcpNoDelay(socket, 1);
        }

        internal static void SetTcpNoDelay(System.Net.Sockets.Socket socket, int amount = 1)
        {
            SetTcpOption(socket, System.Net.Sockets.SocketOptionName.NoDelay, amount);
        }

        #endregion

        #region OutOfBandInline

        public static void EnableTcpOutOfBandDataInLine(System.Net.Sockets.Socket socket)
        {
            socket.SetSocketOption(System.Net.Sockets.SocketOptionLevel.Socket, System.Net.Sockets.SocketOptionName.OutOfBandInline, true);

            //SetTcpOption(socket, System.Net.Sockets.SocketOptionName.OutOfBandInline, 1);
        }

        public static void DisableTcpOutOfBandDataInLine(System.Net.Sockets.Socket socket)
        {
            socket.SetSocketOption(System.Net.Sockets.SocketOptionLevel.Socket, System.Net.Sockets.SocketOptionName.OutOfBandInline, false);
        }

        #endregion

        #region KeepAlive

        const int KeepAliveSize = 12;

        public static void EnableTcpKeepAlive(System.Net.Sockets.Socket socket, int time, int interval)
        {
            using (var optionMemory = new Common.MemorySegment(KeepAliveSize))
            {
                Common.Binary.Write32(optionMemory.Array, 0, false, 1);

                Common.Binary.Write32(optionMemory.Array, Common.Binary.BytesPerInteger, false, time);

                Common.Binary.Write32(optionMemory.Array, Common.Binary.BytesPerLong, false, interval);

                int result = socket.IOControl(System.Net.Sockets.IOControlCode.KeepAliveValues, optionMemory.Array, optionMemory.Array);
            }
        }

        public static void DisableTcpKeepAlive(System.Net.Sockets.Socket socket)
        {
            using (var optionMemory = new Common.MemorySegment(KeepAliveSize))
            {
                Common.Binary.Write32(optionMemory.Array, 0, false, 0);

                int result = socket.IOControl(System.Net.Sockets.IOControlCode.KeepAliveValues, optionMemory.Array, optionMemory.Array);
            }
        }

        #endregion

        #region ReuseAddress

        public static void EnableAddressReuse(System.Net.Sockets.Socket socket, bool exclusiveAddressUse = false)
        {
            socket.ExclusiveAddressUse = exclusiveAddressUse;

            socket.SetSocketOption(System.Net.Sockets.SocketOptionLevel.Socket, System.Net.Sockets.SocketOptionName.ReuseAddress, true);
        }

        public static void DisableAddressReuse(System.Net.Sockets.Socket socket, bool exclusiveAddressUse = true)
        {
            socket.ExclusiveAddressUse = exclusiveAddressUse;

            socket.SetSocketOption(System.Net.Sockets.SocketOptionLevel.Socket, System.Net.Sockets.SocketOptionName.ReuseAddress, false);
        }

        #endregion

        #region NoSynRetries

        public static void SetTcpNoSynRetries(System.Net.Sockets.Socket socket, int amountInSeconds = 1)
        {
            SetTcpOption(socket, (System.Net.Sockets.SocketOptionName)9, amountInSeconds);
        }

        public static void DisableTcpNoSynRetries(System.Net.Sockets.Socket socket)
        {
            SetTcpNoSynRetries(socket, 0);
        }

        public static void EnableTcpNoSynRetries(System.Net.Sockets.Socket socket)
        {
            SetTcpNoSynRetries(socket, 1);
        }

        #endregion

        #region Timestamp

        public static void SetTcpTimestamp(System.Net.Sockets.Socket socket, int value = 1)
        {
            SetTcpOption(socket, (System.Net.Sockets.SocketOptionName)10, value);
        }

        public static void DisableTcpTimestamp(System.Net.Sockets.Socket socket)
        {
            SetTcpNoSynRetries(socket, 0);
        }

        public static void EnableTcpTimestamp(System.Net.Sockets.Socket socket)
        {
            SetTcpNoSynRetries(socket, 1);
        }

        #endregion

        #region CongestionAlgorithm

        public static void SetTcpCongestionAlgorithm(System.Net.Sockets.Socket socket, int value = 1)
        {
            SetTcpOption(socket, (System.Net.Sockets.SocketOptionName)12, value);
        }

        public static void DisableTcpCongestionAlgorithm(System.Net.Sockets.Socket socket)
        {
            SetTcpCongestionAlgorithm(socket, 0);
        }

        public static void EnableTcpCongestionAlgorithm(System.Net.Sockets.Socket socket)
        {
            SetTcpCongestionAlgorithm(socket, 1);
        }

        #endregion

        #region OffloadPreference

        //
        // Offload preferences supported.
        //
        //#define TCP_OFFLOAD_NO_PREFERENCE	0
        public const int TcpOffloadNoPreference = 1;
        //#define	TCP_OFFLOAD_NOT_PREFERRED	1
        public const int TcpOffloadNotPreferred = 1;
        //#define TCP_OFFLOAD_PREFERRED		2
        public const int TcpOffloadPreferred = 2;

        public static void SetTcpOffloadPreference(System.Net.Sockets.Socket socket, int value = TcpOffloadPreferred)
        {
            SetTcpOption(socket, (System.Net.Sockets.SocketOptionName)11, value);
        }

        #endregion

        #region MaxSegmentSize

        public static void SetTcpMaxSegmentSize(System.Net.Sockets.Socket socket, int valueInBytes)
        {
            SetTcpOption(socket, (System.Net.Sockets.SocketOptionName)4, valueInBytes);
        }

        #endregion

        #region DelayFinAck

        //#define WS_TCP_DELAY_FIN_ACK    13
        
        public static void EnableTcpDelayFinAck(System.Net.Sockets.Socket socket)
        {
            SetTcpOption(socket, (System.Net.Sockets.SocketOptionName)13, 1);
        }

        public static void DisableTcpDelayFinAck(System.Net.Sockets.Socket socket)
        {
            SetTcpOption(socket, (System.Net.Sockets.SocketOptionName)13, 0);
        }

        #endregion

        //MaxConnectTime

        //Other useful options or combination methods e.g. NoDelay and SendBuffer / ReceiveBuffer 

        //IPV6_DONTFRAG       
        
        //InterframeGapBits (NetworkInterface)

        /// <summary>
        /// Receives the given amount of bytes into the buffer given a offset and an amount.
        /// </summary>
        /// <param name="buffer">The array to receive into</param>
        /// <param name="offset">The location to receive into</param>
        /// <param name="amount">The 0 based amount of bytes to receive, 0 will have no result</param>
        /// <param name="socket">The socket to receive on</param>
        /// <returns>The amount of bytes recieved which will be equal to the amount paramter unless the data was unable to fit in the given buffer</returns>
        public static int AlignedReceive(byte[] buffer, int offset, int amount, System.Net.Sockets.Socket socket, out System.Net.Sockets.SocketError error)
        {
            //Store any socket errors here incase non-blocking sockets are being used.
            error = System.Net.Sockets.SocketError.SocketError;

            //Return the amount if its negitive;
            if (amount <= 0) return amount;

            //To hold what was received and the maximum amount to receive
            int totalReceived = 0, max = buffer.Length - offset, attempt = 0, justReceived = 0;

            //Ensure that only max is received
            if (amount > max) amount = max;

            //While there is something to receive
            while (amount > 0)
            {
                //Receive it into the buffer at the given offset taking into account what was already received
                justReceived = socket.Receive(buffer, offset, amount, System.Net.Sockets.SocketFlags.None, out error);

                switch (error)
                {
                    case System.Net.Sockets.SocketError.ConnectionReset:
                    case System.Net.Sockets.SocketError.ConnectionAborted:
                    case System.Net.Sockets.SocketError.TimedOut:
                        goto Done;
                    default:
                        {
                            //If nothing was received
                            if (justReceived <= 0)
                            {
                                //Try again maybe
                                ++attempt;

                                //Only if the attempts in operations were greater then the amount of bytes requried
                                if (attempt > amount) goto Done;//case System.Net.Sockets.SocketError.TimedOut;

                                continue;
                            }

                            //decrease the amount by what was received
                            amount -= justReceived;

                            //Increase the offset by what was received
                            offset += justReceived;

                            //Increase total received
                            totalReceived += justReceived;
                            
                            continue;
                        }
                }
            }

        Done:
            return totalReceived;
        }
    }
}
