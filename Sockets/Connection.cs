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
    #region Connection

    /// <summary>
    /// Provides a base class to facilitate the concept of a Connection.
    /// </summary>
    public abstract class Connection : Common.BaseDisposable
    {
        #region Statics

        /// <summary>
        /// A string with the format of:
        ///     `TypeName Id Flags Name`
        /// </summary>
        const string FormatString = "{0} {1} ({2}) {3}";

        #endregion

        #region Properties

        /// <summary>
        /// The unique identifier assoicated with this instance.
        /// </summary>
        public readonly System.Guid Id = System.Guid.NewGuid();

        /// <summary>
        /// The date and time the instance was created.
        /// </summary>
        public readonly System.DateTime Created = System.DateTime.UtcNow;

        /// <summary>
        /// The name assigned to this instance.
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// Indicates if <see cref="Disconnect"/> will be called when disposing.
        /// </summary>
        public bool IsPersistent
        {
            get
            {
                return ShouldDispose == false;
            }
            set
            {
                ShouldDispose = value == false;
            }
        }

        /// <summary>
        /// Provided for derived implementations
        /// </summary>
        protected long Flags { get; set; }

        /// <summary>
        /// Indicates if the Connection is established.
        /// </summary>
        public virtual bool IsEstablished { get; protected set; }

        /// <summary>
        /// The date and time the Connection was established.
        /// </summary>
        public System.DateTime EstablishedDateTime { get; protected set; }

        /// <summary>
        /// The amount of time the connection has been established.
        /// </summary>
        public System.TimeSpan TimeEstablished { get { return IsEstablished ? System.DateTime.UtcNow - EstablishedDateTime : System.TimeSpan.Zero; } }

        #endregion

        #region Connect

        /// <summary>
        /// If <see cref="IsDisposed"/> or <see cref="IsEstablished"/> are true, Sets <see cref="EstablishedDateTime"/> to the current time and <see cref="IsEstablished"/> to true.
        /// </summary>
        public virtual void Connect()
        {
            if (IsDisposed || IsEstablished) return;

            EstablishedDateTime = System.DateTime.UtcNow;

            IsEstablished = true;
        }

        #endregion

        #region Disconnect

        /// <summary>
        /// If <see cref="IsDisposed"/> is true and <see cref="IsEstablished"/> is false, Sets <see cref="IsEstablished"/> to false.
        /// </summary>
        public virtual void Disconnect()
        {
            if (IsDisposed || false == IsEstablished) return;

            IsEstablished = false;
        }

        #endregion

        #region Refresh

        /// <summary>
        /// Refreshes the details of the Connection.
        /// Throws a <see cref="System.ObjectDisposedException"/> if <see cref="IsDisposed"/> is true.
        /// </summary>
        public virtual void Refresh()
        {
            CheckDisposed();
        }

        #endregion

        #region Dispose

        /// <summary>
        /// If <see cref="IsDisposed"/> is True the call returns immediately.
        /// Calls <see cref="Disconnect"/> if <see cref="IsPersistent"/> is False and calls <see cref="Common.BaseDisposable.Dispose"/>
        /// </summary>
        public override void Dispose()
        {
            if (IsDisposed) return;

            if (false == IsPersistent) Disconnect();

            base.Dispose();
        }

        #endregion

        #region Constructor

        public Connection(string name, bool shouldDispose)
            : base(shouldDispose)
        {
            Name = name;
        }

        public Connection()
            : this(string.Empty, true) { }

        #endregion

        #region ToString

        public override string ToString()
        {
            return string.Format(FormatString, GetType().Name.ToString(), Id.ToString(), Flags, Name);
        }

        #endregion
    }

    #endregion
}
