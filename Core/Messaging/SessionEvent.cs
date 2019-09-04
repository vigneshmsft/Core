namespace Core.Messaging
{
    /// <summary>
    /// Represents an event which is part of a session or sequence
    /// To be used when events form a sequence and need to follow a FIFO order.
    /// </summary>
    public abstract class SessionEvent : Event
    {
        public string SessionId { get; }

        protected SessionEvent(string sessionId)
        {
            SessionId = sessionId;
        }
    }
}
