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

//Possibly lighter than an entire Sockets namespace...

//Would also need to provide SocketClient, IpClient, TcpClient, UdpClient and they should probably implement ITransportClient after that point
//E,g, ITransportClient is the lowest level you can get, now to determine if that is protocol specific so maybes its a `TransportProtocolClient` etc.

namespace Concepts.Classes.Sockets
{
    public class SocketEndPoint : System.Net.EndPoint
    {
        public System.Net.Sockets.ProtocolType ProtocolType { get; protected set; }

        public bool IsUnspecifiedProtocolType
        {
            get
            {
                return ProtocolType == System.Net.Sockets.ProtocolType.Unspecified;
            }
        }

        public bool IsUnknownProtocolType
        {
            get { return ProtocolType == System.Net.Sockets.ProtocolType.Unknown; }
        }

        public System.Net.Sockets.ProtocolFamily ProtocolFamily { get; protected set; }

        public bool IsUnspecifiedProtocolFamily
        {
            get
            {
                return ProtocolFamily == System.Net.Sockets.ProtocolFamily.Unspecified;
            }
        }

        public bool IsUnknownProtocolFamily
        {
            get { return ProtocolFamily == System.Net.Sockets.ProtocolFamily.Unknown; }
        }

        public string Scheme
        {
            get { return ProtocolType.ToString().ToLowerInvariant(); }
        }

        public SocketEndPoint(System.Net.Sockets.ProtocolType protocolType)
        {
            ProtocolType = protocolType;
        }

        public SocketEndPoint(System.Net.Sockets.ProtocolType protocolType, System.Net.Sockets.ProtocolFamily protocolFamily)
            : this(protocolType)
        {
            ProtocolFamily = protocolFamily;
        }

        public override bool Equals(object obj)
        {
            if (false.Equals(base.Equals(obj))) return false;

            InternetEndPoint iNetComparand = obj as InternetEndPoint;

            if (iNetComparand == null)
                return false;

            return (ProtocolType == iNetComparand.ProtocolType &&
                    ProtocolFamily == iNetComparand.ProtocolFamily);
        }

        public override int GetHashCode()
        {
            return ProtocolType.GetHashCode() ^ base.GetHashCode();
        }

        public override string ToString()
        {
            return string.Join(Media.Common.Extensions.IPEndPoint.IPEndPointExtensions.SchemeSeperator, ProtocolType.ToString().ToLowerInvariant(), base.ToString());
        }

        public System.Uri ToUri()
        {
            return new System.Uri(ToString());
        }
    }

    //Would have to conditionally inherit from
    //public class InternetAddress : System.Net.IPAddress
    //{
    //    InternetAddress()
    //        : base(0)
    //    {

    //    }
    //}

    //Would have to conditionally inherit from
    ////public class SocketAddress : System.Net.SocketAddress
    ////{
    ////    public SocketAddress()
    ////        : base(System.Net.Sockets.AddressFamily.Unknown)
    ////    {
    ////    }
    ////}

    //Needs a way to go from SocketAddress to IPAddress if IPEndPoint is implicit.

    //May as well add IPAddress if Portability aspects are not certain.

    public class InternetEndPoint : SocketEndPoint //, System.IComparable<System.Net.IPEndPoint>, System.IEquatable<System.Net.IPEndPoint>
    {
        protected int m_Port;

        protected System.Net.Sockets.AddressFamily m_Family;

        public InternetEndPoint()
            : base(System.Net.Sockets.ProtocolType.Unspecified, System.Net.Sockets.ProtocolFamily.Unspecified)
        {
            //default
            //SocketAddress = System.Net.SocketAddress
        }

        public InternetEndPoint(int port, System.Net.Sockets.AddressFamily addressFamily)
            : this(port, addressFamily, System.Net.Sockets.ProtocolType.Unspecified, System.Net.Sockets.ProtocolFamily.Unspecified)
        {

        }

        public InternetEndPoint(int port, System.Net.Sockets.AddressFamily addressFamily,
            System.Net.Sockets.ProtocolType protocolType, System.Net.Sockets.ProtocolFamily protocolFamily)
            : base(protocolType, protocolFamily)
        {
            if (port < System.Net.IPEndPoint.MinPort || port > System.Net.IPEndPoint.MaxPort)
            {
                throw new System.ArgumentOutOfRangeException("port");
            }

            m_Port = port;
        }

