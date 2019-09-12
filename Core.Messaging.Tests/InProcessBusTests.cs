using System;
using System.Threading.Tasks;
using Core.Messaging.InProcess;
using NUnit.Framework;
using Microsoft.Extensions.DependencyInjection;


namespace Core.Messaging.Tests
{
    [TestFixture]
    public class InProcessBusTests
    {
        private IServiceProvider _serviceProvider;
        private InProcessBus _inProcessBus;

        [OneTimeSetUp]
        public void TestSetup()
        {
            _serviceProvider = ServiceCollectionFixture.CreateServiceProvider();
            _inProcessBus = new InProcessBus(_serviceProvider);
        }

        [Test]
        public void OnCreatePublisher_CheckPublisherCreatedSuccessfully()
        {
            const string topicName = "TestTopic";
            var publisher = _inProcessBus.CreatePublisher(topicName);
            Assert.IsNotNull(publisher);
        }

        [Test]
        public void OnCreateSubscription_CheckSubscriberCreatedSuccessfully()
        {
            const string topicName = "TestTopic";
            var subscription = _inProcessBus.CreateSubscription(topicName);
            Assert.IsNotNull(subscription);
        }

        [TestFixture]
        public class WhenEventPublished
        {
            private const string TopicName = nameof(WhenEventPublished);

            [Test]
            public async Task WhenSubscriberNotRegistered_CheckEventHandlerIsNotInvoked()
            {
                var serviceProvider = ServiceCollectionFixture.CreateServiceProvider();
                var inProcessBus = new InProcessBus(serviceProvider);

                var publisher = inProcessBus.CreatePublisher(TopicName);

                _ = inProcessBus.CreateSubscription(TopicName);

                var handlerOne = AssertTestEventHandled<DoSomethingOnTestEvent>(serviceProvider, false);

                await publisher.Publish(TestEvent.WithMessage("This is a test message"));
                await Task.WhenAll(handlerOne);
            }

            [Test]
            public async Task WhenSubscriberRegistered_CheckEventHandlerInvoked()
            {
                var serviceProvider = ServiceCollectionFixture.CreateServiceProvider();
                var inProcessBus = new InProcessBus(serviceProvider);

                var publisher = inProcessBus.CreatePublisher(TopicName);

                var subscription = inProcessBus.CreateSubscription(TopicName);
                subscription.AddSubscriber<TestEvent, DoSomethingOnTestEvent>();

                var handlerOne = AssertTestEventHandled<DoSomethingOnTestEvent>(serviceProvider);

                await publisher.Publish(TestEvent.WithMessage("This is a test message"));
                await Task.WhenAll(handlerOne);
            }

            [Test]
            public async Task WhenSubscriberRegistered_ThrowsException_CheckEventHandlerInvoked()
            {
                var serviceProvider = ServiceCollectionFixture.CreateServiceProvider();
                var inProcessBus = new InProcessBus(serviceProvider);

                var publisher = inProcessBus.CreatePublisher(TopicName);

                var subscription = inProcessBus.CreateSubscription(TopicName);
                subscription.AddSubscriber<TestEvent, DoSomethingOnTestEvent>();
                subscription.AddSubscriber<TestEvent, DoSomethingWithExceptionOnTestEvent>();

                var handlerOne = AssertTestEventHandled<DoSomethingOnTestEvent>(serviceProvider);

                await publisher.Publish(TestEvent.WithMessage("This is a test message"));
                await Task.WhenAll(handlerOne);
            }

            [Test]
            public async Task WhenMultipleSubscriberRegistered_CheckEventHandlerIsInvokedForAll()
            {
                var serviceProvider = ServiceCollectionFixture.CreateServiceProvider();
                var inProcessBus = new InProcessBus(serviceProvider);

                var publisher = inProcessBus.CreatePublisher(TopicName);

                var subscription = inProcessBus.CreateSubscription(TopicName);
                subscription.AddSubscriber<TestEvent, DoSomethingOnTestEvent>();
                subscription.AddSubscriber<TestEvent, DoSomethingElseOnTestEvent>();

                var handlerOne = AssertTestEventHandled<DoSomethingOnTestEvent>(serviceProvider);
                var handlerTwo = AssertTestEventHandled<DoSomethingElseOnTestEvent>(serviceProvider);

                await publisher.Publish(TestEvent.WithMessage("This is a test message"));

                await Task.WhenAll(handlerOne, handlerTwo);
            }

            private async Task AssertTestEventHandled<TTestEventHandler>(IServiceProvider serviceProvider, bool shouldBeHandled = true) where TTestEventHandler : TestEventHandler
            {
                var handled = false;
                var testEventHandler = serviceProvider.GetService<TTestEventHandler>();

                testEventHandler.OnHandled += Handle;

                await Task.Delay(1000);

                testEventHandler.OnHandled -= Handle;

                if (shouldBeHandled)
                    Assert.IsTrue(handled, $"{typeof(TTestEventHandler)} Should be Invoked!");
                else
                    Assert.IsFalse(handled, $"Handle method in {nameof(TTestEventHandler)} should not be Invoked!");

                async Task Handle()
                {
                    handled = true;
                    await Task.CompletedTask;
                }
            }
        }
    }
}