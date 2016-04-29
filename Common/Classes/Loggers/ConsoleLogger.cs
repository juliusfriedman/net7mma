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

        public override void LogException(Exception ex)
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

    //See also https://github.com/perpetual-motion/clrplus/blob/master/Console/ConsoleApi.cs

    //Todo, sometimes the color most be changed differently depending on Platform
    //public static class ConsoleLoggerExtensions
    //{
    //    public static void WriteString(string toWrite, ConsoleColor color)
    //    {
    //        if (Common.Extensions.OperatingSystemExtensions.IsMac)
    //        {
    //            //https://github.com/dotnet/cli/blob/ac4c6702d5d345dc9c397f80290f105fedebe98b/scripts/Microsoft.DotNet.Cli.Build.Framework/AnsiColorExtensions.cs
    //        }
    //    }
    //}
}
