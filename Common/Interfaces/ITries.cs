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

namespace Media.Common.Interfaces
{
    //These concepts are very closely related, to be able to [execute]=> read and write or modify implies Get and Set access when [execute] is not required.

    /// <summary>
    /// Represents an interface which can obtain an instance.
    /// </summary>
    public interface ITryGet
    {
        bool TryGet(out object t);
    }

    /// <summary>
    /// Represents an interface which can obtain an instance.
    /// </summary>
    /// <typeparam name="T">The instance which can be obtained</typeparam>
    public interface ITryGet<T> : ITryGet
    {
        bool TryGet(out T t);
    }

    /// <summary>
    /// Represents an interface which can set an instance.
    /// </summary>
    public interface ITrySet
    {
        bool TrySet(object t);
    }

    /// <summary>
    /// Represents an interface which can set an instance.
    /// </summary>
    /// <typeparam name="T">The instance which can be set</typeparam>
    public interface ITrySet<T> : ITrySet
    {
        bool TrySet(ref T t);
    }

    /// <summary>
    /// Represents an interface which can add an instance.
    /// </summary>
    public interface ITryAdd
    {
        bool TrySet(object t);
    }

    /// <summary>
    /// Represents an interface which can add an instance.
    /// </summary>
    /// <typeparam name="T">The instance which can be added</typeparam>
    public interface ITryAdd<T> : ITryAdd
    {
        bool TryAdd(ref T t);
    }

    /// <summary>
    /// Represents an interface which can remove an instance.
    /// </summary>
    public interface ITryRemove
    {
        bool TryRemove(object t);
    }

    /// <summary>
    /// Represents an interface which can remove an instance.
    /// </summary>
    /// <typeparam name="T">The instance which can be removed</typeparam>
    public interface ITryRemove<T> : ITryRemove
    {
        bool TryRemove(ref T t);
    }
}
