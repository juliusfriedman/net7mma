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

namespace Media.Common.Extensions.Thread
{
    public static class ThreadExtensions
    {
        /// <summary>
        /// Will use the maximum stack size available for creation of a thread.
        /// </summary>
        public const int DefaultStackSize = 0;

        /// <summary>
        /// Will use the smallest amount of stack size available for the creation of a thread.
        /// </summary>
        public const int MinimumStackSize = 1;

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static bool IsRunning(System.Threading.Thread thread)
        {
            return thread != null && (thread.ThreadState & (System.Threading.ThreadState.Stopped | System.Threading.ThreadState.Unstarted)) == 0;
        }

        public static void AbortAndFree(ref System.Threading.Thread thread, System.Threading.ThreadState state = System.Threading.ThreadState.Stopped, int timeout = (int)Common.Extensions.TimeSpan.TimeSpanExtensions.MicrosecondsPerMillisecond)
        {
            //If the worker IsAlive and has the requested state.
            if (thread != null && (thread.IsAlive && thread.ThreadState.HasFlag(state)))
            {
                //Attempt to join
                if (false == thread.Join(timeout))
                {
                    try
                    {
                        //Abort
                        thread.Abort();
                    }
                    catch (System.Threading.ThreadAbortException) { System.Threading.Thread.ResetAbort(); }
                    catch { throw; } //Cancellation not supported
                }

                //Reset the state of the thread to indicate success
                thread = null;
            }
        }

        public static void AbortAndFree(ref System.Threading.Thread thread, System.TimeSpan timeout, System.Threading.ThreadState state = System.Threading.ThreadState.Stopped)
        {
            //If the worker IsAlive and has doesn't have the requested state.
            if (thread != null && false == thread.ThreadState.HasFlag(state)) //IsRunning(thread) &&
            {
                //Attempt to join if not already, todo check flags are compatible in all implementations.
                if (false == thread.ThreadState.HasFlag(System.Threading.ThreadState.AbortRequested | System.Threading.ThreadState.Aborted) && false == thread.Join(timeout))
                {
                    try
                    {
                        //Abort
                        thread.Abort();
                    }
                    catch (System.Threading.ThreadAbortException) { System.Threading.Thread.ResetAbort(); }
                    catch { throw; } //Cancellation not supported
                }
            }

            //Reset the state of the thread to indicate success
            thread = null;
        }

        public static bool TryAbortAndFree(ref System.Threading.Thread thread, System.Threading.ThreadState state = System.Threading.ThreadState.Stopped, int timeout = (int)Common.Extensions.TimeSpan.TimeSpanExtensions.MicrosecondsPerMillisecond)
        {
            try { AbortAndFree(ref thread, state, timeout); }
            catch { return false; }

            return thread == null;
        }

        public static bool TryAbortAndFree(ref System.Threading.Thread thread, System.TimeSpan timeout, System.Threading.ThreadState state = System.Threading.ThreadState.Stopped)
        {
            try { AbortAndFree(ref thread, timeout, state); }
            catch { return false; }

            return thread == null;
        }
    }
}
