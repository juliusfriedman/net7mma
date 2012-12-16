﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace Media.Rtsp
{
    /// <summary>
    /// RFC2326 6.1:
    /// </summary>
    public class RtspRequest : RtspMessage
    {

        #region Propeties

        public RtspMethod Method { get; set; }

        public Uri Location { get; set; }

        /// <summary>
        /// Indicates the UserAgent of this RtspRquest
        /// </summary>
        public String UserAgent { get { return base[RtspHeaders.UserAgent]; } set { if(string.IsNullOrWhiteSpace(value)) throw new ArgumentNullException();  SetHeader(RtspHeaders.UserAgent, value); } }

        #endregion

        public RtspRequest()
            : base(Rtsp.RtspMessageType.Request) { }

        public RtspRequest(RtspMethod method)
            :base(Rtsp.RtspMessageType.Request)        
        {
            Method = method;
        }

        public RtspRequest(RtspMethod method, Uri location)
            : this(method)
        {
            Location = location;
        }

        public RtspRequest(byte[] packet) : base(packet)
        {
            string[] parts = m_FirstLine.Split(' ');
            //Assign method from parts
            Method = (RtspMethod)Enum.Parse(typeof(RtspMethod), parts[0], true);
            Location = new Uri(parts[1]);
            //Version = float.Parse(parts[2].Replace(MessageIdentifier + '/', string.Empty));
        }

        public override byte[] ToBytes()
        {
            List<byte> result = new List<byte>();

            //Add the Method and the Uri
            result.AddRange(Encoding.GetBytes(Method.ToString() + " " + Location.ToString() + " " + VersionString + CRLF));

            //Get the base bytes
            result.AddRange(base.ToBytes());

            return result.ToArray();
        }
        
    }
}
