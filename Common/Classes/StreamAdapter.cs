using System;
using System.Linq;

namespace Media.Common
{
    /// <summary>
    /// Provides the logic for caching a stream index and position, inherits from FileStream to allow backing from the file system. 
    /// should probably just inherit stream and provide an additional class for filestreams
    /// Allows Uri based identification and creation.
    /// Allows delete and move on close operations.
    /// </summary>
    public class StreamAdapter : System.IO.FileStream, IDisposed, IComposed<LifetimeDisposable>
    {
        #region Fields 
        
        internal readonly LifetimeDisposable m_Composed = new LifetimeDisposable(true);

        internal readonly protected Media.Common.Classes.FileInfoEx FileInfo;

        internal protected long m_Position, m_Length;

        readonly Uri m_Source;

        readonly Action AfterClose;

        #endregion

        #region Override Methods

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public override void SetLength(long value)
        {
            base.SetLength(m_Length = value);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Composed.Dispose(disposing);

                if (Precuror != null)
                {
                    if (Precuror.m_ViewAccessor != null)
                    {
                        Precuror.m_ViewAccessor.SafeMemoryMappedViewHandle.DangerousRelease();

                        Precuror.m_ViewAccessor.Dispose();
                    }

                    if (Precuror.m_File != null)
                    {
                        Precuror.m_File.Dispose();
                    }
                }

                base.Dispose(disposing);
            }
        }

