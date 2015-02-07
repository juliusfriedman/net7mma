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
}
