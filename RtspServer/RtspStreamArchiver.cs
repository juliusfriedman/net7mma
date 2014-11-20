using Media.Rtsp.Server.Sources;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Media.Rtsp.Server
{
    public class RtspStreamArchiver : Common.BaseDisposable
    {
        string BaseDirectory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "archive";

        IDictionary<IMedia, RtpTools.RtpDump.Program> Attached = new System.Collections.Concurrent.ConcurrentDictionary<IMedia, RtpTools.RtpDump.Program>();
        
        RtspStreamArchiver()
        {
            if (!System.IO.Directory.Exists(BaseDirectory))
            {
                System.IO.Directory.CreateDirectory(BaseDirectory);
            }
        }

        //Creates directories
        public virtual void Prepare(IMedia stream)
        {
            if (!System.IO.Directory.Exists(BaseDirectory + '/' + stream.Id))
            {
                System.IO.Directory.CreateDirectory(BaseDirectory + '/' + stream.Id);
            }

            //Create Toc file?
        }        

        //Determine if directory is created
        public virtual bool IsArchiving(IMedia stream)
        {
            return Attached.ContainsKey(stream);
        }

        //Writes a .Sdp file
        public virtual void WriteDescription(IMedia stream, Sdp.SessionDescription sdp)
        {
            if (!IsArchiving(stream)) return;

            //Add lines with Alias info?

            System.IO.File.WriteAllText(BaseDirectory + '/' + stream.Id +'/' + "SessionDescription.sdp", sdp.ToString());
        }

        //Writes a RtpToolEntry for the packet
        public virtual void WritePacket(IMedia stream, Common.IPacket packet)
        {
            if (stream == null) return;

            RtpTools.RtpDump.Program program;
            if (!Attached.TryGetValue(stream, out program)) return;

            if (packet is Rtp.RtpPacket) program.Writer.WritePacket(packet as Rtp.RtpPacket);
            program.Writer.WritePacket(packet as Rtcp.RtcpPacket);
        }

        public virtual void Start(IMedia stream, RtpTools.FileFormat format = RtpTools.FileFormat.Binary)
        {

            if (stream is RtpSource)
            {
                RtpTools.RtpDump.Program program;
                if (Attached.TryGetValue(stream, out program)) return;

                Prepare(stream);

                program = new RtpTools.RtpDump.Program(); //.DumpWriter(BaseDirectory + '/' + stream.Id + '/' + DateTime.UtcNow.ToFileTime(), format, new System.Net.IPEndPoint(System.Net.IPAddress.Any, 0));

                Attached.Add(stream, program);

                (stream as RtpSource).RtpClient.RtpPacketReceieved += RtpClientPacketReceieved;
                (stream as RtpSource).RtpClient.RtcpPacketReceieved += RtpClientPacketReceieved;

            }
        }

        void RtpClientPacketReceieved(object sender, Common.IPacket packet)
        {
            if(sender is Rtp.RtpClient)
                WritePacket(Attached.Keys.FirstOrDefault(s => (s as RtpSource).RtpClient == sender as Rtp.RtpClient), packet);
        }

        //Stop recoding a stream
        public virtual void Stop(IMedia stream)
        {
            if (stream is RtpSource)
            {
                RtpTools.RtpDump.Program program;
                if (!Attached.TryGetValue(stream, out program)) return;

                program.Dispose();
                Attached.Remove(stream);

                (stream as RtpSource).RtpClient.RtpPacketReceieved -= RtpClientPacketReceieved;
                (stream as RtpSource).RtpClient.RtcpPacketReceieved -= RtpClientPacketReceieved;
            }
        }

        public override void Dispose()
        {

            if (Disposed) return;

            base.Dispose();

            foreach (var stream in Attached.Keys.ToArray())
                Stop(stream);

            Attached = null;
        }
    }
}
