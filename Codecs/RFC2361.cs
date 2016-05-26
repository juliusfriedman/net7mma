namespace Media.Codec
{
    //https://tools.ietf.org/html/rfc2361
    /// <summary>
    /// 
    /// </summary>
    public sealed class RFC2361
    {
        const string TemplateGuidString = "00000000-0000-0010-8000-00AA00389B71";

        public static readonly System.Guid TemplateGuid = new System.Guid(TemplateGuidString);

        internal static readonly byte[] TemplateGuidBytes = TemplateGuid.ToByteArray();

        public static int GetFourCC(System.Guid guid) { return (int)Common.Binary.ReadU32(guid.ToByteArray(), 0, Media.Common.Binary.IsLittleEndian); }

        public static int GetFourCC(byte[] data, ref int offset, bool reverse) { return (int)Common.Binary.ReadU32(data, ref offset, reverse); }

        public static bool IsFourCCGuid(System.Guid guid) { int fourCC; return IsFourCCGuid(guid, out fourCC); }

        public static bool IsFourCCGuid(System.Guid guid, out int fourCC) { return IsFourCCGuid(ref guid, out fourCC); }

        [System.CLSCompliant(false)]
        public static bool IsFourCCGuid(ref System.Guid guid, out int fourCC)
        {
            fourCC = -1;

            byte[] source = guid.ToByteArray();

            //In reverse check for diference where there can be.
            for (int i = 15; i > 4; --i) if (source[i] != TemplateGuidBytes[i]) return false;

            fourCC = (int)Common.Binary.ReadU32(source, 0, Media.Common.Binary.IsLittleEndian);

            return true;
        }

        public static System.Guid ToGuid(int fourCC) { return ToGuid(ref fourCC); }

        [System.CLSCompliant(false)]
        public static System.Guid ToGuid(ref int fourCC)
        {
            byte[] source = TemplateGuid.ToByteArray();
            
            Common.Binary.Write32(source, 0, Media.Common.Binary.IsLittleEndian, fourCC);

            return new System.Guid(source);
        }

        //CodecRegistry...

    }
}

namespace Media.UnitTests
{
    public static class RFC2361UnitTests
    {
        public static void TestToGuid()
        {
            int fourCC;

            System.Guid toTest = Media.Codec.RFC2361.TemplateGuid;

            if (false == Media.Codec.RFC2361.IsFourCCGuid(ref toTest, out fourCC)) throw new System.Exception("Unexpected IsFourCCGuid result");

            if (fourCC != 0) throw new System.Exception("Unexpected output from IsFourCCGuid");

            if (Media.Codec.RFC2361.ToGuid(ref fourCC) != toTest) throw new System.Exception("Unexpected result from ToGuid");
        }
    }
}
