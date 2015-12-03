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
    #region NetworkConnection

    /// <summary>
    /// Represents a <see cref="Connection"/> specific to the Network.
    /// </summary>
    public class NetworkConnection : Connection, Common.ISocketReference
    {
        #region NetworkConnectionState

        [System.Flags]
        public enum NetworkConnectionState : long
        {
            None = 0,
            Initialized = 1,
            Bound = 2,
            Connected = 4,
            Send = 8,
            Recieve = 16,
        }

        #endregion

        #region Fields

        /// <summary>
        /// Created in <see cref="CreateWaitHandle"/>, Disposed in <see cref="Dispose"/>.
        /// </summary>
        DisposableWaitHandle WaitHandle;

        /// <summary>
        /// The date and time when the Connection was started.
        /// </summary>
        protected System.DateTime LasRemoteConnectionStartedDateTime;

        /// <summary>
        /// The date and time when the Connection was started.
        /// </summary>
        protected System.DateTime LastRemoteConnectionCompletedDateTime;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the amount of time taken to connect to the <see cref="RemoteEndPoint"/>
        /// </summary>
        public System.TimeSpan RemoteConnectionTime { get { return LastRemoteConnectionCompletedDateTime - LasRemoteConnectionStartedDateTime; } }

        /// <summary>
        /// The <see cref="System.Net.NetworkInformation.NetworkInterface"/> assoicated with the NetworkConnection.
        /// </summary>
        public System.Net.NetworkInformation.NetworkInterface NetworkInterface { get; protected set; }

        /// <summary>
        /// The <see cref="System.Net.Sockets.Socket"/> assoicated with the NetworkConnection.
        /// </summary>
        public System.Net.Sockets.Socket ConnectionSocket { get; protected set; }

        /// <summary>
        /// Indicates if the NetworkConnection has a <see cref="NetworkInterface"/> which is not null.
        /// </summary>
        public bool HasNetworkInterface { get { return NetworkInterface != null; } }

        /// <summary>
        /// The <see cref="System.Net.EndPoint"/> from which this NetworkConnection connects to the <see cref="RemoteEndPoint"/>
        /// </summary>
        public System.Net.EndPoint LocalEndPoint { get; protected set; }

        /// <summary>
        /// Indicates if this NetworkConnection has a <see cref="LocalEndPoint"/> which is not null.
        /// </summary>
        public bool HasLocalEndPoint { get { return LocalEndPoint != null; } }

        /// <summary>
        /// The <see cref="System.Net.EndPoint"/> from which this NetworkConnection is connected to via the <see cref="LocalEndPoint"/>
        /// </summary>
        public System.Net.EndPoint RemoteEndPoint { get; protected set; }

        /// <summary>
        /// Indicates if this NetworkConnection has a <see cref="RemoteEndPoint"/> which is not null.
        /// </summary>
        public bool HasRemoteEndPoint { get { return RemoteEndPoint != null; } }

        /// <summary>
        /// Indicates the <see cref="NetworkConnectionState"/> assoicated with this NetworkConnection
        /// </summary>
        public NetworkConnectionState NetworkConnectionFlags
        {
            get { return (NetworkConnectionState)Flags; }
            protected set { Flags = (long)value; }
        }

        #endregion

        #region Constructor

        public NetworkConnection()
            : base() { }

        public NetworkConnection(string name, bool shouldDispose)
            : base(name, shouldDispose) { }

        public NetworkConnection(System.Net.Sockets.Socket existingSocket, bool ownsHandle, bool shouldDispose)
            : this("System.Net.Socket-" + ownsHandle.ToString(), shouldDispose)
        {
            if (existingSocket == null) throw new System.ArgumentNullException("existingSocket");

            //Assign the ConnectionSocket
            ConnectionSocket = existingSocket;

            //Flag Initialized.
            FlagInitialized();

            //Assign the NetworkInterface
            NetworkInterface = Common.Extensions.NetworkInterface.NetworkInterfaceExtensions.GetNetworkInterface(ConnectionSocket);

            //Create a WaitHandle 
            CreateWaitHandle(ConnectionSocket.Handle, ownsHandle);

            //Check IsBound
            if (ConnectionSocket.IsBound)
            {
                //Flag Bound.
                FlagBound();

                //Serialize and Assign LocalEndPoint
                LocalEndPoint = existingSocket.LocalEndPoint;
            }

            //Check Connected
            if (ConnectionSocket.Connected)
            {
                //Sample the clock
                LasRemoteConnectionStartedDateTime = System.DateTime.UtcNow;

                //Serialize and Assign RemoteEndPoint
                RemoteEndPoint = existingSocket.RemoteEndPoint;

                //Call Connect to FlagConnected and call base logic.
                Connect();

                //Sample the clock
                LastRemoteConnectionCompletedDateTime = System.DateTime.UtcNow;
            }
        }

        #endregion

        #region NetworkChange Event Handlers

        void NetworkChange_NetworkAvailabilityChanged(object sender, System.Net.NetworkInformation.NetworkAvailabilityEventArgs e)
        {
            Refresh();
        }

        void NetworkChange_NetworkAddressChanged(object sender, System.EventArgs e)
        {
            Refresh();
        }

        #endregion

        #region Bound

        protected void FlagBound()
        {
            //Indicate Bound
            Flags |= (long)NetworkConnectionState.Bound;
        }

        protected void UnFlagBound()
        {
            //Indicate not Bound
            Flags &= (long)NetworkConnectionState.Bound;
        }

        #endregion

        #region Initialize

        protected void FlagInitialized()
        {
            //Indicate Connected
            Flags |= (long)NetworkConnectionState.Initialized;
        }

        protected void UnFlagInitialized()
        {
            //Indicate Not Connected
            Flags &= (long)NetworkConnectionState.Initialized;
        }

        public virtual void Initialize(bool registerForEvents)
        {
            //Check not already Initialized.
            if (false == NetworkConnectionFlags.HasFlag(NetworkConnectionState.Initialized))
            {
                //Indicate Initialized
                FlagInitialized();

                if (registerForEvents)
                {
                    //Attach events
                    System.Net.NetworkInformation.NetworkChange.NetworkAddressChanged += NetworkChange_NetworkAddressChanged;

                    System.Net.NetworkInformation.NetworkChange.NetworkAvailabilityChanged += NetworkChange_NetworkAvailabilityChanged;
                }
            }
        }

        #endregion

        #region Refresh

        public override void Refresh()
        {
            base.Refresh();
        }

        #endregion

        #region CreateConnectionSocket

        public virtual void CreateConnectionSocket(System.Net.Sockets.AddressFamily addressFamily, System.Net.Sockets.SocketType socketType, System.Net.Sockets.ProtocolType protocolType)
        {
            if (ConnectionSocket == null)
            {
                ConnectionSocket = new System.Net.Sockets.Socket(addressFamily, socketType, protocolType);

                CreateWaitHandle(ConnectionSocket.Handle, true);
            }
        }

        #endregion

        #region CreateWaitHandle

        public void CreateWaitHandle(System.IntPtr handle, bool ownsHandle)
        {
            if (WaitHandle == null)
            {
                WaitHandle = new DisposableWaitHandle(handle, ownsHandle);
            }
        }

        #endregion

        #region Connect

        protected void FlagConnected()
        {
            //Indicate Connected
            Flags |= (long)NetworkConnectionState.Connected;
        }

        protected void UnFlagConnected()
        {
            //Indicate Not Connected
            Flags &= (long)NetworkConnectionState.Connected;
        }

        public override void Connect()
        {
            //Check not already Connected.
            if (false == NetworkConnectionFlags.HasFlag(NetworkConnectionState.Connected))
            {
                //Check IsEstablished
                if (IsEstablished) return;

                if (NetworkInterface == null) throw new System.InvalidOperationException("NetworkInterface must be assigned before calling Connect.");

                if (LocalEndPoint == null) throw new System.InvalidOperationException("LocalEndPoint must be assigned before calling Connect.");

                if (RemoteEndPoint == null) throw new System.InvalidOperationException("RemoteEndPoint must be assigned before calling Connect.");

                //Set established
                base.Connect();

                //Indicate Connected
                FlagConnected();

                //Refresh the connection
                Refresh();
            }
        }

        /// <summary>
        /// Creates the <see cref="CreateConnectionSocket"/> using the specified options and connects the socket.
        /// Assigns <see cref="LocalEndPoint"/> and <see cref="RemoteEndPoint"/>
        /// </summary>
        /// <param name="addressFamily"></param>
        /// <param name="socketType"></param>
        /// <param name="protocolType"></param>
        /// <param name="addressList"></param>
        /// <param name="port"></param>
        public virtual void Connect(System.Net.Sockets.AddressFamily addressFamily, System.Net.Sockets.SocketType socketType, System.Net.Sockets.ProtocolType protocolType, System.Net.IPAddress[] addressList, int port)
        {
            try
            {
                //Create the socket
                CreateConnectionSocket(addressFamily, socketType, protocolType);

                //Sample the clock
                LasRemoteConnectionStartedDateTime = System.DateTime.UtcNow;

                //Connect the socket
                ConnectionSocket.Connect(addressList, port);

                //Sample the clock
                LastRemoteConnectionCompletedDateTime = System.DateTime.UtcNow;
            }
            finally
            {
                //Assign the NetworkInterface
                NetworkInterface = Common.Extensions.NetworkInterface.NetworkInterfaceExtensions.GetNetworkInterface(ConnectionSocket);

                //Assign the LocalEndPoint
                LocalEndPoint = (System.Net.IPEndPoint)ConnectionSocket.LocalEndPoint;

                //Assign the RemoteEndPoint
                RemoteEndPoint = (System.Net.IPEndPoint)ConnectionSocket.RemoteEndPoint;

                //Call Connect to FlagConnected and call base logic.
                Connect();
            }
        }

        #endregion

        #region Disconnect

        public virtual void Disconnect(bool allowReuse = false)
        {
            //Check not already Connected.
            if (((NetworkConnectionState)Flags).HasFlag(NetworkConnectionState.Connected))
            {
                ConnectionSocket.Disconnect(allowReuse);

                base.Disconnect();

                UnFlagConnected();

                Refresh();
            }
        }

        public override void Disconnect()
        {
            Disconnect(false);
        }

        #endregion

        #region Dispose

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            using (WaitHandle)
            {
                System.Net.NetworkInformation.NetworkChange.NetworkAddressChanged -= NetworkChange_NetworkAddressChanged;

                System.Net.NetworkInformation.NetworkChange.NetworkAvailabilityChanged -= NetworkChange_NetworkAvailabilityChanged;

                ConnectionSocket = null;

                LocalEndPoint = RemoteEndPoint = null;

                NetworkInterface = null;
            }
        }

        #endregion

        System.Collections.Generic.IEnumerable<System.Net.Sockets.Socket> Common.ISocketReference.GetReferencedSockets()
        {
            yield return ConnectionSocket;
        }
    }

    #endregion

    public static class NetworkConnectionStateExtensions
    {
        public static bool HasNoFlags(this NetworkConnection nc)
        {
            return nc.NetworkConnectionFlags.HasFlag(Media.Sockets.NetworkConnection.NetworkConnectionState.None);
        }

        public static bool IsBound(this NetworkConnection nc)
        {
            return NetworkConnectionStateExtensions.HasFlag(nc, Media.Sockets.NetworkConnection.NetworkConnectionState.Bound);
        }

        public static bool IsConnected(this NetworkConnection nc)
        {
            return NetworkConnectionStateExtensions.HasFlag(nc, Media.Sockets.NetworkConnection.NetworkConnectionState.Connected);
        }

        public static bool HasFlag(this NetworkConnection nc, Media.Sockets.NetworkConnection.NetworkConnectionState ncs)
        {
            return nc.NetworkConnectionFlags.HasFlag(ncs);
        }
    }
}
