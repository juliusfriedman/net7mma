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

namespace Media.Sockets
{
    #region IPNetworkConnection

    /// <summary>
    /// Represents a <see cref="NetworkConnection"/> which utilizes the IP Protocol.
    /// </summary>
    public class IPNetworkConnection : NetworkConnection
    {
        #region Statics

        public static System.Net.NetworkInformation.IPGlobalProperties IPGlobalProperties
        {
            get { return System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties(); }
        }

        #region CreateIPHostEntry

        public static System.Net.IPHostEntry CreateIPHostEntry(System.Net.IPAddress address, string hostName, params string[] aliases)
        {
            return CreateIPHostEntry(Common.Extensions.Object.ObjectExtensions.ToArray<System.Net.IPAddress>(address),
                hostName,
                aliases);
        }

        public static System.Net.IPHostEntry CreateIPHostEntry(string hostName, params System.Net.IPAddress[] address)
        {
            return CreateIPHostEntry(address, hostName, null);
        }

        public static System.Net.IPHostEntry CreateIPHostEntry(System.Net.IPAddress[] addresses, string hostName, params string[] aliases)
        {
            return new System.Net.IPHostEntry()
            {
                AddressList = Common.Extensions.Array.ArrayExtensions.IsNullOrEmpty(addresses) ? Common.Extensions.Object.ObjectExtensions.ToArray<System.Net.IPAddress>(System.Net.IPAddress.None) : addresses,
                Aliases = Common.Extensions.Array.ArrayExtensions.IsNullOrEmpty(aliases) ? Common.Extensions.Object.ObjectExtensions.ToArray<string>(string.Empty) : aliases,
                HostName = hostName ?? string.Empty
            };
        }

        #endregion

        #endregion

        #region Properties

        /// <summary>
        /// Gets the <see cref="System.Net.NetworkInformation.IPInterfaceProperties"/> assoicated with the <see cref="NetworkInterface"/>
        /// </summary>
        public System.Net.NetworkInformation.IPInterfaceProperties IPInterfaceProperties
        {
            get { return HasNetworkInterface ? NetworkInterface.GetIPProperties() : null; }
        }

        /// <summary>
        /// Indicates if the <see cref="RemoteIPEndPoint"/> has a <see cref="System.Net.NetworkInformation.NetworkInterface"/> on this computer.
        /// </summary>
        public bool IsLocalConnection { get { return HasRemoteIPEndPoint && Common.Extensions.NetworkInterface.NetworkInterfaceExtensions.GetNetworkInterface(RemoteIPEndPoint) != null; } }

        /// <summary>
        /// Indicates if the <see cref="RemoteIPEndPoint"/> is from within the same network as this computer.
        /// </summary>
        public bool IsIntranetConnection
        {
            get { return false == IsLocalConnection && Common.Extensions.IPAddress.IPAddressExtensions.IsOnIntranet(RemoteIPEndPoint.Address); }
        }

        #region Local        

        /// <summary>
        /// The <see cref="System.Net.IPHostEntry"/> assoicated with the <see cref="LocalIPEndPoint"/>
        /// </summary>
        public System.Net.IPHostEntry LocalIPHostEntry { get; protected set; }

        /// <summary>
        /// Indicates if the <see cref="LocalIPHostEntry"/> is not null.
        /// </summary>
        public bool HasLocalIPHostEntry { get { return LocalIPHostEntry != null; } }

        /// <summary>
        /// Gets or sets the <see cref="LocalEndPoint"/>.
        /// 
        /// If the <see cref="LocalEndPoint"/> is not a <see cref="System.Net.IPEndPoint"/> a <see cref="System.InvalidOperationException"/> will be thrown.
        /// </summary>
        public System.Net.IPEndPoint LocalIPEndPoint
        {
            get { return (System.Net.IPEndPoint)LocalEndPoint; }
            set
            {
                if (false == LocalEndPoint is System.Net.IPEndPoint) throw new System.InvalidOperationException("LocalEndPoint is not a System.Net.IPEndPoint");

                LocalEndPoint = value;
            }
        }

        /// <summary>
        /// Indicates if the <see cref="LocalIPEndPoint"/> is not null.
        /// </summary>
        public bool HasLocalIPEndPoint { get { return LocalIPEndPoint != null; } }

        #endregion

        #region Dhcp

