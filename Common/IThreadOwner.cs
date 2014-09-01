using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Common
{
    public interface IThreadOwner
    {
        IEnumerable<System.Threading.Thread> OwnedThreads { get; }
    }


    public static class IThreadOwnerExtensions
    {
        public static void AbortAll(this IThreadOwner owner, int timeoutSec)
        {
            foreach (var tp in owner.OwnedThreads)
            {
                System.Threading.Thread t = tp;
                Utility.Abort(ref t, System.Threading.ThreadState.Running, timeoutSec);
            }
        }
    }
}
