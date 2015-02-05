using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Common
{
    /// <summary>
    /// Defines an interface which allow access to an <see cref="Exception"/> and a user stored object.
    /// </summary>
    public interface ITaggedException
    {
        /// <summary>
        /// <see cref="Exception.InnerException"/>.
        /// </summary>
        Exception InnerException { get; }

        /// <summary>
        /// The <see cref="System.Object"/> which corresponds to the underlying exception.
        /// </summary>
        Object Tag { get; }
    }
}
