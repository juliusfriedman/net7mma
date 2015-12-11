using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Codecs.Audio
{
    //Should be in AudioCodec
    ///// <summary>
    ///// Indicates how individual samples are retrieved from an AudioBuffer in memory.
    ///// </summary>
    //public enum PCMType
    //{
    //    //An unknown type of PCM.
    //    Unknown,
    //    //The most common PCM type.
    //    Linear,
    //    //Rather than representing sample amplitudes on a linear scale as linear PCM coding does, logarithmic PCM coding plots the amplitudes on a logarithmic scale. Log PCM is more often used in telephony and communications applications than in entertainment multimedia applications. (alaw or mulaw)
    //    Logarithmic,
    //    //Values are encoded as differences between the current and the previous value. This reduces the number of bits required per audio sample by about 25% compared to PCM.
    //    Differential,
    //    //The size of the quantization step is varied to allow further reduction of the required bandwidth for a given signal-to-noise ratio.
    //    Adaptive
    //}

    /// <summary>
    /// Defines the logic commonly assoicated with all types of Audio samples.
    /// <see href="http://wiki.multimedia.cx/?title=PCM#Frequency_And_Sample_Rate">The Multimedia Wiki</see>
    /// </summary>
    public class AudioBuffer : Media.Codec.MediaBuffer
    {
        #region Statics

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

        #endregion

        #region Constructor

        /// <summary>
        /// Constructs a new AudioBuffer with the given configuration.
        /// Typically there is enough room to store 1 second of data in the buffer.
        /// </summary>
        /// <param name="byteOrder"></param>
        /// <param name="channels"></param>
        /// <param name="sampleRate"></param>
        /// <param name="bitsPerComponent"></param>
        public AudioBuffer(AudioFormat audioFormat, int numberOfSamples = 1, bool shouldDispose = true)
            : base(audioFormat, CalculateSize(numberOfSamples, audioFormat.Components.Length, audioFormat.SampleRate, audioFormat.Size), null, 0, shouldDispose)
        {
            //Validate the sampleRate given
            if (numberOfSamples <= 0) throw new ArgumentOutOfRangeException("numberOfSamples", "Must be > 0");

            //Validate the sampleRate given
            if (audioFormat.SampleRate <= 0 || false == Common.Binary.IsEven(audioFormat.SampleRate))
                throw new ArgumentOutOfRangeException("sampleRate", "Must be > 0 and an even number");

            //Set the SampleCount from the given value
            //SampleCount = numberOfSamples;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the <see cref="Media.Codecs.Audio.AudioFormat"/> assoicated with the samples within the buffer.
        /// </summary>
        public AudioFormat AudioFormat { get { return MediaFormat as AudioFormat; } }

        /// <summary>
        /// The number of different speakers for which data can be found in the sample.
        /// </summary>
        public int Channels { get { return MediaFormat.Components.Length ; } }

        /// <summary>
        /// Indicates if the sample contains data for only 1 speaker.
        /// </summary>
        public bool Mono { get { return Channels == 1; } }

        /// <summary>
        /// Indicates if the samples contians data for exactly 2 speakers.
        /// </summary>
        public bool Stereo { get { return Channels == 2; } }

        /// <summary>
        /// Indicates if the sample contains data for more than 2 speakers.
        /// </summary>
        public bool MultiChannel { get { return Channels > 2; } }

        /// <summary>
        /// Gets the sample rate
        /// </summary>
        public int SampleRate { get { return AudioFormat.SampleRate; } }

        #endregion
    }
}

namespace Media.UnitTests
{
    /// <summary>
    /// Provides tests which ensure the logic of the supporting class is correct
    /// </summary>
    internal class AudioUnitTests
    {
        public static void Test_AudioFormat_AudioBuffer_Constructor()
        {
            //Construct a new AudioFormat with one component, sampled at 8000hz, all samples are signed 8 bit and in little endian order.
            Media.Codecs.Audio.AudioFormat audioFormat = new Codecs.Audio.AudioFormat(8000, true, Common.Binary.ByteOrder.Little, Media.Codec.DataLayout.Packed, new Media.Codec.MediaComponent[]{
                new Media.Codec.MediaComponent(0, 8)
            });

            //Could be given in place to the constructor.
            using (Media.Codecs.Audio.AudioBuffer audio = new Codecs.Audio.AudioBuffer(audioFormat))
            {
                if (audio.Channels != 1) throw new System.InvalidOperationException();

                //if (audio.SampleCount != 1) throw new System.InvalidOperationException();

                //if (audio.Data.Count != 1000) throw new System.InvalidOperationException();
            }
        }
    }
}