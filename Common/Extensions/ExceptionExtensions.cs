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

namespace Media.Common.Extensions.Exception
{
    /// <summary>
    /// Provides methods which allow detection of <see cref="System.Exception"/> state as well as methods to raise exceptions.
    /// </summary>
    public static class ExceptionExtensions
    {
        /// <summary>
        /// Check if we are in a exception unwind scenario or not.
        /// </summary>
        public static bool InException
        {
            get
            {
                //See http://geekswithblogs.net/akraus1/archive/2008/04/08/121121.aspx
                return System.Runtime.InteropServices.Marshal.GetExceptionPointers() == System.IntPtr.Zero && System.Runtime.InteropServices.Marshal.GetExceptionCode() == 0 ? false : true;
            }
        }

        //http://stackoverflow.com/questions/3007608/resuming-execution-of-code-after-exception-is-thrown-and-caught
        public static System.Exception ResumeOnError(System.Action action)
        {
            try
            {
                action();
                return null;
            }
            catch (System.Exception caught)
            {
                return caught;
            }
        }

    }
}
