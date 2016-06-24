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

#endregion

namespace Media.Common
{
    /// <summary>
    /// Provides a reference to an array of byte with an optional offset.
    /// </summary>
    /// <remarks>This instance should never finalize</remarks>
    public class MemorySegment : SuppressedFinalizerDisposable, IEnumerable<byte>, ICloneable
    {
        #region Statics

        public static readonly byte[] EmptyBytes = new byte[0];

        //Length can be set by other classes through reflection.
        public static readonly MemorySegment Empty = new MemorySegment(EmptyBytes, false);

        /// <summary>
        /// Creates a new instance using a copy of the data in the source
        /// </summary>
        /// <param name="source"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <param name="shouldDispose"></param>
        /// <returns></returns>
        public static MemorySegment CreateCopy(byte[] source, int offset, int count, bool shouldDispose = true)
        {
            MemorySegment result = new MemorySegment(count, shouldDispose);

            if(source != null) System.Array.Copy(source, offset, result.m_Array, 0, count);

            return result;
        }

        #endregion

        #region Fields

        internal protected byte[] m_Array;

        internal protected long m_Offset, m_Length;

        //public readonly Binary.ByteOrder ByteOrder;

        //internal protected uint Flags;

        //IReadOnly

        #endregion

        #region Properties

        /// <summary>
        /// The amount of bytes this instance references
        /// </summary>
        public int Count
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return (int)m_Length; }
            
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            protected set { m_Length = value; }
        }

        /// <summary>
        /// <see cref="Count"/>
        /// </summary>
        public long LongLength
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_Length; }
            
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            protected set { m_Length = value; }
        }

        /// <summary>
        /// The offset in <see cref="Array"/>
        /// </summary>
        public int Offset
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return (int)m_Offset; }

            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            //could just have a Resize method
            protected set { m_Offset = value; }
        }

        /// <summary>
        /// <see cref="Offset"/>
        /// </summary>
        public long LongOffset
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_Offset; }

            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            protected set { m_Offset = value; }
        }

        /// <summary>
        /// The source array
        /// </summary>
        public byte[] Array
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_Array; }

            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            protected set { m_Array = value; } 
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Should never run unless immediately finalized.
        /// </summary>
        ~MemorySegment()
        {
            m_Array = null; 
            
            m_Length = 0;

            Dispose(ShouldDispose = true);

#if DEBUG
            System.Diagnostics.Debug.WriteLine(ToString() + "@Finalize Completed");
#endif
        }
        
        #region Chained

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public MemorySegment(byte[] reference, bool shouldDispose = true)
            : base(shouldDispose)
        {
            if (reference == null) throw new ArgumentNullException("reference");
            
            m_Array = reference;

            m_Length = m_Array.LongLength;

            //ByteOrder = Binary.SystemEndian;
        }

        public MemorySegment(byte[] reference, int offset, bool shouldDispose = true)
            : this(reference, shouldDispose)
        {
            m_Offset = (uint)offset;

            //m_Length -= offset;

            if (m_Offset > m_Length) throw new ArgumentOutOfRangeException("offset");
        }

        public MemorySegment(byte[] reference, int offset, int length, bool shouldDispose = true)
            : this(reference, offset, shouldDispose)
        {
            m_Length = length;

            if (m_Offset + m_Length > m_Array.LongLength) throw new ArgumentOutOfRangeException("length");
        }

        #endregion

        /// <summary>
        /// Constructs an instance with the given size
        /// </summary>
        /// <param name="size"></param>
        /// <param name="shouldDispose"></param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public MemorySegment(long size, bool shouldDispose = true) 
            : base(shouldDispose)
        {
            if (size < 0) throw new ArgumentException("size");

            m_Length = size;

            //If the amount is unaligned then aligned it
            size += Machine.CalulcateAlignedSize(ref size);

            m_Offset = 0;

            m_Array = new byte[size];

            ShouldDispose = shouldDispose;
        }

        /// <summary>
        /// Constructs an instance from another instance
        /// </summary>
        /// <param name="other"></param>
        /// <param name="force"></param>
        /// <param name="shouldDispose"></param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public MemorySegment(MemorySegment other, bool force = false, bool shouldDispose = true)
            : base(shouldDispose)
        {
            //If there is no other instance return
            if (other == null) return;

            //If the finalizer has ran
            if (false.Equals(other.IsUndisposed))
            {
                //If this allocation was not contrived
                if (false.Equals(force))
                {
                    m_Array = EmptyBytes;

                    m_Length = m_Offset = 0;

                    //Dispose immediately
                    Dispose(true);

                    //Do nothing else
                    return;
                }
                
                //Reregister for finalize the other instance
                GC.ReRegisterForFinalize(other);
            }

            m_Array = other.Array;

            m_Offset = other.m_Offset;

            m_Length = other.m_Length;

            //ByteOrder = other.ByteOrder;
        }

        #endregion

        //Make an Enumerator implementation to help with Skip and Copy?

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        IEnumerator<byte> IEnumerable<byte>.GetEnumerator()
        {
            for (int i = 0; i < m_Length; ++i)
            {
#if UNSAFE || NATIVE
                yield return System.Runtime.InteropServices.Marshal.ReadByte(System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement<byte>(m_Array, (int)m_Offset + i));
#else
                yield return m_Array[m_Offset + i]; //this[i]
#endif
            }
        }

        //IEnumerator<byte> GetReverseEnumerator()
        //{
        //    for (uint i = m_Length - 1; i <= 0; --i)
        //    {
        //        yield return m_Array[m_Offset + i]; //this[i]
        //    }
        //}

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<byte>)this).GetEnumerator();
        }

        #region Indexer

        /// <summary>
        /// Allows access by offset index to the source array, negitive values can be used to go previous to the <see cref="Offset"/> and the amount is not enforced to be within the <see cref="Count"/>
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public byte this[int index]
        {
#if UNSAFE
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]

            //Could also use UnsafeAddrOfPinnedArrayElement
            //get { unsafe { return *(byte*)System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement<byte>(m_Array, m_Offset + index); } }
            get { unsafe { fixed (byte* p = &m_Array[m_Offset]) return *(p + index); } }
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]

            //set { unsafe { *(byte*)System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement<byte>(m_Array, m_Offset + index) = value; } }
            set { unsafe { fixed (byte* p = &m_Array[m_Offset]) *(p + index) = value; } }
