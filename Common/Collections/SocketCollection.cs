using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Common.Collections
{
    /// <summary>
    /// A collection which caters to Sockets
    /// </summary>
    public class SocketCollection : System.Collections.ICollection, 
        ICollection<System.Net.Sockets.Socket>, 
        ISocketReference
    {
        IEnumerable<System.Net.Sockets.Socket> ISocketReference.GetReferencedSockets()
        {
            using (var e = ((ICollection<System.Net.Sockets.Socket>)this).GetEnumerator())
            {
                while (e.MoveNext()) yield return e.Current;
            }
        }

        public void Add(System.Net.Sockets.Socket item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(System.Net.Sockets.Socket item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(System.Net.Sockets.Socket[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public int Count
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsReadOnly
        {
            get { throw new NotImplementedException(); }
        }

        public bool Remove(System.Net.Sockets.Socket item)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<System.Net.Sockets.Socket> GetEnumerator()
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