        /// <summary>
        /// Gets the <see cref="System.Net.NetworkInformation.IPAddressCollection"/> assoicated with the <see cref="IPInterfaceProperties"/>
        /// </summary>
        public virtual System.Net.NetworkInformation.IPAddressCollection DhcpServerAddresses
        {
            get
            {
                return IPInterfaceProperties.DhcpServerAddresses;
            }
        }

        /// <summary>
        /// Indicates if the IPNetworkConnection utilized Dhcp
        /// </summary>
        public bool UsesDhcp
        {
            get
            {
                return DhcpServerAddresses.Count > 0; /* && DhcpLeaseLifetime != System.TimeSpan.MinValue;*/
            }
        }

        //Could make a Superset class of to unify paths..
        //System.Net.NetworkInformation.IPAddressInformationCollection ipAddressCollection;

        /// <summary>
        /// If <see cref="UsesDhcp"/> the amount of time of the IPAddress is leased according the <see cref="System.Net.NetworkInformation.IPAddressInformation"/> assoicated with the <see cref="LocalIPEndPoint"/>.
        /// 
        /// If the <see cref="LocalIPEndPoint"/> is not found in the leased <see cref="System.Net.NetworkInformation.IPAddressInformation"/> then <see cref="Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan"/> is returned.
        /// </summary>
        public System.TimeSpan DhcpLeaseLifetime
        {
            get
            {
                //If there is no Dhcp server the DhcpLeaveLifeTime is 0
                if (false == UsesDhcp) return System.TimeSpan.Zero;

                //Check Multicast if the address IsMulticast
                if (Common.Extensions.IPAddress.IPAddressExtensions.IsMulticast(LocalIPEndPoint.Address))
                {
                    System.Net.NetworkInformation.MulticastIPAddressInformationCollection multicastIPAddressInformationCollection = IPInterfaceProperties.MulticastAddresses;

                    foreach (System.Net.NetworkInformation.MulticastIPAddressInformation multicastIPAddressInformation in multicastIPAddressInformationCollection)
                    {
                        if (multicastIPAddressInformation.Address.Equals(LocalIPEndPoint.Address))
                        {
                            return System.TimeSpan.FromSeconds(multicastIPAddressInformation.DhcpLeaseLifetime);
                        }
                    }
                }
                else //Check Unicast otherwise
                {
                    System.Net.NetworkInformation.UnicastIPAddressInformationCollection unicastIPAddressInformationCollection = IPInterfaceProperties.UnicastAddresses;

                    foreach (System.Net.NetworkInformation.UnicastIPAddressInformation unicastIPAddressInformation in unicastIPAddressInformationCollection)
                    {
                        if (unicastIPAddressInformation.Address.Equals(LocalIPEndPoint.Address))
                        {
                            return System.TimeSpan.FromSeconds(unicastIPAddressInformation.DhcpLeaseLifetime);
                        }
                    }
                }

                //Could not find a an IPAddress which matched the LocalIPEndPoint, indicate infinite timeout.
                return Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan;
            }
        }

        /// <summary>
        /// If <see cref="UsesDhcp"/> Gets the number of seconds remaining during which this address is valid.
        /// 
        /// If the <see cref="LocalIPEndPoint"/> is not found in the leased <see cref="System.Net.NetworkInformation.IPAddressInformation"/> then <see cref="Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan"/> is returned.
        /// </summary>
        public System.TimeSpan DhcpAddressValidLifetime
        {
            get
            {
                //If there is no Dhcp server the DhcpLeaveLifeTime is 0
                if (false == UsesDhcp) return System.TimeSpan.Zero;

                //Check Multicast if the address IsMulticast
                if (Common.Extensions.IPAddress.IPAddressExtensions.IsMulticast(LocalIPEndPoint.Address))
                {
                    System.Net.NetworkInformation.MulticastIPAddressInformationCollection multicastIPAddressInformationCollection = IPInterfaceProperties.MulticastAddresses;

                    foreach (System.Net.NetworkInformation.MulticastIPAddressInformation multicastIPAddressInformation in multicastIPAddressInformationCollection)
                    {
                        if (multicastIPAddressInformation.Address.Equals(LocalIPEndPoint.Address))
                        {
                            return System.TimeSpan.FromSeconds(multicastIPAddressInformation.AddressValidLifetime);
                        }
                    }
                }
                else //Check Unicast otherwise
                {
                    System.Net.NetworkInformation.UnicastIPAddressInformationCollection unicastIPAddressInformationCollection = IPInterfaceProperties.UnicastAddresses;

                    foreach (System.Net.NetworkInformation.UnicastIPAddressInformation unicastIPAddressInformation in unicastIPAddressInformationCollection)
                    {
                        if (unicastIPAddressInformation.Address.Equals(LocalIPEndPoint.Address))
                        {
                            return System.TimeSpan.FromSeconds(unicastIPAddressInformation.AddressValidLifetime);
                        }
                    }
                }

                //Could not find a an IPAddress which matched the LocalIPEndPoint, indicate infinite timeout.
                return Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan;
            }
        }

