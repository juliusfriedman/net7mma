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
using System.Net;

namespace Media.Rtsp.Server.Streams
{
    /// <summary>
    /// The base class of all sources the RtspServer can service.
    /// </summary>
    /// <remarks>
    /// Provides a way to augment all classes from one place.
    /// </remarks>
    public abstract class SourceStream : ISource, IMediaStream
    {
        const string UriScheme = "rtspserver://";

        #region StreamState Enumeration

        public enum StreamState
        {
            Stopped,
            Started,
            //Faulted
        }

        #endregion

        #region Fields

        internal DateTime? m_StartedTimeUtc;
        internal Guid m_Id = Guid.NewGuid();
        internal string m_Name;
        internal Uri m_Source;
        internal NetworkCredential m_SourceCred;
        internal List<string> m_Aliases = new List<string>();
        internal bool m_Child = false;
        internal Sdp.SessionDescription m_Sdp;

        //Maybe should be m_AllowUdp?
        internal bool m_ForceTCP;//= true; // To force clients to utilize TCP, Interleaved in Rtsp or Rtp

        internal bool m_DisableQOS; //Disabled optional quality of service, In Rtp this is Rtcp

        #endregion

        #region Properties

        /// <summary>
        /// The amount of time the Stream has been Started
        /// </summary>
        public TimeSpan Uptime { get { if (m_StartedTimeUtc.HasValue) return DateTime.UtcNow - m_StartedTimeUtc.Value; return TimeSpan.MinValue; } }

        /// <summary>
        /// The unique Id of the RtspStream
        /// </summary>
        public Guid Id { get { return m_Id; } private set { m_Id = value; } }

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
        /// The type of Authentication the source requires for the SourceCredential
        /// </summary>
        public virtual AuthenticationSchemes SourceAuthenticationScheme { get; set; }

        /// <summary>
        /// Gets a Uri which indicates to the RtspServer the name of this stream reguardless of alias
        /// </summary>
        public virtual Uri ServerLocation { get { return new Uri(UriScheme + Id.ToString()); } }

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

        /// <summary>
        /// Indicates if the souce should attempt to decode frames which change.
        /// </summary>
        public bool DecodeFrames { get; protected set; }

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

        #endregion

        #region Events

        public delegate void FrameDecodedHandler(SourceStream stream, System.Drawing.Image decoded);

        public delegate void DataDecodedHandler(SourceStream stream, byte[] decoded);

        public event FrameDecodedHandler FrameDecoded;

        public event DataDecodedHandler DataDecoded;

        internal void OnFrameDecoded(System.Drawing.Image decoded) { if (DecodeFrames && decoded != null && FrameDecoded != null) FrameDecoded(this, decoded); }

        internal void OnFrameDecoded(byte[] decoded) { if (DecodeFrames && decoded != null && DataDecoded != null) DataDecoded(this, decoded); }

        #endregion

        #region Methods

        //Sets the State = StreamState.Started
        public virtual void Start() { State = StreamState.Started; m_StartedTimeUtc = DateTime.UtcNow; }

        //Sets the State = StreamState.Stopped
        public virtual void Stop() { State = StreamState.Stopped; m_StartedTimeUtc = null; }

        public void AddAlias(string name)
        {
            if (m_Aliases.Contains(name)) return;
            m_Aliases.Add(name);
        }

        public void RemoveAlias(string alias)
        {
            m_Aliases.Remove(alias);
        }

        public void ClearAliases() { m_Aliases.Clear(); }

        #endregion

        Guid IMediaStream.Id
        {
            get { return Id; }
        }

        Sdp.SessionDescription IMediaStream.SessionDescription { get { return m_Sdp; } }

        SourceStream.StreamState IMediaStream.State
        {
            get { return State; }
        }

        void IMediaStream.Start()
        {
            Start();
        }

        void IMediaStream.Stop()
        {
            Stop();
        }
    }
}
