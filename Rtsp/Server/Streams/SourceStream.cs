using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Media.Rtsp.Server.Streams
{
    //Might need a lower level class Stream with no events and just the basic info

    /// <summary>
    /// The base class of all Streams, Should eventually be exposed on RtspServer not RtspStream
    /// </summary>
    public abstract class SourceStream
    {
        //public static ChildStream CreateChild(SourceStream source) { return new ChildStream(source); }

        public enum StreamState
        {
            Stopped,
            Started
        }

        #region Fields

        internal DateTime? m_Started;
        internal Guid m_Id = Guid.NewGuid();
        internal string m_Name;
        internal Uri m_Source;
        internal NetworkCredential m_SourceCred;
        //internal CredentialCache m_CredentalCache = new CredentialCache();
        internal NetworkCredential m_RemoteCred;
        internal List<string> m_Aliases = new List<string>();
        internal bool m_Child = false;
        volatile internal System.Drawing.Image m_lastFrame;
        internal bool m_ForceTCP;// = true; // To force clients to utilize TCP Interleaved

        #endregion

        #region Properties

        /// <summary>
        /// The amount of time the Stream has been Started
        /// </summary>
        public TimeSpan Uptime { get { if (m_Started.HasValue) return DateTime.Now - m_Started.Value; return TimeSpan.MinValue; } }

        /// <summary>
        /// The unique Id of the RtspStream
        /// </summary>
        public virtual Guid Id { get { return m_Id; } set { m_Id = value; } }

        /// <summary>
        /// The name of this stream, also used as the location on the server
        /// </summary>
        public virtual string Name { get { return m_Name; } set { if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Name", "Cannot be null or consist only of whitespace"); m_Aliases.Add(m_Name); m_Name = value; } }

        /// <summary>
        /// Any Aliases the stream is known by
        /// </summary>
        public virtual string[] Aliases { get { return m_Aliases.ToArray(); } }

        /// <summary>
        /// The credential the source requires
        /// </summary>
        public virtual NetworkCredential SourceCredential { get { return m_SourceCred; } set { m_SourceCred = value; } }
        /// <summary>
        /// The credential of the stream which will be exposed to clients
        /// </summary>
        public virtual NetworkCredential RemoteCredential
        {
            get { return m_RemoteCred; }
            set { m_RemoteCred = value; }
        }

        /// <summary>
        /// State of the stream 
        /// </summary>
        public virtual StreamState State { get; set; }

        /// <summary>
        /// Is this RtspStream dependent on another
        /// </summary>
        public bool Parent { get { return !m_Child; } }

        /// <summary>
        /// The Uri to the source media
        /// </summary>
        public virtual Uri Source
        {
            get { return m_Source; }
            set
            {
                m_Source = value;
            }
        }

        #endregion

        //Needs Packet and Frame Events abstraction?

        public SourceStream(string name, Uri source)
        {
            //The stream name cannot be null or consist only of whitespace
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("The stream name cannot be null or consist only of whitespace", "name");
            m_Name = name;

            m_Source = source;
        }

        public SourceStream(string name, Uri source, NetworkCredential sourceCredential)
            :this(name, source)
        {
            m_SourceCred = sourceCredential;
        }

        public SourceStream(string name, Uri source, NetworkCredential sourceCredential, NetworkCredential remoteCredential)
            :this(name, source, sourceCredential)
        {
            m_RemoteCred = remoteCredential;
            //m_CredentalCache.Add(source, "Basic", remoteCredential);
        }

        public delegate void FrameDecodedHandler(SourceStream stream, System.Drawing.Image decoded);

        public virtual event FrameDecodedHandler FrameDecoded;

        internal void OnFrameDecoded(System.Drawing.Image decoded)
        {
            if (FrameDecoded != null) FrameDecoded(this, decoded);
        }

        public abstract void Start();

        public abstract void Stop();

        public abstract bool Connected { get; }

        public abstract bool Listening { get; }

        public abstract Sdp.SessionDescription SessionDescription { get; }

        public void AddAlias(string name)
        {
            if (m_Aliases.Contains(name)) return;
            m_Aliases.Add(name);
        }

        public void RemoveAlias(string alias)
        {
            m_Aliases.Remove(alias);
        }

        public virtual System.Drawing.Image GetFrame()
        {
            return m_lastFrame;
        }
    }
}
