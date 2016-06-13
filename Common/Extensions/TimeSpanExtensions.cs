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

namespace Media.Common.Extensions.TimeSpan
{
    /// <summary>
    /// Defines methods and properties for working with <see cref="System.TimeSpan"/>
    /// </summary>
    public static class TimeSpanExtensions
    {
        //TimeSpan.MinValue is already defined as
        //public static System.TimeSpan Undefined = System.TimeSpan.FromTicks(unchecked((long)double.NaN));
        public const double MicrosecondsPerMillisecond = 1000,
            NanosecondsPerMicrosecond = MicrosecondsPerMillisecond,//1000,
            NanosecondsPerMillisecond = 1000000, //MicrosecondsPerMillisecond * MicrosecondsPerMillisecond, 
            NanosecondsPerSecond = 1000000000;

        /// <summary>  
        /// The number of ticks per Nanosecond.  
        /// </summary>  
        public const int NanosecondsPerTick = 100;

        /// <summary>
        /// The number of ticks per Microsecond.
        /// </summary>
        public const long TicksPerMicrosecond = 10;

        //const long would be a suitable replacement, then would use the .Ticks property of the instance.
        //public const long InfiniteTicks = -1;

        //readonly ValueType....

        /// <summary>
        /// A <see cref="System.TimeSpan"/> with the value of -1 Millisecond
        /// </summary>
        public static readonly System.TimeSpan InfiniteTimeSpan = System.Threading.Timeout.InfiniteTimeSpan;

        /// <summary>
        /// A <see cref="System.TimeSpan"/> with the value of 1 Tick (100 ns)
        /// </summary>
        public static readonly System.TimeSpan OneTick = System.TimeSpan.FromTicks(1);

        /// <summary>
        /// A <see cref="System.TimeSpan"/> with the value of 2 Tick's (200 ns)
        /// </summary>
        public static readonly System.TimeSpan TwoHundedNanoseconds = System.TimeSpan.FromTicks(2);

        /// <summary>
        /// A <see cref="System.TimeSpan"/> with the value of 1 Second
        /// </summary>
        public static readonly System.TimeSpan OneSecond = System.TimeSpan.FromSeconds(1);

        /// <summary>
        /// A <see cref="System.TimeSpan"/> with the value of 1 Millisecond
        /// </summary>
        public static readonly System.TimeSpan OneMillisecond = InfiniteTimeSpan.Negate();

        /// <summary>
        /// A <see cref="System.TimeSpan"/> with the value of 1 Microsecond (μs)
        /// </summary>
        public static readonly System.TimeSpan OneMicrosecond = System.TimeSpan.FromTicks(10);

        /// <summary>
        /// A <see cref="System.TimeSpan"/> with the value of 1 Hour
        /// </summary>
        public static readonly System.TimeSpan OneHour = System.TimeSpan.FromHours(1);

        /// <summary>
        /// Calulcates the total amount of Microseconds (μs) in the given <see cref="TimeSpan"/>
        /// </summary>
        /// <param name="ts"></param>
        /// <returns></returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static double TotalMicroseconds(this System.TimeSpan ts) { return ts.TotalMilliseconds * MicrosecondsPerMillisecond; }

        /// <summary>
        /// Calulcates the total amount of Nanoseconds (ns) in the given <see cref="TimeSpan"/>
        /// </summary>
        /// <param name="ts"></param>
        /// <returns></returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static double TotalNanoseconds(this System.TimeSpan ts) { return ts.TotalMilliseconds * NanosecondsPerMillisecond; }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static System.TimeSpan FromMicroseconds(double microSeconds) { return System.TimeSpan.FromTicks((long)(microSeconds * OneMicrosecond.Ticks)); }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static System.TimeSpan FromNanoseconds(double nanoSeconds) { return System.TimeSpan.FromTicks((long)(nanoSeconds / NanosecondsPerTick)); }

        ////
        //// Structure used in select() call, taken from the BSD file sys/time.h.
        ////
        //[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        //internal struct TimeValue
        //{
        //    internal long Value;

        //    public int Seconds { get { return (int)Value; } }  // seconds
                                                                 //& int.MaxValue
        //    public int Microseconds { get { return (int)(Value << Binary.BitsPerInteger); } } // and microseconds

        //    public TimeValue(long microSeconds)
        //    {
        //        Value = microSeconds;
        //        //Seconds = System.Math.DivRem((int)microSeconds,(int)NanosecondsPerMillisecond, 
        //        //    out Microseconds);
        //    }

        //}

        //private static void MicrosecondsToTimeValue(long microSeconds, ref TimeValue timeValue)
        //{
        //    timeValue.Value = microSeconds;
        //}

    }
}


namespace Media.UnitTests
{
    internal class TimeSpanExtensionsTests
    {
        public void TestFromMethods()
        {
            if (Common.Extensions.TimeSpan.TimeSpanExtensions.FromNanoseconds(0).Ticks != System.TimeSpan.Zero.Ticks) throw new System.Exception("FromNanoseconds");

            if (Common.Extensions.TimeSpan.TimeSpanExtensions.FromNanoseconds(10).Ticks != System.TimeSpan.Zero.Ticks) throw new System.Exception("FromNanoseconds");

            if (Common.Extensions.TimeSpan.TimeSpanExtensions.FromNanoseconds(99).Ticks != System.TimeSpan.Zero.Ticks) throw new System.Exception("FromNanoseconds");

            if (Common.Extensions.TimeSpan.TimeSpanExtensions.FromNanoseconds(100).Ticks != Common.Extensions.TimeSpan.TimeSpanExtensions.OneTick.Ticks) throw new System.Exception("FromNanoseconds");

            if (Common.Extensions.TimeSpan.TimeSpanExtensions.FromNanoseconds(200).Ticks != 2) throw new System.Exception("FromNanoseconds");

            if (Common.Extensions.TimeSpan.TimeSpanExtensions.FromMicroseconds(1).Ticks != Common.Extensions.TimeSpan.TimeSpanExtensions.OneMicrosecond.Ticks) throw new System.Exception("FromMicroseconds");

            if (Common.Extensions.TimeSpan.TimeSpanExtensions.FromMicroseconds(0.1) != Common.Extensions.TimeSpan.TimeSpanExtensions.OneTick) throw new System.Exception("FromMicroseconds");

            if (Media.Common.Extensions.TimeSpan.TimeSpanExtensions.TotalNanoseconds(Common.Extensions.TimeSpan.TimeSpanExtensions.OneMicrosecond) != Common.Extensions.TimeSpan.TimeSpanExtensions.NanosecondsPerMicrosecond) throw new System.Exception("TotalNanoseconds");

            if (Media.Common.Extensions.TimeSpan.TimeSpanExtensions.TotalMicroseconds(Common.Extensions.TimeSpan.TimeSpanExtensions.OneMicrosecond) != 1) throw new System.Exception("TotalMicroseconds");
        }
    }
}