using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Codecs.Video.Vp8
{

    //http://www.programcreek.com/java-api-examples/index.php?source_dir=javaforce-master/projects/jphonelite-android/src/javaforce/voip/DTMF.java

    class RFC7741Frame
    {
        void Depacketize()
        {
            //Needs 2 bytes of payload data.. (including padding octets)

            byte[] rtp= new byte[14];

            int payloadOffset = 12;

            int vp8Length = 1;

            byte x = 12;//rtp @ headerOctets

            if ((x & 0x80) == 0x80)
            {
                byte ilt = rtp[13];  //I L T RSV-A 

                payloadOffset++;

                vp8Length--;

                if ((ilt & 0x80) == 0x80)
                {  //picture ID 
                    payloadOffset++;
                    vp8Length--;
                }

                if ((ilt & 0x40) == 0x40)
                {  //TL0PICIDX 
                    payloadOffset++;
                    vp8Length--;
                }

                if ((ilt & 0x20) == 0x20)
                {  //TID RSV-B 
                    payloadOffset++;
                    vp8Length--;
                }
            } 
        }

        public void Packetize()
        {
            //http://www.programcreek.com/java-api-examples/index.php?source_dir=javaforce-master/projects/jphonelite-android/src/javaforce/voip/RTPVP8.java
        }

    }
}
