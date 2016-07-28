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

namespace Media.Common.Classes
{
    /// <summary>
    /// An <see cref="interface"/> intended to support the super position of a future defined <see cref="class">Class</see>
    /// </summary>
    public interface IClass { }

    /// <summary>
    /// A <see cref="class"/>
    /// </summary>
    public class Class : IClass //internal @Experimental.I.ICore
    {
        //WIP
        //ToString =>  //ToClassString
        //GetHashCode => GetClassHashCode()
        //Equals => IsClassEqual()
    }

    /// <summary>
    /// An <see cref="Interface"/> which defines <see cref="abstract"/>
    /// </summary>
    public interface IAbstract : Common.Interfaces.Interface { }

    /// <summary>
    /// A <see cref="abstract"/> <see cref="Class"/>
    /// </summary>
    public abstract class Abstraction : Class, IAbstract { }

    /// <summary>
    /// A derived <see cref="Class"/> with a generic type.
    /// </summary>
    /// <typeparam name="T">The element type</typeparam>
    public class Class<T> : Class
    {
        //^%
    }

    /// <summary>
    /// An <see cref="Abstraction"/> which is also an <see cref="Media.Common.Interfaces.InterClass"/>
    /// </summary>
    public class Enum : Abstraction, Media.Common.Interfaces.InterClass
    {
        //System.Enum SystemEnum;

        Class Interfaces.InterClass.Class
        {
            get { return this; }
        }
    }

    /// <summary>
    /// A <see cref="Abstraction"/> which is also an <see cref="Interfaces.Interface "/>
    /// </summary>
    public abstract class ClassInterface : Abstraction, Interfaces.Interface
    {
        //*/
    }

    /// <summary>
    /// A derived <see cref="ClassInterface"/> which is <see cref="abstract"/>
    /// </summary>
    /// <typeparam name="T">The element type</typeparam>
    public abstract class ClassInterface<T> : ClassInterface
    {
        ///*
    }
}
