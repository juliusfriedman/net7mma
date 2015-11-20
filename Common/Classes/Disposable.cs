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

namespace Media.Common
{
    /// <summary>
    /// Wraps the IDisposable object interface for classes that desire to be sure of being called 
    /// a single time for the dispose.
    /// </summary>
    /// <see href="https://code.google.com/p/csharptest-net/source/browse/src/Library/Bases/Disposable.cs">csharptest-net</see>
    public abstract class Disposable : System.IDisposable
    {
        #region Private Fields

        private bool m_IsDisposed;

        private event System.EventHandler DisposedEvent;

        #endregion

        #region Public Properties

        public bool IsDisposed { get { return m_IsDisposed; } }

        #endregion

        #region Constructor / Finalizer

        /// <summary> </summary>
        protected Disposable()
        {
            m_IsDisposed = false;
        }

        /// <summary> last-chance dispose </summary>
        ~Disposable()
        {
            try { OnDispose(false); }
            catch (System.Exception ex)
            {
                Common.Extensions.Exception.ExceptionExtensions.RaiseTaggedException(this, ex.Message, ex);
            }
        }

        #endregion

        #region Public Methods

        /// <summary> disposes of the object if it has not already been disposed </summary>
        public void Dispose()
        {
            try { OnDispose(true); }

            finally { System.GC.SuppressFinalize(this); }
        }

        #endregion

        #region Private Methods

        private void OnDispose(bool disposing)
        {

            try
            {

                if (!m_IsDisposed)
                {

                    Dispose(disposing);

                    if (DisposedEvent != null) DisposedEvent(this, System.EventArgs.Empty);
                }

            }

            finally
            {

                m_IsDisposed = true;

                DisposedEvent = null;

            }

        }

        #endregion

        /// <summary> Raised when the object is disposed </summary>
        public event System.EventHandler Disposed
        {

            add { Assert(); DisposedEvent += value; }

            remove { DisposedEvent -= value; }

        }

        #region Protected Methods

        /// <summary> Raises the ObjectDisposedException if this object has already been disposed </summary>
        protected virtual void Assert(bool enforce = true)
        {

            if (enforce && m_IsDisposed) throw new System.ObjectDisposedException(GetType().FullName);
        }

        #endregion

        /// <summary> Your implementation of the dispose method </summary>
        protected abstract void Dispose(bool disposing);
    }
}
