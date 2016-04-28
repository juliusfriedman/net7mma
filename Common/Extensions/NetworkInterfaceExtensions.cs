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

namespace Media.Common.Extensions.NetworkInterface
{
    public static class NetworkInterfaceExtensions
    {
        /// <summary>
        /// Gets the first <see cref="System.Net.NetworkInformation.NetworkInterface"/> which has the given address bound.
        /// </summary>
        /// <param name="localAddress">The address which should be bound to the interface.</param>
        /// <returns>The <see cref="System.Net.NetworkInformation.NetworkInterface"/> associated with the address or the default if none were found.</returns>
        public static System.Net.NetworkInformation.NetworkInterface GetNetworkInterface(System.Net.IPAddress localAddress)
        {
            if (localAddress == null) throw new System.ArgumentNullException();

            bool isMulticast = Common.Extensions.IPAddress.IPAddressExtensions.IsMulticast(localAddress);

            //Iterate all NetworkInterfaves
            foreach (System.Net.NetworkInformation.NetworkInterface networkInterface in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
            {
                //Only look for interfaces which are UP
                if (networkInterface.OperationalStatus != System.Net.NetworkInformation.OperationalStatus.Up) continue;

                if (isMulticast)
                {
                    if (false == networkInterface.SupportsMulticast) continue;

                    //Check for the Multicast Address to be bound on the networkInterface
                    foreach (System.Net.NetworkInformation.MulticastIPAddressInformation ip in networkInterface.GetIPProperties().MulticastAddresses)
                    {
                        //If equal return
                        if (System.Net.IPAddress.Equals(localAddress, ip.Address))
                        {
                            return networkInterface;
                        }
                    }
                }
                else
                {
                    //Check for the Unicast Address to be bound on the networkInterface
                    foreach (System.Net.NetworkInformation.UnicastIPAddressInformation ip in networkInterface.GetIPProperties().UnicastAddresses)
                    {
                        //If equal return
                        if (System.Net.IPAddress.Equals(localAddress, ip.Address))
                        {
                            return networkInterface;
                        }
                    }

                    //Check for the Anycast Address to be bound on the networkInterface
                    //if(s.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6) foreach (System.Net.NetworkInformation.UnicastIPAddressInformation ip in networkInterface.GetIPProperties().AnycastAddresses)
                    //{
                    //    if (System.Net.IPAddress.Equals(localEndPoint.Address, ip.Address))
                    //    {
                    //        return networkInterface;
                    //    }
                    //}
                }
            }

            return default(System.Net.NetworkInformation.NetworkInterface);
        }

        public static System.Collections.Generic.IEnumerable<System.Net.NetworkInformation.NetworkInterface> GetNetworkInterface(System.Net.NetworkInformation.NetworkInterfaceType interfaceType)
        {
            if(interfaceType == System.Net.NetworkInformation.NetworkInterfaceType.Unknown) yield break;

            foreach (System.Net.NetworkInformation.NetworkInterface networkInterface in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
            {
                if (networkInterface.NetworkInterfaceType == interfaceType) yield return networkInterface;
            }
        }

        public static System.Net.NetworkInformation.NetworkInterface GetNetworkInterface(System.Net.IPEndPoint localEndPoint)
        {
            if (localEndPoint == null) throw new System.ArgumentNullException("localEndPoint");

            return GetNetworkInterface(localEndPoint.Address);
        }

        public static System.Net.NetworkInformation.NetworkInterface GetNetworkInterface(System.Net.Sockets.Socket socket)
        {
            if (socket == null) throw new System.ArgumentNullException("socket");

            if (false == socket.IsBound) throw new System.InvalidOperationException("socket.IsBound must be true.");

            return GetNetworkInterface((System.Net.IPEndPoint)socket.LocalEndPoint);
        }

        /// <summary>
        /// <see href="http://en.wikipedia.org/wiki/Interpacket_gap">Interpacket Gap</see>
        /// </summary>
        public const int MinimumInterframeGapBits = 96;

        /// <summary>
        /// The time in nanoseconds it takes to transmit 96 bits of raw data on the medium
        /// </summary>
        /// <param name="networkInterface"></param>
        /// <returns></returns>
        public static double GetInterframeGapNanoseconds(this System.Net.NetworkInformation.NetworkInterface networkInterface)
        {
            if(networkInterface == null) return 0;

            return (MinimumInterframeGapBits / (networkInterface.Speed / Common.Extensions.TimeSpan.TimeSpanExtensions.NanosecondsPerSecond));
        }

        /// <summary>
        /// The time in microseconds it takes to transmit 96 bits of raw data on the medium
        /// </summary>
        /// <param name="networkInterface"></param>
        /// <returns></returns>
        public static long GetInterframeGapMicroseconds(this System.Net.NetworkInformation.NetworkInterface networkInterface)
        {            
            return (long)(GetInterframeGapNanoseconds(networkInterface) * Media.Common.Extensions.TimeSpan.TimeSpanExtensions.NanosecondsPerSecond);
        }

        public static System.Net.IPAddress GetFirstMulticastIPAddress(System.Net.NetworkInformation.NetworkInterface networkInterface, System.Net.Sockets.AddressFamily addressFamily)
        {
            //Filter interfaces which are not usable.
            if (networkInterface == null || 
                false == networkInterface.SupportsMulticast ||
                networkInterface.OperationalStatus != System.Net.NetworkInformation.OperationalStatus.Up)// The interface is not up (should probably ignore...?)
            {
                return System.Net.IPAddress.None;
            }

            //Get the IPInterfaceProperties for the NetworkInterface
            System.Net.NetworkInformation.IPInterfaceProperties interfaceProperties = networkInterface.GetIPProperties();

            //If there are no IPInterfaceProperties then try the next interface
            if (interfaceProperties == null) return null;

            //Iterate for each Multicast IP bound to the interface
            foreach (System.Net.NetworkInformation.MulticastIPAddressInformation multicastIpInfo in interfaceProperties.MulticastAddresses)
            {
                //If the IP AddresFamily is the same as required then return it. (Maybe Broadcast...)
                if (multicastIpInfo.Address.AddressFamily == addressFamily) return multicastIpInfo.Address;
            }

            //Indicate no multicast IPAddress was found
            return System.Net.IPAddress.None;
        }

        public static System.Net.IPAddress GetFirstUnicastIPAddress(System.Net.NetworkInformation.NetworkInterface networkInterface, System.Net.Sockets.AddressFamily addressFamily)
        {            
            //Filter interfaces which are not usable.
            if (networkInterface == null || 
                networkInterface.OperationalStatus != System.Net.NetworkInformation.OperationalStatus.Up)// The interface is not up
            {
                return System.Net.IPAddress.None;
            }

            //Get the IPInterfaceProperties for the NetworkInterface
            System.Net.NetworkInformation.IPInterfaceProperties interfaceProperties = networkInterface.GetIPProperties();

            //If there are no IPInterfaceProperties then try the next interface
            if (interfaceProperties == null) return null;

            //Iterate for each Unicast IP bound to the interface
            foreach (System.Net.NetworkInformation.UnicastIPAddressInformation unicastIpInfo in interfaceProperties.UnicastAddresses)
            {
                //Get the address
                System.Net.IPAddress address = unicastIpInfo.Address;

                //If the IP AddressFamily is not the same as required then return it.
                if (address.AddressFamily != addressFamily) continue;

                //Don't use Any and don't use Broadcast.
                if (address.Equals(System.Net.IPAddress.Broadcast)
                    ||
                    (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6 ? address.Equals(System.Net.IPAddress.IPv6Any) : address.Equals(System.Net.IPAddress.Any))) continue;

                //Return the compatible address.
                return address;
            }

            //Indicate no unicast IPAddress was found
            return System.Net.IPAddress.None;
        }
    }
}

//UnitTests, Test for Speed 0, -> 100000 Mpbs, Test for Connected, Disconnected etc.