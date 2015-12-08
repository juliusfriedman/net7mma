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

namespace Media.Codecs.Audio
{
    /// <summary>
    /// Represents an AudioFormat
    /// </summary>
    public class AudioFormat : Media.Common.CommonDisposable
    {
        #region Statics

        public static int CalculateFrameSize(AudioFormat format)
        {
            return format.SampleSize >> 3 * format.Channels;
        }

        public int BytesToFrames(AudioFormat format, int bytes)
        {
            return bytes / (format.Channels * format.SampleSize >> 3);
        }

        public int FramesToBytes(AudioFormat format, int samples)
        {
            return samples * (format.Channels * format.SampleSize >> 3);
        }

        public int BytesToSamples(AudioFormat format, int bytes)
        {
            return bytes / (format.SampleSize >> 3);
        }

        public int SamplesToBytes(AudioFormat format, int samples)
        {
            return samples * (format.SampleSize >> 3);
        }

        #endregion

        #region AudioFormats

        //public static readonly AudioFormat Mono_8K_LE = new AudioFormat(8000, 8, 1, false, Common.Binary.ByteOrder.Little);

        //public static readonly AudioFormat Mono16 = new AudioFormat(8000, 16, 1, false, Common.Binary.ByteOrder.Little);

        //public static readonly AudioFormat Mono24 = new AudioFormat(8000, 24, 1, false, Common.Binary.ByteOrder.Little);

        //public static readonly AudioFormat Mono32 = new AudioFormat(8000, 32, 1, false, Common.Binary.ByteOrder.Little);

        #endregion

        #region Fields

        /// <summary>
        /// This parameter measures how many samples/channel are played each second. Frequency is measured in samples/second (Hz). 
        /// Common frequency values include 8000, 11025, 16000, 22050, 32000, 44100, and 48000 Hz.
        /// </summary>
        public readonly int SampleRate;

        /// <summary>
        /// The sample size in bits
        /// </summary>
        public readonly int SampleSize;

        /// <summary>
        /// The amount of channels
        /// </summary>
        public readonly int Channels;

        /// <summary>
        /// Indicates if the samples are signed
        /// </summary>
        public readonly bool IsSigned;

        /// <summary>
        /// Indicates the <see cref="Media.Common.Binary.ByteOrder"/> of the format
        /// </summary>
        public readonly Media.Common.Binary.ByteOrder ByteOrder;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the size in bytes of a single sample.
        /// </summary>
        public int SampleLength { get { return Media.Common.Binary.BitsToBytes(SampleSize); } }

        /// <summary>
        /// Indicates if the sample data is in LittleEndian format.
        /// </summary>
        public bool IsLittleEndian { get { return ByteOrder == Common.Binary.ByteOrder.Little; } }

        /// <summary>
        /// Indicates if the sample data is in BigEndian format.
        /// </summary>
        public bool IsBigEndian { get { return ByteOrder == Common.Binary.ByteOrder.Big; } }

        #region Unused [ReadOnly Fields]

        /// <summary>
        /// Gets the amount of channels.
        /// </summary>
        //public int Channels { get { return m_Channels; } }

        /// <summary>
        /// Gets the size in bits of a single sample.
        /// </summary>
        //public int SampleSize { get { return m_SampleSize; } }

        /// <summary>
        /// Gets the sample rate.
        /// </summary>
        //public int SampleRate { get { return m_SampleRate; } }

        /// <summary>
        /// Gets the <see cref="Media.Common.Binary.ByteOrder"/> of the format.
        /// </summary>
        //public Common.Binary.ByteOrder ByteOrder { get { return m_ByteOrder; } }

        /// <summary>
        /// Indicates if the sample data is signed, useful for determining the mid point.
        /// </summary>
        //public bool IsSigned { get { return m_Signed; } }

        #endregion

        #endregion

        #region Constructor

        /// <summary>
        /// Constructs a new AudioFormat with the given configuration
        /// </summary>
        /// <param name="sampleRate">The sample rate</param>
        /// <param name="sampleSizeInBits">The size in bits of a single sample</param>
        /// <param name="channelCount">The amount of channels</param>
        /// <param name="signed">True if the data is signed, otherwise false</param>
        /// <param name="byteOrder">The <see cref="Common.Binary.ByteOrder"/> of the format</param>
        /// <param name="shouldDispose">Indicates if the instance should be able to be disposed. (True by default)</param>
        public AudioFormat(int sampleRate, int sampleSizeInBits, int channelCount, bool signed, Common.Binary.ByteOrder byteOrder, bool shouldDispose = true)
            :base(shouldDispose)
        {
            if (channelCount < 0) throw new System.ArgumentOutOfRangeException("channelCount", "Must be > 0");
            Channels = channelCount;

            SampleRate = sampleRate;

            SampleSize = sampleSizeInBits;
            
            IsSigned = signed;

            //Validate and assign the byteOrder
            switch (byteOrder)
            {
                case Common.Binary.ByteOrder.All:
                case Common.Binary.ByteOrder.Mixed:
                case Common.Binary.ByteOrder.Unknown:
                    throw new System.InvalidOperationException("byteOrder must not be All, Any, Mixed or Unknown");
                default: ByteOrder = byteOrder; return;
            }
        }

