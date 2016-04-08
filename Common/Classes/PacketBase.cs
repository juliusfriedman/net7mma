﻿namespace Media.Common.Classes
{
    /// <summary>
    /// Represents a base class for binary types of data which are commonly associated with a packet.
    /// </summary>
    public class PacketBase : Common.LifetimeDisposable, IPacket
    {
        #region Fields

        //readonly
        internal protected byte[] m_OwnedOctets; //RawLength => m_OwnedOctets.Length

        //internal protected MemorySegment Memory;

        //int CompleteLength;

        #endregion

        #region Constructor

        public PacketBase(bool shouldDispose = true) //OneHour LifeTime
            : base(shouldDispose)
        {

        }

        public PacketBase(bool shouldDispose, System.TimeSpan lifetime)
            :base(shouldDispose, lifetime)
        {

        }

        public PacketBase(int size, bool shouldDispose) //OneHour LifeTime
            : this(shouldDispose)
        {
            m_OwnedOctets = new byte[size];

            //Memory = new MemorySegment(m_OwnedOctets);
        }

        public PacketBase(int size, bool shouldDispose, System.TimeSpan lifetime)
            : this(shouldDispose, lifetime)
        {
            m_OwnedOctets = new byte[size];

            //Memory = new MemorySegment(m_OwnedOctets);
        }

        #endregion

        #region Properties

        public System.DateTime Created
        {
            get { return base.CreatedUtc.DateTime; }
        }

        public System.DateTime? Transferred { get; internal protected set; }

        /// <summary>
        /// Provides an indication if the packet is complete.
        /// </summary>
        public bool IsComplete { get; internal protected set; }

        /// <summary>
        /// Provides an indication if the packet is compressed
        /// </summary>
        public bool IsCompressed { get; internal protected set; }

        /// <summary>
        /// Provides an indication if the packet is read only
        /// </summary>
        public bool IsReadOnly { get; internal protected set; }

        /// <summary>
        /// Provides an indication of the length of the packet.
        /// </summary>
        public long Length { get; internal protected set; }

        /// <summary>
        /// Used to Complete the packet
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public int CompleteFrom(System.Net.Sockets.Socket socket, MemorySegment buffer)
        {
            if (IsComplete) return 0;

            throw new System.NotImplementedException();
        }

        public System.Collections.Generic.IEnumerable<byte> Prepare()
        {
            return m_OwnedOctets;
        }

        #endregion

        protected internal override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (ShouldDispose)
            {
                m_OwnedOctets = null;
            }
        }
    }
}
