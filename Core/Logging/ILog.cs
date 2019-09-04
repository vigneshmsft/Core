namespace Core.Logging
{
    using System;
    using System.Collections.Generic;

    public interface ILog
    {
        void Request(RequestTrace requestTrace);

        void Trace(string trace);

        void Debug(string debug);

        void Warn(string warning);

        void Info(string information);

        void Error(string error, IDictionary<string, string> parameters = null);

        void Error(Exception exception, IDictionary<string, string> parameters = null);

        void Event(string @event, IDictionary<string, string> parameters = null);
    }
}