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

#region Using Statements

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using Media.Common;

#endregion

namespace Media
{
    /// <summary>
    /// Contains common functions
    /// </summary>
    [CLSCompliant(false)]
    public static class Utility
    {

        //http://en.wikipedia.org/wiki/Interframe_gap

        internal const int InterframeGapBits = 96;

        const int LinkSpeed = 10000; //MB

        //Get Interface Link Speed

        internal const double MicrosecondsPerMillisecond = 1000;

        internal const double InterframeSpacing = InterframeGapBits / LinkSpeed; //µs

        public static byte[] Empty = new byte[0];

        //Build interface table with speeds detected...

        //For raw sockets, must generate your own headers when outgoing, you can copy the incoming header though and modify as required :)
        const int TCP_HEADER = 20; //+
        const int UDP_HEADER = 14; //+
        const int IP_HEADER = 10; //

        #region Extensions
        
        public static IEnumerable<T> Yield<T>(this T t) { yield return t;}

        public static double TotalMicroseconds(this TimeSpan ts) { return ts.TotalMilliseconds / MicrosecondsPerMillisecond; }

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

        public static System.Security.Cryptography.MD5 MD5HashAlgorithm { get { return System.Security.Cryptography.MD5.Create(); } }

        public static Random Random = new Random();

        #endregion

        #region Hex Functions

        public static byte HexCharToByte(char c) { c = char.ToUpperInvariant(c); return (byte)(c > '9' ? c - 'A' + 10 : c - '0'); }

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
        public static byte[] HexStringToBytes(string str, int start = 0, int length = -1)
        {
            if (length == 0) return null;
            if (length <= -1) length = str.Length;
            if (start > length - start) throw new ArgumentOutOfRangeException("start");
            if (length > length - start) throw new ArgumentOutOfRangeException("length");
            List<byte> result = new List<byte>();
            //Dont check the results for overflow
            unchecked
            {
                //Iterate the pointer using the managed length ....
                for (int i = start, e = length; i < e; i += 2)
                {
                    //to reduce string manipulations pre call
                    //while (str[i] == '-') i++;

                    //Conver 2 Chars to a byte
                    result.Add((byte)(HexCharToByte(str[i]) << 4 | HexCharToByte(str[i + 1])));
                }
            }
            //Return the bytes
            return result.ToArray();
        }

        #endregion

        const int SIO_UDP_CONNRESET = -1744830452;

        public static void DontLinger(this Socket socket) { socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true); }

