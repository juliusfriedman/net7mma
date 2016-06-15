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

    //Destructable

    /// <summary>
    /// Provides an implementation which contains the members required to adhere to the IDisposable implementation.
    /// </summary>
    /// <remarks>
    /// Influenced by <see href="https://blogs.msdn.microsoft.com/blambert/2009/07/24/a-simple-and-totally-thread-safe-implementation-of-idisposable/">blambert's blog</see>. I might eventually change Dispose(bool) to ReleaseResources / etc.
    /// I also took some patterns from <see cref="https://blogs.msdn.microsoft.com/bclteam/2007/10/30/dispose-pattern-and-object-lifetime-brian-grunkemeyer/">BLC Team blog</see>
    /// </remarks>
    [CLSCompliant(true)]
    [System.Runtime.InteropServices.ComVisible(true)]
    public abstract class BaseDisposable : IDisposed
    {
        #region Constants / Statics

        /// <summary>
        /// The values which represent the state of the disposition of an instance.
        /// </summary>
        internal const int Finalized = -1, Undisposed = 0, Disposed = 1;

        /// <summary>
        /// If the sender is of the type <see cref="BaseDisposable"/> then <see cref="SetShouldDispose"/> will be called to dispose the instance immediately.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal protected static void SetShouldDisposeIfSenderIsBaseDisposableAndDisposeNow(object sender, EventArgs e)
        {
            if (false.Equals(sender == null) && sender is BaseDisposable) SetShouldDispose((sender as BaseDisposable), true, true);
        }

        /// <summary>
        /// Sets <see cref="ShouldDispose"/> to the given value and optionally calls <see cref="Dispose"/>.
        /// </summary>
        /// <param name="toDispose"></param>
        /// <param name="value"></param>
        /// <param name="callDispose"></param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static void SetShouldDispose(BaseDisposable toDispose, bool value, bool callDispose = false)
        {
            if (IDisposedExtensions.IsNullOrDisposed(toDispose)) return;

            toDispose.ShouldDispose = value;

            if (callDispose) toDispose.Dispose();
        }

        #endregion

        #region Fields

        //Todo, byte fields which are accessed by properties.

        /// <summary>
        /// Holds a value which indicates the state.
        /// </summary>
        long State; // = Undisposed;

        #endregion

        #region Constructor / Destructor

        /// <summary>
        /// Constructs a new BaseDisposable with <see cref="ShouldDispose"/> set to the given value.
        /// </summary>
        /// <param name="shouldDispose">The value of <see cref="ShouldDispose"/></param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        protected BaseDisposable(bool shouldDispose)
        {
            //Todo, should add flag for suppression?

            //If should not dispose then suppress the finalizer
            if (false.Equals((ShouldDispose = shouldDispose | Environment.HasShutdownStarted))) GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Finalizes the BaseDisposable, calls <see cref="Dispose"/> with the value of <see cref="ShouldDispose"/>.
        /// </summary>
        /// <remarks>If ever, only called when there are no more references to the object during a GC Collection.</remarks>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        ~BaseDisposable()
        {
            //Write state in Finalizer
            System.Threading.Interlocked.Exchange(ref State, Finalized);

            //Call the non virtual Destruct method.
            Destruct();

#if DEBUG
            System.Diagnostics.Debug.WriteLine(DateTime.UtcNow.ToFileTimeUtc() + "@" + GetType().Name + "\r\n" + ToString() + "\r\n@Finalize Completed");
#endif
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets a value indicating whether the object is undisposed.
        /// </summary>
        internal bool IsUndisposed
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get
            {
                //return System.Threading.Thread.VolatileRead(ref State) == Undisposed;
                return System.Threading.Interlocked.Read(ref State).Equals(Undisposed);
            }
        }

        /// <summary>
        /// Gets a value indicates if <see cref="Finalize"/> has been called.
        /// </summary>
        internal bool IsFinalized
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get
            {
                //return System.Threading.Thread.VolatileRead(ref State) == Finalized;
                return System.Threading.Interlocked.Read(ref State).Equals(Finalized);
            }
        }

        //Todo, Virtual overhead, Node is the only place this is really used.

        /// <summary>
        /// Indicates if Dispose has been called previously.
        /// </summary>
        public virtual bool IsDisposed { get; protected set; }

        /// <summary>
        /// Indicates if the instance should dispose any resourced when <see cref="Dispose"/> is called.
        /// </summary>
        public bool ShouldDispose { get; internal protected set; }

        #endregion

        #region Methods

        //This could instead be virtual and accept an optional bool to throw.
        /// <summary>
        /// Throws a System.ObjectDisposedException if <see cref="IsDisposed"/> is true and the Finalizer has yet not been called
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal protected void CheckDisposed() { if (false.Equals(IsUndisposed) || IsFinalized || IsDisposed) throw new ObjectDisposedException(GetType().Name); }

        //ReleaseResources
        /// <summary>
        /// Allows derived implemenations a chance to destory manged or unmanged resources.
        /// </summary>
        /// <param name="disposing">Indicates if resources should be destroyed</param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal protected virtual void Dispose(bool disposing)
        {
            //Do not dispose when ShouldDispose is false.
            if (false.Equals(disposing) || false.Equals(ShouldDispose)) return;

            //Mark the instance disposed if disposing
            //If the resources are to be removed then the finalizer has been called.
            //Compare and Swap State with Disposed if it was Undisposed.
            //Determine what to do based on what the State was
            switch (System.Threading.Interlocked.CompareExchange(ref State, Disposed, Undisposed))
            {
                case Undisposed:
                    {
                        goto case Finalized;
                    }
                case Finalized:
                    {
                        IsDisposed = true;

                        return;
                    }
            }
        }

        /// <summary>
        /// if <see cref="IsDisposed"/> returns, otherwise calls <see cref="Dispose"/> with the value of <see cref="ShouldDispose"/> and <see cref="GC.SuppressFinalize"/>
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        void Destruct()
        {
            //Call Dispose
            Dispose(ShouldDispose);

            //If not disposed return.
            if (false.Equals(IsUndisposed)) return;

            //Do not call the finalizer
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Allows derived implemenations a chance to destory manged or unmanged resources.
        /// Calls <see cref="GC.SuppressFinalize"/>
        /// Calls <see cref="Dispose"/> with the value of <see cref="ShouldDispose"/>.
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public virtual void Dispose()
        {
            if (IsFinalized || false.Equals(IsUndisposed) || false.Equals(ShouldDispose) || false.Equals(IsDisposed)) return;
            
            Destruct();
        }

        #endregion

        /// <summary>
        /// Allows derived implemenations a chance to destory manged or unmanged resources.
        /// Calls <see cref="Dispose"/> with the value of <see cref="ShouldDispose"/>
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        void IDisposable.Dispose()
        {
            Destruct();
        }

        /// <summary>
        /// Indicates if the instance is not yet disposed, only checks the virtual constraint if <see cref="State"/> indicates the instance is not already diposed or finalized.
        /// </summary>
        bool IDisposed.IsDisposed
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return false.Equals(IsUndisposed) && false.Equals(IsFinalized) && false.Equals(IsDisposed); }
        }

        //Ugly, double check already not Disposed but only through interface...
        //Determine if the property names should be named VirtualShouldDispose to distinguish this.
        /////// <summary>
        /////// Indicates if the instance should dispose any resourced when <see cref="Dispose"/> is called.
        /////// </summary>
        ////bool IDisposed.ShouldDispose
        ////{
        ////    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        ////    get
        ////    {
        ////        return ShouldDispose && IsUndisposed;
        ////    }
        ////}
    }

    #endregion
}

//Tests