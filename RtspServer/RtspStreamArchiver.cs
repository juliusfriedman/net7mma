using Media.Rtsp.Server.MediaTypes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Media.Rtsp.Server
{
    public class RtspStreamArchiver : Common.BaseDisposable
    {

        //Nested type for playback

        public readonly string BaseDirectory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "archive";

        IDictionary<IMedia, RtpTools.RtpDump.Program> Attached = new System.Collections.Concurrent.ConcurrentDictionary<IMedia, RtpTools.RtpDump.Program>();
        
        RtspStreamArchiver()
        {
            if (false == System.IO.Directory.Exists(BaseDirectory))
            {
                System.IO.Directory.CreateDirectory(BaseDirectory);
            }
        }

        //Creates directories
        public virtual void Prepare(IMedia stream)
        {
            if (false == System.IO.Directory.Exists(BaseDirectory + '/' + stream.Id))
            {
                System.IO.Directory.CreateDirectory(BaseDirectory + '/' + stream.Id);
            }

            //Create Toc file?

            //Start, End
        }        

        //Determine if directory is created
        public virtual bool IsArchiving(IMedia stream)
        {
            return Attached.ContainsKey(stream);
        }

        //Writes a .Sdp file
        public virtual void WriteDescription(IMedia stream, Sdp.SessionDescription sdp)
        {
            if (false == IsArchiving(stream)) return;

            //Add lines with Alias info?

            System.IO.File.WriteAllText(BaseDirectory + '/' + stream.Id +'/' + "SessionDescription.sdp", sdp.ToString());
        }

        //Writes a RtpToolEntry for the packet
        public virtual void WritePacket(IMedia stream, Common.IPacket packet)
        {
            if (stream == null) return;

            RtpTools.RtpDump.Program program;
            if (false == Attached.TryGetValue(stream, out program)) return;

            if (packet is Rtp.RtpPacket) program.Writer.WritePacket(packet as Rtp.RtpPacket);
            else program.Writer.WritePacket(packet as Rtcp.RtcpPacket);
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

        void RtpClientPacketReceieved(object sender, Common.IPacket packet = null, Media.Rtp.RtpClient.TransportContext tc = null)
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
                if (false == Attached.TryGetValue(stream, out program)) return;

                program.Dispose();
                Attached.Remove(stream);

                (stream as RtpSource).RtpClient.RtpPacketReceieved -= RtpClientPacketReceieved;
                (stream as RtpSource).RtpClient.RtcpPacketReceieved -= RtpClientPacketReceieved;
            }
        }

        public override void Dispose()
        {

            if (IsDisposed) return;

            base.Dispose();

            foreach (var stream in Attached.Keys.ToArray())
                Stop(stream);

            Attached = null;
        }

        public readonly List<ArchiveSource> Sources = new List<ArchiveSource>();

        public class ArchiveSource : SourceMedia
        {
            public ArchiveSource(string name, Uri source)
                : base(name, source)
            {

            }

            public ArchiveSource(string name, Uri source, Guid id)
                : this(name, source)
            {
                Id = id;
            }
            

            public List<RtpSource> Playback = new List<RtpSource>();

            public RtpSource CreatePlayback()
            {
                RtpSource created = null;

                Playback.Add(created);

                return created;
            }

            //Implicit operator to RtpSource which creates a new RtpSource configured from the main source using CreatePlayback



        }

        public IMedia FindStreamByLocation(Uri mediaLocation)
        {
            //Check sources for name, if found then return

            return null;
        }
    }
}
