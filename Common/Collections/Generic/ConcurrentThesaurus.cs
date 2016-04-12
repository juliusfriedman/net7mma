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

using Media.Common.Extensions.Generic.Dictionary;
using System;
using System.Collections.Generic;
using System.Linq;//ILookup

#endregion

namespace Media.Common.Collections.Generic
{

    //internal class DictionaryRefTryGetValue<TKey, TValue> : Dictionary<TKey, TValue>
    //{
    //    //needs to have/get access to the private's of the dictionary....
    //    internal bool TryGetValue(ref TKey key, out TValue value)
    //    {
    //        return base.TryGetValue(key, out value);
    //    }
    //}

    #region ConcurrentThesaurus

    /// <summary>
    /// Represents a One to many collection which is backed by a ConcurrentDictionary.
    /// The values are retrieved as a IEnumerable of TKey and duplicate values may exist if inserted.
    /// </summary>
    /// <typeparam name="TKey">The type of the keys</typeparam>
    /// <typeparam name="TValue">The types of the definitions</typeparam>
    /// <remarks>
    /// Fancy tryin to get a IDictionary to flatten into this.
    /// GroupBy gives an IGrouping but we can't use that or GroupBy to implement `IEnumerable<IGrouping<TKey, TValue>>.GetEnumerator()` unless IEnumerable of TValue is used.
    /// </remarks>
    public class ConcurrentThesaurus<TKey, TValue> : ILookup<TKey, TValue>, ICollection<TKey>
    {
        #region Static

        static TValue DefaultValue = default(TValue);

        #endregion

        #region Properties

        Dictionary<TKey, IList<TValue>> Dictionary = new Dictionary<TKey, IList<TValue>>();

        System.Collections.ICollection Collection { get { return ((System.Collections.ICollection)Dictionary); } }

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
            if (false == CoreAdd(ref key, ref DefaultValue, null, false, true))
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
            //IList<TValue> Predicates;

            ////Attempt to get the value
            //bool hadValue = Dictionary.TryGetValue(key, out Predicates);

            ////Add the value
            //if (false == CoreAdd(ref key, ref value, Predicates, hadValue, false))
            //{
            //    //throw new ArgumentException("The given key was already present in the dictionary");
            //}

            Add(ref key, ref value);
        }

        [CLSCompliant(false)]
        public void Add(ref TKey key, ref TValue value)
        {
            IList<TValue> Predicates;

            //Attempt to get the value
            bool hadValue = Dictionary.TryGetValue(key, out Predicates);

            //Add the value
            if (false == CoreAdd(ref key, ref value, Predicates, hadValue, false))
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
            IEnumerable<TValue> removed;

            return Remove(key, out removed);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public bool Remove(TKey key, out IEnumerable<TValue> values)
        {
            //Exception any;
            //IList<TValue> list;
            //bool result = Dictionary.TryRemove(ref key, out list, out any);
            //values = list;
            //return result;

            return Remove(ref key, out values);
        }

        [CLSCompliant(false)]
        public bool Remove(ref TKey key, out IEnumerable<TValue> values)
        {
            Exception any;
            
            IList<TValue> list;
            
            bool result = Dictionary.TryRemove(ref key, out list, out any);
            
            values = list;

            return result;
        }

        //Remove TKey, TValue (Remove a single value from possibly many)

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="results"></param>
        /// <returns></returns>
        public bool TryGetValue(TKey key, out IEnumerable<TValue> results)
        {
            //IList<TValue> values;
            //bool result = Dictionary.TryGetValue(key, out values);
            //results = values;
            //return result;
            return TryGetValue(ref key, out results);
        }

        [CLSCompliant(false)]
        public bool TryGetValue(ref TKey key, out IEnumerable<TValue> results)
        {
            IList<TValue> values;

            //Can't really avoid the non ref here without subclassing Dictionary. (See DictionaryRefTryGetValue above)
            bool result = Dictionary.TryGetValue(key, out values);

            results = values;

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="predicates"></param>
        /// <param name="inDictionary"></param>
        /// <param name="allocateOnly"></param>
        /// <returns></returns>
        internal bool CoreAdd(ref TKey key, ref TValue value, IList<TValue> predicates, bool inDictionary, bool allocateOnly)
        {
            //If the predicates for the key are null then create them with the given value
            if (allocateOnly) predicates = new List<TValue>();
            else if (predicates == null) predicates = new List<TValue>() { value }; //value may be DefaultValue which maybe null
            else predicates.Add(value);//Othewise add the value to the predicates which is a reference to the key

            Exception any;

            //Add the value if not already in the dictionary
            return false == inDictionary ? Dictionary.TryAdd(ref key, ref predicates, out any) : true;
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            foreach (var list in Dictionary)
                foreach (var value in list.Value)
                    yield return new KeyValuePair<TKey, TValue>(list.Key, value);
        }

        #endregion

        #region Interface Implementation

        public IEnumerable<TValue> this[TKey key]
        {
            get
            {
                return Dictionary[key];
            }
        }

        bool ILookup<TKey, TValue>.Contains(TKey key)
        {
            return Dictionary.ContainsKey(key);
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
            //return (IEnumerator<IGrouping<TKey, TValue>>)Generic.Dictionary.SelectMany(p => p.Value, Tuple.Create).ToLookup(p => p.Item1.Key, p => p.Item2);

            //return (IEnumerator<IGrouping<TKey, TValue>>)Generic.Dictionary.GroupBy(k => k.Key);

            foreach (var list in Dictionary) yield return new Grouping<TKey, TValue>(list.Key, list.Value);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return Dictionary.GetEnumerator();
        }

        void ICollection<TKey>.Add(TKey item)
        {
            Add(item);
        }

        void ICollection<TKey>.Clear()
        {
            Dictionary.Clear();
        }

        bool ICollection<TKey>.Contains(TKey item)
        {
            return Dictionary.ContainsKey(item);
        }

        void ICollection<TKey>.CopyTo(TKey[] array, int arrayIndex)
        {
            Dictionary.Keys.CopyTo(array, arrayIndex);
        }

        int ICollection<TKey>.Count
        {
            get { return Collection.Count; }
        }

        bool ICollection<TKey>.IsReadOnly
        {
            get { return false; }
        }

        bool ICollection<TKey>.Remove(TKey item)
        {
            return Remove(item);
        }

        IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator()
        {
            return Dictionary.Keys.GetEnumerator();
        }

        #endregion
    }

    #endregion

    //Maybe a ConcurrentThesaurus<TStorage> where TStorage : ICollection
}
