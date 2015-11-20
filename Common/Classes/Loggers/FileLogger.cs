using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Common.Loggers
{
    /// <summary>
    /// A <see cref="ILogging"/> implementation which writes to a file.
    /// </summary>
    public class FileLogger : BaseLogger
    {

        #region Fields

        System.Text.Encoding m_Encoding = System.Text.Encoding.Default;

        System.IO.FileInfo m_FileInfo;

        #endregion

        #region Constructor

        public FileLogger(System.IO.FileInfo fileInfo)
        {
            m_FileInfo = fileInfo;
        }

        public FileLogger(string fileName)
            : this(new System.IO.FileInfo(fileName))
        {

        }

        ~FileLogger()
        {
            m_FileInfo = null;

            m_Encoding = null;
        }

        #endregion

        public System.Text.Encoding Encoding
        {
            get { return m_Encoding; }
            set
            {
                if (value == null) throw new ArgumentNullException();

                m_Encoding = value;
            }
        }

        public System.IO.FileInfo FileInfo { get { return m_FileInfo; } }

        /// <summary>
        /// Gets the binary representation of the given string.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="len"></param>
        /// <returns></returns>
        internal byte[] GetBytes(string message, out int len)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                len = 0;

                return Media.Common.MemorySegment.EmptyBytes;
            }

            byte[] data = m_Encoding.GetBytes(message + Environment.NewLine);

            len = data.Length;

            return data;
        }

        /// <summary>
        /// Attempts to write the given data to the log file.
        /// </summary>
        /// <param name="message"></param>
        internal void CoreWrite(string message)
        {
            try
            {
                using (var stream = m_FileInfo.Open(System.IO.FileMode.Append, System.IO.FileAccess.Write))
                {
                    int len;

                    stream.Write(GetBytes(message, out len), 0, len);
                }
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Attempts to write the given message to the log file
        /// </summary>
        /// <param name="message"></param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public override void Log(string message) { CoreWrite(message); }

        /// <summary>
        /// Attempts to write the given Exception to the log file.
        /// </summary>
        /// <param name="ex"></param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public override void LogException(Exception ex) { CoreWrite(ex.Message); }
    }
}
