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

namespace Media.Common.Extensions.Generic.Dictionary
{
    using System;
    using System.Collections.Generic;

    public static class DictionaryExtensions
    {
        public static bool TryAdd<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, ref TKey key, ref TValue value, out Exception any)
        {
            any = null;

            try
            {
                //Check if the key is already contained
                if (dictionary.ContainsKey(key).Equals(false))
                {
                    //Attempt to add
                    dictionary.Add(key, value);
                }

                //Indicate success
                return true;
            }
            catch (Exception ex)
            {
                //Assign the exception
                any = ex;

                //Indicate if a failure occured
                return false;
            }
        }

        public static bool TryAdd<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue value, out Exception any) { return TryAdd(dictionary, ref key, ref value, out any); }

        public static bool TryRemove<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, ref TKey key, out Exception any)
        {
            any = null;

            try
            {
                //Attempt to remove
                return dictionary.Remove(key);
            }
            catch (Exception ex)
            {
                //Assign the exception
                any = ex;

                //Indicate if a failure occured
                return false;
            }
        }

        public static bool TryRemove<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, ref TKey key, out TValue value, out Exception any)
        {
            any = null;

            value = default(TValue);

            try
            {
                //Attempt to remove if contaioned
                if (dictionary.TryGetValue(key, out value))
                {
                    return TryRemove(dictionary, key, out any);
                }

                //The item was not contained
                return false;

            }
            catch (Exception ex)
            {
                //Assign the exception
                any = ex;

                //Indicate if a failure occured
                return false;
            }
        }

        public static bool TryRemove<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, out Exception any) { return TryRemove(dictionary, ref key, out any); }

        public static bool TryRemove<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, ref TKey key)
        {
            Exception any; 
            
            return TryRemove(dictionary, ref key, out any);
        }

        public static bool TryRemove<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, out TValue value) { return TryRemove(dictionary, ref key, out value); }
        
        public static bool TryRemove<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, out TValue value, out Exception any)
        { return TryRemove(dictionary, ref key, out value, out any); }

        public static bool TryRemove<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, ref TKey key, out TValue value)
        {
            Exception ex;

            return TryRemove(dictionary, key, out value, out ex);
        }
    }
}
