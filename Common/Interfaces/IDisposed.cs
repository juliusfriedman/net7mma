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

namespace Media.Common
{
    //ICanDispose / IShouldDispose

    //IDisposeAware
    #region IDisposed

    /// <summary>
    /// Defines an interface which expands on <see cref="IDisposable"/> with an IsDisposed property.
    /// Implementers of this type usually only throw <see cref="System.ObjectDisposedExcpetions"/> when desired, typically <see cref="BaseDisposable.CheckDisposed"/> is called to enfore this.
    /// </summary>
    public interface IDisposed : System.IDisposable
    {
        /// <summary>
        /// Indicates if the instance is Disposed.
        /// </summary>
        bool IsDisposed { get; }

        /// <summary>
        /// Indicates if calls to <see cref="Dispose"/> will effect this instance.
        /// </summary>
        bool ShouldDispose { get; }
    }

    #endregion

    public static class IDisposedExtensions
    {
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static bool IsNullOrDisposed(this IDisposed dispose)
        {
            return object.ReferenceEquals(dispose, null) || dispose.IsDisposed;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static bool ShouldDisposed(this IDisposed dispose)
        {
            return false.Equals(object.ReferenceEquals(dispose, null)) && dispose.ShouldDispose;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static void CheckDisposed(this IDisposed dispose)
        {
            if (false.Equals(object.ReferenceEquals(dispose, null)) && dispose.IsDisposed) throw new System.ObjectDisposedException("IDisposedExtensions.CheckDisposed,true");
        }

        //public static void SetShouldDispose(this IDisposed dispose, bool value, bool callDispose = false)
        //{
        //    if (IDisposedExtensions.IsNullOrDisposed(dispose)) return;

        //    dispose.ShouldDispose = value;

        //    if (callDispose) dispose.Dispose();
        //}
    }

}
