using System;
using System.Linq;
using System.Collections.Generic;
namespace Media.Container
{
    /// <summary>
    /// Represents the basic logic around all media files including reading bytes and determining the amount of bytes remaining.
    /// Position and Length are cached to improve performance.
    /// </summary>
    public abstract class MediaFileStream : System.IO.FileStream, IDisposable, Container.IMediaContainer
    {

        #region Statics

        static Dictionary<string, MediaFileStream> m_ExtensionMap = new Dictionary<string, MediaFileStream>();

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public static bool TryRegisterExtension(string extenstion, MediaFileStream implementation)
        {
            if (string.IsNullOrWhiteSpace(extenstion)) return false;

            if (extenstion[0] == (char)Common.ASCII.Period) extenstion = extenstion.Substring(1);

            try
            {
                m_ExtensionMap.Add(extenstion, implementation);
                return true;
            }
            catch
            {
                return false;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public static bool TryUnRegisterExtension(string extenstion)
        {
            if (string.IsNullOrWhiteSpace(extenstion)) return false;

            if (extenstion[0] == (char)Common.ASCII.Period) extenstion = extenstion.Substring(1);

            return m_ExtensionMap.Remove(extenstion);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public static IEnumerable<string> GetRegisteredExtensions() { return m_ExtensionMap.Keys; }

        static Type MediaFileStreamType = typeof(MediaFileStream);

        static Type[] ConstructorTypes = new Type[] { typeof(string), typeof(System.IO.FileAccess) };

        public static MediaFileStream GetCompatbileImplementation(string fileName, AppDomain domain = null)
        {

            if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentNullException("fileName");

            //Determine if to use Extension first?

            //Get all loaded assemblies in the current application domain
            foreach (var assembly in (domain ?? AppDomain.CurrentDomain).GetAssemblies())
            {
                //Iterate each derived type which is a SubClassOf RtcpPacket.
                foreach (var derivedType in assembly.GetTypes().Where(t => t.IsSubclassOf(MediaFileStreamType)))
                {
                    //If the derivedType is an abstraction then add to the AbstractionBag and continue
                    if (derivedType.IsAbstract) continue;

                    //Todo - Don't create the File Handle multiple times, use the existing one.

                    MediaFileStream created = (MediaFileStream)derivedType.GetConstructor(ConstructorTypes).Invoke(new object[] { fileName });

                    var rootNode = created.Root;

                    if (rootNode != null && rootNode.IsComplete) return created;

                    created.Dispose();
                }
            }

            return null;
        }

        #endregion

        #region Fields

        bool m_Disposed;

        Uri m_Source;

        internal protected System.IO.FileInfo FileInfo;

        internal protected long m_Position, m_Length;

        #endregion

        #region Properties

        public bool Disposed { get { return m_Disposed; } }

        public Uri Source { get { return Disposed ? null : m_Source; } }

        public override long Position { get { return Disposed ? -1 : m_Position; } set { if (value == m_Position) return; Seek(value, System.IO.SeekOrigin.Begin); } }

        public override long Length { get { return Disposed ? -1 : m_Length; } }

        public virtual long Remaining { get { return Disposed ? 0 : m_Length - m_Position; } }

        public virtual byte[] ReadBytes(int count) { if(count <= 0) return Utility.Empty; byte[] result = new byte[count]; int i = 0; while((count -= (i += Read(result, i, count))) > 0);  return result; }

        /// <summary>
        /// Updates <see cref="Position"/> if the given value is positive.
        /// </summary>
        /// <param name="count">The amount of bytes to skip.</param>
        /// <returns><see cref="Position"/></returns>
        public virtual long Skip(long count) { return count <= 0 ? Position : Position += count; }

        /// <summary>
        /// using a new FileStream with WriteOnly access a seek to the end is performed and a subsequent write of the given data.
        /// <see cref="Length"/> is updated to reflect the operation when successful and <see cref="FileInfo"/> is Refreshed.
        /// </summary>
        /// <param name="buffer">The data to write</param>
        /// <param name="offset">The offset in data to begin writing</param>
        /// <param name="count">The amount of bytes from <paramref name="offset"/> within <paramref name="data"/></param>
        public virtual void Append(byte[] buffer, int offset, int count)
        {
            if (count > 0)
            {
                using (var appender = FileInfo.OpenWrite())
                {
                    appender.Seek(0, System.IO.SeekOrigin.End);
                    appender.Write(buffer, offset, count);
                    m_Length += count;
                    FileInfo.Refresh();
                }
            }
        }

        /// <summary>
        /// Reads data at the given position
        /// </summary>
        /// <param name="position"></param>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns>The amount of bytes read</returns>
        public virtual int AbsoluteRead(long position, byte[] buffer, int offset, int count)
        {
            if (position + count > Length) throw new InvalidOperationException("position and count must reflect data accessible by Position and Length");
            if (count == 0) return 0;
            using (var stream = FileInfo.OpenRead())
            {
                stream.Seek(position, System.IO.SeekOrigin.Begin);

                int i = 0; while ((count -= (i += stream.Read(buffer, i, count))) > 0) ;

                FileInfo.Refresh();

                return count;
            }            
        }

        public virtual int AbsoluteWrite(long position, byte[] buffer, int offset, int count)
        {
            if (position + count > Length) throw new InvalidOperationException("position and count must reflect data accessible by Position and Length");
            if (count == 0) return 0;
            using (var stream = FileInfo.OpenWrite())
            {
                stream.Seek(position, System.IO.SeekOrigin.Begin);

                stream.Write(buffer, offset, count);

                FileInfo.Refresh();

                return count;
            }           
        }

        #endregion

        #region Constructor / Destructor

        ~MediaFileStream() { m_Disposed = true; Close(); }

        public MediaFileStream(string filename, System.IO.FileAccess access = System.IO.FileAccess.Read) : this(new Uri(filename), access) { }

        public MediaFileStream(Uri location, System.IO.FileAccess access = System.IO.FileAccess.Read)
            : base(location.LocalPath, System.IO.FileMode.Open, access, System.IO.FileShare.ReadWrite)
        {
            m_Source = location;

            FileInfo = new System.IO.FileInfo(m_Source.LocalPath);

            m_Position = base.Position;

            m_Length = base.Length;
        }

        #endregion

        #region Overrides Methods

        public override void Close()
        {
            if (Disposed) return;
            m_Disposed = true;
            m_Position = m_Length = -1;
            m_Source = null;
            FileInfo = null;
            base.Close();
        }

        public override long Seek(long offset, System.IO.SeekOrigin origin) { return m_Position = base.Seek(offset, origin); }

        public override int Read(byte[] buffer, int offset, int count) { int result = base.Read(buffer, offset, count); m_Position += result; return result; }

        public override int ReadByte() { int result = base.ReadByte(); if (result != -1) ++m_Position; return result; }        

        public override void Write(byte[] array, int offset, int count)
        {
            if (m_Position == m_Length) m_Length += count;
            base.Write(array, offset, count);
            m_Position += count;
            FileInfo.Refresh();
        }

        public override void WriteByte(byte value)
        {
            if (m_Position == m_Length) ++m_Length;
            base.WriteByte(value);
            ++m_Position;
            FileInfo.Refresh();
        }
        public override IAsyncResult BeginWrite(byte[] array, int offset, int numBytes, AsyncCallback userCallback, object stateObject)
        {
            //If at the end of the stream when done writing then update positions
            if (m_Position == m_Length) userCallback = (AsyncCallback)AsyncCallback.Combine(new Action(() =>
            {
                //Update Positions
                m_Length += numBytes;
                m_Position += numBytes;
                FileInfo.Refresh();
            }), userCallback);

            return base.BeginWrite(array, offset, numBytes, userCallback, stateObject);
        }

        public override System.Threading.Tasks.Task WriteAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken)
        {
            System.Threading.Tasks.Task t = base.WriteAsync(buffer, offset, count, cancellationToken);

            if (m_Position == m_Length) t.ContinueWith(task =>
            {
                if (!task.IsCanceled && !task.IsFaulted && task.IsCompleted)
                {
                    //Update Positions
                    m_Length += count;
                    m_Position += count;
                    FileInfo.Refresh();
                }
            });

            return t;
        }       
        
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
