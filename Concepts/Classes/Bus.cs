using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Concepts.Classes
{
    public abstract class Bus : Common.SuppressedFinalizerDisposable
    {
        public readonly Timer Clock = new Timer(Common.Extensions.TimeSpan.TimeSpanExtensions.OneTick);

        public Bus() : base(false) { Clock.Start(); }
    }

    public class ClockedBus : Bus
    {
        long FrequencyHz, Maximum, End;

        readonly Media.Common.Collections.Generic.ConcurrentLinkedQueueSlim<byte[]> Input = new Media.Common.Collections.Generic.ConcurrentLinkedQueueSlim<byte[]>(), Output = new Media.Common.Collections.Generic.ConcurrentLinkedQueueSlim<byte[]>();

        readonly double m_Bias;

        public ClockedBus(long frequencyHz, double bias = 1.5)
        {
            m_Bias = bias;

            cache = Clock.m_Clock.InstructionsPerClockUpdate / 1000;

            SetFrequency(frequencyHz);

            Clock.Tick += Clock_Tick;

            Clock.Start();
        }

        public void SetFrequency(long frequencyHz)
        {
            unchecked
            {
                FrequencyHz = frequencyHz;

                //Clock.m_Frequency = new TimeSpan(Clock.m_Clock.InstructionsPerClockUpdate / 1000); 

                //Maximum = System.TimeSpan.TicksPerSecond / Clock.m_Clock.InstructionsPerClockUpdate;

                //Maximum = Clock.m_Clock.InstructionsPerClockUpdate / System.TimeSpan.TicksPerSecond;

                Maximum = 1 + cache / 1 + (cache / 1 + FrequencyHz);

                Maximum *= System.TimeSpan.TicksPerSecond;

                Maximum = 1 + (cache / 1 + FrequencyHz);

                End = 1 - Maximum * 2;

                Clock.m_Frequency = new TimeSpan(Maximum);

                if (cache < frequencyHz * m_Bias) throw new Exception("Cannot obtain stable clock");

                try { if (Clock.Producer.Count > 10) Clock.Producer.Clear(); }
                catch { }
            } 
        }

        public override void Dispose()
        {
            ShouldDispose = true;

            Clock.Tick -= Clock_Tick;

            Clock.Stop();

            Clock.Dispose();

            base.Dispose();
        }

        long sample = 0, steps = 0, count = 0, avg = 0, elapsed = 0, cache = 1;

        bool inv;

        void Clock_Tick(ref long ticks)
        {
            if (ShouldDispose == false && false == IsDisposed)
            {
                //Console.WriteLine("@ops=>" + Clock.m_Ops + " @ticks=>" + Clock.m_Ticks + " @Lticks=>" + ticks + "@=>" + Clock.m_Clock.Now.TimeOfDay + "@=>" + (Clock.m_Clock.Now - Clock.m_Clock.Created));

                steps = sample;

                sample = ticks;

                ++count;

                System.ConsoleColor f = System.Console.ForegroundColor;

                if (count <= Maximum)
                {
                    System.Console.BackgroundColor = inv ? ConsoleColor.Black : ConsoleColor.Yellow;

                    System.Console.ForegroundColor = inv ? ConsoleColor.Yellow : ConsoleColor.Green;

                    //Console.WriteLine("count=> " + count + "@=>" + Clock.m_Clock.Now.TimeOfDay + "@=>" + (Clock.m_Clock.Now - Clock.m_Clock.Created) + " - " + DateTime.UtcNow.ToString("MM/dd/yyyy hh:mm:ss.ffffff tt"));

                    Console.Write(".");

                    avg += Maximum / count;

                    if (Clock.m_Clock.InstructionsPerClockUpdate / count > Maximum)
                    {
                        System.Console.BackgroundColor = inv ? ConsoleColor.Blue : ConsoleColor.Black;

                        System.Console.ForegroundColor = inv ? ConsoleColor.Black : ConsoleColor.Blue;

                        //Console.WriteLine("---- Over InstructionsPerClockUpdate ----" + FrequencyHz);
                        Console.Write(" ");
                    }
                }
                else if (count >= End)
                {
                    ++elapsed;

                    inv = Common.Binary.IsPowerOfTwo(ref elapsed);

                    System.Console.BackgroundColor = inv ? ConsoleColor.Blue : ConsoleColor.Black;

                    System.Console.ForegroundColor = inv ? ConsoleColor.Black : ConsoleColor.Blue;

                    avg += Maximum / count;

                    avg /= elapsed;

                    //Console.WriteLine("avg=> " + avg + "@=>" + FrequencyHz);

                    Console.Write("-");

                    count = 0;
                }
            }
        }

        //Read, Write at Frequency

    }
}

namespace Media.UnitTests
{
    internal class BusTests
    {

        public void TestForOneHert()
        {
            System.Console.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);

            using (Media.Concepts.Classes.ClockedBus cb = new Concepts.Classes.ClockedBus(1))
            {
                while (false == System.Console.KeyAvailable || System.Console.ReadKey(true).Key == ConsoleKey.Q)
                {
                    //cb.Clock.m_Clock.NanoSleep((long)Common.Extensions.TimeSpan.TimeSpanExtensions.TotalNanoseconds(Common.Extensions.TimeSpan.TimeSpanExtensions.OneMicrosecond));
                    if (System.Console.KeyAvailable) break;
                }
            }

            System.Console.WriteLine("Done");
        }

        //Blocks
        public void TestForFiveHertz()
        {
            System.Console.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);

