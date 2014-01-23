using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Common
{
    /// <see href="http://www.asciitable.com/">The ASCII Table</see>
    public sealed class ASCII
    {
        public const byte Space = 0x20,// ` `
            LineFeed = 0x0A, // `\n` => 10 Decimal
            NewLine = 0x0D, // `\r` => 13 Decimal
            Equals = 0x3d,
            R = 0x52; // 'R' = 82 Decimal
    }
}
