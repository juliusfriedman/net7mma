using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.Rtsp.Server.Streams
{
    /// <summary>
    /// Sends images by encoding them in RFC2435 Jpeg
    /// </summary>
    public class ImageSourceStream : RtpSourceStream
    {
        #region Fields        

        //Should be moved to SourceStream? Should have Fps and calculate for developers?
        protected readonly int clockRate = 9000;

        //Should be moved to SourceStream?
        protected readonly uint sourceId = (uint)DateTime.UtcNow.Ticks;

        //Should be moved to SourceStream?
        protected uint timeStamp = 0;

        //Should be moved to SourceStream?
        protected uint sequenceNumber = 0;

        //Where images are placed to be encoded and sent
        protected Queue<Rtp.JpegFrame> m_Frames = new Queue<Rtp.JpegFrame>();

        //RtpClient so events can be sourced to Clients through RtspServer
        protected Rtp.RtpClient m_RtpClient;

        //Worker for encoding / packetizing
        protected System.Threading.Thread m_Worker;

        //Watches for files if given in constructor
        protected System.IO.FileSystemWatcher m_Watcher;

        //Sdp created dynamically per instance
        protected Sdp.SessionDescription m_Sdp = new Sdp.SessionDescription(1);

        #endregion

        #region Propeties

        public bool Loop { get; set; }

        public override bool Connected { get { return true; } }

        public override bool Listening { get { return true; } }

        public override Sdp.SessionDescription SessionDescription { get { return m_Sdp; } }

        public override Rtp.RtpClient RtpClient { get { return m_RtpClient; } }

        public override bool Ready { get { return m_Frames.Count > 0; } }

        #endregion

        #region Constructor

        public ImageSourceStream(string name, string directory = null, bool watch = true)
            : base(name, new Uri("file://" + System.IO.Path.GetDirectoryName(directory))) 
        {

            directory = base.Source.LocalPath;

            //If we were given a directory and the directory exists then make a FileSystemWatcher
            if (!string.IsNullOrWhiteSpace(directory) && System.IO.Directory.Exists(directory))
            {
                if (watch)
                {
                    m_Watcher = new System.IO.FileSystemWatcher(directory);
                    m_Watcher.EnableRaisingEvents = true;
                    m_Watcher.NotifyFilter = System.IO.NotifyFilters.CreationTime;
                    m_Watcher.Created += m_Watcher_Created;
                }

                foreach (string file in System.IO.Directory.GetFiles(directory, "*.jpg"))
                {
                    try
                    {
                        AddImage(System.Drawing.Image.FromFile(file));
                    }
                    catch
                    {
                        continue;
                    }
                }

            }

            //Add a MediaDescription to our Sdp
            m_Sdp.Add(new Sdp.MediaDescription(Sdp.MediaType.video, 0, MediaProtocol, Rtp.JpegFrame.PayloadType));

            //Add the control line
            m_Sdp.MediaDescriptions[0].Add(new Sdp.SessionDescriptionLine("a=control:trackID=1"));
            
            //Create a RtpClient so events can be fired
            m_RtpClient = Rtp.RtpClient.Sender(System.Net.IPAddress.Any);

            //Add a Interleave
            m_RtpClient.AddInterleave(new Rtp.RtpClient.Interleave(0, 1, sourceId, m_Sdp.MediaDescriptions[0]));
        }

        #endregion

        #region Methods

        //SourceStream Implementation
        public override void Start()
        {
            if (m_Worker != null) return;
            m_Worker = new System.Threading.Thread(Packetize);
            m_Worker.Name = "ImageStream" + Id;
            m_Worker.Start();
        }

        public override void Stop()
        {
            if (m_Worker != null)
            {
                m_Worker.Abort();
                m_Worker = null;
            }
            if (m_Watcher != null)
            {
                m_Watcher.EnableRaisingEvents = false;
                m_Watcher.Created -= m_Watcher_Created;
                m_Watcher.Dispose();
                m_Watcher = null;
            }
        }

        void m_Watcher_Created(object sender, System.IO.FileSystemEventArgs e)
        {
            string path = e.FullPath.ToLowerInvariant();
            if (path.EndsWith("bmp") || path.EndsWith("jpg") || path.EndsWith("jpeg") || path.EndsWith("gif") || path.EndsWith("png"))
            {
                try
                {
                    AddFrame(new Rtp.JpegFrame(System.Drawing.Image.FromFile(path), 100, sourceId, sequenceNumber++, timeStamp));
                }
                catch { }
            }
        }

        public void AddFrame(Rtp.JpegFrame frame)
        {
            lock (m_Frames)
            {
                try
                {
                    m_Frames.Enqueue(frame);
                }
                catch { }
            }
        }

        /// <summary>
        /// Adds an Image to Encode and Send
        /// </summary>
        /// <param name="image">The Image to Encode and Send</param>
        public void AddImage(System.Drawing.Image image)
        {
            lock (m_Frames)
            {
                try
                {
                    m_Frames.Enqueue(new Rtp.JpegFrame(image, 100, sourceId, sequenceNumber++, timeStamp));
                }
                catch { }
            }
        }

        //Move to SourceStream or RtpSourceStream?
        internal virtual void Packetize()
        {
            while (true)
            {
                try
                {

                    if (m_Frames.Count > 0)
                    {
                        Rtp.JpegFrame frame = m_Frames.Dequeue();

                        timeStamp = (uint)(DateTime.UtcNow.Ticks / TimeSpan.TicksPerSecond * clockRate);

                        frame.TimeStamp = timeStamp;

                        foreach (Rtp.RtpPacket packet in frame) RtpClient.OnRtpPacketReceieved(packet);
                            
                        if (Loop) m_Frames.Enqueue(frame);
                    }

                }
                catch { break; }
            }
        }

        #endregion
    }

    public sealed class ChildImageStream : ChildStream
    {
        public ChildImageStream(ImageSourceStream source) : base(source) { }
    }
}
