using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Common
{
    /// <summary>
    /// Defines an interface [to a disposeable object] which allow access to an <see cref="Exception"/> and a user stored object which is related to the exception.
    /// </summary>
    public interface ITaggedException /*: IDisposed*/
    {
        /// <summary>
        /// <see cref="Exception.InnerException"/>.
        /// </summary>
        Exception InnerException { get; } //CurrentException

        /// <summary>
        /// The <see cref="System.Object"/> which corresponds to the underlying exception.
        /// </summary>
        Object Tag { get; }
    }

    //ITaggedExceptionExtensions
}
