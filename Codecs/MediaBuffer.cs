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

namespace Media.Codec
{
    /// <summary>
    /// Represents a base class for common types of media
    /// </summary>
    public class MediaBuffer : Common.CommonDisposable, Media.Codec.Interfaces.IMediaBuffer
    {
        //What codec is this sample relevent to
        public readonly Media.Codec.Interfaces.ICodec Codec;

        //Already have base.Created
        public readonly long Timestamp;

        //Needs to come from format.
        public readonly MediaFormat MediaFormat;

        //

        public readonly Common.MemorySegment Data = Common.MemorySegment.Empty;

        public MediaBuffer(MediaFormat mediaFormat, Common.MemorySegment data, Media.Codec.Interfaces.ICodec codec = null, long timestamp = 0, bool shouldDispose = true)
            :base(shouldDispose)
        {
            MediaFormat = mediaFormat;

            Codec = codec;

            Data = data;

            if (Data.Count < SampleLength) throw new System.InvalidOperationException(string.Format("Insufficient Data for Sample, found: {0}, expected: {1}", data.Count, SampleLength));

            Timestamp = timestamp;

            //SampleCount = 1;
        }

        public MediaBuffer(MediaFormat mediaFormat, int size, Media.Codec.Interfaces.ICodec codec = null, long timestamp = 0, bool shouldDispose = true)
            :this(mediaFormat, new Common.MemorySegment(size), codec, timestamp, shouldDispose)
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
        public int SampleSize { get { return MediaFormat.Size; } }

        /// <summary>
        /// Indicates the amount of Bytes used by a sample sample in the buffer.
        /// </summary>
        public int SampleLength { get { return Common.Binary.BitsToBytes(SampleSize); } }

        public Common.Binary.ByteOrder ByteOrder { get { return MediaFormat.ByteOrder; } }

        public DataLayout DataLayout { get { return MediaFormat.DataLayout; } }

        /// <summary>
        /// Indicates the amount of samples contained in the buffer.
        /// Typically there is only 1 sample for Images, and more for Audio or Video.
        /// The size of each sample is retrieved from <see cref="SampleSize"/>
        /// </summary>
        //public virtual int SampleCount { get; set; }

        public virtual int SampleCount { get { return Data.Count / SampleLength; } }

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

namespace Media.UnitTests
{
    /// <summary>
    /// Provides tests which ensure the logic of the supporting class is correct
    /// </summary>
    internal class MediaBufferUnitTests
    {
        public static void Test_Constructor()
        {
            try
            {
                //Make a media buffer with all supported layouts and byte orders and sample sizes...
                using (Media.Codec.MediaBuffer mb = new Media.Codec.MediaBuffer(new Codec.MediaFormat(Codec.MediaType.Unknown, Common.Binary.ByteOrder.Unknown, Codec.DataLayout.Unknown, null), null))
                {

                }
            }
            catch
            {
                //Probably expected since you would need to know the type of buffer...
            }
        }
    }
}