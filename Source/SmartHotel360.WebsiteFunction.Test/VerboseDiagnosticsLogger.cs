using System;
using System.Diagnostics;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace SmartHotel360.WebsiteFunction.Test
{
    public class VerboseDiagnosticsLogger : ILogger
    {

        public VerboseDiagnosticsLogger() : base()
        {

        }

      

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            //Debug.WriteLine(formatter.);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }
    }
}
