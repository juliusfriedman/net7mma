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
using System.Linq;
using System.Collections.Generic;
#endregion

namespace Media.Common
{
    /// <summary>
    /// Used to crete a continious stream to locations of memory which may not be next to each other and could even overlap.
    /// </summary>
    public class SegmentStream : System.IO.Stream, IDisposed
    {
        ///// <summary>
        ///// Combines all given instances into a single instance.
        ///// </summary>
        ///// <param name="streams"></param>
        ///// <returns>The combined instance.</returns>
        //public static SegmentStream Combine(params SegmentStream[] streams)
        //{
        //    return new SegmentStream(streams.SelectMany(s=> s.Segments));
        //}

        #region Fields

        long m_Position, m_Count;

        readonly IList<Common.MemorySegment> Segments;

        Common.MemorySegment WorkingSegment = Common.MemorySegment.Empty;

        int m_Index = -1;

        //Could keep remaining instead of cursor would be easier to keep track of but would require an extra calulcation given Position
        //Position => can be based on the cursor with m_Position + 'm_Cursor' and based on remaining with m_Position + '(WorkingSegment.m_Length - m_Remaining)'

        long m_Cursor;

        #endregion

        #region Constructor

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public SegmentStream() { Segments = new System.Collections.Generic.List<Common.MemorySegment>(); }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public SegmentStream(IList<Common.MemorySegment> existing)
        {
            if (existing == null) throw new ArgumentNullException();

            //Use the existing list
            Segments = existing;

            m_Count = Segments.Sum(s => Common.IDisposedExtensions.IsNullOrDisposed(s) ? 0 : s.m_Length);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public SegmentStream(IEnumerable<Common.MemorySegment> existing)
        {
            //Create a new list.
            Segments = new System.Collections.Generic.List<Common.MemorySegment>(existing);

            m_Count = Segments.Sum(s => Common.IDisposedExtensions.IsNullOrDisposed(s) ? 0 : s.m_Length);
        }

        #endregion

        #region Methods

        //Write without copy
        public void AddMemory(Common.MemorySegment segment)
        {
            if (Common.IDisposedExtensions.IsNullOrDisposed(segment)) segment = Common.MemorySegment.Empty;

            Segments.Add(segment);

            m_Count += segment.m_Length;
        }

        //Write with copy
        public void AddPersistedMemory(Common.MemorySegment segment)
        {
            //Allow dead space...
            if (Common.IDisposedExtensions.IsNullOrDisposed(segment))
            {
                AddMemory(Common.MemorySegment.Empty);

                return;
            }

            //Could avoid LOH allocation by splitting blocks..
            //Would need additional state, seems like a job for a dervied class, BlockSegmentStream...
            Common.MemorySegment copy = new MemorySegment(segment.m_Length, false); //Don't dispose unless forced

            //Copy the data
            System.Array.Copy(segment.m_Array, segment.m_Offset, copy.m_Array, 0, segment.m_Length);

            //Add it.
            AddMemory(copy);
        }

        //Todo, should write directly into segments until at end and then create new segments?

        /// <summary>
        /// Writes a copy of the given data at the end of the stream for now
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        public void WritePersisted(byte[] buffer, int offset, int count)
        {
            if (IsDisposed || count <= 0) return;

            AddPersistedMemory(new Common.MemorySegment(buffer, offset, count));
        }


        public void InsertMemory(int index, Common.MemorySegment toInsert) { InsertMemory(ref index, toInsert); }

        [CLSCompliant(false)]
        public void InsertMemory(ref int index, Common.MemorySegment toInsert)
        {
            if (Common.IDisposedExtensions.IsNullOrDisposed(toInsert)) toInsert = Common.MemorySegment.Empty;

            Segments.Insert(index, toInsert);

            //Should be optional to update the position
            //m_Postion should be offset also if the segment being inserted is at an index <= m_Index;
            if (index <= m_Index)
            {
                m_Position += toInsert.m_Length;

                ++m_Index;
            }

            //Update the count;
            m_Count += toInsert.m_Length;
        }

        public void InsertPersistedMemory(int index, Common.MemorySegment toInsert) { InsertPersistedMemory(ref index, toInsert); }

        [CLSCompliant(false)]
        public void InsertPersistedMemory(ref int index, Common.MemorySegment toInsert)
        {

            if (Common.IDisposedExtensions.IsNullOrDisposed(toInsert))
            {
                InsertMemory(ref index, (Common.MemorySegment.Empty));

                return;
            }

            Common.MemorySegment copy = new MemorySegment(toInsert.Count);

            System.Array.Copy(toInsert.m_Array, toInsert.m_Offset, copy.m_Array, 0, toInsert.m_Length);

            InsertMemory(ref index, copy);
        }

        //Dirty, feels easiery to just insert, this would be something another type of stream would potentially do. (Add data to an existing segment)
        //void AppendMemory(ref int index, Common.MemorySegment with)
        //{
        //    if (index < 0 || index >= Segments.Count || Segments.IsReadOnly) return;

        //Could also just remove at index, create a new segment with the total len and insert at index.

        //    Common.MemorySegment ms = Segments[index];

        //    //Take the existing array
        //    byte[] array = ms.Array;

        //    //Determine the new size
        //    int len = ms.Count + with.Count;

        //    //Resize the array
        //    Array.Resize<byte>(ref array, ms.Count + with.Count);

        //    //Copy the new data
        //    Array.Copy(array, ms.Offset, with.Array, with.Offset, with.Count);

        //    //Set the length
        //    ms.m_Length = len;
        //}

        public void Free(int index) { Free(ref index); }

        [CLSCompliant(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal protected void Free(ref int index)
        {
            //Some lists are read only.
            if (index < 0 || index >= Segments.Count || Segments.IsReadOnly) return;

            //Call dispose at the end of this
            using (Common.MemorySegment segment = Segments[index])
            {
                //If index == m_Index the workingSegment needs to be maintained, it's no longer valid.

                //Remove the Segment
                Segments.RemoveAt(index);

                if (index <= m_Index)
                {
                    m_Position -= segment.m_Length;

                    --m_Index;
                }

                //Decrment for length of the segment
                m_Count -= segment.m_Length;
            }
        }

        /// <summary>
        /// Copies all contained data to the given destination.
        /// <see cref="Position"/> is not moved.
        /// </summary>
        /// <param name="destination">The destination</param>
        /// <param name="offset">The offset in <paramref name="destination"/> to start copying</param>
        /// <returns>The amount of bytes copied to <paramref name="destination"/>, usually equal to <see cref="Length"/></returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public int CopyTo(byte[] destination, int offset)
        {
            return CopyTo(destination, offset, (int)m_Count);
        }

        /// <summary>
        /// Copies the given count of data to the destinaion from the beginning of the stream.
        /// <see cref="Position"/> is not moved.
        /// </summary>
        /// <param name="destination">The destination</param>
        /// <param name="offset">The offset in <paramref name="destination"/> to start copying</param>
        /// <param name="count">The amount of bytes to copy</param>
        /// <returns>The amount of bytes copied to <paramref name="destination"/></returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public int CopyTo(byte[] destination, int offset, int count)
        {
            if (IsDisposed) return 0;

            int total = 0, min;

            foreach (Common.MemorySegment ms in Segments)
            {
                min = Binary.Min(count, ms.Count);

                System.Array.Copy(ms.Array, ms.m_Offset, destination, offset, min);

                offset += min;

                total += min;

                if ((count -= min) <= 0) break;
            }

            return total;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public new void CopyTo(System.IO.Stream s) { CopyToStream(s); }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public new void CopyTo(System.IO.Stream s, int bufferSize) { CopyToStream(s); }

        /// <summary>
        /// Creates a copy of all data in a contigious allocation.
        /// <see cref="Position"/> is not moved.
        /// </summary>
        /// <returns>The array created which will have a size equal to <see cref="Length"/></returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public byte[] ToArray()
        {
            if (IsDisposed) return null;

            byte[] result = new byte[m_Count];

            CopyTo(result, 0, (int)m_Count);

            return result;
        }

        /// <summary>
        /// Reads the bytes from the current stream at <see cref="Position"/> and writes them to another stream. <see cref="Position"/> is during as the copy occurs.
        /// Note, this implementation does not use an intermediate buffer allocation.
        /// </summary>
        /// <param name="destination">The stream to which the contents of the current stream will be copied.</param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public void CopyToStream(System.IO.Stream destination)
        {
            if (IsDisposed) return;

            if (destination == null) throw new ArgumentNullException();

            int toCopy = 0;

            while (destination.CanWrite && CanRead && m_Position < m_Count)
            {
                //From the current cursor and array copy all bytes

                //Calculate how much to adjust
                toCopy = (int)(WorkingSegment.m_Length - m_Cursor);

                //Call Write
                destination.Write(WorkingSegment.Array, (int)(WorkingSegment.m_Offset + m_Cursor), toCopy);

                //Move the adjusted amount and if not at the end continue 
                if ((m_Position += toCopy) < m_Count)
                {
                    do //See notes in ReadByte
                    {
                        WorkingSegment = Segments[++m_Index];

                        m_Cursor = 0;

                    } while (m_Cursor >= WorkingSegment.m_Length);

                    continue;
                }

                //There is nothing left to copy
                break;
            }
        }

        /// <summary>
        /// Using a temporary stream created from the <see cref="Segments"/> contained, <see cref="Seek"/> is called and <see cref="Read"/> is called.
        /// </summary>
        /// <param name="streamOffset"></param>
        /// <param name="buffer"></param>
        /// <param name="bufferOffset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public int ReadAt(long streamOffset, byte[] buffer, int bufferOffset, int count)
        {
            //Use a temp stream to allow for threading...
            using (var tempStream = new SegmentStream(Segments))
            {
                //This stream should not dispose, the GC will manage it.
                //If it disposes then the current stream will be cleared...
                tempStream.ShouldDispose = false;

                //Seek to the given position
                tempStream.Seek(streamOffset, System.IO.SeekOrigin.Begin);

                //Call read
                tempStream.Read(buffer, bufferOffset, count);

                //If for some reason read does not return all data...
                //while((count -= tempStream.Read(buffer, bufferOffset, count)) > 0);

                //return (int)(tempStream.Position - streamOffset);
            }

            return count;
        }

        /// <summary>
        /// Using a temporary stream created from the <see cref="Segments"/> contained, <see cref="Seek"/> is called and <see cref="Write"/> is called.
        /// </summary>
        /// <param name="streamOffset"></param>
        /// <param name="buffer"></param>
        /// <param name="bufferOffset"></param>
        /// <param name="count"></param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public void WriteAt(long streamOffset, byte[] buffer, int bufferOffset, int count)
        {
            //Use a temp stream to allow for threading...
            using (var tempStream = new SegmentStream(Segments))
            {
                //This stream should not dispose, the GC will manage it.
                //If it disposes then the current stream will be cleared...
                tempStream.ShouldDispose = false;

                //Seek to the given position
                tempStream.Seek(streamOffset, System.IO.SeekOrigin.Begin);

                //Write to the buffer
                tempStream.Write(buffer, bufferOffset, count);
            }
        }

        /// <summary>
        /// Calls <see cref="Free"/> for each entry in <see cref="Segments"/>
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal protected void Clear()
        {
            for (int i = 0, e = Segments.Count - 1; e >= i; --e) Free(ref e);

            m_Index = -1;

            WorkingSegment = Common.MemorySegment.Empty;
        }       

        #endregion

        #region Properites

        #region Stream Override

        public override bool CanRead
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return false == IsDisposed; }
        }

        public override bool CanSeek
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return false == IsDisposed; }
        }

