namespace Media.Rtp
{
    #region JitterBuffer

    //Useful for holding onto frame for longer than one cycle.
    //Could be used from the application during the FrameChangedEvent when 'final' is set to true.
    //E.g. when final == true, =>
    //Common.BaseDisposable.SetShouldDispose(frame, false, false);
    //JitterBuffer.Add(frame);
    //Could also be used by the RtpPacketRecieved event when not using FrameChangedEvents.

    /// <summary>
    /// RtpPacket and RtpFrame storage.
    /// </summary>
    public class JitterBuffer : Common.BaseDisposable
    {
        //PayloadType, Frames for PayloadType
        readonly Common.Collections.Generic.ConcurrentThesaurus<int, RtpFrame> Frames = new Common.Collections.Generic.ConcurrentThesaurus<int, RtpFrame>();

        //Todo
        //Properties to track for max, Memory, Packets, Time etc.

        #region Constructor

        public JitterBuffer(bool shouldDispose) : base(shouldDispose) { }

        #endregion

        #region Methods

        /// <summary>
        /// Adds the given frame using the PayloadType specified by the frame.
        /// </summary>
        /// <param name="frame"></param>
        public void Add(RtpFrame frame) { Add(frame.PayloadType, frame); }

        /// <summary>
        /// Adds a frame using the specified payloadType.
        /// </summary>
        /// <param name="payloadType"></param>
        /// <param name="frame"></param>
        public void Add(int payloadType, RtpFrame frame)
        {
            if (Common.IDisposedExtensions.IsNullOrDisposed(frame)) return;

            Frames.Add(payloadType, frame);
        }

        /// <summary>
        /// Adds a packet using the PayloadType specified in the packet.
        /// </summary>
        /// <param name="packet">The packet</param>
        public void Add(RtpPacket packet) { Add(packet.PayloadType, packet); }

        /// <summary>
        /// Adds a packet with the specified payloadType
        /// </summary>
        /// <param name="payloadType"></param>
        /// <param name="packet"></param>
        public void Add(int payloadType, RtpPacket packet)
        {
            RtpFrame addedTo;

            Add(payloadType, packet, out addedTo);
        }

        /// <summary>
        /// Adds the given packet to the contained frames and returns if the add results in completion of a RtpFrame.
        /// </summary>
        /// <param name="payloadType">The payloadType to use for the add operation</param>
        /// <param name="packet">The packet to add</param>
        /// <param name="addedTo">The frame which the packet was added to.</param>
        /// <returns>True if <paramref name="addedTo"/> is complete, otherwise false.</returns>
        public bool Add(int payloadType, RtpPacket packet, out RtpFrame addedTo)
        {
            addedTo = null;

            if (Common.IDisposedExtensions.IsNullOrDisposed(packet)) return false;

            System.Collections.Generic.IEnumerable<RtpFrame> frames;

            //Use the given payloadType to get frames
            if (Frames.TryGetValue(payloadType, out frames))
            {
                //loop the frames found
                foreach (RtpFrame frame in frames)
                {
                    //if the timestamp is eqaul try to add the packet
                    if (frame.Timestamp == packet.Timestamp)
                    {
                        //Try to add the packet and if added return.
                        if (frame.TryAdd(packet))
                        {
                            addedTo = frame;

                            return frame.IsComplete;
                        }
                    }
                }
                
                //Must add a new frame to frames.
                addedTo = new RtpFrame(packet);

                Frames.Add(ref payloadType, ref addedTo);

                return addedTo.IsComplete;
            }

            return false;
        }

        /// <summary>
        /// Attempts to retrieve all <see cref="RtpFrame"/> instances related to the given payloadType
        /// </summary>
        /// <param name="payloadType"></param>
        /// <param name="frames"></param>
        /// <returns>True if <paramref name="payloadType"/> was contained, otherwise false.</returns>
        public bool TryGetFrames(int payloadType, out System.Collections.Generic.IEnumerable<RtpFrame> frames) { return Frames.TryGetValue(payloadType, out frames); }

        //Remove with timestamp start and end

        /// <summary>
        /// Clears all contained frames and optionally disposes all contained frames when removed.
        /// </summary>
        /// <param name="disposeFrames"></param>
        public void Clear(bool disposeFrames = true)
        {
            int[] keys = System.Linq.Enumerable.ToArray(Frames.Keys);

            int Key;

            //Store the frames at the key
            System.Collections.Generic.IEnumerable<RtpFrame> frames;

            //Could perform in parallel, would need frames local.
            //System.Linq.ParallelEnumerable.ForAll(keys, () => { });

            //Enumerate an array of contained keys
            for (int i = 0, e = keys.Length; i < e; ++i)
            {
                //Get the key
                Key = keys[i];

                //if removed from the ConcurrentThesaurus
                if (Frames.Remove(ref Key, out frames))
                {
                    //if we need to dispose the frames then Loop the frames contined at the key
                    if (disposeFrames) foreach (RtpFrame frame in frames)
                        {
                            //Set ShouldDispose through the base class.
                            Common.BaseDisposable.SetShouldDispose(frame, true, true);

                            //Dispose the frame (already done with above call)
                            frame.Dispose();
                        }
                }
            }
        }

        #endregion

        public override void Dispose()
        {
            base.Dispose();

            if (ShouldDispose)
            {
                Clear();
            }
        }
    }

    #endregion
}
