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

namespace Media.Common.Classes.Loggers
{
    /// <summary>
    /// Represents an <see cref="Abstraction"/> over <see cref="Media.Common.Loggers.BaseLogger"/> which adds the ability to format.
    /// </summary>
    public abstract class FormattableLogger : Media.Common.Loggers.BaseLogger, IAbstract
    {
        /// <summary>
        /// The format used for <see cref="Log"/>
        /// </summary>
        public string LogFormat { get; protected set; }

        /// <summary>
        /// The format used for <see cref="LogException"/>
        /// </summary>
        public string ExceptionFormat { get; protected set; }

        /// <summary>
        /// A call to <see cref="string.Format"/>.
        /// </summary>
        /// <param name="format">A composite format string</param>
        /// <param name="toFormat">The object to format</param>
        /// <returns>The formatted string.</returns>
        public static string Format(string format, string toFormat)
        {
            return string.Format(format, toFormat);
        }

        /// <summary>
        /// A call to <see cref="Format"/> with <see cref="LogFormat"/>
        /// </summary>
        /// <param name="log">The log</param>
        /// <returns>The formatted string</returns>
        public string FormatLog(string log)
        {
            return Format(LogFormat, log);
        }

        /// <summary>
        /// A call to <see cref="Format"/> with <see cref="ExceptionFormat"/>
        /// </summary>
        /// <param name="exception">The exception</param>
        /// <returns>The formatted string</returns>
        public string FormatException(string exception)
        {
            return Format(ExceptionFormat, exception); 
        }
    }
}
