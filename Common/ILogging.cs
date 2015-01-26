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
        //void LogException(Exception ex);

        //Notice, Fatal, Information(al)/WhoCares
    }
}
