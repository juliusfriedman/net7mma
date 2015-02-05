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
            EqualsSign = 0x3d, // =
            HyphenSign = 0x2d, // -
            Comma = 0x2c, // ,
            Period = 0x2e, // .
            ForwardSlash = 0x2F, // '/'
            Colon = 0x3a, // :
            SemiColon = 0x3b, // ;
            AtSign = 0x40, // @
            R = 0x52, // 'R' = 82 Decimal
            BackSlash = 0x5C; // '\'
    }
}
