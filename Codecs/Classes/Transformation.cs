#region Copyright
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
#endregion

namespace Media.Codec
{
    /// <summary>
    /// Represent the base class of any Transformation
    /// </summary>
    public abstract class Transformation : Media.Common.BaseDisposable, Media.Codec.Interfaces.ITransformation
    {

        //#region Nested Types

        //Collides with abstraction, could move to Codec ... 
        //public delegate void Transform(byte[] source, byte[] dest);

        //#endregion

        #region Fields

        /// <summary>
        /// 
        /// </summary>
        public readonly Media.Codec.MediaType MediaType = Media.Codec.MediaType.Unknown;

        /// <summary>
        /// 
        /// </summary>
        public readonly TransformationQuality Quality = TransformationQuality.Unspecified;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructs a Transformation with an Unknown MediaType and Quality
        /// </summary>
        /// <param name="shouldDispose">False if the Transformation should not be allowed to dispose, True by default</param>
        internal protected Transformation(bool shouldDispose)
            : base(shouldDispose)
        {

        }

        /// <summary>
        /// Constructs a new Transformation with the given properties
        /// </summary>
        /// <param name="mediaType">The type of media the tranformation applies to</param>
        /// <param name="quality">The quality of the transformation</param>
        /// <param name="shouldDispose">False if the Transformation should not be allowed to dispose, True by default</param>
        public Transformation(Media.Codec.MediaType mediaType, TransformationQuality quality, bool shouldDispose = true)
            : this(shouldDispose)
        {
            if (mediaType == Media.Codec.MediaType.Unknown) throw new System.ArgumentException("mediaType", "Cannot be Codec.MediaType.Unknown");
            MediaType = mediaType;

            if (quality == TransformationQuality.Unspecified) throw new System.ArgumentException("quality", "Cannot be Codec.TransformationQuality.Unspecified");
            Quality = quality;
        }

        #endregion

        #region Properties

        /// <summary>
        /// 
        /// </summary>
        Media.Codec.MediaType Media.Codec.Interfaces.ITransformation.MediaType
        {
            get { return MediaType; }
        }

        /// <summary>
        /// 
        /// </summary>
        TransformationQuality Media.Codec.Interfaces.ITransformation.Quality
        {
            get { return Quality; }
        }

        #endregion

        #region Methods

        public abstract void Transform();

        #endregion
    }
}