namespace Core.Messaging.Tests
{
    public class TestEvent : Event
    {
        public TestEvent(string message)
        {
            TestMessage = message;
        }

        public string TestMessage { get; }

        public static TestEvent WithMessage(string message)
        {
            return new TestEvent(message);
        }
    }
}
