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

    //Provides a way to derermine how many classes hold a reference if the API was used.
    //public class ReferenceCountingDisposable : CommonDisposable
    //{
    //    int m_ReferenceCount;
    //}

    //Provide an event around Dispose
    //public class EventingDisposable : CommonDisposable
    //{
    //    Action DiposeEvent; // event Disposed;
    //}
    
}
