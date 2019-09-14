namespace Core.Messaging.Tests
{
    using Moq;
    using System;
    using Core.Logging;
    using Microsoft.Extensions.DependencyInjection;

    public class ServiceCollectionFixture
    {
        public static IServiceScopeFactory CreateServiceProviderScopeFactory()
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddSingleton<ILog>(Mock.Of<ILog>());
            serviceCollection.AddSingleton<DoSomethingOnTestEvent>();
            serviceCollection.AddSingleton<DoSomethingElseOnTestEvent>();
            serviceCollection.AddSingleton<DoSomethingWithExceptionOnTestEvent>();

            var serviceProvider = serviceCollection.BuildServiceProvider();
            return serviceProvider.GetRequiredService<IServiceScopeFactory>();
        }
    }
}
