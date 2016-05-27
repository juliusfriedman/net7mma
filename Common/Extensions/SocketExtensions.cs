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
        //public static bool IsFatal(System.Net.Sockets.SocketError error)
        //{
        //    switch (error)
        //    {
        //        case  System.Net.Sockets.SocketError.ConnectionAborted:
        //        case  System.Net.Sockets.SocketError.ConnectionReset:
        //            return true;
        //        default: return false;
        //    }
        //}

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public static System.Net.Sockets.Socket ReservePort(System.Net.Sockets.SocketType socketType, System.Net.Sockets.ProtocolType protocol, System.Net.IPAddress localIp, int port)
        {
            System.Net.Sockets.Socket result = new System.Net.Sockets.Socket(localIp.AddressFamily, socketType, protocol);

            Media.Common.Extensions.Socket.SocketExtensions.EnableAddressReuse(result);

            result.Bind(new System.Net.IPEndPoint(localIp, port));

            return result;
        }

        /// <summary>        
        /// </summary>
        /// <param name="type"></param>
        /// <param name="start"></param>
        /// <param name="even"></param>
        /// <param name="localIp"></param>
        /// <returns>-1 if no open ports were found or the next open port.</returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public static int FindOpenPort(System.Net.Sockets.ProtocolType type, int start = 30000, bool even = true, System.Net.IPAddress localIp = null)
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
            System.Collections.Generic.IEnumerable<System.Net.IPEndPoint> listeners;// = System.Linq.Enumerable.Empty<System.Net.IPEndPoint>();

            //Try to get the active listeners of the type of protocol specified.
            try
            {
                //Determine if Udp or Tcp listeners are being checked.
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
            }
            catch (System.NotImplementedException)
            {
                //When the method is not implemented then use ProbeForOpenPorts.
                return ProbeForOpenPort(type, start, even, localIp);
            }

            //Enumerate the listeners that are == then port and increase port along the way
            foreach (System.Net.IPEndPoint ep in listeners)
            {
                //If the port is less than the port in question continue.
                if (ep.Port < port) continue;

                //Ensure correctly filtering to the given IP
                if (localIp != null && ep.Address != localIp) continue;

                if (port == ep.Port) port++; //Increment the port
                else if (ep.Port == port + 1) port += 2; //Increment by 2, probably not needed. Trying to find a port pair is beyond the scope of this function.

                //Only look until the max port is reached.
                if (port > ushort.MaxValue) return -1;
            }

            //If we only want even ports and we found an even one return it
            if (even && Binary.IsEven(port) || false == even && Binary.IsOdd(port)) return port;

            //We found an even and we wanted odd or vice versa
            //Only increase the port if not ushort.MaxValue
            return port == ushort.MaxValue ? port : ++port;
        }

        /// <summary>
        /// Probes for an Open Port without needing a IPGlobalProperties implementation.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="start"></param>
        /// <param name="even"></param>
        /// <param name="localIp"></param>
        /// <returns></returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public static int ProbeForOpenPort(System.Net.Sockets.ProtocolType type, int start = 30000, bool even = true, System.Net.IPAddress localIp = null)
        {
            if (localIp == null) localIp = GetFirstUnicastIPAddress(System.Net.Sockets.AddressFamily.InterNetwork); // System.Net.IPAddress.Any should give unused ports across all IP's?

            System.Net.Sockets.Socket working = null;

            //Switch on the type
            switch (type)
            {
                //Handle TCP
                case System.Net.Sockets.ProtocolType.Tcp:
                    {
                        working = new System.Net.Sockets.Socket(localIp.AddressFamily, System.Net.Sockets.SocketType.Stream, type);

                        Media.Common.Extensions.Socket.SocketExtensions.DisableAddressReuse(working);

                        break;
                    }
                //Handle UDP
                case System.Net.Sockets.ProtocolType.Udp:
                    {
                        working = new System.Net.Sockets.Socket(localIp.AddressFamily, System.Net.Sockets.SocketType.Dgram, type);

                        Media.Common.Extensions.Socket.SocketExtensions.DisableAddressReuse(working);
                        
                        break;
                    }
                //Don't handle
                default: return -1;
            }

            //The port is in the valid range.
            using (working) while (start <= ushort.MaxValue)
            {
                try
                {
                    //Try to bind the end point.
                    working.Bind(new System.Net.IPEndPoint(localIp, start));

                    //We are done if we can bind.
                    break;
                }
                catch (System.Exception ex)
                {
                    //Check for the expected error.
                    if (ex is System.Net.Sockets.SocketException)
                    {
                        System.Net.Sockets.SocketException se = (System.Net.Sockets.SocketException)ex;

                        if (se.SocketErrorCode == System.Net.Sockets.SocketError.AddressAlreadyInUse)
                        {
                            //Try next port
                            if (++start > ushort.MaxValue)
                            {
                                //No port found
                                start = -1;

                                break;
                            }

                            //Ensure even if possible
                            if (even && Common.Binary.IsOdd(ref start) && start < ushort.MaxValue) ++start;

                            //Iterate again
                            continue;
                        }

                    }

                    //Something bad happened
                    start = -1;

                    break;
                }
            }

            //Return the port.
            return start;
        }

        /// <summary>
        /// Determine the computers first Ipv4 Address 
        /// </summary>
        /// <returns>The First IPV4 Address Found on the Machine</returns>
        public static System.Net.IPAddress GetFirstV4IPAddress()
        {
            return GetFirstUnicastIPAddress(System.Net.Sockets.AddressFamily.InterNetwork);
        }

        public static System.Net.IPAddress GetFirstV6IPAddress()
        {
            return GetFirstUnicastIPAddress(System.Net.Sockets.AddressFamily.InterNetworkV6);
        }

        public static System.Net.IPAddress GetFirstUnicastIPAddress(System.Net.Sockets.AddressFamily addressFamily)
        {
            System.Net.NetworkInformation.NetworkInterface networkInterface;

            return GetFirstUnicastIPAddress(addressFamily, out networkInterface);
        }

        public static System.Net.IPAddress GetFirstUnicastIPAddress(System.Net.Sockets.AddressFamily addressFamily, out System.Net.NetworkInformation.NetworkInterface networkInterface)
        {
            networkInterface = null;

            //Check for a supported AddressFamily
            switch (addressFamily)
            {
                case System.Net.Sockets.AddressFamily.InterNetwork:
                case System.Net.Sockets.AddressFamily.InterNetworkV6:
                    {
                        //If there is no network available use the Loopback adapter.
                        if (false == System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable()) return addressFamily == System.Net.Sockets.AddressFamily.InterNetwork ? System.Net.IPAddress.Loopback : System.Net.IPAddress.IPv6Loopback;

                        //Iterate for each Network Interface available.
                        foreach (System.Net.NetworkInformation.NetworkInterface foundInterface in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
                        {
                            //Get the first GetFirstUnicastIPAddress bound to the networkInterface
                            System.Net.IPAddress result = Common.Extensions.NetworkInterface.NetworkInterfaceExtensions.GetFirstUnicastIPAddress(foundInterface, addressFamily);

                            //If the result is not null and the result is not System.Net.IPAddress.None
                            if (result != null && false == Equals(result, System.Net.IPAddress.None))
                            {
                                networkInterface = foundInterface;

                                return result;
                            }
                        }

                        //Could not find an IP.
                        return System.Net.IPAddress.None;
                    }
                default: throw new System.NotSupportedException("Only InterNetwork or InterNetworkV6 is supported.");
            }
        }

        public static System.Net.IPAddress GetFirstMulticastIPAddress(System.Net.Sockets.AddressFamily addressFamily, out System.Net.NetworkInformation.NetworkInterface networkInterface)
        {
            networkInterface = null;

            //Check for a supported AddressFamily
            switch (addressFamily)
            {
                case System.Net.Sockets.AddressFamily.InterNetwork:
                case System.Net.Sockets.AddressFamily.InterNetworkV6:
                    {
                        //If there is no network available use the Loopback adapter.
                        if (false == System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable()) return addressFamily == System.Net.Sockets.AddressFamily.InterNetwork ? System.Net.IPAddress.Loopback : System.Net.IPAddress.IPv6Loopback;

                        //Iterate for each Network Interface available.
                        foreach (System.Net.NetworkInformation.NetworkInterface foundInterface in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
                        {
                            //Get the first GetFirstMulticastIPAddress bound to the Interface

                            //Notes that you may have to make a Socket.IO Control to determine what network Interface to use in some cases or bind on IPAddress.Any / IPAddress.V6Any
                            //https://github.com/conferencexp/conferencexp/blob/master/MSR.LST.Net.Rtp/NetworkingBasics/utility.cs

                            System.Net.IPAddress result = Common.Extensions.NetworkInterface.NetworkInterfaceExtensions.GetFirstMulticastIPAddress(foundInterface, addressFamily);

                            //If the result is not null and the result is not System.Net.IPAddress.None
                            if (result != null && false == Equals(result, System.Net.IPAddress.None))
                            {
                                networkInterface = foundInterface;

                                return result;
                            }
                        }

                        //Could not find an IP.
                        return System.Net.IPAddress.None;
                    }
                default: throw new System.NotSupportedException("Only InterNetwork or InterNetworkV6 is supported.");
            }
        }

        public static System.Net.IPAddress GetFirstMulticastIPAddress(System.Net.Sockets.AddressFamily addressFamily)
        {
            System.Net.NetworkInformation.NetworkInterface networkInterface;

            return GetFirstMulticastIPAddress(addressFamily, out networkInterface);
        }

        public static void SetMulticastTimeToLive(this System.Net.Sockets.Socket socket, int ttl)
        {
            switch (socket.AddressFamily)
            {
                case System.Net.Sockets.AddressFamily.InterNetwork:
                    {
                        socket.SetSocketOption(System.Net.Sockets.SocketOptionLevel.IP, System.Net.Sockets.SocketOptionName.MulticastTimeToLive, ttl);

                        return;
                    }
                case System.Net.Sockets.AddressFamily.InterNetworkV6:
                    {
                        socket.SetSocketOption(System.Net.Sockets.SocketOptionLevel.IPv6, System.Net.Sockets.SocketOptionName.MulticastTimeToLive, ttl);

                        return;
                    }
            }
        }

        public static void JoinMulticastGroup(this System.Net.Sockets.Socket socket, System.Net.IPAddress toJoin)
        {
            switch (toJoin.AddressFamily)
            {
                case System.Net.Sockets.AddressFamily.InterNetwork:
                    socket.SetSocketOption(System.Net.Sockets.SocketOptionLevel.IP, System.Net.Sockets.SocketOptionName.AddMembership,
                                            new System.Net.Sockets.MulticastOption(toJoin));

                    //socket.MulticastLoopback = false;

                    ////Try to specify the multicast adapter to use.
                    //socket.SetSocketOption(System.Net.Sockets.SocketOptionLevel.IP,
                    //        System.Net.Sockets.SocketOptionName.MulticastInterface,
                    //        Common.Extensions.NetworkInterface.NetworkInterfaceExtensions.GetNetworkInterface(socket).GetIPProperties().GetIPv4Properties().Index);

                    //socket.SetSocketOption(System.Net.Sockets.SocketOptionLevel.IPv6, System.Net.Sockets.SocketOptionName.AddMembership,
                    //                        new System.Net.Sockets.MulticastOption(toJoin, System.Net.IPAddress.NetworkToHostOrder(Common.Extensions.NetworkInterface.NetworkInterfaceExtensions.GetNetworkInterface(socket).GetIPProperties().GetIPv4Properties().Index)));

                    //Call SetMulticastTimeToLive
                    //socket.SetSocketOption(System.Net.Sockets.SocketOptionLevel.IP, System.Net.Sockets.SocketOptionName.MulticastTimeToLive, ttl);

                    break;
                case System.Net.Sockets.AddressFamily.InterNetworkV6:
                    socket.SetSocketOption(System.Net.Sockets.SocketOptionLevel.IPv6, System.Net.Sockets.SocketOptionName.AddMembership,
                                            new System.Net.Sockets.IPv6MulticastOption(toJoin));
                    
                    //socket.MulticastLoopback = false;

                    ////Try to specify the multicast adapter to use.
                    //socket.SetSocketOption(System.Net.Sockets.SocketOptionLevel.IPv6,
                    //        System.Net.Sockets.SocketOptionName.MulticastInterface,
                    //        Common.Extensions.NetworkInterface.NetworkInterfaceExtensions.GetNetworkInterface(socket).GetIPProperties().GetIPv6Properties().Index);

                    //socket.SetSocketOption(System.Net.Sockets.SocketOptionLevel.IPv6, System.Net.Sockets.SocketOptionName.AddMembership,
                    //                        new System.Net.Sockets.IPv6MulticastOption(toJoin, System.Net.IPAddress.NetworkToHostOrder(Common.Extensions.NetworkInterface.NetworkInterfaceExtensions.GetNetworkInterface(socket).GetIPProperties().GetIPv6Properties().Index)));

                    //Call SetMulticastTimeToLive
                    //socket.SetSocketOption(System.Net.Sockets.SocketOptionLevel.IPv6, System.Net.Sockets.SocketOptionName.MulticastTimeToLive, ttl);

                    break;
            }
        }

        //asm
        /// <summary>
        /// 
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="toJoin"></param>
        /// <param name="interfaceIndex">expected as given from the NetworkInterface in Host byte order. (In some cases it can return -1 or throw an exception)</param>
        /// <param name="ttl"></param>
        public static void JoinMulticastGroup(this System.Net.Sockets.Socket socket, System.Net.IPAddress toJoin, int interfaceIndex)
        {
            //if (interfaceIndex == -1) throw new System.InvalidOperationException();

            switch (toJoin.AddressFamily)
            {
                case System.Net.Sockets.AddressFamily.InterNetwork:
                    {
                        //socket.SetSocketOption(System.Net.Sockets.SocketOptionLevel.IP, System.Net.Sockets.SocketOptionName.MulticastInterface, interfaceIndex);

                        socket.SetSocketOption(
                            System.Net.Sockets.SocketOptionLevel.IP,
                            System.Net.Sockets.SocketOptionName.AddMembership,
                            new System.Net.Sockets.MulticastOption(toJoin, interfaceIndex));

                        //Call SetMulticastTimeToLive
                        //socket.SetSocketOption(System.Net.Sockets.SocketOptionLevel.IP, System.Net.Sockets.SocketOptionName.MulticastTimeToLive, ttl);

                        return;
                    }
                case System.Net.Sockets.AddressFamily.InterNetworkV6:

                    //socket.SetSocketOption(System.Net.Sockets.SocketOptionLevel.IPv6, System.Net.Sockets.SocketOptionName.MulticastInterface, interfaceIndex);

                    socket.SetSocketOption(
                        System.Net.Sockets.SocketOptionLevel.IPv6,
                        System.Net.Sockets.SocketOptionName.AddMembership,
                        new System.Net.Sockets.IPv6MulticastOption(toJoin, interfaceIndex));

                     //Call SetMulticastTimeToLive
                    //socket.SetSocketOption(System.Net.Sockets.SocketOptionLevel.IPv6, System.Net.Sockets.SocketOptionName.MulticastTimeToLive, ttl);

                    return;
            }
        }

        /// <summary>
        /// USed for ssm  / ip_mreq_source 
        /// </summary>
        /// <param name="localIp"></param>
        /// <param name="multicastIp"></param>
        /// <param name="sourceIp"></param>
        /// <returns></returns>
        public static byte[] CreateMembershipAddress(System.Net.IPAddress localIp, System.Net.IPAddress multicastIp, System.Net.IPAddress sourceIp)
        {
            //Todo, should check localIp, multicastIp and sourceIp versions and addresses...

            //3 ips, 12 or 36 bytes.
            int size = localIp.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork ? 4 : 12;

            int size2 = size + size;

            int size3 = size + size2;

            byte[] membershipAddress = new byte[size3];

            multicastIp.GetAddressBytes().CopyTo(membershipAddress, 0);

            sourceIp.GetAddressBytes().CopyTo(membershipAddress, size);

            localIp.GetAddressBytes().CopyTo(membershipAddress, size2);

            return membershipAddress;
        }

        //Ssm (uses the routing tables to determine the index), (maybe can specify for v6..)
        //https://social.msdn.microsoft.com/Forums/en-US/e8063f6d-22f5-445e-a00c-bf46b46c1561/how-to-join-source-specific-multicast-group-in-c?forum=netfxnetcom
        public static void JoinMulticastGroup(this System.Net.Sockets.Socket socket, System.Net.IPAddress toJoin, System.Net.IPAddress sourceIp, out byte[] membershipAddress)
        {
            membershipAddress = CreateMembershipAddress(((System.Net.IPEndPoint)socket.LocalEndPoint).Address, toJoin, sourceIp);

            switch (socket.AddressFamily)
            {
                case System.Net.Sockets.AddressFamily.InterNetwork:
                    {
                        socket.SetSocketOption(System.Net.Sockets.SocketOptionLevel.IP, 
                            System.Net.Sockets.SocketOptionName.AddSourceMembership, 
                            membershipAddress);

                        return;
                    }
                case System.Net.Sockets.AddressFamily.InterNetworkV6:
                    {
                        socket.SetSocketOption(System.Net.Sockets.SocketOptionLevel.IPv6, 
                            System.Net.Sockets.SocketOptionName.AddSourceMembership, 
                            membershipAddress);

                        return;
                    }
            }

        }

        //should also have index... Group_Req, MGroup_Req
        public static void LeaveMulticastGroup(this System.Net.Sockets.Socket socket, byte[] membershipAddress)
        {
            switch (socket.AddressFamily)
            {
                case System.Net.Sockets.AddressFamily.InterNetwork:
                    {
                        socket.SetSocketOption(System.Net.Sockets.SocketOptionLevel.IP, System.Net.Sockets.SocketOptionName.DropSourceMembership, membershipAddress);
                        
                        return;
                    }
                case System.Net.Sockets.AddressFamily.InterNetworkV6:
                    {
                        socket.SetSocketOption(System.Net.Sockets.SocketOptionLevel.IPv6, System.Net.Sockets.SocketOptionName.DropSourceMembership, membershipAddress);
                        
                        return;
                    }
            }
            
        }

        #region Other Values

        //Todo, make an IP to test and determine what options are support e.g. /MapSocketOptions.
        //Could also just have a SocketOptionProvider...

        //// SO_CONNECT_TIME         =   0x700C,

        ////IP_MULTICAST_IF         =   9, 

        ////IPV6_MULTICAST_HOPS     =   10,

        ////IPV6_MULTICAST_LOOP = 11

        //const int JoinGroup = 41;  //12 AddMembership //IPV6_ADD_MEMBERSHIP

        //const int LeaveGroup = 42; //13 DropMembership //IPV6_DROP_MEMBERSHIP //IPV6_LEAVE_GROUP        

        //const int BlockSource = 43; //17

        //const int UnblockSource = 44; //18

        ////IP_PKTINFO          = 19

        ////P_HOPLIMIT         =   21, IPV6_HOPLIMIT           

        ////IP_RECEIVE_BROADCAST    =   22,

        ////IP_RECVIF           =   24, IPV6_RECVIF         

        ////IP_IFLIST           =   28,

        ////DontFragment = 14

        //const int JoinSourceGroup = 45; // 15 (AddSourceMembership)

        //const int LeaveSourceGroup = 46; // 16 (DropSouceMembership)

        //const int Filter = 47; //MCAST_MSFILTER  

        #endregion

        public static void LeaveMulticastGroup(this System.Net.Sockets.Socket socket, System.Net.IPAddress toJoin, System.Net.IPAddress sourceIp)
        {
            LeaveMulticastGroup(socket, CreateMembershipAddress(((System.Net.IPEndPoint)socket.LocalEndPoint).Address, toJoin, sourceIp));
        }

        static void LeaveMulticastGroup(this System.Net.Sockets.Socket socket, System.Net.IPAddress toDrop)
        {
            switch (toDrop.AddressFamily)
            {
                case System.Net.Sockets.AddressFamily.InterNetwork:
                    socket.SetSocketOption(System.Net.Sockets.SocketOptionLevel.IP, System.Net.Sockets.SocketOptionName.DropMembership,
                                            new System.Net.Sockets.MulticastOption(toDrop));

                    //socket.MulticastLoopback = false;

                    return;
                case System.Net.Sockets.AddressFamily.InterNetworkV6:
                    socket.SetSocketOption(System.Net.Sockets.SocketOptionLevel.IPv6, System.Net.Sockets.SocketOptionName.DropMembership,
                                            new System.Net.Sockets.IPv6MulticastOption(toDrop));

                    //socket.MulticastLoopback = false;

                    return;
            }
        }

        static void LeaveMulticastGroup(this System.Net.Sockets.Socket socket, int interfaceIndex, System.Net.IPAddress toDrop)
        {
            switch (toDrop.AddressFamily)
            {
                case System.Net.Sockets.AddressFamily.InterNetwork:
                    {
                        socket.SetSocketOption(
                        System.Net.Sockets.SocketOptionLevel.IP,
                        System.Net.Sockets.SocketOptionName.DropMembership,
                        new System.Net.Sockets.MulticastOption(toDrop, interfaceIndex));

                        return;
                    }
                case System.Net.Sockets.AddressFamily.InterNetworkV6:
                    socket.SetSocketOption(
                        System.Net.Sockets.SocketOptionLevel.IPv6,
                        System.Net.Sockets.SocketOptionName.DropMembership,
                        new System.Net.Sockets.IPv6MulticastOption(toDrop, interfaceIndex));

                    return;
            }
        }

        //Should also have a TrySetSocketOption

        //Should ensure that the correct options are being set, these are all verified as windows options but Linux or Mac may not have them

        //SetSocketOption_internal should be determined by OperatingSystemExtensions and RuntimeExtensions.
        //Will need to build a Map of names to values for those platforms and translate.        

        internal static void SetTcpOption(System.Net.Sockets.Socket socket, System.Net.Sockets.SocketOptionName name, int value)
        {

            /*if (Common.Extensions.OperatingSystemExtensions.IsWindows) */socket.SetSocketOption(System.Net.Sockets.SocketOptionLevel.Tcp, name, value);
            //else SetSocketOption_internal 
        }

        #region Linger
        public static void DisableLinger(System.Net.Sockets.Socket socket) { socket.SetSocketOption(System.Net.Sockets.SocketOptionLevel.Socket, System.Net.Sockets.SocketOptionName.DontLinger, true); }

        public static void EnableLinger(System.Net.Sockets.Socket socket) { socket.SetSocketOption(System.Net.Sockets.SocketOptionLevel.Socket, System.Net.Sockets.SocketOptionName.DontLinger, false); }

        public static System.Net.Sockets.LingerOption GetLingerOption(System.Net.Sockets.Socket socket)
        {
            //could be byte[] or something else socket.GetSocketOption(System.Net.Sockets.SocketOptionLevel.Socket, System.Net.Sockets.SocketOptionName.Linger);

            byte [] value = socket.GetSocketOption(System.Net.Sockets.SocketOptionLevel.Socket, System.Net.Sockets.SocketOptionName.Linger, 8);

            return new System.Net.Sockets.LingerOption(Common.Binary.ReadU32(value, 0, Media.Common.Binary.IsLittleEndian) > 0, 
                (int)Common.Binary.ReadU32(value, 4, Media.Common.Binary.IsLittleEndian));
        }

        public static void SetLingerOption(System.Net.Sockets.Socket socket, System.Net.Sockets.LingerOption lingerOption)
        {
            socket.SetSocketOption(System.Net.Sockets.SocketOptionLevel.Socket, System.Net.Sockets.SocketOptionName.Linger, lingerOption);
        }

        public static void SetLingerOption(System.Net.Sockets.Socket socket, bool enable, int seconds)
        {
            SetLingerOption(socket, new System.Net.Sockets.LingerOption(enable, seconds));
        }

        #endregion

        #region Retransmission

        public static void SetMaximumTcpRetransmissionTime(System.Net.Sockets.Socket socket, int amountInSeconds = 3)
        {
            //On windows this is TCP_MAXRT elsewhere USER_TIMEOUT

            //Mono checks the options and will not call setsocketopt if the name is not known to the Mono Runtime.
            //A work around would be to either define the call for get and set socketopt in this library or call the SetSocketOption_internal method for mono.

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
            System.Net.Sockets.SocketOptionName optionName = (System.Net.Sockets.SocketOptionName)(Common.Extensions.OperatingSystemExtensions.IsWindows ? 4 : 2);

            result = (int)socket.GetSocketOption(System.Net.Sockets.SocketOptionLevel.Tcp, optionName);
        }

        public static void SetMaximumSegmentSize(System.Net.Sockets.Socket socket, int size)
        {
            System.Net.Sockets.SocketOptionName optionName = (System.Net.Sockets.SocketOptionName)(Common.Extensions.OperatingSystemExtensions.IsWindows ? 4 : 2);

            SetTcpOption(socket, optionName, size);
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

        #region UnicastPortReuse

        //Notes 4.6 has ReuseUnicastPort

        const int ReuseUnicastPort = 0x3007;

        static readonly System.Net.Sockets.SocketOptionName ReuseUnicastPortOption = (System.Net.Sockets.SocketOptionName)ReuseUnicastPort;

        public static void SetUnicastPortReuse(System.Net.Sockets.Socket socket, int value)
        {
            socket.SetSocketOption(System.Net.Sockets.SocketOptionLevel.Socket, ReuseUnicastPortOption, value);
        }

        public static void DisableUnicastPortReuse(System.Net.Sockets.Socket socket)
        {
            SetUnicastPortReuse(socket, 0);
        }

        public static void EnableUnicastPortReuse(System.Net.Sockets.Socket socket)
        {
            SetUnicastPortReuse(socket, 1);
        }

        #endregion

        #region NoSynRetries

        const int NoSynRetries = 9;

        static readonly System.Net.Sockets.SocketOptionName NoSynRetriesOption = (System.Net.Sockets.SocketOptionName)NoSynRetries;

        public static void SetTcpNoSynRetries(System.Net.Sockets.Socket socket, int amountInSeconds = 1)
        {
            SetTcpOption(socket, NoSynRetriesOption, amountInSeconds);
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

        const int Timestamp = 10;

        static readonly System.Net.Sockets.SocketOptionName TimestampOption = (System.Net.Sockets.SocketOptionName)Timestamp;

        public static void SetTcpTimestamp(System.Net.Sockets.Socket socket, int value = 1)
        {
            SetTcpOption(socket, TimestampOption, value);
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

        #region OffloadPreference

        //Todo should be enum..
        //public enum TcpOffloadPreference
        //{
        //    NoPreference = 0,
        //    NotPreferred = 1,
        //    Preferred = 2
        //}

        //
        // Offload preferences supported.
        //
        //#define TCP_OFFLOAD_NO_PREFERENCE	0
        public const int TcpOffloadNoPreference = 0;
        //#define	TCP_OFFLOAD_NOT_PREFERRED	1
        public const int TcpOffloadNotPreferred = 1;
        //#define TCP_OFFLOAD_PREFERRED		2
        public const int TcpOffloadPreferred = 2;

        const int TcpOffloadPreference = 11;
        static readonly System.Net.Sockets.SocketOptionName TcpOffloadPreferenceOption = (System.Net.Sockets.SocketOptionName)TcpOffloadPreference;


        public static void SetTcpOffloadPreference(System.Net.Sockets.Socket socket, int value = TcpOffloadPreferred) // TcpOffloadPreference.NoPreference
        {
            SetTcpOption(socket, TcpOffloadPreferenceOption, value);
        }

        #endregion

        #region CongestionAlgorithm

        const int TcpCongestionAlgorithm = 12;
        static readonly System.Net.Sockets.SocketOptionName TcpCongestionAlgorithmOption = (System.Net.Sockets.SocketOptionName)TcpCongestionAlgorithm;

        public static void SetTcpCongestionAlgorithm(System.Net.Sockets.Socket socket, int value = 1)
        {
            SetTcpOption(socket, TcpCongestionAlgorithmOption, value);
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

        #region DelayFinAck

        //#define WS_TCP_DELAY_FIN_ACK    13

        const int DelayFinAck = 13;

        static readonly System.Net.Sockets.SocketOptionName DelayFinAckOption = (System.Net.Sockets.SocketOptionName)DelayFinAck;
        
        public static void EnableTcpDelayFinAck(System.Net.Sockets.Socket socket)
        {
            SetTcpOption(socket, DelayFinAckOption, 1);
        }

        public static void DisableTcpDelayFinAck(System.Net.Sockets.Socket socket)
        {
            SetTcpOption(socket, DelayFinAckOption, 0);
        }

        #endregion

        //MaxConnectTime

        //Other useful options or combination methods e.g. NoDelay and SendBuffer / ReceiveBuffer 

        //IPV6_DONTFRAG       
        
        //InterframeGapBits (NetworkInterface)

        //Todo ->

        //SendAll / SendAllTo

        //RecieveAll / RecieveAllFrom

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
            int totalReceived = 0, max , attempt = 0, justReceived = 0;

            if (Common.Extensions.Array.ArrayExtensions.IsNullOrEmpty(buffer, out max)) return 0;

            //Account for the offset
            max -= offset;

            //Ensure that only max is received
            if (amount > max) amount = max;

            //While there is something to receive
            while (amount > 0 && socket.Connected) //poll write 0
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
                                if (attempt > amount) goto Done;

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

        /// <summary>
        /// Provides a way to Call SendTo specifying an <see cref="System.Net.Sockets.SocketError"/>.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="size"></param>
        /// <param name="socket"></param>
        /// <param name="remote"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static int SendTo(byte[] buffer, int offset, int size, System.Net.Sockets.Socket socket, System.Net.EndPoint remote, System.Net.Sockets.SocketFlags flags, out System.Net.Sockets.SocketError error)
        {
            error = System.Net.Sockets.SocketError.SocketError;

            int sent = 0;

            try
            {
                while (sent < size) sent += socket.SendTo(buffer, offset + sent, size - sent, flags, remote);

                error = System.Net.Sockets.SocketError.Success;
            }
            catch (System.Net.Sockets.SocketException se)
            {
                error = se.SocketErrorCode;
            }
            catch
            {
                throw;
            }

            return sent;
        }
    }
}
