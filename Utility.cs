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
        internal const double MicrosecondsPerMillisecond = 1000, NanosecondsPerMillisecond = MicrosecondsPerMillisecond * MicrosecondsPerMillisecond, NanosecondsPerSecond = 1000000000;

        public static byte[] Empty = new byte[0];

        #region Extensions

        public static IEnumerable<T> Yield<T>(this T t) { yield return t; }

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

        public static bool IsMulticast(this IPAddress ip)
        {
            bool result = true;
            if (!ip.IsIPv6Multicast)
            {
                byte highIP = ip.GetAddressBytes()[0];
                if (highIP < 224 || highIP > 239)
                {
                    result = false;
                }
            }
            return result;
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
            List<byte> result = new List<byte>(length / 2);
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

        //Move to ISocketOwner

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
            while (i <= 0 && o < e);//While the delemit was not found and the offset is less then the end

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
                    if ((lastPosition = Array.IndexOf<byte>(buffer, octets[checkedBytes], position, count - position)) >= start)
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

        public static int Find(this byte[] array, byte[] needle, int startIndex, int sourceLength)
        {
            int needleLen = needle.Length;
            int index;

            while (sourceLength >= needleLen)
            {
                // find needle's starting element
                index = Array.IndexOf(array, needle[0], startIndex, sourceLength - needleLen + 1);

                // if we did not find even the first element of the needls, then the search is failed
                if (index == -1)
                    return -1;

                int i, p;
                // check for needle
                for (i = 0, p = index; i < needleLen; i++, p++)
                {
                    if (array[p] != needle[i])
                    {
                        break;
                    }
                }

                if (i == needleLen)
                {
                    // needle was found
                    return index;
                }

                // continue to search for needle
                sourceLength -= (index - startIndex + 1);
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

                if (amount > max) amount = max;

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
            if (ipGlobalProperties == null) return port = -1;

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
            ulong ticks = (ulong)((seconds * TimeSpan.TicksPerSecond) + ((fractions * TimeSpan.TicksPerSecond) / 0x100000000L));
            if (epoch.HasValue) return epoch.Value + TimeSpan.FromTicks((Int64)ticks);
            return (seconds & 0x80000000L) == 0 ? UtcEpoch2036 + TimeSpan.FromTicks((Int64)ticks) : UtcEpoch1900 + TimeSpan.FromTicks((Int64)ticks);
        }

        //When the First Epoch will wrap (The real Y2k)
        public static DateTime UtcEpoch2036 = new DateTime(2036, 2, 7, 6, 28, 16, DateTimeKind.Utc);

        public static DateTime UtcEpoch1900 = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static DateTime UtcEpoch1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        #endregion

        #region RgbYuv.cs

        /// <summary>
        /// Provides cached RGB to YUV lookup without alpha support.
        /// </summary>
        /// <remarks>
        /// This class is public so a user can manually load and unload the lookup table.
        /// Looking up a color calculates the lookup table if not present.
        /// All methods except UnloadLookupTable should be thread-safe, although there will be a performance overhead if GetYUV is called while Initialize has not finished.
        /// </remarks>
        public static class RgbYuv
        {
            const uint RgbMask = 0x00ffffff;

            private static volatile int[] lookupTable;
            private static int[] LookupTable
            {
                get
                {
                    if (lookupTable == null) Initialize();
                    return lookupTable;
                }
            }

            /// <summary>
            /// Gets whether the lookup table is ready.
            /// </summary>
            public static bool Initialized
            {
                get
                {
                    return lookupTable != null;
                }
            }

            /// <summary>
            /// Returns the 24bit YUV equivalent of the provided 24bit RGB color.
            /// <para>Any alpha component is dropped.</para>
            /// </summary>
            /// <param name="rgb">A 24bit rgb color.</param>
            /// <returns>The corresponding 24bit YUV color.</returns>
            public static int GetYuv(uint rgb)
            {
                return LookupTable[rgb & RgbMask];
            }

            /// <summary>
            /// Calculates the lookup table.
            /// </summary>
            public static unsafe void Initialize()
            {
                var lTable = new int[0x1000000]; // 256 * 256 * 256
                fixed (int* lookupP = lTable)
                {
                    byte* lP = (byte*)lookupP;
                    for (uint i = 0; i < lTable.Length; i++)
                    {
                        float r = (i & 0xff0000) >> 16;
                        float g = (i & 0x00ff00) >> 8;
                        float b = (i & 0x0000ff);

                        lP++; //Skip alpha byte
                        *(lP++) = (byte)(.299 * r + .587 * g + .114 * b);
                        *(lP++) = (byte)((int)(-.169 * r - .331 * g + .5 * b) + 128);
                        *(lP++) = (byte)((int)(.5 * r - .419 * g - .081 * b) + 128);
                    }
                }
                lookupTable = lTable;
            }

            /// <summary>
            /// Releases the reference to the lookup table.
            /// <para>The table has to be calculated again for the next lookup.</para>
            /// </summary>
            public static void UnloadLookupTable()
            {
                lookupTable = null;
            }
        }

        #endregion

        #region Color Conversion Routines

        //Todo standardize

        internal static unsafe void YUV2RGBManaged(byte[] YUVData, byte[] RGBData, int width, int height)
        {

            //returned pixel format is 2yuv - i.e. luminance, y, is represented for every pixel and the u and v are alternated
            //like this (where Cb = u , Cr = y)
            //Y0 Cb Y1 Cr Y2 Cb Y3 

            /*http://msdn.microsoft.com/en-us/library/ms893078.aspx
             * 
             * C = Y - 16
             D = U - 128
             E = V - 128
             R = clip(( 298 * C           + 409 * E + 128) >> 8)
             G = clip(( 298 * C - 100 * D - 208 * E + 128) >> 8)
             B = clip(( 298 * C + 516 * D           + 128) >> 8)

             * here are a whole bunch more formats for doing this...
             * http://stackoverflow.com/questions/3943779/converting-to-yuv-ycbcr-colour-space-many-versions
             */


            fixed (byte* pRGBs = RGBData, pYUVs = YUVData)
            {
                for (int r = 0; r < height; r++)
                {
                    byte* pRGB = pRGBs + r * width * 3;
                    byte* pYUV = pYUVs + r * width * 2;

                    //process two pixels at a time
                    for (int c = 0; c < width; c += 2)
                    {
                        int C1 = pYUV[1] - 16;
                        int C2 = pYUV[3] - 16;
                        int D = pYUV[2] - 128;
                        int E = pYUV[0] - 128;

                        int R1 = (298 * C1 + 409 * E + 128) >> 8;
                        int G1 = (298 * C1 - 100 * D - 208 * E + 128) >> 8;
                        int B1 = (298 * C1 + 516 * D + 128) >> 8;

                        int R2 = (298 * C2 + 409 * E + 128) >> 8;
                        int G2 = (298 * C2 - 100 * D - 208 * E + 128) >> 8;
                        int B2 = (298 * C2 + 516 * D + 128) >> 8;
#if true
                        //check for overflow
                        //unsurprisingly this takes the bulk of the time.
                        pRGB[0] = (byte)(R1 < 0 ? 0 : R1 > 255 ? 255 : R1);
                        pRGB[1] = (byte)(G1 < 0 ? 0 : G1 > 255 ? 255 : G1);
                        pRGB[2] = (byte)(B1 < 0 ? 0 : B1 > 255 ? 255 : B1);

                        pRGB[3] = (byte)(R2 < 0 ? 0 : R2 > 255 ? 255 : R2);
                        pRGB[4] = (byte)(G2 < 0 ? 0 : G2 > 255 ? 255 : G2);
                        pRGB[5] = (byte)(B2 < 0 ? 0 : B2 > 255 ? 255 : B2);
#else
                    pRGB[0] = (byte)(R1);
                    pRGB[1] = (byte)(G1);
                    pRGB[2] = (byte)(B1);

                    pRGB[3] = (byte)(R2);
                    pRGB[4] = (byte)(G2);
                    pRGB[5] = (byte)(B2);
#endif

                        pRGB += 6;
                        pYUV += 4;
                    }
                }
            }
        }

        internal static unsafe byte[] ABGRA2YUV420Managed(System.Drawing.Bitmap image)
        {

            int frameSize = image.Width * image.Height;
            int chromasize = frameSize / 4;

            int yIndex = 0;
            int uIndex = frameSize;
            int vIndex = frameSize + chromasize;
            byte[] yuv = new byte[frameSize * 3 / 2];

            //Get RGB Stride
            System.Drawing.Imaging.BitmapData data = ((System.Drawing.Bitmap)image).LockBits(new System.Drawing.Rectangle(0, 0, image.Width, image.Height),
                       System.Drawing.Imaging.ImageLockMode.ReadOnly, image.PixelFormat);

            uint* rgbValues = (uint*)data.Scan0.ToPointer();

            int index = 0;

            //Parrallel

            for (int j = 0; j < image.Height; j++)
            {
                for (int i = 0; i < image.Width; i++)
                {
                    uint B = (rgbValues[index] & 0xff000000) >> 24;
                    uint G = (rgbValues[index] & 0xff0000) >> 16;
                    uint R = (rgbValues[index] & 0xff00) >> 8;
                    uint a = (rgbValues[index] & 0xff) >> 0;

                    //int yuvC = Utility.RgbYuv.GetYuv(Common.Binary.ReverseU32(rgbValues[index]));

                    uint Y = ((66 * R + 129 * G + 25 * B + 128) >> 8) + 16;
                    uint U = (uint)(((-38 * R - 74 * G + 112 * B + 128) >> 8) + 128);
                    uint V = ((112 * R - 94 * G - 18 * B + 128) >> 8) + 128;

                    yuv[yIndex++] = (byte)((Y < 0) ? 0 : ((Y > 255) ? 255 : Y));// (byte)((yuvC & 0xff0000) >> 16); //

                    if (j % 2 == 0 && index % 2 == 0)
                    {
                        yuv[uIndex++] = (byte)((U < 0) ? 0 : ((U > 255) ? 255 : U));//(byte)((yuvC  & 0xff00) >> 8);//
                        yuv[vIndex++] = (byte)((V < 0) ? 0 : ((V > 255) ? 255 : V));// (byte)((yuvC & 0xff) >> 0);//
                    }

                    index++;
                }
            }

            ((System.Drawing.Bitmap)image).UnlockBits(data);

            data = null;

            return yuv;
        }

        #endregion
    }
}
