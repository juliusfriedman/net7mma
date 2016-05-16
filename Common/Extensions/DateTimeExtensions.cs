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

namespace Media.Common.Extensions.DateTime
{
    /// <summary>
    /// Defines methods for working with <see cref="System.DateTime"/>
    /// </summary>
    public static class DateTimeExtensions
    {
        /// <summary>  
        /// Gets the microsecond fraction of a DateTime.  
        /// </summary>  
        /// <param name="self"></param>  
        /// <returns></returns>  
        public static int Microseconds(this System.DateTime self)
        {
            return (int)System.Math.Floor((self.Ticks % System.TimeSpan.TicksPerMillisecond) / (double)Common.Extensions.TimeSpan.TimeSpanExtensions.OneMicrosecond.Ticks);
        }

        /// <summary>  
        /// Gets the Nanosecond fraction of a DateTime.  Note that the DateTime  
        /// object can only store nanoseconds at resolution of 100 nanoseconds.  
        /// </summary>  
        /// <param name="self">The DateTime object.</param>  
        /// <returns>the number of Nanoseconds.</returns>  
        public static int Nanoseconds(this System.DateTime self)
        {
            return (int)(self.Ticks % System.TimeSpan.TicksPerMillisecond % Common.Extensions.TimeSpan.TimeSpanExtensions.OneMicrosecond.Ticks) * Common.Extensions.TimeSpan.TimeSpanExtensions.NanosecondsPerTick;
        }

        /// <summary>  
        /// Adds a number of microseconds to this DateTime object.  
        /// </summary>  
        /// <param name="self">The DateTime object.</param>  
        /// <param name="microseconds">The number of milliseconds to add.</param>  
        public static System.DateTime AddMicroseconds(this System.DateTime self, int microseconds)
        {
            return self.AddTicks(microseconds * Common.Extensions.TimeSpan.TimeSpanExtensions.OneMicrosecond.Ticks);
        }

        /// <summary>  
        /// Adds a number of nanoseconds to this DateTime object.  Note: this  
        /// object only stores nanoseconds of resolutions of 100 seconds.  
        /// Any nanoseconds passed in lower than that will be rounded using  
        /// the default rounding algorithm in Math.Round().  
        /// </summary>  
        /// <param name="self">The DateTime object.</param>  
        /// <param name="nanoseconds">The number of nanoseconds to add.</param>  
        public static System.DateTime AddNanoseconds(this System.DateTime self, int nanoSeconds)
        {
            return self.AddTicks((int)System.Math.Round(nanoSeconds / (double)Common.Extensions.TimeSpan.TimeSpanExtensions.NanosecondsPerTick));
        }  
    }
}