using System.Threading.Tasks;

namespace Core.Messaging.Tests
{
    public delegate Task InvokedHandler();

    public abstract class TestEventHandler : IEventSubscriber<TestEvent>
    {
        public event InvokedHandler OnHandled;
        public virtual async Task Handle(TestEvent @event)
        {
            await OnHandled?.Invoke();
            await Task.CompletedTask;
        }
    }

    public class DoSomethingOnTestEvent : TestEventHandler
    {
    }

    public class DoSomethingElseOnTestEvent : TestEventHandler
    {
    }

    public class DoSomethingWithExceptionOnTestEvent : TestEventHandler
    {
        public override async Task Handle(TestEvent @event)
        {
            await Task.Delay(0);
            throw new System.InvalidOperationException();
        }
    }
}
