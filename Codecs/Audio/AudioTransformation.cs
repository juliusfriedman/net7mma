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

namespace Media.Codecs.Audio
{
    /// <summary>
    /// Defines the base class of all Audio transformations
    /// </summary>
    public abstract class AudioTransformation : Media.Codec.Transformation
    {
        #region Nested Types

        public delegate void AudioTransform(AudioBuffer source, AudioBuffer dest);

        #endregion

        #region Fields

        protected AudioBuffer m_Source, m_Dest;

        #endregion

        #region Constructor

        /// <summary>
        ///
        /// </summary>
        /// <param name="quality"></param>
        /// <param name="shouldDispose"></param>
        protected AudioTransformation(Codec.TransformationQuality quality = Codec.TransformationQuality.Unspecified, bool shouldDispose = true)
            : base(Codec.MediaType.Audio, quality, shouldDispose)
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="source"></param>
        /// <param name="dest"></param>
        /// <param name="quality"></param>
        /// <param name="shouldDispose"></param>
        public AudioTransformation(AudioBuffer source, AudioBuffer dest, Codec.TransformationQuality quality = Codec.TransformationQuality.Unspecified, bool shouldDispose = true)
            :this(quality, shouldDispose)
        {
            if (Common.IDisposedExtensions.IsNullOrDisposed(source)) throw new System.ArgumentNullException("source");
            m_Source = source;

            if (Common.IDisposedExtensions.IsNullOrDisposed(dest)) throw new System.ArgumentNullException("dest");
            m_Dest = dest;  
        }

        #endregion

        #region Properties

        public AudioBuffer Source
        {
            get { return m_Source; }
            set
            {
                if (Common.IDisposedExtensions.IsNullOrDisposed(value)) throw new System.ArgumentNullException("value");
                m_Source = value;
            }
        }

        public AudioBuffer Destination
        {
            get { return m_Dest; }
            set
            {
                if (Common.IDisposedExtensions.IsNullOrDisposed(value)) throw new System.ArgumentNullException("value");
                m_Dest = value;
            }
        }

        #endregion

        #region Methods

        public override void Dispose()
        {
            m_Source = null;

            m_Dest = null;

            base.Dispose();
        }

        #endregion
    }
}
