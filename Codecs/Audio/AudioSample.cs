using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Codecs.Audio
{
    public class AudioSample : Media.Codec.Sample
    {
        public readonly int Channels, Rate, BitsPerComponent;

        public AudioSample(int channels, int rate, int bitsPerComponent)
            : base(Media.Codec.MediaType.Audio, new byte[channels * ((rate / bitsPerComponent) + 1)])
        {
            Channels = channels;

            Rate = rate;

            BitsPerComponent = 16;
        }
    }
}
