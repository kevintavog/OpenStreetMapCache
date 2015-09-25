using Nancy;
using NLog;
using Rangic.Utilities.Log;

namespace OpenStreetMapCache
{
    static public class NancyContextLoggerExtensions
    {
        const string loggerKey = "LogTimer";

        static public void RegisterLogger(this NancyContext context, Logger logger)
        {
            context.Items[loggerKey] = new LogTimer(logger, LogLevel.Info);
        }

        static public LogTimer GetLogger(this NancyContext context)
        {
            return context.Items[loggerKey] as LogTimer;
        }
    }
}

