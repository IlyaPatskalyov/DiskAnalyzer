using System.Diagnostics;
using Serilog;
using Serilog.Events;

namespace DiskAnalyzer.Configuration
{
    public static class LoggingFactory
    {
        private const string Template =
            "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{SourceContext}] [{ThreadId}] [{Level}] {Message}{NewLine}{Exception}";

        public static ILogger Build()
        {
            Serilog.Debugging.SelfLog.Enable(m => Debug.Write(m));

            return new LoggerConfiguration()
                   .MinimumLevel.Debug()
                   .Enrich.WithThreadId()
                   .Enrich.FromLogContext()
                   .WriteTo.Debug(LogEventLevel.Debug, Template)
                   .WriteTo.EventLog(nameof(DiskAnalyzer),
                                     manageEventSource: false,
                                     restrictedToMinimumLevel: LogEventLevel.Error,
                                     outputTemplate: Template)
                   .CreateLogger();
        }
    }
}