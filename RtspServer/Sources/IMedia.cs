using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Rtsp.Server.Sources
{
    public interface IMedia : IDisposable
    {

        IEnumerable<string> Aliases { get; }

        String Name { get; }

        Guid Id { get; }

        SourceStream.StreamState State { get; }

        Sdp.SessionDescription SessionDescription { get; }

        Uri ServerLocation { get; }

        bool Ready { get; }

        void Start();

        void Stop();
    }
}
