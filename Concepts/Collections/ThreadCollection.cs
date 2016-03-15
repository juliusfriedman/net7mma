using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Concepts.Collections
{
    //A collection which caters to threads
    //A collection which caters to threads
    public class ThreadCollection : System.Collections.ICollection,
        ICollection<System.Threading.Thread>,
        Media.Common.IThreadReference
    {

        public Action<System.Threading.Thread> ConfigureThread { get; set; }

        //MaximumStackSize, MinimumStackSize, PreferredApartmentState

        IEnumerable<System.Threading.Thread> Media.Common.IThreadReference.GetReferencedThreads()
        {
            using (var e = ((ICollection<System.Threading.Thread>)this).GetEnumerator())
            {
                while (e.MoveNext()) yield return e.Current;
            }
        }

        void ICollection<System.Threading.Thread>.Add(System.Threading.Thread item)
        {
            throw new NotImplementedException();
        }

        void ICollection<System.Threading.Thread>.Clear()
        {
            throw new NotImplementedException();
        }

        bool ICollection<System.Threading.Thread>.Contains(System.Threading.Thread item)
        {
            throw new NotImplementedException();
        }

        void ICollection<System.Threading.Thread>.CopyTo(System.Threading.Thread[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        int ICollection<System.Threading.Thread>.Count
        {
            get { throw new NotImplementedException(); }
        }

        bool ICollection<System.Threading.Thread>.IsReadOnly
        {
            get { throw new NotImplementedException(); }
        }

        bool ICollection<System.Threading.Thread>.Remove(System.Threading.Thread item)
        {
            throw new NotImplementedException();
        }

        IEnumerator<System.Threading.Thread> IEnumerable<System.Threading.Thread>.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        void System.Collections.ICollection.CopyTo(Array array, int index)
        {
            throw new NotImplementedException();
        }

        int System.Collections.ICollection.Count
        {
            get { throw new NotImplementedException(); }
        }

        bool System.Collections.ICollection.IsSynchronized
        {
            get { throw new NotImplementedException(); }
        }

        object System.Collections.ICollection.SyncRoot
        {
            get { throw new NotImplementedException(); }
        }
    }
}