        public InternetEndPoint(int port, System.Net.Sockets.AddressFamily addressFamily, System.Net.SocketAddress socketAddress,
            System.Net.Sockets.ProtocolType protocolType, System.Net.Sockets.ProtocolFamily protocolFamily)
            : this(port, addressFamily, protocolType, protocolFamily)
        {
            if (socketAddress == null) throw new System.ArgumentNullException("socketAddress");

            if (false.Equals(socketAddress.Family == AddressFamily)) throw new System.InvalidOperationException("AddressFamily must match the socketAddress.Family");

            SocketAddress = socketAddress;
        }

        public System.Net.SocketAddress SocketAddress { get; protected set; }

        public override System.Net.Sockets.AddressFamily AddressFamily
        {
            get
            {
                return m_Family;
            }
        }

        public int Port
        {
            get
            {
                return m_Port;
            }
        }

        public override string ToString()
        {
            return string.Concat(m_Family, "/", ProtocolType, "\\", ProtocolType, "\\",
                string.Join(Media.Common.Extensions.IPEndPoint.IPEndPointExtensions.SchemeSeperator, ProtocolType.ToString().ToLowerInvariant(), SocketAddress == null ? string.Empty : SocketAddress.ToString()),
                Media.Common.Extensions.IPEndPoint.IPEndPointExtensions.PortSeperator, m_Port);
        }

        public override int GetHashCode()
        {
            return m_Family.GetHashCode() ^ m_Port ^ base.GetHashCode();
        }

        public override bool Equals(object comparand)
        {
            if (false.Equals(base.Equals(comparand))) return false;

            InternetEndPoint iNetComparand = comparand as InternetEndPoint;

            if (iNetComparand == null)
                return false;

            return (m_Family == iNetComparand.m_Family &&
                    m_Port == iNetComparand.m_Port);
        }

        public static implicit operator System.Net.IPEndPoint(InternetEndPoint iep)
        {
            return new System.Net.IPEndPoint(System.Net.IPAddress.Any, iep.Port); //new System.Net.IPAddress(iep.SocketAddress)
        }
    }

    public class DnsEndPoint : InternetEndPoint
    {
        string m_Host;

        public DnsEndPoint(string host, int port) : this(host, port, System.Net.Sockets.AddressFamily.Unspecified) { }

        public DnsEndPoint(string host, int port, System.Net.Sockets.AddressFamily addressFamily, System.Net.Sockets.ProtocolType protocolType)
            : this(host, port, addressFamily, protocolType, System.Net.Sockets.ProtocolFamily.Unspecified) { }
        public DnsEndPoint(string host, int port, System.Net.Sockets.AddressFamily addressFamily)
            : this(host, port, addressFamily, System.Net.Sockets.ProtocolType.Unspecified, System.Net.Sockets.ProtocolFamily.Unspecified) { }

        public DnsEndPoint(string host, int port, System.Net.Sockets.AddressFamily addressFamily,
            System.Net.Sockets.ProtocolType protocolType, System.Net.Sockets.ProtocolFamily protocolFamily)
            : base(port, addressFamily, protocolType, protocolFamily)
        {
            if (string.IsNullOrWhiteSpace(host))
            {
                throw new System.ArgumentException("Cannot be null or consist only of whitespace", "host");
            }

            if (false.Equals(m_Family == System.Net.Sockets.AddressFamily.InterNetwork)
                &&
                false.Equals(m_Family == System.Net.Sockets.AddressFamily.InterNetworkV6))
            {
                throw new System.ArgumentException("Invalid Address Family", "addressFamily");
            }

            m_Host = host;

            m_Family = addressFamily;
        }

        public override bool Equals(object comparand)
        {
            if (false.Equals(base.Equals(comparand))) return false;

            DnsEndPoint dnsComparand = comparand as DnsEndPoint;

            if (dnsComparand == null)
                return false;

            return (m_Family == dnsComparand.m_Family &&
                    m_Port == dnsComparand.m_Port &&
                    m_Host == dnsComparand.m_Host);
        }

        public override int GetHashCode()
        {
            return m_Host.GetHashCode() ^ base.GetHashCode();
        }

        public override string ToString()
        {
            return string.Concat(m_Family, "/",
                string.Join(Media.Common.Extensions.IPEndPoint.IPEndPointExtensions.SchemeSeperator, ProtocolType.ToString().ToLowerInvariant(), m_Host),
                Media.Common.Extensions.IPEndPoint.IPEndPointExtensions.PortSeperator, m_Port);
        }

        public string Host
        {
            get
            {
                return m_Host;
            }
        }
    }
}
