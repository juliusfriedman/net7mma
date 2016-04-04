namespace Media.Concepts.Classes
{
    public class UpdateableBase : Common.BaseDisposable, Common.IUpdateable
    {
        /// <summary>
        /// An instance which will never dispose and is always under modification.
        /// </summary>
        public static UpdateableBase AlwaysUnderModification = new UpdateableBase(true, 0, System.TimeSpan.Zero, false);

        readonly System.Threading.ManualResetEventSlim m_ResetEvent;

        readonly System.Threading.CancellationTokenSource m_TokenSource;

        System.Threading.ManualResetEventSlim Common.IUpdateable.ManualResetEvent
        {
            get { return m_ResetEvent; }
        }

        System.Threading.CancellationTokenSource Common.IUpdateable.UpdateTokenSource
        {
            get { return m_TokenSource; }
        }

        public bool UnderModification { get { return Common.IUpdateableExtensions.UnderModification(this); } }

        public UpdateableBase(bool initialState, int spinCount, System.TimeSpan delay, bool shouldDispose = true)
            :base(shouldDispose)
        {
            m_ResetEvent = new System.Threading.ManualResetEventSlim(initialState, spinCount);

            m_TokenSource = new System.Threading.CancellationTokenSource(delay);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (ShouldDispose)
            {
                m_ResetEvent.Dispose();

                //m_ResetEvent = null;

                m_TokenSource.Dispose();

                //m_TokenSource = null;

            }
        }
    }
}
