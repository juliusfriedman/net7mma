using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Rtsp.Server.Streams
{
    /// <summary>
    /// Represents the logic necessary to read files in Matroska RIFF Format (.mkv)
    /// </summary>
    public class MatroskaFileSource : FileSourceStream
    {
        #region Statics

        internal static Container.ContainerElement ReadElements(string path)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Constructor

        public MatroskaFileSource(string name, Uri source)
            : base(name, source) { }

        #endregion

        #region Fields

        Container.ContainerElement m_FirstBox;

        List<TrackReference> m_Tracks = new List<TrackReference>();

        #endregion

        #region Methods

        protected override void Initialize()
        {
            base.Initialize();

            m_FirstBox = ReadElement("*").First();

            if (m_FirstBox == null)
            {
                Stop();
                throw new InvalidOperationException("The format is not supported");
            }
        }

        public override Rtp.RtpFrame GetSample(TrackReference track, out TimeSpan duration)
        {
            TrackReference sampleTrack = GetTracks().FirstOrDefault(t => t.MediaType == track.MediaType);

            if (sampleTrack == null) throw new InvalidOperationException("Cannot find a track for given media type.");

            if (track.Position > sampleTrack.Duration) throw new ArgumentOutOfRangeException("offset");

            throw new NotImplementedException();
        }

        internal void BuildTrackList()
        {
            foreach (Container.ContainerElement trackBoxes in ReadElement("Tracks/*"))
            {
                TrackReference trackInfo = new TrackReference();

                m_Tracks.Add(trackInfo);
            }
        }

        public override IEnumerable<FileSourceStream.TrackReference> GetTracks()
        {
            if (m_Tracks.Count == 0) BuildTrackList();

            return m_Tracks;
        }

        internal IEnumerable<Container.ContainerElement> ReadElement(string path)
        {
            while (m_FileStream.Position < m_FileStream.Length)
            {
                Container.ContainerElement current = ReadElements(path);

                yield return current;
            }
        }

        #endregion
    }
}
