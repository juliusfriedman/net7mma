using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Container.Mcf
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

                Skip(next.Size);
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