        public override bool CanWrite
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return false == IsDisposed; }
        }

        public override void Flush()
        {
            return;
        }

        public override long Length
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return (long)m_Count; }
        }

        public override long Position
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get
            {
                return (long)m_Position; // + m_Cursor causes a calc for each position but makes it slightly more accurate and makes individual movement faster in some cases.. (Determine how much)
            }
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            set
            {
                Seek(value, System.IO.SeekOrigin.Begin);
            }
        }

        #endregion

        /// <summary>
        /// The amount of bytes remaining in the stream based on <see cref="Position"/>
        /// </summary>
        public long Remains
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_Count - m_Position; }
        }

        #endregion

        #region Stream Method Overrides

        /// <summary>
        /// Reads a single byte from the underlying stream without an intermediate allocation.
        /// </summary>
        /// <returns>-1 if at the end of stream or <see cref="IsDisposed"/>, otherwise the byte read.</returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public override int ReadByte()
        {
            //If at the end of data return 0 unless closed...
            if (IsDisposed || m_Position >= m_Count) return -1;

            //Was if, changed to while to allow dead spots
            //Maybe should also handle null or disposed first with Common.IDisposedExtensions.IsNullOrDisposed(WorkingSegment)
            while (m_Cursor >= WorkingSegment.m_Length)
            {
                WorkingSegment = Segments[++m_Index];

                m_Cursor = 0;
            }

            byte result;

#if UNSAFE
            unsafe { result = *(byte*)System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement<byte>(WorkingSegment.m_Array, (int)(WorkingSegment.m_Offset + m_Cursor++)); }
#elif NATIVE
            result = System.Runtime.InteropServices.Marshal.ReadByte(System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement<byte>(WorkingSegment.m_Array, (int)(WorkingSegment.m_Offset + m_Cursor++)));
#else
            result = WorkingSegment.m_Array[WorkingSegment.m_Offset + m_Cursor++];
#endif
            ++m_Position;

            return result;
        }

        //For better performance a tempory array could be used and then Flush would be called to take the temporary array and copy it into the Segments at the appropriate index(- m_Cursor)
        /// <summary>
        /// Write one byte to the stream.
        /// If before the end of the stream the byte is written directly to the segment available, otherwise a new segment is added to <see cref="Segments"/> with only one byte.
        /// </summary>
        /// <param name="value"></param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public override void WriteByte(byte value)
        {
            //If at the end of data return 0 unless closed...
            if (IsDisposed) return;

            //If the data is new data
            if (m_Position >= m_Count)
            {
                //Todo, could improve with Flush api.

                //Add it
                AddMemory(new MemorySegment(Common.Extensions.Object.ObjectExtensions.ToArray<byte>(value)));

                //Increase the position
                ++m_Position;

                //Increase count;
                ++m_Count;

                //Set the cursor
                m_Cursor = 1;

                //Return
                return;
            }

            //See notes in ReadByte
            while (m_Cursor >= WorkingSegment.m_Length)
            {
                WorkingSegment = Segments[++m_Index];

                m_Cursor = 0;
            }
#if UNSAFE
            unsafe { *(byte*)System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement<byte>(WorkingSegment.m_Array, (int)(WorkingSegment.m_Offset + m_Cursor++)) = value; }
#elif NATIVE
            System.Runtime.InteropServices.Marshal.WriteByte(System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement<byte>(WorkingSegment.m_Array, (int)(WorkingSegment.m_Offset + m_Cursor++)), value);
#else
            //Write the value
            WorkingSegment.m_Array[WorkingSegment.m_Offset + m_Cursor++] = value;
#endif

            //Move the position
            ++m_Position;
        }

        //Could make this a threaded stream by giving absolute values for offset and count
        //Would then remove all instance fields including working segment and m_Cursor
        //Each read operation would be 'atomic' to that call

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            //If at the end of data return 0 unless closed...
            if (IsDisposed) return -1;

            if (count <= 0 || m_Position >= m_Count) return 0;

            //if (WorkingSegment == null) WorkingSegment = Segments[m_Index];

            int total = 0, toCopy;

            //While there is data to read and the data to read is in the region of memory we can read
            do
            {
                toCopy = (int)(WorkingSegment.m_Length - m_Cursor);

                toCopy = Common.Binary.Min(ref count, ref toCopy);

                //Copy the data from the working segment from the offset + cursor to the amount of bytes to copy.
                Array.Copy(WorkingSegment.m_Array, WorkingSegment.m_Offset + m_Cursor, buffer, offset, toCopy);

                //Increment for total
                total += toCopy;

                m_Position += toCopy;

                offset += toCopy;

                count -= toCopy;

                if ((m_Cursor += toCopy) >= WorkingSegment.m_Length && m_Position < m_Count)
                {
                    WorkingSegment = Segments[++m_Index];

                    m_Cursor = 0;
                }

                //break;
            } while (count > 0 && m_Position < m_Count);

            return total;
        }

        /// <summary>
        /// Based on the current <see cref="Position"/> writes the data to the stream.
        /// If <see cref="Position"/> approaches the end of the stream is reached the remaining data is added at the end of the stream by copying from the given buffer to a new segment.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            //If there is no way to write or nothing to write then do nothing.
            if (IsDisposed || count <= 0) return;

            //Keep a counter for how much we can copy or move.
            long len;

            //If the buffer is null or empty return for now (dead space)...
            if (Common.Extensions.Array.ArrayExtensions.IsNullOrEmpty(buffer, out len)) return;

            //If more data was specified than exists use len
            if (count > len) count = (int)len;

            //While there is count and the position is less than the end
            while (count > 0)
            {
                //How much can be copied to the current segment with respect to count.
                len = Common.Binary.Min(WorkingSegment.m_Length - m_Cursor, count);

                //Copy len from the buffer at the offset to the working segment's array at the offset + the cursor
                Array.Copy(buffer, offset, WorkingSegment.m_Array, WorkingSegment.m_Offset + m_Cursor, len);
                
                //Move the cursor
                m_Cursor += len;

                //Move the position
                m_Position += len;

                //Move the offset
                offset += (int)len;

                //Adjust count for len copied.
                //If there is nothing left to do return.
                if(0 == (count -= (int)len)) return;

                //If at the end of the current segment and not at the end of the stream
                if (m_Cursor >= WorkingSegment.m_Length && m_Position < m_Count)
                {
                    //Advance the segment
                    WorkingSegment = Segments[++m_Index];

                    //Set the cursor to 0
                    m_Cursor = 0;

                    //Do another iteration
                    continue;
                }

                //m_Position is >= m_Count

                //Add a copy of the memory which remains and set the WorkingSegment (could give via out)
                WritePersisted(buffer, offset, count);

                //Get the next segment and account for the segment added
                WorkingSegment = Segments[++m_Index];

                //Set the cursor
                m_Cursor = count;

                //Move the position
                m_Position += count;

                //Increase for the bytes written.
                m_Count += count;

                //Set count to 0;
                //count -= count;

                //Done
                break;
            }
        }

        /// <summary>
        /// Seeks to the given position in the stream.
        /// </summary>
        /// <param name="offset">The offset to seek to</param>
        /// <param name="origin">The seeking style to use</param>
        /// <returns>The <see cref="Position"/> after seeking.</returns>
        public override long Seek(long offset, System.IO.SeekOrigin origin)
        {
            if (IsDisposed) return -1;

            switch (origin)
            {
                case System.IO.SeekOrigin.Begin:
                    {
                        //Nothing to do?
                        if (m_Position == offset) return m_Position;

                        //If the offset past or at the end (Could goto End)
                        if (offset >= m_Count)
                        {
                            offset = 0;

                            goto case System.IO.SeekOrigin.End;
                        }
                        else if (offset <= 0)
                        {
                            //Should use End style when offset < 0 but not == 0...

                            WorkingSegment = Segments[(m_Index = 0)];

                            return m_Cursor = m_Position = 0;
                        }


                        //Check for data within the current segment
                        if (offset < m_Position)
                        {
                            //If at the first byte in a new segment go to the last segment
                            if (m_Cursor == 0)
                            {
                                WorkingSegment = Segments[--m_Index];

                                m_Cursor = WorkingSegment.m_Length;
                            }

                            ////Determine the amount of movement relative to the position
                            //long diff = m_Position - offset;

                            ////If the offset is within the current segment then there is no change of index
                            //if ((m_Cursor -= diff) >= 0)
                            //{
                            //    m_Cursor -= diff;

                            //    //return m_Position -= diff;

                            //    return m_Position = offset;
                            //}

                            //Combined for performance, if the data is not wihin the segment we search from the beginning anyway.

                            //This could be optomized by searching backward but current should handle that, this implies the locations will always be somewhere in the segment or at the beginning of the stream.

                            //If the offset is within the current segment then there is no change of index
                            if ((m_Cursor -= m_Position - offset) >= 0)
                            {
                                return m_Position = offset;
                            }

                            //Seek forward from 0 to find the segment
                            m_Position = m_Index = 0;

                            WorkingSegment = Segments[m_Index];
                        }
                        else //offset > m_Position
                        {
                            //Determine the amount of movement relative to the position
                            long diff = offset - m_Position;

                            //If the offset is within the current segment then there is no change of index
                            if ((m_Cursor += diff) < WorkingSegment.m_Length)
                            {
                                return m_Position = offset;
                            }

                            //The position must be moved to the end of the segment with respect to where the cursor was previously.
                            //Keep the same offset for a greater position
                            //Account for where we are already.

                            //The position is added to for the length of the remaining data in the WorkingSegment.
                            //The offset is modified to account for the position in the stream
                            offset -= (m_Position += WorkingSegment.m_Length - (m_Cursor - diff));

                            //Move to the next segment
                            WorkingSegment = Segments[++m_Index];

                            //Set the cursor to 0
                            m_Cursor = 0;
                        }

                        //Seek forward to find the offset.
                        do
                        {
                            m_Cursor = Binary.Min(ref WorkingSegment.m_Length, ref offset);

                            m_Position += m_Cursor;

                            offset -= m_Cursor;

                            if (m_Cursor >= WorkingSegment.m_Length)
                            {
                                WorkingSegment = Segments[++m_Index];

                                m_Cursor = 0;
                            }
                        }
                        while (offset > 0);

                        return m_Position;
                    }
                case System.IO.SeekOrigin.Current:
                    {
                        //If there is no change in offset return the position
                        if (offset == 0) return m_Position;

                        //When offset < 0
                        if (offset < 0)
                        {
                            //Make the offset based on the position
                            offset += m_Position;

                            goto case System.IO.SeekOrigin.Begin;
                        }

                        return m_Position;
                    }
                case System.IO.SeekOrigin.End:
                    {
                        //If the offset is referring to a index <= 0 goto the 0th position
                        if (-offset >= m_Count)
                        {
                            //<= 0 offset mean 0 to begin.
                            goto case System.IO.SeekOrigin.Begin;

                            //////If the offset desired is within the working segment no index change is required.
                            ////if (m_Cursor > 0 && offset <= m_Cursor)
                            ////{
                            ////    m_Cursor -= offset;

                            ////    return m_Position -= offset;
                            ////}


                            //////Seek backward to find the segment.
                            ////do
                            ////{
                            ////    m_Cursor = Binary.Min(ref m_Cursor, ref offset);

                            ////    m_Position -= m_Cursor;

                            ////    offset -= m_Cursor;

                            ////    if (offset > 0)
                            ////    {
                            ////        WorkingSegment = Segments[--m_Index];

                            ////        m_Cursor = WorkingSegment.m_Length;
                            ////    }
                            ////}
                            ////while (offset > 0);

                        }

                        //Move to the end position
                        WorkingSegment = Segments[(m_Index = Segments.Count - 1)];

                        //Set the cursor
                        m_Cursor = WorkingSegment.m_Length;

                        //Set the position
                        m_Position = m_Count;

                        //If the given offset refers to any other position seek from the end
                        if (offset < 0) goto case System.IO.SeekOrigin.Current;

                        //position is now equal to given offset >= 0 from the end.
                        return m_Position;
                    }
                default: return m_Position;
            }
            //x86 may benefit from break and return
            //return m_Position
        }

        /// <summary>
        /// Sets the length of the stream and sets <see cref="Position"/> to the end. If the value is greater than or equal to <see cref="Length"/> no change is performed.
        /// </summary>
        /// <param name="value"></param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public override void SetLength(long value)
        {
            if (IsDisposed || value >= m_Count) return;

            if (value <= 0)
            {
                Clear();

                return;
            }

            //Go to the position (m_Position and m_Cursor is set with respect to value)
            Seek(value, System.IO.SeekOrigin.Begin);

            //All segments after this segment will be removed.
            for (int i = m_Index + 1, e = Segments.Count - 1; i < e; ++i) Free(ref i);

            //Calculate the difference in the length of the working segment - where the cursor is which will be the new end of this stream
            m_Cursor = (WorkingSegment.m_Length - m_Cursor);

            //Dispose the old segment
            using (Common.MemorySegment previouslyWorkingSegment = WorkingSegment)
            {
                //make the new segment
                WorkingSegment = new MemorySegment(previouslyWorkingSegment.m_Array, (int)previouslyWorkingSegment.Offset, (int)m_Cursor, previouslyWorkingSegment.ShouldDispose);

                ////Set the position to count, Decrease for the change in bytes
                m_Position = m_Count -= WorkingSegment.m_Length - previouslyWorkingSegment.m_Length;
            }
        }

        /// <summary>
        /// Calls <see cref="Dispose"/> with the value of ShouldDispose. Called by base Dispose.
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public override void Close()
        {
            //IsClosed = true;
            //LeaveOpen...

            //If the stream should dispose then call base Close to call Dispose(true) and Supress the finalizer
            if (ShouldDispose) base.Close();
            else System.GC.SuppressFinalize(this);
        }

        #region Task Based

        //The only way to avoid the allocation in the base class with CopyTo is to handly the copying here....
        //If you use CopyTo the CLR will use it's own buffer to middle man the reading and writing to the destination.
        //http://referencesource.microsoft.com/#mscorlib/system/io/stream.cs
        //Alternatively a CopyToStream could be made or also a function which hid the base member via new.

        /// <summary>
        /// Asynchronously reads the bytes from the current stream and writes them to another stream without using an intermediate buffer allocation.
        /// </summary>
        /// <param name="destination">The stream to which the contents of the current stream will be copied.</param>
        /// <param name="bufferSize">This parameter is not used in this implementation</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A task that represents the asynchronous copy operation.</returns>
        public override System.Threading.Tasks.Task CopyToAsync(System.IO.Stream destination, int bufferSize, System.Threading.CancellationToken cancellationToken)
        {
            return CopyToAsyncInternal(destination, bufferSize, cancellationToken);
        }

        private async System.Threading.Tasks.Task CopyToAsyncInternal(System.IO.Stream destination, int bufferSize, System.Threading.CancellationToken cancellationToken)
        {
            int toCopy = 0;

            while (false == cancellationToken.IsCancellationRequested)
            {
                //From the current cursor and array copy all bytes

                //Calculate how much to adjust
                toCopy = (int)(WorkingSegment.m_Length - m_Cursor);

                //Await that write
                await destination.WriteAsync(WorkingSegment.Array, (int)(WorkingSegment.Offset + m_Cursor), toCopy, cancellationToken).ConfigureAwait(false);

                //Move the adjusted amount and if not at the end continue 
                if ((m_Position += toCopy) < m_Count)
                {
                    //Move to the next segment
                    WorkingSegment = Segments[++m_Index];

                    //Set the cursor to 0
                    m_Cursor = 0;

                    continue;
                }

                //There is nothing left to copy
                break;
            }
        }

        #endregion        

        #endregion

        #region Destructor / IDisposed

        internal protected bool ShouldDispose = true, IsDisposed;

        protected override void Dispose(bool disposing)
        {
            //Does nothing base.Dispose(disposing);

            //If disposing
            if (IsDisposed = disposing)
            {
                //Calls Close virtual (Calls Dispose(true) and GC.SuppressFinalize(this))
                //base.Dispose(); 

                //Clear memory
                Clear();

                m_Position = m_Cursor = -1;

                //Handled with free...
                if (WorkingSegment != null)
                {
                    BaseDisposable.SetShouldDispose(WorkingSegment, true, true);

                    WorkingSegment = null;
                }
            }
        }

        bool IDisposed.IsDisposed
        {
            get { return IsDisposed; }
        }

        bool IDisposed.ShouldDispose
        {
            get { return ShouldDispose; }
        }

        //Keep the same semantics as Stream...

        void IDisposable.Dispose() { Close(); }

        ~SegmentStream() { Close(); }

        #endregion
    }
}

