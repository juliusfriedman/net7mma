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

namespace Media.Common.Collections.Generic
{
    /// <summary>
    /// 
    /// </summary>
    class PrecicseList : Media.Common.Interfaces.ISharedList
    {
        //Used to floor, ciel, etc for the index on int versions
        //int Precision;

        System.Collections.Generic.SortedList<double, object> List = new System.Collections.Generic.SortedList<double, object>();

        int System.Collections.IList.Add(object value)
        {
            List.Add(List.Count, value);

            return List.Count;
        }

        void System.Collections.IList.Clear()
        {
            List.Clear();
        }

        bool System.Collections.IList.Contains(object value)
        {
            return List.IndexOfValue(value) >= 0;
        }

        int System.Collections.IList.IndexOf(object value)
        {
            return List.IndexOfValue(value);
        }

        void System.Collections.IList.Insert(int index, object value)
        {
            List.Add(index, value);
        }

        bool System.Collections.IList.IsFixedSize
        {
            get { return false; }
        }

        bool System.Collections.IList.IsReadOnly
        {
            get { return false; }
        }

        void System.Collections.IList.Remove(object value)
        {
            int index;

            while ((index = List.IndexOfValue(value)) >= 0) List.RemoveAt(index);
        }

        void System.Collections.IList.RemoveAt(int index)
        {
            List.RemoveAt(index);
        }

        object System.Collections.IList.this[int index]
        {
            get
            {
                return List[index];
            }
            set
            {
                List[index] = value;
            }
        }

        void System.Collections.ICollection.CopyTo(System.Array array, int index)
        {
            throw new System.NotImplementedException();
        }

        int System.Collections.ICollection.Count
        {
            get { return List.Count; }
        }

        bool System.Collections.ICollection.IsSynchronized
        {
            get { return false; }
        }

        object System.Collections.ICollection.SyncRoot
        {
            get { return List; }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return List.GetEnumerator();
        }

        bool Interfaces.IShared.IsShared
        {
            get { return false; }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    class PrecisionIndexableList : PrecicseList, Media.Common.Interfaces.IndexableList
    {
        long UpperBound, LowerBound;

        long Interfaces.Indexable.LowerBound
        {
            get { return LowerBound; }
        }

        long Interfaces.Indexable.UpperBound
        {
            get { return UpperBound; }
        }
    }

    //`genus`
}
