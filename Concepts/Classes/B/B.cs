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

namespace Media.Concepts.Classes.B
{
    /// <summary>
    /// An interface which represents the api assoicated with;
    /// </summary>
    public interface IBias : Common.Interfaces.Interface
    {
        bool Conscious { get; }
    }

    /// <summary>
    /// An interface which represents an api to which there is `mens rea` in addition to <see cref="IBias"/>
    /// </summary>
    public interface IDecision : IBias
    {
        /// <summary>
        /// Bad
        /// </summary>
        /// <returns></returns>
        bool Malus();

        /// <summary>
        /// Good
        /// </summary>
        /// <returns></returns>
        bool Bonum();

        /// <summary>
        /// Decide
        /// </summary>
        /// <returns></returns>
        bool Scire();
    }

    /// <summary>
    /// An implementation of the <see cref="IDecision"/> <see cref="Common.Interfaces.Interface"/>
    /// </summary>
    public class Decision : IDecision, Media.Common.Interfaces.ITryGet<bool>, Media.Common.Interfaces.ITrySet<bool>
    {
        #region Statics

        /// <summary>
        /// Performs no logic.
        /// </summary>
        /// <param name="b"></param>
        static void Realize(bool b)
        {
            return;
        }

        /// <summary>
        /// Throws an <see cref="System.Exception"/> when true.
        /// </summary>
        /// <param name="b"></param>
        static void ExceptIfTrue(bool b)
        {
            if (b.Equals(false)) return;

            throw new System.Exception();
        }

        /// <summary>
        /// Throws an <see cref="System.Exception"/> when false.
        /// </summary>
        /// <param name="b"></param>
        static void ExceptIfFalse(bool b)
        {
            if (b) return;

            throw new System.Exception();
        }

        #endregion

        //ref..

        /// <summary>
        /// Judge
        /// </summary>
        System.Action<bool> Iudex;

        /// <summary>
        /// The bias
        /// </summary>
        bool Bias;

        /// <summary>
        /// Indicates the consciousness
        /// </summary>
        public bool Conscious
        {
            get;
            internal protected set;
        }

        /// <summary>
        /// Constructs the instance
        /// </summary>
        /// <param name="bias">The bias</param>
        public Decision(bool bias = false)
        {
            Bias = bias;

            Iudex = Realize;
        }

        #region Methods

        /// <summary>
        /// <see cref="Bias"/> when <see cref="Conscious"/>, otherwise the opposite.
        /// </summary>
        /// <returns></returns>
        public bool ConsciousBias() { return Conscious ? Bias : !Bias; }

        /// <summary>
        /// <see cref="Iudex">Decide</see> if the <see cref="ConsciousBias"/> is good.
        /// </summary>
        /// <returns>True if <see cref="Bias"/> equals <see cref="true"/>, otherwise false</returns>
        public bool Good()
        {
            if (object.ReferenceEquals(Iudex, null)) return false;

            Iudex(ConsciousBias());

            return Bias.Equals(true);
        }

        /// <summary>
        /// <see cref="Iudex">Decide</see> if the <see cref="ConsciousBias"/> is bad.
        /// </summary>
        /// <returns>True if <see cref="Bias"/> equals <see cref="false"/>, otherwise true</returns>
        public bool Bad()
        {
            if (object.ReferenceEquals(Iudex, null)) return false;

            Iudex(ConsciousBias());

            return Bias.Equals(false);
        }

        #endregion

        #region IDecision

        /// <summary>
        /// <see cref="Good"/>
        /// </summary>
        /// <returns></returns>
        bool IDecision.Malus()
        {
            return Good();
        }

        /// <summary>
        /// <see cref="Bad"/>
        /// </summary>
        /// <returns></returns>
        bool IDecision.Bonum()
        {
            return Bad();
        }

        /// <summary>
        /// <see cref="ConsciousBias"/>
        /// </summary>
        /// <returns></returns>
        bool IDecision.Scire()
        {
            return ConsciousBias();
        }

        #endregion

        #region ITries

        /// <summary>
        /// Assign the result after deciding.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        bool Common.Interfaces.ITryGet<bool>.TryGet(out bool t)
        {
            Iudex(t = ConsciousBias());

            return true;
        }

        /// <summary>
        /// Assign this instance and return <see cref="ConsciousBias"/>
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        bool Common.Interfaces.ITryGet.TryGet(out object t)
        {
            t = this;

            return ConsciousBias();
        }

        /// <summary>
        /// Judge the result and assign the bias.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        bool Common.Interfaces.ITrySet<bool>.TrySet(ref bool t)
        {
            Iudex(t);

            Bias = t;

            return true;
        }

        /// <summary>
        /// Assign <see cref="Bias"/> false if null, otherwise true.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        bool Common.Interfaces.ITrySet.TrySet(object t)
        {
            if (object.ReferenceEquals(t, null)) return Bias = false;

            //could be anything in t...

            return Bias = true;
        }

        #endregion
    }

    /// <summary>
    /// The <see cref="Decision"/> which is also <see cref="Media.Common.Interfaces.IGeneric"/>
    /// </summary>
    /// <typeparam name="T">The type</typeparam>
    public class Decision<T> : Decision, Media.Common.Interfaces.IGeneric<T> { }

    //There must exist a bad decision [which is actually biased to be good] because a good descision exists.
    //The only way to equaly balance the two [via anihilation; inter alia] requires the two to be in equals terms ...
    //good + bad = true + false
    //bad + good = false + true
    //
    //bad + bad = bad =?indifferent
    //good + good = good =?indifferent

    //This can be further complicated et freud.
}
