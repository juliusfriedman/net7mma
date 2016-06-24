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

namespace Media.Concepts.Classes.GC
{
    /// <summary>
    /// Uses threads  
    /// </summary>
    public static class Controller
    {
        /// <summary>
        /// The time to spend in controlation, must be set before calling <see cref="Start"/>
        /// </summary>
        public static System.TimeSpan Timeout = Media.Common.Extensions.TimeSpan.TimeSpanExtensions.OneMillisecond;

        /// <summary>
        /// The thread
        /// </summary>
        internal static System.Threading.Thread ControlationThread;

        /// <summary>
        /// The result
        /// </summary>
        internal static System.IAsyncResult CurrentResult;

        /// <summary>
        /// Enters, waits <see cref="Timeout"/> and Exits on the result.
        /// </summary>
        /// <param name="result">If null nothing occurs</param>
        static void Controlation(System.IAsyncResult result)
        {
            if (result == null) return;

            System.Threading.Monitor.Enter(result);

            System.Threading.Monitor.Wait(result, Timeout);

            System.Threading.Monitor.Exit(result);
        }

        /// <summary>
        /// 
        /// </summary>
        static void ControlationLogic()
        {
            System.TimeSpan HalfTimeout = System.TimeSpan.FromTicks(Timeout.Ticks >> 1);

            System.DateTime lastControlation;

        Start:

            System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.BelowNormal;

            lastControlation = System.DateTime.UtcNow;

            CurrentResult = Media.Common.Extensions.Delegate.ActionExtensions.NoOp.BeginInvoke(Controlation, null);

            //use the handle on the already allocated result which was obtained by calling BeginAccept.
            using (System.Threading.WaitHandle handle = CurrentResult.AsyncWaitHandle)
            {
                System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Lowest;

                //Wait half the timeout using the event
                while (false.Equals(CurrentResult.IsCompleted) && false.Equals(handle.WaitOne(HalfTimeout)))
                {
                    //Check for stop or completion and wait the other half
                    if (CurrentResult.IsCompleted) continue;
                    else if (System.DateTime.UtcNow - lastControlation >= Timeout) break;
                }
            }

            CurrentResult = null;

            goto Start;
        }

        public static bool InControlation
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get
            {
                return false.Equals(object.ReferenceEquals(CurrentResult, null)) && System.Threading.Monitor.IsEntered(CurrentResult);
            }
        }

        public static bool IsAlive
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get
            {
                return false.Equals(object.ReferenceEquals(ControlationThread, null)) && ControlationThread.IsAlive;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static void Start()
        {
            if (ControlationThread == null)
            {
                ControlationThread = new System.Threading.Thread(ControlationLogic);

                ControlationThread.Start();
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static void Stop()
        {
            if (false.Equals(IsAlive)) Media.Common.Extensions.Thread.ThreadExtensions.AbortAndFree(ref ControlationThread, Timeout);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static void Suspend()
        {
           if(IsAlive) ControlationThread.Suspend();
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static void Resume()
        {
            if (false.Equals(IsAlive)) ControlationThread.Resume();
        }
    }
}