#elif NATIVE
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]

            get { return System.Runtime.InteropServices.Marshal.ReadByte(System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement<byte>(m_Array, (int)m_Offset + index)); }
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]

            set { System.Runtime.InteropServices.Marshal.WriteByte(System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement<byte>(m_Array, (int)m_Offset + index), value); }
#else
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]

            get { return m_Array[m_Offset + index]; }
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]

            set { m_Array[m_Offset + index] = value; }
#endif
        }

        #endregion

        #region Unused

        //...
        //internal byte[] this[params object[] arg]
        //{
        //    get
        //    {
        //        if (Common.Extensions.Array.ArrayExtensions.IsNullOrEmpty(arg)) return Common.MemorySegment.EmptyBytes;
        //        var a = new ArgIterator((System.RuntimeArgumentHandle)arg[0]);
        //        var tr = a.GetNextArg();
        //        System.TypedReference.SetTypedReference(tr, this);
        //        return m_Array;
        //    }
        //}

        //Raw copy/copyto
        //internal T Get<T> (int offset)
        //{
        //    return default(T);
        //}

        #endregion

        #region Methods

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public MemorySegment Clone(bool force = false, bool shouldDispose = true) { return new MemorySegment(this, force, shouldDispose); }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public void Update(ref byte[] source, ref int offset, ref int length)
        {
            if (IsDisposed) return;

            m_Array = source;

            m_Offset = offset;

            m_Length = length;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public void Update(ref byte[] source)
        {
            if (IsDisposed) return;

            m_Array = source;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public void Update(ref int offset) { m_Offset = offset; }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public void IncreaseLength(long length) { m_Length += length; }

        #endregion

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return (int)(m_Array.GetHashCode() ^ m_Offset ^ m_Length);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj)
        {
            if (System.Object.ReferenceEquals(this, obj)) return true;

            if (false == (obj is MemorySegment)) return false;

            MemorySegment other = obj as MemorySegment;

            return Equals(other);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public bool Equals(MemorySegment other)
        {
            if (other == null) return false;

            return other.GetHashCode() == GetHashCode();
        }

        #region Operators

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(MemorySegment a, MemorySegment b)
        {
            object boxA = a, boxB = b;
            return boxA == null ? boxB == null : a.Equals(b);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(MemorySegment a, MemorySegment b) { return false == (a == b); }

        //>> , << etc

        #endregion

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        object ICloneable.Clone() { return Clone(); }
    }

    //Should probably enforce usability with additional derivations, Todo

    //UsableMemorySegment, IUsable

    //ReadOnlyMemorySegment, IReadOnly

#if UNSAFE
    //internal class UnsafeMemorySegment : MemorySegment, System.Collections.Generic.IList<byte>
    //{
    //    //Don't need the byte[] but do need a way to get the bytes from an IntPtr.
    //    //See Array<T> in concepts for a better overall structure
    //}
#endif

    //Should propably be the base class of MemorySegment because it's a lower order concept
    /// <summary>
    /// Extends MemorySegment with the ability to store certain bit offsets
    /// </summary>
    public class BitSegment : MemorySegment
    {
        long m_BitOffset, m_BitCount;

        public int BitCount
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return (int)m_BitCount; }
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            protected set { m_BitCount = value; }
        }

        public long LongBitCount
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_BitCount; }
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            protected set { m_BitCount = value; }
        }

        public int BitOffset
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return (int)m_BitOffset; }
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            protected set { m_BitOffset = value; }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public BitSegment(int bitSize, bool shouldDispose = true) : base(Common.Binary.BitsToBytes(ref bitSize), shouldDispose) { m_BitCount = bitSize; }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public BitSegment(byte[] reference, int bitOffset, int bitCount, bool shouldDispose = true)
            : base(reference, Common.Binary.BitsToBytes(ref bitOffset), Common.Binary.BitsToBytes(ref bitCount), shouldDispose)
        {
            m_BitOffset = bitOffset;

            m_BitCount = bitCount;
        }

        //reference may be null
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public BitSegment(byte[] reference, int bitOffset, bool shouldDispose = true) : this(reference, bitOffset, Common.Binary.BytesToBits(reference.Length) - bitOffset, shouldDispose) { }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public BitSegment(byte[] reference) : this(reference, 0, Common.Binary.BytesToBits(reference.Length)) { }


        //Would have to implement a Copy virtual method to ensure that bitOffsets were not accidentally copied using Array.Copy

    }

    /// <summary>
    /// Provides useful extension methods for the <see cref="MemorySegment"/> class
    /// </summary>
    public static class MemorySegmentExtensions
    {
        //public static System.IO.MemoryStream ToMemoryStream() readable, writeable, publicablyVisible...

        public static byte[] ToArray(this MemorySegment segment)
        {
            if (Common.IDisposedExtensions.IsNullOrDisposed(segment)) return null;

            if (segment.Count == 0) return MemorySegment.EmptyBytes;

            byte[] copy = new byte[segment.LongLength];

            CopyTo(segment, copy, 0, segment.Count);

            //Copy the rest
            if (segment.LongLength > segment.Count) Array.Copy(segment.Array, segment.Offset + segment.Count, copy, segment.Count, segment.LongLength - segment.Count);

            return copy;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static System.ArraySegment<byte> ToByteArraySegment(this MemorySegment segment)
        {
            return new ArraySegment<byte>(segment.Array, segment.Offset, segment.Count);
        }
        
        /// <summary>
        /// Copies all bytes from the segment to dest
        /// </summary>
        /// <param name="segment"></param>
        /// <param name="dest"></param>
        /// <param name="destinationIndex">The offset in <paramref name="dest"/> to start copying</param>
        public static void CopyTo(this MemorySegment segment, byte[] dest, int destinationIndex)
        {
            CopyTo(segment, dest, destinationIndex, segment.Count);
        }

        /// <summary>
        /// Copies bytes from the segment to the dest
        /// </summary>
        /// <param name="segment"></param>
        /// <param name="dest"></param>
        /// <param name="destinationIndex">The offset in <paramref name="dest"/> to start copying</param>
        /// <param name="length">The amount of bytes to copy from <paramref name="segment"/></param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static void CopyTo(this MemorySegment segment, byte[] dest, int destinationIndex, int length)
        {
            if (Common.IDisposedExtensions.IsNullOrDisposed(segment)) return;

            //could check dest and also verify length 

            Array.Copy(segment.Array, segment.Offset, dest, destinationIndex, length);
        }

        //make Left / Right or expect the callers to use -length when they need to...
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static MemorySegment Subset(this MemorySegment segment, int offset, int length, bool shouldDispose = true)
        {
            //Should propably enforce that offset and length do not supercede existing length or this is not a true subset.
            return new MemorySegment(segment.Array, offset, length, shouldDispose);
        }

        public static int Find(byte[] source, int start, int count, MemorySegment first, params MemorySegment[] segments)
        {
            int found = 0;

            int needed = count;

            first = null;

            foreach (var segment in segments)
            {
                //Search for the partial match in the segment
                found = Utility.ContainsBytes(segment.Array, ref start, ref count, source, start, needed);

                //If it was found
                if (found >= 0)
                {
                    //If not already set then set it
                    if(first == null) first = segment;

                    //Subtract from needed and if 0 remains break
                    if ((needed -= found) == 0) break;

                    //Continue
                    continue;
                }
                
                //Reset the count, the match needs to be found in order.
                needed = count;

                //Reset to no first segment
                first = null;
            }

            //return the index or the last partial match.
            return found;
        }
    }

    #region Musing

#if UNSAFE

    //Ugly and requires unsafe to build but is efficient

    //Could probably just keep the array managed and not require unsafe and at if I changed up the layout a bit.

    //Could or should also have native functionality

    /// <summary>
    /// Provides a structure which contains a byte array of 4 bytes
    /// </summary>
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Explicit, Size = Common.Binary.BytesPerInteger)]
    public unsafe struct FourBytes
    {
        [CLSCompliant(false)]
        [System.Runtime.InteropServices.FieldOffset(0)]
        public fixed byte ByteArray[Common.Binary.BytesPerInteger];

        [CLSCompliant(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public FourBytes(byte* b, int offset, int length, bool reverse = false)
        {
            if (b == null) throw new System.ArgumentNullException();

            if(reverse) fixed (byte* B = ByteArray) for (int i = length - 1; i <= 0; --i) *((byte*)(B + i)) = *((byte*)(b + i + offset));
            else fixed (byte* B = ByteArray) for (int i = 0; i < length; ++i) *((byte*)(B + i)) = *((byte*)(b + i + offset));
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public unsafe FourBytes(byte[] bytes, int offset, int length, bool reverse = false)
            :this()
        {
            if (bytes == null) throw new System.ArgumentNullException();

            fixed (byte* b = bytes)
            {
                if (reverse) fixed (byte* B = ByteArray) for (int i = length - 1; i <= 0; --i) *((byte*)(B + i)) = *((byte*)(b + i + offset));
                else fixed (byte* B = ByteArray) for (int i = 0; i < length; ++i) *((byte*)(B + i)) = *((byte*)(b + i + offset));
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            fixed (byte* b = ByteArray)
            {
                return b->GetHashCode();
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public unsafe override string ToString()
        {
            return ToString(null);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public unsafe string ToString(System.Text.Encoding encoding)
        {
            fixed (byte* b = ByteArray)
            {
                return System.Runtime.InteropServices.Marshal.PtrToStringAuto((System.IntPtr)b, Common.Binary.BytesPerInteger);
                //return (encoding ?? System.Text.Encoding.Default).GetString(b, Common.Binary.BytesPerInteger);
            }
        }

    }

    /// <summary>
    /// Provides a structure which contains a byte array of 8 bytes
    /// </summary>
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Explicit, Size = Common.Binary.BytesPerLong)]
    public unsafe struct EightBytes
    {
        [CLSCompliant(false)]
        [System.Runtime.InteropServices.FieldOffset(0)]
        public fixed byte ByteArray[Common.Binary.BytesPerLong];

        [CLSCompliant(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public unsafe EightBytes(byte* b, int offset, int length, bool reverse = false)
        {
            if (b == null) throw new System.ArgumentNullException();

            if (reverse) fixed (byte* B = ByteArray) for (int i = length - 1; i <= 0; --i) *((byte*)(B + i)) = *((byte*)(b + i + offset));
            else fixed (byte* B = ByteArray) for (int i = 0; i < length; ++i) *((byte*)(B + i)) = *((byte*)(b + i + offset));
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            fixed (byte* b = ByteArray)
            {
                return b->GetHashCode();
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public unsafe override string ToString()
        {
            return ToString(null);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public unsafe string ToString(System.Text.Encoding encoding)
        {
            fixed (byte* b = ByteArray)
            {
                return System.Runtime.InteropServices.Marshal.PtrToStringAuto((System.IntPtr)b, Common.Binary.BytesPerLong);
                //return (encoding ?? System.Text.Encoding.Default).GetString(b, Common.Binary.BytesPerLong);
            }
        }
    }

    /// <summary>
    /// Provides a structure of 4 bytes, 32 bits which allows access to the individual bytes as value types.
    /// </summary>
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Explicit, Size = Common.Binary.BytesPerInteger)]
    public struct Word
    {
        [System.Runtime.InteropServices.FieldOffset(0)]
        public FourBytes Bytes;

        [System.Runtime.InteropServices.FieldOffset(0)]
        public int Value;

        [CLSCompliant(false)]
        [System.Runtime.InteropServices.FieldOffset(0)]
        public uint UnsignedValue;

        [System.Runtime.InteropServices.FieldOffset(0)]
        public short High;

        [System.Runtime.InteropServices.FieldOffset(2)]
        public short Low;

        [CLSCompliant(false)]
        [System.Runtime.InteropServices.FieldOffset(0)]
        public ushort UnsignedHigh;

        [CLSCompliant(false)]
        [System.Runtime.InteropServices.FieldOffset(2)]
        public ushort UnsignedLow;

        [System.Runtime.InteropServices.FieldOffset(0)]
        public byte Zero;

        [System.Runtime.InteropServices.FieldOffset(1)]
        public byte One;

        [System.Runtime.InteropServices.FieldOffset(2)]
        public byte Two;

        [System.Runtime.InteropServices.FieldOffset(3)]
        public byte Three;

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public Word(byte[] value, int offset, int length)
            : this()
        {
            if (value == null) throw new System.ArgumentNullException();

            Bytes = new FourBytes(value, offset, length);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public Word(int value)
            : this()
        {
            Value = value;
        }

        [CLSCompliant(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public Word(uint value)
            : this()
        {
            UnsignedValue = value;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public Word(byte zero, byte one, byte two, byte three)
            : this()
        {
            Zero = zero;

            One = one;

            Two = two;

            Three = three;
        }
    }

    /// <summary>
    /// Provides a struvture of 8 bytes, 64 bits which allows access to the individual bytes as value types.
    /// </summary>
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Explicit, Size = Common.Binary.BytesPerLong)]
    public struct DoubleWord
    {
        [System.Runtime.InteropServices.FieldOffset(0)]
        public long Value;

        [CLSCompliant(false)]
        [System.Runtime.InteropServices.FieldOffset(0)]
        public ulong UnsignedValue;

        [System.Runtime.InteropServices.FieldOffset(0)]
        internal EightBytes Bytes;

        [System.Runtime.InteropServices.FieldOffset(0)]
        public Word One;

        [System.Runtime.InteropServices.FieldOffset(4)]
        public Word Two;

        [CLSCompliant(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public DoubleWord(uint value)
            : this()
        {
            One = new Word(value);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public DoubleWord(int value)
            : this()
        {
            One = new Word(value);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public DoubleWord(long value)
            : this()
        {
            Value = value;
        }

        [CLSCompliant(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public DoubleWord(ulong value)
            : this()
        {
            UnsignedValue = value;
        }


        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public DoubleWord(byte zero, byte one, byte two, byte three)
            : this()
        {
            One = new Word(zero, one, two, three);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public DoubleWord(byte zero, byte one, byte two, byte three, byte four, byte five, byte six, byte seven)
            : this(zero, one, two, three)
        {
            Two = new Word(four, five, six, seven);
        }

    }

    //All to get something like this
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Explicit, Size = Common.Binary.BytesPerInteger)]
    public struct Framing
    {
        [System.Runtime.InteropServices.FieldOffset(0)]
        Word Word;

        [CLSCompliant(false)]
        public Framing(byte channel, ushort length)
        {
            Word = new Word();

            Word.One = channel;

            Word.UnsignedLow = length;
        }

        public Framing(byte channel, short length)
        {
            Word = new Word();

            Word.One = channel;

            Word.Low = length;
        }

        public Framing(short length)
        {
            Word = new Word();

            Word.High = length;
        }

        [CLSCompliant(false)]
        public Framing(ushort length)
        {
            Word = new Word();

            Word.UnsignedHigh = length;
        }

        //Wouldn't be able to send on a socket without interop.
        //public byte[] ToBytes()
        //{
        //    return Word.Bytes.ByteArray;
        //}
    }

#endif

    //The AlignedByteSegment could store it's values in the m_Offset, keep m_Array null and use a special m_Count or not
    //Coulbe be IntPtr for aligned access also.... but that would be super abusive and non intuitive...
    //The problem would be that unsafe access would be required and it would look ugly, it could look slightly nicer using int or long as shown.

    //public class AlignedSegment
    //{
    //    int Member = 0;

    //    public int Count { get; protected set; }

    //    public unsafe byte this[int index]
    //    {
    //        get { fixed (int* x = &Member) return *(((byte*)x) + index); }
    //        set { fixed (int* x = &Member) *(((byte*)x) + index) = value; }
    //    }

    //These copy by value, e.g. dereference the ref to to the copy.
    //There would have to be a setter for byte* to use this with single bytes...
    //The meaning would not be the same as you would think if the value of one changes after this call.
    //To achive that a byte* version would be need and it would have to be fixed..
    //Fixing is bad enough and to fix a single byte is even worse...

    //    public AlignedSegment(ref byte one)
    //    {
    //        this[0] = one;
    //    }
    //    public AlignedSegment(ref byte one, byte two)
    //        :this(one)
    //    {
    //        this[1] = two;
    //    }
    //    public AlignedSegment(ref byte one, ref byte two,  ref byte three)
    //        : this(one, two)
    //    {
    //        this[2] = three;
    //    }
    //    public AlignedSegment(ref byte one, ref byte two, ref byte three, ref byte fourc)
    //        : this(one, two, three)
    //    {
    //        this[3] = fourc;
    //    }
    //}

    #endregion

    #region Concepts...

    ///////////////////////////////////
    //SpannedMemorySegment / MemorySegmentList

    //MemorySegmentPointer Array is obtained with unsafe or function...    

    //Enumerable version

    //public EnumerableMemorySegment(IEnumerable<byte> source, int offset, int length)
    //{
    //    //m_Array = source.GetEnumerator()

    //    m_Offset = (uint)offset;

    //    m_Length = (uint)length;
    //}

    #endregion
}

namespace Media.UnitTests
{

    public class MemorySegmentTests
    {
        public void TestConstructorAndDispose()
        {
            using (Common.MemorySegment test = new Common.MemorySegment(4))
            {
                if (test.Offset != 0) throw new System.Exception("Offset");

                if (test.Count != 4) throw new System.Exception("Count");

                if (Common.Binary.Read32(test, 0, false) != uint.MinValue) throw new System.Exception("Array");

                if (test.IsDisposed) throw new System.Exception("IsDisposed");

                Common.Binary.Write32(test.Array, test.Offset, false, 1);

                if (test.IsDisposed) throw new System.Exception("IsDisposed");

                if (Common.Binary.Read32(test, 0, false) != 1) throw new System.Exception("Array");

                if (test.IsDisposed) throw new System.Exception("IsDisposed");

                foreach (byte b in test) if (System.Array.IndexOf(test.Array, b) == -1) throw new System.Exception("GetEnumerator");

                if (test.IsDisposed) throw new System.Exception("IsDisposed");

                byte[] bytes = test.ToArray();

                if (test.IsDisposed) throw new System.Exception("IsDisposed");

                if (false == bytes.SequenceEqual(test)) throw new System.Exception("Array");

                bytes = test.Concat(bytes).ToArray();

                if (test.IsDisposed) throw new System.Exception("IsDisposed");

                if (bytes.Length != 8) throw new System.Exception("ToArray");
            }
        }
    }
}