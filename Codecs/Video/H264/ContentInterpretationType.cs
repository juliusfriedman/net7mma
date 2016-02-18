namespace Media.Codecs.Video.H264
{
    public enum ContentInterpretationType : byte
    {
        /// <summary>
        /// Unspecified relationship between the frame packed constituent frames
        /// </summary>
        Unspecified = 0,

        /// <summary>
        /// Indicates that the two constituent frames form the left and right views of a stereo view scene, with
        /// frame 0 being associated with the left view and frame 1 being associated with the right view
        /// </summary>
        StereoViewLeftFirst = 1,

        /// <summary>
        /// Indicates that the two constituent frames form the right and left views of a stereo view scene, with
        /// frame 0 being associated with the right view and frame 1 being associated with the left view
        /// </summary>
        StereoViewRightFirst = 2,
    }
}
