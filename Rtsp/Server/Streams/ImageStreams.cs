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

        #endregion

        #region Constructor

        public ImageSourceStream(string name, string directory = null, bool watch = true)
            : base(name, new Uri("file://" + System.IO.Path.GetDirectoryName(directory))) 
        {
            if (System.IO.Directory.Exists(base.Source.LocalPath) && watch)
            {
                m_Watcher = new System.IO.FileSystemWatcher(base.Source.LocalPath);
                m_Watcher.EnableRaisingEvents = true;
                m_Watcher.NotifyFilter = System.IO.NotifyFilters.CreationTime;
                m_Watcher.Created += m_Watcher_Created;
            }

            //Add a MediaDescription to our Sdp
            m_Sdp.Add(new Sdp.MediaDescription(Sdp.MediaType.video, 0, MediaProtocol, Rtp.JpegFrame.PayloadType));

            //Add the control line
            m_Sdp.MediaDescriptions[0].Add(new Sdp.SessionDescriptionLine("a=control:trackID=1"));
        }

        #endregion

        #region Methods

        //SourceStream Implementation
        public override void Start()
        {
            if (m_Worker != null) return;

            //Create a RtpClient so events can be fired
            m_RtpClient = Rtp.RtpClient.Sender(System.Net.IPAddress.Any);

            //Add a Interleave (We are not sending Rtcp Packets becaues the Server is doing that) We would use that if we wanted to use this ImageSteam without the server.
            m_RtpClient.AddInterleave(new Rtp.RtpClient.Interleave(0, 1, sourceId, m_Sdp.MediaDescriptions[0]) { RtcpEnabled = false});

            //Ensure never stops sending
            m_RtpClient.InactivityTimeoutSeconds = -1;

            //Makes it faster to send because we already have the frames
            m_RtpClient.m_FrameEventsEnabled = false;

            //We don't need to increment the coutners via an event we can do that (for instance if we didn't need to we might not want to)
            m_RtpClient.m_PacketEventsEnabled = false;

            //Make the thread
            m_Worker = new System.Threading.Thread(Packetize);
            m_Worker.Name = "ImageStream" + Id;
            m_Worker.IsBackground = true;
            m_Worker.Start();

            //If we were given a directory and the directory exists then make a FileSystemWatcher
            if (!string.IsNullOrWhiteSpace(base.Source.LocalPath) && System.IO.Directory.Exists(base.Source.LocalPath))
            {
                System.Threading.ThreadPool.QueueUserWorkItem(o =>
                {
                    foreach (string file in System.IO.Directory.GetFiles(base.Source.LocalPath, "*.jpg"))
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

                    //Only ready after all pictures are in the queue
                    m_Ready = true;
#if DEBUG
                    System.Diagnostics.Debug.WriteLine("ImageStream" + Id + " Started");
#endif
                });
            }
        }

        public override void Stop()
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine("ImageStream" + Id + " Stopped");
#endif

            m_Ready = false;

            if (m_Worker != null)
            {
                try
                {
                    m_Worker.Abort();
                }
                catch { }
                m_Worker = null;
            }

            if (m_Watcher != null)
            {
                m_Watcher.EnableRaisingEvents = false;
                m_Watcher.Created -= m_Watcher_Created;
                m_Watcher.Dispose();
                m_Watcher = null;
            }

            if (m_RtpClient != null)
            {
                m_RtpClient.Disconnect();
                m_RtpClient = null;
            }

            m_Frames.Clear();
        }

        void m_Watcher_Created(object sender, System.IO.FileSystemEventArgs e)
        {
            string path = e.FullPath.ToLowerInvariant();
            if (path.EndsWith("bmp") || path.EndsWith("jpg") || path.EndsWith("jpeg") || path.EndsWith("gif") || path.EndsWith("png"))
            {
                try
                {
                    AddFrame(new Rtp.JpegFrame(System.Drawing.Image.FromFile(path), 100, sourceId, sequenceNumber, timeStamp));
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
                    m_Frames.Enqueue(new Rtp.JpegFrame(image, 100, sourceId,0, 0));
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
                    //Don't need to overload Outgoing Packets we can rest if there are many packets still not sent
                    if (m_Frames.Count > 0)
                    {
                        Rtp.JpegFrame frame = m_Frames.Dequeue();

                        timeStamp = (uint)(DateTime.UtcNow.Ticks / TimeSpan.TicksPerSecond * clockRate);

                        //Get the interleave for the packet
                        Rtp.RtpClient.Interleave interleave = RtpClient.GetInterleaveForPacket(frame.Packets.Last());

                        //Updated values on the Interleave
                        interleave.NtpTimestamp = Utility.DateTimeToNtpTimestamp(DateTime.UtcNow);
                        interleave.RtpTimestamp = frame.TimeStamp;
                        interleave.SequenceNumber = frame.HighestSequenceNumber;

                        //Create a new frame so the timestamps and sequence numbers can change
                        Rtp.JpegFrame next = new Rtp.JpegFrame()
                        {
                            SynchronizationSourceIdentifier = sourceId,
                            TimeStamp = timeStamp
                        };

                        //Iterate each packet and put it into the next frame
                        foreach (Rtp.RtpPacket packet in frame)
                        {
                            packet.Channel = interleave.DataChannel;
                            packet.TimeStamp = timeStamp;
                            packet.SequenceNumber = (int)++sequenceNumber;
                            next.Add(packet);
                            RtpClient.OnRtpPacketReceieved(packet);

                            //If we are keeping track of everything we should increment the counters so the server can send correct Rtcp Reports
                            if (!RtpClient.m_PacketEventsEnabled)
                            {
                                interleave.RtpPacketsSent++;
                                interleave.RtpBytesSent += packet.Length;
                            }
                        }

                        //Keep the Current and LastFrame updated if we disabled events
                        if (!RtpClient.m_PacketEventsEnabled)
                        {
                            //Update frames on Interleave incase there is a UI somewhere showing frames from this source
                            if (interleave.CurrentFrame == null)
                            {
                                interleave.CurrentFrame = frame;
                            }
                            else
                            {
                                interleave.LastFrame = interleave.CurrentFrame;
                                interleave.CurrentFrame = frame;
                            }
                        }
                        
                        //If we are to loop images then add it back at the end
                        if (Loop)
                        {
                            m_Frames.Enqueue(next);
                        }
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
