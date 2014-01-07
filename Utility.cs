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
    [CLSCompliant(false)]
    public static class Utility
    {

        #region Interface Speed

        //http://en.wikipedia.org/wiki/Interframe_gap

        internal const int InterframeGapBits = 96;

        const int LinkSpeed = 10000; //MB

        const double MicrosecondsPerMillisecond = 1000;

        internal const double InterframeSpacing = InterframeGapBits / LinkSpeed; //µs

        public static byte[] Empty = new byte[0];

        //Build interface table with speeds detected...

        #endregion

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

        public static System.Security.Cryptography.MD5 MD5HashAlgorithm = System.Security.Cryptography.MD5.Create();

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

        #region Static Helper Functions

        /// <summary>
        /// Checks the first two bits and the last two bits of each byte while moving the count to the correct position while doing so.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="start"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        /// <remarks>If knew the width did you, faster it could be..</remarks>
        public static bool FoundValidUniversalTextFormat(byte[] buffer, ref int start, ref int count)
        {
            unchecked //unaligned
            {
                //1100001 1
                while ((buffer[start] & 0xC3) == 0 && start < --count) ++start;
                return start < count;
            }
        }

        /// <summary>
        /// Indicates the position of the match in a given buffer to a given set of octets.
        /// If the match fails the start parameter will reflect the position of the last partial match.
        /// </summary>
        /// <param name="buffer">The bytes to search</param>
        /// <param name="start">The 0 based index to to start the forward based search</param>
        /// <param name="count">The amount of bytes to search in the buffer</param>
        /// <param name="octets">The bytes to search for</param>
        /// <param name="octetStart">The 0 based offset in the octets to search from</param>
        /// <param name="octetCount">The amount of octets required for a successful match</param>
        /// <returns>-1 if the match failed; Additionally start and count will reflect the position of the last partially matched byte, otherwise..
        /// The position within the buffer reletive to the start position in which the first occurance of octets given the octetStart and octetCount was matched
        /// </returns>
        public static int FoundBytes(byte[] buffer, ref int start, ref int count, byte[] octets, int octetStart, int octetCount)
        {
            //If the buffer or the octets are null no dice
            if (buffer == null || octets == null) return -1;

            //Cache the length
            int bufferLength = buffer.Length;

            //Maybe in reverse, undefined...
            if (count < start) count = bufferLength - start;

            //Make sure there is no way to run out of bounds given correct input
            if (bufferLength < octetCount || start + count > bufferLength) return -1;

            //Nothing to search
            if (octetCount == 0 && bufferLength == 0) return -1;

            //Create the variables we will use in the searching process
            int checkedBytes = 0, lastPosition = -1;

            try
            {
                //Loop the buffer from start to count
                while (start < count && checkedBytes < octetCount)
                {
                    //Find the next occurance of the required octet storing the result in bufferLength reducing the amount of places to search each time
                    if ((lastPosition = Array.IndexOf<byte>(buffer, octets[checkedBytes], start + checkedBytes, count - start - checkedBytes)) >= start) ++checkedBytes; //Partial match
                    else checkedBytes = 0;//The match failed at the current offset

                    //Move the position
                    start++;
                    count--;
                }

                //If matched totally then put the start and count variables at their correct places
                if (checkedBytes == octetCount)
                {
                    start -= checkedBytes;
                    count += checkedBytes;
                }

                //Return the last position of the partial match
                return lastPosition;
            }
            catch { throw; }
        }

        //ToDo Linqu use

        /// <summary>
        /// Receives the given amount of bytes into the buffer given a offset and an amount.
        /// </summary>
        /// <param name="buffer">The array to receive into</param>
        /// <param name="offset">The location to receive into</param>
        /// <param name="amount">The 0 based amount of bytes to receive, 0 will have no result</param>
        /// <param name="socket">The socket to receive on</param>
        /// <returns>The amount of bytes recieved which will be equal to the amount paramter unless the data was unable to fit in the given buffer</returns>
        public static int AlignedReceive(byte[] buffer, int offset, int amount, Socket socket)
        {
            //Return the amount if its negitive;
            if (amount <= 0) return amount;
            try
            {
                //To hold what was received and the maximum amount to receive
                int received = 0, max = buffer.Length - offset;

                //Store any socket errors here incase non-blocking sockets are being used.
                SocketError error = SocketError.SocketError;

                //While there is something to receive
                while (amount > 0 && offset < max)
                {
                    //Receive it into the buffer at the given offset taking into account what was already received
                    received += socket.Receive(buffer, offset + received, amount, SocketFlags.None, out error);

                    //decrease the amount by what was received
                    amount -= received;
                    //Increase the offset by what was received
                    offset += received;

                    //Break on any error besides WouldBlock, Could use Poll here
                    if (error != SocketError.WouldBlock) break;
                    else if (offset > max) break;
                }

                //Return the result
                return received;
            }
            catch { throw; }
        }

        #endregion

        #region IPacket

        /// <summary>
        /// An interface to encapsulate binary data which usually traverses an ethernet line.
        /// </summary>
        public interface IPacket
        {
            /// <summary>
            /// Encapsulates the object as binary data
            /// </summary>
            /// <returns>The binary representation of the IPacket</returns>
            IEnumerable<byte> ToBytes();

            /// <summary>
            /// The length of the IPacket in bytes
            /// </summary>
            int Length { get; }

            /// <summary>
            /// The DateTime(UTC) in which the IPacket was Sent
            /// </summary>
            /// <remarks>
            /// To determine if the IPacket was received use (!IPacket.Sent.HasValue)
            /// </remarks>
            DateTime? Sent { get; set; }

            /// <summary>
            /// The DateTime(UTC) in which the IPacket was Created
            /// </summary>        
            DateTime Created { get; }


            byte[] Buffer { get; }
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

        public static byte ReverseOctet(byte source)
        {
            if (source == 0) return 0;
            
            //If there is a bit in the source in the 0th position ensure cast the result to 16 bits, reverse and shift right 15 to obtain the reversed value
            if (source > sbyte.MaxValue) return (byte)(ReverseUnsignedShort((ushort)source) >> 15);
            
            //Circular shift the source
            return (byte)(source << 8 - source);
        }

        public static ushort ReverseUnsignedShort(ushort source) { return (ushort)(((source & 0xFF) << 8) | ((source >> 8) & 0xFF)); }

        public static ushort ConvertToUnsignedShort(byte[] buffer, int index, bool bigEndian)
        {   
            //The result of shifting a byte is an integer with 32 bits 16 of which are 0 when shifted left by 8 bits
            if(bigEndian) return (ushort)(buffer[index] << 8 | buffer[index + 1]);//Combined with the next 8 bits using binary OR and then shortened with a cast
            return (ushort)(buffer[index + 1] << 8 | buffer[index]);
        }

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

        #region ConcurrentThesaurus

        /// <summary>
        /// Represents a One to many collection which is backed by a ConcurrentDictionary.
        /// The values are retrieved as a IList of TKey
        /// </summary>
        /// <typeparam name="TKey">The type of the keys</typeparam>
        /// <typeparam name="TValue">The types of the definitions</typeparam>
        /// <remarks>
        /// Fancy tryin to get a IDictionary to flatten into this
        /// </remarks>
        public class ConcurrentThesaurus<TKey, TValue> : ILookup<TKey, TValue>, ICollection<TKey>
        {
            System.Collections.Concurrent.ConcurrentDictionary<TKey, IList<TValue>> Dictionary = new System.Collections.Concurrent.ConcurrentDictionary<TKey, IList<TValue>>();

            ICollection<TKey> Collection { get { return (ICollection<TKey>)Dictionary; } }

            public bool ContainsKey(TKey key)
            {
                return Dictionary.ContainsKey(key);
            }

            public void Add(TKey key)
            {
                if (!CoreAdd(key, default(TValue), null, false, true))
                {
                    //throw new ArgumentException("The given key was already present in the dictionary");
                }
            }

            public void Add(TKey key, TValue value)
            {
                IList<TValue> Predicates;

                //Attempt to get the value
                bool hadValue = Dictionary.TryGetValue(key, out Predicates);

                //Add the value
                if (!CoreAdd(key, value, Predicates, hadValue, false))
                {
                    //throw new ArgumentException("The given key was already present in the dictionary");
                }
            }

            public bool Remove(TKey key)
            {
                IList<TValue> removed;
                return Remove(key, out removed);
            }

            public bool Remove(TKey key, out IList<TValue> values)
            {
                return Dictionary.TryRemove(key, out values);
            }

            internal bool CoreAdd(TKey key, TValue value, IList<TValue> predicates, bool inDictionary, bool allocteOnly)
            {
                //If the predicates for the key are null then create them with the given value
                if (allocteOnly) predicates = new List<TValue>();
                else if (predicates == null) predicates = new List<TValue>() { value };
                else predicates.Add(value);//Othewise add the value to the predicates

                //Attempt to set the value of the Dictionary
                if (inDictionary)
                {
                    Dictionary[key] = predicates;
                    return true;
                }

                return Dictionary.TryAdd(key, predicates);
            }

            public IList<TValue> this[TKey key]
            {
                get
                {
                    return Dictionary[key];
                }
                set
                {
                    Dictionary[key] = value;
                }
            }

            bool ILookup<TKey, TValue>.Contains(TKey key)
            {
                return ContainsKey(key);
            }

            int ILookup<TKey, TValue>.Count
            {
                get { return Dictionary.Count; }
            }

            IEnumerable<TValue> ILookup<TKey, TValue>.this[TKey key]
            {
                get { return this[key]; }
            }

            IEnumerator<IGrouping<TKey, TValue>> IEnumerable<IGrouping<TKey, TValue>>.GetEnumerator()
            {
                return (IEnumerator<IGrouping<TKey, TValue>>)Dictionary.ToLookup(kvp => kvp.Key).GetEnumerator();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return Dictionary.GetEnumerator();
            }

            void ICollection<TKey>.Add(TKey item)
            {
                Collection.Add(item);
            }

            void ICollection<TKey>.Clear()
            {
                Collection.Clear();
            }

            bool ICollection<TKey>.Contains(TKey item)
            {
                return Collection.Contains(item);
            }

            void ICollection<TKey>.CopyTo(TKey[] array, int arrayIndex)
            {
                Collection.CopyTo(array, arrayIndex);
            }

            int ICollection<TKey>.Count
            {
                get { return Collection.Count; }
            }

            bool ICollection<TKey>.IsReadOnly
            {
                get { return Collection.IsReadOnly; }
            }

            bool ICollection<TKey>.Remove(TKey item)
            {
                return Collection.Remove(item);
            }

            IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator()
            {
                return Collection.GetEnumerator();
            }
        }

        #endregion

        #region RFC3550

        public static int Random32(int type = 0)
        {

            /*
               gettimeofday(&s.tv, 0);
               uname(&s.name);
               s.type = type;
               s.cpu  = clock();
               s.pid  = getpid();
               s.hid  = gethostid();
               s.uid  = getuid();
               s.gid  = getgid();
             */

            byte[] structure = BitConverter.GetBytes(type).//int     type;
                Concat(BitConverter.GetBytes(DateTime.UtcNow.Ticks)).Concat(BitConverter.GetBytes(Environment.TickCount)).//struct  timeval tv;
                Concat(BitConverter.GetBytes(TimeSpan.TicksPerMillisecond)).//clock_t cpu;
                Concat(BitConverter.GetBytes(System.Diagnostics.Process.GetCurrentProcess().Id)).//pid_t   pid;
                Concat(BitConverter.GetBytes(42)).//u_long  hid;
                Concat(BitConverter.GetBytes(7)).//uid_t   uid;
                Concat(Guid.NewGuid().ToByteArray()).//gid_t   gid;
                Concat(System.Text.Encoding.Default.GetBytes(Environment.OSVersion.VersionString)).ToArray();//struct  utsname name;
            
            //UtsName equivelant information would be

            //char  sysname[]  name of this implementation of the operating system
            //char  nodename[] name of this node within an implementation-dependent                 communications network
            //char  release[]  current release level of this implementation
            //char  version[]  current version level of this release
            //char  machine[]  name of the hardware type on which the system is running

            //Perform MD5 on structure per 3550

            byte[] digest = MD5HashAlgorithm.ComputeHash(structure);

            //Complete hash
            uint r = 0;
            r ^= BitConverter.ToUInt32(digest, 0);
            r ^= BitConverter.ToUInt32(digest, 4);
            r ^= BitConverter.ToUInt32(digest, 8);
            r ^= BitConverter.ToUInt32(digest, 12);
            return (int)r;
        }

        #endregion

        #region BaseDisposable

        /// <summary>
        /// Provides an implementation which contains the members required to adhere to the IDisposable implementation
        /// </summary>
        [CLSCompliant(true)]
        public abstract class BaseDisposable : IDisposable
        {
            /// <summary>
            /// Constructs a new BaseDisposable
            /// </summary>
            protected BaseDisposable() { }

            /// <summary>
            /// Finalizes the BaseDisposable by calling Dispose.
            /// </summary>
            ~BaseDisposable() { Dispose(); }

            /// <summary>
            /// Indicates if Dispose has been called previously.
            /// </summary>
            public bool Disposed { get; protected set; }

            /// <summary>
            /// Throws an ObjectDisposedException if Disposed is true.
            /// </summary>
            protected void CheckDisposed() { if (Disposed) throw new ObjectDisposedException(GetType().Name); }

            /// <summary>
            /// Allows derived implemenations a chance to destory manged or unmanged resources.
            /// The System.Runtime.CompilerServices.MethodImplOptions.Synchronized attribute prevents two threads from being in this method at the same time.
            /// </summary>
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            public virtual void Dispose()
            {
                //If already disposed return
                if (Disposed) return;

                //Mark the instance disposed
                Disposed = true;

                //Do not call the finalizer
                GC.SuppressFinalize(this);
            }
        }

        #endregion

    }
}
