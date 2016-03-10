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

#endregion

namespace Media.Common
{
    #region BaseDisposable

    /// <summary>
    /// Provides an implementation which contains the members required to adhere to the IDisposable implementation.
    /// </summary>
    [CLSCompliant(true)]
    [System.Runtime.InteropServices.ComVisible(true)]
    public abstract class BaseDisposable : IDisposed
    {
        //Not really needed
        public static bool IsNullOrDisposed(BaseDisposable toCheck) { return IDisposedExtensions.IsNullOrDisposed(toCheck); }
       
        internal static void SetShouldDisposeIfSenderIsBaseDisposable(object sender, EventArgs e)
        {
            if (sender != null && sender is BaseDisposable) SetShouldDispose((sender as BaseDisposable), true);
        }

        internal static void SetShouldDispose(BaseDisposable toDispose, bool dispose = false)
        {
            if (IDisposedExtensions.IsNullOrDisposed(toDispose)) return;
            toDispose.ShouldDispose = true;
            if (dispose) toDispose.Dispose();
        }

        /// <summary>
        /// Constructs a new BaseDisposable
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        protected BaseDisposable()
        {
            AppDomain.CurrentDomain.DomainUnload += SetShouldDisposeIfSenderIsBaseDisposable;
        }

        /// <summary>
        /// Constructs a new BaseDisposable with <see cref="ShouldDispose"/> set to the given value.
        /// </summary>
        /// <param name="shouldDispose">The value of <see cref="ShouldDispose"/></param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        protected BaseDisposable(bool shouldDispose)
        {
            ShouldDispose = shouldDispose || Environment.HasShutdownStarted;
        }

        /// <summary>
        /// Finalizes the BaseDisposable by calling Dispose.
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        ~BaseDisposable() { Dispose(); }

        /// <summary>
        /// Indicates if Dispose has been called previously.
        /// </summary>
        public virtual bool IsDisposed { get; protected set; }

        /// <summary>
        /// Indicates if the instance should dispose any resourced when disposed.
        /// </summary>
        public virtual bool ShouldDispose { get; internal protected set; }

        /// <summary>
        /// Throws a System.ObjectDisposedException if <see cref="IsDisposed"/> is true.
        /// </summary>
        internal protected void CheckDisposed() { if (IsDisposed) throw new ObjectDisposedException(GetType().Name); }

        /// <summary>
        /// Allows derived implemenations a chance to destory manged or unmanged resources.
        /// </summary>
        /// <param name="disposing">Indicates if resources should be destroyed</param>
        internal protected virtual void Dispose(bool disposing)
        {
            //If already IsDisposed return
            if (IsDisposed) return;

            //Mark the instance disposed
            IsDisposed = true;

            //If the resources are to be removed then the finalizer has been called.
            if (disposing)
            {
                //Do not call the finalizer
                GC.SuppressFinalize(this);
            }

            AppDomain.CurrentDomain.DomainUnload -= SetShouldDisposeIfSenderIsBaseDisposable;
        }

        /// <summary>
        /// Allows derived implemenations a chance to destory manged or unmanged resources.
        /// Calls <see cref="Dispose"/> with the value of <see cref="ShouldDispose"/>
        /// </summary>
        public virtual void Dispose()
        {
            Dispose(ShouldDispose);
        }
    }

    #endregion
}
