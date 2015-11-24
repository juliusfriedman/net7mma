﻿using System;
using System.Collections.Generic;

//Will eventually prodive the base class for Http and Rtsp
//Right now Rtsp inherits from Http which is a bit confusing.

//With it the way it is now the RtspClient will still be able to transmit a HttpMessage... and vice versa.

//This will prevent that by default but once implict operators are added in the implementation then it will be possible again

namespace Media.Common.Classes
{

    public enum MessageType
    {
        Unknown,
        Request,
        Response
    }

    public abstract class MessageBase : CommonDisposable, IPacket
    {

        /// <summary>
        /// The Date and Time the message was created.
        /// </summary>
        public readonly DateTime Created = DateTime.UtcNow;

        public DateTime? Transferred;

        public MessageBase()
            : base(true)
        {

        }

        public MessageType MessageType { get; protected set; }


        DateTime IPacket.Created
        {
            get { return Created; }
        }

        DateTime? IPacket.Transferred
        {
            get { return Transferred; }
        }

        public abstract bool IsComplete { get; }

        public abstract bool IsCompressed { get; }

        public abstract bool IsReadOnly { get; }

        public abstract long Length { get; }

        public abstract int CompleteFrom(System.Net.Sockets.Socket socket, MemorySegment buffer);

        public abstract IEnumerable<byte> Prepare();

    }
}
