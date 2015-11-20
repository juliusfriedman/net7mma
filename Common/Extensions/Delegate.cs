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

using System.Linq;

namespace Media.Common.Extensions.Delegate
{
    public static class DelegateExtensions
    {
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

        public static void CreateDelegate(ref System.Delegate self, System.Type type = null)
        {
            if(self != null) foreach(var inv in self.GetInvocationList())
                    self = System.Delegate.Combine(self, System.Delegate.CreateDelegate(type ?? inv.Method.ReturnType, inv.Target, inv.Method));
        }

        public static T ConvertTo<T>(this System.Delegate self)
        {
            return (T)(object)self.ConvertTo(typeof(T));
        }

        public static U As<T, U>(T t) where U : class
        {
            return t as U;
        }

        public static void AppendTo<T>(this System.Delegate newDel, ref T baseDel) where T : class
        {
            baseDel = DelegateExtensions.As<System.Delegate, T>(newDel);
            T oldBaseDel, newBaseDel;
            do
            {
                oldBaseDel = baseDel;
                newBaseDel = (T)(System.Object)System.Delegate.Combine((System.Delegate)(object)oldBaseDel, newDel);
            } while (System.Threading.Interlocked.CompareExchange(ref baseDel, newBaseDel, oldBaseDel) != oldBaseDel);
        }

        public static void SubtractFrom<T>(this System.Delegate newDel, ref T baseDel) where T : class
        {
            baseDel = DelegateExtensions.As<System.Delegate, T>(newDel);
            T oldBaseDel, newBaseDel;
            do
            {
                oldBaseDel = baseDel;
                newBaseDel = (T)(System.Object)System.Delegate.Remove((System.Delegate)(object)oldBaseDel, newDel);
            } while (System.Threading.Interlocked.CompareExchange(ref baseDel, newBaseDel, oldBaseDel) != oldBaseDel);
        }
    }
}
