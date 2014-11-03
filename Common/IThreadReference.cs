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
        IEnumerable<System.Threading.Thread> ReferencedThreads { get; }
    }

    /// <summary>
    /// Provides functions useful to <see cref="IThreadReference"/>
    /// </summary>
    public static class IThreadReferenceExtensions
    {
        public static void AbortAll(this IThreadReference reference, int timeoutSec)
        {
            foreach (var tp in reference.ReferencedThreads)
            {
                System.Threading.Thread t = tp;
                Utility.Abort(ref t, System.Threading.ThreadState.Running, timeoutSec);
            }
        }
    }
}
