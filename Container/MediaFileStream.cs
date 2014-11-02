using System;
using System.Collections.Generic;
namespace Media.Container
{
    /// <summary>
    /// Represents the basic logic around all media files including reading bytes and determining the amount of bytes remaining.
    /// Position and Length are cached to improve performance.
    /// </summary>
    public abstract class MediaFileStream : System.IO.FileStream, IDisposable, Container.IMediaContainer
    {
        #region Fields

        bool m_Disposed;

        Uri m_Source;

        System.IO.FileInfo m_FileInfo;

        internal protected long m_Position, m_Length;

        #endregion

        #region Properties

        public bool Disposed { get { return m_Disposed; } }

        public Uri Source { get { return Disposed ? null : m_Source; } }

        public override long Position { get { return Disposed ? -1 : m_Position; } set { if (value == m_Position) return; m_Position = base.Seek(value, System.IO.SeekOrigin.Begin); } }

        public override long Length { get { return Disposed ? -1 : m_Length; } }

        public long Remaining { get { return Disposed ? 0 : m_Length - m_Position; } }

        //public virtual byte[] ReadBytes(int count) { if(count <= 0) return Utility.Empty; byte[] result = new byte[count]; int i = 0; while((count -= (i += Read(result, i, count))) > 0);  return result; }

        public override int Read(byte[] buffer, int offset, int count) { int result = base.Read(buffer, offset, count); m_Position += result; return result; }

        public override int ReadByte() { int result = base.ReadByte(); if (result != -1) ++m_Position; return result; }

        public long Skip(long count) { return count <= 0 ? Position : Position += count; }

        protected System.IO.FileInfo FileInfo { get { return m_FileInfo; } }

        #endregion

        #region Constructor / Destructor

        ~MediaFileStream() { m_Disposed = true; Dispose(); }

        public MediaFileStream(string filename, System.IO.FileAccess access = System.IO.FileAccess.Read) : this(new Uri(filename), access) { }

        public MediaFileStream(Uri location, System.IO.FileAccess access = System.IO.FileAccess.Read)
            : base(location.LocalPath, System.IO.FileMode.Open, access, System.IO.FileShare.ReadWrite)
        {
            m_Source = location;

            m_FileInfo = new System.IO.FileInfo(m_Source.LocalPath);

            m_Position = base.Position;

            m_Length = base.Length;
        }

        //Used for segmenting a stream, another class might be more useful which would also allow a offset start position.
        internal MediaFileStream(MediaFileStream other, long? start, long? length, int bufferSize = 8192)
            : base(other.SafeFileHandle, System.IO.FileAccess.Read, bufferSize)
        {
            m_Source = other.m_Source;

            m_FileInfo = other.m_FileInfo;

            m_Position = start ?? other.Position;

            SetLength(m_Length = length ?? other.Length);
        }

        #endregion

        #region Methods

        public override void Close()
        {
            if (Disposed) return;
            m_Disposed = true;
            m_Position = m_Length = -1;
            m_Source = null;
            m_FileInfo = null;
            base.Close();
        }

        //public virtual MediaFileStream Fork(long offset, long count)
        //{
        //    return new MediaFileStream(this, offset - count);
        //}

        #endregion

        #region Abstraction

        /// <summary>
        /// Identifies the first <see cref="Node"/> found in the stream.
        /// </summary>
        public abstract Node Root { get; }

        //Move to explicit declaration?
        public abstract Node TableOfContents { get; }

        public abstract IEnumerator<Node> GetEnumerator();

        //Should abstract KnownExtensions? MimeTypes!!!!

        //Should abstract NodeIdentifierSize, NodeLengthSize(-1 for Variable?), and MinimumNodeSize, MaximumNodeSize?

        /// <summary>
        /// When overriden in a derived class, Provides information for each 'Track' in the Media
        /// </summary>
        public abstract IEnumerable<Track> GetTracks();

        /// <summary>
        /// When overriden in a derived class, retrieves the <see cref="Rtp.RtpFrame"/> related to the given parameters
        /// </summary>
        /// <param name="track">The <see cref="TrackReference"/> which identifies the Track to retrieve the sample data from</param>       
        /// <param name="duration">The amount of time related to the result</param>
        /// <returns>The <see cref="Rtp.RtpFrame"/> containing the sample data</returns>
        public abstract byte[] GetSample(Track track, out TimeSpan duration);

        //Enumerable of samples ?

        #endregion

        #region IMediaContainer

        public System.IO.Stream BaseStream { get { return this; } }

        public Uri Location { get { return m_Source; } }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }

        #endregion
    }
}