        /// <summary>
        /// Constructs a new AudioFormat with the given configuration
        /// </summary>
        /// <param name="sampleRate">The sample rate</param>
        /// <param name="sampleSizeInBits">The size in bits of a single sample</param>
        /// <param name="channelCount">The amount of channels</param>
        /// <param name="signed">True if the data is signed, otherwise false</param>
        /// <param name="bigEndian">True to specify <see cref="Common.Binary.ByteOrder.BigEndian"/> or false to specify <see cref="Common.Binary.ByteOrder.Little"/> </param>
        public AudioFormat(int sampleRate, int sampleSizeInBits, int channelCount, bool signed, bool bigEndian)
            : this(sampleRate, sampleSizeInBits, channelCount, signed, bigEndian ? Common.Binary.ByteOrder.Big : Common.Binary.ByteOrder.Little)
        {

        }

        /// <summary>
        /// Clones an AudioFormat
        /// </summary>
        /// <param name="format">The AudioFormat to clone</param>
        internal protected AudioFormat(AudioFormat format)
            : this(format.SampleRate, format.SampleSize, format.Channels, format.IsSigned, format.ByteOrder)
        {

        }

        /// <summary>
        /// Constructs a new AudioFormat which is a derivative of the given AudioFormat with a different sample rate.
        /// </summary>
        /// <param name="format">The existing AudioFormat</param>
        /// <param name="newSampleRate">The new sampleRate</param>
        public AudioFormat(AudioFormat format, int newSampleRate)
            : this(newSampleRate, format.SampleSize, format.Channels, format.IsSigned, format.ByteOrder)
        {

        }

        /// <summary>
        /// Constructs a new AudioFormat which is a derivative of the given AudioFormat with a different byte order.
        /// </summary>
        /// <param name="format">The existing AudioFormat</param>
        /// <param name="newByteOrder">The new byte order</param>
        public AudioFormat(AudioFormat format, Common.Binary.ByteOrder newByteOrder)
            : this(format.SampleRate, format.SampleSize, format.Channels, format.IsSigned, format.ByteOrder)
        {

        }

        /// <summary>
        /// Constructs a new AudioFormat which is a derivative of the given AudioFormat with a different sample rate and bits per sample.
        /// </summary>
        /// <param name="format">The existing AudioFormat</param>
        /// <param name="newSampleRate">The new sampleRate</param>
        /// <param name="newSampleSize">The new amount of bits per sample</param>
        public AudioFormat(AudioFormat format, int newSampleRate, int newSampleSize)
            : this(newSampleRate, newSampleSize, format.Channels, format.IsSigned, format.ByteOrder)
        {

        }

        /// <summary>
        /// Constructs a new AudioFormat which is a derivative of the given AudioFormat with a different sample rate, bits per sample and number of channels.
        /// </summary>
        /// <param name="format">The existing AudioFormat</param>
        /// <param name="newSampleRate">The new sampleRate</param>
        /// <param name="newSampleSize">The new amount of bits per sample</param>
        /// <param name="newChannels">The new number of channels</param>
        public AudioFormat(AudioFormat format, int newSampleRate, int newSampleSize, int newChannels)
            : this(newSampleRate, newSampleRate, newChannels, format.IsSigned, format.ByteOrder)
        {
            
        }

        /// <summary>
        /// Constructs a new AudioFormat which is a derivative of the given AudioFormat with a different sample rate, bits per sample and number of channels and byte order.
        /// </summary>
        /// <param name="format">The existing AudioFormat</param>
        /// <param name="newSampleRate">The new sampleRate</param>
        /// <param name="newSampleSize">The new amount of bits per sample</param>
        /// <param name="newChannels">The new number of channels</param>
        /// <param name="newByteOrder">The new byte order</param>
        public AudioFormat(AudioFormat format, int newSampleRate, int newSampleSize, int newChannels, Common.Binary.ByteOrder newByteOrder)
            : this(newSampleRate, newSampleRate, newChannels, format.IsSigned, newByteOrder)
        {

        }

        #endregion
    }
}
