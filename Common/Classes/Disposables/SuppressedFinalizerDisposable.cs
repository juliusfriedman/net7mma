using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Common
{
    /// <summary>
    /// Provides an implementation of the <see cref="BaseDisposable"/> with a supressed finalizer.
    /// </summary>
    /// <remarks>
    /// <see href="http://stackoverflow.com/questions/18020861/how-to-get-notified-before-static-variables-are-finalized/18316325#18316325">StackOverflow</see> some for details
    /// </remarks>
    public class SuppressedFinalizerDisposable : BaseDisposable
    {
        /// <summary>
        /// Determine if the object was created in the default app domain and which event handler to unhook at dispose
        /// </summary>
        bool DefaultAppDomain;

        /// <summary>
        /// Should never run unless immediately finalized.
        /// </summary>
        ~SuppressedFinalizerDisposable()
        {            

#if DEBUG
            if (DefaultAppDomain) AppDomain.CurrentDomain.ProcessExit -= SetShouldDisposeIfSenderIsBaseDisposableAndDisposeNow;
            else AppDomain.CurrentDomain.DomainUnload -= SetShouldDisposeIfSenderIsBaseDisposableAndDisposeNow;
#endif

            Dispose(ShouldDispose = true);

#if DEBUG
            System.Diagnostics.Debug.WriteLine(ToString() + "@Finalize Completed");
#endif
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public SuppressedFinalizerDisposable(bool shouldDispose)
            : base(shouldDispose)
        {
            //Suppress the finalizer always.
            GC.SuppressFinalize(this);

#if DEBUG
            if (DefaultAppDomain = AppDomain.CurrentDomain.IsDefaultAppDomain())
                AppDomain.CurrentDomain.ProcessExit += SetShouldDisposeIfSenderIsBaseDisposableAndDisposeNow;
            else
                AppDomain.CurrentDomain.DomainUnload += SetShouldDisposeIfSenderIsBaseDisposableAndDisposeNow;
#endif
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal protected override void Dispose(bool disposing)
        {
            //If already disposed or disposing and should not dispose return.
            if (IsDisposed || false == disposing || false == ShouldDispose) return;

            base.Dispose(disposing);
        }
    }
}