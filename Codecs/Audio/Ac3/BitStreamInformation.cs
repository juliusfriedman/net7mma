using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Codecs.Audio.Ac3
{
    public class BitStreamInformation
    {
        public static int GetNumberOfChannels(byte acMod, bool lfeOn)
        {
            int result = GetNumberOfFullRangeChannels(acMod);
            return lfeOn ? ++result : result;
        }

        /// <summary>
        /// Return number of channels excluding LFE
        /// </summary>
        public static int GetNumberOfFullRangeChannels(byte acMod)
        {
            switch (acMod)
            {
                case 0:
                    return 2;
                case 1:
                    return 1;
                case 2:
                    return 2;
                case 3:
                    return 3;
                case 4:
                    return 3;
                case 5:
                    return 4;
                case 6:
                    return 4;
                case 7:
                    return 5;
                default:
                    throw new ArgumentException("Invalid audio coding mode");
            }
        }

    }
}
