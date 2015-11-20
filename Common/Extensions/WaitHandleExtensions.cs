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

namespace Media.Common.Extensions.WaitHandle
{

    public class DisposableWaitHandle : System.Threading.WaitHandle, IDisposed
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
            if (m_Base.IsDisposed) return;

            base.Dispose(explicitDisposing);
            
            m_Base.Dispose();
        }

        #endregion
    }

    public static class WaitHandleExtensions
    {
        public static void TryWaitOnHandleAndDispose(ref System.Threading.WaitHandle handle)
        {

            if (handle == null) return;

            try
            {
                handle.WaitOne();
            }
            catch (System.ObjectDisposedException)
            {
                return;
            }
            catch (System.Exception ex)
            {
                Media.Common.Extensions.Exception.ExceptionExtensions.TryRaiseTaggedException(handle, "An exception occured while waiting.", ex);
            }
            finally
            {
                if (handle != null) handle.Dispose();
            }

            handle = null;
        }

        public static bool TrySignalHandle(System.Threading.WaitHandle handle, int timeoutMsec = (int)Media.Common.Extensions.TimeSpan.TimeSpanExtensions.MicrosecondsPerMillisecond, bool exitContext = false)
        {
            return System.Threading.WaitHandle.SignalAndWait(handle, handle, timeoutMsec, exitContext);
        }
    }
}
