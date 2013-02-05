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
        
        //Maybe should be m_AllowUdp?
        internal bool m_ForceTCP;// = true; // To force clients to utilize TCP, Interleaved in Rtsp or Rtp

        internal bool m_DisableQOS; //Disabled optional quality of service, In Rtp this is Rtcp

        #endregion

        #region Properties

        /// <summary>
        /// The amount of time the Stream has been Started
        /// </summary>
        public TimeSpan Uptime { get { if (m_Started.HasValue) return DateTime.UtcNow - m_Started.Value; return TimeSpan.MinValue; } }

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
        public virtual NetworkCredential RemoteCredential { get { return m_RemoteCred; } set { m_RemoteCred = value; } }

        /// <summary>
        /// State of the stream 
        /// </summary>
        public virtual StreamState State { get; protected set; }

        /// <summary>
        /// Is this RtspStream dependent on another
        /// </summary>
        public bool IsParent { get { return !m_Child; } }

        /// <summary>
        /// The Uri to the source media
        /// </summary>
        public virtual Uri Source { get { return m_Source; } set { m_Source = value; } }

        /// <summary>
        /// Indicates the source is ready to have clients connect
        /// </summary>
        public virtual bool Ready { get; protected set; }

        #endregion

        #region Constructor        

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

        #endregion

        #region Events

        public delegate void FrameDecodedHandler(SourceStream stream, System.Drawing.Image decoded);

        public event FrameDecodedHandler FrameDecoded;

        internal void OnFrameDecoded(System.Drawing.Image decoded) { if (FrameDecoded != null) FrameDecoded(this, decoded); }

        #endregion

        #region Methods

        //Sets the State = StreamState.Started
        public virtual void Start() { State = StreamState.Started; }

        //Sets the State = StreamState.Stopped
        public virtual void Stop() { State = StreamState.Stopped; }

        public void AddAlias(string name)
        {
            if (m_Aliases.Contains(name)) return;
            m_Aliases.Add(name);
        }

        public void RemoveAlias(string alias)
        {
            m_Aliases.Remove(alias);
        }

        #endregion
    }
}
