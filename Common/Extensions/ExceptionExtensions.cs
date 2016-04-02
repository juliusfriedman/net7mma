#region Copyright
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Common.Extensions.Exception
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
            {
                //See http://geekswithblogs.net/akraus1/archive/2008/04/08/121121.aspx
                return System.Runtime.InteropServices.Marshal.GetExceptionPointers() == IntPtr.Zero && System.Runtime.InteropServices.Marshal.GetExceptionCode() == 0 ? false : true;
            }
        }

        //These methods should be in TaggegExceptionExtensions....

        /// <summary>
        /// Raises the given <see cref="TaggedException"/>
        /// </summary>
        /// <typeparam name="T">The type related to the exception.</typeparam>
        /// <param name="exception">The <see cref="System.Exception"/> which occured.</param>
        public static void Raise<T>(this TaggedException<T> exception) { if (exception != null) throw exception; }

        /// <summary>
        /// Tries to <see cref="Raise"/> the given <see cref="TaggedException"/>
        /// </summary>
        /// <typeparam name="T">The type related to the exception.</typeparam>
        /// <param name="exception">The <see cref="System.Exception"/> which occured.</param>
        
        public static void TryRaise<T>(this TaggedException<T> exception) //storeData
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
                if (false == Common.Extensions.Debug.DebugExtensions.Attach()) return;

                //Break if still attached
                Common.Extensions.Debug.DebugExtensions.BreakIfAttached();
            }
        }

        /// <summary>
        /// Raises an <see cref="Common.Exception"/> on the calling thread.
        /// </summary>
        /// <typeparam name="T">The type of the exception to raise.</typeparam>
        /// <param name="tag">The element related to the exception</param>
        /// <param name="message">The message realted to the exception, if not provided a default message will be used.</param>
        /// <param name="innerException">any <see cref="System.Exception"/> which is related to the exception being thrown</param>
        public static void RaiseTaggedException<T>(T tag, string message, System.Exception innerException = null) { new TaggedException<T>(tag, message ?? TaggedException<T>.DefaultExceptionTypeMessage<T>(), innerException).Raise(); }

        /// <summary>
        /// Tries to raises an <see cref="Common.Exception"/> on the calling thread and if the exception is not handled it will be discared.
        /// </summary>
        /// <typeparam name="T">The type of the exception to raise.</typeparam>
        /// <param name="tag">The element related to the exception</param>
        /// <param name="message">The message realted to the exception, if not provided a default message will be used.</param>
        /// <param name="innerException">any <see cref="System.Exception"/> which is related to the exception being thrown</param>
        public static void TryRaiseTaggedException<T>(T tag, string message, System.Exception innerException = null) { new TaggedException<T>(tag, message ?? TaggedException<T>.DefaultExceptionTypeMessage<T>(), innerException).TryRaise(); }

        //This is possibly the only one which would be useful here

        //http://stackoverflow.com/questions/3007608/resuming-execution-of-code-after-exception-is-thrown-and-caught
        //public static System.Exception ResumeOnError(Action action)
        //{
        //    try
        //    {
        //        action();
        //        return null;
        //    }
        //    catch (System.Exception caught)
        //    {
        //        return caught;
        //    }
        //}

    }
}
