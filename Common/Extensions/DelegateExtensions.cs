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
namespace Media.Common.Extensions.Delegate
{
    using System.Linq;

    /// <summary>
    /// Provides extension methods which are useful for working with delegates
    /// </summary>
    public static class DelegateExtensions
    {

        public static readonly System.Type TypeOfDelegate = typeof(System.Delegate);

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static System.Delegate ConvertTo(this System.Delegate self, System.Type type)
        {
            if (type == null) { throw new System.ArgumentNullException("type"); }
            if (self == null) { return null; }

            if (self.GetType() == type)
                return self;

            return System.Delegate.Combine(
                self.GetInvocationList()
                    .Select(i => System.Delegate.CreateDelegate(type, i.Target, i.Method))
                    .ToArray());
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static void CreateDelegate(ref System.Delegate self, System.Type type = null)
        {
            if(self != null) foreach(var inv in self.GetInvocationList())
                    self = System.Delegate.Combine(self, System.Delegate.CreateDelegate(type ?? inv.Method.ReturnType, inv.Target, inv.Method));
        }

        /// <summary>
        /// Creates a delegate from a MethodInfo using <see cref="System.Linq.Expressions.Expression.GetDelegateType"/>
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        public static System.Delegate CreateDelegate(System.Reflection.MethodInfo method)
        {
            if (method == null) throw new System.ArgumentNullException("method");

            return method.CreateDelegate(System.Linq.Expressions.Expression.GetDelegateType(method.GetParameters().Select(p => p.ParameterType).Concat(Media.Common.Extensions.Linq.LinqExtensions.Yield(method.ReturnType)).ToArray()));
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static T ConvertTo<T>(this System.Delegate self)
        {
            return (T)(object)self.ConvertTo(typeof(T));
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static void AppendTo<T>(this System.Delegate newDel, ref T baseDel) where T : class
        {
            baseDel = Common.Extensions.Generic.GenericExtensions.As<System.Delegate, T>(newDel);
            T oldBaseDel, newBaseDel;
            do
            {
                oldBaseDel = baseDel;
                newBaseDel = (T)(System.Object)System.Delegate.Combine((System.Delegate)(object)oldBaseDel, newDel);
            } while (System.Threading.Interlocked.CompareExchange(ref baseDel, newBaseDel, oldBaseDel) != oldBaseDel);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static void SubtractFrom<T>(this System.Delegate newDel, ref T baseDel) where T : class
        {
            baseDel = Common.Extensions.Generic.GenericExtensions.As<System.Delegate, T>(newDel);
            T oldBaseDel, newBaseDel;
            do
            {
                oldBaseDel = baseDel;
                newBaseDel = (T)(System.Object)System.Delegate.Remove((System.Delegate)(object)oldBaseDel, newDel);
            } while (System.Threading.Interlocked.CompareExchange(ref baseDel, newBaseDel, oldBaseDel) != oldBaseDel);
        }
    }

    //http://www.codeproject.com/Articles/1104555/The-Function-Decorator-Pattern-Reanimation-of-Func @ ActionExtensions
    public static class FuncExtensions
    {
        public static System.Func<TArg, TResult> GetOrCache<TArg, TResult, TCache>(this System.Func<TArg, TResult> func, TCache cache) 
            where TCache : class, System.Collections.Generic.IDictionary<TArg, TResult>
        {
            return (arg) =>
            {
                TResult value;

                if (cache.TryGetValue(arg, out value))
                {
                    return value;
                }

                value = func(arg);

                cache.Add(arg, value);

                return value;
            };
        }
  
        public static System.Func<TArg, TResult> WaitExecute<TArg, TResult>(this System.Func<TArg, TResult> func, System.TimeSpan amount)
        {
            return (arg) =>
            {
                // added functionality
                System.Threading.Thread.Sleep(amount);

                // original functionality
                return func(arg);
            };
        }

        public static System.Func<TArg, TResult> RetryIfFailed<TArg, TResult>(this System.Func<TArg, TResult> func, int maxRetry)
        {
            return (arg) =>
            {
                int t = 0;
                do
                {
                    try
                    {
                        return func(arg);
                    }
                    catch (System.Exception)
                    {
                        if (++t > maxRetry)
                        {
                            throw;
                        }
                    }
                } while (true);
            };
        }
    }
}