        /// <summary>
        /// If <see cref="UsesDhcp"/> Gets the number of seconds remaining during which this address is the preferred address.
        /// 
        /// If the <see cref="LocalIPEndPoint"/> is not found in the leased <see cref="System.Net.NetworkInformation.IPAddressInformation"/> then <see cref="Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan"/> is returned.
        /// </summary>
        public System.TimeSpan DhcpAddressPreferredLifetime
        {
            get
            {
                //If there is no Dhcp server the DhcpLeaveLifeTime is 0
                if (false == UsesDhcp) return System.TimeSpan.Zero;

                //Check Multicast if the address IsMulticast
                if (Common.Extensions.IPAddress.IPAddressExtensions.IsMulticast(LocalIPEndPoint.Address))
                {
                    System.Net.NetworkInformation.MulticastIPAddressInformationCollection multicastIPAddressInformationCollection = IPInterfaceProperties.MulticastAddresses;

                    foreach (System.Net.NetworkInformation.MulticastIPAddressInformation multicastIPAddressInformation in multicastIPAddressInformationCollection)
                    {
                        if (multicastIPAddressInformation.Address.Equals(LocalIPEndPoint.Address))
                        {
                            return System.TimeSpan.FromSeconds(multicastIPAddressInformation.AddressPreferredLifetime);
                        }
                    }
                }
                else //Check Unicast otherwise
                {
                    System.Net.NetworkInformation.UnicastIPAddressInformationCollection unicastIPAddressInformationCollection = IPInterfaceProperties.UnicastAddresses;

                    foreach (System.Net.NetworkInformation.UnicastIPAddressInformation unicastIPAddressInformation in unicastIPAddressInformationCollection)
                    {
                        if (unicastIPAddressInformation.Address.Equals(LocalIPEndPoint.Address))
                        {
                            return System.TimeSpan.FromSeconds(unicastIPAddressInformation.AddressPreferredLifetime);
                        }
                    }
                }

                //Could not find a an IPAddress which matched the LocalIPEndPoint, indicate infinite timeout.
                return Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan;
            }
        }

        #endregion

        #region Remote

        /// <summary>
        /// Provides information about the <see cref="RemoteEndPoint"/>.Address
        /// </summary>
        public System.Net.NetworkInformation.IPAddressInformation RemoteAddressInformation { get; protected set; }

        /// <summary>
        /// Indicates if the <see cref="RemoteAddressInformation"/> is not null.
        /// </summary>
        public bool HasRemoteAddressInformation { get { return RemoteAddressInformation != null; } }

        /// <summary>
        /// The <see cref="System.Net.IPHostEntry"/> assoicated with the <see cref="RemoteIPEndPoint"/>
        /// </summary>
        public System.Net.IPHostEntry RemoteIPHostEntry { get; protected set; }

        /// <summary>
        /// Indicates if the <see cref="RemoteIPHostEntry"/> is not null.
        /// </summary>
        public bool HasRemoteIPHostEntry { get { return RemoteIPHostEntry != null; } }

        /// <summary>
        /// Gets or sets the <see cref="RemoteEndPoint"/>.
        /// 
        /// If the <see cref="RemoteEndPoint"/> is not a <see cref="System.Net.IPEndPoint"/> a <see cref="System.InvalidOperationException"/> will be thrown.
        /// </summary>
        public System.Net.IPEndPoint RemoteIPEndPoint
        {
            get { return (System.Net.IPEndPoint)RemoteEndPoint; }
            set
            {
                if (false == RemoteEndPoint is System.Net.IPEndPoint) throw new System.InvalidOperationException("RemoteEndPoint is not a System.Net.IPEndPoint");

                RemoteEndPoint = value;
            }
        }

