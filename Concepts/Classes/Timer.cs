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
    /// Provides a Timer implementation which can be used across all platforms and does not rely on the existing Timer implementation.
    /// This is fast enough to count all cycles and measure micro time, almost nano time.
    /// </summary>
    /// <remarks>
    /// <see ref="http://www.pinvoke.net/default.aspx/kernel32.getthreadtimes">Also</see>
    /// </remarks>
    public class Timer : Common.SuppressedFinalizerDisposable
    {
        internal readonly System.Threading.Thread m_Counter; // m_Consumer, m_Producer

        internal System.TimeSpan m_Frequency;

        internal ulong m_Ops = 0, m_Ticks = 0;

        long m_Enabled;

        internal System.DateTimeOffset m_Started;

        public delegate void TickEvent(ref long ticks);

        public event TickEvent Tick;

        public bool Enabled
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return System.Threading.Interlocked.Read(ref m_Enabled) > uint.MinValue; }

            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            set { m_Enabled = (value ? uint.MaxValue : uint.MinValue); }
        }

        public System.TimeSpan Frequency
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_Frequency; }
        }

        internal ulong m_Bias;

        //

        //Could just use a single int, 32 bits is more than enough.

        //uint m_Flags;

        //

        readonly internal Clock m_Clock = new Clock();
        
        readonly internal Media.Common.Collections.Generic.ConcurrentLinkedQueueSlim<long> Producer;

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        void Count()
        {
            //Todo, make JumpExpression ... along with TypedReferenceExpression
            //System.Action ShouldStop = () => if (false.Equals(m_Enabled.Equals(uint.MinValue))) goto Approximate; System.Threading.Thread.ResetAbort();

            System.Threading.Thread Event = null;

            Event = new System.Threading.Thread(new System.Threading.ThreadStart(() =>
            {
                System.Threading.Thread.BeginCriticalRegion();

                long sample = 0;

            AfterSample:

                try
                {
                Top:
                    System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Highest;

                    /*do */
                    if (Producer.TryDequeue(out sample)) Tick(ref sample);
                    //while (m_Enabled && Producer.Count >= 0);

                    System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Lowest;

                    if (m_Enabled.Equals(uint.MinValue)) return;
                    else if (Producer.Count.Equals(uint.MinValue))
                    {
                        if (System.Threading.Thread.CurrentThread.Equals(m_Counter))
                        {
                            //Wait for other threads and if no other thread wanted time join the event thread indefinitely.
                            if (false.Equals(System.Threading.Thread.Yield()))
                            {
                                //If not already waiting or sleeping or joining
                                if (false.Equals(Event.ThreadState.HasFlag(System.Threading.ThreadState.WaitSleepJoin)))
                                {
                                    Event.Join(System.Threading.Timeout.InfiniteTimeSpan);  //++m_Ops;
                                }
                            }
                        }
                        else //Not the counter so return
                        {
                            return;

                            //m_Counter.Join(m_Frequency);  //++m_Ops;
                        }
                    }

                    goto Top;
                }
                catch { if (m_Enabled.Equals(uint.MinValue)) return; goto AfterSample; }
                finally { System.Threading.Thread.EndCriticalRegion(); }
            }))
            {
                Priority = System.Threading.ThreadPriority.AboveNormal
            };

            Event.TrySetApartmentState(System.Threading.ApartmentState.MTA);

            Event.Start();

            //to ensure the slice offset is different, e.g. more bias
        //m_Clock.NanoSleep(0);

        Approximate:

            ulong approximate = (ulong)Common.Binary.Clamp((m_Clock.AverageOperationsPerTick / (Frequency.Ticks + 1)), 1, ulong.MaxValue), x = 0;

            try
            {
                m_Started = m_Clock.Now;

                System.Threading.Thread.BeginCriticalRegion();

                unchecked
                {
                Start:

                    if (IsDisposed) return;

                    switch (++m_Ops)
                    {
                        default:
                            {
                                if (false.Equals(m_Enabled.Equals(uint.MinValue)) && m_Bias + ++m_Ops >= approximate)
                                {
                                    System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Highest;

                                    Producer.Enqueue((long)m_Ticks++);

                                    x = (ulong)Common.Binary.Clamp((m_Bias = m_Ops / approximate), 0, m_Bias);

                                    while (false.Equals(m_Enabled.Equals(uint.MinValue)) && 1 > --x /*&& Producer.Count <= m_Frequency.Ticks*/) Producer.Enqueue((long)++m_Ticks);

                                    m_Ops += m_Bias;

                                    System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Lowest;
                                }

                                if (false.Equals(Event == null) && Event.IsAlive)
                                {
                                    //If this is the counter thread
                                    if (System.Threading.Thread.CurrentThread.Equals(m_Counter))
                                    {
                                        //If not event is not sleeping or joined then
                                        if (false.Equals(Event.ThreadState.HasFlag(System.Threading.ThreadState.WaitSleepJoin)))
                                        {
                                            //Join the event thread for the frequency
                                            Event.Join(m_Frequency);
                                        }

                                        //Time and Stack..
                                        //Event.Interrupt();
                                    }

                                }
                                else //Not the counter so return
                                {
                                    //Option for delay 

                                    //Time and Stack..
                                    //m_Counter.Interrupt();

                                    //if (false.Equals(System.Threading.Thread.Yield())) ; //m_Counter.Interrupt();

                                    return;
                                }

                                goto Start;
                            }
                    }
                }
            }
            catch (System.SystemException se)
            {
                if (se is System.Threading.ThreadAbortException) System.Threading.Thread.ResetAbort();
                else if (se is System.Threading.ThreadInterruptedException | false.Equals(m_Enabled.Equals(uint.MinValue))) goto Approximate;
                else if (se is System.OutOfMemoryException)
                {
                    if ((ulong)Producer.Count > approximate) Producer.Clear();

                    if (false.Equals(m_Enabled.Equals(uint.MinValue))) goto Approximate;
                }
            }
            catch
            {
                if (false.Equals(m_Enabled.Equals(uint.MinValue))) goto Approximate;
            }
            finally
            {
                Event = null;

                System.Threading.Thread.EndCriticalRegion();
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public Timer(System.TimeSpan frequency, bool shouldDispose = true) 
            :base(shouldDispose)
        {
            m_Frequency = frequency;

            Producer = new Common.Collections.Generic.ConcurrentLinkedQueueSlim<long>();

            m_Counter = new System.Threading.Thread(new System.Threading.ThreadStart(Count))
            {
                Priority = System.Threading.ThreadPriority.AboveNormal
            };

            m_Counter.TrySetApartmentState(System.Threading.ApartmentState.MTA);

            Tick = delegate { unchecked { m_Ops += 1 + m_Bias; } };
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public void Start()
        {
            if (false.Equals(m_Enabled.Equals(uint.MinValue))) return;

            if (IsDisposed) throw new System.ObjectDisposedException("The Timer has already been disposed.");

            Change(m_Frequency, System.TimeSpan.Zero);

            m_Counter.Start();

            System.Threading.ThreadPriority previous = System.Threading.Thread.CurrentThread.Priority;

            System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Lowest;

            while (false.Equals(m_Enabled.Equals(uint.MinValue)) && m_Ops == 0)
            {
                if (System.Threading.Thread.CurrentThread.Equals(m_Counter))
                {
                    m_Counter.Join(System.TimeSpan.Zero);  //++m_Ops;
                }
                else
                {
                    m_Counter.Join(System.Threading.Timeout.InfiniteTimeSpan);  //++m_Ops;
                }
            }
            
            System.Threading.Thread.CurrentThread.Priority = previous;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public void Stop()
        {
            System.Threading.Interlocked.Exchange(ref m_Enabled, uint.MinValue);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        void Change(System.TimeSpan interval, System.TimeSpan dueTime)
        {
            System.Threading.Interlocked.Exchange(ref m_Enabled, uint.MinValue);

            m_Frequency = interval;

            System.Threading.Interlocked.Exchange(ref m_Enabled, uint.MaxValue);
        }

        delegate void ElapsedEvent(object sender, object args);

        protected override void Dispose(bool disposing)
        {
            if (IsDisposed || false == disposing || false == ShouldDispose) return;

            base.Dispose(disposing);
            
            Stop(); 

            try { m_Counter.Abort(m_Frequency); }
            catch (System.Threading.ThreadAbortException) { System.Threading.Thread.ResetAbort(); }
            catch { }
            finally
            {
                Tick = null;
            }

            
        }

    }
}

namespace Media.UnitTests
{
    internal class TimerTests
    {
        public void TestForOneTick()
        {
            //Create a Timer that will elapse every `OneMicrosecond`
             using (Media.Concepts.Classes.Timer t = new Media.Concepts.Classes.Timer(Media.Common.Extensions.TimeSpan.TimeSpanExtensions.OneTick))
            {
                int count = 0;

                //Handle the event by incrementing count
                t.Tick += (ref long st) => { ++count; };

                //Do the same for another counter
                int anotherCount = 0;

                t.Tick += (ref long st) => { ++anotherCount; };

                //Do the same for another counter
                int lastCounter = 0;

                t.Tick += (ref long st) => { ++lastCounter; };

                System.Diagnostics.Stopwatch testSw = new System.Diagnostics.Stopwatch();

                t.Start();

                testSw.Start();

                System.Console.WriteLine("Frequency: " + t.Frequency);

                System.Console.WriteLine("Started: " + t.m_Started.ToString("MM/dd/yyyy hh:mm:ss.ffffff tt"));

                //Sleep the frequency
                 System.TimeSpan s = new System.TimeSpan(((long)t.m_Ticks + testSw.ElapsedTicks));

                while (s < t.Frequency)
                {
                    System.Console.WriteLine((t.m_Clock.UtcNow + s).ToString("MM/dd/yyyy hh:mm:ss.ffffff tt"));
                    //System.Threading.Thread.SpinWait(0);
                    t.m_Clock.NanoSleep(0);
                    s = new System.TimeSpan(((long)t.m_Ticks + testSw.ElapsedTicks));
                }


                t.Stop();

                testSw.Stop();

                var finished = System.DateTime.UtcNow;

                var taken = finished - t.m_Started;

                System.Console.WriteLine("Finished: " + finished.ToString("MM/dd/yyyy hh:mm:ss.ffffff tt"));

                System.Console.WriteLine("Taken Microseconds: " + Media.Common.Extensions.TimeSpan.TimeSpanExtensions.TotalMicroseconds(taken));

                System.Console.WriteLine("Taken Total: " + taken.ToString());

                System.Console.WriteLine("Managed Ticks: " + t.m_Ticks);

                //The maximum amount of times the timer can elapse in the given frequency
                double maxCount = taken.Ticks / t.Frequency.Ticks;

                System.Console.WriteLine("Maximum Count: " + taken.Ticks / t.Frequency.Ticks);

                System.Console.WriteLine("Actual Count: " + count);

                System.Console.WriteLine("Another Count: " + anotherCount);

                System.Console.WriteLine("Last Counter: " + lastCounter);

                //100 ns or (1 tick) is equal to about 100 counts in this case
                //Since sleep may not be accurate then there must be atleast 900 counts
                if ((taken > System.TimeSpan.Zero && count < Media.Common.Extensions.TimeSpan.TimeSpanExtensions.MicrosecondsPerMillisecond
                    || //In all cases the count must never be less than what is possible during the physical given frequency
                    count < maxCount)
                    && t.m_Ticks > 0 &&
                    (long)t.m_Ticks < count) throw new System.Exception("Did not count all intervals");

                //Write the rough amount of time taken in nano seconds
                System.Console.WriteLine("Time Estimated Taken: " + count * Media.Common.Extensions.TimeSpan.TimeSpanExtensions.TotalNanoseconds(t.Frequency) + "ns");

                //Write the rough amount of time taken in  micro seconds
                System.Console.WriteLine("Time Estimated Taken: " + count * Media.Common.Extensions.TimeSpan.TimeSpanExtensions.TotalMicroseconds(t.Frequency) + "μs");

                //How many more times the event was fired than needed
                double overage = (count - maxCount);

                //Display the amount of operations which were performed more than required.
                System.Console.WriteLine("Count Overage: " + overage);

                System.Console.WriteLine("Ticks Overage: " + System.Math.Abs(overage - testSw.Elapsed.Ticks));
            }
        }

        public void TestForOneMillisecond()
        {
            //Create a Timer that will elapse every `OneMicrosecond`
            using (Media.Concepts.Classes.Timer t = new Media.Concepts.Classes.Timer(Media.Common.Extensions.TimeSpan.TimeSpanExtensions.OneMillisecond))
            {
                int count = 0;

                //Handle the event by incrementing count
                t.Tick += (ref long st) => { ++count; };

                //Do the same for another counter
                int anotherCount = 0;

                t.Tick += (ref long st) => { ++anotherCount; };

                //Do the same for another counter
                int lastCounter = 0;

                t.Tick += (ref long st) => { ++lastCounter; };

                System.Diagnostics.Stopwatch testSw = new System.Diagnostics.Stopwatch();

                t.Start();

                testSw.Start();

                System.Console.WriteLine("Frequency: " + t.Frequency);

                System.Console.WriteLine("Started: " + t.m_Started.ToString("MM/dd/yyyy hh:mm:ss.ffffff tt"));

                //Sleep the frequency
                System.TimeSpan s = new System.TimeSpan(((long)t.m_Ticks + testSw.ElapsedTicks));

                while (s < t.Frequency)
                {
                    System.Console.WriteLine((t.m_Clock.UtcNow + s).ToString("MM/dd/yyyy hh:mm:ss.ffffff tt"));
                    //System.Threading.Thread.SpinWait(0);
                    t.m_Clock.NanoSleep(0);
                    s = new System.TimeSpan(((long)t.m_Ticks + testSw.ElapsedTicks));
                }



                t.Stop();

                testSw.Stop();

                var finished = System.DateTime.UtcNow;

                var taken = finished - t.m_Started;

                System.Console.WriteLine("Finished: " + finished.ToString("MM/dd/yyyy hh:mm:ss.ffffff tt"));

                System.Console.WriteLine("Taken Microseconds: " + Media.Common.Extensions.TimeSpan.TimeSpanExtensions.TotalMicroseconds(taken));

                System.Console.WriteLine("Taken Total: " + taken.ToString());

                System.Console.WriteLine("Managed Ticks: " + t.m_Ticks);

                //The maximum amount of times the timer can elapse in the given frequency
                double maxCount = taken.TotalMilliseconds / t.Frequency.TotalMilliseconds;

                System.Console.WriteLine("Maximum Count: " + taken.TotalMilliseconds / t.Frequency.TotalMilliseconds);

                System.Console.WriteLine("Actual Count: " + count);

                System.Console.WriteLine("Another Count: " + anotherCount);

                System.Console.WriteLine("Last Counter: " + lastCounter);

                //100 ns or (1 tick) is equal to about 100 counts in this case
                //Since sleep may not be accurate then there must be atleast 900 counts
                if ((taken > System.TimeSpan.Zero && count < Media.Common.Extensions.TimeSpan.TimeSpanExtensions.MicrosecondsPerMillisecond
                    || //In all cases the count must never be less than what is possible during the physical given frequency
                    count < maxCount)
                    && t.m_Ticks > 0 &&
                    (long)t.m_Ticks < count) throw new System.Exception("Did not count all intervals");

                //Write the rough amount of time taken in nano seconds
                System.Console.WriteLine("Time Estimated Taken: " + count * Media.Common.Extensions.TimeSpan.TimeSpanExtensions.TotalNanoseconds(t.Frequency) + "ns");

                //Write the rough amount of time taken in  micro seconds
                System.Console.WriteLine("Time Estimated Taken: " + count * Media.Common.Extensions.TimeSpan.TimeSpanExtensions.TotalMicroseconds(t.Frequency) + "μs");

                //How many more times the event was fired than needed
                double overage = (count - maxCount);

                //Display the amount of operations which were performed more than required.
                System.Console.WriteLine("Count Overage: " + overage);

                System.Console.WriteLine("Ticks Overage: " + System.Math.Abs(overage - testSw.Elapsed.Ticks));
            }
        }

        public void TestForOneMicrosecond()
        {
            //Create a Timer that will elapse every `OneMicrosecond`
            using (Media.Concepts.Classes.Timer t = new Media.Concepts.Classes.Timer(Media.Common.Extensions.TimeSpan.TimeSpanExtensions.OneMicrosecond))
            {
                int count = 0;

                //Handle the event by incrementing count
                t.Tick += (ref long st) => { ++count; };

                //Do the same for another counter
                int anotherCount = 0;

                t.Tick += (ref long st) => { ++anotherCount; };

                //Do the same for another counter
                int lastCounter = 0;

                t.Tick += (ref long st) => { ++lastCounter; };

                System.Diagnostics.Stopwatch testSw = new System.Diagnostics.Stopwatch();

                t.Start();

                testSw.Start();

                System.Console.WriteLine("Frequency: " + t.Frequency);

                System.Console.WriteLine("Started: " + t.m_Started.ToString("MM/dd/yyyy hh:mm:ss.ffffff tt"));

                //Sleep the frequency
                System.TimeSpan s = new System.TimeSpan(((long)t.m_Ticks + testSw.ElapsedTicks + 1));

                while (s < t.Frequency)
                {
                    System.Console.WriteLine((t.m_Clock.UtcNow + s).ToString("MM/dd/yyyy hh:mm:ss.ffffff tt"));
                    //System.Threading.Thread.SpinWait(0);
                    t.m_Clock.NanoSleep(0);
                    s = new System.TimeSpan(((long)t.m_Ticks + testSw.ElapsedTicks + 1));
                }

                t.Stop();

                testSw.Stop();

                var finished = System.DateTime.UtcNow;

                var taken = finished - t.m_Started;

                System.Console.WriteLine("Finished: " + finished.ToString("MM/dd/yyyy hh:mm:ss.ffffff tt"));

                System.Console.WriteLine("Taken Microseconds: " + Media.Common.Extensions.TimeSpan.TimeSpanExtensions.TotalMicroseconds(taken));

                System.Console.WriteLine("Taken Total: " + taken.ToString());

                System.Console.WriteLine("Managed Ticks: " + t.m_Ticks);

                //The maximum amount of times the timer can elapse in the given frequency
                double maxCount = Media.Common.Extensions.TimeSpan.TimeSpanExtensions.TotalMicroseconds(taken) / Media.Common.Extensions.TimeSpan.TimeSpanExtensions.TotalMicroseconds(t.Frequency);

                System.Console.WriteLine("Maximum Count: " + Common.Extensions.TimeSpan.TimeSpanExtensions.TotalMicroseconds(taken));

                System.Console.WriteLine("Actual Count: " + count);

                System.Console.WriteLine("Another Count: " + anotherCount);

                System.Console.WriteLine("Last Counter: " + lastCounter);

                //100 ns or (1 tick) is equal to about 100 counts in this case
                //Since sleep may not be accurate then there must be atleast 900 counts
                if ((taken > System.TimeSpan.Zero && count < Media.Common.Extensions.TimeSpan.TimeSpanExtensions.MicrosecondsPerMillisecond
                    || //In all cases the count must never be less than what is possible during the physical given frequency
                    count < maxCount)
                    && t.m_Ticks > 0 &&
                    (long)t.m_Ticks < count) throw new System.Exception("Did not count all intervals");

                //Write the rough amount of time taken in nano seconds
                System.Console.WriteLine("Time Estimated Taken: " + count * Media.Common.Extensions.TimeSpan.TimeSpanExtensions.TotalNanoseconds(t.Frequency) + "ns");

                //Write the rough amount of time taken in  micro seconds
                System.Console.WriteLine("Time Estimated Taken: " + count * Media.Common.Extensions.TimeSpan.TimeSpanExtensions.TotalMicroseconds(t.Frequency) + "μs");

                //How many more times the event was fired than needed
                double overage = (count - maxCount);

                //Display the amount of operations which were performed more than required.
                System.Console.WriteLine("Count Overage: " + overage);

                System.Console.WriteLine("Ticks Overage: " + System.Math.Abs(overage - testSw.Elapsed.Ticks));
            }
        }

        public void TestForZero()
        {
            using (Media.Concepts.Classes.Timer t = new Media.Concepts.Classes.Timer(System.TimeSpan.Zero))
            {
                int count = 0;

                //Handle the event by incrementing count
                t.Tick += (ref long st) => { ++count; };

                //Do the same for another counter
                int anotherCount = 0;

                t.Tick += (ref long st) => { ++anotherCount; };

                //Do the same for another counter
                int lastCounter = 0;

                t.Tick += (ref long st) => { ++lastCounter; };

                System.Diagnostics.Stopwatch testSw = new System.Diagnostics.Stopwatch();

                t.Start();

                testSw.Start();

                System.Console.WriteLine("Frequency: " + t.Frequency);

                System.Console.WriteLine("Started: " + t.m_Started.ToString("MM/dd/yyyy hh:mm:ss.ffffff tt"));

                //Sleep the frequency
                System.TimeSpan s = new System.TimeSpan(((long)t.m_Ticks + testSw.ElapsedTicks + 1));

                while (s < t.Frequency)
                {
                    System.Console.WriteLine((t.m_Clock.UtcNow + s).ToString("MM/dd/yyyy hh:mm:ss.ffffff tt"));
                    //System.Threading.Thread.SpinWait(0);
                    t.m_Clock.NanoSleep(0);
                    s = new System.TimeSpan(((long)t.m_Ticks + testSw.ElapsedTicks + 1));
                }

                t.Stop();

                testSw.Stop();

                var finished = System.DateTime.UtcNow;

                var taken = finished - t.m_Started;

                System.Console.WriteLine("Finished: " + finished.ToString("MM/dd/yyyy hh:mm:ss.ffffff tt"));

                System.Console.WriteLine("Taken Microseconds: " + Media.Common.Extensions.TimeSpan.TimeSpanExtensions.TotalMicroseconds(taken));

                System.Console.WriteLine("Taken Total: " + taken.ToString());

                System.Console.WriteLine("Managed Ticks: " + t.m_Ticks);

                //The maximum amount of times the timer can elapse in the given frequency
                double maxCount = Media.Common.Extensions.TimeSpan.TimeSpanExtensions.TotalMicroseconds(taken) / Media.Common.Extensions.TimeSpan.TimeSpanExtensions.TotalMicroseconds(t.Frequency);

                System.Console.WriteLine("Maximum Count: " + Common.Extensions.TimeSpan.TimeSpanExtensions.TotalMicroseconds(taken));

                System.Console.WriteLine("Actual Count: " + count);

                System.Console.WriteLine("Another Count: " + anotherCount);

                System.Console.WriteLine("Last Counter: " + lastCounter);

                //100 ns or (1 tick) is equal to about 100 counts in this case
                //Since sleep may not be accurate then there must be atleast 900 counts
                if ((taken > System.TimeSpan.Zero && count < Media.Common.Extensions.TimeSpan.TimeSpanExtensions.MicrosecondsPerMillisecond
                    || //In all cases the count must never be less than what is possible during the physical given frequency
                    count < maxCount)
                    && t.m_Ticks > 0 &&
                    (long)t.m_Ticks < count) throw new System.Exception("Did not count all intervals");

                //Write the rough amount of time taken in nano seconds
                System.Console.WriteLine("Time Estimated Taken: " + count * Media.Common.Extensions.TimeSpan.TimeSpanExtensions.TotalNanoseconds(t.Frequency) + "ns");

                //Write the rough amount of time taken in  micro seconds
                System.Console.WriteLine("Time Estimated Taken: " + count * Media.Common.Extensions.TimeSpan.TimeSpanExtensions.TotalMicroseconds(t.Frequency) + "μs");

                //How many more times the event was fired than needed
                double overage = (count - maxCount);

                //Display the amount of operations which were performed more than required.
                System.Console.WriteLine("Count Overage: " + overage);

                System.Console.WriteLine("Ticks Overage: " + System.Math.Abs(overage - testSw.Elapsed.Ticks));
            }
        }

        public void TestForTwentyMicroseconds()
        {
            //Create a Timer that will elapse every `OneMicrosecond`
            using (Media.Concepts.Classes.Timer t = new Media.Concepts.Classes.Timer(new System.TimeSpan(Media.Common.Extensions.TimeSpan.TimeSpanExtensions.OneMicrosecond.Ticks * 20)))
            {
                int count = 0;

                //Handle the event by incrementing count
                t.Tick += (ref long st) => { ++count; };

                //Do the same for another counter
                int anotherCount = 0;

                t.Tick += (ref long st) => { ++anotherCount; };

                //Do the same for another counter
                int lastCounter = 0;

                t.Tick += (ref long st) => { ++lastCounter; };

                System.Diagnostics.Stopwatch testSw = new System.Diagnostics.Stopwatch();

                t.Start();

                testSw.Start();

                System.Console.WriteLine("Frequency: " + t.Frequency);

                System.Console.WriteLine("Started: " + t.m_Started.ToString("MM/dd/yyyy hh:mm:ss.ffffff tt"));

                //Sleep the frequency
                System.TimeSpan s = new System.TimeSpan(((long)t.m_Ticks + testSw.ElapsedTicks + 1));

                while (s < t.Frequency)
                {
                    System.Console.WriteLine((t.m_Clock.UtcNow + s).ToString("MM/dd/yyyy hh:mm:ss.ffffff tt"));
                    //System.Threading.Thread.SpinWait(0);
                    //t.m_Clock.NanoSleep(0);
                    s = new System.TimeSpan(((long)t.m_Ticks + testSw.ElapsedTicks + 1));
                }

                t.Stop();

                testSw.Stop();

                var finished = System.DateTime.UtcNow;

                var taken = finished - t.m_Started;

                System.Console.WriteLine("Finished: " + finished.ToString("MM/dd/yyyy hh:mm:ss.ffffff tt"));

                System.Console.WriteLine("Taken Microseconds: " + Media.Common.Extensions.TimeSpan.TimeSpanExtensions.TotalMicroseconds(taken));

                System.Console.WriteLine("Taken Total: " + taken.ToString());

                System.Console.WriteLine("Managed Ticks: " + t.m_Ticks);

                //The maximum amount of times the timer can elapse in the given frequency
                double maxCount = Media.Common.Extensions.TimeSpan.TimeSpanExtensions.TotalMicroseconds(taken) / Media.Common.Extensions.TimeSpan.TimeSpanExtensions.TotalMicroseconds(t.Frequency);

                System.Console.WriteLine("Maximum Count: " + Common.Extensions.TimeSpan.TimeSpanExtensions.TotalMicroseconds(taken));

                System.Console.WriteLine("Actual Count: " + count);

                System.Console.WriteLine("Another Count: " + anotherCount);

                System.Console.WriteLine("Last Counter: " + lastCounter);

                //100 ns or (1 tick) is equal to about 100 counts in this case
                //Since sleep may not be accurate then there must be atleast 900 counts
                if ((taken > System.TimeSpan.Zero && count < Media.Common.Extensions.TimeSpan.TimeSpanExtensions.MicrosecondsPerMillisecond
                    || //In all cases the count must never be less than what is possible during the physical given frequency
                    count < maxCount)
                    && t.m_Ticks > 0 &&
                    (long)t.m_Ticks < count) throw new System.Exception("Did not count all intervals");

                //Write the rough amount of time taken in nano seconds
                System.Console.WriteLine("Time Estimated Taken: " + count * Media.Common.Extensions.TimeSpan.TimeSpanExtensions.TotalNanoseconds(t.Frequency) + "ns");

                //Write the rough amount of time taken in  micro seconds, only when using the same bus frequency as was sampled in the clock
                System.Console.WriteLine("Time Estimated Taken: " + count * Media.Common.Extensions.TimeSpan.TimeSpanExtensions.TotalMicroseconds(t.Frequency) + "μs");

                //How many more times the event was fired than needed
                double overage = (count - maxCount);

                //Display the amount of operations which were performed more than required.
                System.Console.WriteLine("Count Overage: " + overage);

                System.Console.WriteLine("Ticks Overage: " + System.Math.Abs(overage - testSw.Elapsed.Ticks));
            }
        }
    }
}
