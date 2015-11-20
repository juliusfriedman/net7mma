using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Common.Loggers
{
    public abstract class BaseLogger : BaseDisposable, ILogging
    {
        public abstract void Log(string message);

        public abstract void LogException(Exception exception);

        public override void Dispose()
        {
            base.Dispose();
        }
    }
}
