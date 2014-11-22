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
using Media.Container;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.Containers.Mcf
{
    /// <summary>
    /// Aims to provide an implementation of the Media Container Format.
    /// (.mcf, .av.mcf, .audio.mcf, .video.mcf)
    /// <see cref="http://en.wikipedia.org/wiki/Multimedia_Container_Format">MCF Wikipedia Entry</see>
    /// <see cref="http://mukoli.free.fr/mcf/">Unfinished Format Specification</see>
    /// </summary>
    /// <notes>Like Matroska but ALL Elements are fixed sized except the Block</notes>
    public class McfReader : MediaFileStream, IMediaContainer
    {

        public enum Identifiers
        {

        }

        #region Constants

        const int MinimumSize = 8, 
            HeaderSize = 0x1400, TypeHeaderSize = 160, ActualHeaderSize = 864, ExtendedInfoSize = 3072, ContentSpecificInfoSize = 1024,
            TrackEntrySize = 0x240, ClusterSize = 16, FooterSize = 4, BlockHeaderSize = 10;

        //Positions?

        #endregion

        #region Statics

        #endregion

        public McfReader(string filename, System.IO.FileAccess access = System.IO.FileAccess.Read) : base(filename, access) { }

        public McfReader(Uri source, System.IO.FileAccess access = System.IO.FileAccess.Read) : base(source, access) { }

        public McfReader(System.IO.FileStream source, System.IO.FileAccess access = System.IO.FileAccess.Read) : base(source, access) { }

        public Node ReadNext()
        {
            //Read Code?

            //switch(code)
            // determine length

            throw new NotImplementedException();
        }
        
        public override IEnumerator<Node> GetEnumerator()
        {
            while (Remaining >= MinimumSize)
            {
                Node next = ReadNext();

                if (next == null) yield break;

                yield return next;

                Skip(next.DataSize);
            }
        }

        public override IEnumerable<Track> GetTracks()
        {
            throw new NotImplementedException();
        }

        public override byte[] GetSample(Track track, out TimeSpan duration)
        {
            throw new NotImplementedException();
        }

        public override Node Root
        {
            get { throw new NotImplementedException(); }
        }

        public override Node TableOfContents
        {
            get { throw new NotImplementedException(); }
        }
    }
}
