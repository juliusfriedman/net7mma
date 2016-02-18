namespace Media.Codecs.Video.H264
{
    public enum FramePackingArrangementType
    {
        Checkerboard = 0,
        ColumnBasedInterleaving = 1,
        RowBasedInterleaving = 2,
        SideBySide = 3,
        TopBottom = 4,
        FrameSequential = 5, // a.k.a. temporal interleaving
        WithoutFramePacking = 6,
    }
}
