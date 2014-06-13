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
    public class RFC2435Stream : RtpSource
    {
        #region Fields

        //Should be moved to SourceStream? Should have Fps and calculate for developers?
        protected int clockRate = 9;//kHz //90 dekahertz

        //Should be moved to SourceStream?
        protected readonly int sourceId = (int)DateTime.UtcNow.Ticks;

        protected Queue<Rtp.RtpFrame> m_Frames = new Queue<Rtp.RtpFrame>();

        //RtpClient so events can be sourced to Clients through RtspServer
        protected Rtp.RtpClient m_RtpClient;

        //Watches for files if given in constructor
        protected System.IO.FileSystemWatcher m_Watcher;

        protected int m_FramesPerSecondCounter = 0;

        #endregion

        #region Propeties

        public virtual double FramesPerSecond { get { return Math.Max(m_FramesPerSecondCounter, 1) / Math.Abs(Uptime.TotalSeconds); } }

        /// <summary>
        /// Indicates if the Stream should continue from the beginning once reaching the end
        /// </summary>
        public virtual bool Loop { get; set; }

        /// <summary>
        /// Implementes the SessionDescription property for RtpSourceStream
        /// </summary>
        public override Rtp.RtpClient RtpClient { get { return m_RtpClient; } }

        #endregion

        #region Constructor

        public RFC2435Stream(string name, string directory = null, bool watch = true)
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
            if (m_RtpClient != null) return;

            //Create a RtpClient so events can be sourced from the Server to many clients without this Client knowing about all participants
            //If this class was used to send directly to one person it would be setup with the recievers address
            m_RtpClient = Rtp.RtpClient.Sender(System.Net.IPAddress.Any);

            SessionDescription = new Sdp.SessionDescription(1, "v√ƒ", Name );
            SessionDescription.Add(new Sdp.Lines.SessionConnectionLine()
            {
                NetworkType = "IN",
                AddressType = "*",
                Address = "0.0.0.0"
            });

            //Add a MediaDescription to our Sdp on any available port for RTP/AVP Transport using the RtpJpegPayloadType            
            SessionDescription.Add(new Sdp.MediaDescription(Sdp.MediaType.video, 0, RtpSource.RtpMediaProtocol, Rtp.RFC2435Frame.RtpJpegPayloadType));

            //Add a Interleave (We are not sending Rtcp Packets becaues the Server is doing that) We would use that if we wanted to use this ImageSteam without the server.            
            //See the notes about having a Dictionary to support various tracks
            m_RtpClient.AddTransportContext(new Rtp.RtpClient.TransportContext(0, 1, sourceId, SessionDescription.MediaDescriptions[0], false, 0));

            //Add the control line
            SessionDescription.MediaDescriptions[0].Add(new Sdp.SessionDescriptionLine("a=control:trackID=1"));

            //Add the line with the clock rate in ms, obtained by TimeSpan.TicksPerMillisecond * clockRate            

            //Make the thread
            m_RtpClient.m_WorkerThread = new System.Threading.Thread(SendPackets);
            m_RtpClient.m_WorkerThread.TrySetApartmentState(System.Threading.ApartmentState.MTA);
            m_RtpClient.m_WorkerThread.IsBackground = true;
            m_RtpClient.m_WorkerThread.Priority = System.Threading.ThreadPriority.BelowNormal;
            m_RtpClient.m_WorkerThread.Name = "RFC2435Stream-" + Id;

            //If we are watching and there are already files in the directory then add them to the Queue
            if (m_Watcher != null && !string.IsNullOrWhiteSpace(base.Source.LocalPath) && System.IO.Directory.Exists(base.Source.LocalPath))
            {
                foreach (string file in System.IO.Directory.GetFiles(base.Source.LocalPath, "*.jpg").AsParallel())
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
                    catch (Exception ex)
                    {
#if DEBUG
                        System.Diagnostics.Debug.WriteLine("ImageStream" + Id + " Exception: " + ex);
#endif
                        continue;
                    }
                }

                //If we have not been stopped already
                if (m_RtpClient.m_WorkerThread != null)
                {
                    //Only ready after all pictures are in the queue
                    Ready = true;
                    m_RtpClient.m_WorkerThread.Start();
                }
#if DEBUG
                System.Diagnostics.Debug.WriteLine("ImageStream" + Id + " Started");
#endif
            }
            else
            {
                //We are ready
                Ready = true;
                m_RtpClient.m_WorkerThread.Start();
            }
            base.Start();
        }

        public override void Stop()
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine("ImageStream" + Id + " Stopped");
#endif

            Ready = false;

            Utility.Abort(ref m_RtpClient.m_WorkerThread);

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

            SessionDescription = null;

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
                catch { throw; }
            }
        }

        /// <summary>
        /// Add a frame of existing packetized data
        /// </summary>
        /// <param name="frame">The frame with packets to send</param>
        public void AddFrame(Rtp.RtpFrame frame)
        {
            lock (m_Frames)
            {
                try { m_Frames.Enqueue(frame); }
                catch { throw; }
            }
        }

        /// <summary>
        /// Packetize's an Image for Sending
        /// </summary>
        /// <param name="image">The Image to Encode and Send</param>
        /// <param name="quality">The quality of the encoded image, 100 specifies the quantization tables are sent in band</param>
        public virtual void Packetize(System.Drawing.Image image, int quality = 50, bool interlaced = false)
        {
            lock (m_Frames)
            {
                try { m_Frames.Enqueue(Rtp.RFC2435Frame.Packetize(image, quality, interlaced, (int)sourceId)); }
                catch { throw; }
            }
        }

        //Needs to only send packets and not worry about updating the frame, that should be done by ImageSource

        internal virtual void SendPackets()
        {
            while (State == StreamState.Started)
            {
                try
                {
                    if (m_Frames.Count == 0)
                    {
                        System.Threading.Thread.Sleep(clockRate);
                        continue;
                    }

                    int period = (clockRate * 1000 / m_Frames.Count);

                    //Dequeue a frame or die
                    Rtp.RtpFrame frame = m_Frames.Dequeue();

                    //Get the transportChannel for the packet
                    Rtp.RtpClient.TransportContext transportContext = RtpClient.GetContextBySourceId(frame.SynchronizationSourceIdentifier);

                    if (transportContext != null)
                    {

                        DateTime now = DateTime.UtcNow;

                        //transportContext.RtpTimestamp += (uint)(clockRate * 1000 / (m_Frames.Count + 1));

                        transportContext.RtpTimestamp += period;

                        //transportContext.RtpTimestamp = (uint)(now.Ticks / TimeSpan.TicksPerMillisecond * clockRate);

                        //Iterate each packet and put it into the next frame (Todo In clock cycles)
                        //Again nothing to much to gain here in terms of parallelism (unless you want multiple pictures in the same buffer on the client)
                        foreach (Rtp.RtpPacket packet in frame)
                        {
                            //Copy the values before we signal the server
                            //packet.Channel = transportContext.DataChannel;
                            packet.SynchronizationSourceIdentifier = (int)sourceId;
                            packet.Timestamp = (int)transportContext.RtpTimestamp;

                            //Increment the sequence number on the transportChannel and assign the result to the packet
                            packet.SequenceNumber = ++transportContext.SequenceNumber;

                            //Fire an event so the server sends a packet to all clients connected to this source
                            RtpClient.OnRtpPacketReceieved(packet);
                        }

                        if (frame.PayloadTypeByte == 26) OnFrameDecoded((Rtp.RFC2435Frame)frame);

                        System.Threading.Interlocked.Increment(ref m_FramesPerSecondCounter);
                    }

                    //If we are to loop images then add it back at the end
                    if (Loop)
                    {
                        m_Frames.Enqueue(frame);
                    }

                    System.Threading.Thread.Sleep(clockRate);
                        
                }
                catch (OverflowException)
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine("Source " + Id + " Overflow");
#endif
                    //m_FramesPerSecondCounter overflowed, take a break
                    System.Threading.Thread.Sleep(0);
                    continue;
                }
                catch (Exception ex)
                {
                    if (ex is System.Threading.ThreadAbortException) return;
                    continue;
                }
            }
        }

        #endregion
    }

    //public sealed class ChildRtpImageSource : ChildStream
    //{
    //    public ChildRtpImageSource(RtpImageSource source) : base(source) { }
    //}
}