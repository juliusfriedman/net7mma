namespace Media.Sockets
{
    public class DisposableWaitHandle : System.Threading.WaitHandle, Media.Common.IDisposed
    {
        #region Fields

        readonly Common.CommonDisposable m_Base = new Common.CommonDisposable(true);

        #endregion

        #region Properties

        /// <summary>
        /// Indicates if the Exception has been previously disposed
        /// </summary>
        public bool IsDisposed { get { return m_Base.IsDisposed; } }

        public bool ShouldDispose { get { return m_Base.ShouldDispose; } }

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a <see cref="Microsoft.Win32.SafeHandles.SafeWaitHandle"/> from the given pointer
        /// </summary>
        /// <param name="ptr"></param>
        /// <param name="ownsHandle"></param>
        public DisposableWaitHandle(System.IntPtr ptr, bool ownsHandle)
        {
            //base.Handle = ptr;

            base.SafeWaitHandle = new Microsoft.Win32.SafeHandles.SafeWaitHandle(ptr, ownsHandle);
        }

        /// <summary>
        /// Will dispose the given upon disposition of this instance.
        /// </summary>
        /// <param name="waitHandle"></param>
        public DisposableWaitHandle(System.Threading.WaitHandle waitHandle)
        {
            if (waitHandle == null) throw new System.ArgumentNullException("waitHandle");

            //base.Handle = waitHandle.Handle;

            base.SafeWaitHandle = waitHandle.SafeWaitHandle;
        }

        #endregion

        #region Overrides

        /// <summary>
        /// Disposes the exception
        /// </summary>
        protected override void Dispose(bool explicitDisposing)
        {
            //Should check ShouldDispose...
            if (m_Base.IsDisposed) return;

            base.Dispose(explicitDisposing);

            m_Base.Dispose();
        }

        #endregion
    }
}
