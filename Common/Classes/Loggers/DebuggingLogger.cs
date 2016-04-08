using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Common.Loggers
{
    public class DebuggingLogger : BaseLogger
    {

        internal static void CoreWrite(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;

            try { System.Diagnostics.Debug.WriteLine(message); }
            catch { throw; }
        }

        public override void LogException(Exception ex)
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
