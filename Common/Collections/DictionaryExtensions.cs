using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Common.Collections
{
    public static class DictionaryExtensions
    {
        public static bool TryAdd<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue value, out Exception any)
        {
            any = null;

            try
            {
                //Check if the key is already contained
                if (false == dictionary.ContainsKey(key))
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

        public static bool TryRemove<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, out Exception any)
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

        public static bool TryRemove<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, out TValue value, out Exception any)
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
    }
}
