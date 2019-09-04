namespace Core.Messaging
{
    using System;

    public abstract class Event
    {
        /// <summary>
        /// Time the event occured (in UTC).
        /// </summary>
        public DateTime EventTime { get; }

        /// <summary>
        /// The event identifier.
        /// </summary>
        public Guid EventId { get; }

        protected Event()
        {
            EventTime = DateTime.UtcNow;
            EventId = Guid.NewGuid();
        }
    }
}