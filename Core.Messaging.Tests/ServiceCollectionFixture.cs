namespace Core.Messaging.Tests
{
    using Moq;
    using System;
    using Core.Logging;
    using Microsoft.Extensions.DependencyInjection;

    public class ServiceCollectionFixture
    {
        public static IServiceProvider CreateServiceProvider()
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddSingleton<ILog>(Mock.Of<ILog>());
            serviceCollection.AddSingleton<DoSomethingOnTestEvent>();
            serviceCollection.AddSingleton<DoSomethingElseOnTestEvent>();
            serviceCollection.AddSingleton<DoSomethingWithExceptionOnTestEvent>();

            return serviceCollection.BuildServiceProvider();
        }
    }
}
