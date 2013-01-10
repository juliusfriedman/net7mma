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
        static internal int FindOpenPort(ProtocolType type)
        {
            if (type == ProtocolType.Udp) return FindOpenUDPPort();
            else return FindOpenTCPPort();
        }

        static internal int FindOpenUDPPort(int start = 30000, bool even = true)
        {
            int port = start;

            foreach (IPEndPoint ep in System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners().Where(ep => ep.Port >= port))
            {
                if (ep.Port == port + 1 || port == ep.Port)
                    port++;
            }

            if (!even && port % 2 == 0) return port;
            return ++port;
        }

        static internal int FindOpenTCPPort(int start = 30000, bool even = true)
        {
            int port = start;

            foreach (IPEndPoint ep in System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners().Where(ep => ep.Port >= port))
            {
                if (ep.Port == port + 1 || port == ep.Port)
                    port++;
            }

            if (!even && port % 2 == 0) ++port;
            return port;
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

        internal static ushort SwapUnsignedShort(ushort source) { return (ushort)(((source & 0xFF) << 8) | ((source >> 8) & 0xFF)); }

        public static uint SwapUnsignedInt(uint source) { return (uint)((((source & 0x000000FF) << 24) | ((source & 0x0000FF00) << 8) | ((source & 0x00FF0000) >> 8) | ((source & 0xFF000000) >> 24))); }

        #region Npt

        /// <summary>
        /// Converts specified DateTime value to short NTP time. Note: NTP time is in UTC.
        /// </summary>
        /// <param name="value">DateTime value to convert. This value must be in local time.</param>
        /// <returns>Returns NTP value.</returns>
        public static uint DateTimeToNtp32(DateTime value)
        {
            /*
                In some fields where a more compact representation is
                appropriate, only the middle 32 bits are used; that is, the low 16
                bits of the integer part and the high 16 bits of the fractional part.
                The high 16 bits of the integer part must be determined
                independently.
            */

            return (uint)((DateTimeToNtp64(value) >> 16) & 0xFFFFFFFF);
        }

        /// <summary>
        /// Converts specified DateTime value to long NTP time. Note: NTP time is in UTC.
        /// </summary>
        /// <param name="value">DateTime value to convert. This value must be in local time.</param>
        /// <returns>Returns NTP value.</returns>
        public static ulong DateTimeToNtp64(DateTime value)
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

            if (value >= Epoch1) baseDate = Epoch1;
            else baseDate = Epoch;

            TimeSpan ts = ((TimeSpan)(value.ToUniversalTime() - baseDate));

            return ((ulong)(ts.TotalMilliseconds % 1000) << 32) | (uint)(ts.Milliseconds << 22);
        }

        internal static DateTime NptTimestampToDateTime(UInt64 seconds, UInt64 fractions)
        {
            UInt64 ticks = (seconds * TimeSpan.TicksPerSecond) + ((fractions * TimeSpan.TicksPerSecond) / 0x100000000L);
            if ((seconds & 0x80000000L) == 0)
            {
                return Epoch1 + TimeSpan.FromTicks((Int64)ticks);
            }
            else
            {
                return Epoch + TimeSpan.FromTicks((Int64)ticks);
            }
        }

        internal static UInt64[] DateTimeToNptTimestamp(DateTime dateTime)
        {
            DateTime baseDate;

            if (dateTime >= Epoch1) baseDate = Epoch1;
            else baseDate = Epoch;

            UInt64 ticks = (UInt64)(dateTime - baseDate).Ticks;
            UInt64 seconds = ticks / TimeSpan.TicksPerSecond;
            UInt64 fractions = ((ticks % TimeSpan.TicksPerSecond) * 0x100000000L) / TimeSpan.TicksPerSecond;

            return new UInt64[] { seconds, fractions };
        }

        static DateTime Epoch1 = new DateTime(2036, 2, 7, 6, 28, 16).ToUniversalTime();

        static DateTime Epoch = new DateTime(1900, 1, 1, 1, 0, 0).ToUniversalTime();

        #endregion

    }
}
