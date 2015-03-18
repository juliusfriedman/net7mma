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
    /// Provides a completely managed implementation of <see cref="System.Diagnostics.Stopwatch"/> which expresses time in the same units as <see cref="System.TimeSpan"/>.
    /// </summary>
    public class Stopwatch : Common.BaseDisposable
    {
        Timer Timer;

        long Units;

        public bool Enabled { get { return Timer != null && Timer.Enabled; } }

        public double ElapsedMicroseconds { get { return Units * Media.Common.Extensions.TimeSpan.TimeSpanExtensions.TotalMicroseconds(Timer.Frequency); } }

        public double ElapsedMilliseconds { get { return Units * Timer.Frequency.TotalMilliseconds; } }

        public double ElapsedSeconds { get { return Units * Timer.Frequency.TotalSeconds; } }

        //public System.TimeSpan Elapsed { get { return System.TimeSpan.FromMilliseconds(ElapsedMilliseconds / System.TimeSpan.TicksPerMillisecond); } }

        public System.TimeSpan Elapsed
        {
            get
            {
                var finished = System.DateTime.UtcNow;

                var taken = finished - Timer.m_Started;

                //The maximum amount of times the timer can elapse in the given frequency
                double maxCount = (taken.TotalMilliseconds / Timer.Frequency.TotalMilliseconds) / ElapsedMilliseconds;

                if (Units > maxCount)
                {
                    //How many more times the event was fired than needed
                    double overage = (maxCount - Units);

                    return taken.Add(new System.TimeSpan(System.Convert.ToInt64(Media.Common.Extensions.Math.MathExtensions.Clamp(Units, overage, maxCount) / System.TimeSpan.TicksPerSecond)));

                    //return taken.Add(new System.TimeSpan((long)Media.Common.Extensions.Math.MathExtensions.Clamp(Units, overage, maxCount)));
                }

                //return taken.Add(new System.TimeSpan(Units));

                return taken.Add(new System.TimeSpan(System.Convert.ToInt64(Units / System.TimeSpan.TicksPerSecond)));
            }
        }

        public void Start()
        {
            if (Enabled) return;

            Units = 0;

            //Create a Timer that will elapse every `OneMicrosecond`
            Timer = new Timer(Media.Common.Extensions.TimeSpan.TimeSpanExtensions.OneMicrosecond);

            Timer.Start();

            //Handle the event by incrementing count
            Timer.Tick += Count;
        }

        public void Stop()
        {
            if (false == Enabled) return;

            Timer.Stop();

            Timer.Dispose();           

        }

        void Count(ref long count) { ++Units; }
    }
}

namespace Media.UnitTests
{
    internal class StopWatchTests
    {
        public void TestForOneMicrosecond()
        {
            //Create a Timer that will elapse every `OneMicrosecond`
            using (Media.Concepts.Classes.Stopwatch sw = new Media.Concepts.Classes.Stopwatch())
            {
                var started = System.DateTime.UtcNow;

                System.Console.WriteLine("Started: " + started.ToString("MM/dd/yyyy hh:mm:ss.ffffff tt"));

                //Define some amount of time
                System.TimeSpan sleepTime = Media.Common.Extensions.TimeSpan.TimeSpanExtensions.OneMicrosecond;

                System.Diagnostics.Stopwatch testSw = new System.Diagnostics.Stopwatch();

                //Start
                testSw.Start();

                //Start
                sw.Start();

                //Sleep the desired amount
                System.Threading.Thread.Sleep(sleepTime);

                //Stop
                testSw.Stop();

                //Stop
                sw.Stop();

                var finished = System.DateTime.UtcNow;

                var taken = finished - started;

                System.Console.WriteLine("Finished: " + finished.ToString("MM/dd/yyyy hh:mm:ss.ffffff tt"));

                System.Console.WriteLine("Sleep Time: " + sleepTime.ToString());

                System.Console.WriteLine("Real Taken Total: " + taken.ToString());

                System.Console.WriteLine("Real Taken msec Total: " + taken.TotalMilliseconds.ToString());

                System.Console.WriteLine("Real Taken sec Total: " + taken.TotalSeconds.ToString());

                System.Console.WriteLine("Real Taken μs Total: " + Media.Common.Extensions.TimeSpan.TimeSpanExtensions.TotalMicroseconds(taken).ToString());

                System.Console.WriteLine("Managed Taken Total: " + sw.Elapsed.ToString());

                System.Console.WriteLine("Diagnostic Taken Total: " + testSw.Elapsed.ToString());

                System.Console.WriteLine("Diagnostic Elapsed Seconds  Total: " + ((testSw.ElapsedTicks / (double)System.Diagnostics.Stopwatch.Frequency)));

                //Write the rough amount of time taken in  micro seconds
                System.Console.WriteLine("Managed Time Estimated Taken: " + sw.ElapsedMicroseconds + "μs");

                //Write the rough amount of time taken in  micro seconds
                System.Console.WriteLine("Diagnostic Time Estimated Taken: " + Media.Common.Extensions.TimeSpan.TimeSpanExtensions.TotalMicroseconds(testSw.Elapsed) + "μs");

                System.Console.WriteLine("Managed Time Estimated Taken: " + sw.ElapsedMilliseconds);

                System.Console.WriteLine("Diagnostic Time Estimated Taken: " + testSw.ElapsedMilliseconds);

                System.Console.WriteLine("Managed Time Estimated Taken: " + sw.ElapsedSeconds);

                System.Console.WriteLine("Diagnostic Time Estimated Taken: " + sw.Elapsed.TotalSeconds);

                if (sw.Elapsed < testSw.Elapsed)
                {
                    System.Console.WriteLine("Faster than Diagnostic StopWatch");
                }
                else if (sw.Elapsed > testSw.Elapsed)
                {
                    System.Console.WriteLine("Slower than Diagnostic StopWatch");
                }
                else
                {
                    System.Console.WriteLine("Equal to Diagnostic StopWatch");
                }
            }
        }
    }
}