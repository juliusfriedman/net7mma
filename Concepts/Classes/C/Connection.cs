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

//To compete, allow expansion and compatability with with Sockets.Connection; what would possibly be a ConnectionSlim

namespace Media.Concepts.Classes.C
{
    /// <summary>
    /// A representation of the methods and properties assoicated with; in addition to <see cref="Media.Common.IDisposed"/>
    /// </summary>
    public interface IConnecton : Common.Interfaces.Interface, Media.Common.IDisposed
    {
        /// <summary>
        /// Start the connection process
        /// </summary>
        void BeginConnect();

        /// <summary>
        /// End the connection process
        /// </summary>
        void EndConnect();

        /// <summary>
        /// Start and End the connection process
        /// </summary>
        void Connect();

        /// <summary>
        /// Indicates if the connection is in progress
        /// </summary>
        bool IsConnecting { get; }

        /// <summary>
        /// Indicates if the connection is complete
        /// </summary>
        bool IsConnected { get; }
    }

    /// <summary>
    /// An implemenation of <see cref="IConnection"/>
    /// </summary>
    public class Connection : Common.SuppressedFinalizerDisposable, IConnecton
    {
        #region Statics

        /// <summary>
        /// If the connection is null or disposed, false; 
        /// If the connection is connecting, false;
        /// If the connection is connected, true;
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        static bool Refresh(Connection connection)
        {
            return ConnectionExtensions.IsConnecting(connection) ? false : ConnectionExtensions.IsConnected(connection);
        }

        #endregion

        #region Fields

        bool m_Connecting, m_Connected;

        /// <summary>
        /// An event which is fired to update the connection
        /// </summary>
        public event System.Action ConnectionEvent;

        #endregion

        #region Constructor

        public Connection(System.Action connectionEvent = null, bool shouldDispose = false)
            : base(shouldDispose)
        {
            ConnectionEvent = connectionEvent ?? Refresh;
        }

        #endregion

        //Media.Common.Structures.Getter Getter;
        
        //Media.Common.Structures.Setter Setter;

        #region Setters

        internal protected void SetConnected()
        {
            m_Connected = true;
        }

        internal protected void SetDisconnected()
        {
            m_Connected = false;
        }

        #endregion

        #region Properties

        public bool IsConnected
        {
            get { return m_Connected; }
        }

        public bool IsConnecting
        {
            get { return m_Connecting; }
        }

        public bool IsPersistent
        {
            get { return ShouldDispose.Equals(false); }
        }

        #endregion

        #region Methods

        /// <summary>
        /// The default handler of the <see cref="ConnectionEvent"/>
        /// </summary>
        void Refresh()
        {
            m_Connected = Refresh(this);
        }

        protected void BeginConnect()
        {
            m_Connecting = true;
        }

        protected void EndConnect()
        {
            m_Connecting = false;
        }

        #endregion

        #region IConnection

        void IConnecton.BeginConnect()
        {
            BeginConnect();
        }

        void IConnecton.EndConnect()
        {
            EndConnect();
        }

        void IConnecton.Connect()
        {
            BeginConnect();

            EndConnect();

            m_Connected = false.Equals(m_Connecting);
        }

        #endregion
    }

    /// <summary>
    /// Useful methods for instance of <see cref="IConnecton"/>
    /// </summary>
    static class ConnectionExtensions
    {
        public static bool IsConnecting(IConnecton connection)
        {
            if (Common.IDisposedExtensions.IsNullOrDisposed(connection)) return false;

            return connection.IsConnecting;
        }

        public static bool IsConnected(IConnecton connection)
        {
            if (Common.IDisposedExtensions.IsNullOrDisposed(connection)) return false;

            return connection.IsConnected;
        }
    }
}
