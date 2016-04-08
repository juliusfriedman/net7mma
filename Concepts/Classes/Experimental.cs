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

using Media.Common.Extensions.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#endregion

namespace Media.Concepts.Experimental
{
    #region Experimental

    #region SocketPrecisionWaitHandle

    /// <summary>
    /// Use SocketAsyncEventArgs to have IOCP Workers which can potentially bypass time barriers due to APC
    /// </summary>
    internal class SocketPrecisionWaitHandle : System.Threading.WaitHandle
    {

    }

    #endregion

    #region Iterators

    public class Iterator<T> :
        EnumerableSegment<T>, //Rather then EnumerableSegment<T> Have no base class maybe and copy the logic and have Array access so iterators can set members? Maybe use caching style?
        Iterator
    {
        #region Properties

        public DateTime Started { get; internal protected set; }
        public DateTime Ended { get; internal protected set; }
        public Iterator Parent { get; protected set; }

        public IEnumerable<T> Enumerable { get; protected set; }

        public IEnumerator<T> Enumerator { get; protected set; }

        #endregion

        #region Overrides

        public override T Current
        {
            get
            {
                Exception ex = null;
                try
                {
                    T current = Current;
                    if(current != null) try { OnCurrentRead(); }
                    catch (Exception _) { ex = _; }
                    return base.Current;
                }
                finally { if (ex != null) throw ex; }
            }
            internal protected set
            {
                try { base.t = value; }
                finally { CurrentAssigned(this); }
            }
        }

        public override bool MoveNext()
        {
            bool error = false;
            try
            {
                if (Index == -1) OnBegin();
                try { OnPreIncrement(); }
                catch { error = true; }
                finally { error = error || false == base.MoveNext(); }
                IEnumerator<T> enumerator = Enumerator;
                error = false == enumerator.MoveNext();
                if (false == error) Current = (T)enumerator.Current;
                return false == error;
            }
            finally { OnPostIncrement(); if (error || Index > VirtualCount) OnEnd(); }
        }

        #endregion

        #region Constructor

        public Iterator() : base() { AssignEvents(this); }

        public Iterator(IEnumerable<T> enumerable) : base(enumerable) { AssignEvents(this); Enumerable = enumerable; Enumerator = Enumerable.GetEnumerator(); }

        public Iterator(IEnumerable<T> enumerable, int index, int count) : base(enumerable, index, count) { AssignEvents(this); Enumerable = enumerable; Enumerator = Enumerable.GetEnumerator(); }

        static void AssignEvents(Iterator<T> iterator)
        {
            iterator.PostDecrement += IteratorPostDecrement;
            iterator.PostIncrement += IteratorPostIncrement;
            iterator.PreIncrement += IteratorPreIncrement;
            iterator.PreDecrement += IteratorPreDecrement;

            iterator.CurrentAssigned += IteratorCurrentAssigned;
            iterator.CurrentRead += IteratorCurrentRead;

            iterator.Begin += IteratorBegin;
            iterator.End += IteratorEnd;
        }

        static void IteratorEnd(Iterator sender)
        {
            (sender as Iterator<T>).Ended = DateTime.UtcNow;
        }

        static void IteratorBegin(Iterator sender)
        {
            (sender as Iterator<T>).Started = DateTime.UtcNow;
        }

        static void IteratorPostIncrement(Iterator sender)
        {
            //Reserved
        }

        static void IteratorCurrentRead(Iterator sender)
        {
            //Reserved
        }

        static void IteratorCurrentAssigned(Iterator sender)
        {
            //Reserved
        }

        static void IteratorPreIncrement(Iterator sender)
        {
            //Reserved
        }

        static void IteratorPreDecrement(Iterator sender)
        {
            //Reserved
        }

        static void IteratorPostDecrement(Iterator sender)
        {
            //Reserved
        }

        static void IteratorPostIncrment(Iterator sender)
        {
            //Reserved
        }

        #endregion

        #region Iterator

        public static Iterator operator +(Iterator<T> it, Iterator<T> that)
        {
            return (it as IEnumerable<T>).Concat(that) as Iterator;
        }

        public static Iterator operator -(Iterator<T> it, Iterator<T> that)
        {
            return (it as IEnumerable<T>).Skip(that.Count) as Iterator;
        }

        int Iterator.VirtualCount
        {
            get { return base.VirtualCount; }
        }

        int Iterator.VirtualIndex
        {
            get { return base.VirtualIndex; }
        }

        /// <summary>
        /// Set to -1 to restart the Iteration from the VirtualIndex
        /// </summary>
        int Iterator.CurrentIndex
        {
            get
            {
                return base.Index;
            }
            set
            {
                if (value < 0) base.Reset();
                base.Index = value;
            }
        }

        public void SetCurrent(T current) { base.Current = current; }

        public T GetCurrent(T current) { return base.Current; }

        #endregion

        #region Events

        internal void OnBegin()
        {
            Begin(this);
        }

        internal void OnEnd()
        {
            End(this);
        }

        internal void OnPreIncrement()
        {
            PreIncrement(this);
        }

        internal protected void OnPostIncrement()
        {
            PostIncrement(this);
        }

        internal void OnPreDecrement()
        {
            PreDecrement(this);
        }

        internal protected void OnPostDecrement()
        {
            PostDecrement(this);
        }

        internal protected void OnSiblingIncrement()
        {
            SiblingIncrement(this);
        }

        internal protected void OnSiblingDecrement()
        {
            SiblingDecrement(this);
        }

        internal protected void OnCurrentRead()
        {
            CurrentRead(this);
        }

        internal protected void OnCurrentAssigned()
        {
            CurrentAssigned(this);
        }

        public event IterationHanlder PreIncrement;

        public event IterationHanlder PreDecrement;

        public event IterationHanlder PostIncrement;

        public event IterationHanlder PostDecrement;

        public event IterationHanlder SiblingIncrement;

        public event IterationHanlder SiblingDecrement;

        public event IterationHanlder CurrentRead;

        public event IterationHanlder CurrentAssigned;

        public event IterationHanlder Begin;

        public event IterationHanlder End;

        #endregion

        #region IEnumerator

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this;
        }

