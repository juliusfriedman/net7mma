using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Codecs.Audio
{
    /// <summary>
    /// Indicates how individual samples are retrieved from an AudioBuffer in memory.
    /// </summary>
    public enum Packing
    {
        //An unknown type of PCM.
        Unknown,
        //The most common PCM type.
        Linear,
        //Rather than representing sample amplitudes on a linear scale as linear PCM coding does, logarithmic PCM coding plots the amplitudes on a logarithmic scale. Log PCM is more often used in telephony and communications applications than in entertainment multimedia applications. (alaw or mulaw)
        Logarithmic,
        //Values are encoded as differences between the current and the previous value. This reduces the number of bits required per audio sample by about 25% compared to PCM.
        Differential,
        //The size of the quantization step is varied to allow further reduction of the required bandwidth for a given signal-to-noise ratio.
        Adaptive
    }

    /// <summary>
    /// Defines the logic commonly assoicated with all types of Audio samples.
    /// <see href="http://wiki.multimedia.cx/?title=PCM#Frequency_And_Sample_Rate">The Multimedia Wiki</see>
    /// </summary>
    public class AudioBuffer : Media.Codec.MediaBuffer
    {

        /// <summary>
        /// Calulcates the size in bytes required to store data of the given configuration
        /// </summary>
        /// <param name="numberOfSamples"></param>
        /// <param name="channels">The number of channels</param>
        /// <param name="sampleRate">The rate at which the audio will be played each second (hZ)</param>
        /// <param name="bitsPerComponent">The amount of bits </param>
        /// <returns>The amount of bytes required.</returns>
        static int CalculateSize(int numberOfSamples, int channels, int sampleRate, int bitsPerComponent)
        {
            return numberOfSamples * (sampleRate / (Common.Binary.BitsToBytes(bitsPerComponent) * channels));
        }

        /// <summary>
        /// This parameter measures how many samples/channel are played each second. Frequency is measured in samples/second (Hz). 
        /// Common frequency values include 8000, 11025, 16000, 22050, 32000, 44100, and 48000 Hz.
        /// </summary>
        public readonly int SampleRate;

        /// <summary>
        /// Indicates how the audio data is accessed from Data.
        /// </summary>
        public readonly Packing Packing;

        /// <summary>
        /// Constructs a new
        /// </summary>
        /// <param name="byteOrder"></param>
        /// <param name="channels"></param>
        /// <param name="sampleRate"></param>
        /// <param name="bitsPerComponent"></param>
        public AudioBuffer(Packing packing, Common.Binary.ByteOrder byteOrder, int channels, int sampleRate, int bitsPerComponent, int numberOfSamples = 1)
            : base(Media.Codec.MediaType.Audio, CalculateSize(numberOfSamples, channels, sampleRate, bitsPerComponent), byteOrder, bitsPerComponent, channels)
        {
            //Validate the sampleRate given
            if (numberOfSamples <= 0) throw new ArgumentOutOfRangeException("numberOfSamples", "Must be > 0");

            //Set the SampleCount from the given value
            SampleCount = numberOfSamples;

            //Validate the sampleRate given
            if (sampleRate <= 0 || false == Common.Binary.IsEven(sampleRate))
                throw new ArgumentOutOfRangeException("sampleRate", "Must be > 0 and an even number");

            //Set the SampleRate from the given value
            SampleRate = sampleRate;

            //Set the Packing from the given value
            Packing = packing;
        }

        /// <summary>
        /// The number of different speakers for which data can be found in the sample.
        /// </summary>
        public int Channels { get { return ComponentCount; } }

        /// <summary>
        /// Indicates if the sample contains data for only 1 speaker.
        /// </summary>
        public bool Mono { get { return ComponentCount == 1; } }

        /// <summary>
        /// Indicates if the samples contians data for exactly 2 speakers.
        /// </summary>
        public bool Stereo { get { return ComponentCount == 2; } }

        /// <summary>
        /// Indicates if the sample contains data for more than 2 speakers.
        /// </summary>
        public bool MultiChannel { get { return ComponentCount > 2; } }
    }
}
