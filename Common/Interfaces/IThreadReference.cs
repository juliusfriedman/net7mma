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

        /// <summary>
        /// Provides a function which configures a thread as required.
        /// </summary>
        //Action<System.Threading.Thread> ConfigureThread { get; }
    }

    /// <summary>
    /// Provides functions useful to <see cref="IThreadReference"/>
    /// </summary>
    public static class IThreadReferenceExtensions
    {
        public static void AbortAndFreeAll(this IThreadReference reference, int timeoutmSec = (int)Common.Extensions.TimeSpan.TimeSpanExtensions.MicrosecondsPerMillisecond)
        {
            foreach (var tp in reference.GetReferencedThreads())
            {
                //Take a reference to that thread
                System.Threading.Thread t = tp;

                //Call AbortAndFree                                                    //Should be Stopped to check for stop?
                if (false == Media.Common.Extensions.Thread.ThreadExtensions.TryAbortAndFree(ref t, System.Threading.ThreadState.Running, timeoutmSec)) t = null; //Remove the reference if required
            }
        }

        public static void AbortAndFreeAll(this IThreadReference reference, System.TimeSpan timeout)
        {
            foreach (var tp in reference.GetReferencedThreads())
            {
                //Take a reference to that thread
                System.Threading.Thread t = tp;

                //Call AbortAndFree                                                    //Should be Stopped to check for stop?
                if (false == Media.Common.Extensions.Thread.ThreadExtensions.TryAbortAndFree(ref t, timeout, System.Threading.ThreadState.Running)) t = null; //Remove the reference if required
            }
        }
    }
}