        object System.Collections.IEnumerator.Current
        {
            get { return (this as Iterator<T>).Current; }
        }

        bool System.Collections.IEnumerator.MoveNext()
        {
            return (this as Iterator<T>).MoveNext();
        }

        void System.Collections.IEnumerator.Reset()
        {
            (this as Iterator<T>).Reset();
        }

        #endregion

        #region Statics

        public static void ForAll()
        {
        }

        public static bool Any()
        {
            return false;
        }

        #endregion



    }

    public delegate void IterationHanlder(Iterator sender);

    public interface Iterator : System.Collections.IEnumerable, System.Collections.IEnumerator, IDisposable
    {
        event IterationHanlder Begin;
        event IterationHanlder End;

        int VirtualCount { get; }
        int VirtualIndex { get; }

        int CurrentIndex { get; set; }

        event IterationHanlder PreIncrement;
        event IterationHanlder PreDecrement;
        event IterationHanlder PostIncrement;
        event IterationHanlder PostDecrement;
        event IterationHanlder SiblingIncrement;
        event IterationHanlder SiblingDecrement;

        event IterationHanlder CurrentRead;
        event IterationHanlder CurrentAssigned;
    }

    #endregion

    #region EnumerableSegment

    /// <summary>
    /// Enumerable base class with offset and count.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class EnumerableSegment<T> : IEnumerable<T>, IEnumerator<T>
    {
        internal protected bool m_Disposed;

        //The T currently associated with the Current property
        internal protected T t;

        //The place values indicating where the EnumerableSegment starts and stops
        internal protected readonly int VirtualIndex, VirtualCount;

        /// <summary>
        /// The current index of the EnumerableSegment where the Current property was retrieved from if enumerating this class in a index based fashion.
        /// </summary>
        public int Index { get; protected set; }

        /// <summary>
        /// The amount of items contained in the EnumerableSegment
        /// </summary>
        public int Count { get { return VirtualCount; } }

        public virtual T Current
        {
            get { if (Index == -1) throw new InvalidOperationException("Enumeration has not started call MoveNext"); return t; }
            internal protected set { t = value; }
        }

        void IDisposable.Dispose()
        {
            m_Disposed = true;
        }

        /// <summary>
        /// Causes Index to increase by 1 and returns if the Index is <= VirtualCount
        /// </summary>
        /// <returns></returns>
        public virtual bool MoveNext()
        {
            if (m_Disposed) return false;
            else return ++Index <= VirtualCount;
        }

        /// <summary>
        /// Boxes t as Object
        /// </summary>
        object System.Collections.IEnumerator.Current
        {
            get { if (Index < 0) throw new InvalidOperationException("Enumeration has not started call MoveNext"); return t; }
        }

        bool System.Collections.IEnumerator.MoveNext()
        {
            if (m_Disposed) return false;
            else return MoveNext();
        }

        void System.Collections.IEnumerator.Reset()
        {
            if (m_Disposed) return;
            Index = VirtualIndex;
            t = default(T);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return this;
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this;
        }

        public virtual int Remaining
        {
            get { return Index > VirtualCount ? 0 : VirtualCount - Index; }
        }

        public EnumerableSegment(IEnumerable<T> enumerable, int start = 0, int count = -1)
        {
            Index = VirtualIndex = start;
            if (count == -1) VirtualCount = enumerable.Count();
            else VirtualCount = count;
        }

        public EnumerableSegment(T item)
        {
            VirtualIndex = Index = 0;
            Current = item;
            VirtualCount = 1;
        }

        public EnumerableSegment() { Index = -1; }

        public virtual void Reset() { Index = VirtualIndex; }

        public static void ForAll(IEnumerable<T> enumerable, Action<T> action, int index = 0, int count = -1)
        {
            foreach (T t in new EnumerableSegment<T>(enumerable, index, count))
                action(t);
        }

        public static bool Any(IEnumerable<T> enumerable, Func<T, bool> predicate, int index = 0, int count = -1)
        {
            foreach (T t in new EnumerableSegment<T>(enumerable, index, count))
                if (predicate(t)) return true;
            return false;
        }

        public static bool All(IEnumerable<T> enumerable, Func<T, bool> predicate, int index = 0, int count = -1)
        {
            foreach (T t in new EnumerableSegment<T>(enumerable, index, count))
                if (!predicate(t)) return false;
            return true;
        }

        public static bool Some(IEnumerable<T> enumerable, Func<T, bool> predicate, int index = 0, int count = -1)
        {
            bool one = false;
            foreach (T t in new EnumerableSegment<T>(enumerable, index, count))
                if (predicate(t)) one = true;
            return one;
        }

        public static IEnumerable<T> Where(IEnumerable<T> enumerable, Func<T, bool> selector, int index = 0, int count = -1)
        {
            return new EnumerableSegment<T>(enumerable, index, count).Where(selector);
        }

        public static IEnumerable<T> OrderBy(IEnumerable<T> enumerable, Func<T, bool> selector, int index = 0, int count = -1)
        {
            return new EnumerableSegment<T>(enumerable, index, count).OrderBy(selector);
        }

        public static T FirstOrDefault(IEnumerable<T> enumerable, int index = 0, int count = -1)
        {
            return new EnumerableSegment<T>(enumerable, index, count).FirstOrDefault();
        }

        public static T LasttOrDefault(IEnumerable<T> enumerable, int index = 0, int count = -1)
        {
            return new EnumerableSegment<T>(enumerable, index, count).LastOrDefault();
        }

        public static decimal Sum(IEnumerable<T> enumerable, Func<T, decimal> selector, int index = 0, int count = -1)
        {
            return new EnumerableSegment<T>(enumerable, index, count).Sum(selector);
        }

        public static T Aggregate(IEnumerable<T> enumerable, Func<T, T, T> func, int index = 0, int count = -1)
        {
            return new EnumerableSegment<T>(enumerable, index, count).Aggregate(func);
        }

        //--

    }

    #endregion

    #region ArraySegmentList<T>

    /// <summary>
    /// Allows creation of items delemited by a given segment capacity.
    /// </summary>
    /// <typeparam name="T">The type of items contained</typeparam>
    public class ArraySegmentList<T> :
        IList<T>, //Acts as single IList of T
        IEnumerable<IList<T>>, //You can enumerate by IList contained (Less call overhead)
        IEnumerable<List<T>>, //You can also enumerate by each List contained 
        IEnumerable<ArraySegment<T>> //You can Generate ArraySegment<T> for each List<T> contained by using the Segments property
    {
        public readonly int SegmentCapacity = -1;

        List<List<T>> m_Segments = new List<List<T>>();

        List<T> m_CurrentSegment = new List<T>();

        public IEnumerable<ArraySegment<T>> Segments
        {
            get { return (this as IEnumerable<ArraySegment<T>>); }
        }

        public IEnumerable<List<T>> Lists
        {
            get { return (this as IEnumerable<List<T>>); }
        }

        /// <summary>
        /// Used to partition data into segments of a maximum capacity
        /// Creates a ArraySegmentList with the SegmentCapacity set to the given value.
        /// If -1 is used then no maximum size if given to the capacity
        /// </summary>
        /// <param name="segmentCapacity"></param>
        public ArraySegmentList(int segmentCapacity = -1)
        {
            SegmentCapacity = segmentCapacity;
        }

        void EnsureCapacity(int amount = 1)
        {
            if (m_CurrentSegment.Count + amount > SegmentCapacity)
            {
                m_Segments.Add(new List<T>(m_CurrentSegment.ToArray()));
                m_CurrentSegment.Clear();
            }
        }

        public T[] ToArray()
        {
            return ToArray(0, Count);
        }

        public T[] ToArray(int offset, int index)
        {
            T[] result = new T[Count - index];
            CopyTo(result, index);
            return result;
        }

        public int IndexOf(T item) { return IndexOf(item, 0); }

        public int IndexOf(T item, int index = -1)
        {
            //Check the current segment if the index allows
            if (index < SegmentCapacity) index = m_CurrentSegment.IndexOf(item, Math.Max(0, index));
            if (index != -1) return index;
            else index = 0;
            foreach (List<T> segment in m_Segments)
            {
                int localIndex = segment.IndexOf(item, index);
                if (localIndex != -1) return index + localIndex;
                else index += segment.Count;
            }
            return -1;
        }

        public void Insert(int index, T item)
        {
            List<T> found = FindSegmentForIndex(ref index);
            if (found != default(List<T>) && found.Count < SegmentCapacity) found.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            List<T> found = FindSegmentForIndex(ref index);
            found.RemoveAt(index);
        }

        List<T> FindSegmentForIndex(ref int index)
        {
            foreach (List<T> segment in m_Segments)
            {
                if (index >= SegmentCapacity) index -= segment.Count;
                else return segment;
            }
            return m_CurrentSegment;
        }

        public T this[int index]
        {
            get
            {
                List<T> found = FindSegmentForIndex(ref index);
                if (found != null) return found[Math.Min(found.Count - 1, index)];
                else return default(T);
            }
            set
            {
                List<T> found = FindSegmentForIndex(ref index);
                if (found != null) found[index] = value;
            }
        }

        public void Add(T item)
        {
            EnsureCapacity(1);
            m_CurrentSegment.Add(item);
        }

        public void AddRange(IEnumerable<T> items, int start = 0, int count = -1)
        {
            if (count == -1) count = items.Count();
            EnsureCapacity(count);
            //NEEDS Utility from Media
            //m_CurrentSegment.AddRange(items, start, count);
        }

        public void RemoveRange(int start, int count)
        {
            List<T> found = FindSegmentForIndex(ref start);
            if (found == null) return;
            while (--count > 0) found.RemoveAt(start);
        }

        public void Clear()
        {
            m_Segments.Clear();
            m_CurrentSegment.Clear();
        }

        public bool Contains(T item)
        {
            return m_CurrentSegment.Contains(item) || m_Segments.Any(s => s.Contains(item));
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            foreach (List<T> segment in m_Segments)
            {
                segment.CopyTo(array, arrayIndex);
                arrayIndex += segment.Count;
            }
            m_CurrentSegment.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return m_CurrentSegment.Count + m_Segments.Sum(s => s.Count); }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(T item) { return Remove(item, false); }

        public bool Remove(T item, bool allSegments = false)
        {
            bool removed = false;
            if (allSegments) foreach (List<T> segment in m_Segments) removed = segment.Remove(item) || removed;
            return removed = m_CurrentSegment.Remove(item) || removed;
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return m_Segments.SelectMany(s => s.ToArray()).Concat(m_CurrentSegment.ToArray()).GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return m_Segments.Concat(m_CurrentSegment.Yield()).GetEnumerator();
        }

        public IEnumerator<ArraySegment<T>> GetEnumerator()
        {
            IEnumerable<ArraySegment<T>> result = Enumerable.Empty<ArraySegment<T>>();
            foreach (List<T> segment in m_Segments)
                result = result.Concat(new ArraySegment<T>(segment.ToArray(), 0, segment.Count).Yield());
            result = result.Concat(new ArraySegment<T>(m_CurrentSegment.ToArray(), 0, m_CurrentSegment.Count).Yield());
            return result.GetEnumerator();
        }

        IEnumerator<IList<T>> IEnumerable<IList<T>>.GetEnumerator()
        {
            IEnumerable<IList<T>> result = Enumerable.Empty<IList<T>>();
            foreach (IList<T> segment in m_Segments as IList<T>)
                result = result.Concat((segment as IList<T>).Yield());
            (m_CurrentSegment as IList<T>).Yield();
            return result.GetEnumerator();
        }

        IEnumerator<List<T>> IEnumerable<List<T>>.GetEnumerator()
        {
            IEnumerable<List<T>> result = Enumerable.Empty<List<T>>();
            foreach (List<T> segment in m_Segments)
                result = result.Concat(segment.Yield());
            (m_CurrentSegment as List<T>).Yield();
            return result.GetEnumerator();
        }
    }

    #endregion

    #region Stream Classes

    #region EnumerableByteStream

    public class EnumerableByteStream : EnumerableSegment<byte>, IEnumerable<byte>, IEnumerator<byte>, IList<byte>
    {
        protected int m_Current;

        internal protected System.IO.Stream m_Stream;

        internal protected bool m_Disposed;

        internal EnumerableByteStream m_Self;

        bool Initialized { get { return CurrentInt != -1; } }

        public int CurrentInt { get { return Current; } }

        public EnumerableByteStream(byte Byte) : this(Byte.Yield()) { }

        public EnumerableByteStream(IEnumerable<byte> bytes, int? index = null, int? count = null)
            : base(bytes, index ?? 0, (int)(count.HasValue ? count - index : -1)) { }

        public EnumerableByteStream(System.IO.Stream stream, int? index = null, int? count = null)
            : this(null as IEnumerable<byte>, index ?? (int)stream.Position, count ?? (int)stream.Length)
        {
            m_Stream = stream;
            m_Current = Current;
        }

        //public override int Remaining
        //{
        //    get
        //    {
        //        return (int)(m_Stream .Length + base.Remaining);
        //    }
        //}

        public IEnumerable<byte> ToArray(int offset, int count, byte[] buffer)
        {
            if (offset < 0) throw new ArgumentOutOfRangeException("offset must refer to a location within the buffer.");
            else if (count + offset > Length) throw new ArgumentOutOfRangeException("count must refer to a location within the buffer with respect to offset.");

            if (count == 0) return Enumerable.Empty<byte>();
            buffer = buffer ?? new byte[count];
            int len = count;
            while ((len -= m_Stream.Read(buffer, offset, count)) > 0
                &&
                Remaining > 0)
            {
                //
            }
            return buffer;
        }

        public IEnumerator<byte> GetEnumerator()
        {
            if (CurrentInt == -1) MoveNext();
            while (!m_Disposed && MoveNext()) yield return Current = (byte)m_Current;
        }

        public override bool MoveNext()
        {
            if (base.MoveNext()) return CoreGetEnumerator() != -1;
            return false;
        }

        int CoreGetEnumerator(long direction = 1)
        {
            if (!m_Disposed && m_Stream.Position < Count) return Current = (byte)(m_Current = m_Stream.ReadByte());
            else unchecked { return Current = (byte)(m_Current = -1); }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }

        internal protected long CoreIndexOf(IEnumerable<byte> items, int start = -1, int count = -1)
        {
            if (m_Stream == null || m_Disposed || items == null || items == Enumerable.Empty<byte>() || count == 0) return -1;
            if (count == -1) count = items.Count();

            if (!Initialized && !MoveNext()) return -1;

            if (start != -1 && start + count > Count) return -1;
            else if (start != -1 && start != Position) if (m_Stream.CanSeek) m_Stream.Seek(start, System.IO.SeekOrigin.Begin);
                else return -1;//throw new InvalidOperationException("The underlying stream must be able to seek if the start index is specified and not equal to -1");

            using (IEnumerator<byte> itemPointer = items.Skip(start).Take(count).GetEnumerator())
            {
                if (!itemPointer.MoveNext()) return -1;
                //We start at 
                long position = Position;
                if (start == -1 && m_Stream.CanSeek && m_Stream.Position != 0 && itemPointer.Current != Current)
                {
                    Reset();
                }
                else start = (int)m_Stream.Position;
                //While there is an itemPointer
                while (itemPointer != null)
                {
                    int j = count;
                    while (itemPointer.Current == Current && (--j > 0))
                    {
                        if (!itemPointer.MoveNext()) break;
                    }
                    //The match is complete
                    if (j == 0)
                    {
                        //If CanSeek and moved the position and we will go back to where we were
                        //if (m_Stream.CanSeek && position != Position) m_Stream.Seek(position, System.IO.SeekOrigin.Begin); //Curent and Begin need to be aware...
                        return m_Stream.Position - 1; //-1 Because a byte was read to obtain Current
                    }
                    if (!MoveNext()) break;
                }
                if (start == -1 && m_Stream.CanSeek && position != Position) m_Stream.Seek(position, System.IO.SeekOrigin.Begin);
                return -1;
            }
        }

        internal protected int CoreIndexOf(byte item, int start, int count) { return (int)CoreIndexOf(item.Yield(), start, count); }

        public virtual int IndexOf(byte item)
        {
            return CoreIndexOf(item, -1, 1);
        }

        public virtual void Insert(int index, byte item)
        {

            //System.IO.MemoryStream newMemory = new System.IO.MemoryStream(Count + 1);
            //System.IO.Stream oldMemory;
            //using (EnumerableByteStream preSegment = new EnumerableByteStream(m_Stream, 0, index - 1))
            //{
            //    using (EnumerableByteStream postSegment = new EnumerableByteStream(m_Stream, index - 1, Count - index + 1))
            //    {
            //        foreach (byte b in preSegment) newMemory.WriteByte(b);
            //        newMemory.WriteByte(item);
            //        foreach (byte b in postSegment) newMemory.WriteByte(b);
            //    }
            //}
            //oldMemory = m_Stream;
            //m_Stream = newMemory;
            //oldMemory.Dispose();

            //Linked stream around origional bytes up to index
            //additional byte
            //Rest of old stream
            //long preInsert = m_Stream.Position;
            EnumerableByteStream newPointer = new EnumerableByteStream((new EnumerableByteStream(m_Stream, 0, index - 1) as IEnumerable<byte>).
                Concat(item.Yield()).
                Concat(new EnumerableByteStream(m_Stream, index - 1, Count - index + 1) as IEnumerable<byte>));
            //m_Stream = LinkedStream.LinkAll(new EnumerableByteStream(m_Stream, 0, index - 1), new EnumerableByteStream(item), new EnumerableByteStream(m_Stream, index - 1, Count - index + 1));
            m_Self = newPointer;
            //m_Stream.Position = preInsert;
        }

        public virtual void RemoveAt(int index)
        {
            //Linked stream around index
            m_Stream = LinkedStream.LinkAll(new EnumerableByteStream(m_Stream, 0, index), new EnumerableByteStream(m_Stream, ++index, Count - index));
        }

        /// <summary>
        /// Sets or Retrieves a byte from the underlying stream
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public virtual byte this[int index]
        {
            get
            {
                if (index < 1) index = 0;
                if (index != m_Stream.Position)
                {
                    if (m_Stream.CanSeek) m_Stream.Seek(index, System.IO.SeekOrigin.Begin);
                    else throw new InvalidOperationException("You can only move the index if the underlying stream CanSeek");
                }
                return (byte)m_Stream.ReadByte();
            }
            set
            {
                if (m_Stream.CanWrite && m_Stream.CanSeek) m_Stream.Seek(index, System.IO.SeekOrigin.Begin);
                else throw new InvalidOperationException("You can logically set a byte in the stream the index if the underlying stream supports CanWrite and CanSeek");
                m_Stream.Write(value.Yield().ToArray(), 0, 1);
            }
        }

        public virtual void Add(byte item)
        {
            if (m_Stream.CanWrite) m_Stream.Write(item.Yield().ToArray(), 0, 1);
            else throw new InvalidOperationException("You can logically set a byte in the stream the index if the underlying stream supports CanWrite");
        }

        /// <summary>
        /// Erases all bytes in perspective of the EnumerableByteStream
        /// </summary>
        /// <remarks>
        /// Creates a new <see cref="System.IO.MemoryStream"/> in the place of the m_Stream.
        /// </remarks>
        public virtual void Clear()
        {
            m_Stream = new System.IO.MemoryStream();
            return;
        }

        public virtual bool Contains(byte item)
        {
            //See CachingEnumerableByteStream on why not < -1
            return CoreIndexOf(item, 0, 1) != -1;
        }

        /// <summary>
        /// Advanced the underlying System.IO.Stream by reading into the given array
        /// </summary>
        /// <param name="array">The array to read into</param>
        /// <param name="arrayIndex">The index into the given array to stary copying at</param>
        public virtual void CopyTo(byte[] array, int arrayIndex)
        {
            CoreCopyTo(array, arrayIndex);
        }

        public virtual void CopyTo(System.IO.Stream stream, int? bufferSize = null)
        {
            m_Stream.CopyTo(stream, (int)(stream.Length - stream.Position));
        }

        public virtual void CoreCopyTo(byte[] array, int arrayIndex, int length = -1)
        {
            if (length <= 0) return;
            if (length == -1) length = array.Length - arrayIndex;
            else if (length > m_Stream.Length) throw new ArgumentOutOfRangeException("Can't copy more bytes then are availble from the stream");
            m_Stream.Read(array, arrayIndex, length);
        }

        public long Position { get { return Index; } }

        public virtual long Length
        {
            get { return Count; }
        }

        public virtual bool IsReadOnly
        {
            get { return m_Stream.CanWrite; }
        }

        public virtual bool Remove(byte item)
        {
            //Create N new EnumerableByteStreams with the items index noted in each iteration.
            //For each occurance of item in the underlying stream place an index
            //For each index create a new EnumerableByteStream with the index = i and the count = 1
            //m_Stream = new LinkedStream(parts)
            //return true if any of this happened 
            return false;
        }

        public static implicit operator System.IO.Stream(EnumerableByteStream eByteStream) { return eByteStream.m_Stream; }

        object System.Collections.IEnumerator.Current
        {
            get { return GetEnumerator().Current; }
        }

        public override void Reset()
        {
            m_Current = -1;
            if (m_Stream.CanSeek) m_Stream.Seek(VirtualIndex, System.IO.SeekOrigin.Begin);
            base.Reset();
        }

        public long Seek(long offset, System.IO.SeekOrigin origin) { if (m_Stream.CanSeek) return m_Stream.Seek(offset, origin); return m_Stream.Position; }
    }

    #endregion

    #region CachingEnumerableByteStream

    //Todo Test and Complete, use LinkedStream if required
    //Aims to be a type of constrained stream as well give the ability to cache previous read bytes which may no longer be able to be read from the stream
    public class CachingEnumerableByteStream : EnumerableByteStream
    {
        //Same as above but with a Cache for previously read bytes
        List<byte> m_ReadCache = new List<byte>(), m_WriteCache = new List<byte>();

        public CachingEnumerableByteStream(System.IO.Stream stream)
            : base(stream)
        {
        }

        internal void EnsureCache(int index)
        {
            try
            {
                //Read Ahead
                if (index > m_ReadCache.Count)
                {
                    byte[] buffer = new byte[index - m_Stream.Position];
                    CopyTo(buffer, 0);
                    m_ReadCache.AddRange(buffer);
                }
            }
            catch { throw; }
        }

        public override byte this[int index]
        {
            get
            {
                EnsureCache(index);
                return m_ReadCache[index];
            }
            set
            {
                //Read Ahead
                EnsureCache(index);
                base[index] = m_ReadCache[index] = value;
            }
        }
    }

    #endregion

    #region LinkedStream

    /// <summary>
    /// Represtents multiple streams as a single stream.
    /// </summary>
    /// <remarks>
    /// Turning into a interesting little looping buffer when you just call AsPerpetual, would be a cool idea for a RingBuffer
    /// </remarks>
    public class LinkedStream :
        System.IO.Stream, //LinkedStream is a System.IO.Stream
        IEnumerable<EnumerableByteStream>, //Which happens to be IEnumerable of more Stream's
        IEnumerator<EnumerableByteStream>, //It can maintain the state of those Stream's which it is IEnumerable
        IEnumerable<byte>, //It can be thought of as a single contagious allocation of memory to callers
        IEnumerator<byte>//It can maintain a state of those bytes in which it enumerates
    {
        #region Fields

        internal protected IEnumerable<EnumerableByteStream> m_Streams;

        internal protected ulong m_AbsolutePosition;

        internal protected IEnumerator<EnumerableByteStream> m_Enumerator;

        internal EnumerableByteStream m_CurrentStream;

        internal int m_StreamIndex = 0;

        internal protected bool m_Disposed;

        #endregion

        #region Propeties

        public System.IO.Stream CurrentStream
        {
            get
            {
                if (m_Disposed) return null;
                if (m_CurrentStream == null) m_CurrentStream = m_Enumerator.Current;
                return m_CurrentStream;
            }
        }

        public override bool CanRead
        {
            get { return !m_Disposed && CurrentStream.CanRead; }
        }

        public override bool CanSeek
        {
            get { return !m_Disposed && CurrentStream.CanSeek; }
        }

        public override bool CanWrite
        {
            get { return !m_Disposed && CurrentStream.CanWrite; }
        }

        public override void Flush()
        {
            if (!m_Disposed) CurrentStream.Flush();
        }

        public override long Length
        {
            get { return (long)TotalLength; }
        }

        public ulong TotalLength
        {
            get
            {
                if (m_Disposed) return 0;
                ulong totalLength = 0;
                foreach (EnumerableByteStream stream in this as IEnumerable<EnumerableByteStream>) totalLength += (ulong)(stream.Count);
                return totalLength;
            }
        }

        public override long Position
        {
            get
            {
                return (long)TotalPosition;
            }
            set
            {
                if (!m_Disposed) SeekAbsolute(value, System.IO.SeekOrigin.Current);
            }
        }

        public ulong TotalPosition
        {
            ///The total position is the m_AbsolutePosition and the currentPosition which is free roaming
            get { return m_Disposed ? 0 : m_AbsolutePosition + (ulong)m_CurrentStream.Position; }
            protected internal set { if (m_Disposed) return; m_AbsolutePosition = value; }
        }

        #endregion

        #region Constructors

        public LinkedStream(IEnumerable<EnumerableByteStream> streams)
        {
            if (streams == null) streams = Enumerable.Empty<EnumerableByteStream>();
            m_Streams = streams;
            m_Enumerator = GetEnumerator();
            m_Enumerator.MoveNext();
        }

        public LinkedStream(EnumerableByteStream stream) : this(stream.Yield()) { }


        public static LinkedStream LinkAll(params EnumerableByteStream[] streams) { return new LinkedStream(streams); }

        #endregion

        #region Methods

        /// <summary>
        /// Creates a new MemoryStream with the contents of all sub streams
        /// </summary>
        /// <returns>The resulting MemoryStream</returns>
        public System.IO.MemoryStream Flatten()
        {
            System.IO.MemoryStream memory = new System.IO.MemoryStream((int)TotalLength);
            foreach (System.IO.Stream stream in m_Streams) stream.CopyTo(memory);
            return memory;
        }

        public LinkedStream Link(params EnumerableByteStream[] streams) { return new LinkedStream(m_Streams.Concat(streams)); }

        public LinkedStream Link(EnumerableByteStream stream) { return Link(stream.Yield().ToArray()); }

        public LinkedStream Unlink(int streamIndex, bool reverse = false)
        {
            if (streamIndex > m_Streams.Count()) throw new ArgumentOutOfRangeException("index cannot be greater than the amount of contained streams");
            IEnumerable<EnumerableByteStream> streams = m_Streams;
            if (reverse) streams.Reverse();
            return new LinkedStream(m_Streams.Skip(streamIndex));
        }

        public LinkedStream SubStream(ulong absoluteIndex, ulong count)
        {

            LinkedStream result = new LinkedStream(new EnumerableByteStream(new System.IO.MemoryStream()));

            if (count == 0) return result;

            while (absoluteIndex > 0)
            {
                //absoluteIndex -= Read(// StreamOverload
            }

            return result;
        }

        internal void SelectStream(int logicalIndex)
        {
            if (m_Disposed) return;
            //If the stream is already selected return
            if (m_StreamIndex == logicalIndex) return;
            else if (logicalIndex < m_StreamIndex) m_Enumerator.Reset(); //If the logicalIndex is bofore the stream index then reset
            while (logicalIndex > m_StreamIndex) MoveNext();//While the logicalIndex is > the stream index MoveNext (casues m_StreamIndex to be increased).
        }

        internal void SelectStream(ulong absolutePosition)
        {
            if (m_Disposed) return;
            if (TotalPosition == absolutePosition) return;
            else if (absolutePosition < TotalPosition) m_Enumerator.Reset(); //If seeking to a position before the TotalPosition reset
            //While the total postion is not in range
            while (TotalPosition < absolutePosition)
            {
                //Move to the next stream (causing TotalPosition to advance by the current streams length), if we cant then return
                if (!MoveNext()) return;
                //subtract the length of the stream we skipped from the absolutePosition
                if (CurrentStream.Length > 0) absolutePosition -= (ulong)CurrentStream.Length;
            }
        }

        #endregion

        #region Wrappers

        public override int ReadByte()
        {
            return CurrentStream.ReadByte();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return CoreRead(buffer, offset, count);
        }

        internal protected int CoreRead(byte[] buffer, long offset, long count, System.IO.SeekOrigin readOrigin = System.IO.SeekOrigin.Current)
        {
            switch (readOrigin)
            {
                case System.IO.SeekOrigin.Begin:
                    {
                        Seek(offset, System.IO.SeekOrigin.Begin);
                        goto case System.IO.SeekOrigin.Current;
                    }
                case System.IO.SeekOrigin.End:
                    {
                        //Seek to the end
                        MoveToEnd();

                        //Use the current case
                        goto case System.IO.SeekOrigin.Current;
                    }
                case System.IO.SeekOrigin.Current:
                    {
                        int read = 0;
                        while (read < count)
                        {
                            if (CurrentStream.Position < offset && CurrentStream.CanSeek) CurrentStream.Seek(offset, System.IO.SeekOrigin.Begin);
                            read += CurrentStream.Read(buffer, (int)offset, (int)count - (int)read);
                            if (CurrentStream.Position >= CurrentStream.Length && read < count) if (!MoveNext()) break;
                            //if (read != -1) m_AbsolutePosition += (uint)read;
                        }
                        return read;
                    }
                default:
                    {
                        Media.Common.Extensions.Debug.DebugExtensions.BreakIfAttached();
                        break;
                    }
            }
            return 0;
        }

        bool MoveNext()
        {
            if (m_Disposed || m_Streams == null || m_Enumerator == null) return false;
            //If the current stream can seek it's not a big deal to reset it now also
            //if (m_CurrentStream.CanSeek) m_CurrentStream.Seek(0, System.IO.SeekOrigin.Begin);
            m_AbsolutePosition += (ulong)(m_CurrentStream.Count - m_CurrentStream.Position); //Cumulate the position of the non seeking stream - what was already read
            //Advance the stream index
            ++m_StreamIndex;
            //Return the result of moving the enumerator to the next available streeam
            return m_Enumerator.MoveNext();
        }

        public override long Seek(long offset, System.IO.SeekOrigin origin)
        {
            switch (origin)
            {
                case System.IO.SeekOrigin.End:
                case System.IO.SeekOrigin.Begin:
                    return (long)SeekAbsolute(offset, System.IO.SeekOrigin.Begin);
                case System.IO.SeekOrigin.Current:
                    return m_CurrentStream.Seek(offset, origin);
                default: return m_CurrentStream.Position;
            }
        }

        public void MoveToEnd()
        {
            //While thre is a stream to advance
            while (MoveNext())
            {
                //Move the current stream to it's last position
                CurrentStream.Position = CurrentStream.Length;
            }
        }

        public ulong SeekAbsolute(long offset, System.IO.SeekOrigin origin)
        {
            switch (origin)
            {
                case System.IO.SeekOrigin.Current:
                    {

                        //Determine how many bytes to read in terms of long
                        long offsetPosition = (long)TotalPosition - offset;
                        if (offsetPosition < 0) CurrentStream.Seek(offsetPosition, System.IO.SeekOrigin.Current);
                        else CurrentStream.Position = offsetPosition;
                        return TotalPosition;
                    }
                case System.IO.SeekOrigin.Begin:
                    {
                        SelectStream((ulong)offset);
                        goto case System.IO.SeekOrigin.Current;
                    }
                case System.IO.SeekOrigin.End:
                    {
                        MoveToEnd();
                        goto case System.IO.SeekOrigin.Current;
                    }
                default:
                    {
                        Media.Common.Extensions.Debug.DebugExtensions.BreakIfAttached();
                        break;
                    }
            }
            return m_AbsolutePosition;
        }



        IEnumerator<EnumerableByteStream> GetEnumerator() { return m_Disposed ? null : m_Streams.GetEnumerator(); }

        public override void SetLength(long value)
        {
            //If the position is < value set the position to the value first which will update CurrentStream if required.
            if (value < Length) Position = (long)m_AbsolutePosition + value;
            //Set the length of the current stream 
            CurrentStream.SetLength(value);
            m_AbsolutePosition = (ulong)value;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (count < CoreWrite(buffer, (ulong)offset, (ulong)count))
            {
                Media.Common.Extensions.Debug.DebugExtensions.BreakIfAttached();
            }
        }

        ///Todo => Test and complete
        internal protected int CoreWrite(byte[] buffer, ulong offset, ulong count, System.IO.SeekOrigin readOrigin = System.IO.SeekOrigin.Current)
        {
            //Select the stream to write based on the offset
            SelectStream((ulong)offset);
            switch (readOrigin)
            {
                case System.IO.SeekOrigin.Current:
                    {
                        return (this as IEnumerator<System.IO.Stream>).Current.Read(buffer, (int)offset, (int)count);
                    }
                case System.IO.SeekOrigin.Begin:
                    {
                        return (this as IEnumerator<System.IO.Stream>).Current.Read(buffer, (int)offset, (int)count);
                    }
                case System.IO.SeekOrigin.End:
                    {
                        return (this as IEnumerator<System.IO.Stream>).Current.Read(buffer, (int)offset, (int)count);
                    }
                default:
                    {
                        Media.Common.Extensions.Debug.DebugExtensions.BreakIfAttached();

                        break;
                    }
            }

            return 0;
        }

        IEnumerator<EnumerableByteStream> IEnumerable<EnumerableByteStream>.GetEnumerator()
        {
            return m_Streams.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return m_Streams.GetEnumerator();
        }

        EnumerableByteStream IEnumerator<EnumerableByteStream>.Current
        {
            get { return GetEnumerator().Current; }
        }

        void IDisposable.Dispose()
        {
            m_Disposed = true;
            if (m_Streams != null) m_Streams = null;
            m_StreamIndex = -1;
            base.Dispose();
        }

        void System.Collections.IEnumerator.Reset()
        {
            foreach (EnumerableByteStream stream in m_Streams) if (stream.m_Stream.CanSeek) stream.Seek(0, System.IO.SeekOrigin.Begin);
            GetEnumerator().Reset();
        }

        object System.Collections.IEnumerator.Current
        {
            get { return CurrentStream; }
        }

        bool System.Collections.IEnumerator.MoveNext()
        {
            return MoveNext();
        }

        IEnumerator<byte> IEnumerable<byte>.GetEnumerator() { return new EnumerableByteStream(this as IEnumerable<byte>).GetEnumerator(); }

        byte IEnumerator<byte>.Current { get { return (this as IEnumerable<byte>).GetEnumerator().Current; } }

        #endregion
    }

    #endregion

    #endregion

    #region Extensions

    //Break down into various other classes e.g. EnumerableExtensions etc..
    public static class Extensions
    {
        //public static IEnumerator<T> AsPerpetual<T>(this IEnumerable<T> enumerable) { return new PerpetuatingEnumerable<T>(enumerable); }

        //public static System.Collections.IEnumerator AsPerpetual<T>(this System.Collections.IEnumerable enumerable) { return new PerpetuatingEnumerable<T>(enumerable); }

        public static LinkedStream Linked(this EnumerableByteStream stream) { return new LinkedStream(stream); }

        public static LinkedStream Link(this EnumerableByteStream stream, IEnumerable<EnumerableByteStream> streams) { return new LinkedStream(stream.Yield().Concat(streams)); } //stream.Yield().Concat(streams);
    }

    #endregion

    #region Fast Random

    /// <summary>
    /// The theory behind this class is that Guid generation is faster then Random Generation...
    /// The other theory is that when looking for a number in - between two numbers that n >= max || n <= min
    /// In such cases you can take max from N or add min to N to get closer.
    /// Just need to make it faster
    /// </summary>
    internal sealed class FastRandom
    {
        public static Guid Generator { get { return Guid.NewGuid(); } }

        public static byte[] NextBytes() { return Generator.ToByteArray(); }

        public static int Next(int min = int.MinValue, int max = int.MaxValue, int seed = -1)
        {
            byte[] carindals = NextBytes();

            int result = seed | max - min;

            foreach (byte b in carindals)
            {
                result |= Math.Min(max, b);
                if (result < min) result += max;
                if (result > max) result -= min;
                if (result > min && result < max) break;
            }

            //If the result is ready return it
            if ((result > min && result < max)) return result;
            else
            {
                //The result must be in result.
                foreach (byte b in BitConverter.GetBytes(result))
                {
                    result &= Math.Max(b, min);
                    if (result < min) result += max;
                    if (result > max) result -= min;
                    if (result > min && result < max) break;
                }
            }

            //Return the result
            return result;
        }

    }

    #endregion

    //Blitable
    
    //Number

    //Physics

    //Units

    #endregion   
}