        /// <summary>
        /// Indicates if the <see cref="RemoteEndPoint"/> is a <see cref="System.Net.IPEndPoint"/>
        /// </summary>
        public bool HasRemoteIPEndPoint { get { return RemoteIPEndPoint != null; } }

        #endregion

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new NetworkConnection with the given.
        /// </summary>
        /// <param name="remoteIPHostEntry">given</param>
        public IPNetworkConnection(System.Net.IPHostEntry remoteIPHostEntry)
            : base()
        {
            if (remoteIPHostEntry == null) throw new System.ArgumentNullException("remoteIPHostEntry");

            RemoteIPHostEntry = remoteIPHostEntry;            
        }

        /// <summary>
        /// Creates a new NetworkConnection by resolving the given using <see cref="System.Net.Dns.GetHostEntry"/>
        /// </summary>
        /// <param name="hostNameOrAddress">given</param>
        public IPNetworkConnection(string hostNameOrAddress) :
            this(System.Net.Dns.GetHostEntry(hostNameOrAddress))
        {
            RemoteAddressInformation = new IPAddressInformation(System.Net.IPAddress.None, true, false);
        }

        /// <summary>
        /// Creates a new NetworkConnection and <see cref="new System.Net.IPHostEntry"/> using the given address.
        /// </summary>
        /// <param name="address">The address</param>
        public IPNetworkConnection(System.Net.IPAddress address) :
            this(CreateIPHostEntry(string.Empty, address))
        {
            RemoteAddressInformation = new IPAddressInformation(System.Net.IPAddress.None, false, false);
        }

        public IPNetworkConnection(System.Uri uri) : this(uri.DnsSafeHost) { }

        #endregion

        #region Refresh

        /// <summary>
        /// If <see cref="HasNetworkInterface"/> is True And <see cref="HasLocalIPEndPoint"/> then <see cref="NetworkInterface"/> is updated using <see cref="Common.Extensions.NetworkInterface.NetworkInterfaceExtensions.GetNetworkInterface"/>
        /// </summary>
        public void RefreshNetworkInterface()
        {
            if (HasNetworkInterface && HasLocalIPEndPoint)
            {                
                NetworkInterface = Common.Extensions.NetworkInterface.NetworkInterfaceExtensions.GetNetworkInterface(LocalIPEndPoint);
            }
        }

        /// <summary>
        /// If <see cref="HasRemoteAddressInformation"/> is True And <see cref="RemoteAddressInformation.IsDnsEligible"/> then the <see cref="RemoteIPHostEntry"/> is updated using <see cref="System.Net.Dns.GetHostEntry"/>
        /// </summary>
        public void RefreshRemoteIPHostEntry()
        {
            if (HasRemoteAddressInformation && RemoteAddressInformation.IsDnsEligible)
            {
                RemoteIPHostEntry = System.Net.Dns.GetHostEntry(RemoteIPEndPoint.Address);
            }
        }

        public override void Refresh()
        {
            if (IsDisposed) return;

            base.Refresh();

            RefreshNetworkInterface();

            RefreshRemoteIPHostEntry();
        }

        #endregion

        #region Connect

        public void Connect(int addressIndex, System.Net.NetworkInformation.NetworkInterface networkInterface, int port = 0)
        {
            if (ConnectionSocket == null) throw new System.InvalidOperationException("There must be a ConnectionSocket assigned before calling Connect.");

            if (addressIndex < 0) throw new System.IndexOutOfRangeException("addressIndex must be > 0 and < HostEntry.AddressList.Length");

            if (networkInterface == null) throw new System.ArgumentNullException("networkInterface");

            NetworkInterface = networkInterface;

            RemoteEndPoint = new System.Net.IPEndPoint(RemoteIPHostEntry.AddressList[addressIndex], port);

            Connect();

            LocalEndPoint = ConnectionSocket.LocalEndPoint;

            RemoteAddressInformation = new IPAddressInformation(RemoteIPEndPoint.Address, RemoteAddressInformation.IsDnsEligible, RemoteAddressInformation.IsTransient);
        }

        #endregion

        #region Dispose

        public override void Dispose()
        {
            base.Dispose();

            RemoteIPHostEntry = null;

            LocalEndPoint = RemoteEndPoint = null;

            NetworkInterface = null;
        }

        #endregion
    }

    #endregion
}
