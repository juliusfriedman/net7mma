﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace Media.Rtsp
{

    /// <summary>
    /// Enumeration to describe the available Rtsp Methods
    /// </summary>
    public enum RtspMethod
    {
        UNKNOWN,
        ANNOUNCE,
        DESCRIBE,
        REDIRECT,
        OPTIONS,
        SETUP,
        GET_PARAMETER,
        SET_PARAMETER,
        PLAY,
        PAUSE,
        RECORD,
        TEARDOWN
    }

    /// <summary>
    /// RFC2326 6.1:
    /// </summary>
    public class RtspRequest : RtspMessage
    {
        public static byte[] ToHttpBytes(RtspRequest request, int minorVersion = 0) 
        {
            byte[] messageBytes = messageBytes = request.Encoding.GetBytes(System.Convert.ToBase64String(request.ToBytes()));
            
            List<byte> result = new List<byte>();
            
            result.AddRange(System.Text.Encoding.UTF8.GetBytes("HTTP 1." + minorVersion.ToString() + "\r\n GET " + request.Location + "\r\n"));
            result.AddRange(System.Text.Encoding.UTF8.GetBytes("Accept:application/x-rtsp-tunnelled \r\n"));
            result.AddRange(System.Text.Encoding.UTF8.GetBytes("Pragma:no-cache \r\n"));
            result.AddRange(System.Text.Encoding.UTF8.GetBytes("Cache-Control:no-cache \r\n"));
            result.AddRange(System.Text.Encoding.UTF8.GetBytes("Content-Length:" + messageBytes.Length + "\r\n"));
            result.AddRange(System.Text.Encoding.UTF8.GetBytes("\r\n\r\n"));
            
            result.AddRange(messageBytes);
            
            return result.ToArray();
        }

        #region Propeties

        public RtspMethod Method { get; set; }

        public Uri Location { get; set; }

        /// <summary>
        /// Indicates the UserAgent of this RtspRquest
        /// </summary>
        public String UserAgent { get { return GetHeader(RtspHeaders.UserAgent); } set { if(string.IsNullOrWhiteSpace(value)) throw new ArgumentNullException();  SetHeader(RtspHeaders.UserAgent, value); } }

        #endregion

        public RtspRequest() : base(Rtsp.RtspMessageType.Request) { }

        public RtspRequest(RtspMethod method)
            : base(Rtsp.RtspMessageType.Request)        
        {
            Method = method;
        }

        public RtspRequest(RtspMethod method, Uri location)
            : this(method)
        {
            Location = location;
        }

        public RtspRequest(byte[] packet, int offset = 0) : base(packet, offset)
        {
            try
            {
                string[] parts = m_FirstLine.Split(' ');
                //Assign method from parts
                Method = (RtspMethod)Enum.Parse(typeof(RtspMethod), parts[0], true);
                Location = new Uri(parts[1]);
                //Version = float.Parse(parts[2].Replace(MessageIdentifier + '/', string.Empty));
            }
            catch(Exception ex) { throw new RtspMessageException("Invalid RtspRequest", ex); }
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
