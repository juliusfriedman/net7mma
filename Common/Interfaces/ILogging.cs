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

namespace Media.Common
{
    /// <summary>
    /// Defines a common interface to support logging.
    /// </summary>
    public interface ILogging : IDisposable
    {
        /// <summary>
        /// Writes a message to the log.
        /// </summary>
        /// <param name="message">The message to write</param>
        void Log(string message);

        ///// <summary>
        ///// Writes an Exception to the log.
        ///// </summary>
        ///// <param name="ex">The exception to write</param>
        ///// <notes>Only provided to allow differentation between types of logging, users can just use Log </notes>
        void LogException(Exception ex);

        //Notice, Fatal, Information(al)/WhoCares
    }

    public static class ILoggingExtensions
    {
        public static void Log(this ILogging log, string message)
        {
            if (log == null) return;

            log.Log(message);
        }

        public static void LogException(this ILogging log, System.Exception exception)
        {
            if (log == null || exception == null) return;

            log.LogException(exception);
        }
    }
}
