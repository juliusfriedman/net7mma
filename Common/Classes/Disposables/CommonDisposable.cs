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

        /// <summary>
        /// Creates an instance
        /// </summary>
        /// <param name="shouldDispose">Indicates if <see cref="Dispose"/> will change the state of the instance</param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public CommonDisposable(bool shouldDispose = true) : base(shouldDispose) { }
    }

    //Could provide hooks to determine manually... (SOS wrapper seems like a seperately useful class)
    //Should provide for Mono and other CLR's also.

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


//Tests