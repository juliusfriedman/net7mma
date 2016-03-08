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

namespace Media.Codec.Mpeg
{
    /// <summary>
    /// Implementation of MPEG Audio Stream reading based on Audio SyncWord
    /// </summary>
    public class AudioDataTransportStream : ElementaryStream
    {
        #region Reference

        //http://wiki.multimedia.cx/index.php?title=ADTS
        //http://iknowu.duckdns.org/files/public/MP4Maker/MP4Maker.htm

        #endregion

        #region Constants / Statics

        public const ushort SyncWord = 0xFFF;

        #endregion

        #region NestedTypes

        public class AudioDataTransportFrame // :
        {
            // preceeded by syncword - 12 bits - 0xFFF
            public bool ID;
            public byte Layer; // 2 bits
            public bool ProtectionAbsent; // protection_absent;
            public byte ProfileObjectType; // profile_ObjectType - 2 bits
            public byte SamplingFrequencyIndex; // sampling_frequency_index - 4 bits, the escape value is not permitted
            public bool PrivateBit; // private_bit
            public byte ChannelConfiguration; // channel_configuration - 3 bits
            public bool OriginalCopy; // original_copy
            public bool Home; // private_bit

            //[ISO/IEC 14496-3] adts_variable_header
            // MPEG-2 AAC (ISO/IEC 13818-7): "raw_data_block() always contains data representing 1024 output samples".
            // MPEG-4 AAC: It's an acceptable to assume an ADTS frame will have 1024 samples per frame, but 960 is possible too.

            //RawDataBlocks
        }

        //FixedHeader

        //VariableHeader

        #endregion

        #region Methods

        //ReadFrame()

        //GetRawDataBlocks(AudioDataTransportFrame frame)

        #endregion

    }
}
