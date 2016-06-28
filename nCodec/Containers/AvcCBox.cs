using nVideo.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace nVideo.Codecs.H264
{
    public class AvcCBox : Box {

    private int profile;
    private int profileCompat;
    private int level;
    private int nalLengthSize;

    private List<MemoryStream> spsList = new List<MemoryStream>();
    private List<MemoryStream> ppsList = new List<MemoryStream>();

    public AvcCBox(Box other) : base(other) {
        
    }

    public AvcCBox() : base(new Header(fourcc()))
    {
    }

    public AvcCBox(Header header) : base(header) { }

    public AvcCBox(int profile, int profileCompat, int level, List<MemoryStream> spsList, List<MemoryStream> ppsList) : this() {
        
        this.profile = profile;
        this.profileCompat = profileCompat;
        this.level = level;
        this.spsList = spsList;
        this.ppsList = ppsList;
    }

    public static String fourcc() {
        return "avcC";
    }

    public override void parse(MemoryStream input) {
        StreamExtensions.skip(input, 1);
        profile = input.get() & 0xff;
        profileCompat = input.get() & 0xff;
        level = input.get() & 0xff;
        int flags = input.get() & 0xff;
        nalLengthSize = (flags & 0x03) + 1;

        int nSPS = input.get() & 0x1f; // 3 bits reserved + 5 bits number of
                                       // sps
        for (int i = 0; i < nSPS; i++) {
            int spsSize = input.getShort();
            //Assert.assertEquals(0x27, input.get() & 0x3f);
            spsList.Add(StreamExtensions.read(input, spsSize - 1));
        }

        int nPPS = input.get() & 0xff;
        for (int i = 0; i < nPPS; i++) {
            int ppsSize = input.getShort();
            //Assert.assertEquals(0x28, input.get() & 0x3f);
            ppsList.Add(StreamExtensions.read(input, ppsSize - 1));
        }
    }

    protected override void doWrite(MemoryStream outb) {

        outb.put((byte) 0x1); // version
        outb.put((byte) profile);
        outb.put((byte) profileCompat);
        outb.put((byte) level);
        outb.put((byte) 0xff);

        outb.put((byte) (spsList.Count() | 0xe0));
        foreach (var sps in spsList) {
            outb.putShort((short) (sps.remaining() + 1));
            outb.put((byte) 0x67);
            StreamExtensions.write(outb, sps);
        }

        outb.put((byte) ppsList.Count());
        foreach (var pps in ppsList) {
            outb.putShort((byte) (pps.remaining() + 1));
            outb.put((byte) 0x68);
            StreamExtensions.write(outb, pps);
        }
    }

    public int getProfile() {
        return profile;
    }

    public int getProfileCompat() {
        return profileCompat;
    }

    public int getLevel() {
        return level;
    }

    public List<MemoryStream> getSpsList() {
        return spsList;
    }

    public List<MemoryStream> getPpsList() {
        return ppsList;
    }

    public int getNalLengthSize() {
        return nalLengthSize;
    }
}
}
