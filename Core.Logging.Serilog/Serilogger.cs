namespace Core.Logging.Serilog
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Concurrent;
    using global::Serilog;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.DataContracts;

    public class SeriLogger : ILog, IDisposable
    {
        private readonly ILogger _logger;
        private readonly TelemetryClient _telemetryClient;
        private readonly string _appName;

        public SeriLogger(ILogger logger)
        {
            _logger = logger;
        }

        internal SeriLogger(ILogger logger, TelemetryClient telemetryClient, string appName) : this(logger)
        {
            _telemetryClient = telemetryClient;
            _appName = appName;
        }

        public void Request(RequestTrace requestTrace)
        {
            var requestTelemetry = new RequestTelemetry(requestTrace.Route, requestTrace.StartTime,
                requestTrace.Duration, requestTrace.ResponseCode, requestTrace.Successful);
            AddAppName(requestTelemetry.Properties);
            _telemetryClient?.TrackRequest(requestTelemetry);
        }

        public void Trace(string trace)
        {
            _logger.Verbose(trace);
        }

        public void Debug(string debug)
        {
            _logger.Debug(debug);
        }

        public void Warn(string warning)
        {
            _logger.Warning(warning);
            _telemetryClient?.TrackEvent("Warning", AddAppName(new Dictionary<string, string> { { "Message", warning } }));
        }

        public void Info(string information)
        {
            _logger.Information(information);
        }

        public void Error(string error, IDictionary<string, string> parameters = null)
        {
            _logger.Error(error);
            _telemetryClient?.TrackException(new Exception(error), AddAppName(parameters));
        }

        public void Error(Exception exception, IDictionary<string, string> parameters = null)
        {
            _logger.Error(exception, $"Exception {exception}");
            _telemetryClient?.TrackException(exception, AddAppName(parameters));
        }

        public void Event(string @event, IDictionary<string, string> parameters = null)
        {
            _telemetryClient?.TrackEvent(@event, AddAppName(parameters));
        }

        public void Dispose()
        {
            _telemetryClient?.Flush();
        }

        private IDictionary<string, string> AddAppName(IDictionary<string, string> properties)
        {
            if (properties == null)
                properties = new ConcurrentDictionary<string, string>();

            properties["App"] = _appName;
            return properties;
        }
    }
}