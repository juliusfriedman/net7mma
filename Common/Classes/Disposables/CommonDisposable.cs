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
        
        //Could store Created time?

        //Should be on base class...

        //public readonly DateTime Created = DateTime.UtcNow;

        public CommonDisposable(bool shouldDispose) : base(shouldDispose) { }
    }

    //public class ReferenceCountingDisposable : CommonDisposable
    //{
    //    int m_ReferenceCount;
    //}
    
}
