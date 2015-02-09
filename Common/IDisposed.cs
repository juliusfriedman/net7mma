using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Common
{
    //ICanDispose / IShouldDispose

    //IDisposeAware
    #region IDisposed

    /// <summary>
    /// Defines an interface which expands on <see cref="IDisposable"/> with an IsDisposed property.
    /// Implementers of this type usually only throw <see cref="System.ObjectDisposedExcpetions"/> when desired, typically <see cref="BaseDisposable.CheckDisposed"/> is called to enfore this.
    /// </summary>
    public interface IDisposed : IDisposable
    {
        bool IsDisposed { get; }

        bool ShouldDispose { get; }
    }

    #endregion

    public static class IDisposedExtensions
    {
        //public static bool IsIDisposed(this IDisposable dispose) { return dispose != null && dispose is IDisposed; }

        public static bool IsDisposed(this IDisposed dispose) { return null == dispose || dispose.IsDisposed; }

        public static bool ShouldDisposed(this IDisposed dispose) { return dispose != null && dispose.ShouldDispose; }
    }

}
