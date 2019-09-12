using NUnit.Framework;

namespace Core.Messaging.Tests
{
    [TestFixture]
    public class SubscriptionFactoryTests
    {
        [Test]
        public void GetSubscriberForEvent_WhenNoSubscriptionStored_ReturnsNothing()
        {
            using var subscriptionFactory = new SubscriptionFactory();
            var testEventSubscription = subscriptionFactory.GetSubscriberForEvent<TestEvent>();
            Assert.IsNull(testEventSubscription, "testEventSubscription should be null");
        }

        [Test]
        public void GetSubscriberForEvent_WhenSubscriptionStored_ReturnsSubscription()
        {
            var subscriptionFactory = new SubscriptionFactory();
            var createdSubscription = subscriptionFactory.CreateSubscriberForEvent<TestEvent>();

            var testEventSubscription = subscriptionFactory.GetSubscriberForEvent<TestEvent>();

            Assert.AreSame(createdSubscription, testEventSubscription, "Subscription instances are not the same");
        }

        [Test]
        public void GivenMultipleInstances_Check_ReturnsCorrectSubscriptionFromInstance()
        {
            var subscriptionFactory = new SubscriptionFactory();
            var createdSubscription = subscriptionFactory.CreateSubscriberForEvent<TestEvent>();

            using var newSubscriptionFactory = new SubscriptionFactory();
            var newTestEventSubscription = newSubscriptionFactory.GetSubscriberForEvent<TestEvent>();
            Assert.IsNull(newTestEventSubscription, "newTestEventSubscription should be null");

            var newlyCreatedSubscription = newSubscriptionFactory.CreateSubscriberForEvent<TestEvent>();

            var testEventSubscription = subscriptionFactory.GetSubscriberForEvent<TestEvent>();

            Assert.AreSame(createdSubscription, testEventSubscription, "Subscription instances are not the same");
            Assert.AreNotSame(createdSubscription, newlyCreatedSubscription, "Subscription instances should not be same");
        }
    }
}
