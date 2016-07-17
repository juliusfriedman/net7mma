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
namespace Media.Rtp
{
    /// <summary>
    /// The fields of a <see cref="RtpClient"/> instance.
    /// </summary>
    public partial class RtpClient
    {
        #region Fields

        //Buffer for data
        //Used in ReceiveData, Each TransportContext gets a chance to receive into the buffer, when the recieve completes the data is parsed if there is any then the next TransportContext goes.
        //Doing this in parallel doesn't really offset much because the decoder must be able to handle the data and if you back log the decoder you are just wasting cycles.        
        internal Common.MemorySegment m_Buffer;

        //Todo, ThreadPriorityInformation

        //Each session gets its own thread to send and recieve
        internal System.Threading.Thread m_WorkerThread, m_EventThread; // and possibly another for events.

        //This signal determines if the workers will continue each iteration, it may be possible to use int to signal various other states.
        internal bool m_StopRequested, m_ThreadEvents, //on or off right now, int could allow levels of threading..
            m_IListSockets; //Indicates if to use the IList send overloads.

        //Collection to handle the dispatch of events.
        //Notes that Collections.Concurrent.Queue may be better suited for this in production until the ConcurrentLinkedQueue has been thoroughly engineered and tested.
        //The context, the item, final, recieved
        readonly Media.Common.Collections.Generic.ConcurrentLinkedQueueSlim<System.Tuple<RtpClient.TransportContext, Common.BaseDisposable, bool, bool>> m_EventData = new Media.Common.Collections.Generic.ConcurrentLinkedQueueSlim<System.Tuple<RtpClient.TransportContext, Common.BaseDisposable, bool, bool>>();

        //Todo, LinkedQueue and Clock.
        readonly System.Threading.ManualResetEventSlim m_EventReady = new System.Threading.ManualResetEventSlim(false, 100); //should be caluclated based on memory and speed. SpinWait uses 10 as a default.

        //Outgoing Packets, Not a Queue because you cant re-order a Queue (in place) and you can't take a range from the Queue (in a single operation)
        //Those things aside, ordering is not performed here and only single packets are iterated and would eliminate the need for removing after the operation.
        //Benchmark with Queue and ConcurrentQueue and a custom impl.
        //IPacket could also work in an implementaiton which sends evertyhing in the outgoing list at one time.
        internal readonly System.Collections.Generic.List<RtpPacket> m_OutgoingRtpPackets = new System.Collections.Generic.List<RtpPacket>();
        internal readonly System.Collections.Generic.List<Media.Rtcp.RtcpPacket> m_OutgoingRtcpPackets = new System.Collections.Generic.List<Media.Rtcp.RtcpPacket>();

        /// <summary>
        /// Any TransportContext's which are added go here for removal. This list can never be null.
        /// </summary>
        /// <notes>This possibly should be sorted but sorted lists cannot contain duplicates.</notes>
        internal readonly System.Collections.Generic.List<TransportContext> TransportContexts = new System.Collections.Generic.List<TransportContext>();

        /// <summary>
        /// Unique id assigned to each RtpClient instance. (16 byte overhead)
        /// </summary>
        internal readonly System.Guid InternalId = System.Guid.NewGuid();

        /// <summary>
        /// An implementation of ILogging which can be null if unassigned.
        /// </summary>
        public Common.ILogging Logger;

        #endregion
    }
}