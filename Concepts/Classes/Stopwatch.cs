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
    public class Stopwatch : Common.CommonDisposable
    {
        internal Timer Timer;

        internal long Units;

        public bool Enabled
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return Timer != null && Timer.Enabled; }
        }

        public double ElapsedTicks
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get
            {
                if (Timer.Frequency <= Common.Extensions.TimeSpan.TimeSpanExtensions.OneTick) return Units;
                else return Units * Timer.Frequency.Ticks;
            }
        }

        public double ElapsedNanoseconds
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get
            {
                if (Timer.Frequency <= Common.Extensions.TimeSpan.TimeSpanExtensions.OneTick) return Media.Common.Extensions.TimeSpan.TimeSpanExtensions.TotalNanoseconds(Timer.Frequency) / Units;
                else return Units * Media.Common.Extensions.TimeSpan.TimeSpanExtensions.TotalNanoseconds(Timer.Frequency);
            }
        }

        public double ElapsedMicroseconds
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get
            {
                //return Units * Media.Common.Extensions.TimeSpan.TimeSpanExtensions.TotalMicroseconds(Timer.Frequency);
                if (Timer.Frequency <= Common.Extensions.TimeSpan.TimeSpanExtensions.OneMicrosecond) return Media.Common.Extensions.TimeSpan.TimeSpanExtensions.TotalMicroseconds(Timer.Frequency) / Units;
                else return Units * Media.Common.Extensions.TimeSpan.TimeSpanExtensions.TotalMicroseconds(Timer.Frequency);
            }
        }

        public double ElapsedMilliseconds
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get
            {
                //return Units * Timer.Frequency.TotalMilliseconds;
                if (Timer.Frequency <= Common.Extensions.TimeSpan.TimeSpanExtensions.OneMillisecond) return Timer.Frequency.TotalMilliseconds / Units;
                else return Units * Timer.Frequency.TotalMilliseconds;
            }
        }

        public double ElapsedSeconds
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get
            {
                //return Units * Timer.Frequency.TotalSeconds;
                if (Timer.Frequency <= Common.Extensions.TimeSpan.TimeSpanExtensions.OneMillisecond) return Timer.Frequency.TotalSeconds / Units;
                else return Units * Timer.Frequency.TotalSeconds;
            }
        }

        //public System.TimeSpan Elapsed { get { return System.TimeSpan.FromMilliseconds(ElapsedMilliseconds / System.TimeSpan.TicksPerMillisecond); } }

        public System.TimeSpan Elapsed
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get
            {
                switch (Units)
                {
                    case 0: return System.TimeSpan.Zero;
                    default:
                        {
                            System.TimeSpan taken = System.DateTime.UtcNow - Timer.m_Started;

                            return taken.Add(new System.TimeSpan(Units * Timer.Frequency.Ticks));

                            //System.TimeSpan additional = new System.TimeSpan(Media.Common.Extensions.Math.MathExtensions.Clamp(Units, 0, Timer.Frequency.Ticks));

                            //return taken.Add(additional);

                            ////The maximum amount of times the timer can elapse in the given frequency
                            //double maxCount = (taken.TotalMilliseconds / Timer.Frequency.TotalMilliseconds) / ElapsedMilliseconds;

                            //if (Units > maxCount)
                            //{
                            //    //How many more times the event was fired than needed
                            //    double overage = (maxCount - Units);

                            //    System.TimeSpan additional = new System.TimeSpan(System.Convert.ToInt64(Media.Common.Extensions.Math.MathExtensions.Clamp(Units, overage, maxCount)));

                            //    //return taken.Add(new System.TimeSpan((long)Media.Common.Extensions.Math.MathExtensions.Clamp(Units, overage, maxCount)));
                            //}
                            
                            //return taken.Add(new System.TimeSpan(Units));
                        }
                }
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public void Start()
        {
            if (Enabled) return;

            Units = 0;

            //Create a Timer that will elapse every OneTick //`OneMicrosecond`
            Timer = new Timer(Media.Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan);

            //Handle the event by incrementing count
            Timer.Tick += Count;

            Timer.Start();
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public void Stop()
        {
            if (false == Enabled) return;

            Timer.Stop();

            Timer.Dispose();           
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        void Count(ref long count) { unchecked { ++Units; } }
    }
}

namespace Media.UnitTests
{
    internal class StopWatchTests
    {
        public void TestForOneMicrosecond()
        {
            System.Collections.Generic.List<System.Tuple<bool, System.TimeSpan, System.TimeSpan>> l = new System.Collections.Generic.List<System.Tuple<bool, System.TimeSpan, System.TimeSpan>>();

            //Create a Timer that will elapse every `OneMicrosecond`
            for (int i = 0; i < 250; ++i) using (Media.Concepts.Classes.Stopwatch sw = new Media.Concepts.Classes.Stopwatch())
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

                //while (sw.Elapsed.Ticks < sleepTime.Ticks - (Common.Extensions.TimeSpan.TimeSpanExtensions.OneTick + Common.Extensions.TimeSpan.TimeSpanExtensions.OneTick).Ticks)
                //{
                //    //sw.Timer.m_Clock.NanoSleep(0); //System.Threading.Thread.SpinWait(0);

                //    System.Console.WriteLine(sw.ElapsedNanoseconds);

                //}

                while (double.IsNaN(sw.ElapsedNanoseconds))
                {
                    sw.Timer.m_Clock.NanoSleep(0);
                    //sw.Timer.m_Counter.Join(0);
                }

                //while (sw.ElapsedNanoseconds == 0.0 && sw.Elapsed == System.TimeSpan.Zero)
                //{
                //    sw.Timer.m_Clock.NanoSleep(0);

                //    System.Console.WriteLine(sw.ElapsedNanoseconds);
                //}

                //Sleep the desired amount
                //System.Threading.Thread.Sleep(sleepTime);

                //Stop
                testSw.Stop();

                //Stop
                sw.Stop();

                System.Console.WriteLine(sw.ElapsedNanoseconds);

                System.Console.WriteLine(sw.Units);

                var finished = System.DateTime.UtcNow;

                var taken = finished - started;

                var cc = System.Console.ForegroundColor;

                System.Console.WriteLine("Finished: " + finished.ToString("MM/dd/yyyy hh:mm:ss.ffffff tt"));

                System.Console.WriteLine("Sleep Time: " + sleepTime.ToString());

                System.Console.WriteLine("Real Taken Total: " + taken.ToString());

                if (taken > sleepTime) 
                {
                    System.Console.ForegroundColor = System.ConsoleColor.Red;
                    System.Console.WriteLine("Missed by: " + (taken - sleepTime));
                }
                else
                {
                    System.Console.ForegroundColor = System.ConsoleColor.Green;
                    System.Console.WriteLine("Still have: " + (sleepTime - taken));
                }

                System.Console.ForegroundColor = cc;

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

                System.Console.WriteLine("Diagnostic Time Estimated Taken: " + testSw.Elapsed.TotalSeconds);

                if (sw.Elapsed < testSw.Elapsed)
                {
                    System.Console.WriteLine("Faster than Diagnostic StopWatch");
                    l.Add(new System.Tuple<bool, System.TimeSpan, System.TimeSpan>(true, sw.Elapsed, testSw.Elapsed));
                }
                else if (sw.Elapsed > testSw.Elapsed)
                {
                    System.Console.WriteLine("Slower than Diagnostic StopWatch");
                    l.Add(new System.Tuple<bool, System.TimeSpan, System.TimeSpan>(false, sw.Elapsed, testSw.Elapsed));
                }
                else
                {
                    System.Console.WriteLine("Equal to Diagnostic StopWatch");
                    l.Add(new System.Tuple<bool, System.TimeSpan, System.TimeSpan>(true, sw.Elapsed, testSw.Elapsed));
                }
            }

            int w = 0, f = 0;

            var cc2 = System.Console.ForegroundColor;

            foreach (var t in l)
            {
                if (t.Item1)
                {
                    System.Console.ForegroundColor = System.ConsoleColor.Green;
                    ++w; System.Console.WriteLine("Faster than Diagnostic StopWatch by: " + (t.Item3 - t.Item2));
                }
                else
                {
                    System.Console.ForegroundColor = System.ConsoleColor.Red;
                    ++f; System.Console.WriteLine("Slower than Diagnostic StopWatch by: " + (t.Item2 - t.Item3));
                }
            }

            System.Console.ForegroundColor = System.ConsoleColor.Green;
            System.Console.WriteLine("Wins = " + w);

            System.Console.ForegroundColor = System.ConsoleColor.Red;
            System.Console.WriteLine("Loss = " + f);

            System.Console.ForegroundColor = cc2;
        }
    }
}