namespace Ocelot.UnitTests.ServiceDiscovery
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Steeltoe.Common.Discovery;
    using Values;
    using System.Collections.Generic;
    using Moq;
    using Ocelot.Configuration;
    using Ocelot.Configuration.Builder;
    using Ocelot.Logging;
    using Ocelot.ServiceDiscovery;
    using Ocelot.ServiceDiscovery.Providers;
    using Shouldly;
    using TestStack.BDDfy;
    using Xunit;

    public class ServiceProviderFactoryTests
    {
        private ServiceProviderConfiguration _serviceConfig;
        private IServiceDiscoveryProvider _result;
        private ServiceDiscoveryProviderFactory _factory;
        private DownstreamReRoute _reRoute;
        private Mock<IOcelotLoggerFactory> _loggerFactory;
        private Mock<IDiscoveryClient> _discoveryClient;
        private Mock<IOcelotLogger> _logger;
        private IServiceProvider _provider;
        private IServiceCollection _collection;

        public ServiceProviderFactoryTests()
        {
            _loggerFactory = new Mock<IOcelotLoggerFactory>();
            _logger = new Mock<IOcelotLogger>();
            _discoveryClient = new Mock<IDiscoveryClient>();
            _collection = new ServiceCollection();
            _provider = _collection.BuildServiceProvider();
            _factory = new ServiceDiscoveryProviderFactory(_loggerFactory.Object, _discoveryClient.Object, _provider);
        }
        
        [Fact]
        public void should_return_no_service_provider()
        {
            var serviceConfig = new ServiceProviderConfigurationBuilder()
                .Build();

            var reRoute = new DownstreamReRouteBuilder().Build();

            this.Given(x => x.GivenTheReRoute(serviceConfig, reRoute))
                .When(x => x.WhenIGetTheServiceProvider())
                .Then(x => x.ThenTheServiceProviderIs<ConfigurationServiceProvider>())
                .BDDfy();
        }

        [Fact]
        public void should_return_list_of_configuration_services()
        {
            var serviceConfig = new ServiceProviderConfigurationBuilder()
                .Build();

            var downstreamAddresses = new List<DownstreamHostAndPort>()
            {
                new DownstreamHostAndPort("asdf.com", 80),
                new DownstreamHostAndPort("abc.com", 80)
            };

            var reRoute = new DownstreamReRouteBuilder().WithDownstreamAddresses(downstreamAddresses).Build();

            this.Given(x => x.GivenTheReRoute(serviceConfig, reRoute))
                .When(x => x.WhenIGetTheServiceProvider())
                .Then(x => x.ThenTheServiceProviderIs<ConfigurationServiceProvider>())
                .Then(x => ThenTheFollowingServicesAreReturned(downstreamAddresses))
                .BDDfy();
        }

        [Fact]
        public void should_call_delegate()
        {
            var reRoute = new DownstreamReRouteBuilder()
                .WithServiceName("product")
                .WithUseServiceDiscovery(true)
                .Build();

            var serviceConfig = new ServiceProviderConfigurationBuilder()
                .Build();

            this.Given(x => x.GivenTheReRoute(serviceConfig, reRoute))
                .And(x => GivenAFakeDelegate())
                .When(x => x.WhenIGetTheServiceProvider())
                .Then(x => x.ThenTheDelegateIsCalled())
                .BDDfy();
        }

        [Fact]
        public void should_return_service_fabric_provider()
        {
            var reRoute = new DownstreamReRouteBuilder()
                .WithServiceName("product")
                .WithUseServiceDiscovery(true)
                .Build();

            var serviceConfig = new ServiceProviderConfigurationBuilder()
                .WithType("ServiceFabric")
                .Build();

            this.Given(x => x.GivenTheReRoute(serviceConfig, reRoute))
                .When(x => x.WhenIGetTheServiceProvider())
                .Then(x => x.ThenTheServiceProviderIs<ServiceFabricServiceDiscoveryProvider>())
                .BDDfy();
        }

        [Fact]
        public void should_return_eureka_provider()
        {
            var reRoute = new DownstreamReRouteBuilder()
                .WithServiceName("product")
                .WithUseServiceDiscovery(true)
                .Build();

            var serviceConfig = new ServiceProviderConfigurationBuilder()
                .WithType("Eureka")
                .Build();

            this.Given(x => x.GivenTheReRoute(serviceConfig, reRoute))
                .When(x => x.WhenIGetTheServiceProvider())
                .Then(x => x.ThenTheServiceProviderIs<EurekaServiceDiscoveryProvider>())
                .BDDfy();
        }

        private void GivenAFakeDelegate()
        {
            ServiceDiscoveryFinderDelegate fake = (provider, config, name) => new Fake();
            _collection.AddSingleton(fake);
            _provider = _collection.BuildServiceProvider();
            _factory = new ServiceDiscoveryProviderFactory(_loggerFactory.Object, _discoveryClient.Object, _provider);
        }

        class Fake : IServiceDiscoveryProvider
        {
            public Task<List<Service>> Get()
            {
                return null;
            }
        }

        private void ThenTheDelegateIsCalled()
        {
            _result.GetType().Name.ShouldBe("Fake");
        }

        private void ThenTheFollowingServicesAreReturned(List<DownstreamHostAndPort> downstreamAddresses)
        {
            var result = (ConfigurationServiceProvider)_result;
            var services = result.Get().Result;
            
            for (int i = 0; i < services.Count; i++)
            {
                var service = services[i];
                var downstreamAddress = downstreamAddresses[i];

                service.HostAndPort.DownstreamHost.ShouldBe(downstreamAddress.Host);
                service.HostAndPort.DownstreamPort.ShouldBe(downstreamAddress.Port);
            }
        }

        private void GivenTheReRoute(ServiceProviderConfiguration serviceConfig, DownstreamReRoute reRoute)
        {
            _serviceConfig = serviceConfig;
            _reRoute = reRoute;
        }

        private void WhenIGetTheServiceProvider()
        {
            _result = _factory.Get(_serviceConfig, _reRoute);
        }

        private void ThenTheServiceProviderIs<T>()
        {
            _result.ShouldBeOfType<T>();
        }
    }
}
