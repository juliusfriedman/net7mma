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

namespace Media.Common.Extensions.Process
{
    public static class ProcessExtensions
    {
        /// <summary>
        /// Sets the affinity on the current process and optionally the given sub threads.
        /// </summary>
        /// <param name="process">The process to configure or the null if the current process should be used.</param>
        /// <param name="affinityFlags"><see href="https://msdn.microsoft.com/en-us/library/system.diagnostics.processthread.processoraffinity.aspx">MSDN</see></param>
        /// <param name="threads">The number of threads</param>
        /// <param name="idealProcessor">The ideal processor for execution</param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static void SetAffinity(this System.Diagnostics.Process process, long affinityFlags, int threads = 0, int idealProcessor = -1)
        {
            //use the process or the current process.
            process = process ?? System.Diagnostics.Process.GetCurrentProcess();

            //Set the flags
            process.ProcessorAffinity = (System.IntPtr)affinityFlags;

            //If no threads were specified use the count of threads the process already has.
            if (threads < 0) threads = process.Threads.Count;

            //Loop the process threads in reverse
            for (int i = threads - 1; i >= 0; --i)
            {
                //Get the ProcessThread
                System.Diagnostics.ProcessThread Thread = process.Threads[i];

                //Set the affinty
                Thread.ProcessorAffinity = (System.IntPtr)affinityFlags;

                //If there was an ideal processor indicate such.
                if(idealProcessor >= 0) Thread.IdealProcessor = idealProcessor;
            }
        }

        //Todo,
        //This likely really uses SetThreadAffinityMask on Windows and sched_getaffinity on Unix
        //Could just scope those.
    }
}
