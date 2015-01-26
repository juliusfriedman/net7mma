using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Common.Loggers
{
    /// <summary>
    /// A <see cref="ILogging"/> implementation which does nothing.
    /// </summary>
    public class NullLogger : BaseLogger
    {
        public override void Log(string message) { }

        public void LogException(Exception ex) { }
    }
}