            using (Media.Concepts.Classes.ClockedBus cb = new Concepts.Classes.ClockedBus(5))
            {
                while (false == System.Console.KeyAvailable || System.Console.ReadKey(true).Key == ConsoleKey.Q)
                {
                    //cb.Clock.m_Clock.NanoSleep((long)Common.Extensions.TimeSpan.TimeSpanExtensions.TotalNanoseconds(Common.Extensions.TimeSpan.TimeSpanExtensions.OneMicrosecond));
                    if (System.Console.KeyAvailable) break;
                }
            }

            System.Console.WriteLine("Done");
        }

        public void TestForSeven()
        {
            System.Console.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);

            using (Media.Concepts.Classes.ClockedBus cb = new Concepts.Classes.ClockedBus(7))
            {
                while (false == System.Console.KeyAvailable)
                {
                    //cb.Clock.m_Clock.NanoSleep((long)Common.Extensions.TimeSpan.TimeSpanExtensions.TotalNanoseconds(Common.Extensions.TimeSpan.TimeSpanExtensions.OneMicrosecond));
                    if (System.Console.KeyAvailable) break;
                }
            }

            while (System.Console.KeyAvailable) System.Console.ReadKey(true);

            System.Console.WriteLine("Done");
        }

        public void TestVariable1x10x1()
        {
            System.Console.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);

            int times = 1;

            using (Media.Concepts.Classes.ClockedBus cb = new Concepts.Classes.ClockedBus(times))
            {
                while (false == System.Console.KeyAvailable) for (int i = 2, e = 21; i < e; ++i)
                {
                    if (System.Console.KeyAvailable) goto Done;

                    cb.Clock.m_Clock.NanoSleep(i * i * 10);

                    if (System.Console.KeyAvailable) goto Done;

                    if (i <= 10) cb.SetFrequency(times * i);
                    else cb.SetFrequency((e - i) * times);

                    if (System.Console.KeyAvailable) goto Done;

                    cb.Clock.m_Clock.NanoSleep(i * i * 100);

                    if (System.Console.KeyAvailable) goto Done;
                }
            }

            Done:

            while (System.Console.KeyAvailable) System.Console.ReadKey(true);

            System.Console.WriteLine("Done");
        }

        public void TestVariables()
        {
            System.Console.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);

            int times = 2;

            using (Media.Concepts.Classes.ClockedBus cb = new Concepts.Classes.ClockedBus(times))
            {
                while (false == System.Console.KeyAvailable) for (int i = 0, e = System.Console.WindowWidth; i < e; ++i)
                        for (int j = 0, z = System.Console.WindowHeight; i < e; ++i)
                    {
                        if (System.Console.KeyAvailable) goto Done;

                        System.Console.SetCursorPosition(i, j);

                        cb.Clock.m_Clock.NanoSleep(i * i * 10);

                        if (System.Console.KeyAvailable) goto Done;

                        if (i <= 10) cb.SetFrequency(times * i);
                        else cb.SetFrequency((e - i) * times);

                        if (System.Console.KeyAvailable) goto Done;
                    }
            }

        Done:

            while (System.Console.KeyAvailable) System.Console.ReadKey(true);

            System.Console.WriteLine("Done");
        }

        public void TestForTenHertz()
        {
            System.Console.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);

            using (Media.Concepts.Classes.ClockedBus cb = new Concepts.Classes.ClockedBus(10))
            {
                while (false == System.Console.KeyAvailable || System.Console.ReadKey(true).Key == ConsoleKey.Q)
                {
                    //cb.Clock.m_Clock.NanoSleep((long)Common.Extensions.TimeSpan.TimeSpanExtensions.TotalNanoseconds(Common.Extensions.TimeSpan.TimeSpanExtensions.OneMicrosecond));
                    if (System.Console.KeyAvailable) break;
                }
            }

            while (System.Console.KeyAvailable) System.Console.ReadKey(true);

            System.Console.WriteLine("Done");

        }

        public void TestForOneHundredSeventyFiveHertz()
        {
            System.Console.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);

            using (Media.Concepts.Classes.ClockedBus cb = new Concepts.Classes.ClockedBus(175))
            {
                while (false == System.Console.KeyAvailable)
                {
                    //cb.Clock.m_Clock.NanoSleep((long)Common.Extensions.TimeSpan.TimeSpanExtensions.TotalNanoseconds(Common.Extensions.TimeSpan.TimeSpanExtensions.OneMicrosecond));
                    if (System.Console.KeyAvailable) break;
                }
            }

            while (System.Console.KeyAvailable) System.Console.ReadKey(true);

            System.Console.WriteLine("Done");
            
        }

        public void TestForSevenHundredMegaHertz()
        {
            using (Media.Concepts.Classes.ClockedBus cb = new Concepts.Classes.ClockedBus(700))
            {
                while (false == System.Console.KeyAvailable)
                {
                    //cb.Clock.m_Clock.NanoSleep((long)Common.Extensions.TimeSpan.TimeSpanExtensions.TotalNanoseconds(Common.Extensions.TimeSpan.TimeSpanExtensions.OneMicrosecond));
                    if (System.Console.KeyAvailable) break;
                }
            }

            while (System.Console.KeyAvailable) System.Console.ReadKey(true);

            System.Console.WriteLine("Done");

        }

        public void TestForOneMegaHertz()
        {
            System.Console.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);

            using (Media.Concepts.Classes.ClockedBus cb = new Concepts.Classes.ClockedBus(1000))
            {
                while (false == System.Console.KeyAvailable)
                {
                    //cb.Clock.m_Clock.NanoSleep((long)Common.Extensions.TimeSpan.TimeSpanExtensions.TotalNanoseconds(Common.Extensions.TimeSpan.TimeSpanExtensions.OneMicrosecond));
                    if (System.Console.KeyAvailable) break;
                }
            }

            while (System.Console.KeyAvailable) System.Console.ReadKey(true);

            System.Console.WriteLine("Done");
        }
    }
}
