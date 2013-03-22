using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.Rtsp.Server.Streams
{

    //Todo Seperate ImageStream from JpegRtpImageSource

    //public class ImageStream : SourceStream
    //{
    //    public Rtp.RtpFrame CreateFrames() { }
    //    public virtual void Encoode() { }
    //    public virtual void Decode() { }
    //}

    /// <summary>
    /// Sends System.Drawing.Images over Rtp by encoding them as a RFC2435 Jpeg
    /// </summary>
    public class JpegRtpImageSource : RtpSource
    {
        #region Fields

        //Should be moved to SourceStream? Should have Fps and calculate for developers?
        protected readonly int clockRate = 9000;

        //Should be moved to SourceStream?
        protected readonly uint sourceId = (uint)DateTime.UtcNow.Ticks;

        //Where images are placed to be encoded and sent (should have a Dictionary<string, Queue<Image>> if more then one track should be supported.)
        //Or MediaDescription, Queue<Image>
        //trackId=1, Queue<Image>,
        //trackId=2, Queue<Image>...
        protected Queue<Rtp.JpegFrame> m_Frames = new Queue<Rtp.JpegFrame>();

        //RtpClient so events can be sourced to Clients through RtspServer
        protected Rtp.RtpClient m_RtpClient;

        //Worker for encoding / packetizing
        protected System.Threading.Thread m_Worker;

        //Watches for files if given in constructor
        protected System.IO.FileSystemWatcher m_Watcher;

        #endregion

        #region Propeties

        /// <summary>
        /// Indicates if the Stream should continue from the beginning once reaching the end
        /// </summary>
        public bool Loop { get; set; }

        /// <summary>
        /// Implementes the SessionDescription property for RtpSourceStream
        /// </summary>
        public override Rtp.RtpClient RtpClient { get { return m_RtpClient; } }

        #endregion

        #region Constructor

        public JpegRtpImageSource(string name, string directory = null, bool watch = true)
            : base(name, new Uri("file://" + System.IO.Path.GetDirectoryName(directory)))
        {
            //If we were told to watch and given a directory and the directory exists then make a FileSystemWatcher
            if (System.IO.Directory.Exists(base.Source.LocalPath) && watch)
            {
                m_Watcher = new System.IO.FileSystemWatcher(base.Source.LocalPath);
                m_Watcher.EnableRaisingEvents = true;
                m_Watcher.NotifyFilter = System.IO.NotifyFilters.CreationTime;
                m_Watcher.Created += FileCreated;
            }            
        }

        #endregion

        #region Methods

        //SourceStream Implementation
        public override void Start()
        {
            if (m_Worker != null) return;

            //Create a RtpClient so events can be sourced from the Server to many clients without this Client knowing about all participants
            //If this class was used to send directly to one person it would be setup with the recievers address
            m_RtpClient = Rtp.RtpClient.Sender(System.Net.IPAddress.Any);

            //Add a MediaDescription to our Sdp on any available port for RTP/AVP Transport using the RtpJpegPayloadType
            SessionDescription.Add(new Sdp.MediaDescription(Sdp.MediaType.video, 0, RtpSource.RtpMediaProtocol, Rtp.JpegFrame.RtpJpegPayloadType));

            //Add a Interleave (We are not sending Rtcp Packets becaues the Server is doing that) We would use that if we wanted to use this ImageSteam without the server.            
            //See the notes about having a Dictionary to support various tracks
            m_RtpClient.AddTransportContext(new Rtp.RtpClient.TransportContext(0, 1, sourceId, SessionDescription.MediaDescriptions[0], false));

            //Add the control line
            SessionDescription.MediaDescriptions[0].Add(new Sdp.SessionDescriptionLine("a=control:trackID=1"));

            //Ensure never stops sending
            m_RtpClient.InactivityTimeoutSeconds = -1;

            //We don't need to increment the counters or keep track of frames via an event we can do that (Makes it a little faster to send since you are not waiting for events to increment counters)
            m_RtpClient.m_OutgoingPacketEventsEnabled = false;

            //Make the thread
            m_Worker = new System.Threading.Thread(SendPackets);
            m_Worker.Name = "ImageStream" + Id;

            //If we are watching and there are already files in the directory then add them to the Queue
            if (m_Watcher != null && !string.IsNullOrWhiteSpace(base.Source.LocalPath) && System.IO.Directory.Exists(base.Source.LocalPath))
            {
                System.Threading.ThreadPool.QueueUserWorkItem(o =>
                {
                    foreach (string file in System.IO.Directory.GetFiles(base.Source.LocalPath, "*.jpg"))
                    {
                        try
                        {
#if DEBUG
                            System.Diagnostics.Debug.WriteLine("ImageStream" + Id + " Encoding: " + file);
#endif
                            //Packetize the Image adding the resulting Frame to the Queue (Encoded implicitly with operator)
                            Packetize(System.Drawing.Image.FromFile(file));
#if DEBUG
                            System.Diagnostics.Debug.WriteLine("ImageStream" + Id + " Done Encoding: " + file);
#endif
                        }
                        catch
                        {
                            continue;
                        }
                    }
                    
                    //If we have not been stopped already
                    if (m_Worker != null)
                    {
                        //Only ready after all pictures are in the queue
                        Ready = true;
                        m_Worker.Start();
                    }
#if DEBUG
                    System.Diagnostics.Debug.WriteLine("ImageStream" + Id + " Started");
#endif
                });
            }
            else
            {
                //We are ready
                Ready = true;
                m_Worker.Start();
            }
            base.Start();
        }

        public override void Stop()
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine("ImageStream" + Id + " Stopped");
#endif

            Ready = false;

            if (m_Worker != null)
            {
                try { m_Worker.Abort(); }
                catch { }
                m_Worker = null;
            }

            if (m_Watcher != null)
            {
                m_Watcher.EnableRaisingEvents = false;
                m_Watcher.Created -= FileCreated;
                m_Watcher.Dispose();
                m_Watcher = null;
            }

            if (m_RtpClient != null)
            {
                m_RtpClient.Disconnect();
                m_RtpClient = null;
            }

            m_Frames.Clear();

            base.Stop();
        }

        /// <summary>
        /// Called to add a file to the Queue when it was created in the watched directory if the file was an Image.
        /// </summary>
        /// <param name="sender">The object who called this method</param>
        /// <param name="e">The FileSystemEventArgs which correspond to the file created</param>
        internal virtual void FileCreated(object sender, System.IO.FileSystemEventArgs e)
        {
            string path = e.FullPath.ToLowerInvariant();
            if (path.EndsWith("bmp") || path.EndsWith("jpg") || path.EndsWith("jpeg") || path.EndsWith("gif") || path.EndsWith("png"))
            {
                try { Packetize(System.Drawing.Image.FromFile(path)); }
                catch { }
            }
        }

        /// <summary>
        /// Add a frame of existing packetized data
        /// </summary>
        /// <param name="frame">The frame with packets to send</param>
        public void AddFrame(Rtp.JpegFrame frame)
        {
            lock (m_Frames)
            {
                try { m_Frames.Enqueue(frame); }
                catch { /**/ }
            }
        }

        /// <summary>
        /// Packetize's an Image for Sending
        /// </summary>
        /// <param name="image">The Image to Encode and Send</param>
        public void Packetize(System.Drawing.Image image)
        {
            lock (m_Frames)
            {
                try { m_Frames.Enqueue(new Rtp.JpegFrame(image, 100, sourceId, 0, 0)); }
                catch { /**/ }
            }
        }

        //Needs to only send packets and not worry about updating the frame, that should be done by ImageSource

        internal virtual void SendPackets()
        {
            while (State == StreamState.Started)
            {

                if (m_RtpClient.m_OutgoingRtpPackets.Count > 60)
                {
                    System.Threading.Thread.Sleep(m_Frames.Count);
                    continue;
                };

                try
                {
                    //Dequeue a frame or die
                    Rtp.JpegFrame frame = m_Frames.Dequeue();

                    //Get the transportChannel for the packet
                    Rtp.RtpClient.TransportContext transportContext = RtpClient.GetContextBySourceId(frame.SynchronizationSourceIdentifier);

                    DateTime now = DateTime.UtcNow;

                    //Updated values on the transportChannel
                    transportContext.NtpTimestamp = Utility.DateTimeToNptTimestamp(now);
                    transportContext.RtpTimestamp = (uint)(now.Ticks / TimeSpan.TicksPerSecond * clockRate);

                    //Keep the Current and LastFrame updated if we disabled events
                    if (!RtpClient.m_IncomingPacketEventsEnabled)
                    {
                        //Update frames on transportContext incase there is a UI somewhere showing frames from this source
                        if (transportContext.CurrentFrame == null)
                        {
                            transportContext.CurrentFrame = frame;
                        }
                        else
                        {
                            transportContext.LastFrame = transportContext.CurrentFrame;
                            transportContext.CurrentFrame = frame;
                        }
                        //We should also raise an event to let the UI know
                        //Normally this would be fired for us when the marker was seen through raising OnRtpPacketReceieved
                        //But as a Sender we have disabled this event
                        RtpClient.OnRtpFrameChanged(transportContext.CurrentFrame);
                    }

                    //Iterate each packet and put it into the next frame
                    foreach (Rtp.RtpPacket packet in frame)
                    {
                        //Copy the values before we signal the server
                        packet.Channel = transportContext.DataChannel;
                        packet.SynchronizationSourceIdentifier = sourceId;
                        packet.TimeStamp = transportContext.RtpTimestamp;
                        //Increment the sequence number on the transportChannel and assign the result to the packet
                        packet.SequenceNumber = ++transportContext.SequenceNumber;
                        
                        //Fire an event so the server sends a packet to all clients connected to this source
                        RtpClient.OnRtpPacketReceieved(packet);

                        //If we are keeping track of everything we should increment the counters so the server can send correct Rtcp Reports
                        if (!RtpClient.m_OutgoingPacketEventsEnabled)
                        {
                            transportContext.RtpPacketsSent++;
                            transportContext.RtpBytesSent += packet.Length;
                        }
                    }

                    //If we are to loop images then add it back at the end
                    if (Loop)
                    {
                        m_Frames.Enqueue(frame);
                    }

                }
                catch { break; }
            }
        }

        #endregion
    }

    //public sealed class ChildRtpImageSource : ChildStream
    //{
    //    public ChildRtpImageSource(RtpImageSource source) : base(source) { }
    //}
}