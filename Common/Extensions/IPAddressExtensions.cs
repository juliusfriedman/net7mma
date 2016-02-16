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

        private static System.Net.IPAddress emptyIpv6 = emptyIpv4.MapToIPv6();
        private static System.Net.IPAddress intranetMask1v6 = intranetMask1v4.MapToIPv6();
        private static System.Net.IPAddress intranetMask2v6 = intranetMask2v4.MapToIPv6();
        private static System.Net.IPAddress intranetMask3v6 = intranetMask3v4.MapToIPv6();
        private static System.Net.IPAddress intranetMask4v6 = intranetMask4v4.MapToIPv6();

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
            if (emptyIpv4.Equals(ipAddress))
            {
                return false;
            }

            bool onIntranet = System.Net.IPAddress.IsLoopback(ipAddress);

            if (false == onIntranet)
            {
                //Handle IPv6 by getting the IPv4 Mapped Address. 
                if (ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                {

                    onIntranet = System.Net.IPAddress.Equals(ipAddress, ipAddress.And(intranetMask1v6)); //10.255.255.255
                    onIntranet = onIntranet || System.Net.IPAddress.Equals(ipAddress, ipAddress.And(intranetMask4v6)); ////192.168.255.255

                    onIntranet = onIntranet || (intranetMask2v4.Equals(ipAddress.And(intranetMask2v6))
                      && System.Net.IPAddress.Equals(ipAddress, ipAddress.And(intranetMask3v6)));
                }
                else if (ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    onIntranet = System.Net.IPAddress.Equals(ipAddress, ipAddress.And(intranetMask1v4)); //10.255.255.255
                    onIntranet = onIntranet || System.Net.IPAddress.Equals(ipAddress, ipAddress.And(intranetMask4v4)); ////192.168.255.255

                    onIntranet = onIntranet || (intranetMask2v4.Equals(ipAddress.And(intranetMask2v4))
                      && System.Net.IPAddress.Equals(ipAddress, ipAddress.And(intranetMask3v4)));
                }
                else throw new System.NotSupportedException("Only InterNetwork and InterNetworkV6 Address Families are supported.");
            }

            return onIntranet;
        }

        public static bool IsMulticast(this System.Net.IPAddress ip)
        {
            //Check for a ipv6 multicast address
            if (ip.IsIPv6Multicast) return true;

            //Check if mapped to v6 from v4 and unmap
            if (ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();

            byte highIP = ip.GetAddressBytes()[0];

            if (highIP < 224 || highIP > 239)
            {
                return false;
            }

            //Is a multicast address
            return true;
        }


#if __IOS__ || __WATCHOS__ || __TVOS__ || __ANDROID__ || __ANDROID_11__
        public static System.Net.IPAddress MapToIPv6(this System.Net.IPAddress addr)
        {
            if (addr.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork) System.Console.WriteLine("Must pass an IPv4 address to MapToIPv6");
            
            return System.Net.IPAddress.Parse("::ffff:" + addr.ToString());
        }
#endif

    }
}
