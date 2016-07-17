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
namespace Media.Rtsp.Server.MediaTypes
{
    /// <summary>
    /// Provides the basis of a rtsp data source, of which the source data is then propagated further as required.
    /// </summary>
    /// <remarks>
    /// Commonly used to create rtsp data when there is no `real` <see cref="RtspSource"/> from which to consume.
    /// I.e. to emulate a <see cref="RtspSource"/> or otherwise. 
    /// </remarks>
    public class RtspSink : RtspSource, IMediaSink
    {
        #region Constructors

        public RtspSink(string name, System.Uri source) : base(name, source) { }

        public RtspSink(string name, System.Uri source, Rtp.RtpClient client, bool perPacket = false)
            : base(name, source, perPacket, new RtspClient(RtspMessage.Wildcard))
        {
            RtpClient = client;
        }

        #endregion

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public void SendData(byte[] data, int offset = 0, int length = -1)
        {
            //if (RtspClient != null)//...
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public void EnqueData(byte[] data, int offset = 0, int length = -1)
        {
            //if (RtspClient != null) //...
        }
    }
}
