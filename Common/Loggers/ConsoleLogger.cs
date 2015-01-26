using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Common.Loggers
{
    /// <summary>
    /// A <see cref="ILogging"/> implementation which writes to the <see cref="System.Console"/>
    /// </summary>
    public class ConsoleLogger : BaseLogger
    {
        /// <summary>
        /// Gets or sets the color used.
        /// </summary>
        public ConsoleColor ExceptionColor = ConsoleColor.Red, NormalColor = ConsoleColor.Green;

        public override void Log(string message)
        {
            ConsoleColor previous = Console.ForegroundColor;
            try
            {
                Console.ForegroundColor = NormalColor;
                System.Console.WriteLine(message);
            }
            catch { throw; }
            finally { Console.ForegroundColor = previous; }
        }

        public void LogException(Exception ex)
        {
            ConsoleColor previous = Console.ForegroundColor;
            try
            {
                Console.ForegroundColor = ExceptionColor;
                System.Console.WriteLine(ex.Message);
            }
            catch { throw; }
            finally { Console.ForegroundColor = previous; }
        }
    }
}
