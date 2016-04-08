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
        public static readonly NullLogger Default = new NullLogger(false);

        public NullLogger(bool shouldDispose) : base(shouldDispose) { }

        public override void Log(string message) { }

        public override void LogException(Exception ex) { }
    }
}
