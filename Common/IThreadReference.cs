using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Common
{
    /// <summary>
    /// Provides an interface which allows access to any <see cref="System.Threading.Thread"/> owned by an implementer of this interface.
    /// </summary>
    public interface IThreadReference
    {
        IEnumerable<System.Threading.Thread> GetReferencedThreads();
    }

    /// <summary>
    /// Provides functions useful to <see cref="IThreadReference"/>
    /// </summary>
    public static class IThreadReferenceExtensions
    {
        public static void AbortAll(this IThreadReference reference, int timeoutmSec = (int)Common.Extensions.TimeSpan.TimeSpanExtensions.MicrosecondsPerMillisecond)
        {
            foreach (var tp in reference.GetReferencedThreads())
            {
                System.Threading.Thread t = tp;
                Media.Common.IThreadReferenceExtensions.TryAbort(ref t, System.Threading.ThreadState.Running, timeoutmSec);
            }
        }

        public static void Abort(ref System.Threading.Thread thread, System.Threading.ThreadState state = System.Threading.ThreadState.Stopped, int timeout = (int)Common.Extensions.TimeSpan.TimeSpanExtensions.MicrosecondsPerMillisecond)
        {
            //If the worker IsAlive and has the requested state.
            if (thread != null && (thread.IsAlive && thread.ThreadState.HasFlag(state)))
            {
                //Attempt to join
                if (false == thread.Join(timeout))
                {
                    try
                    {
                        //Abort
                        thread.Abort();
                    }
                    catch (System.Threading.ThreadAbortException) { System.Threading.Thread.ResetAbort(); }
                    catch { throw; } //Cancellation not supported
                }

                //Reset the state of the thread to indicate success
                thread = null;
            }
        }

        public static void Abort(ref System.Threading.Thread thread, System.TimeSpan timeout, System.Threading.ThreadState state = System.Threading.ThreadState.Stopped)
        {
            //If the worker IsAlive and has the requested state.
            if (thread != null && (thread.IsAlive && thread.ThreadState.HasFlag(state)))
            {
                //Attempt to join
                if (false == thread.Join(timeout))
                {
                    try
                    {
                        //Abort
                        thread.Abort();
                    }
                    catch (System.Threading.ThreadAbortException) { System.Threading.Thread.ResetAbort(); }
                    catch { throw; } //Cancellation not supported
                }

                //Reset the state of the thread to indicate success
                thread = null;
            }
        }

        public static bool TryAbort(ref System.Threading.Thread thread, System.Threading.ThreadState state = System.Threading.ThreadState.Stopped, int timeout = 1000)
        {
            try { Abort(ref thread, state, timeout); }
            catch { return false; }

            return thread == null;
        }
    }
}
