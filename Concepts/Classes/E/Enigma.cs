namespace Media.Concepts.Classes.E
{
    /// <summary>
    /// An <see cref="Media.Common.Interfaces.Interface"/>
    /// </summary>
    /// <remarks>
    /// `a person or thing that is mysterious, puzzling, or difficult to understand.`
    /// </remarks>
    public interface Ainigma : Media.Common.Interfaces.ISimilarInterface { }

    /// <summary>
    /// <see cref="Media.Common.Classes.Abstraction"/> or <see cref="Ainigma"/>
    /// The `Enigma` machines;
    /// were a series of electro-mechanical rotor cipher machines developed and used in the early- to mid-twentieth century to protect commercial, 
    /// diplomatic and military communication. 
    /// - Wikipedia
    /// </summary>
    public abstract class Enigma : Media.Common.Classes.Abstraction, Media.Common.MachineInterface, Ainigma
    {
        //
    }

    /// <summary>
    /// a soldier or guard whose job is to stand and keep watch.
    /// station a soldier or guard by (a place) to keep watch. - 'a wide course had been roped off and sentineled with police'
    /// </summary>
    public class Sentinel : Media.Common.Classes.Class, Media.Common.Interfaces.InterClass
    {
        /// <summary>
        /// `this`
        /// </summary>
        Common.Classes.Class Common.Interfaces.InterClass.Class
        {
            get { return this; }
        }
    }

    /// <summary>
    /// An implemenation of <see cref="Enigma"/> which contains a <see cref="Sentinel"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Sentinel<T> : Enigma, Media.Common.Interfaces.ITryGet<T>, Media.Common.Interfaces.IComposed<T>, Media.Common.Interfaces.InterClass
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static Sentinel<T> Guard(T t)
        {
            return new Sentinel<T>()
            {
                Key = t
            };
        }

        /// <summary>
        /// The <typeparam name="T">element</typeparam>
        /// </summary>
        internal protected T Key;

        /// <summary>
        /// The <see cref="Sentinel"/>
        /// </summary>
        public Sentinel Sentient { get; internal protected set; }

        bool Common.Interfaces.ITryGet<T>.TryGet(out T t)
        {
            t = Key;

            return true;
        }

        bool Common.Interfaces.ITryGet.TryGet(out object t)
        {
            t = Key;

            return true;
        }

        T Common.Interfaces.IComposed<T>.ComposedElement
        {
            get { return Key; }
        }

        object Common.Interfaces.IComposed.ComposedObject
        {
            get { return this; }
        }

        Common.Classes.Class Common.Interfaces.InterClass.Class
        {
            get { return Sentient; }
        }
    }
}
