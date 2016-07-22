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
namespace Media.Concepts.Classes
{
    /// <summary>
    /// 
    /// </summary>
    public class UpdateableBase : Common.BaseDisposable, Common.IUpdateable//, Concepts.Interfaces.IUsable
    {
        #region Statics

        /// <summary>
        /// An instance which will never dispose and is always under modification.
        /// </summary>
        public static UpdateableBase AlwaysUnderModification = new UpdateableBase(true, 0, System.TimeSpan.Zero, false);

        #endregion

        #region Fields

        readonly System.Threading.ManualResetEventSlim m_ResetEvent;

        readonly System.Threading.CancellationTokenSource m_TokenSource;

        #endregion

        #region IUpdateable

        System.Threading.ManualResetEventSlim Common.IUpdateable.ManualResetEvent
        {
            get { return m_ResetEvent; }
        }

        System.Threading.CancellationTokenSource Common.IUpdateable.UpdateTokenSource
        {
            get { return m_TokenSource; }
        }

        #endregion

        #region Properties

        public bool UnderModification { get { return Common.IUpdateableExtensions.UnderModification(this); } }

        #endregion

        #region Constrcutor

        public UpdateableBase(bool initialState, int spinCount, System.TimeSpan delay, bool shouldDispose = true)
            :base(shouldDispose)
        {
            m_ResetEvent = new System.Threading.ManualResetEventSlim(initialState, spinCount);

            m_TokenSource = new System.Threading.CancellationTokenSource(delay);
        }

        public UpdateableBase(bool initialState, int spinCount, System.TimeSpan delay, bool shouldDispose = true, params System.Threading.CancellationToken[] token)
            :this(initialState, spinCount, delay, shouldDispose)
        {
            //Recreate the TokenSource from the TokenSource's Token and the TokenSource created from linking the existing tokens.
            m_TokenSource = System.Threading.CancellationTokenSource.CreateLinkedTokenSource(m_TokenSource.Token, 
                    System.Threading.CancellationTokenSource.CreateLinkedTokenSource(token).Token);
        }

        #endregion

        #region Dispose

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (ShouldDispose)
            {
                m_ResetEvent.Dispose();

                //m_ResetEvent = null;

                m_TokenSource.Dispose();

                //m_TokenSource = null;

            }
        }

        #endregion
    }
}
