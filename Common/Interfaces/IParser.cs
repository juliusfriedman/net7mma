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

//Parsing

namespace Media.Common.Interfaces
{
    /// <summary>
    /// An interface which defines completion and is also <see cref="IMutable"/>
    /// </summary>
    public interface ICompletable : IMutable
    {
        /// <summary>
        /// Called when completion occurs.
        /// </summary>
        /// <param name="final">Indicates if this is the last time this method will be called in the given context</param>
        void OnCompletion(bool final = false);

        bool IsComplete { get; }
    }

    /// <summary>
    /// An interface which represents a parser which is <see cref="ICompletable"/>
    /// </summary>
    interface IParser : ICompletable
    {
        /// <summary>
        /// Called when the state changes
        /// </summary>
        /// <param name="state">The state</param>
        /// <param name="left">The left context</param>
        /// <param name="right">The right context</param>
        void OnStateChanged(ref uint state, ref ulong left, ref ulong right);
    }

    /// <summary>
    /// An implementation of the <see cref="IParser"/> interface.
    /// </summary>
    internal class BasicParser : IParser
    {
        const uint Zero = 0, Distortion = uint.MaxValue, Evolution = 1, Juxtaposition = 2, Complete = int.MaxValue;

        uint State = Zero;

        public DimensionInformationDelegation OnUnknownData { get; protected set; }

        //Todo, returns...

        public System.Action OnEvolution { get; protected set; }  //=> IsComplete was True, yet more data arrived which may have or may not have changed the value of IsComplete

        public System.Action OnRealization { get; protected set; }  //=> IsComplete was True, yet more data arrived which may have or may not have changed the value of IsComplete

        public System.Action OnDistortion { get; protected set; }  //=> OnUnknownData was fired, yet more data arrived which HAS changed the value of IsComplete

        public System.Action OnDisposition { get; protected set; }  //=> OnUnknownData was fired, yet more data arrived which HAS NOT changed the value of IsComplete

        public System.Action OnJuxtaposition { get; protected set; }  //=> Data arrives, left is what is there and right is what is not.

        // Provides all of the above with state determining the result.... (RFPHD)

        void IParser.OnStateChanged(ref uint state, ref ulong left, ref ulong right)
        {
            switch (state)
            {
                case Zero:
                    {
                        ((ICompletable)this).OnCompletion(object.ReferenceEquals(OnRealization, null).Equals(false));

                        break;
                    }
                case Distortion: OnDistortion(); break;
                case Evolution: OnEvolution(); break;
                case Juxtaposition:
                    {
                        if (left.Equals(right))
                        {
                            OnJuxtaposition();
                        }
                        else if (left > right)
                        {
                            OnEvolution(); 
                        }
                        else
                        {
                            OnDisposition(); 
                        }

                        break;
                    }
                default:
                    {
                        if (State.Equals(Complete))
                        {
                            OnRealization();
                        }

                        System.Tuple<int, ulong, ulong> stack = new System.Tuple<int, ulong, ulong>((int)state, left, right);

                        OnUnknownData(ref stack);

                        break;
                    }
            }
        }

        //OnCompletion(bool final = false) final => no more evolution or distortion
        void ICompletable.OnCompletion(bool final)
        {
            State = Complete;
        }

        bool IMutable.Mutable
        {
            get { return true; }
        }

        bool IReadOnly.IsReadOnly
        {
            get { return true; }
        }

        bool IWriteOnly.IsWriteOnly
        {
            get { return true; }
        }

        bool ICompletable.IsComplete
        {
            get { return State.Equals(Complete); }
        }
    }
}
