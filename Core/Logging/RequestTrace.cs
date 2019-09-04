using System;
using System.Collections.Generic;

namespace Core.Logging
{
    public struct RequestTrace
    {
        public string Route { get; set; }

        public DateTimeOffset StartTime { get; set; }

        public DateTimeOffset EndTime { get; set; }

        public string ResponseCode { get; set; }

        public bool Successful { get; set; }

        public IDictionary<string, string> Properties { get; set; }

        public TimeSpan Duration => EndTime - StartTime;
    }
}