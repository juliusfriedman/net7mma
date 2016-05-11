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
    // Generic types cannot have this layout...
    //[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Explicit)]
    public class ArrayElement<T> : System.Collections.Generic.IList<T>
    {
        //bool m_IsReadOnly

        //[System.Runtime.InteropServices.FieldOffset(0)]
        internal int m_Index = 0;

        //[System.Runtime.InteropServices.FieldOffset(4)]
        internal T m_Source;

        //Todo, could have PseudoArrayHeader.

        #region Properties

        /// <summary>
        /// The element
        /// </summary>
        public T Source
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_Source; }
        }

        /// <summary>
        /// The index
        /// </summary>
        public int Index
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_Index; }
        }

        /// <summary>
        /// The length
        /// </summary>
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

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return Source.GetHashCode();
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
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

    #region Notes

    //ArrayLike was renamed to ArrayElement.

    //Could probably use ArrayLike to make chars of String accessible
    //e.g. ArrayLike<string> which also implements the char overloads...

    //e.g. ArrayLike<int> from ArrayLike<byte>

    //e.g. ArrayLike<short> from ArrayLike<int>

    //etc

    //The concept still works as in the static ArrayTest method though.

    //Shows how an array header can be 'forged'
    //using something like that i can imagine it would be possible to use the forged header to make arrays seem offset 
    //1) copy the bytes used in the element at the offset which corresponds to the size of the array header.
    //2) make an array header with the desire type (and offset length)
    //3) put that header where the copied bytes existed.
    //4) cast the pointer from ArrayHeader to T[] which will then point to the header at the first element would be @ the offset, the count would be the offset count.
    //5) put the data back from the indexes which didn't change (where the forged header was)    
    
    //Could be done in Finalize etc.

    //The following attempts to do all of the leg work for the above, the only thing which is not yet implemented is the array header forging with the above class.

    #endregion

    #region PseudoArrayHeader

    //[System.Runtime.CompilerServices.UnsafeValueType]
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Explicit)]
    struct PseudoArrayHeader //Implement Finalize to have the updates from the implicit copy propagate to the source.
    {
        #region Constructor

        //PseudoArrayHeader() : this() { }

        //------------

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public PseudoArrayHeader(object source) : this()
        {
            m_IntPtr = Unsafe.AddressOf<object>(ref m_Object);

            //IntPtr may overlap m_Length.
            //m_Length = Unsafe.ArrayOfTwoElements<object>.AddressingDifference();

            m_Object = source;
        }
        
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public PseudoArrayHeader(System.Array source, int offset) 
            : this(source, offset, source.Length - offset)
        {

        }
        
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public PseudoArrayHeader(System.Array source, int offset, int count) : this()
        {
            m_Array = source;

            m_Length = count;

            m_Offset = offset;
        }

        #endregion

        #region Methods

        //------------

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public bool IsNullPointerOrObject() { return m_IntPtr == System.IntPtr.Zero | m_Object == null; }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public bool IsNullPointer() { return m_IntPtr == System.IntPtr.Zero; }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public bool IsNullObject() { return m_Object == null; }

        //Would be used to indicate if the m_IntPtr member has a value which points to what was a managed object at one point.
        //[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        //public bool IsNative() { return m_Length == 0 && m_Offset > 0; }
        //public bool IsNative() { return m_IntPtr == AddressOf8(); }

        //Should be the same in unsafe as fixed(int*x = &this){ int*y = x + System.IntPtr.Size; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal System.IntPtr get_AddressOf8() { return Unsafe.AddressOf<object>(ref m_Object); }

        /// <summary>
        /// The length of the data in the array or the low bytes if <see cref="m_IntPtr"/> is assigned
        /// </summary>
        /// <returns></returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public int get_Length() { return m_Length; }

        /// <summary>
        /// The object which can be relocated in memory's reference
        /// </summary>
        /// <returns></returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public object get_Object() { return m_Object; }

        /// <summary>
        /// The array interpretation of <see cref="m_Object"/>
        /// </summary>
        /// <returns></returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public System.Array Array() { return m_Array; }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj) { return (m_Object ?? m_IntPtr).Equals(obj); }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() { return (m_Object ?? m_IntPtr).GetHashCode(); }

        #endregion

        #region Fields

        //------------
        
        //4 or 8 bytes
        [System.Runtime.InteropServices.FieldOffset(0)]
        internal System.IntPtr m_IntPtr;

        //4 bytes
        [System.Runtime.InteropServices.FieldOffset(0)]
        internal int m_Offset;

        //------------ (always the low bytes of m_IntPtr when assigned)

        //4 bytes
        [System.Runtime.InteropServices.FieldOffset(4)]
        internal int m_Length;

        //------------ Should be last in structure)

        //4 or 8 bytes as reference, used to keep a reference local so fixed is not necessary.
        [System.Runtime.InteropServices.FieldOffset(8)]
        internal object m_Object;

        [System.Runtime.InteropServices.FieldOffset(8)]
        internal System.String m_String;

        [System.Runtime.InteropServices.FieldOffset(8)]
        internal System.Array m_Array;

        //------------ (Should be stored at offsets higher than the object, because pointers may be 4 or 8 bytes, especially for when an Array is used.)

        //4 or 8 bytes
        ////[System.Runtime.InteropServices.FieldOffset(16)]
        //internal System.IntPtr m_IntPtr2;

        //4 bytes
        //[System.Runtime.InteropServices.FieldOffset(16)]
        //int m_SourceSize;

        //4 bytes, always the low bytes of m_IntPtr2 when assigned.
        //[System.Runtime.InteropServices.FieldOffset(20)]
        //int m_Version; // used for various things, maybe m_Reserved is a better name.

        #endregion

        //SizeOf() => 16 on x64 and 12 on x86, (+4 for m_SourceSize, +4 for m_Version) for a total of 20 - 24 bytes.
    }

    #endregion

    #region Array

    /// <summary>
    /// Provides a generic ArraySegment / Slice class.
    /// Has static methods for reading and writing Array types without a bounds check.
    /// Has static methods for updating arrays from the slice.
    /// Has methods to update the source from a different array.  
    /// Can be used to convert strings to bytes or modify the bytes in a string in place.
    /// Can implicitly become an array which is stored as a reference in <see cref="Allocations"/>
    /// </summary>
    /// <notes>
    /// Creates a new array when implicitly assigned from one.. this means this class could track created array instances for whatever purpose...
    /// </notes>
    /// <typeparam name="T"></typeparam>
    public class Array<T> : System.Collections.Generic.IList<T> //=> ICollection<T>, IEnumerable<T>, IEnumerable, 
                                                                 //could also be implicitly IList<ArrayElement<T>>
    {
        #region Fields

        /// <summary>
        /// The 12 - 16 bytes which reside directly at offset 0. (possibly 20 - 24)
        /// </summary>
        PseudoArrayHeader m_Header; //Todo, could store source element size, still must convert when reading if sizes are different.

        //4 - 8 + bytes (IntPtr), maybe null for strings or pointer types, duplicate of reference stored in header.
        /*readonly */ T[] m_Source;

        //All implict reads are stored here for whatever purpose necessary, especially if they need to be sychronized or their header has been modified.
        internal readonly System.Collections.Generic.HashSet<T[]> Allocations = new System.Collections.Generic.HashSet<T[]>();

        #endregion        

        #region Constructor

        //[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        //public Array<T>(){  m_Header.m_SizeOfT = Unsafe.ArrayOfTwoElements<T>.AddressingDifference(); }

        /// <summary>
        /// Given any T this will convert the chars in the string to T.
        /// </summary>
        /// <param name="s">The string</param>
        /// <param name="offset">The offset in the string</param>
        /// <param name="length">The length of characters to use from the string.</param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public Array(string s, int offset = 0, int length = -1)
        {
            int bytesPerT = Unsafe.ArrayOfTwoElements<T>.AddressingDifference();

            //Negitive length means use the whole length.
            if (length <= -1) length = s.Length;

            //m_SizeOfT

            //Store the object reference
            m_Header.m_String = s;

            #region Remarks

            //It's possible that these two pointers could be made equal but the casting is not necessary now, only when reading.
            //Since the header structure potentially has 16 + extra bytes the header could also be relocated and reverted with a method set, Prepare, Restore
            //This would allow pointers given to the source to be natively read and write and start directly at the offset given and maintain the bounds check appropraitely.

            //m_Source = Unsafe.ReinterpretCast<object, T[]>(ref m_Header.m_Object);

            #endregion

            //Determine how many elements the array will have.
            switch (bytesPerT)
            {
                    //bytes
                case 1:
                    {
                        m_Header.m_Length = 2 * length; //s.Length;
                        
                        break;
                    }
                case 2://char, short, ushort
                    {
                        m_Header.m_Length = length; // s.Length;

                        break;
                    }
                default://uint, int, long, etc.
                    {
                        m_Header.m_Length = length * 2; //s.Length * 2;

                        //Handle conversion
                        if (bytesPerT > m_Header.m_Length)
                        {
                            m_Header.m_Length /= bytesPerT;
                        }
                        else
                        {
                            m_Header.m_Length /= bytesPerT;
                        }

                        break;
                    }
            }

            #region Unused

#if UNSAFE
            ////Determine the overhead of the clr header.
            //unsafe
            //{
            //    fixed (char* t = s)
            //    {
            //        //Store the address in Offset, increase by 12 for the CLR Header. (string is + 12 to the first character in x86 of x64)
            //        m_Header.m_Offset = (int)((int)(System.IntPtr)t - (int)Unsafe.AddressOf<string>(ref s));
            //    }
            //}
#else
            //Determine the overhead of the clr header, element -3 of the array would be at -6 from the first element (2 bytes per char)
            //m_Header.m_Offset = (int)((int)System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(m_Header.m_Array, -3) - (int)Unsafe.AddressOf<string>(ref s));
#endif

            #endregion

            //Adjust for character offset. (Should be done above in switch)...
            m_Header.m_Offset += -(3 + offset);
        }

        /// <summary>
        /// Array variant slice, Given any T this will convert the data in source to T.
        /// </summary>
        /// <param name="source">The source</param>
        /// <param name="offset">The offset in the source</param>
        /// <param name="count">The amount of elements in the span.</param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public Array(System.Array source, int offset, int count)
        {
            //if (count + offset > source.Length) throw new System.ArgumentOutOfRangeException();

            //Store the object reference
            m_Header.m_Array = source;

            //Doesn't matter
            //m_Source = Unsafe.ReinterpretCast<System.Array, T[]>(ref m_Header.m_Array);

            //Store the offset to access the first element of the array.
            m_Header.m_Offset = offset;

            //Set the length
            m_Header.m_Length = count;

            //Use reserved in the header to indicate source is null

            //store sizes of original elements, could use 0 to indicate source was null.
        }

        #region Generic

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public Array(T t)
        {
            //Store the object reference (helps with branchless reading / writing)
            m_Header.m_Array = m_Source = Common.Extensions.Object.ObjectExtensions.ToArray<T>(t);
            
            m_Header.m_Offset = 0;

            m_Header.m_Length = 1;
        }
      
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public Array(T[] source, int offset, int count)
        {
            //Store the object reference (helps with branchless reading / writing)
            m_Header.m_Array = m_Source = source;

            m_Header.m_Offset = offset;

            m_Header.m_Length = count;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public Array(T[] source, int offset) : this(source, offset, source.Length - offset) { }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public Array(T[] source) : this(source, 0, source.Length) { }

        #endregion

        #endregion

        #region Properties

        /// <summary>
        /// Gets the structure at offset 0, used for Native pointers.
        /// </summary>
        internal PseudoArrayHeader Header
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_Header; }
        }

        /// <summary>
        /// A <see cref="System.Array"/> cast of the <see cref="Source"/>.
        /// </summary>
        public System.Array OriginalArray //Array name is the same as enclosing type..
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_Header.m_Array; }            
        }

        /// <summary>
        /// A <see cref="System.Object"/> case of the <see cref="Source"/>
        /// </summary>
        public System.Object OriginalObject
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_Header.m_Object; }
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
            get { return m_Header.m_Offset; }
        }

        /// <summary>
        /// How many elements this array holds
        /// </summary>
        public int Length
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_Header.m_Length; }
        }

        #endregion

        #region Contains

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public bool Contains(ArrayElement<T> element)
        {
            return UnsafeRead(m_Source, ref element.m_Index).Equals(element.Source);
        }

        //Should be index of.

        public bool Contains(T[] array) { return Contains(array, 0, array.Length); }

        public bool Contains(Array<T> array) { return Contains(array.m_Source, array.m_Header.m_Offset, array.m_Header.m_Length); }

        public bool Contains(T[] array, int offset, int length)
        {
            //Try the fast way...
            if (array == m_Source &&
                //Check for the offset to be present in the array
                0 >= m_Header.m_Offset - offset
                &&
                //Check for the length to be present
                offset <= m_Header.m_Length && length <= m_Header.m_Length) return true;

            //Check for equality manualy.

            //Could do the memcmp type thing here for a performance increase.

            int matched = 0;

            for (int i = offset; i < length; ++i)
            {
                if (UnsafeRead(array, ref i).Equals(this[i])) ++matched;
                else matched = 0;
            }

            //Should keep return or output i so this can call IndexOf()>= 0;

            return matched == length;
        }

        #endregion

        #region Indexer

        public T this[ArrayElement<T> arrayIndex]
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get
            {
                //return m_Source[m_Offset + index];

                return UnsafeRead(this, ref arrayIndex.m_Index);
            }

            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            set
            {
                //m_Source[m_Offset + index] = value;

                UnsafeWrite(this, arrayIndex.m_Index, ref arrayIndex.m_Source);
            }
        }

        public T this[int index]
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

        //See also http://www.codeproject.com/Articles/3467/Arrays-UNDOCUMENTED

        //public T System.Collections.Generic.IList<T>. this[int index]
        //{
        //    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        //    get
        //    {
        //        //return m_Source[m_Offset + index];

        //        return UnsafeRead(this, ref index);
        //    }

        //    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        //    set
        //    {
        //        //m_Source[m_Offset + index] = value;

        //        UnsafeWrite(this, index, ref value);
        //    }
        //}

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        int System.Collections.Generic.IList<T>.IndexOf(T item)
        {
            if (false == m_Header.IsNullObject()) return System.Array.IndexOf<T>(m_Source, item, m_Header.m_Offset, m_Header.m_Length);
            else for (int i = 0; i < m_Header.m_Length; ++i) if(UnsafeRead(this, ref i).Equals(item)) return i; 
            return -1;
        }

        void System.Collections.Generic.IList<T>.Insert(int index, T item)
        {
            throw new System.NotImplementedException();
            //InsertHelper
            //StringBuilder for strings..
        }

        void System.Collections.Generic.IList<T>.RemoveAt(int index)
        {
            throw new System.NotImplementedException();
            //RemoveHelper
            //StringBuilder for strings..
        }

        void System.Collections.Generic.ICollection<T>.Add(T item)
        {
            throw new System.NotImplementedException();
            //Insert(Count, item);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        void System.Collections.Generic.ICollection<T>.Clear()
        {
            if (false == m_Header.IsNullObject()) System.Array.Clear(m_Source, m_Header.m_Offset, m_Header.m_Length);
            else
            {
                T toWrite = default(T); for (int i = 0; i < m_Header.m_Length; ++i) UnsafeWrite(this, i, ref toWrite);
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        bool System.Collections.Generic.ICollection<T>.Contains(T item)
        {
            return System.Array.IndexOf<T>(m_Source, item, m_Header.m_Offset, m_Source.Length - m_Header.m_Length) >= 0;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        void System.Collections.Generic.ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            System.Array.Copy(m_Source, m_Header.m_Offset, array, arrayIndex, m_Header.m_Length);
        }

        int System.Collections.Generic.ICollection<T>.Count
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_Header.m_Length; }
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
            for (int i = 0; i < m_Header.m_Length; ++i) yield return UnsafeRead(this, ref i); //m_Source[m_Offset + i];
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return System.Linq.Enumerable.Skip(m_Source, m_Header.m_Offset).GetEnumerator();
        }

        #endregion

        #region Operators

        /// <summary>
        /// Allocates a new native array of T with the contents from the source array T starting at the offset and spanning the length indicated by the instance.
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public unsafe static implicit operator T[](Array<T> array)
        {
            //Maybe faster to use Array.Empty<T>() and resize.

            //Make a new array
            T[] result = new T[array.m_Header.m_Length];

            //Determine the size in bytes of all elements
            int sz = Unsafe.ArrayOfTwoElements<T>.AddressingDifference() * array.m_Header.m_Length;

            //Copy from the starting address to the end of the contents to the result array.
            //Maybe able to use Buffer.BlockCopy..
            //System.Buffer.MemoryCopy((void*)ComputeAddress(array), (void*)System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement<T>(result, 0), sz, sz);

            //Brachless
            System.Buffer.MemoryCopy((void*)ComputeBaseAddress(array), (void*)System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement<T>(result, 0), sz, sz);

            //Add the allocation to the hash
            array.Allocations.Add(result);

            //return the result.
            return result;

            //System.Runtime.InteropServices.GCHandle gcHandle = default(System.Runtime.InteropServices.GCHandle);

            //try
            //{
            //    result = new T[array.m_Length];

            //    gcHandle = default(System.Runtime.InteropServices.GCHandle);

            //    int sz = Unsafe.ArrayOfTwoElements<T>.AddressingDifference() * array.m_Length;

            //    gcHandle = System.Runtime.InteropServices.GCHandle.Alloc(result, System.Runtime.InteropServices.GCHandleType.Pinned);

            //    System.Buffer.MemoryCopy((void*)ComputeAddress(array), gcHandle.AddrOfPinnedObject().ToPointer(), sz, sz);

            //    array.Allocations.Add(result);

            //    return result;

            //}
            //catch { throw; }
            //finally { if (gcHandle.IsAllocated) gcHandle.Free(); }

            //Are potentially other ways to do the same damn thing.
            //CommonIntermediateLanguage.Cpyblk<T>(array.m_Source, array.m_Offset, result, 0, sz);

            //have alignment issues here
            //System.Buffer.BlockCopy(array.m_Source, array.m_Offset, result, 0, sz);

            //Alternatively when array.m_Source is not null
            //System.Array.Copy(array.m_Source, array.m_Offset, result, 0, array.m_Length);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static implicit operator Array<T>(T[] array) { return new Array<T>(array); }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Array<T> a, Array<T> b) { object boxA = a, boxB = b; return boxA == null ? boxB == null : a.Equals(b); }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Array<T> a, Array<T> b) { return false == (a == b); }

        #endregion

        #region Statics

        /// <summary>
        /// Creates an <see cref="System.ArraySegment"/>&lt;<typeparamref name="T"/>&gt; without causing an allocation of <see cref="System.Array"/> if the generic array's source was a native array.
        /// </summary>
        /// <param name="array">The generic array</param>
        /// <param name="segment">The array segment created</param>
        /// <returns>True if <paramref name="segment"/> was created.</returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static bool TryGetArray(Array<T> array, out System.ArraySegment<T> segment)
        {
            //Could have m_Reserved next to m_SizeOfT in m_Header and use m_Reserved to balance offset.
            //Would also know if source was null and size of T for original
            //Could be close but may need to branch for when we don't want the implicit array.
            //new System.ArraySegment<T>(array, array.m_Header.m_Offset + array.m_Header.m_Reserved, array.m_Header.m_Length);

            //Branch...  same as array.m_Source == null but faster because of 0
            //Uses implicit conversion to array because array.m_Source may be null, (array.m_Source ?? array) works but still branches.
            segment = array.m_Header.m_Offset < 0 ? 
                //Strings...
                new System.ArraySegment<T>(array /*array.m_Source ?? array*/, 0, array.m_Header.m_Length) : 
                //Everything else
                new System.ArraySegment<T>(array /*array.m_Source ?? array*/, array.m_Header.m_Offset, array.m_Header.m_Length);

            return true;
        }

        /// <summary>
        /// Updates the given source array using the given generic array's <see cref="UpdateFrom"/> method.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="from"></param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static void Update(/*this */T[] source, Array<T> from) { from.UpdateFrom(source); }

        #region Unused

        ///// <summary>
        ///// Computes the address of the first element in the array based on offset used to create it.
        ///// </summary>
        ///// <param name="array"></param>
        ///// <returns></returns>
        //[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        //static System.IntPtr ComputeAddress(Array<T> array)
        //{
        //    //Could do something with offset to skip the null check.
        //    //return array.m_Source == null ? (System.IntPtr)array.m_Header.m_Offset : System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement<T>(array.m_Source, array.m_Header.m_Offset);

        //    //If the object was not null then compute the address using the stored offset, otherwise use the native array.
        //    //This could also be used for native pointers by using array.m_IntPtr...
        //    return false == array.m_Header.IsNullObject() ? array.m_Header.get_AddressOf8() + array.m_Header.m_Offset : System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement<T>(array.m_Source, array.m_Header.m_Offset);

        //    #region Branchless

        //    //Need to scope an index, and should be able to use when native or string.
        //    //ComputeElementAlignedAddress(array, array.m_Header.m_Offset)

        //    //Almost works for branchless logic but uses ??, also offset is not checked to be in bounds and automatically aligned to size of element type.
        //    //return System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(array.m_Source ?? array.m_Header.m_Array, array.m_Header.m_Offset);

        //    //Almost works but uses math.
        //    //return array.m_Header.AddressOf8() + array.m_Header.m_Offset;

        //    //return System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(array.m_Source ?? array.m_Header.m_Array, array.m_Header.m_Offset);
        //    //Alignment, to work correctly the m_Offset must be in chars and not T...
        //    //System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(array.m_Header.m_Array, 0) - 6
        //    //System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(array.m_Header.m_Array, 0)

        //    //Null would be 0 but element access need to be aligned to the pointer type of T to work correctly which is what UnsafeAddrOfPinnedArrayElement adjusts for, we could do this be factoring in sizeof(T)
        //    //return Unsafe.AddressOf(ref array.m_Source) + 16 + (Unsafe.ArrayOfTwoElements<T>.AddressingDifference() * array.m_Offset);

        //    #endregion
        //}

        #endregion

        /// <summary>
        /// Computes the address of the highest accessible element in the generic array
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static System.IntPtr ComputeMaxAddress(Array<T> array)
        {
            return System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(array.m_Header.m_Array, array.m_Header.m_Length);
        }

        /// <summary>
        /// Computes the address of the lowest accessible element in the generic array
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static System.IntPtr ComputeBaseAddress(Array<T> array)
        {
            return System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(array.m_Header.m_Array, array.m_Header.m_Offset);
        }

        #region Unused

        ////Not yet working 100% for aligned or unaligned cases
        ////The amount of bytes between index and the offset in the array, used to calulcate position. result can be +/-, used for BaseAddress + or MaxAddress -
        //[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        //static System.IntPtr ComputeElementAlignedAddress/*<ElementType>*/(Array<T> array, ref int index)
        //{
        //    //Not correctl because T may be different size than source, would require conversion to dest size from source size.
        //    //return System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(array.m_Header.m_Array, array.m_Header.m_Offset + index);

        //    //Where is element 0 in the array.
        //    System.IntPtr baseAddress = ComputeBaseAddress(array);

        //    //Strings... or different sizes...
        //    //How big is one element in source, array.m_SizeOfT
        //    //int sourceSize = ((int)System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(array.m_Source ?? array.m_Header.m_Array, 1) - (int)System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(array.m_Source ?? array.m_Header.m_Array, 0));

        //    //destSize is Unsafe.ArrayOfTwoElements<ElementType>.AddressingDifference()

        //    #region Unused

        //    //How far away is the element according to T
        //    //int distance = (int)baseAddress - (int)System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(array.m_Header.m_Array, index);

        //    //int distance = (int)baseAddress - (int)System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(array.m_Source ?? array.m_Header.m_Array, 0);

        //    //How far to the first element, add that to the baseAddress.
        //    //return baseAddress - distance;

        //    #endregion

        //    //Calulcate the address of the elment at index of (T source) according to size of T (dest)
        //    //return ((baseAddress - (sourceSize * array.m_Header.m_Offset)) + (Unsafe.ArrayOfTwoElements<T>.AddressingDifference() * index));

        //    return baseAddress + (Unsafe.ArrayOfTwoElements<T>.AddressingDifference() * index);
        //}

        #endregion

        /// <summary>
        /// Skips a bounds check while reading the index from source.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="index"></param>
        /// <returns>The element</returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static T UnsafeRead(T[] source, ref int index)
        {
            return Unsafe.Read<T>(System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement<T>(source, index));
        }

        [System.CLSCompliant(false)]
        public static T UnsafeRead(T[] source, int index) { return UnsafeRead(source, ref index); }

        /// <summary>
        /// Skips a bounds check while writing the index from source.
        /// </summary>
        /// <param name="source">The array to write</param>
        /// <param name="index">The index into the array</param>
        /// <param name="value">The value to write</param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static void UnsafeWrite(T[] source, ref int index, ref T value)
        {
            Unsafe.Write<T>(System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement<T>(source, index), ref value);
        }

        [System.CLSCompliant(false)]
        public static void UnsafeWrite(T[] source, int index, ref T value) { UnsafeWrite(source, ref index, ref value); }

        /// <summary>
        /// Reads a <typeparamref name="T"/> from the array at the index.
        /// </summary>
        /// <param name="array"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public unsafe static T UnsafeRead(Array<T> array, ref int index)
        {
            //ComputeAddress(array) + ComputeElementAddress(array, ref index)
            //return Unsafe.Read<T>(ComputeAddress(array) + Unsafe.ArrayOfTwoElements<T>.AddressingDifference() * index);

            //Branchless
            return Unsafe.Read<T>(ComputeBaseAddress(array) + Unsafe.ArrayOfTwoElements<T>.AddressingDifference() * index);
        }

        /// <summary>
        /// Writes a <typeparamref name="T"/> from the array at the index.
        /// </summary>
        /// <param name="array"></param>
        /// <param name="index"></param>
        /// <param name="value"></param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public unsafe static void UnsafeWrite(Array<T> array, int index, ref T value)
        {
            //UnsafeWrite(array.m_Source, array.m_Offset + index, ref value);
            //Unsafe.Write<T>(System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement<T>(array.m_Source, array.m_Offset + index), ref value);

            //Need the size to address the element correctly
            int size = Unsafe.ArrayOfTwoElements<T>.AddressingDifference();

            //Write the element at the computed address
            //Unsafe.Write<T>(ComputeAddress(array) + size * index, ref value, ref size);

            //Branchless
            Unsafe.Write<T>(ComputeBaseAddress(array) + size * index, ref value, ref size);
        }

        //Here an empty array could be created and the RTTI modified such that the pointer to the first element points to the offset desired,
        //Unfortunately there is no such member on the array.
        //What would be done in a IDisposable pattern or otherwise is to copy the array header to the offset of the first desired element and change the length in that header.
        //This causes 12 - 16 bytes to be wasted so those bytes need to be swapped out somewhere so they can be put back on dispose.
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static T[] ArrayTest(Array<T> array)
        {
            T[] refArray = array.m_Source;

            T[] tArray = refArray;

            //To see the interesting bits use a new array. (beware if you don't the original array will also be modified)
            ////Take an empty array (already allocated members) to 0.
            //T[] tArray = System.Array.Empty<T>();

            //See notes in Unsafe.Create

            //int overHead = 16;

            ////Check for value type or ref type.
            //if (false == typeof(T).IsValueType) //primitive
            //{
            //    overHead -= 4;
            //}

            //if (array.Source.Length > 81920)
            //{
            //    overHead += 8;
            //}

            //This works well

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

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return (int)(m_Source.GetHashCode() ^ m_Header.m_Offset ^ m_Header.m_Length);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
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
        public void UpdateFrom(T[] source, int offset, int count) { System.Array.Copy(source, offset, m_Source, m_Header.m_Offset, count); }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public void UpdateFrom(T[] source) { UpdateFrom(source, 0, source.Length); }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public void UpdateFrom(Array<T> array) { UpdateFrom(array.m_Source, array.m_Header.m_Offset, array.m_Header.m_Length); }        

        #endregion
    }

    #endregion

    #region ManagedBufferPool

    //ManagedBufferPool is another good concept
    //https://github.com/dotnet/corefx/issues/4547

    #endregion
}


//https://github.com/dotnet/corefx/pull/7966

//https://github.com/DotNetCross/Memory.Unsafe/issues/7

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

            //Populate this as a test which should not cause an System.Array allocation.
            System.ArraySegment<byte> arraySegmentByte;

            //Make the generic array.
            Concepts.Classes.Array<int> genericArray = new Concepts.Classes.Array<int>(clrArray, offset, count);

            //Take 16 bytes from the 4 integer values in the array above.
            Concepts.Classes.Array<byte> testBytez = new Concepts.Classes.Array<byte>(clrArray, 0, 16);

            //Print the bytes
            foreach (var byteType in testBytez) System.Console.WriteLine("byteType: " + byteType);

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

            //Get the bytes without causing an allocation
            if (Concepts.Classes.Array<byte>.TryGetArray(testBytez, out arraySegmentByte))
            {
                foreach (var byteType in arraySegmentByte)
                {
                    System.Console.WriteLine("byteType: " + byteType);
                }
            }

            #region String Tests

            string clrString = "test";           

            //Unsafe is just for comparison, we don't need unsafe as the Array class keeps a reference to the string or object it was sourced from.
            unsafe
            {
                fixed (char* p = clrString)
                {


                    System.Console.WriteLine("char&@0: " + (int)p);

                    System.Console.WriteLine("char@0: " + clrString[0]);
                    System.Console.WriteLine("char@1: " + clrString[1]);
                    System.Console.WriteLine("char@2: " + clrString[2]);
                    System.Console.WriteLine("char@3: " + clrString[3]);
                    

                    char[] unsafeclrChars = Concepts.Classes.Unsafe.ReinterpretCast<string, char[]>(ref clrString);

                    System.IntPtr ptrCast = Concepts.Classes.Unsafe.ReinterpretCast<string, System.IntPtr>(ref clrString);

                    System.Console.WriteLine("p@" + ptrCast.ToString());

                    //Won't work because the header of string doesn't match Array, would have to manually set Length and place the header at the correct offset.
                    //System.Console.WriteLine(unsafeclrChars[0] = 'b');

                    System.Console.WriteLine(unsafeclrChars[0]);

                    fixed (char* q = unsafeclrChars)
                    {
                        System.Console.WriteLine("q@" + (int)q);
                    }

                    //could also make a special UnicodeByteArray / use Span which reads the string at aligned offsets to allow conversion to a byte[]
                    Concepts.Classes.Array<byte> testBytes = new Concepts.Classes.Array<byte>(clrString);

                    Concepts.Classes.Array<char> testChars = new Concepts.Classes.Array<char>(clrString);

                    Concepts.Classes.Array<long> testLong = new Concepts.Classes.Array<long>(clrString);

                    //Can't use the array :(
                    //System.Array.Sort(testBytes.m_Header.m_Array)
                    //'System.Array.Sort(testBytes.m_Header.m_Array)' threw an exception of type 'System.RankException'
                    //    base: {"Only single dimension arrays are supported here."}
                    //testBytes.m_Header.m_Array.GetUpperBound()
                    //No overload for method 'GetUpperBound' takes 0 arguments
                    //testBytes.m_Header.m_Array.GetUpperBound(0)
                    //'testBytes.m_Header.m_Array.GetUpperBound(0)' threw an exception of type 'System.ArgumentException'
                    //    base: {"Cannot find the method on the object instance."}
                    
                    //Get the bytes without causing an allocation
                    if (Concepts.Classes.Array<byte>.TryGetArray(testBytes, out arraySegmentByte))
                    {
                        foreach (var byteType in arraySegmentByte)
                        {
                            System.Console.WriteLine("byteType: " + byteType);
                        }
                    }

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

                    foreach (var longType in testLong)
                    {
                        System.Console.WriteLine("longType: " + longType.ToString("X"));
                    }

                    System.Collections.Generic.IList<char> cIlist = testChars;

                    //Modify the char in the string...
                    cIlist[0] = 'a';

                    System.Console.WriteLine(cIlist[0]);
                    System.Console.WriteLine(clrString[0]);
                    System.Console.WriteLine(testChars[0]);

                    //Write to the low order byte in the first char. (of the string) through the testBytes
                    testBytes[1] = 0x80;

                    if (clrString[0] != '聡') throw new System.Exception("Failed to set original memory");

                    // '聡', e, s, t
                    System.Console.WriteLine(string.Join(",", System.Array.ConvertAll<char, string>(testChars, System.Convert.ToString)));

                    //Chars are two bytes, we only modify one at a time...
                    //a, 0x80, e, 0
                    System.Console.WriteLine(string.Join(",", System.Array.ConvertAll<byte, string>(testBytes, System.Convert.ToString)));

                    System.Console.WriteLine(testLong[0]);
                }
            }

            //etc

            #endregion

            #region ReinterpretCast

            //A different kind of slice... would be useful with fixed...

            //This hides the Length :( there are someways to fix that but the ArrayHeader itself has to be modified which isn't hard to do but still...

            int total = 0;

            short[] shorts = Concepts.Classes.Unsafe.ReinterpretCast<int[], short[]>(ref clrArray);

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
            byte[] bytes = Concepts.Classes.Unsafe.ReinterpretCast<int[], byte[]>(ref clrArray);

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


            System.Console.WriteLine("totalBytes:" + total);

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

            #endregion

        }
    }
}