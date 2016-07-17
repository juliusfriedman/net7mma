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
namespace Media.Common
{
    /// <summary>
    /// Supports a basic pattern for concurrent access which can also be <see cref="IDisposed">disposed</see>.
    /// </summary>
    public interface IUpdateable : IDisposed //Could seperate IDisposed also and have IUsable.
    {
        /// <summary>
        /// 
        /// </summary>
        System.Threading.ManualResetEventSlim ManualResetEvent { get; } // = new System.Threading.ManualResetEventSlim(true);

        /// <summary>
        /// 
        /// </summary>
        System.Threading.CancellationTokenSource UpdateTokenSource { get; } //= new System.Threading.CancellationTokenSource();
        
        //System.Threading.CancellationToken LastToken { get; }

        //System.Threading.CancellationToken BeginUpdate(); //out Token ^

        //void EndUpdate(System.Threading.CancellationToken token);
    }

    /// <summary>
    /// Provides extension methods which can be used to ensure concurrent access of an instance which implements <see cref="IUpdateable"/>
    /// </summary>
    public static class IUpdateableExtensions
    {
        /// <summary>
        /// Indicates if the instance is not null or disposed and that the <see cref="ManualResetEvent.IsSet"/> is not true 
        /// OR
        /// That the <see cref="UpdateTokenSource.IsCancellationRequested"/> is true.
        /// </summary>
        /// <param name="updateable">The instance</param>
        /// <returns>True or False</returns>
        public static bool UnderModification(this IUpdateable updateable)
        {
            if (Common.IDisposedExtensions.IsNullOrDisposed(updateable)) return false;

            return updateable.ManualResetEvent.IsSet.Equals(false) && updateable.UpdateTokenSource.IsCancellationRequested.Equals(false);
        }

        public static bool UnderModification(this IUpdateable updateable, System.Threading.CancellationToken token)
        {
            return object.ReferenceEquals(token, null).Equals(false) && 
                Common.IDisposedExtensions.IsNullOrDisposed(updateable).Equals(false) &&
                token.Equals(updateable.UpdateTokenSource.Token);
        }


        public static System.Threading.CancellationToken BeginUpdate(this IUpdateable updateable, out bool reset)
        {
            if (Common.IDisposedExtensions.IsNullOrDisposed(updateable)) throw new System.ArgumentNullException(); //return default(System.Threading.CancellationToken);

            if (reset = System.Threading.WaitHandle.SignalAndWait(updateable.UpdateTokenSource.Token.WaitHandle, updateable.ManualResetEvent.WaitHandle))
            {
                updateable.ManualResetEvent.Reset();
                
                reset = true;
            }

            return updateable.UpdateTokenSource.Token;
        }

        public static System.Threading.CancellationToken BeginUpdateIn(this IUpdateable updateable, System.TimeSpan amount, ref bool exitContext, out bool reset)
        {
            if (Common.IDisposedExtensions.IsNullOrDisposed(updateable)) throw new System.ArgumentNullException();

            if (reset = System.Threading.WaitHandle.SignalAndWait(updateable.UpdateTokenSource.Token.WaitHandle, updateable.ManualResetEvent.WaitHandle, amount, exitContext))
            {
                updateable.ManualResetEvent.Reset();
            }

            return updateable.UpdateTokenSource.Token;
        }

        static readonly System.InvalidOperationException InvalidStateException = new System.InvalidOperationException("Must obtain the CancellationToken from a call to BeginUpdate.");

        public static void EndUpdate(this IUpdateable updateable, System.Threading.CancellationToken token)
        {
            if (Common.IDisposedExtensions.IsNullOrDisposed(updateable)) throw new System.ArgumentNullException(); //return default(System.Threading.CancellationToken);

            //Ensure a token
            if (object.ReferenceEquals(token, null)) return;

            //That came from out cancellation source
            if (token.Equals(updateable.UpdateTokenSource.Token).Equals(false)) throw InvalidStateException;

            // check for manually removed state or a call without an update..
            //if(m_Update.Wait(1, token)) { would check that the event was manually cleared... }

            // acknowledge cancellation 
            if (token.IsCancellationRequested) throw new System.OperationCanceledException(token);

            //Allow threads to modify
            updateable.ManualResetEvent.Set(); //To unblocked
        }
    }
}
