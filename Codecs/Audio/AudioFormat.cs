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

using System.Linq;

namespace Media.Codecs.Audio
{
    /// <summary>
    /// Represents an AudioFormat which is a specific type of MediaFormat
    /// </summary>
    public class AudioFormat : Codec.MediaFormat
    {
        #region Statics

        public static int CalculateFrameSize(AudioFormat format)
        {
            return format.Size >> 3 * format.Components.Length;
        }

        public int BytesToFrames(AudioFormat format, int bytes)
        {
            return bytes / (format.Components.Length * format.Size >> 3);
        }

        public int FramesToBytes(AudioFormat format, int samples)
        {
            return samples * (format.Components.Length * format.Size >> 3);
        }

        public int BytesToSamples(AudioFormat format, int bytes)
        {
            return bytes / (format.Size >> 3);
        }

        public int SamplesToBytes(AudioFormat format, int samples)
        {
            return samples * (format.Size >> 3);
        }

        public static AudioFormat SingleChannel(AudioFormat other, int componentIndex)
        {
            int otherComponentsLength = other.Components.Length;

            if (otherComponentsLength == 1) return other;

            if (componentIndex > otherComponentsLength) throw new System.ArgumentOutOfRangeException("componentIndex");

            Media.Codec.MediaComponent componentNeeded = other.Components[componentIndex];

            return new AudioFormat(other.SampleRate, other.IsSigned, other.ByteOrder, other.DataLayout, Common.Extensions.Linq.LinqExtensions.Yield(componentNeeded).ToArray());
        }

        public static AudioFormat FilterChannels(AudioFormat other, int componentIndex, int componentCount)
        {
            int otherComponentsLength = other.Components.Length;

            if (otherComponentsLength == 1) return other;

            if (componentIndex > otherComponentsLength || componentCount + componentIndex > otherComponentsLength) throw new System.ArgumentOutOfRangeException("componentIndex");

            return new AudioFormat(other.SampleRate, other.IsSigned, other.ByteOrder, other.DataLayout, System.Linq.Enumerable.Skip(other.Components, componentIndex).Take(componentCount));
        }

        public static AudioFormat Packed(AudioFormat other)
        {
            return new AudioFormat(Codec.MediaFormat.Packed(other));
        }

        public static AudioFormat Planar(AudioFormat other)
        {
            return new AudioFormat(Codec.MediaFormat.Planar(other));
        }

        public static AudioFormat SemiPlanar(AudioFormat other)
        {
            return new AudioFormat(Codec.MediaFormat.SemiPlanar(other));
            //return new ImageFormat(other.ByteOrder, Codec.DataLayout.SemiPlanar, other.Components);
        }

        #endregion

        #region Fields

        /// <summary>
        /// This parameter measures how many samples/channel are played each second. Frequency is measured in samples/second (Hz). 
        /// Common frequency values include 8000, 11025, 16000, 22050, 32000, 44100, and 48000 Hz.
        /// </summary>
        public readonly int SampleRate;

        /// <summary>
        /// Indicates if the samples are signed
        /// </summary>
        public readonly bool IsSigned;

        #endregion

        #region Properties

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
        public AudioFormat(int sampleRate, bool signed, Common.Binary.ByteOrder byteOrder, Codec.DataLayout dataLayout, params Codec.MediaComponent[] components)
            : base(Codec.MediaType.Audio, byteOrder, dataLayout, components)
        {
            SampleRate = sampleRate;

            IsSigned = signed;
        }

        public AudioFormat(int sampleRate, bool signed, Common.Binary.ByteOrder byteOrder, Codec.DataLayout dataLayout, System.Collections.Generic.IEnumerable<Codec.MediaComponent> components)
            : base(Codec.MediaType.Audio, byteOrder, dataLayout, components)
        {
            SampleRate = sampleRate;

            IsSigned = signed;
        }



        /// <summary>
        /// Constructs a new AudioFormat with the given configuration
        /// </summary>
        /// <param name="sampleRate">The sample rate</param>
        /// <param name="sampleSizeInBits">The size in bits of a single sample</param>
        /// <param name="channelCount">The amount of channels</param>
        /// <param name="signed">True if the data is signed, otherwise false</param>
        /// <param name="bigEndian">True to specify <see cref="Common.Binary.ByteOrder.BigEndian"/> or false to specify <see cref="Common.Binary.ByteOrder.Little"/> </param>
        public AudioFormat(int sampleRate, bool signed, bool bigEndian, Codec.DataLayout dataLayout)
            : this(sampleRate, signed, bigEndian ? Common.Binary.ByteOrder.Big : Common.Binary.ByteOrder.Little, dataLayout)
        {

        }

        /// <summary>
        /// Clones an AudioFormat
        /// </summary>
        /// <param name="format">The AudioFormat to clone</param>
        internal protected AudioFormat(AudioFormat format)
            : this(format.SampleRate, format.IsSigned, format.ByteOrder, format.DataLayout)
        {

        }

        /// <summary>
        /// Constructs a new AudioFormat which is a derivative of the given AudioFormat with a different sample rate.
        /// </summary>
        /// <param name="format">The existing AudioFormat</param>
        /// <param name="newSampleRate">The new sampleRate</param>
        public AudioFormat(AudioFormat format, int newSampleRate)
            : this(newSampleRate, format.IsSigned, format.ByteOrder, format.DataLayout)
        {

        }

        /// <summary>
        /// Constructs a new AudioFormat which is a derivative of the given AudioFormat with a different byte order.
        /// </summary>
        /// <param name="format">The existing AudioFormat</param>
        /// <param name="newByteOrder">The new byte order</param>
        public AudioFormat(AudioFormat format, Common.Binary.ByteOrder newByteOrder)
            : this(format.SampleRate, format.IsSigned, format.ByteOrder, format.DataLayout)
        {

        }

        public AudioFormat(Codec.MediaFormat format)
            : base(format)
        {
            if (format == null) throw new System.ArgumentNullException("format");

            if (format.MediaType != Codec.MediaType.Audio) throw new System.ArgumentException("format.MediaType", "Must be Codec.MediaType.Audio.");
        }
       

        #endregion
    }
}
