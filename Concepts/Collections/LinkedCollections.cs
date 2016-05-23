using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Concepts.Collections
{
    /// <summary>
    /// Provides a <see cref="System.Collections.Generic.LinkedList"/>
    /// </summary>
    /// <typeparam name="T">The type of items in the list</typeparam>
    public abstract class LinkedListBase<T> : IEnumerable<T>
    {
        /// <summary>
        /// Constructs the class with the <see cref="Capacity"/> set to <see cref="int.MaxValue"/>
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public LinkedListBase()
        {
            Capacity = int.MaxValue;
        }

        /// <summary>
        /// Constructs the class and sets the capacity
        /// </summary>
        /// <param name="capacity"></param>
        public LinkedListBase(int capacity)
        {
            if (capacity < 0) throw new System.ArgumentException("capacity must be greater than or equal to 0.");

            Capacity = capacity;
        }

        /// <summary>
        /// The amount of elements contained
        /// </summary>
        public int Count
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return LinkedList.Count; }
        }

        /// <summary>
        /// Gets the amount of elements allowed to be contained.
        /// </summary>
        public int Capacity { get; protected set; }

        /// <summary>
        /// Sets <see cref="Count"/> to 0
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public void Clear() { LinkedList.Clear(); }

        /// <summary>
        /// The storage mechanism
        /// </summary>
        protected System.Collections.Generic.LinkedList<T> LinkedList = new LinkedList<T>();        

        /// <summary>
        /// Peek the first item from storage without checking <see cref="Count"/>.
        /// </summary>
        /// <returns></returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        protected T PeekFirst()
        {
            LinkedListNode<T> item = LinkedList.First;

            return item.Value;
        }

        /// <summary>
        /// Peek the last item from storage without checking <see cref="Count"/>.
        /// </summary>
        /// <returns></returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        protected T PeekLast()
        {
            LinkedListNode<T> item = LinkedList.Last;

            return item.Value;
        }

        /// <summary>
        /// Add the given data at the first Node without checking <see cref="Capcity"/>
        /// </summary>
        /// <param name="data"></param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        protected void AddFirst(ref T data)
        {
            LinkedList.AddFirst(data);
        }

        /// <summary>
        /// Add the given data at the last Node without checking <see cref="Capcity"/>
        /// </summary>
        /// <param name="data"></param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        protected void AddLast(ref T data)
        {
            LinkedList.AddLast(data);
        }

        /// <summary>
        /// Remove the First node and assign it's value to data without checking <see cref="Count"/>.
        /// </summary>
        /// <param name="data"></param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        protected void RemoveFirst(out T data)
        {
            LinkedListNode<T> item = LinkedList.First;

            LinkedList.Remove(item);

            data = item.Value;
        }

        /// <summary>
        /// Remove the Last node and assign it's value to data without checking <see cref="Count"/>.
        /// </summary>
        /// <param name="data"></param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        protected void RemoveLast(out T data)
        {
            LinkedListNode<T> item = LinkedList.Last;

            LinkedList.Remove(item);

            data = item.Value;
        }

        /// <summary>
        /// Gets the enumerator of all elements stored
        /// </summary>
        /// <returns></returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return LinkedList.GetEnumerator();
        }

        /// <summary>
        /// Enumerates all stored elements
        /// </summary>
        /// <returns></returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return LinkedList.GetEnumerator();
        }
    }

    /// <summary>
    /// Stack implemented with a <see cref="LinkedListBase"/>
    /// </summary>
    /// <typeparam name="T">Type of elements in the Queue</typeparam>
    public class LinkedStack<T> : LinkedListBase<T>
    {
        /// <summary>
        /// Adds the given data to the beginning of storage
        /// </summary>
        /// <param name="data">The data to add</param>
        public void Push(T data)
        {
            if (Count >= Capacity) return;

            AddFirst(ref data);
        }

        /// <summary>
        /// Attempts to adds the given data to the beginning of storage.
        /// </summary>
        /// <param name="data">The data to add</param>
        /// <returns>True when the operation succeeded, otherwise False.</returns>
        public bool TryPush(ref T data)
        {
            if (Count >= Capacity) return false;

            AddFirst(ref data);

            return true;
        }

        /// <summary>
        /// Attempts to remove the first item from storage.
        /// </summary>
        /// <param name="data">The data to be assigned</param>
        /// <returns>True if the item was removed and assigned otherwise False.</returns>
        public bool TryPop(out T data)
        {
            if (Count <= 0)
            {
                data = default(T);

                return false;
            }

            RemoveFirst(out data);

            return true;
        }

        /// <summary>
        /// Peeks the first item from storage.
        /// </summary>
        /// <returns>The first item stored</returns>
        public T Peek()
        {
            return PeekFirst();
        }

        /// <summary>
        /// Attempts to return the first item from storage.
        /// </summary>
        /// <param name="data">The data assigned</param>
        /// <returns>True if the data was assigned, otherwise False.</returns>
        public bool TryPeek(out T data)
        {
            if (Count <= 0)
            {
                data = default(T);

                return false;
            }

            data = PeekFirst();

            return true;
        }
    }

    /// <summary>
    /// Queue implemented with a <see cref="LinkedListBase"/>
    /// </summary>
    /// <typeparam name="T">Type of elements in the Queue</typeparam>
    public class LinkedQueue<T> : LinkedListBase<T>
    {
        /// <summary>
        /// Adds the given data to the beginning of storage
        /// </summary>
        /// <param name="data">The data to add</param>
        public void Enqueue(T data)
        {
            if (Count >= Capacity) return;

            AddFirst(ref data);
        }

        /// <summary>
        /// Attempts to adds the given data to the beginning of storage.
        /// </summary>
        /// <param name="data">The data to add</param>
        /// <returns>True when the operation succeeded, otherwise False.</returns>
        public bool TryEnqueue(ref T data)
        {
            if (Count >= Capacity) return false;

            AddFirst(ref data);

            return true;
        }

        /// <summary>
        /// Attempts to remove the last item from storage.
        /// </summary>
        /// <param name="data">The data to be assigned</param>
        /// <returns>True if the item was removed and assigned otherwise False.</returns>
        public bool TryDequeue(out T data)
        {
            if (Count <= 0)
            {
                data = default(T);

                return false;
            }

            RemoveLast(out data);

            return true;
        }

        /// <summary>
        /// Peeks the last item from storage.
        /// </summary>
        /// <returns>The last item stored</returns>
        public T Peek()
        {
            return PeekLast();
        }

        /// <summary>
        /// Attempts to return the last item from storage.
        /// </summary>
        /// <param name="data">The data assigned</param>
        /// <returns>True if the data was assigned, otherwise False.</returns>
        public bool TryPeek(out T data)
        {
            if (Count <= 0)
            {
                data = default(T);

                return false;
            }

            data = PeekLast();

            return true;
        }

    }
}