        public override void Close()
        {
            if (IsDisposed) return;

            m_Position = m_Length = -1;

            base.Close();

            if (AfterClose != null) AfterClose();
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public override long Seek(long offset, System.IO.SeekOrigin origin) { try { return m_Position = base.Seek(offset, origin); } finally { RefreshFileInfo(); } }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public override int Read(byte[] buffer, int offset, int count) { try { int result = base.Read(buffer, offset, count); m_Position += result; return result; } finally { RefreshFileInfo(); } }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public override int ReadByte() { try { int result = base.ReadByte(); if (result != -1) ++m_Position; return result; } finally { RefreshFileInfo(); } }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public override void Write(byte[] array, int offset, int count)
        {
            try
            {
                long total = m_Position + count;
                base.Write(array, offset, count);
                if (total > m_Length) m_Length += total - m_Length;
                m_Position += count;
            }
            finally { RefreshFileInfo(); }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public override void WriteByte(byte value)
        {
            try
            {
                base.WriteByte(value);
                if (m_Position > m_Length) ++m_Length;
                ++m_Position;
            }
            finally { RefreshFileInfo(); }
        }

        #endregion

        #region Methods

        public virtual void RefreshFileInfo(bool updateLength = true)
        {
            if (FileInfo != null)
            {
                FileInfo.Refresh();

                if (updateLength && FileInfo.Exists) m_Length = FileInfo.Length;
            }
        }

        /// <summary>
        /// Reads the given amount of bytes into the buffer. If reading past the end of the stream an exception is thrown.
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public virtual byte[] ReadBytes(int count) { if (count <= 0) return Media.Common.MemorySegment.EmptyBytes; byte[] result = new byte[count]; int i = 0; while ((count -= (i += Read(result, i, count))) > 0);  return result; }

        //if(m_Position + count > m_Length) count = (m_Position + count - m_Length) to ensure correct amount of byes read

        /// <summary>
        /// Updates <see cref="Position"/> if the given value is positive.
        /// </summary>
        /// <param name="count">The amount of bytes to skip.</param>
        /// <returns><see cref="Position"/></returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public virtual long Skip(long count) { return count <= 0 ? Position : Position += count; }

        /// <summary>
        /// using a new FileStream with ReadOnly access a seek to the given position is performed and a subsequent read at the given position is performed.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns>The amount of bytes read</returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public virtual int ReadAt(long position, byte[] buffer, int offset, int count) { return ReadAt(position, buffer, offset, count, true); }
        public virtual int ReadAt(long position, byte[] buffer, int offset, int count, bool refreshFileInfo = true)
        {
            if (count == 0) return 0;

            using (var stream = new System.IO.FileStream(Name, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite))
            {
                if (position != stream.Seek(position, System.IO.SeekOrigin.Begin)) throw new InvalidOperationException("Unable to obtain the given position");

                int i = 0; while ((count -= (i += stream.Read(buffer, i, count))) > 0) ;

                RefreshFileInfo(refreshFileInfo);

                return count;
            }
        }

        /// <summary>
        /// using a new FileStream with WriteOnly access a seek to the given position is performed and a subsequent write at the given position is performed.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public virtual void WriteAt(long position, byte[] buffer, int offset, int count) { WriteAt(position, buffer, offset, count, true); }
        public virtual void WriteAt(long position, byte[] buffer, int offset, int count, bool refreshFileInfo = true)
        {
            if (count == 0) return;
            using (var stream = new System.IO.FileStream(Name, System.IO.FileMode.Open, System.IO.FileAccess.Write, System.IO.FileShare.ReadWrite))
            {
                if (position != stream.Seek(position, System.IO.SeekOrigin.Begin)) throw new InvalidOperationException("Unable to obtain the given position");

                stream.Write(buffer, offset, count);

                //Could manually update m_Lenth

                RefreshFileInfo(refreshFileInfo);
            }
        }

        /// <summary>
        /// using a new FileStream with WriteOnly access a seek to the end is performed and a subsequent write of the given data.
        /// <see cref="Length"/> is updated to reflect the operation when successful and <see cref="FileInfo"/> is Refreshed.
        /// </summary>
        /// <param name="buffer">The data to write</param>
        /// <param name="offset">The offset in data to begin writing</param>
        /// <param name="count">The amount of bytes from <paramref name="offset"/> within <paramref name="data"/></param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public virtual void Append(byte[] buffer, int offset, int count) { WriteAt(Length, buffer, offset, count); }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        internal protected long GetPosition() { return base.Position; }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        internal protected long GetLength() { return base.Length; }

        #endregion

        #region Properties

        public LifetimeDisposable Composed
        {
            get { return m_Composed; }
        }

        public bool IsDisposed
        {
            get { return Composed.IsDisposed; }
        }

        public bool ShouldDispose
        {
            get { return Composed.ShouldDispose; }
        }

        public Uri Source { get { return IsDisposed ? null : m_Source; } }

        public override long Position { get { return IsDisposed ? -1 : m_Position; } set { if (value == m_Position) return; Seek(value, System.IO.SeekOrigin.Begin); } }

        public override long Length { get { return IsDisposed ? -1 : m_Length; } }

        //-1 instead of 0 when disposed?
        public virtual long Remaining { get { return IsDisposed ? 0 : m_Length - m_Position; } }

        #endregion

        #region Constructor / Destructor

        ~StreamAdapter() { Dispose(); }

        #region Precurorc

        readonly Precurorc Precuror;

        internal class Precurorc
        {
            internal System.IO.MemoryMappedFiles.MemoryMappedFile m_File;

            internal System.IO.MemoryMappedFiles.MemoryMappedViewAccessor m_ViewAccessor;

            internal IntPtr m_Ptr;

            static System.IO.MemoryMappedFiles.MemoryMappedFile CreateMemoryMappedFile(string mapName, long capacity)
            {
                System.IO.MemoryMappedFiles.MemoryMappedFile PagedMemoryMapped = System.IO.MemoryMappedFiles.MemoryMappedFile.CreateNew(
                       mapName,                                                                   // Name
                       capacity,                                                                  // Size
                       System.IO.MemoryMappedFiles.MemoryMappedFileAccess.ReadWriteExecute,       // Access type
                       System.IO.MemoryMappedFiles.MemoryMappedFileOptions.DelayAllocatePages,    // Pseudo reserve/commit
                       new System.IO.MemoryMappedFiles.MemoryMappedFileSecurity(),                // You can customize the security
                       System.IO.HandleInheritability.Inheritable);                               // Inherit to child process

                return PagedMemoryMapped;
            }

            void CreateMemoryMappedHandle(string mapName, long capacity)
            {
                if (m_File != null) return;

                m_File = CreateMemoryMappedFile(mapName, capacity);

                m_ViewAccessor = m_File.CreateViewAccessor(0, capacity, System.IO.MemoryMappedFiles.MemoryMappedFileAccess.ReadWriteExecute);

                m_Ptr = m_ViewAccessor.SafeMemoryMappedViewHandle.DangerousGetHandle();
            }

            public Precurorc(string n, long c)
            {
                CreateMemoryMappedHandle(n, c);
            }
        }

        const string NullFile = "nul";

        const string SchemeSeperator = "://";

        public static System.IO.FileInfo NullFileInfo = new System.IO.FileInfo(NullFile);

        #endregion

        //Write, no Read
        static readonly IntPtr UnknownNullStream = new IntPtr(3);

        public StreamAdapter(System.IO.Stream a, string virtualName = null, long capacity = byte.MaxValue)
            : base(UnknownNullStream, System.IO.FileAccess.ReadWrite)//this(NullFileInfo.FullName, System.IO.FileAccess.ReadWrite)
        {
            FileInfo = NullFileInfo;
            //System.IO.StreamWriter.Null
            //System.IO.StreamReader.Null
            //Could apply new uri here "stream://"
            //Sould just also set the disposition
            //SafeFileHandle sfh = (SafeFileHandle)typeof(FileStream).GetField("_handle", BindingFlags.NonPublic | BindingFlags.Instance).GetValue((FileStream)YOUR_BINARY_READER.BaseStream)
        }

        //Giving a SocketHere is almost pointless even though it can be done, the problem comes when seeking which is what is desired anyway.

        //Could call reflection on a Composed instance, this would avoid the constructor nuiances and allow the handle to be set to anything desired
        //There is not really an upside inheriting from FileStream besides the free buffer, should probablly drop down to stream, add LastFlush semantics and then make my own pools for reading and writing
        //Then a FileStreamAdapter could inherit that and provide the FileStream Methods.

        /// <summary>
        /// Creates a new FileStream from the given
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="access"></param>
        public StreamAdapter(string filename, System.IO.FileAccess access = System.IO.FileAccess.Read, System.IO.FileInfo fileInfo = null) : this(new Uri(System.Uri.UriSchemeFile + SchemeSeperator + filename), access, fileInfo) { }

        /// <summary>
        /// Creates a new FileStream from the given
        /// </summary>
        /// <param name="location"></param>
        /// <param name="access"></param>
        public StreamAdapter(Uri location, System.IO.FileAccess access = System.IO.FileAccess.Read, System.IO.FileInfo fileInfo = null)
            : base(location.LocalPath, System.IO.FileMode.Open, access, System.IO.FileShare.ReadWrite)
        {
            m_Source = location;

            FileInfo = fileInfo ?? new System.IO.FileInfo(m_Source.LocalPath);

            m_Position = base.Position;

            m_Length = base.Length;
        }

        public StreamAdapter(System.IO.FileStream stream, System.IO.FileAccess access = System.IO.FileAccess.Read)
            : base(stream.SafeFileHandle, access)
        {
            m_Source = new Uri(stream.Name);
        }

        public StreamAdapter(System.IO.FileStream stream, Uri uri, System.IO.FileAccess access = System.IO.FileAccess.Read, bool ownsHandle = true)
            : base(stream.Handle, access, ownsHandle)
        {
            m_Source = uri ?? new Uri(stream.Name);
        }

        /// <summary>
        /// Creates a new FileStream from the given.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="access"></param>
        /// <param name="uri"></param>
        /// <param name="fi"></param>
        /// <param name="afterClose"></param>
        public StreamAdapter(System.IO.FileStream stream, System.IO.FileAccess access = System.IO.FileAccess.Read, Uri uri = null, System.IO.FileInfo fi = null, Action afterClose = null)
            : this(stream, uri, access)
        {
            try
            {
                AfterClose = afterClose;

                if (m_Source.IsFile)
                {
                    FileInfo = new System.IO.FileInfo(m_Source.LocalPath);

                    Position = stream.Position;

                    m_Length = stream.Length;
                }
                else if (fi == null)
                {
                    FileInfo = new System.IO.FileInfo(new System.IO.FileInfo(".").Directory.ToString() + m_Source.Segments.Last());
                }
                else FileInfo = fi;
            }
            catch { throw; }
        }

        #endregion   
  
        //Delete / Move on close is better implemented via action for best portablility

        [System.Runtime.InteropServices.DllImport("kernel32.dll", PreserveSig = false)]
        private static extern void SetFileInformationByHandle(Microsoft.Win32.SafeHandles.SafeFileHandle handle, int fileInformationClass, ref uint fileDispositionInfoDeleteFile, int bufferSize);


        private const int FileDispositionInfo = 4;

        internal static void PrepareDeleteOnCloseStreamForDisposal(System.IO.FileStream stream)
        {
            // tomat: Set disposition to "delete" on the stream, so to avoid ForeFront EndPoint
            // Protection driver scanning the file. Note that after calling this on a file that's open with DeleteOnClose, 
            // the file can't be opened again, not even by the same process.
            uint trueValue = 1;
            SetFileInformationByHandle(stream.SafeFileHandle, FileDispositionInfo, ref trueValue, sizeof(uint));
        }

        internal static void DeleteFileOnClose(string fullPath)
        {
            using (var stream = new System.IO.FileStream(fullPath, System.IO.FileMode.Open, System.IO.FileAccess.ReadWrite, System.IO.FileShare.Delete | System.IO.FileShare.ReadWrite, 8, System.IO.FileOptions.DeleteOnClose))
            {
                PrepareDeleteOnCloseStreamForDisposal(stream);
            }
        }
   
        //public static bool MarkAsSparseFile(System.IO.FileStream fileStream)
        //{
        //    int bytesReturned = 0;
        //    System.Threading.NativeOverlapped lpOverlapped = new System.Threading.NativeOverlapped();
        //    return DeviceIoControl(
        //    fileStream.SafeFileHandle,
        //    590020, //FSCTL_SET_SPARSE,
        //    IntPtr.Zero,
        //    0,
        //    IntPtr.Zero,
        //    0,
        //    ref bytesReturned,
        //    ref lpOverlapped);
        //}
    }
}
