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
            result.SetSocketOption(System.Net.Sockets.SocketOptionLevel.Socket, System.Net.Sockets.SocketOptionName.ReuseAddress, true);
            result.Bind(new System.Net.IPEndPoint(localIp, port));
            return result;
        }

        public static int FindOpenPort(System.Net.Sockets.ProtocolType type, int start = 30000, bool even = true)
        {
            //Only Tcp or Udp :)
            if (type != System.Net.Sockets.ProtocolType.Udp && type != System.Net.Sockets.ProtocolType.Tcp) return -1;

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

        //Todo
        //Make non Loopback overloads

        /// <summary>
        /// Determine the computers first Ipv4 Address 
        /// </summary>
        /// <returns>The First IPV4 Address Found on the Machine</returns>
        public static System.Net.IPAddress GetFirstV4IPAddress()
        {
            return GetFirstIPAddress(System.Net.Sockets.AddressFamily.InterNetwork);
        }

        public static System.Net.IPAddress GetFirstIPAddress(System.Net.Sockets.AddressFamily addressFamily)
        {
            foreach (System.Net.IPAddress ip in System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName()).AddressList) 
                if (ip.AddressFamily == addressFamily) 
                    return ip;

            return System.Net.IPAddress.None;
        }

        public static System.Net.NetworkInformation.NetworkInterface GetNetworkInterface(System.Net.Sockets.Socket s)
        {
            foreach (System.Net.NetworkInformation.NetworkInterface networkInterface in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
            {
                foreach (System.Net.NetworkInformation.UnicastIPAddressInformation ip in networkInterface.GetIPProperties().UnicastAddresses)
                {
                    if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        return networkInterface;
                    }
                }
            }

            return default(System.Net.NetworkInformation.NetworkInterface);
        }

        public static void DisableLinger(System.Net.Sockets.Socket socket) { socket.SetSocketOption(System.Net.Sockets.SocketOptionLevel.Socket, System.Net.Sockets.SocketOptionName.DontLinger, true); }

        public static void EnableLinger(System.Net.Sockets.Socket socket) { socket.SetSocketOption(System.Net.Sockets.SocketOptionLevel.Socket, System.Net.Sockets.SocketOptionName.DontLinger, false); }

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
            int totalReceived = 0, max = buffer.Length - offset, attempt = 0;

            //Ensure that only max is received
            if (amount > max) amount = max;

            //While there is something to receive
            while (amount > 0)
            {
                lock (socket)
                {
                    //Receive it into the buffer at the given offset taking into account what was already received
                    int justReceived = socket.Receive(buffer, offset, amount, System.Net.Sockets.SocketFlags.None, out error);

                    //decrease the amount by what was received
                    amount -= justReceived;

                    //Increase the offset by what was received
                    offset += justReceived;

                    //Increase total received
                    totalReceived += justReceived;

                    //If nothing was received
                    if (justReceived == 0)
                    {
                        //Try again maybe
                        ++attempt;

                        //Only if the attempts in operations were greater then the amount of bytes requried
                        if (attempt > amount) error = System.Net.Sockets.SocketError.TimedOut;
                    }

                    //Break on offset reaching the max or any error which requires
                    if (offset >= max || error == System.Net.Sockets.SocketError.ConnectionAborted || error == System.Net.Sockets.SocketError.TimedOut || error == System.Net.Sockets.SocketError.ConnectionReset) break;
                }
            }

            return totalReceived;
        }
    }
}
