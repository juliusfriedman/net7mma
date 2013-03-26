using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace Media
{
    /// <summary>
    /// Contains common functions
    /// </summary>
    public static class Utility
    {
        #region Extensions

        private static void CheckIPVersion(IPAddress ipAddress, IPAddress mask, out byte[] addressBytes, out byte[] maskBytes)
        {
            if (mask == null)
            {
                throw new ArgumentException();
            }

            addressBytes = ipAddress.GetAddressBytes();
            maskBytes = mask.GetAddressBytes();

            if (addressBytes.Length != maskBytes.Length)
            {
                throw new ArgumentException("The address and mask don't use the same IP standard");
            }
        }

        public static IPAddress And(this IPAddress ipAddress, IPAddress mask)
        {
            byte[] addressBytes;
            byte[] maskBytes;
            CheckIPVersion(ipAddress, mask, out addressBytes, out maskBytes);

            byte[] resultBytes = new byte[addressBytes.Length];
            for (int i = 0; i < addressBytes.Length; ++i)
            {
                resultBytes[i] = (byte)(addressBytes[i] & maskBytes[i]);
            }

            return new IPAddress(resultBytes);
        }

        private static IPAddress empty = IPAddress.Parse("0.0.0.0");
        private static IPAddress intranetMask1 = IPAddress.Parse("10.255.255.255");
        private static IPAddress intranetMask2 = IPAddress.Parse("172.16.0.0");
        private static IPAddress intranetMask3 = IPAddress.Parse("172.31.255.255");
        private static IPAddress intranetMask4 = IPAddress.Parse("192.168.255.255");
        
        /// <summary>
        /// Retuns true if the ip address is one of the following
        /// IANA-reserved private IPv4 network ranges (from http://en.wikipedia.org/wiki/IP_address)
        ///  Start 	      End 	
        ///  10.0.0.0 	    10.255.255.255 	
        ///  172.16.0.0 	  172.31.255.255 	
        ///  192.168.0.0   192.168.255.255 
        /// </summary>
        /// <returns></returns>
        public static bool IsOnIntranet(this IPAddress ipAddress)
        {
            if (empty.Equals(ipAddress))
            {
                return false;
            }
            bool onIntranet = IPAddress.IsLoopback(ipAddress);
            onIntranet = onIntranet || ipAddress.Equals(ipAddress.And(intranetMask1)); //10.255.255.255
            onIntranet = onIntranet || ipAddress.Equals(ipAddress.And(intranetMask4)); ////192.168.255.255

            onIntranet = onIntranet || (intranetMask2.Equals(ipAddress.And(intranetMask2))
              && ipAddress.Equals(ipAddress.And(intranetMask3)));

            return onIntranet;
        }

        public static void AddRange<T>(this List<T> list, IEnumerable<T> source, int start, int length)
        {
            if (list == null) throw new ArgumentNullException("list");
            if (source == null) throw new ArgumentNullException("source");
            int count = source.Count<T>();
            if (start > count || start < 0) throw new ArgumentOutOfRangeException("start");
            if (length - start > count) throw new ArgumentOutOfRangeException("length");
            list.AddRange(source.Skip(start).Take(length));
        }

        #endregion

        #region Properties

        public static System.Security.Cryptography.MD5 MD5HashAlgorithm = System.Security.Cryptography.MD5.Create();

        public static Random Random = new Random();

        #endregion

        #region Hex Functions

        public static byte HexCharToByte(char c) { /*c = char.ToUpperInvariant(c);*/ return (byte)(c > '9' ? c - 'A' + 10 : c - '0'); }

        /// <summary>
        /// Converts a String in the form 0011AABB to a Byte[] using the chars in the string as bytes to caulcate the decimal value.
        /// Lower case values are not supported and no error checking is performed.
        /// </summary>
        /// <notes>
        /// Reduced string allocations from managed version substring
        /// About 10 milliseconds faster then Managed when doing it 100,000 times. otherwise no change
        /// </notes>
        /// <param name="str"></param>
        /// <returns></returns>
        public unsafe static byte[] HexStringToBytes(string str, int start = 0, int length = -1)
        {
            if (length == 0) return null;
            if (length <= -1) length = str.Length;
            if (start > length - start) throw new ArgumentOutOfRangeException("start");
            if (length > length - start) throw new ArgumentOutOfRangeException("length");
            List<byte> result = new List<byte>();
            //Dont check the results for overflow
            unchecked
            {
                //So we don't have to substring get a pointer to the first char
                fixed (char* pChar = str)
                {
                    //Iterate the pointer using the managed length ....
                    for (int i = start, e = length; i < e; i += 2)
                    {
                        //(maybe check for if(pChar[i] == '-' ) ++i to reduce string manipulations pre call)

                        //Add a byte which is parsed from the string representation of the char* 2 chars long from the current index
                        //result.Add(byte.Parse(new String(pChar, i, 2), System.Globalization.NumberStyles.HexNumber));
                        //Conver 2 Chars to a byte
                        result.Add((byte)(HexCharToByte(pChar[i]) << 4 | HexCharToByte(pChar[i + 1])));
                    }
                }
            }
            //Return the bytes
            return result.ToArray();
        }

        #endregion

        #region Port and IPAddress Functions

        public static int FindOpenPort(ProtocolType type, int start = 30000, bool even = true)
        {
            //Only Tcp or Udp :)
            if (type != ProtocolType.Udp && type != ProtocolType.Tcp) return -1;
            
            int port = start;

            //Get the IpGlobalProperties
            System.Net.NetworkInformation.IPGlobalProperties ipGlobalProperties = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties();

            //Can't get any information
            if(ipGlobalProperties == null) return port = -1;

            //We need endpoints to ensure the ports we want are not in use
            IEnumerable<IPEndPoint> listeners = null;

            //Get the endpoints
            if (type == ProtocolType.Udp) listeners = ipGlobalProperties.GetActiveUdpListeners();
            else if (type == ProtocolType.Tcp) listeners = ipGlobalProperties.GetActiveTcpListeners();            

            //Enumerate the ones that are = or > then port and increase port along the way
            foreach (IPEndPoint ep in listeners.Where(ep => ep.Port >= port))
            {
                if (port == ep.Port) port++;
                else if (ep.Port == port + 1) port += 2;
            }

            int remainder = port % 2;

            //If we only want even ports and we found an even one return it
            if (even && remainder == 0 || !even && remainder != 0) return port;

            //We found an even and we wanted odd or vice versa
            return ++port;
        }

        /// <summary>
        /// Determine the computers first Ipv4 Address 
        /// </summary>
        /// <returns>The First IPV4 Address Found on the Machine</returns>
        public static IPAddress GetFirstV4IPAddress() { return GetFirstIPAddress(System.Net.Sockets.AddressFamily.InterNetwork); }

        public static IPAddress GetFirstIPAddress(System.Net.Sockets.AddressFamily addressFamily) { foreach (System.Net.IPAddress ip in System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName()).AddressList) if (ip.AddressFamily == addressFamily) return ip; return IPAddress.Loopback; }

        #endregion

        #region Bit Manipulation

        public static ushort ReverseUnsignedShort(ushort source) { return (ushort)(((source & 0xFF) << 8) | ((source >> 8) & 0xFF)); }

        public static uint ReverseUnsignedInt(uint source) { return (uint)((((source & 0x000000FF) << 24) | ((source & 0x0000FF00) << 8) | ((source & 0x00FF0000) >> 8) | ((source & 0xFF000000) >> 24))); }

        //Could perform logic in Reverse Functions and rename them ToBigEndian then make seperate explicit Function for Reverse for ease of refactoring usage.
        //public static void ToBigEndian(Array array) { if (!BitConverter.IsLittleEndian) return; Array.Reverse(array); }

        #endregion

        #region Npt

        /// <summary>
        /// Converts specified DateTime value to short NPT time.
        /// </summary>
        /// <param name="value">DateTime value to convert.</param>
        /// <returns>Returns NPT value.</returns>
        /// <notes>
        /// In some fields where a more compact representation is
        /// appropriate, only the middle 32 bits are used; that is, the low 16
        /// bits of the integer part and the high 16 bits of the fractional part.
        /// The high 16 bits of the integer part must be determined independently.
        /// </notes>
        public static uint DateTimeToNptTimestamp32(DateTime value) { return (uint)((DateTimeToNptTimestamp(value) >> 16) & 0xFFFFFFFF); }

        /// <summary>
        /// Converts specified DateTime value to long NPT time.
        /// </summary>
        /// <param name="value">DateTime value to convert. This value must be in local time.</param>
        /// <returns>Returns NPT value.</returns>
        /// <notes>
        /// Wallclock time (absolute date and time) is represented using the
        /// timestamp format of the Network Time Protocol (NPT), which is in
        /// seconds relative to 0h UTC on 1 January 1900 [4].  The full
        /// resolution NPT timestamp is a 64-bit unsigned fixed-point number with
        /// the integer part in the first 32 bits and the fractional part in the
        /// last 32 bits. In some fields where a more compact representation is
        /// appropriate, only the middle 32 bits are used; that is, the low 16
        /// bits of the integer part and the high 16 bits of the fractional part.
        /// The high 16 bits of the integer part must be determined independently.
        /// </notes>
        public static ulong DateTimeToNptTimestamp(DateTime value)
        {
            DateTime baseDate = value >= UtcEpoch2036 ? UtcEpoch2036 : UtcEpoch1900;
            
            TimeSpan elapsedTime = value > baseDate ? value.ToUniversalTime() - baseDate.ToUniversalTime() : baseDate.ToUniversalTime() - value.ToUniversalTime();

            return ((ulong)(elapsedTime.Ticks / TimeSpan.TicksPerSecond) << 32) | (uint)(elapsedTime.Ticks / TimeSpan.TicksPerSecond * 0x100000000L);
        }

        public static DateTime NptTimestampToDateTime(ulong nptTimestamp) { return NptTimestampToDateTime((uint)((nptTimestamp >> 32) & 0xFFFFFFFF), (uint)(nptTimestamp & 0xFFFFFFFF)); }

        public static DateTime NptTimestampToDateTime(uint seconds, uint fractions)
        {
            ulong ticks =(ulong)((seconds * TimeSpan.TicksPerSecond) + ((fractions * TimeSpan.TicksPerSecond) / 0x100000000L));
            return (seconds & 0x80000000L) == 0 ? UtcEpoch2036 + TimeSpan.FromTicks((Int64)ticks) : UtcEpoch1900 + TimeSpan.FromTicks((Int64)ticks);
        }

        //When the First Epoch will wrap (The real Y2k)
        public static DateTime UtcEpoch2036 = new DateTime(2036, 2, 7, 6, 28, 16, DateTimeKind.Utc);

        public static DateTime UtcEpoch1900 = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static DateTime UtcEpoch1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        #endregion        
    }
}
