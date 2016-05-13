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

namespace Media.Concepts.Experimental
{
    public interface IComposed//Object
    {
        object ComposedObject { get; }
    }

    //Not really needed
    //public class Composed : IComposed<object>
    //{
    //    public object ComposedObject { get; protected set; }

    //    object IComposed<object>.Composed
    //    {
    //        get { return ComposedObject; }
    //    }
    //}

    /// <summary>
    /// Provides access to a type with no virtual call overhead.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IComposed/*Element*/<T> : IComposed
    {
        T ComposedElement { get; }
    }

    /// <summary>
    /// Provides a definite reference to a <typeparamref name="T"/>.
    /// Provides a middle man class where a sealed type must be inherited.
    /// Provides explicit declaration of a method which corresponds to the <see cref="IComposed"/> interface.
    /// </summary>
    /// <typeparam name="T">The type</typeparam>
    public class ComposedOf/*Element*/<T> : IComposed<T>
    {
        T m_Composed;

        public T ComposedElement
        {
            get { return m_Composed; }
        }

        public ComposedOf(T t)
        {
            m_Composed = t;
        }

        object IComposed.ComposedObject
        {
            get { return m_Composed; }
        }
    }
}
