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

        /// <summary>
        /// Gets the first <see cref="System.Net.NetworkInformation.NetworkInterface"/> by the given criteria.
        /// </summary>
        /// <param name="status">The status of the interface.</param>
        /// <param name="interfaceTypes">The types of interfaces.</param>
        /// <returns>If any, the interfaces which correspond to the given criteria.</returns>
        public static System.Collections.Generic.IEnumerable<System.Net.NetworkInformation.NetworkInterface> GetNetworkInterface(System.Net.NetworkInformation.OperationalStatus status, params System.Net.NetworkInformation.NetworkInterfaceType[] interfaceTypes)
        {
            if(interfaceTypes == null) yield break;

            foreach (System.Net.NetworkInformation.NetworkInterface networkInterface in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
            {
                if(0 >= System.Array.IndexOf(interfaceTypes, networkInterface.NetworkInterfaceType) && networkInterface.OperationalStatus.Equals(status)) yield return networkInterface;
            }
        }

        /// <summary>
        /// Gets the first <see cref="System.Net.NetworkInformation.NetworkInterface"/> with the <see cref="System.Net.NetworkInformation.OperationalStatus"/> of <see cref="System.Net.NetworkInformation.OperationalStatus.Up"/>
        /// </summary>
        /// <param name="interfaceTypes">The <see cref="System.Net.NetworkInformation.NetworkInterfaceType"/>'s which correspond to the interfaces to enumerate.</param>
        /// <returns>If any, the interfaces which correspond to the given criteria.</returns>
        public static System.Collections.Generic.IEnumerable<System.Net.NetworkInformation.NetworkInterface> GetNetworkInterface(params System.Net.NetworkInformation.NetworkInterfaceType[] interfaceTypes)
        {
            return GetNetworkInterface(System.Net.NetworkInformation.OperationalStatus.Up, interfaceTypes);
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
        /// Converts the speed from Bits to Bytes per second.
        /// </summary>
        /// <param name="networkInterface"></param>
        /// <returns></returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static double GetSpeedInMBytesPerSecond(this System.Net.NetworkInformation.NetworkInterface networkInterface)
        {
            if (networkInterface == null) return 0;

            long speed = networkInterface.Speed;

            if (speed <= 0) return 0;

            return  (networkInterface.Speed / Common.Extensions.TimeSpan.TimeSpanExtensions.NanosecondsPerMillisecond);
        }

        /// <summary>
        /// <see href="http://en.wikipedia.org/wiki/Interpacket_gap">Interpacket Gap</see>
        /// </summary>
        /// <remarks>12 bytes</remarks>
        public const int MinimumInterframeGapBits = 96;

        /// <summary>
        /// Calculate the time in nanoseconds it takes to transmit 96 bits given a speed in bits per second.
        /// </summary>
        /// <param name="speedAsBitsPerSecond">A speed on a network interface in the terms bits per second.</param>
        /// <returns>The amount of Nanoseconds which corresponds to the interframe gap.</returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static double CaulculateInterframeGapNanoseconds(long speedAsBitsPerSecond)
        {
            //Speed can be 0 or -1 if not connected.
            if (speedAsBitsPerSecond <= 0) return 0;

            //Convert the bits per second to bytes per second and divide by the MinimumInterframeGapBits
            return (MinimumInterframeGapBits / (double)(speedAsBitsPerSecond / Common.Extensions.TimeSpan.TimeSpanExtensions.NanosecondsPerSecond));

            //Faster but has small rounding error less than 1...
            //System.Math.Ceiling(
            //Resolves this rounding error, remember to round or use ceiling to compare the results.
            //return (MinimumInterframeGapBits / (double)speedAsBitsPerSecond) * Common.Extensions.TimeSpan.TimeSpanExtensions.NanosecondsPerSecond;
        }

        /// <summary>
        /// The time in nanoseconds it takes to transmit 96 bits of raw data on the medium
        /// </summary>
        /// <param name="networkInterface"></param>
        /// <returns></returns>
        public static double GetInterframeGapNanoseconds(this System.Net.NetworkInformation.NetworkInterface networkInterface)
        {
            if(networkInterface == null) return 0;

            return CaulculateInterframeGapNanoseconds(networkInterface.Speed);
        }

        /// <summary>
        /// The time in microseconds it takes to transmit 96 bits of raw data on the medium
        /// </summary>
        /// <param name="networkInterface"></param>
        /// <returns></returns>
        public static double GetInterframeGapMicroseconds(this System.Net.NetworkInformation.NetworkInterface networkInterface)
        {
            return (GetInterframeGapNanoseconds(networkInterface) * Media.Common.Extensions.TimeSpan.TimeSpanExtensions.NanosecondsPerMicrosecond);
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


namespace Media.UnitTests
{
    internal class NetworkInterfaceExtensionsTests
    {
        public void TestInterframeGap()
        {
            long speedInBitsPerSecond;

            double gapNanos;

            double gapMicros;

            System.TimeSpan microTime;

            //Todo, method with various types of interfaces
            foreach (var nif in Media.Common.Extensions.NetworkInterface.NetworkInterfaceExtensions.GetNetworkInterface(System.Net.NetworkInformation.NetworkInterfaceType.Ethernet, System.Net.NetworkInformation.NetworkInterfaceType.Wireless80211))
            {
                //Speed changes for Wifi between calls but speed always shows 100000 would need interop to ask stack what the current speed or max speed is...
                speedInBitsPerSecond = nif.Speed;

                //Calulcate the gap in Nanoseconds
                gapNanos = Media.Common.Extensions.NetworkInterface.NetworkInterfaceExtensions.GetInterframeGapNanoseconds(nif);

                //Caulcate the same gap but in Microseconds
                gapMicros = Media.Common.Extensions.NetworkInterface.NetworkInterfaceExtensions.GetInterframeGapMicroseconds(nif);

                //Calulcate a TimeSpan which represents the total Microseconds.
                microTime = Media.Common.Extensions.TimeSpan.TimeSpanExtensions.FromMicroseconds((double)gapMicros);

                //Rounding ...
                //if (Media.Common.Extensions.TimeSpan.TimeSpanExtensions.TotalMicroseconds(microTime) != gapMicros) throw new System.Exception("TotalMicroseconds");

                //Verify that the conversion was correct
                if ((int)Media.Common.Extensions.TimeSpan.TimeSpanExtensions.TotalMicroseconds(microTime) != (int)gapMicros) throw new System.Exception("TotalMicroseconds");

                //Calculate how much difference there is when converting from the microsecond term to the nano second term.
                double diff = gapMicros * Media.Common.Extensions.TimeSpan.TimeSpanExtensions.NanosecondsPerMicrosecond  - Media.Common.Extensions.TimeSpan.TimeSpanExtensions.TotalNanoseconds(microTime);

                //If there was any difference 
                if (diff > 0)
                {
                    //if that difference is greater than 1 nano second throw an exception
                    if (diff > Media.Common.Extensions.NetworkInterface.NetworkInterfaceExtensions.MinimumInterframeGapBits) throw new System.Exception("TotalNanoseconds");
                    else System.Console.WriteLine("μs to ns conversion Different By: " + diff);
                }

                //Write the information
                System.Console.WriteLine(string.Format("Name: {0}, Type: {1}, \r\nSpeed Bits Per Second: {2}, \r\nGapMicros:{3} GapNanos{4}, MicroTime:{5}", nif.Name, nif.NetworkInterfaceType, speedInBitsPerSecond, gapMicros, gapNanos, microTime));

                System.Console.WriteLine("Speed In MBytes Per Second: " + Media.Common.Extensions.NetworkInterface.NetworkInterfaceExtensions.GetSpeedInMBytesPerSecond(nif));

                //Verify results
                switch (speedInBitsPerSecond)
                {
                    //1Gpbs
                    case 10000000000:
                        {
                            if (gapNanos != 9.6) throw new System.Exception("Invalid InterframeGap");
                            break;
                        }
                    //1Gpbs
                    case 1000000000:
                        {
                            if (gapNanos != 96) throw new System.Exception("Invalid InterframeGap");
                            break;
                        }
                    //100 Mbps
                    case 100000000:
                        {
                            if (gapNanos != 960) throw new System.Exception("Invalid InterframeGap");
                            break;
                        }
                    //10 Mbps
                    case 10000000:
                        {
                            if (gapNanos != 9600) throw new System.Exception("Invalid InterframeGap");
                            break;
                        }
                    //1 Mbps
                    case 1000000:
                        {
                            if (gapNanos != 96000) throw new System.Exception("Invalid InterframeGap");
                            break;
                        }
                }
            }

            ////Todo, fix overflow for all speeds.
            //for (int i = 1; i <= 1000000000; ++i)
            //{
            //    speedInBitsPerSecond = i * 100;

            //    gapNanos = Media.Common.Extensions.NetworkInterface.NetworkInterfaceExtensions.CaulculateInterframeGapNanoseconds(speedInBitsPerSecond);

            //    gapMicros = (long)(gapNanos * Media.Common.Extensions.TimeSpan.TimeSpanExtensions.NanosecondsPerMicrosecond);

            //    microTime = Media.Common.Extensions.TimeSpan.TimeSpanExtensions.FromMicroseconds((double)gapMicros);

            //    if ((int)Media.Common.Extensions.TimeSpan.TimeSpanExtensions.TotalMicroseconds(microTime) != (int)gapMicros) throw new System.Exception("TotalMicroseconds");

            //    //Calculate how much difference there is when converting from the microsecond term to the nano second term.
            //    double diff = gapMicros * Media.Common.Extensions.TimeSpan.TimeSpanExtensions.NanosecondsPerMicrosecond - Media.Common.Extensions.TimeSpan.TimeSpanExtensions.TotalNanoseconds(microTime);

            //    if (diff > 0)
            //    {
            //        //if that difference is greater than 1 nano second throw an exception
            //        if (diff > Media.Common.Extensions.TimeSpan.TimeSpanExtensions.NanosecondsPerTick) throw new System.Exception("TotalNanoseconds");
            //        else System.Console.WriteLine("μs to ns conversion Different By: " + diff);
            //    }

            //    System.Console.WriteLine(string.Format("Name{0}, Type: {1}, Speed: {2}, GapMicros:{3} GapNanos{4}, MicroTime:{5}", "N/A", "N/A", speedInBitsPerSecond, gapMicros, gapNanos, microTime));

            //    System.Console.WriteLine("Speed In MBytes Per Second: " + speedInBitsPerSecond / Common.Extensions.TimeSpan.TimeSpanExtensions.NanosecondsPerMillisecond);
            //}
        }
    }
}