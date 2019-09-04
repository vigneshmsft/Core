namespace Core.Logging.Serilog
{
    using System;
    using global::Serilog.Events;
    using global::Serilog;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel;
    using Microsoft.Extensions.DependencyInjection;

    public class LoggerFactory
    {
        private static readonly bool IsTraceEnabled = TraceEnabled();

        public static ILog CreateFileLogger(string logFilePath)
        {
            var serilogger = new LoggerConfiguration().MinimumLevel.Verbose()
                    .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7)
                    .CreateLogger();

            return new SeriLogger(serilogger);
        }

        public static ILog CreateApplicationInsightsLogger(IServiceCollection services, string instrumentationKey, string appName = "")
        {
            var telemetryClient = new TelemetryClient
            {
                InstrumentationKey = instrumentationKey
            };

            services.AddApplicationInsightsTelemetry(options =>
            {
                options.InstrumentationKey = instrumentationKey;
                options.ApplicationVersion = appName;
            });

            var telemetryConfiguration = new TelemetryConfiguration(instrumentationKey, new ServerTelemetryChannel());

            var serilogger = new LoggerConfiguration().MinimumLevel.Verbose()
                .WriteTo.ApplicationInsights(telemetryConfiguration, TelemetryConverter.Traces)
                .Filter.ByExcluding(ShouldExcludeLogging)
                .CreateLogger();

            return new SeriLogger(serilogger, telemetryClient, appName);
        }

        public static ILog CreateApplicationInsightsLogger(string instrumentationKey, string appName = "")
        {
            var telemetryClient = new TelemetryClient
            {
                InstrumentationKey = instrumentationKey
            };

            var telemetryConfiguration = new TelemetryConfiguration(instrumentationKey, new ServerTelemetryChannel());

            var serilogger = new LoggerConfiguration().MinimumLevel.Verbose()
                    .WriteTo.ApplicationInsights(telemetryConfiguration, TelemetryConverter.Traces)
                    .Filter.ByExcluding(ShouldExcludeLogging)
                    .CreateLogger();

            return new SeriLogger(serilogger, telemetryClient, appName);
        }

        public static ILog CreateApplicationInsightsAndFileLogger(string logFilePath, string instrumentationKey, string appName = "")
        {
            var telemetryClient = new TelemetryClient
            {
                InstrumentationKey = instrumentationKey
            };

            var telemetryConfiguration = new TelemetryConfiguration(instrumentationKey, new ServerTelemetryChannel());

            var serilogger = new LoggerConfiguration().MinimumLevel.Verbose()
                    .WriteTo.File(logFilePath)
                    .WriteTo.ApplicationInsights(telemetryConfiguration, TelemetryConverter.Traces)
                    .Filter.ByExcluding(ShouldExcludeLogging)
                    .CreateLogger();

            return new SeriLogger(serilogger, telemetryClient, appName);
        }

        public static ILog CreateConsoleFileLogger()
        {

            var serilogger = new LoggerConfiguration().MinimumLevel.Verbose()
                                .Enrich.FromLogContext()
                                .WriteTo.Console().CreateLogger();

            return new SeriLogger(serilogger);
        }

        private static bool ShouldExcludeLogging(LogEvent logEvent)
        {
            if (logEvent.Level == LogEventLevel.Information ||
                logEvent.Level == LogEventLevel.Error ||
                logEvent.Level == LogEventLevel.Fatal)
            {
                return false;
            }

            return !IsTraceEnabled;
        }

        private static bool TraceEnabled()
        {
            return Environment.GetEnvironmentVariable("TraceEnabled")?.Equals("true", StringComparison.CurrentCultureIgnoreCase) ?? false;
        }
    }
}