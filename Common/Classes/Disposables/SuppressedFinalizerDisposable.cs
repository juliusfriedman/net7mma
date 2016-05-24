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

            //Catch domain shutdown (Hack: frantically look for things we can catch)
            if (DefaultAppDomain = AppDomain.CurrentDomain.IsDefaultAppDomain())
                AppDomain.CurrentDomain.ProcessExit += SetShouldDisposeIfSenderIsBaseDisposableAndDisposeNow;
            else
                AppDomain.CurrentDomain.DomainUnload += SetShouldDisposeIfSenderIsBaseDisposableAndDisposeNow;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal protected override void Dispose(bool disposing)
        {
            //If already disposed or disposing and should not dispose return.
            if (IsDisposed || disposing && false == ShouldDispose) return;

            //Mark disposed.
            IsDisposed = disposing;

            //http://stackoverflow.com/questions/18020861/how-to-get-notified-before-static-variables-are-finalized/18316325#18316325
            if (DefaultAppDomain) AppDomain.CurrentDomain.ProcessExit -= SetShouldDisposeIfSenderIsBaseDisposableAndDisposeNow;
            else AppDomain.CurrentDomain.DomainUnload -= SetShouldDisposeIfSenderIsBaseDisposableAndDisposeNow;
        }
    }
}