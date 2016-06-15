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
namespace Media.Concepts.Interfaces
{
    //Relative as IsReadOnly can potentially change from time to time?

    /// <summary>
    /// Represents an interface which can indicate if the instance is shared by other instances.
    /// </summary>
    public interface IShared
    {
        /// <summary>
        /// Indicates if the instance is shared by other instanced.
        /// </summary>
        bool IsShared { get; }
    }

    /// <summary>
    /// Represents a generic <see cref="IShared"/> instance which is <see cref="Media.Concepts.Experimental.IComposed"/> of that the same type.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IShared<T> : IShared, Media.Concepts.Experimental.IComposed<T>
    {
        
    }

    /// <summary>
    /// Represents a <see cref="IShared"/> <see cref="System.Collections.IList"/>
    /// </summary>
    public interface ISharedList : IShared, System.Collections.IList
    {

    }

    /// <summary>
    /// Represents a <see cref="IShared"/> <see cref="System.Collections.Generic.IList{T}"/>
    /// </summary>
    public interface ISharedList<T> : IShared, System.Collections.Generic.IList<T>
    {

    }

    /// <summary>
    /// An <see cref="ISharedList{T}"/> <see cref="System.Collections.Generic.List{T}"/>
    /// </summary>
    /// <typeparam name="T">The type of element</typeparam>
    public class SharedList<T> : System.Collections.Generic.List<T>, ISharedList<T>
    {
        /// <summary>
        /// A value which is used to indicate if the instance is shared.
        /// </summary>
        bool m_IsShared;

        /// <summary>
        /// Indicates if the list is shared.
        /// </summary>
        public bool IsShared
        {
            get { return m_IsShared; }

            protected set { m_IsShared = value; }
        }
    }

    /// <summary>
    /// Represents a <see cref="IShared"/> <see cref="System.Collections.ICollection"/>
    /// </summary>
    public interface ISharedCollection : IShared, System.Collections.ICollection
    {

    }

    /// <summary>
    /// Represents a <see cref="IShared"/> <see cref="System.Collections.ICollection{T}"/>
    /// </summary>
    public interface ISharedCollection<T> : ISharedCollection, System.Collections.Generic.ICollection<T>
    {

    }
}
