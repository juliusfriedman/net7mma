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
namespace Media.Concepts.Classes
{
    /// <summary>
    /// Provides a clock with a given offset and calendar.
    /// </summary>
    public class Clock : Media.Common.BaseDisposable
    {
        #region Fields

        /// <summary>
        /// Indicates when the clock was created
        /// </summary>
        public readonly System.DateTimeOffset Created;

        /// <summary>
        /// The calendar system of the clock
        /// </summary>
        public readonly System.Globalization.Calendar Calendar;

        /// <summary>
        /// The amount of ticks which occur per update of the <see cref="System.Environment.TickCount"/> member.
        /// </summary>
        public readonly long TicksPerUpdate;

        /// <summary>
        /// The amount of instructions which occured when synchronizing with the system clock.
        /// </summary>
        public readonly long InstructionsPerClockUpdate;

        #endregion

        #region Properties

        /// <summary>
        /// The TimeZone offset of the clock from UTC
        /// </summary>
        public System.TimeSpan Offset { get { return Created.Offset; } }

        /// <summary>
        /// The average amount of operations per tick.
        /// </summary>
        public long AverageOperationsPerTick { get { return InstructionsPerClockUpdate / TicksPerUpdate; } }

        /// <summary>
        /// The <see cref="System.TimeSpan"/> which represents <see cref="TicksPerUpdate"/> as an amount of time.
        /// </summary>
        public System.TimeSpan SystemClockResolution { get { return System.TimeSpan.FromTicks(TicksPerUpdate); } }

        /// <summary>
        /// Return the current system time in the TimeZone offset of this clock
        /// </summary>
        public System.DateTimeOffset Now { get { return System.DateTimeOffset.Now.ToOffset(Offset); } }

        /// <summary>
        /// Return the current system time in the TimeZone offset of this clock converter to UniversalTime.
        /// </summary>
        public System.DateTimeOffset UtcNow { get { return Now.ToUniversalTime(); } }

        //public bool IsUtc { get { return Offset == System.TimeSpan.Zero; } }

        //public bool IsDaylightSavingTime { get { return Created.LocalDateTime.IsDaylightSavingTime(); } }

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a clock using the system's current timezone and calendar.
        /// The system clock is profiled to determine it's accuracy
        /// <see cref="System.DateTimeOffset.Now.Offset"/>
        /// <see cref="System.Globalization.CultureInfo.CurrentCulture.Calendar"/>
        /// </summary>
        public Clock(bool shouldDispose = true)
            : this(System.DateTimeOffset.Now.Offset, System.Globalization.CultureInfo.CurrentCulture.Calendar, shouldDispose)
        {

            throw new System.NotImplementedException("Contribute!");

            //Sample the TickCount
            long ticksStart = System.Environment.TickCount,
                ticksEnd;

            //Continually sample the TickCount. while the value has not changed increment OperationsPerTick
            while ((ticksEnd = System.Environment.TickCount) == ticksStart) ++InstructionsPerClockUpdate;

            //How many ticks occur per update of TickCount
            TicksPerUpdate = ticksEnd - ticksStart;
        }

        /// <summary>
        /// Constructs a new clock using the given TimeZone offset and Calendar system
        /// </summary>
        /// <param name="timeZoneOffset"></param>
        /// <param name="calendar"></param>
        /// <param name="shouldDispose">Indicates if the instace should be diposed when Dispose is called.</param>
        public Clock(System.TimeSpan timeZoneOffset, System.Globalization.Calendar calendar, bool shouldDispose = true)
        {
            //Allow disposal
            ShouldDispose = shouldDispose;

            Calendar = System.Globalization.CultureInfo.CurrentCulture.Calendar;

            Created = new System.DateTimeOffset(System.DateTime.Now, timeZoneOffset);
        }

        #endregion

        #region Overrides

        public override void Dispose()
        {

            if (false == ShouldDispose) return;

            base.Dispose();
        }

        #endregion

        //Methods or statics for OperationCountToTimeSpan? (Estimate)
    }
}

namespace Media.UnitTests
{
    internal class ClockTests
    {

        /// <summary>
        /// A static clock that should not dispose, set it to null when it is no longer required.
        /// </summary>
        /// <notes>
        /// A static instance can be useful to warm a type but should not be relied upon for absolute time requirements.
        /// You will notice based on it's properties that it may or may not have been started just before a system clock update and can only update at the same frequency of that clock.
        /// </notes>
        static Media.Concepts.Classes.Clock staticClock = new Concepts.Classes.Clock(false);

