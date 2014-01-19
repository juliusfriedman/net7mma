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
using Octet = System.Byte;
using OctetSegment = System.ArraySegment<byte>;

#endregion

namespace Media.Common
{
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
        /// Indicates if the instance should dispose any resourced when disposed.
        /// </summary>
        public bool ShouldDispose { get; protected set; }

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
