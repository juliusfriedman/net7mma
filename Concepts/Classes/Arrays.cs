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

namespace Media.Concepts.Classes
{
    #region ArrayElement

    /// <summary>
    /// Provides an array like structure for a single element.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class ArrayElement<T> : System.Collections.Generic.IList<T>
    {
        //bool m_IsReadOnly

        internal T m_Source;

        internal int m_Index = 0;

        #region Properties

        public T Source
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_Source; }
        }

        public int Index
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_Index; }
        }

        public int Length
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return 1; }
        }

        #endregion

        //[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        //public ArrayElement(string source)
        //{
        //    //Doesn't translate correctly. Causes crashed when enumerated a few times
        //    Source = Unsafe.ReinterpretCast<string, T>(source);

        //    //Doesn't translate correctly. when it does crash it crashes hard. probably also a security exploit but thats unsafe for you.
        //    //Source = Unsafe.ReinterpretCast<System.Text.StringBuilder, T>(new System.Text.StringBuilder(source));
        //}

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public ArrayElement(T[] source, int index = 0)
        {
            m_Source = source[m_Index = index];
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public ArrayElement(T source)
        {
            m_Source = source;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        int System.Collections.Generic.IList<T>.IndexOf(T item)
        {
            return Source.Equals(item) ? 0 : -1;
        }

        void System.Collections.Generic.IList<T>.Insert(int index, T item)
        {
            throw new System.NotImplementedException();
        }

        void System.Collections.Generic.IList<T>.RemoveAt(int index)
        {
            throw new System.NotImplementedException();
        }

        T System.Collections.Generic.IList<T>.this[int index]
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get
            {
                return Source;
            }

            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            set
            {
                if (index == m_Index) m_Source = value;
            }
        }

        void System.Collections.Generic.ICollection<T>.Add(T item)
        {
            throw new System.NotImplementedException();
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        void System.Collections.Generic.ICollection<T>.Clear()
        {
            m_Source = default(T);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        bool System.Collections.Generic.ICollection<T>.Contains(T item)
        {
            return Source.Equals(item);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        void System.Collections.Generic.ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            array[arrayIndex] = Source;
        }

        int System.Collections.Generic.ICollection<T>.Count
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return 1; }
        }

        bool System.Collections.Generic.ICollection<T>.IsReadOnly
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return false; } //m_IsReadOnly
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        bool System.Collections.Generic.ICollection<T>.Remove(T item)
        {
            throw new System.NotImplementedException();
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        System.Collections.Generic.IEnumerator<T> System.Collections.Generic.IEnumerable<T>.GetEnumerator()
        {
            return Common.Extensions.Linq.LinqExtensions.Yield<T>(Source).GetEnumerator();
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return Common.Extensions.Linq.LinqExtensions.Yield<T>(Source).GetEnumerator();
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static implicit operator T[](ArrayElement<T> t)
        {
            return Common.Extensions.Object.ObjectExtensions.ToArray<T>(t.Source);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static implicit operator T(ArrayElement<T> t)
        {
            return t.Source;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static implicit operator ArrayElement<T>(T t)
        {
            return new ArrayElement<T>(t);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public void Set(T t)
        {
            m_Source = t;
        }

        public override int GetHashCode()
        {
            return Source.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return System.Object.Equals(Source, obj);
        }


        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(ArrayElement<T> a, ArrayElement<T> b)
        {
            object boxA = a, boxB = b;
            return boxA == null ? boxB == null : a.Source.Equals(b.Source);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(ArrayElement<T> a, ArrayElement<T> b) { return false == (a == b); }

        //Could actually use Source = Array<T> from here but it feels weird since this came first.
    }

    #endregion

    //ArrayLike was renamed to ArrayElement.

    //Could probably use ArrayLike to make chars of String accessible
    //e.g. ArrayLike<string> which also implements the char overloads...

    //e.g. ArrayLike<int> from ArrayLike<byte>

    //e.g. ArrayLike<short> from ArrayLike<int>

    //etc

    //See also
    //https://gist.github.com/OmerMor/1050703

    //Shows how an array header can be 'forged'
    //using something like that i can imagine it would be possible to use the forged header to make arrays seem offset 
    //1) copy the bytes used in the element at the offset which corresponds to the size of the array header.
    //2) make an array header with the desire type (and offset length)
    //3) put that header where the coppied bytes exited.
    //4) cast the pointer from ArrayHeader to T[] which will then point to the header at the first element would be @ the offset, the count would be the offset count.
    //5) put the data back from the indexes which didn't change (where the forged header was)    

    //The following attempts to do all of the leg work for the above, the only thing which is not yet implemented is the array header forging.

    #region Array

    /// <summary>
    /// Provides a generic ArraySegment / Slice class.
    /// Has static methods for reading and writing Array types without a bounds check.
    /// Has static methods for updating arrays from the slice.
    /// Has methods to update the source from a different array.    
    /// </summary>
    /// <notes>
    /// Creates a new array when implicitly assigned from one.. this means this class could track created array instances for whatever purpose...
    /// </notes>
    /// <typeparam name="T"></typeparam>
    internal class Array<T> : System.Collections.Generic.IList<T>
    {        
        #region Fields

        /*readonly */ T[] m_Source;

        int m_Offset;

        int m_Length;

        internal readonly System.Collections.Generic.HashSet<T[]> Allocations = new System.Collections.Generic.HashSet<T[]>();

        //System.Text.StringBuilder m_String;

        #endregion        

        #region Constructor

        /// <summary>
        /// This is not very useful or stable yet.
        /// </summary>
        /// <param name="s"></param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public Array(string s)
        {
            //The reason why this works is that only what is incorrectly passed as the array of string is assigned into ArrayElement and then copied out of memory as T implicitly.            (The values are garbadge)            
            m_Source = new ArrayElement<T>(Unsafe.ReinterpretCast<string, T[]>(s));

            m_Length = 1;

            #region Close to working.....
            //needs logic in UnsafeRead but it works...
            ////m_String = new System.Text.StringBuilder(s);

            ////m_Source = Unsafe.ReinterpretCast<System.Text.StringBuilder, T[]>(m_String);

            ////m_Offset = 288;

            //////System.Runtime.InteropServices.Marshal.PtrToStringAuto((System.IntPtr)(&m_String) + 288, s.Length)

            //////System.Runtime.InteropServices.Marshal.PtrToStringAuto((System.IntPtr)(&m_Source) + 304, s.Length)

            ////m_Length = m_String.Length;

            //As a last resort the seemingly only other option would be to walk use stack walking which is more reliable but slower...
            //That slowerness would defeat the purpose of doing any of this.

            //https://msdn.microsoft.com/en-us/library/e8w969hb(v=vs.110).aspx

            //you can do this with marshal, so there has to be a way to do it in managed code...

            //https://limbioliong.wordpress.com/2011/11/01/using-the-stringbuilder-in-unmanaged-api-calls/

            #endregion

            #region Failed

            //Always given as array with one element which is probably not correct, the only way to fix this is to handle the string conversion in arrayLike..
            //The other way to be to specialize in ArrayLike for string but then it may not perform as one would think by itself. ArrayElement could be internal only...

            //ArrayElement<string> ae = new ArrayElement<string>(s);

            //m_Source = Unsafe.ReinterpretCast<string, T[]>(ae.m_Source);

            //System.Text.StringBuilder sb = new System.Text.StringBuilder(s);

            //m_Source = Unsafe.ReinterpretCast<System.IntPtr, T[]>(Unsafe.ReinterpretCast<System.Text.StringBuilder, System.IntPtr>(sb)); //Unsafe.ReinterpretCast<System.Text.StringBuilder, T[]>(sb);

            //System.Text.StringBuilder r = Unsafe.ReinterpretCast<System.IntPtr, System.Text.StringBuilder>(Unsafe.ReinterpretCast<System.Text.StringBuilder, System.IntPtr>(sb));

            //System.Console.WriteLine(r);

            ////Just use offset...
            ////m_Source = Unsafe.ReinterpretCast<System.IntPtr, T[]>(Unsafe.AddressOf(sb) + 25);            

            #endregion

            #region Failed.

            //m_Source = Unsafe.ReinterpretCast<string, T[]>(s); //Unsafe.ReinterpretCast<ArrayLike<System.Text.StringBuilder>, T[]>(new ArrayLike<System.Text.StringBuilder>(new System.Text.StringBuilder(s)));

            //Right now the CLR thinks instances of this class is an array of strings when T = string ...
            //m_Source = Unsafe.ReinterpretCast<string, T[]>(s);  //Unsafe.ReinterpretCast<char[], T[]>(s.ToCharArray());

            //This is probably not necessary to cast twice.
            //m_Source = Unsafe.ReinterpretCast<char[], T[]>(Unsafe.ReinterpretCast<string, char[]>(s));

            //This may be better..
            //m_Source = Unsafe.ReinterpretCast<char[], T[]>(Unsafe.ReinterpretCast<System.Text.StringBuilder, char[]>(new System.Text.StringBuilder(s)));            

            //m_Source = Unsafe.ReinterpretCast<System.IntPtr, T[]>(Unsafe.AddressOf(new System.Text.StringBuilder(s)) + 25); //Unsafe.ReinterpretCast<System.Text.StringBuilder, T[]>(new System.Text.StringBuilder(s)); //

            //m_Source = Unsafe.ReinterpretCast<System.Text.StringBuilder, T[]>(sb);

            //Could probably keep source null and store the address in offset although it would be cleaner to use ArrayLike specialization.

            //Seems to be about 24 - 26 bytes away most times in x64

            //System.Runtime.InteropServices.Marshal.PtrToStringAuto((System.IntPtr)(int*)((&s + 25) + 0), 2 * s.Length);
            //Unsafe.Read<char>((System.IntPtr)((char*)(&sb + 25)+5))

            //m_Offset = 25; // + offset

            //In chars.
            //m_Length = (int)(Unsafe.BytesPer<T>() <= 2 ? s.Length : Unsafe.BytesPer<T>() / s.Length);//s.Length; // length - offset;

            #endregion
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public Array(T t)
        {
            m_Source = Common.Extensions.Object.ObjectExtensions.ToArray<T>(t);

            m_Offset = 0;

            m_Length = 1;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public Array(T[] source, int offset, int count)
        {
            m_Source = source;

            m_Offset = offset;

            m_Length = count;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public Array(T[] source, int offset) : this(source, offset, source.Length - offset) { }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public Array(T[] source) : this(source, 0, source.Length) { }

        #endregion

        #region Properties

        /// <summary>
        /// A <see cref="System.Array"/> cast of the <see cref="Source"/>
        /// </summary>
        public System.Array OriginalArray //Array name is the same as enclosing type..
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_Source; }
            //get { return Source; }
        }

        /// <summary>
        /// The raw array of elements
        /// </summary>
        public T[] Source
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_Source; }
        }

        /// <summary>
        /// The offset in <see cref="Source"/> where this array begins
        /// </summary>
        public int Offset
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_Offset; }
        }

        /// <summary>
        /// How many elements this array holds
        /// </summary>
        public int Length
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_Length; }
        }

        #endregion

        #region Contains

        //Should be index of.

        public bool Contains(T[] array) { return Contains(array, 0, array.Length); }

        public bool Contains(Array<T> array) { return Contains(array.m_Source, array.m_Offset, array.m_Length); }

        public bool Contains(T[] array, int offset, int length)
        {
            //Try the fast way...
            if (array == m_Source &&
                //Check for the offset to be present in the array
                0 >= m_Offset - offset
                &&
                //Check for the length to be present
                offset <= m_Length && length <= m_Length) return true;

            //Check for equality manualy.

            //Could do the memcmp type thing here for a performance increase.

            int matched = 0;

            for (int i = offset; i < length; ++i)
            {
                if (UnsafeRead(array, i).Equals(this[i])) ++matched;
                else matched = 0;
            }

            //Should keep return or output i so this can call IndexOf()>= 0;

            return matched == length;
        }

        #endregion

        #region Indexer

        public T /*System.Collections.Generic.IList<T>.*/this[int index]
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get
            {
                //return m_Source[m_Offset + index];

                return UnsafeRead(this, ref index);
            }

            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            set
            {
                //m_Source[m_Offset + index] = value;

                UnsafeWrite(this, index, ref value);
            }
        }

        #endregion

        #region IList

        //http://www.codeproject.com/Articles/3467/Arrays-UNDOCUMENTED

        int System.Collections.Generic.IList<T>.IndexOf(T item)
        {
            return System.Array.IndexOf<T>(m_Source, item, m_Offset, m_Length);
        }

        void System.Collections.Generic.IList<T>.Insert(int index, T item)
        {
            throw new System.NotImplementedException();
            //InsertHelper
        }

        void System.Collections.Generic.IList<T>.RemoveAt(int index)
        {
            throw new System.NotImplementedException();
            //RemoveHelper
        }

        void System.Collections.Generic.ICollection<T>.Add(T item)
        {
            throw new System.NotImplementedException();
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        void System.Collections.Generic.ICollection<T>.Clear()
        {
            System.Array.Clear(m_Source, m_Offset, m_Length);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        bool System.Collections.Generic.ICollection<T>.Contains(T item)
        {
            return System.Array.IndexOf<T>(m_Source, item, m_Offset, m_Source.Length - m_Length) >= 0;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        void System.Collections.Generic.ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            System.Array.Copy(m_Source, m_Offset, array, arrayIndex, m_Length);
        }

        int System.Collections.Generic.ICollection<T>.Count
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_Length; }
        }

        bool System.Collections.Generic.ICollection<T>.IsReadOnly
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return false; } //m_IsReadOnly
        }

        bool System.Collections.Generic.ICollection<T>.Remove(T item)
        {
            throw new System.NotImplementedException();
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        System.Collections.Generic.IEnumerator<T> System.Collections.Generic.IEnumerable<T>.GetEnumerator()
        {
            for (int i = 0; i < m_Length; ++i) yield return m_Source[m_Offset + i];
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return System.Linq.Enumerable.Skip(m_Source, m_Offset).GetEnumerator();
        }

        #endregion

        #region Operators

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public unsafe static implicit operator T[](Array<T> array)
        {
            //Doesn't really work well
            //return Unsafe.Create<T>((void*)Unsafe.AddressOf(array.m_Source), array.m_Length);

            //Make a new array
            T[] result = new T[array.m_Length];

            System.Array.Copy(array.m_Source, array.m_Offset, result, 0, array.m_Length);

            //Won't work with Strings... (RankException) (Could try to copy the header / forge the header. see ArrayTest method)
            array.Allocations.Add(result);

            return result;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static implicit operator Array<T>(T[] array)
        {
            return new Array<T>(array);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Array<T> a, Array<T> b)
        {
            object boxA = a, boxB = b;
            return boxA == null ? boxB == null : a.Equals(b);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Array<T> a, Array<T> b) { return false == (a == b); }

        #endregion

        #region Statics

        /// <summary>
        /// Skips a bounds check while reading the index from source.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="index"></param>
        /// <returns>The element</returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static T UnsafeRead(T[] source, int index)
        {
            return Unsafe.Read<T>(System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement<T>(source, index));
        }

        /// <summary>
        /// Skips a bounds check while writing the index from source.
        /// </summary>
        /// <param name="source">The array to write</param>
        /// <param name="index">The index into the array</param>
        /// <param name="value">The value to write</param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static void UnsafeWrite(T[] source, int index, ref T value)
        {
            Unsafe.Write<T>(System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement<T>(source, index), ref value);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public unsafe static T UnsafeRead(Array<T> array, ref int index)
        {
            #region Strings as Char[]

            //When not using ArrayElement for now..

            //Won't work.
            //return Unsafe.Read<T>(System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement<char>(Unsafe.ReinterpretCast<T[], char[]>(array.Source), array.m_Offset + index));

            //Works but is ugly, should use ArrayLike to hide this.
            //if (typeof(T) == typeof(string)) unsafe
            //    {
            //        string temp = Unsafe.ReinterpretCast<T[], string>(array.Source);
            //        fixed (char* stringPtr = temp)
            //        {
            //            return Unsafe.Read<T>((System.IntPtr)stringPtr);
            //        }
            //    }

            //kinds of works...
            //if (typeof(T) == typeof(string)) unsafe
            //    {
            //        return Unsafe.ReinterpretCast<char, T>(array.m_String[index]);
            //        //return Unsafe.Read<T>((System.IntPtr)Unsafe.ReinterpretCast<T[], System.IntPtr>(array.Source));
            //    }

            //Could check if array.m_Source is null to use the offset and index alone...

            #endregion

            return UnsafeRead(array.m_Source, array.m_Offset + index);

            //return Unsafe.Read<T>(System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement<T>(array.m_Source, array.m_Offset + index));
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public unsafe static void UnsafeWrite(Array<T> array, int index, ref T value)
        {
            UnsafeWrite(array.m_Source, array.m_Offset + index, ref value);
            //Unsafe.Write<T>(System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement<T>(array.m_Source, array.m_Offset + index), ref value);
        }

        //Here an empty array could be created and the RTTI modified such that the pointer to the first element points to the offset desired,
        //Unfortunately there is no such member on the array.
        static T[] ArrayTest(Array<T> array)
        {

            T[] refArray = array.m_Source;

            T[] tArray = refArray;

            //To see the interesting bits use a new array. (beware if you don't the original array will also be modified)
            ////Take an empty array (already allocated members) to 0.
            //T[] tArray = System.Array.Empty<T>();

            //See notes in Unsafe.Create

            //int overHead = 16;

            ////Check for value type or array type.
            //if (false == typeof(T).IsValueType)
            //{
            //    overHead -= 4;
            //}

            //if (array.Source.Length > 81920)
            //{
            //    overHead += 8;
            //}

            ////Copy the ElementType
            ////*(int*)(System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(tArray, 0) - 16) = *(int*)(System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(array.Source, 0) - 16);

            ////Copy the number of dimensions
            //*(int*)(System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(tArray, 0) - 4) = *(int*)System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(array.Source, 0) - 4;

            ////Set Count
            //*(int*)(System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(tArray, 0) - 8) = array.m_Length;

            #region Doesn't work but interesting.

            //Comparing from the &array.Source and System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(array.Source, 0) there is always X(104) bytes of overhead... after a collect this may change
            //*(int*)(System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(tArray, 0) - 16) = (int)(int*)(System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(array.Source, array.m_Offset) - array.m_Offset + 104);

            //Setting the value to anything doesn't change the first element except on accident.
            //*(int*)(System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(tArray, 0) - 16) = *(int*)(System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(array.Source, 0));

            //Write the pointer of the address of the array.
            //System.Runtime.InteropServices.Marshal.WriteIntPtr(System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(tArray, 0) - 16, System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(array.Source, array.m_Offset));

            //System.Runtime.InteropServices.Marshal.WriteIntPtr(System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(tArray, 0) - overHead, System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(array.Source, 0) - overHead);

            //*(int*)(System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(tArray, 0) - 16) = *(int*)(System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(array.Source, array.m_Offset) - 16);

            //*(int*)(System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(tArray, 0) - 16) = (int*)(System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(array.Source, array.m_Offset));

            #region Interesting

            //Arrays already have their members allocated by their Length, ValueTypes have their value in the array based on their size and ReferenceTypes have their IntPtr references to the Object.

            //The only curious example is this which invalidates element 0 and causes an alignment issues.
            //Could be called protect
            //System.Runtime.InteropServices.Marshal.WriteIntPtr(System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(tArray, 0) - 8, System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(array.Source, 0));

            //This does suffer from the alignment issue but still doesn't work.
            //Could be called Unprotect and could pass orig pointer in 0 and reprotect and then pass to the unprotecter which would copy the original header after unprotecting...
            //System.Runtime.InteropServices.Marshal.WriteIntPtr(System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(tArray, 0) - 8, System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(array.Source, 0) - 16);

            //I imagine rather than trying to trick the CLR into doing this that the CLR should just eventually support the concept...
            //After all Array[*] is already a behind the scene array type and arrays with non 0 lower bounds are already suppored.

            #endregion

            #endregion

            return tArray;
        }

        #endregion

        #region Overrides

        public override int GetHashCode()
        {
            return (int)(m_Source.GetHashCode() ^ m_Offset ^ m_Length);
        }

        public override bool Equals(object obj)
        {
            if (false == (obj is Array<T>)) return false;

            Array<T> array = obj as Array<T>;

            return GetHashCode() == array.GetHashCode();
        }

        #endregion

        #region Methods

        //Could check versions...

        //Could check if the array was actually created from this instance...

        //Would have to keep track of all sub arrays generated with the implicit operator...

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public void UpdateFrom(T[] source, int offset, int count) { System.Array.Copy(source, offset, m_Source, m_Offset, count); }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public void UpdateFrom(T[] source) { UpdateFrom(source, 0, source.Length); }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public void UpdateFrom(Array<T> array) { UpdateFrom(array.m_Source, array.m_Offset, array.m_Length); }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static void Update(/*this */T[] source, Array<T> from) { from.UpdateFrom(source); }

        #endregion
    }

    #endregion

    //ManagedBufferPool
    //https://github.com/dotnet/corefx/issues/4547
}


namespace Media.UnitTests
{
    internal class ExperimentalTests
    {
        public void TestArrayLike()
        {
            Concepts.Classes.ArrayElement<int> intTest = new Concepts.Classes.ArrayElement<int>(0);

            //Implicit conversion to array
            int[] array = intTest;

            //Implicit conversion to T
            int test = intTest;
            
            //Set value implicitly?
            intTest = 1;

            if (intTest != 1) throw new System.Exception();

            intTest.Set(0);

            if (intTest != 0) throw new System.Exception();

            System.Console.WriteLine(array[0]);

            System.Collections.Generic.IList<int> iList = intTest as System.Collections.Generic.IList<int>;

            System.Console.WriteLine(iList[0]);

            iList[0] = 2;

            //array[0] value remains unless it's a reference?

            if (intTest != 2) throw new System.Exception();

            System.Console.WriteLine(iList[0]);

            //Not really worth using here would be much more useful when combined with Array<string> specialization.

            string testString = "test";

            Concepts.Classes.ArrayElement<string> stringTest = new Concepts.Classes.ArrayElement<string>(testString);

            //Weird but doesn't crash
            //System.Console.WriteLine(stringTest);

            System.Collections.Generic.IList<string> iListS = stringTest as System.Collections.Generic.IList<string>;

            if (false == System.Object.ReferenceEquals(iListS[0], testString)) throw new System.Exception("Not ReferenceEqual");

            System.Console.WriteLine(iListS[0]);

            System.Console.WriteLine(iListS.Count);

            System.Collections.Generic.IList<char> iListC = stringTest as System.Collections.Generic.IList<char>;

            if (iListC != null)
            {
                System.Console.WriteLine(iListC[0]);
                System.Console.WriteLine(iListC.Count);
            }
            else System.Console.WriteLine("null char IList no exception?");
        }

        public void TestArrayT()
        {
            //Make a CLR Array
            int[] clrArray = new int[4] { 4, 3, 2, 1 };

            int offset = 2, count = 2;

            //Make the generic array.
            Concepts.Classes.Array<int> genericArray = new Concepts.Classes.Array<int>(clrArray, offset, count);

            //Cast the generic array as Ilist
            System.Collections.Generic.IList<int> iList = genericArray as System.Collections.Generic.IList<int>;

            //Show the members at index 3 of the CLR Array.
            System.Console.WriteLine(clrArray[2]);

            //Show the members at index 0 of the iList
            System.Console.WriteLine(iList[0]);

            //Write to the CLR Array at index 2
            clrArray[2] = 0;

            //Show the member has changed
            System.Console.WriteLine(iList[0]);

            //Write to the iList at index 0 which corresponds to clrArray @ [3]
            iList[0] = 4;

            if (clrArray[0] != iList[0]) throw new System.Exception("Failed to set original memory");

            //Show the member changed in the iList
            System.Console.WriteLine(iList[0]);

            //Show the member changed in the clrArray @ 2
            System.Console.WriteLine(clrArray[2]);

            //Implicit cast from generic array to typed array with no boxing.
            int[] asArray = genericArray;

            //Test Length and LongLength
            System.Console.WriteLine("asArray.Length: " + asArray.Length);

            System.Console.WriteLine("asArray.LongLength: " + asArray.LongLength);

            System.Console.WriteLine("asArray.GetUpperBound: " + asArray.GetUpperBound(0));

            System.Console.WriteLine("asArray.GetLowerBound: " + asArray.GetLowerBound(0));

            //Enumerate the values in fromArray
            foreach (var intValue in asArray)
            {
                System.Console.WriteLine("asArray.Current: " + intValue);
            }

            System.Console.WriteLine(asArray[0]);

            System.Console.WriteLine(asArray[1]);

            //Iterate the values in clrArray
            for (int i = 0; i < asArray.Length; ++i)
            {
                System.Console.WriteLine("asArray[" + i + "]" + " = " + asArray[i]);

                //Type in TypedReference cannot be null.
                System.Console.WriteLine("asArray.GetValue(" + i + ")" + " = " + asArray.GetValue(i));

                if (asArray[i] != iList[i]) throw new System.Exception("asArray Did not point to correct element");

                if (iList[i] != clrArray[i + offset]) throw new System.Exception("iList Did not point to correct element");
            }

            //Should be Out of bounds..
            try
            {
                System.Console.WriteLine(asArray[2]);

                System.Console.WriteLine(iList[2]);
            }
            catch
            {
                System.Console.WriteLine(asArray[0]);

                System.Console.WriteLine(iList[0]);
            }

            //Attempt to Write to fromArray at index 1 which corresponds to clrArray @ 2 + 1;
            asArray[0] = 5;

            asArray[1] = 5;

            //Show the value changed in fromArray
            System.Console.WriteLine(asArray[0]);

            if (asArray[0] == iList[0]) throw new System.Exception("Set original memory");

            //Show the value changed
            System.Console.WriteLine(iList[1]);

            if (asArray[0] == clrArray[2]) throw new System.Exception("Set original memory");

            //Show the value did not changed
            System.Console.WriteLine(clrArray[2]);

            //Test Length and LongLength
            System.Console.WriteLine("asArray.Length" + asArray.Length);

            System.Console.WriteLine("asArray.LongLength" + asArray.LongLength);

            //Enumerate the values in fromArray
            foreach (var intValue in asArray)
            {
                System.Console.WriteLine("asArray.Current: " + intValue);
            }

            //Iterate the values in clrArray
            for (int i = 0; i < asArray.Length; ++i)
            {
                System.Console.WriteLine("asArray[" + i + "]" + " = " + asArray[i]);

                System.Console.WriteLine("asArray.GetValue(" + i + ")" + " = " + asArray.GetValue(i));
            }

            //Show that the references are not the same
            System.Console.WriteLine(object.ReferenceEquals(asArray, clrArray));

            //Write to the array at member 3
            clrArray[3] = 0;

            if (clrArray[3] != iList[1]) throw new System.Exception("Failed to set iList memory");

            //Show the changes in iList @ 1
            System.Console.WriteLine(iList[1]);

            if (clrArray[3] == asArray[1]) throw new System.Exception("Set original memory when writing to fromArray memory");

            //Show the changes in fromArray @ 1
            System.Console.WriteLine(asArray[1]);

            //Write to clrArray at member 2
            clrArray[2] = 0;

            if (clrArray[2] != iList[0]) throw new System.Exception("Failed to set iList memory");

            //Show the changes in iList @ 0
            System.Console.WriteLine(iList[0]);

            if (clrArray[2] == asArray[0]) throw new System.Exception("Set slice memory");

            //Test Length and LongLength
            System.Console.WriteLine("clrArray.Length" + clrArray.Length);

            System.Console.WriteLine("clrArray.LongLength" + clrArray.LongLength);

            //Enumerate the values in clrArray
            foreach (var intValue in clrArray)
            {
                System.Console.WriteLine("Current: " + intValue);
            }

            //Iterate the values in clrArray
            for (int i = 0; i < clrArray.Length; ++i)
            {
                System.Console.WriteLine("clrArray[" + i + "]" + " = " + clrArray[i]);

                System.Console.WriteLine("clrArray.GetValue(" + i + ")" + " = " + clrArray.GetValue(i));
            }

            //Show it has it's own values seperate from the clrArray
            System.Console.WriteLine(string.Join(",", System.Array.ConvertAll<int, string>(asArray, System.Convert.ToString)));

            //Which does show the modifications to the original array.
            System.Console.WriteLine(string.Join(",", System.Array.ConvertAll<int, string>(clrArray, System.Convert.ToString)));

            //Show the iList writes have persisted and the writes to asArray are seperate.
            System.Console.WriteLine(string.Join(",", System.Array.ConvertAll<int, string>(genericArray, System.Convert.ToString)));

            //Finally take the memory we updated and synchronize it with the array we wrote to.
            genericArray.UpdateFrom(asArray);

            //Verify the writes
            if (clrArray[2] != asArray[0]) throw new System.Exception("Failed to set original memory when writing to fromArray memory");

            if (clrArray[3] != asArray[1]) throw new System.Exception("Failed to set original memory when writing to fromArray memory");

            //Show the writes have persisted
            System.Console.WriteLine(string.Join(",", System.Array.ConvertAll<int, string>(genericArray, System.Convert.ToString)));

            //Which does show the modifications to the original array.
            System.Console.WriteLine(string.Join(",", System.Array.ConvertAll<int, string>(clrArray, System.Convert.ToString)));

            #region ReinterpretCast

            //A different kind of slice... would be useful with fixed...
            
            //This hides the Length :( there are someways to fix that but the ArrayHeader itself has to be modified which isn't hard to do but still...

            int total = 0;

            short[] shorts = Concepts.Classes.Unsafe.ReinterpretCast<int[], short[]>(clrArray);

            try
            {
                foreach (var shortType in shorts)
                {
                    System.Console.WriteLine("shortValue: " + shortType); ++total; 
                }
            }
            catch
            {

            }

            System.Console.WriteLine("totalShorts:" + total);

            //System.Console.WriteLine(cast[-1]);

            System.Console.WriteLine(shorts[0]);

            System.Console.WriteLine(shorts[1]);

            //Out of bounds.
            System.Console.WriteLine(shorts[2]);

            //0.....
            System.Console.WriteLine("shortLength: " + shorts.Length);

            total = 0;

            //Try again from bytes
            byte[] bytes = Concepts.Classes.Unsafe.ReinterpretCast<int[], byte[]>(clrArray);

            try
            {
                foreach (var byteType in bytes)
                {
                    System.Console.WriteLine("byteType: " + byteType); ++total; 
                }
            }
            catch
            {

            }
            

            System.Console.WriteLine("totalShorts:" + total);

            System.Console.WriteLine(bytes[0]);

            System.Console.WriteLine(bytes[1]);

            System.Console.WriteLine(bytes[2]);

            System.Console.WriteLine(bytes[3]);

            System.Console.WriteLine(bytes[4]);

            //Out of bounds.
            //System.Console.WriteLine(bytes[5]);

            //System.Console.WriteLine(bytes[15]);

            //0.....
            System.Console.WriteLine("byteLength: " + bytes.Length);

            //Fails..
            //System.Console.WriteLine(cast[3]);

            //System.Console.WriteLine(cast[4]);

            //System.Console.WriteLine(cast[5]);

            //System.Console.WriteLine(cast[7]);

            //System.Console.WriteLine(string.Join(",", System.Array.ConvertAll<short, string>(cast, System.Convert.ToString)));

            //String / Char tests (Needs ArrayLike specialization to also work apparently)

            string clrString = "test";           

            unsafe
            {
                fixed (char* p = clrString)
                {


                    System.Console.WriteLine("char&@0: " + (int)p);

                    System.Console.WriteLine("char@0: " + clrString[0]);
                    System.Console.WriteLine("char@1: " + clrString[1]);
                    System.Console.WriteLine("char@2: " + clrString[2]);
                    System.Console.WriteLine("char@3: " + clrString[3]);
                    

                    char[] unsafeclrChars = Concepts.Classes.Unsafe.ReinterpretCast<string, char[]>(clrString);

                    //Won't work because the header of string doesn't match Array, would have to manually set Length...
                    System.Console.WriteLine(unsafeclrChars[0]);

                    fixed (char* q = unsafeclrChars)
                    {
                        System.Console.WriteLine("q@" + (int)q);
                    }

                    /*
                    System.Console.WriteLine(clrChars[1]);
                    System.Console.WriteLine(clrChars[2]);
                    System.Console.WriteLine(clrChars[3]);
                    */

                    //This works the first time until the protection checks occur.
                    //Suprisingly byte will fail immediately if you try to access members with the indexer.
                    Concepts.Classes.Array<byte> testBytes = new Concepts.Classes.Array<byte>(clrString);

                    Concepts.Classes.Array<char> testChars = new Concepts.Classes.Array<char>(clrString);

                    foreach (var byteType in testBytes)
                    {
                        System.Console.WriteLine("charValue Default:" + System.Text.Encoding.Default.GetChars(new byte[] { byteType })[0]);
                        System.Console.WriteLine("charValue ASCII:" + System.Text.Encoding.ASCII.GetChars(new byte[] { byteType })[0]);
                        System.Console.WriteLine("charValue UTF8:" + System.Text.Encoding.UTF8.GetChars(new byte[] { byteType })[0]);
                        System.Console.WriteLine("byteType: " + byteType);
                    }

                    foreach (var charType in testChars)
                    {
                        System.Console.WriteLine("byteValue: " + (byte)charType);
                        System.Console.WriteLine("charType: " + charType);
                    }

                    //Crash..
                    //System.Console.WriteLine(test[0]);

                    //Fails immediately when the protection has occured.
                    //System.Collections.Generic.IList<char> cIlist = test;

                    //Crash
                    //System.Console.WriteLine(string.Join(",", System.Array.ConvertAll<char, string>(test, System.Convert.ToString)));

                    //System.Console.WriteLine(test[0]);
                }
            }



            //etc

            #endregion
        }
    }
}