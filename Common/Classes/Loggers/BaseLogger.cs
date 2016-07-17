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

namespace Media.Common.Loggers
{
    /// <summary>
    /// Represents the basic required implemenation as required by <see cref="ILogging"/>
    /// </summary>
    public abstract class BaseLogger : SuppressedFinalizerDisposable, ILogging
    {
        /// <summary>
        /// Creates an instance which which may or may not be disposed based on the value of <paramref name="shouldDispose"/>
        /// </summary>
        /// <param name="shouldDispose">Indicates if the instance should dispose when <see cref="Dispose"/> is called.</param>
        public BaseLogger(bool shouldDispose) : base(shouldDispose) { }

        /// <summary>
        /// Creates an instance which will dispose when <see cref="Dispose"/> is called.
        /// </summary>
        public BaseLogger() : this(true) { }

        /// <summary>
        /// Log the <paramref name="message"/>
        /// </summary>
        /// <param name="message">The message</param>
        public abstract void Log(string message);

        /// <summary>
        /// Log the <paramref name="exception"/>
        /// </summary>
        /// <param name="exception">The exception</param>
        public abstract void LogException(System.Exception exception);
    }
}
