using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Codec
{
    public class MediaBuffer : Common.CommonDisposable, Media.Codec.Interfaces.IMediaBuffer
    {
        //What codec is this sample relevent to
        public readonly Media.Codec.Interfaces.ICodec Codec;

        public readonly MediaType MediaType;

        public readonly Common.Binary.ByteOrder ByteOrder;        

        //Already have base.Created
        public readonly long Timestamp;

        public readonly int BitsPerComponent;

        public readonly int ComponentCount;

        public readonly Common.MemorySegment Data = Common.MemorySegment.Empty;

        //public Common.MemorySegment Data { get; internal protected set; }

        public MediaBuffer(MediaType type, Common.MemorySegment data, Common.Binary.ByteOrder byteOrder, int bitsPerComponent, int componentCount, 
            long timestamp = 0, Media.Codec.Interfaces.ICodec codec = null, bool shouldDispose = true)
            :base(shouldDispose)
        {
            ComponentCount = componentCount;

            BitsPerComponent = bitsPerComponent;

            Codec = codec;

            MediaType = type;

            Data = data;

            ByteOrder = byteOrder;

            Timestamp = timestamp;

            SampleCount = 1;
        }

        public MediaBuffer(MediaType type, int size, Common.Binary.ByteOrder byteOrder, int bitsPerComponent, int componentCount,
            Media.Codec.Interfaces.ICodec codec = null, long timestamp = 0, bool shouldDispose = true)
            : this(type, new Common.MemorySegment(size), byteOrder, bitsPerComponent, componentCount, timestamp, codec, shouldDispose)
        {

        }

        //public override void Dispose()
        //{
        //    base.Dispose();

        //    if (Data != null)
        //    {
        //        Data.Dispose();

        //        Data = null;
        //    }
        //}

        ///// <summary>
        ///// Adds the given data to the sample.
        ///// </summary>
        ///// <param name="data"></param>
        ///// <param name="offset"></param>
        ///// <param name="length"></param>
        //public void AddSample(byte[] data, int offset, int length)
        //{
        //    Common.MemorySegment newData = new Common.MemorySegment(Data.Count + length), oldData = Data;

        //    oldData.Array.CopyTo(newData.Array, 0);

        //    Data = newData;

        //    System.Array.Copy(data, offset, Data.Array, oldData.Count, length);

        //    oldData.Dispose();

        //    oldData = null;
        //}

        ///// <summary>
        ///// Ensures the buffer has the given capacity.
        ///// </summary>
        ///// <param name="capacity"></param>
        //public void EnsureCapacity(int capacity)
        //{
        //    if (capacity > Data.Count)
        //    {
        //        Common.MemorySegment newData = new Common.MemorySegment(capacity), oldData = Data;
                
        //        oldData.Array.CopyTo(newData.Array, 0);
                
        //        Data = newData;
                
        //        oldData.Dispose();
                
        //        oldData = null;
        //    }
        //}

        /// <summary>
        /// Indicates the amount of Bits used by a single sample in the buffer.
        /// </summary>
        public virtual int SampleSize { get { return BitsPerComponent * ComponentCount; } }

        /// <summary>
        /// Indicates the amount of Bytes used by a sample sample in the buffer.
        /// </summary>
        public int SampleLength { get { return Common.Binary.BitsToBytes(SampleSize); } }

        /// <summary>
        /// Indicates the amount of samples contained in the buffer.
        /// Typically there is only 1 sample for Images, and more for Audio or Video.
        /// The size of each sample is retrieved from <see cref="SampleSize"/>
        /// </summary>
        public virtual int SampleCount { get; set; }

        Media.Codec.Interfaces.ICodec Media.Codec.Interfaces.IMediaBuffer.Codec
        {
            get { return Codec; }
        }


        Common.MemorySegment Interfaces.IMediaBuffer.Data
        {
            get { return Data; }
        }
    }
}
