using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Common
{
    /// <summary>
    /// Provides a default implementation of the <see cref="BaseDisposable"/>
    /// </summary>
    public class CommonDisposable : BaseDisposable
    {
        public CommonDisposable(bool shouldDispose)
        {
            ShouldDispose = shouldDispose;
        }
    }

    //public class DisposableContext : System.Threading.SynchronizationContext, IDisposed
    //{

    //    public readonly CommonDisposable DisposePoint;

    //    public DisposableContext(bool shouldDispose)
    //        : base()
    //    {
    //        DisposePoint = new CommonDisposable(shouldDispose);
    //    }


    //    bool IDisposed.IsDisposed
    //    {
    //        get { return DisposePoint.IsDisposed; }
    //    }

    //    bool IDisposed.ShouldDispose
    //    {
    //        get { return DisposePoint.ShouldDispose; }
    //    }

    //    void IDisposable.Dispose()
    //    {
    //        DisposePoint.Dispose();
    //    }
    //}
}
