using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Rtsp.Server.Streams
{
    /// <summary>
    /// Represents the logic necessary to read ISO Complaint Base Media Format Files.
    /// http://en.wikipedia.org/wiki/ISO_base_media_file_format
    /// </summary>
    public class BaseMediaFileSource : FileSourceStream
    {
        #region Statics

        /// <summary>
        /// Given a box string '*' all boxes will be read.
        /// Given a box string './*' all boxes in the current box will be read/
        /// Given a box string '/someBox/anotherBox/*' someBox/anotherBox will be read.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        internal static Container.ContainerElement ReadBox(string path)
        {
            throw new NotImplementedException();
        }

        public static List<string> ParentBoxes = new List<string>()
        {
            "moov",
            "trak",
            "mdia",
            "minf",
            "dinf",
            "stbl",
            //"avc1",
            //"mp4a",
            "edts",
            "stsd",
            "udta"
        };

        //SampleBoxes

        #endregion

        #region Constructor

        public BaseMediaFileSource(string name, Uri source)
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

            //ftyp?

            m_FirstBox = ReadBoxes("*").First();

            if (m_FirstBox == null || !ParentBoxes.Contains(m_FirstBox.FourCC))
            {
                Stop();
                throw new InvalidOperationException("The format is not supported");
            }
        }

        public override Rtp.RtpFrame GetSample(TrackReference track, out TimeSpan duration)
        {
            TrackReference sampleTrack = GetTracks().FirstOrDefault(t => t.MediaType == track.MediaType);

            if (sampleTrack == null) throw new InvalidOperationException("Cannot find a track for given media type.");

            throw new NotImplementedException();
        }

        internal void BuildTrackList()
        {
            foreach (Container.ContainerElement trackBoxes in ReadBoxes("trak/*"))
            {
                //Check for supported codecs because a Packetizer is needed

                TrackReference trackInfo = new TrackReference();

                m_Tracks.Add(trackInfo);
            }
        }

        public override IEnumerable<FileSourceStream.TrackReference> GetTracks()
        {
            if (m_Tracks.Count == 0) BuildTrackList();

            return m_Tracks;
        }

        internal IEnumerable<Container.ContainerElement> ReadBoxes(string path)
        {
            while (base.m_FileStream.Position < base.m_FileStream.Length)
            {
                Container.ContainerElement current = ReadBox(path);

                yield return current;
            }
        }

        #endregion
    }
}
