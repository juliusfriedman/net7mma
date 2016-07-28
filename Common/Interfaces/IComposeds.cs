﻿#region Copyright
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

namespace Media.Common.Interfaces
{
    /// <summary>
    /// Represents a <see cref="Common.Interfaces.Interface">superposition</see> which is `similar` to another instance.
    /// </summary>
    public interface ISimilar : Common.Interfaces.Interface { }
    
    /// <summary>
    /// The <see cref="internal"/> <see cref="interface"/> which extends <see cref="ISimilar"/>
    /// </summary>
    /// <remarks> @ k |K| :=
    /// Not `Limerick`
    /// `clay` - `limi`
    /// `education or` - `ilimi k`
    /// `limit"k"` => `mali bonu`
    /// - https://en.wikipedia.org/wiki/K
    /// </remarks>
    internal interface ILimic : ISimilar { }

    /// <summary>
    /// An <see cref="Interface"/> which is used to convey similaryity to another <see cref="Interface"/>
    /// </summary>
    public interface ISimilarInterface : ISimilar { }

    /// <summary>
    /// An <see cref="Interface"/> which is used to convey similaryity to another <see cref="Class"/>
    /// </summary>
    public interface ISimilarClass : ISimilar, InterClass { }

    /// <summary>
    /// An <see cref="Interface"/> which is used to convey similaryity to another <see cref="Struct"/>
    /// </summary>
    public interface ISimilarStruct : ISimilar, InterStruct { }

    //Generics...

    //Specifics..

    /// <summary>
    /// Represents an interface which can obtain an instance.
    /// The instance obtained is usually of a more or less derived type of the same instance which implements this interface.
    /// </summary>
    public interface IComposed : ISimilar //Object
    {
        object ComposedObject { get; }
    }

    #region Unused [object]

    //Not really needed
    //public class Composed : IComposed<object>
    //{
    //    public object ComposedObject { get; protected set; }

    //    object IComposed<object>.Composed
    //    {
    //        get { return ComposedObject; }
    //    }
    //}

    #endregion

    /// <summary>
    /// Provides access to a type <typeparamref name="T"/>
    /// </summary>
    /// <typeparam name="T">The type</typeparam>
    public interface IComposed/*Element*/<T> : IComposed
    {
        T ComposedElement { get; }
    }

    /// <summary>
    /// Provides a definite reference to a <typeparamref name="T"/>.
    /// Provides a middle man class where a sealed type must be inherited or otherwise
    /// Provides explicit declaration of a method which corresponds to the <see cref="IComposed"/> interface.
    /// </summary>
    /// <typeparam name="T">The type</typeparam>
    public class Composed/*Element*/<T> : IComposed<T>
    {
        T m_Composed;

        public Composed(T t)
        {
            m_Composed = t;
        }

        public T ComposedElement
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_Composed; }
        }

        object IComposed.ComposedObject
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_Composed; }
        }
    }
}
