using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Media.Rtsp
{
    /// <summary>
    /// Handles saving frames to a .arpt file.
    /// Maybe should be abstract and have FileArchiver, DatabaseArchiver etc...
    /// </summary>
    public class RtspArchiver
    {
        /// <summary>
        /// The id of the RtspStream
        /// </summary>
        Guid m_SourceId;

        List<Rtp.RtpPacket> m_Packets;

        SortedDictionary<uint, Rtp.RtpFrame> m_Frames;

        RtspListener m_Source;

        Thread m_FramerThread;

        public RtspArchiver(Guid sourceId, RtspListener source)
        {
            m_Source = source;
        }

        ~RtspArchiver()
        {
            Stop();
        }

        void source_OnRtpPacketRecieved(RtspListener sender, Rtp.RtpPacket packet)
        {
            lock (m_Packets)
            {
                m_Packets.Add(packet);
            }
        }

        public void Start() { 
            //Create a file and folder with details
            //Ideally this should be in a database
            m_Source.OnRtpPacketRecieved += source_OnRtpPacketRecieved;
            m_FramerThread = new Thread(new ThreadStart(FrameLoop));
            m_FramerThread.Name = "RtspArchiver-" + m_SourceId;
            m_FramerThread.Start();
        }

        public void Stop() {
            m_Source.OnRtpPacketRecieved -= source_OnRtpPacketRecieved;
            //Wait for last packets to be framed
            if (!m_FramerThread.Join(2000)) m_FramerThread.Abort();
            m_FramerThread = null;
            m_Source = null;
            //Flush out Frames remaining in m_Frames
        }

        internal void FrameLoop()
        {
            //Make a new frame and iterate packets..
            //While the sequence number is the same then add else add the frame and make a new one
        }
    }
}
