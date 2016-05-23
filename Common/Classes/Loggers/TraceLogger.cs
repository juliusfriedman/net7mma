namespace Media.Common.Loggers
{
    public class TraceLogger : BaseLogger
    {
        internal static void CoreWrite(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;

            try { System.Diagnostics.Trace.WriteLine(message); }
            catch { throw; }
        }

        public override void LogException(System.Exception ex)
        {
            try { CoreWrite(ex.Message); }
            catch { throw; }
        }

        public override void Log(string data)
        {
            try { CoreWrite(data); }
            catch { throw; }
        }
    }
}
