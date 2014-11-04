using Media.Rtsp.Server.Streams;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Rtsp.Server
{
    public class RtspStreamArchiver : Common.BaseDisposable
    {
        string BaseDirectory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "archive";

        IDictionary<IMediaStream, RtpTools.RtpDump.Program> Attached = new System.Collections.Concurrent.ConcurrentDictionary<IMediaStream, RtpTools.RtpDump.Program>();
        
        RtspStreamArchiver()
        {
            if (!System.IO.Directory.Exists(BaseDirectory))
            {
                System.IO.Directory.CreateDirectory(BaseDirectory);
            }
        }

        //Creates directories
        public virtual void Prepare(IMediaStream stream)
        {
            if (!System.IO.Directory.Exists(BaseDirectory + '/' + stream.Id))
            {
                System.IO.Directory.CreateDirectory(BaseDirectory + '/' + stream.Id);
            }

            //Create Toc file?
        }        

        //Determine if directory is created
        public virtual bool IsArchiving(IMediaStream stream)
        {
            return Attached.ContainsKey(stream);
        }

        //Writes a .Sdp file
        public virtual void WriteDescription(IMediaStream stream, Media.Sdp.SessionDescription sdp)
        {
            if (!IsArchiving(stream)) return;

            //Add lines with Alias info?

            System.IO.File.WriteAllText(BaseDirectory + '/' + stream.Id +'/' + "SessionDescription.sdp", sdp.ToString());
        }

        //Writes a RtpToolEntry for the packet
        public virtual void WritePacket(IMediaStream stream, Common.IPacket packet)
        {
            if (stream == null) return;

            RtpTools.RtpDump.Program program;
            if (!Attached.TryGetValue(stream, out program)) return;

            if (packet is Rtp.RtpPacket) program.Writer.WritePacket(packet as Rtp.RtpPacket);
            program.Writer.WritePacket(packet as Rtcp.RtcpPacket);
        }

        public virtual void Start(IMediaStream stream, RtpTools.FileFormat format = RtpTools.FileFormat.Binary)
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

        void RtpClientPacketReceieved(object sender, Media.Common.IPacket packet)
        {
            if(sender is Rtp.RtpClient)
                WritePacket(Attached.Keys.FirstOrDefault(s => (s as RtpSource).RtpClient == sender as Rtp.RtpClient), packet);
        }

        //Stop recoding a stream
        public virtual void Stop(IMediaStream stream)
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
