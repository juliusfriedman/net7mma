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
        public static void AbortAndFreeAll(this IThreadReference reference, int timeoutmSec = (int)Common.Extensions.TimeSpan.TimeSpanExtensions.MicrosecondsPerMillisecond)
        {
            foreach (var tp in reference.GetReferencedThreads())
            {
                System.Threading.Thread t = tp;
                Media.Common.Extensions.Thread.ThreadExtensions.TryAbortAndFree(ref t, System.Threading.ThreadState.Running, timeoutmSec);
            }
        }

        public static void AbortAndFreeAll(this IThreadReference reference, System.TimeSpan timeout)
        {
            foreach (var tp in reference.GetReferencedThreads())
            {
                System.Threading.Thread t = tp;
                Media.Common.Extensions.Thread.ThreadExtensions.TryAbortAndFree(ref t, timeout, System.Threading.ThreadState.Running);
            }
        }
    }
}
