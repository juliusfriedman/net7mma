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

#region Using Statements

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#endregion

namespace Media.Common//.Collections
{
    #region ConcurrentThesaurus

    /// <summary>
    /// Represents a One to many collection which is backed by a ConcurrentDictionary.
    /// The values are retrieved as a IList of TKey
    /// </summary>
    /// <typeparam name="TKey">The type of the keys</typeparam>
    /// <typeparam name="TValue">The types of the definitions</typeparam>
    /// <remarks>
    /// Fancy tryin to get a IDictionary to flatten into this
    /// </remarks>
    public class ConcurrentThesaurus<TKey, TValue> : ILookup<TKey, TValue>, ICollection<TKey>
    {
        #region Properties

        System.Collections.Concurrent.ConcurrentDictionary<TKey, IList<TValue>> Dictionary = new System.Collections.Concurrent.ConcurrentDictionary<TKey, IList<TValue>>();

        ICollection<TKey> Collection { get { return (ICollection<TKey>)Dictionary; } }

        public int Count { get { return Dictionary.Count; } }

        public IEnumerable<TKey> Keys { get { return Dictionary.Keys; } }

        #endregion

        #region Methods

        public void Clear() { Dictionary.Clear(); }


        public bool ContainsKey(TKey key)
        {
            return Dictionary.ContainsKey(key);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        public void Add(TKey key)
        {
            if (!CoreAdd(key, default(TValue), null, false, true))
            {
                //throw new ArgumentException("The given key was already present in the dictionary");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Add(TKey key, TValue value)
        {
            IList<TValue> Predicates;

            //Attempt to get the value
            bool hadValue = Dictionary.TryGetValue(key, out Predicates);

            //Add the value
            if (!CoreAdd(key, value, Predicates, hadValue, false))
            {
                //throw new ArgumentException("The given key was already present in the dictionary");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool Remove(TKey key)
        {
            IList<TValue> removed;
            return Remove(key, out removed);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public bool Remove(TKey key, out IList<TValue> values)
        {
            return Dictionary.TryRemove(key, out values);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="predicates"></param>
        /// <param name="inDictionary"></param>
        /// <param name="allocteOnly"></param>
        /// <returns></returns>
        internal bool CoreAdd(TKey key, TValue value, IList<TValue> predicates, bool inDictionary, bool allocteOnly)
        {
            //If the predicates for the key are null then create them with the given value
            if (allocteOnly) predicates = new List<TValue>();
            else if (predicates == null) predicates = new List<TValue>() { value };
            else predicates.Add(value);//Othewise add the value to the predicates which is a reference to the key

            //Add the value if not already in the dictionary
            if (!inDictionary) return Dictionary.TryAdd(key, predicates);

            return true;    
        }

        //public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        //{
        //    foreach (var list in Dictionary)
        //        foreach (var value in list.Value)
        //            yield return new KeyValuePair<TKey, TValue>(list.Key, value);
        //}

        #endregion

        #region Interface Implementation

        public IList<TValue> this[TKey key]
        {
            get
            {
                return Dictionary[key];
            }
            set
            {
                Dictionary[key] = value;
            }
        }

        bool ILookup<TKey, TValue>.Contains(TKey key)
        {
            return ContainsKey(key);
        }

        int ILookup<TKey, TValue>.Count
        {
            get { return Dictionary.Count; }
        }

        IEnumerable<TValue> ILookup<TKey, TValue>.this[TKey key]
        {
            get { return this[key]; }
        }

        IEnumerator<IGrouping<TKey, TValue>> IEnumerable<IGrouping<TKey, TValue>>.GetEnumerator()
        {
            return (IEnumerator<IGrouping<TKey, TValue>>)Dictionary.ToLookup(kvp => kvp.Key).GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return Dictionary.GetEnumerator();
        }

        void ICollection<TKey>.Add(TKey item)
        {
            Collection.Add(item);
        }

        void ICollection<TKey>.Clear()
        {
            Collection.Clear();
        }

        bool ICollection<TKey>.Contains(TKey item)
        {
            return Collection.Contains(item);
        }

        void ICollection<TKey>.CopyTo(TKey[] array, int arrayIndex)
        {
            Collection.CopyTo(array, arrayIndex);
        }

        int ICollection<TKey>.Count
        {
            get { return Collection.Count; }
        }

        bool ICollection<TKey>.IsReadOnly
        {
            get { return Collection.IsReadOnly; }
        }

        bool ICollection<TKey>.Remove(TKey item)
        {
            return Collection.Remove(item);
        }

        IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator()
        {
            return Collection.GetEnumerator();
        }

        #endregion
    }

    #endregion
}
