using nVideo.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nVideo.Codecs.MPEG12
{
    public class HLSFixPMT
    {

        public void fix(string file)
        {
            FileStream ra = null;

            try
            {
                ra = new FileStream(file, FileMode.Open);
                byte[] tsPkt = new byte[188];

                while (ra.Read(tsPkt, 0, 188) == 188)
                {

                    //Assert.assertEquals(0x47, tsPkt[0] & 0xff);
                    int guidFlags = ((tsPkt[1] & 0xff) << 8) | (tsPkt[2] & 0xff);
                    int guid = (int)guidFlags & 0x1fff;
                    int payloadStart = (guidFlags >> 14) & 0x1;
                    int b0 = tsPkt[3] & 0xff;
                    int counter = b0 & 0xf;
                    int payloadOff = 0;
                    if ((b0 & 0x20) != 0)
                    {
                        payloadOff = (tsPkt[4 + payloadOff] & 0xff) + 1;
                    }
                    if (payloadStart == 1)
                    {
                        payloadOff += (tsPkt[4 + payloadOff] & 0xff) + 1;
                    }

                    if (guid == 0)
                    {
                        if (payloadStart == 0)
                            throw new Exception("PAT spans multiple TS packets, not supported!!!!!!");
                        MemoryStream bb = StreamExtensions.wrap(tsPkt, 4 + payloadOff, 184 - payloadOff);
                        fixPAT(bb);
                        ra.Seek(ra.Position - 188, SeekOrigin.Current);
                        ra.Write(tsPkt, 0, tsPkt.Length);
                    }
                }
            }
            finally
            {
                if (ra != null)
                    ra.Dispose();
            }
        }

        public static void fixPAT(MemoryStream data)
        {
            MemoryStream table = data.duplicate();
            //MTSUtils.parseSection(data);
            MemoryStream newPmt = data.duplicate();

            while (data.remaining() > 4)
            {
                short num = data.getShort();
                short pid = data.getShort();
                if (num != 0)
                {
                    newPmt.putShort(num);
                    newPmt.putShort(pid);
                }
            }
            if (newPmt.position() != data.position())
            {
                // rewrite Section len
                MemoryStream section = table.duplicate();
                section.get();
                int sectionLen = newPmt.position() - table.position() + 1;
                section.putShort((short)((sectionLen & 0xfff) | 0xB000));
                // Redo crc32
                //CRC32 crc32 = new CRC32();
                //table.limit(newPmt.position());
                //crc32.update(NIOUtils.toArray(table));
                //newPmt.putInt((int) crc32.getValue());
                // fill with 0xff
                while (newPmt.hasRemaining())
                    newPmt.put((byte)0xff);
            }
        }

        public static void main(String[] args)
        {
            //if (args.length < 1)
            //    exit("Please specify package location");

            //File hlsPkg = new File(args[0]);

            //if (!hlsPkg.isDirectory())
            //    exit("Not an HLS package, expected a folder");

            //File[] listFiles = hlsPkg.listFiles(new FilenameFilter() {
            //    public bool accept(File dir, String name) {
            //        return name.endsWith(".ts");
            //    }
            //});
            //HLSFixPMT fix = new HLSFixPMT();
            //for (File file : listFiles) {
            //    System.err.println("Processing: " + file.getName());
            //    fix.fix(file);
            //}
        }

        private static void exit(String message)
        {
            //System.err.println("Syntax: hls_fixpmt <hls package location>");
            //System.err.println(message);
            //System.exit(-1);
        }
    }
}
