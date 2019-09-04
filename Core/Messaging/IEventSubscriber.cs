namespace Core.Messaging
{
    using System.Threading.Tasks;

    public interface IEventSubscriber<in TEvent> where TEvent : Event
    {
        /// <summary>
        /// Handles the <see cref="TEvent"/>
        /// </summary>
        /// <typeparam name="TEvent">The event that will be handled.</typeparam>
        Task Handle(TEvent @event);
    }
}