        public static void Linger(this Socket socket) { socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, false); }

        #region Static Helper Functions

        public static void Abort(ref System.Threading.Thread thread, System.Threading.ThreadState state = System.Threading.ThreadState.Running, int timeout = 1000)
        {
            //If the worker is running
            if (thread != null && thread.ThreadState.HasFlag(state))
            {
                //Attempt to join
                if (!thread.Join(timeout))
                {
                    try
                    {
                        //Abort
                        thread.Abort();
                    }
                    catch { return; } //Cancellation not supported
                }

                //Reset the state of the thread to indicate success
                thread = null;
            }
        }

        public static int IndexOfAny<T>(this T[] array, params T[] delemits)
        {
            int o = -1, //Offset
                i = -1, //Index
                e = delemits.Length;//End
            //Set i = the index of the entry of the delemit
            do
                i = Array.IndexOf(array, delemits[++o]);
            while(i <= 0 && o < e);//While the delemit was not found and the offset is less then the end

            //Return the index of the last delemit
            return i;
        }

        /// <summary>
        /// Checks the first two bits and the last two bits of each byte while moving the count to the correct position while doing so.
        /// The function does not check array bounds or preserve the stack and prevents math overflow.
        /// </summary>
        /// <param name="buffer">The array to check</param>
        /// <param name="start">The offset to start checking</param>
        /// <param name="count">The amount of bytes in the buffer</param>
        /// <param name="reverse">optionally indicates if the bytes being checked should be reversed before being checked</param>
        /// <returns></returns>
        /// <remarks>If knew the width did you, faster it could be..</remarks>
        public static bool FoundValidUniversalTextFormat(byte[] buffer, ref int start, ref int count, bool reverse = false)
        {
            unchecked //unaligned
            {
                //1100001 1
                while (((reverse ? (Common.Binary.ReverseU8(buffer[start])) : buffer[start]) & 0xC3) == 0 && start < --count) ++start;
                return count > 0;
            }
        }

        /// <summary>
        /// Indicates the position of the match in a given buffer to a given set of octets.
        /// If the match fails the start parameter will reflect the position of the last partial match, otherwise it will be incremented by <paramref name="octetCount"/>
        /// Additionally start and count will reflect the position of the last partially matched byte, E.g. if 1 octets were match start was incremented by 1.
        /// </summary>
        /// <param name="buffer">The bytes to search</param>
        /// <param name="start">The 0 based index to to start the forward based search</param>
        /// <param name="count">The amount of bytes to search in the buffer</param>
        /// <param name="octets">The bytes to search for</param>
        /// <param name="octetStart">The 0 based offset in the octets to search from</param>
        /// <param name="octetCount">The amount of octets required for a successful match</param>
        /// <returns>
        /// -1 if the match failed or could not be performed; otherwise,
        /// the position within the buffer reletive to the start position in which the first occurance of octets given the octetStart and octetCount was matched.
        /// If more than 1 octet is required for a match and the buffer does not encapsulate the entire match start will still reflect the occurance of the partial match.
        /// </returns>
        public static int ContainsBytes(this byte[] buffer, ref int start, ref int count, byte[] octets, int octetStart, int octetCount)
        {
            //If the buffer or the octets are null no dice
            if (buffer == null || octets == null) return -1;

            //Cache the length
            int bufferLength = buffer.Length;

            //Maybe in reverse, undefined...
            if (count < start) count = bufferLength - start;

            //Make sure there is no way to run out of bounds given correct input
            if (bufferLength < octetCount || start + count > bufferLength) return -1;

            //Nothing to search nothing to return, leave start where it was.
            if (octetCount == 0 && bufferLength == 0) return -1;

            //Create the variables we will use in the searching process
            int checkedBytes = 0, lastPosition = -1;

            //Attempt to match
            try
            {
                //Loop the buffer from start to count
                while (start < count && checkedBytes < octetCount)
                {

                    int position = start + checkedBytes;

                    //Find the next occurance of the required octet storing the result in lastPosition reducing the amount of places to search each time
                    if ((lastPosition = Array.IndexOf<byte>(buffer, octets[checkedBytes], position,  count - position)) >= start)
                    {
                        //Check for completion
                        if (++checkedBytes == octetCount) break;
                        
                        //Partial match only
                        start = lastPosition;
                    }
                    else
                    {
                        //The match failed at the current offset
                        checkedBytes = 0;

                        //Move the position
                        start++;

                        //Decrease the amount which remains
                        count--;
                    }
                }

                //start now reflects the position after a parse occurs

                //Return the last position of the partial match
                return lastPosition;
            }
            catch { throw; }
        }

        public static int Find(this byte[] array, byte[] needle, int startIndex, int sourceLength )
        {
            int needleLen = needle.Length;
            int index;

            while ( sourceLength >= needleLen )
            {
                // find needle's starting element
                index = Array.IndexOf( array, needle[0], startIndex, sourceLength - needleLen + 1 );

                // if we did not find even the first element of the needls, then the search is failed
                if ( index == -1 )
                    return -1;

                int i, p;
                // check for needle
                for ( i = 0, p = index; i < needleLen; i++, p++ )
                {
                    if ( array[p] != needle[i] )
                    {
                        break;
                    }
                }

                if ( i == needleLen )
                {
                    // needle was found
                    return index;
                }

                // continue to search for needle
                sourceLength -= ( index - startIndex + 1 );
                startIndex = index + 1;
            }
            return -1;
        }

        /// <summary>
        /// Receives the given amount of bytes into the buffer given a offset and an amount.
        /// </summary>
        /// <param name="buffer">The array to receive into</param>
        /// <param name="offset">The location to receive into</param>
        /// <param name="amount">The 0 based amount of bytes to receive, 0 will have no result</param>
        /// <param name="socket">The socket to receive on</param>
        /// <returns>The amount of bytes recieved which will be equal to the amount paramter unless the data was unable to fit in the given buffer</returns>
        public static int AlignedReceive(byte[] buffer, int offset, int amount, Socket socket, out SocketError error)
        {
            //Store any socket errors here incase non-blocking sockets are being used.
            error = SocketError.SocketError;

            //Return the amount if its negitive;
            if (amount <= 0) return amount;
            try
            {
                //To hold what was received and the maximum amount to receive
                int totalReceived = 0, max = buffer.Length - offset, attempt = 0;

                if(amount > max) amount = max;

                //While there is something to receive
                while (amount > 0 /* && offset <= max*/)
                {
                    lock (socket)
                    {
                        //Receive it into the buffer at the given offset taking into account what was already received
                        int justReceived = socket.Receive(buffer, offset, amount, SocketFlags.None, out error);

                        //decrease the amount by what was received
                        amount -= justReceived;
                        //Increase the offset by what was received
                        offset += justReceived;
                        //Increase total received
                        totalReceived += justReceived;
                        //If nothing was received
                        if (justReceived == 0)
                        {
                            //error = SocketError.TimedOut;
                            //Try again maybe
                            ++attempt;
                            //Only if the attempts in operations were greater then the amount of bytes requried
                            if (attempt > amount) error = SocketError.TimedOut;
                        }

                        //Break on any error besides WouldBlock, Could use Poll here
                        if (error == SocketError.ConnectionAborted || error == SocketError.TimedOut || error == SocketError.ConnectionReset)
                        {
                            //Set the total to the amount given because something bad happened
                            return totalReceived;
                        }
                        else if (offset > max) break;  
                    }
                }

                //Return the result
                return totalReceived;
            }
            catch { throw; }
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

        #region Npt

        //Should all be DateTimeOffset

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
        public static uint DateTimeToNptTimestamp32(DateTime value) { return (uint)((DateTimeToNptTimestamp(value) << 16) & 0xFFFFFFFF); }

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

            return ((ulong)(elapsedTime.Ticks / TimeSpan.TicksPerSecond) << 32) | (uint)(elapsedTime.Ticks / MicrosecondsPerMillisecond);
        }

        public static DateTime NptTimestampToDateTime(ulong nptTimestamp) { return NptTimestampToDateTime((uint)((nptTimestamp >> 32) & 0xFFFFFFFF), (uint)(nptTimestamp & 0xFFFFFFFF)); }

        public static DateTime NptTimestampToDateTime(uint seconds, uint fractions, DateTime? epoch = null)
        {
            ulong ticks =(ulong)((seconds * TimeSpan.TicksPerSecond) + ((fractions * TimeSpan.TicksPerSecond) / 0x100000000L));
            if (epoch.HasValue) return epoch.Value + TimeSpan.FromTicks((Int64)ticks);
            return (seconds & 0x80000000L) == 0 ? UtcEpoch2036 + TimeSpan.FromTicks((Int64)ticks) : UtcEpoch1900 + TimeSpan.FromTicks((Int64)ticks);
        }

        //When the First Epoch will wrap (The real Y2k)
        public static DateTime UtcEpoch2036 = new DateTime(2036, 2, 7, 6, 28, 16, DateTimeKind.Utc);

        public static DateTime UtcEpoch1900 = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static DateTime UtcEpoch1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        #endregion        
    }
}