namespace Media.UnitTests
{
    public class SegmentStreamTests
    {
        public static void TestWritingAndReadingAndSeeking()
        {
            //Test a random amount of times
            for (int test = 0, testCount = Utility.Random.Next(1, 100); test < testCount; ++test)
            {
                int TestBytesLength = Utility.Random.Next(100, 1024);

                //Console.WriteLine("RandomBytesLength = " + TestBytesLength);

                //Todo, use a single buffer and span the data betwen offsets....

                //Create random bytes
                byte[] randomBytes = new byte[TestBytesLength];

                //Create a buffer with the exact same length as the randomBytes
                byte[] buffer = new byte[TestBytesLength];

                //Fill with random bytes.
                Media.Utility.Random.NextBytes(randomBytes);

                //Copy those bytes
                randomBytes.CopyTo(buffer, 0);

                List<Common.MemorySegment> segments = new List<Common.MemorySegment>();

                int offset = 0, toTake = 0;

                //Make a segment stream
                using (Common.SegmentStream stream = new Common.SegmentStream(segments))
                {
                    //make random length segments of all bytes which are contained.
                    for (int remains = TestBytesLength; remains > 0; )
                    {
                        //Take a random amount
                        toTake = remains > 1 ? Media.Utility.Random.Next(1, remains) : 1;

                        //Ensure that we do not take more than what remains
                        if (toTake > remains) toTake = remains;

                        //Create the segment
                        Common.MemorySegment created = new Common.MemorySegment(randomBytes, offset, toTake);

                        //Add it to the stream (and list) //Write?
                        stream.AddMemory(created);

                        //Move the offset
                        offset += toTake;

                        //Decrease for what remains
                        remains -= toTake;
                    }

                    //Console.WriteLine(segments.Count);

                    //Ensure the amount of all memory added is equal to the length of the stream.
                    if (stream.Length != TestBytesLength) throw new System.Exception("Not equal Length");

                    //Seek to position 0.
                    stream.Position = 0;

                    //Enumerate each segment added
                    foreach (Media.Common.MemorySegment ms in segments)
                    {
                        //Create an array which is equal to the amount of bytes in the segment
                        byte[] expected = new byte[ms.Count];

                        //Read that many bytes and ensure the amount read
                        if (stream.Read(expected, 0, ms.Count) != ms.Count) throw new System.Exception("Read");

                        //Ensure the bytes read are equal to the existing memory
                        if (false == expected.SequenceEqual(ms)) throw new System.Exception("Not equal");

                        //Ensure the bytes read correspond to the original bytes added
                        if (false == randomBytes.Skip(ms.Offset).Take(ms.Count).SequenceEqual(expected)) throw new System.Exception("Not equal original");

                        //Read that many bytes and ensure the amount read
                        if (stream.ReadAt(ms.Offset, expected, 0, ms.Count) != ms.Count) throw new System.Exception("Read");

                        //Ensure the bytes read are equal to the existing memory
                        if (false == expected.SequenceEqual(ms)) throw new System.Exception("Not equal");

                        //Todo, fix bugs with Write
                        //Write that same data in the stream at the exact same position (writes to randomBytes)
                        stream.WriteAt(ms.Offset, expected, 0, ms.Count);

                        //Seek backward
                        stream.Seek(-ms.Count, System.IO.SeekOrigin.Current);

                        if (stream.Position != ms.Offset) throw new System.Exception("Position");

                        //Read that many bytes and ensure the amount read
                        if (stream.Read(expected, 0, ms.Count) != ms.Count) throw new System.Exception("Read");

                        //Ensure the bytes read correspond to the original bytes added (use buffer because randomByte was modified with WriteAt
                        if (false == buffer.Skip(ms.Offset).Take(ms.Count).SequenceEqual(expected)) throw new System.Exception("Not equal original");
                    }

                    //Ensure all bytes read in buffer are equal to the initial bytes and in the exact same order
                    if (false == buffer.SequenceEqual(randomBytes)) throw new System.Exception("Not equal");

                    //Clear buffer
                    Array.Clear(buffer, 0, TestBytesLength);

                    //Seek to position 0.
                    stream.Position = 0;

                    offset = 0;

                    int streamRemains = (int)(stream.Length - stream.Position);

                    //Iterate segments (try to read past the end)
                    while (offset < stream.Length)
                    {
                        //Issue random reads at the SegmentStream for a value inclusive of what remains in the stream.

                        int toRead = Utility.Random.Next(1, streamRemains);

                        if (toRead > streamRemains) toRead = streamRemains;

                        int streamRead = stream.Read(buffer, offset, toRead);

                        if (streamRead != toRead) throw new System.Exception("Read");

                        offset += streamRead;

                        streamRemains = (int)(stream.Length - stream.Position);
                    }

                    //Ensure all bytes read are equal to the initial bytes and in the exact same order
                    if (false == buffer.SequenceEqual(randomBytes)) throw new System.Exception("Not equal");

                    //Reset Position to 0.
                    stream.Position = 0;

                    //Test against memory stream (Fix the capacity to help with GC)
                    using (System.IO.MemoryStream memoryStream = new System.IO.MemoryStream(TestBytesLength))
                    {
                        //Ensure Read returns 0 at end of stream
                        stream.CopyTo(memoryStream);

                        //Create the array from the memory stream
                        byte[] actual = memoryStream.ToArray();

                        //Ensure that the bytes read are exactly equal
                        if (false == actual.SequenceEqual(randomBytes)) throw new System.Exception("Not equal");

                        //Test ToArray
                        actual = stream.ToArray();

                        //Ensure that the bytes read are exactly equal
                        if (false == actual.SequenceEqual(randomBytes)) throw new System.Exception("ToArray");

                        //Test CopyTo
                        if (TestBytesLength != stream.CopyTo(actual, 0) || false == actual.SequenceEqual(randomBytes)) throw new System.Exception("CopyTo");
                    }

                    //Reads at the end of the stream should return 0 bytes read.
                    if (stream.Read(randomBytes, 0, 1) != 0) throw new System.Exception("Read");

                    //Reads at the end of the stream should return -1 for the result of ReadByte
                    if (stream.ReadByte() != -1) throw new System.Exception("ReadByte");

                    //Ensure all bytes read are equal to the initial bytes and in the exact same order
                    if (false == buffer.SequenceEqual(randomBytes)) throw new System.Exception("Not equal");

                    //Reset Position to 0.
                    stream.Position = 0;

                    //Iterate for all bytes that should be in the stream
                    for (int i = 0; i < TestBytesLength; ++i)
                    {
                        //Check the result of Read
                        if (1 != stream.Read(buffer, i, 1) || buffer[i] != randomBytes[i]) throw new System.Exception("Read");
                    }

                    if (false == buffer.SequenceEqual(randomBytes)) throw new System.Exception("Not equal");

                    //Reset Position to 0.
                    stream.Position = 0;

                    //Iterate for all bytes that should be in the stream
                    for (int i = 0; i < TestBytesLength; ++i)
                    {
                        //Check the result of ReadByte
                        if (randomBytes[i] != stream.ReadByte()) throw new System.Exception("ReadByte");
                    }

                    //Reset Position to 0.
                    stream.Position = 0;

                    //Iterate for all bytes that should be in the stream
                    for (int i = 0; i < TestBytesLength; ++i)
                    {
                        //Ensure the position
                        if (stream.Position != i) throw new System.Exception("Position");

                        //Check the byte at Position
                        if (randomBytes[i] != stream.ReadByte()) throw new System.Exception("ReadByte");

                        //Set the position back by 1 using Seek.Begin
                        stream.Position = i;

                        //Ensure the position
                        if (stream.Position != i) throw new System.Exception("Position");

                        //Read the byte again
                        if (randomBytes[i] != stream.ReadByte()) throw new System.Exception("ReadByte");

                        //Set the position back by 1 using Seek.Begin
                        stream.Position = i;

                        //Ensure the position
                        if (stream.Position != i) throw new System.Exception("Position");

                        //Check the result of Read
                        if (1 != stream.Read(buffer, i, 1) || buffer[i] != randomBytes[i]) throw new System.Exception("Read");

                        //Set the position back by 1 using Current
                        if (i != stream.Seek(-1, System.IO.SeekOrigin.Current)) throw new System.Exception("Seek.Current");

                        //Ensure the position
                        if (stream.Position != i) throw new System.Exception("Position");

                        //Read the byte again
                        if (randomBytes[i] != stream.ReadByte()) throw new System.Exception("ReadByte");

                        //Set the position back by 1 using Current
                        if (i != stream.Seek(-1, System.IO.SeekOrigin.Current)) throw new System.Exception("Seek.Current");

                        //Ensure the position
                        if (stream.Position != i) throw new System.Exception("Position");

                        //Check the result of Read
                        if (1 != stream.Read(buffer, i, 1) || buffer[i] != randomBytes[i]) throw new System.Exception("Read");

                        //Set the position back to i using End (Not working because of 0 based offsets in reverse)
                        if (i != stream.Seek(-(stream.Length - i), System.IO.SeekOrigin.End)) throw new System.Exception("Seek.End");

                        //Ensure the position
                        if (stream.Position != i) throw new System.Exception("Position");

                        //Read the byte again
                        if (randomBytes[i] != stream.ReadByte()) throw new System.Exception("ReadByte");

                        //Set the position back to i using End (Not working because of 0 based offsets in reverse)
                        if (i != stream.Seek(-(stream.Length - i), System.IO.SeekOrigin.End)) throw new System.Exception("Seek.End");

                        //Ensure the position
                        if (stream.Position != i) throw new System.Exception("Position");

                        //Check the result of Read
                        if (1 != stream.Read(buffer, i, 1) || buffer[i] != randomBytes[i]) throw new System.Exception("Read");
                    }

                    //Ensure all bytes read are equal to the initial bytes and in the exact same order
                    if (false == buffer.SequenceEqual(randomBytes)) throw new System.Exception("Not equal");

                    //Test Seeking to random points and reading the byte at that offset and after it, test also seeking backwards by one byte.
                    for (int i = 0; i < stream.Length; ++i)
                    {
                        //Log previous
                        //Console.WriteLine("Previously@: " + stream.Position);

                        //Access a random point
                        int point = (int)Utility.Random.Next(i, (int)(stream.Length - 1));

                        //Test setting the position
                        stream.Position = point;

                        //Log Current
                        //Console.WriteLine("Currently@: " + stream.Position);

                        //Ensure the position is what is expected
                        if (stream.Position != point) throw new System.Exception("Position");

                        //Seek Begin with point < offset may be wrong.

                        //Check for the byte expected
                        if (randomBytes[point] != stream.ReadByte()) throw new System.Exception("ReadByte");

                        //Test moving the position
                        stream.Position++;

                        //Ensure the position is what is expected
                        if (stream.Position != Common.Binary.Min(TestBytesLength, point + 2)) throw new System.Exception("Position");

                        if (point + 2 >= TestBytesLength)
                        {
                            //Check for the byte expected
                            if (-1 != stream.ReadByte()) throw new System.Exception("ReadByte");

                            //Test seeking backwards from this position using begin.
                            point = (int)(stream.Position -= 2);

                            //Check for the byte expected
                            if (randomBytes[point] != stream.ReadByte()) throw new System.Exception("ReadByte");

                            //Check for the byte expected
                            if (randomBytes[point + 1] != stream.ReadByte()) throw new System.Exception("ReadByte");

                            //Test seeking backwards from this position using begin.
                            point = (int)stream.Seek(-2, System.IO.SeekOrigin.Current);

                            //Check for the byte expected
                            if (randomBytes[point] != stream.ReadByte()) throw new System.Exception("ReadByte");

                            //Check for the byte expected
                            if (randomBytes[point + 1] != stream.ReadByte()) throw new System.Exception("ReadByte");
                        }
                        else
                        {
                            //Check for the byte expected
                            if (randomBytes[point + 2] != stream.ReadByte()) throw new System.Exception("ReadByte");

                            //Test seeking backwards from this position
                            stream.Position -= 2;

                            //Ensure the position is what is expected
                            if (stream.Position != point + 1) throw new System.Exception("Position");

                            //Check for the byte expected
                            if (randomBytes[point + 1] != stream.ReadByte()) throw new System.Exception("ReadByte");

                            //Test seeking backwards from this position
                            if (point + 1 != (int)stream.Seek(-1, System.IO.SeekOrigin.Current)) throw new System.Exception("Seek.Current");

                            //Check for the byte expected
                            if (randomBytes[point + 1] != stream.ReadByte()) throw new System.Exception("ReadByte");
                        }
                    }

                    //Todo, test writing into segments at boundaries.
                    //Todo, test SetLength
                    
                    //Close the stream
                    stream.Close();

                    //Ensure Disposed after Close for now...
                    if (false == stream.IsDisposed) throw new System.Exception("IsDisposed");

                    //Dispose is also called
                }

                //Console.WriteLine(segments.Count);
            }
        }

        //TestAppend

        //Test whatever else.

    }
}