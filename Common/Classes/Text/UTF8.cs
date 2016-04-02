﻿#region Copyright
/*
This file came from Managed Media Aggregation, You can always find the latest version @ https://net7mma.codeplex.com/
  
 Julius.Friedman@gmail.com / (SR. Software Engineer ASTI Transportation Inc. http://www.asti-trans.com)

Permission is hereby granted, free of charge, 
 * to any person obtaining a copy of this software and associated documentation files (the "Software"), 
 * to deal in the Software without restriction, 
 * including without limitation the rights to :
 * use, 
 * copy, 
 * modify, 
 * merge, 
 * publish, 
 * distribute, 
 * sublicense, 
 * and/or sell copies of the Software, 
 * and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * 
 * 
 * JuliusFriedman@gmail.com should be contacted for further details.

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. 
 * 
 * IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, 
 * DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, 
 * TORT OR OTHERWISE, 
 * ARISING FROM, 
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 * 
 * v//
 */
#endregion

namespace Media.Common
{
    [System.CLSCompliant(true)]
    public sealed class UTF8
    {
        public static readonly byte[] LineEndingBytes = new byte[] { Media.Common.ASCII.NewLine, Media.Common.ASCII.LineFeed }; //, Media.Common.ASCII.FormFeed

        public static readonly char[] LineEndingCharacters = System.Text.Encoding.UTF8.GetChars(LineEndingBytes);

        public static readonly byte[] WhiteSpaceBytes = new byte[] { Media.Common.ASCII.Space };

        public static readonly char[] WhiteSpaceCharacters = System.Text.Encoding.UTF8.GetChars(WhiteSpaceBytes);

        public static readonly byte[] SemiColonBytes = new byte[] { Common.ASCII.SemiColon };

        public static readonly char[] SemiColonCharacters = System.Text.Encoding.UTF8.GetChars(SemiColonBytes);

        public static readonly byte[] ColonBytes = new byte[] { Common.ASCII.Colon };

        public static readonly char[] ColonCharacters = System.Text.Encoding.UTF8.GetChars(ColonBytes);

        public static readonly byte[] ForwardSlashBytes = new byte[] { Common.ASCII.ForwardSlash };

        public static readonly char[] ForwardSlashCharacters = System.Text.Encoding.UTF8.GetChars(ForwardSlashBytes);

        public static readonly byte[] TabBytes = new byte[] { Common.ASCII.HorizontalTab };

        public static readonly char[] TabCharacters = System.Text.Encoding.UTF8.GetChars(TabBytes);
    }
}
