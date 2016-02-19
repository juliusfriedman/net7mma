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

namespace Media.Common.Extensions.IPAddress
{
    public static class IPAddressExtensions
    {
        private static void CheckIPVersion(System.Net.IPAddress ipAddress, System.Net.IPAddress mask, out byte[] addressBytes, out byte[] maskBytes)
        {
            if (ipAddress == null) throw new System.ArgumentNullException("ipAddress");
        
            if (mask == null) throw new System.ArgumentNullException("mask");

            addressBytes = ipAddress.GetAddressBytes();
            maskBytes = mask.GetAddressBytes();

            if (addressBytes.Length != maskBytes.Length)
            {
                throw new System.ArgumentException("The address and mask don't use the same IP standard");
            }
        }

        public static System.Net.IPAddress And(this System.Net.IPAddress ipAddress, System.Net.IPAddress mask)
        {
            byte[] addressBytes;
            byte[] maskBytes;

            CheckIPVersion(ipAddress, mask, out addressBytes, out maskBytes);

            byte[] resultBytes = new byte[addressBytes.Length];

            for (int i = 0, e = addressBytes.Length; i < e; ++i)
            {
                resultBytes[i] = (byte)(addressBytes[i] & maskBytes[i]);
            }

            return new System.Net.IPAddress(resultBytes);
        }

        private static System.Net.IPAddress emptyIpv4 = System.Net.IPAddress.Parse("0.0.0.0");
        private static System.Net.IPAddress intranetMask1v4 = System.Net.IPAddress.Parse("10.255.255.255");
        private static System.Net.IPAddress intranetMask2v4 = System.Net.IPAddress.Parse("172.16.0.0");
        private static System.Net.IPAddress intranetMask3v4 = System.Net.IPAddress.Parse("172.31.255.255");
        private static System.Net.IPAddress intranetMask4v4 = System.Net.IPAddress.Parse("192.168.255.255");

        //Should check if ipV6 is even supported before defining them.
        //Shoul be null and then in Static constructor should check =>
        //System.Net.Sockets.Socket.OSSupportsIPv6 or try GetIPv6Properties().Index > -999 from the networkInterface...

        private static System.Net.IPAddress emptyIpv6 = System.Net.IPAddress.IPv6Any;
        private static System.Net.IPAddress intranetMask1v6 = System.Net.IPAddress.Parse("::ffff:10.255.255.255");
        private static System.Net.IPAddress intranetMask2v6 = System.Net.IPAddress.Parse("::ffff:172.16.0.0");
        private static System.Net.IPAddress intranetMask3v6 = System.Net.IPAddress.Parse("::ffff:172.31.255.255");
        private static System.Net.IPAddress intranetMask4v6 = System.Net.IPAddress.Parse("::ffff:192.168.255.255");

        /// <summary>
        /// Retuns true if the ip address is one of the following
        /// IANA-reserved private IPv4 network ranges (from http://en.wikipedia.org/wiki/IP_address)
        ///  Start 	      End 	
        ///  10.0.0.0 	    10.255.255.255 	
        ///  172.16.0.0 	  172.31.255.255 	
        ///  192.168.0.0   192.168.255.255 
        /// </summary>
        /// <returns></returns>
        public static bool IsOnIntranet(this System.Net.IPAddress ipAddress) //Nat
        {            
            bool onIntranet = System.Net.IPAddress.IsLoopback(ipAddress);

            if (false == onIntranet)
            {
                switch (ipAddress.AddressFamily)
                {
                    case System.Net.Sockets.AddressFamily.InterNetwork:
                        {
                            if (emptyIpv4.Equals(ipAddress))
                            {
                                return false;
                            }

                            onIntranet = System.Net.IPAddress.Equals(ipAddress, ipAddress.And(intranetMask1v4)); //10.255.255.255
                            onIntranet = onIntranet || System.Net.IPAddress.Equals(ipAddress, ipAddress.And(intranetMask4v4)); ////192.168.255.255

                            return onIntranet = onIntranet || (intranetMask2v4.Equals(ipAddress.And(intranetMask2v4))
                              && System.Net.IPAddress.Equals(ipAddress, ipAddress.And(intranetMask3v4)));
                        }
                    case System.Net.Sockets.AddressFamily.InterNetworkV6:
                        {
                            if (emptyIpv6.Equals(ipAddress))
                            {
                                return false;
                            }

                            onIntranet = System.Net.IPAddress.Equals(ipAddress, ipAddress.And(intranetMask1v6)); //10.255.255.255
                            onIntranet = onIntranet || System.Net.IPAddress.Equals(ipAddress, ipAddress.And(intranetMask4v6)); ////192.168.255.255

                            return onIntranet = onIntranet || (intranetMask2v6.Equals(ipAddress.And(intranetMask2v6))
                              && System.Net.IPAddress.Equals(ipAddress, ipAddress.And(intranetMask3v6)));
                        }
                    default: throw new System.NotSupportedException("Only InterNetwork and InterNetworkV6 Address Families are supported.");
                }
            }

            return onIntranet;
        }

        public static bool IsMulticast(this System.Net.IPAddress ipAddress)
        {
            if (ipAddress == null) return false;

            switch (ipAddress.AddressFamily)
            {
                case System.Net.Sockets.AddressFamily.InterNetwork:
                    {
                        byte highIP = ipAddress.GetAddressBytes()[0];

                        if (highIP < 224 || highIP > 239)
                        {
                            return false;
                        }

                        //Is a multicast address
                        return true;
                    }
                case System.Net.Sockets.AddressFamily.InterNetworkV6:
                    {
                        //Check for a ipv6 multicast address
                        if (ipAddress.IsIPv6Multicast) return true;

                        //Check if mapped to v6 from v4 and unmap
                        if (ipAddress.IsIPv4MappedToIPv6)
                        {
                            ipAddress = ipAddress.MapToIPv4();

                            //handle as v4
                            goto case System.Net.Sockets.AddressFamily.InterNetwork;
                        }

                        return false;
                    }
                default: return false;
            }
        }
    }
}