        public void TestStaticClock()
        {
            System.Console.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);

            //There must be a static clock
            if (staticClock == null) throw new System.InvalidOperationException("There is no static Clock.");

            //The static clock also has JIT overhead

            System.Console.WriteLine("Static SystemClockResolution: " + staticClock.SystemClockResolution);

            System.Console.WriteLine("Static TicksPerUpdate: " + staticClock.TicksPerUpdate);

            System.Console.WriteLine("Static OperationsPerClockUpdate: " + staticClock.InstructionsPerClockUpdate);

            System.Console.WriteLine("Static AverageOperationsPerTick: " + staticClock.AverageOperationsPerTick);

            //Probably not very accurate but WILL NEVER reflect a frequency the CPU is not capable of running at (Turbo mode)
            System.Console.WriteLine("CPU Estimated Frequency:" + staticClock.InstructionsPerClockUpdate / 1000 + "hZ");

            //Perform the same logic using a reference to the clock (The values are the same)

            //Make a reference to the clock which should not dispose
            using (Media.Concepts.Classes.Clock staticClockReference = staticClock)
            {
                System.Console.WriteLine("Static SystemClockResolution: " + staticClockReference.SystemClockResolution);

                System.Console.WriteLine("Static TicksPerUpdate: " + staticClockReference.TicksPerUpdate);

                System.Console.WriteLine("Static OperationsPerClockUpdate: " + staticClockReference.InstructionsPerClockUpdate);

                System.Console.WriteLine("Static AverageOperationsPerTick: " + staticClockReference.AverageOperationsPerTick);

                System.Console.WriteLine("CPU Estimated Frequency:" + staticClockReference.InstructionsPerClockUpdate / 1000 + "hZ");
            }

            //The staticClock must not dispose
            if (staticClock.IsDisposed != staticClock.ShouldDispose) throw new System.InvalidOperationException("staticClock cannot be Disposed.");
        }

        public void TestClock()
        {
            System.Console.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);

            //There must be a static clock
            if (staticClock == null) throw new System.InvalidOperationException("There is no static Clock.");

            //Call Dispose on the staticClock, just to test it
            staticClock.Dispose();

            //The staticClock must not dispose
            if (staticClock.IsDisposed != staticClock.ShouldDispose) throw new System.InvalidOperationException("staticClock cannot be Disposed.");

            //Make a new clock
            using (var clock = new Media.Concepts.Classes.Clock())
            {
                System.Console.WriteLine("SystemClockResolution: " + clock.SystemClockResolution);

                System.Console.WriteLine("TicksPerUpdate: " + clock.TicksPerUpdate);

                System.Console.WriteLine("OperationsPerClockUpdate: " + clock.InstructionsPerClockUpdate);

                System.Console.WriteLine("AverageOperationsPerTick: " + clock.AverageOperationsPerTick);

                //Convert from ticks to hZ
                System.Console.WriteLine("CPU Estimated Frequency:" + clock.InstructionsPerClockUpdate / 1000 + "hZ");

                //These values may or may not have been obtained based on a system time update which recently occured

                //For as many times are as indicated in the Ticks per update from the staticClock perform a test
                for (int i = 0; i < staticClock.TicksPerUpdate; ++i)
                {
                    using (var clockTest = new Media.Concepts.Classes.Clock())
                    {
                        System.Console.WriteLine("SystemClockResolution: " + clockTest.SystemClockResolution);

                        System.Console.WriteLine("TicksPerUpdate: " + clockTest.TicksPerUpdate);

                        System.Console.WriteLine("OperationsPerClockUpdate: " + clockTest.InstructionsPerClockUpdate);

                        System.Console.WriteLine("AverageOperationsPerTick: " + clockTest.AverageOperationsPerTick);

                        //Convert from ticks to hZ
                        System.Console.WriteLine("CPU Estimated Frequency:" + clock.InstructionsPerClockUpdate / 1000 + "hZ");
                    }
                }

                #region Notes on results

                //Even with a RTOS
                //http://en.wikipedia.org/wiki/Real-time_operating_system

                //Out of the above runs you should see close to the same values as produced by the initial clock and somewhat different that that of the static clock
                //If you run this test multiple times you will see seemingly random results.
                //This is due to CPU cache access which allows the code to sometimes run in between updates of the system clock if the code is in cache.
                //Ironically this may make the CPU seem slower but what is occuring is that the System.Environment.TickCount changes in a smaller amount of physical units in the time domain.
                //In a RTOS there will be slightly less variation in between all results

                #endregion

            }
        }
    }
}
