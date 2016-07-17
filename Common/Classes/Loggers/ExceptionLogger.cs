using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Common.Classes.Loggers
{
    /// <summary>
    /// Not intended for use from your code. [for now]
    /// </summary>
    internal interface IException : Interfaces.Interface { }

    /// <summary>
    /// Not intended for use from your code. [for now]
    /// </summary>
    internal class ExceptionSpewer : Class, Interfaces.InterClass
    {
        /// <summary>
        /// `this`
        /// </summary>
        Class Interfaces.InterClass.Class
        {
            get { return this; }
        }

        /// <summary>
        /// A derivation of <see cref="Class"/>
        /// </summary>
        internal class ExceptionClass : Class { }

        /// <summary>
        /// Creates an instance of <see cref="System.Exception"/> with the specified <paramref name="message"/>
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>The <see cref="System.Exception"/> created.</returns>
        internal static System.Exception Spew(string message)
        {
            return new System.Exception(message);
        }
    }

    /// <summary>
    /// A derivation of <see cref="FormattableLogger"/> which uses <see cref="System.Exception"/> instances as the mechanism for which the underlying is achieved.
    /// </summary>
    public class ExceptionLogger : FormattableLogger
    {
        internal System.Exception LastLog, LastException;

        public override void Log(string message)
        {
            throw LastLog = ExceptionSpewer.Spew(FormatLog(message));
        }

        public override void LogException(Exception exception)
        {
            throw LastException = ExceptionSpewer.Spew(FormatException(exception.Message));
        }

        protected internal override void Dispose(bool disposing)
        {
            if (false.Equals(disposing) || false.Equals(ShouldDispose)) return;

            base.Dispose(disposing);

            if (IsDisposed.Equals(false)) return;

            LastLog = LastException = Media.Common.Extensions.Exception.ExceptionExtensions.NilException;
        }

    }
}
