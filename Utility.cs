using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Media
{
    /// <summary>
    /// Contains common functions
    /// </summary>
    public static class Utility
    {

        #region Extensions

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

        ///// <summary>
        ///// Converts a String to a Byte[]
        ///// </summary>
        ///// <param name="str">String in the format FF00AABB</param>
        ///// <returns>The corresponding byte array</returns>
        //internal static byte[] GetBytes(string str)
        //{
        //    List<byte> result = new List<byte>();
        //    for (int i = 0, e = str.Length; i < e; i += 2)
        //    {
        //        result.Add(byte.Parse(str.Substring(i, 2), System.Globalization.NumberStyles.HexNumber));
        //    }
        //    return result.ToArray();
        //}

        /// <summary>
        /// About 10 milliseconds faster then Managed when doing it 100,000 times. otherwise no change
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        internal unsafe static byte[] GetBytesUnsafe(string str)
        {
            List<byte> result = new List<byte>();
            //So we don't have to substring get a pointer
            fixed (char* pChar = str)
            {
                //Iterate the pointer using the managed length ....
                for (int i = 0, e = str.Length; i < e; i += 2)
                {
                    //Add a byte which is parsed from the string representation of the char* 2 chars long from the current index
                    result.Add(byte.Parse(new String(pChar, i, 2), System.Globalization.NumberStyles.HexNumber));
                }
            }
            //Return the bytes
            return result.ToArray();
        }

        static internal int FindOpenPort(ProtocolType type, int start = 30000, bool even = true)
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
                if (ep.Port == port + 1 || port == ep.Port)
                    port++;

            //If we only want even ports and we found an even one return it
            if (even && port % 2 == 0) return port;

            //We found an even and we wanted odd
            return ++port;
        }

        /// <summary>
        /// Determine the computers first Ipv4 Address 
        /// </summary>
        /// <returns>The First IPV4 Address Found on the Machine</returns>
        static internal IPAddress GetV4IPAddress()
        {
            return GetFirstIPAddress(System.Net.Sockets.AddressFamily.InterNetwork);
        }

        static internal IPAddress GetFirstIPAddress(System.Net.Sockets.AddressFamily addressFamily)
        {
            foreach (System.Net.IPAddress ip in System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName()).AddressList)
                if (ip.AddressFamily == addressFamily) return ip;
            return IPAddress.Loopback;
        }

        internal static ushort ReverseUnsignedShort(ushort source) { return (ushort)(((source & 0xFF) << 8) | ((source >> 8) & 0xFF)); }

        public static uint ReverseUnsignedInt(uint source) { return (uint)((((source & 0x000000FF) << 24) | ((source & 0x0000FF00) << 8) | ((source & 0x00FF0000) >> 8) | ((source & 0xFF000000) >> 24))); }

        #region Npt

        /// <summary>
        /// Converts specified DateTime value to short NTP time. Note: NTP time is in UTC.
        /// </summary>
        /// <param name="value">DateTime value to convert. This value must be in local time.</param>
        /// <returns>Returns NTP value.</returns>
        public static uint DateTimeToNtpTimestamp32(DateTime value)
        {
            /*
                In some fields where a more compact representation is
                appropriate, only the middle 32 bits are used; that is, the low 16
                bits of the integer part and the high 16 bits of the fractional part.
                The high 16 bits of the integer part must be determined
                independently.
            */

            return (uint)((DateTimeToNtpTimestamp(value) >> 16) & 0xFFFFFFFF);
        }

        /// <summary>
        /// Converts specified DateTime value to long NTP time. Note: NTP time is in UTC.
        /// </summary>
        /// <param name="value">DateTime value to convert. This value must be in local time.</param>
        /// <returns>Returns NTP value.</returns>
        public static ulong DateTimeToNtpTimestamp(DateTime value)
        {
            /*
                Wallclock time (absolute date and time) is represented using the
                timestamp format of the Network Time Protocol (NTP), which is in
                seconds relative to 0h UTC on 1 January 1900 [4].  The full
                resolution NTP timestamp is a 64-bit unsigned fixed-point number with
                the integer part in the first 32 bits and the fractional part in the
                last 32 bits. In some fields where a more compact representation is
                appropriate, only the middle 32 bits are used; that is, the low 16
                bits of the integer part and the high 16 bits of the fractional part.
                The high 16 bits of the integer part must be determined
                independently.
            */

            DateTime baseDate;

            if (value >= UtcEpoch2036) baseDate = UtcEpoch2036;
            else baseDate = UtcEpoch1900;

            TimeSpan ts = ((TimeSpan)(value.ToUniversalTime() - baseDate.ToUniversalTime()));

            return ((ulong)(ts.Ticks / TimeSpan.TicksPerSecond) << 32) | (uint)(ts.Ticks / TimeSpan.TicksPerSecond * 0x100000000L);
        }

        public static DateTime NptTimestampToDateTime(ulong ntpTimestamp)
        {
            return NptTimestampToDateTime((uint)((ntpTimestamp >> 32) & 0xFFFFFFFF), (uint)(ntpTimestamp & 0xFFFFFFFF));
        }

        public static DateTime NptTimestampToDateTime(uint seconds, uint fractions)
        {
            ulong ticks =(ulong)((seconds * TimeSpan.TicksPerSecond) + ((fractions * TimeSpan.TicksPerSecond) / 0x100000000L));
            if ((seconds & 0x80000000L) == 0)
            {
                return UtcEpoch2036 + TimeSpan.FromTicks((Int64)ticks);
            }
            else
            {
                return UtcEpoch1900 + TimeSpan.FromTicks((Int64)ticks);
            }
        }

        public static DateTime UtcEpoch2036 = new DateTime(2036, 2, 7, 6, 28, 16, DateTimeKind.Utc);

        public static DateTime UtcEpoch1900 = new DateTime(1900, 1, 1, 1, 0, 0, DateTimeKind.Utc);

        public static DateTime UtcEpoch1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        #endregion

    }
}
