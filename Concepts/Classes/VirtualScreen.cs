using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Concepts.Classes
{
    public class VirtualScreen
    {
        TimeSpan RefreshRate;

        bool VerticalSync;

        int Width, Height;
        
        Common.MemorySegment DisplayMemory, BackBuffer, DisplayBuffer;
    }
}
