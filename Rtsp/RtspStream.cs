using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace Media.Rtsp
{
    /// <summary>
    /// Each source stream the RtspServer encapsulates and can be played by clients
    /// </summary>
    public class RtspStream
    {
        #region Fields

        internal Guid m_Id = Guid.NewGuid();
        internal string m_Name;
        internal Uri m_Source;
        internal NetworkCredential m_Cred;
        internal NetworkCredential m_RtspCred;
        internal RtspArchiver m_Archiver;
        internal List<string> m_Aliases = new List<string>();

        #endregion

        #region Properties

        /// <summary>
        /// The unique Id of the RtspStream
        /// </summary>
        public Guid Id { get { return m_Id; } }

        /// <summary>
        /// The name of this stream, also used as the location on the server
        /// </summary>
        public string Name { get { return m_Name; } set { if (string.IsNullOrWhiteSpace(value)) throw new ArgumentNullException("Name", "Cannot consist of only null or whitespace"); m_Aliases.Add(m_Name); m_Name = value; } }

        /// <summary>
        /// The credential the source requires
        /// </summary>
        public NetworkCredential SourceCredential
        {
            get { return m_Cred; }
            set
            {
                m_Cred = value;

                bool wasConnected = Listener.Connected;

                if (wasConnected)
                {
                    //Disconnect with the old password
                    Stop();
                }

                //Set the new creds
                Listener.Credential = value;

                if (wasConnected)
                {
                    //Connect again if we need to
                    Start();
                }
            }
        }

        /// <summary>
        /// The credential of the stream which will be exposed to clients
        /// </summary>
        public NetworkCredential RtspCredential
        {
            get { return m_Cred; }
            set { m_RtspCred = value; }
        }

        //Will need to have supported methods also.. and a handler dictionary to get the handler for each request server will use its by default..
        //Will need to adjust or subclass for Archived Streams
        public RtspListener Listener { get; set; }

        /// <summary>
        /// Indicates if the sources listener is connected
        /// </summary>
        public bool Connected { get { return Listener.Connected; } }

        /// <summary>
        /// Indicates if the sources listener is listening
        /// </summary>
        public bool Listening { get { return Listener.Listening; } }

        /// <summary>
        /// Indicates if the stream is being archived
        /// </summary>
        public bool Archiving
        {
            get { return m_Archiver != null; }
            set
            {
                if (value == false && Archiving)
                {
                    m_Archiver.Stop();
                    m_Archiver = null;
                }
                else if (value == true && !Archiving)
                {
                    m_Archiver = new RtspArchiver(Id, Listener);
                    m_Archiver.Start();
                }
            }
        }

        /// <summary>
        /// Gets or sets the archiver used with this stream.
        /// </summary>
        public RtspArchiver Archiver
        {
            get { return m_Archiver; }
            set
            {
                bool wasArchiving = Archiving;
                Archiving = false;
                m_Archiver = value;
                Archiving = wasArchiving;
            }
        }

        /// <summary>
        /// The Uri to the source media
        /// </summary>
        public Uri Source
        {
            get { return m_Source; }
            set
            {

                if (value.Scheme != "rtsp://") throw new ArgumentException("value", "Must have the rtsp:// scheme.");

                bool wasConnected = Listener.Connected;

                if (wasConnected) Stop();

                Listener.Location = m_Source = value;

                if (wasConnected) Start();

            }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Constructs a RtspStream for use in a RtspServer
        /// </summary>
        /// <param name="name">The name given to the stream on the RtspServer</param>
        /// <param name="sourceLocation">The rtsp uri to the media</param>
        public RtspStream(string name, Uri sourceLocation)
        {
            //The stream name cannot be null or consist only of whitespace
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("The stream name cannot be null or consist only of whitespace", "name");
            
            //Set the name
            m_Name = name;

            //Set the source
            Source = sourceLocation;

            //Create the listener
            Listener = new RtspListener(m_Source);
        }

        /// <summary>
        /// Constructs a RtspStream for use in a RtspServer
        /// </summary>
        /// <param name="name">The name given to the stream on the RtspServer</param>
        /// <param name="sourceLocation">The rtsp uri to the media</param>
        public RtspStream(string name, string sourceLocation) : this(name, new Uri(sourceLocation)) { }
        
        /// <summary>
        /// Constructs a RtspStream for use in a RtspServer
        /// </summary>
        /// <param name="name">The name given to the stream on the RtspServer</param>
        /// <param name="sourceLocation">The rtsp uri to the media</param>
        /// <param name="credential">The network credential the stream requires</param>
        public RtspStream(string name, Uri sourceLocation, NetworkCredential credential)
            : this(name, sourceLocation)
        {
            //Set the creds
            Listener.Credential = m_Cred = credential;
        }

        /// <summary>
        /// Constructs a RtspStream for use in a RtspServer
        /// </summary>
        /// <param name="name">The name given to the stream on the RtspServer</param>
        /// <param name="sourceLocation">The rtsp uri to the media</param>
        /// <param name="credential">The network credential the stream requires</param>
        public RtspStream(string name, string sourceLocation, NetworkCredential credential)
            : this(name, sourceLocation)
        {
            //Set the creds
            Listener.Credential = m_Cred = credential;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Beings streaming from the source
        /// </summary>
        internal void Start()
        {
            if (!Listener.Connected)
            {
                try
                {
                    Listener.Connect();
                    Listener.SendOptions();
                    Listener.SendDescribe();
                    Listener.SendSetup();
                    Listener.SendPlay();
                    Listener.StartListening();
                }
                catch (RtspListener.RtspListenerException)
                {
                    //Wrong Credentails etc...
                }
                catch
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Stops streaming from the source
        /// </summary>
        internal void Stop()
        {
            if (Listener.Connected)
            {
                Listener.Disconnect();
            }
        }

        public void AddAlias(string name)
        {
            if (m_Aliases.Contains(name)) return;
            m_Aliases.Add(name);
        }

        #endregion
    }
}
