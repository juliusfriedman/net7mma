using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Container
{
    /// <summary>
    /// Represents the basic logic around all media files including reading bytes and determining the amount of bytes remaining.
    /// Position and Length are cached to improve performance.
    /// </summary>
    public abstract class MediaFileStream : Common.BaseDisposable, Container.IMediaContainer
    {
        #region Fields

        Uri m_Source;

        internal System.IO.FileStream m_Stream;

        long m_Position, m_Length;

        #endregion

        #region Properties

        public Uri Source { get { return Disposed ? null : m_Source; } }

        public long Position { get { return Disposed ? -1 : m_Position; } set { if (value == m_Position) return; m_Position = m_Stream.Seek(value, System.IO.SeekOrigin.Begin); } }

        public long Length { get { return Disposed ? -1 : m_Length; } }

        public long Remaining { get { return Disposed ? 0 : m_Length - m_Position; } }

        public int Read(byte[] buffer, int offset, int count) { int result = m_Stream.Read(buffer, offset, count); m_Position += result; return result; }

        public int ReadByte() { int result = m_Stream.ReadByte(); if (result != -1) ++m_Position; return result; }

        public long Skip(long count) { return count <= 0 ? Position : Position += count; }

        public System.IO.Stream BaseStream { get { return m_Stream; } }

        #endregion

        #region Constructor / Destructor

        ~MediaFileStream() { Dispose(); }

        public MediaFileStream(string filename, System.IO.FileAccess access = System.IO.FileAccess.Read) : this(new Uri(filename), access) { }

        public MediaFileStream(Uri location, System.IO.FileAccess access = System.IO.FileAccess.Read)
        {
            if (location == null) throw new ArgumentNullException("location");
            else if (!location.IsFile) throw new InvalidOperationException("location must point to a file.");

            m_Source = location;

            if (!System.IO.File.Exists(m_Source.LocalPath)) throw new System.IO.FileNotFoundException("Could not find" + m_Source.LocalPath);

            m_Stream = new System.IO.FileStream(m_Source.LocalPath, System.IO.FileMode.Open, access, System.IO.FileShare.ReadWrite);
            
            m_Position = m_Stream.Position;

            m_Length = m_Stream.Length;
        }

        #endregion

        public override void Dispose()
        {
            if (Disposed) return;
            base.Dispose();
            m_Position = m_Length = -1;
            m_Stream.Dispose();
            m_Stream = null;
        }

        #region Abstraction

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

        #endregion

        #region IMediaContainer

        public Uri Location
        {
            get { return m_Source; }
        }

        public abstract Element Root { get; }

        public abstract Element TableOfContents { get; }

        public abstract IEnumerator<Element> GetEnumerator();
      
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}
