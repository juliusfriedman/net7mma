using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Common
{
    /// <summary>
    /// Provides methods which allow detection of <see cref="System.Exception"/> state as well as methods to raise exceptions.
    /// </summary>
    public static class ExceptionExtensions
    {

        /// <summary>
        /// Check if we are in a exception unwind scenario or not.
        /// </summary>
        public static bool InException
        {
            get
            {   // Errata: The red marked code seems to be necessary. Since unit tests with .NET 2.0
                // have shown that only checking for the Exception Pointers structure does not always work.
                return System.Runtime.InteropServices.Marshal.GetExceptionPointers() == IntPtr.Zero && System.Runtime.InteropServices.Marshal.GetExceptionCode() == 0 ? false : true;
            }
        }

        /// <summary>
        /// Raises the given <see cref="TaggedException"/>
        /// </summary>
        /// <typeparam name="T">The type related to the exception.</typeparam>
        /// <param name="exception">The <see cref="System.Exception"/> which occured.</param>
        public static void Raise<T>(this TaggedException<T> exception) { if(exception != null) throw exception; }

        /// <summary>
        /// Tries to <see cref="Raise"/> the given <see cref="TaggedException"/>
        /// </summary>
        /// <typeparam name="T">The type related to the exception.</typeparam>
        /// <param name="exception">The <see cref="System.Exception"/> which occured.</param>
        
        public static void TryRaise<T>(this TaggedException<T> exception)
        {
            try { exception.Raise(); }
            catch { /*hide*/ }
        }

        /// <summary>
        /// Raises the given <see cref="TaggedException"/>
        /// </summary>
        /// <typeparam name="T">The type related to the exception.</typeparam>
        /// <param name="exception">The <see cref="System.Exception"/> which occured.</param>
        /// <param name="breakForResume">Indicates if the function should attach the debugger.</param>
        public static void RaiseAndAttachIfUnhandled<T>(this TaggedException<T> exception, bool breakForResume = true)
        {
            if (false == breakForResume)
            {
                exception.TryRaise();

                return;
            }

            //Raise the exception
            try { exception.Raise(); }
            catch //Handle it
            {
                //If the debugger is not attached and it cannot be then return
                if (false == System.Diagnostics.Debugger.IsAttached && false == System.Diagnostics.Debugger.Launch()) return; 

                //Break
                System.Diagnostics.Debugger.Break();
            }
        }

        //Resumeable?

        /// <summary>
        /// Raises an <see cref="Common.Exception"/> on the calling thread.
        /// </summary>
        /// <typeparam name="T">The type of the exception to raise.</typeparam>
        /// <param name="tag">The element related to the exception</param>
        /// <param name="message">The message realted to the exception, if not provided a default message will be used.</param>
        /// <param name="innerException">any <see cref="System.Exception"/> which is related to the exception being thrown</param>
        public static void RaiseTaggedException<T>(T tag, string message, Exception innerException = null) { new TaggedException<T>(tag, message ?? TaggedException<T>.DefaultExceptionTypeMessage<T>(), innerException).Raise(); }

        /// <summary>
        /// Tries to raises an <see cref="Common.Exception"/> on the calling thread and if the exception is not handled it will be discared.
        /// </summary>
        /// <typeparam name="T">The type of the exception to raise.</typeparam>
        /// <param name="tag">The element related to the exception</param>
        /// <param name="message">The message realted to the exception, if not provided a default message will be used.</param>
        /// <param name="innerException">any <see cref="System.Exception"/> which is related to the exception being thrown</param>
        public static void TryRaiseTaggedException<T>(T tag, string message, Exception innerException = null) { new TaggedException<T>(tag, message ?? TaggedException<T>.DefaultExceptionTypeMessage<T>(), innerException).TryRaise(); }
    }
}
