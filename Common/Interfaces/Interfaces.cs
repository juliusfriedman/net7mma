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

namespace Media.Common.Interfaces
{
    /// <summary>
    /// The <see cref="interface"/> which defines a pattern.
    /// </summary>
    internal interface IPattern : Interfaces.Interface { }

    /// <summary>
    /// The <see cref="interface"/> which defines the local <see href="https://en.wikipedia.org/wiki/Causality">causality</see> [sic].
    /// </summary>
    public interface ISpecific : Interface { }

    /// <summary>
    /// The <see cref="Interface"/> which defines the remote <see href="https://en.wikipedia.org/wiki/Causality">causality</see> [sic].
    /// </summary>
    public interface IGeneric : ISpecific { }

    /// <summary>
    /// The <see cref="interface"/> which is `open` or `higher kinded`.
    /// </summary>
    /// <typeparam name="T">T</typeparam>
    public interface IGeneric<T> { }

    /// <summary>
    /// An interface which contains a <see cref="Class"/>
    /// </summary>
    public interface InterClass
    {
        Media.Common.Classes.Class Class { get; }
    }

    public static class InterClassExtensions //: Media.Concepts.Classes.E.Extensions
    {
        public static InterClass FromObject(object o) { throw new System.NotImplementedException(); }

        public static object ToObject(InterClass ic) { throw new System.NotImplementedException(); }

    }

    /// <summary>
    /// An interface which contains a <see cref="Struct"/>
    /// </summary>
    public interface InterStruct
    {
        Media.Common.Structures.Struct Struct { get; }
    }

    public static class InterStructExtensions //: Media.Concepts.Classes.E.Extensions
    {
        public static InterStruct FromObject(object o) { throw new System.NotImplementedException(); }

        public static object ToObject(InterStruct ic) { throw new System.NotImplementedException(); }

    }
}
