﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Container.Real
{
    //Reads .rm files
    //http://wiki.multimedia.cx/?title=RealMedia
    public class RealReader : MediaFileStream, IMediaContainer
    {
        //FromRamFile

        public enum ChunkType
        {
            /*.*/RA/*0xFD*/, //RealAudio (1.0) Version = 3, (2.0) Version = 4
            // RealMedia file header (only one per file, must be the first chunk)
            /*.*/RMF,
            //File properties (only one per file)
            PROP,
            //Stream properties (one for each stream)
            MDPR,
            // Content description/metadata (typically one per file)
            CONT,
            //File data (Audio or Video)
            DATA,
            // File index (typically one per stream)
            INDX
        }

        #region Constants

        const int IdentifierSize = 4, LengthSize = 8, MinimumSize = IdentifierSize + LengthSize;

        #endregion

        #region Statics

        #endregion

        public RealReader(string filename, System.IO.FileAccess access = System.IO.FileAccess.Read) : base(filename, access) { }

        public RealReader(Uri source, System.IO.FileAccess access = System.IO.FileAccess.Read) : base(source, access) { }

        void ParseHeader()
        {
            using (var root = Root)
            {

            }
        }

        //PROP Properties only available is m_FileVersion > 4

        public IEnumerable<Node> ReadChunks(long offset, long count, params ChunkType[] chunkTypes)
        {
            long position = Position;

            foreach (var chunk in this)
            {
                if (chunkTypes == null || chunkTypes.Count() == 0 || chunkTypes.Contains((ChunkType)Common.Binary.Read32(chunk.Identifier, 0, !BitConverter.IsLittleEndian)))
                    yield return chunk;

                count -= chunk.DataSize;

                if (count <= 0) break;
            }

            Position = position;
        }

        public Node ReadChunk(ChunkType chunkType, long offset, long count)
        {
            long position = Position;

            Node result = ReadChunks(offset, count, chunkType).FirstOrDefault();

            Position = position;

            return result;
        }

        public override IEnumerable<Track> GetTracks()
        {
            //Determine Header Version

            //Switch and either Read all (MDPR) for RMF or
            //Read from Root for RA (only a single track)
            //for version 1.0 (3) codec is always [IpcJ] maynot be present

            throw new NotImplementedException();
        }

        public override byte[] GetSample(Track track, out TimeSpan duration)
        {
            throw new NotImplementedException();
        }

        public override Node Root
        {
            get { return ReadChunks(0, Length, ChunkType.RA, ChunkType.RMF).FirstOrDefault(); }
        }

        public override Node TableOfContents
        {
            get { using(var root = Root) return ReadChunks(root.Offset + root.DataSize, Length - root.Offset + root.DataSize, ChunkType.INDX).FirstOrDefault(); }
        }

        public Node ReadNext()
        {
            byte[] identifier = new byte[IdentifierSize];

            Read(identifier, 0, IdentifierSize);

            long length = 0;

            switch (identifier[2])
            {
                case (byte)'a':
                    {
                        //Header

                        //Get Version next word

                        int version = 0;

                        //HeaderSize
                        length = -8;

                        switch (version)
                        {
                            case 3:
                                {
                                    break;
                                }
                            case 4:
                                {

                                    //.ra4 sig
                                    
                                    //Next dWord
                                    length = 8;

                                    break;
                                }
                            default: throw new InvalidOperationException("Unknown Chunk Type");
                        }

                        break;
                    }
                default:
                    {
                        //Read ChunkSize inlcuding 8 byte prebmle
                        //+ word chunk_version
                        length = -8;
                        break;
                    }
            }

            return new Node(this, identifier, LengthSize, Position, length, length <= Remaining);
        }

        public override IEnumerator<Node> GetEnumerator()
        {
            while (Remaining >= 0)
            {
                Node next = ReadNext();

                if (next == null) yield break;

                yield return next;

                Skip(next.DataSize);
            }
        }
    }
}
