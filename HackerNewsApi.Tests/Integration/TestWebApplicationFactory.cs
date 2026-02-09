using HackerNewsApi.Clients;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace HackerNewsApi.Tests.Integration
{
    public class TestWebApplicationFactory: WebApplicationFactory<Program>
    {
        public Mock<IHackerNewsClient> HackerNewsClientMock { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // delete real client
                var descriptor = services.Single(
                    d => d.ServiceType == typeof(IHackerNewsClient));

                services.Remove(descriptor);

                // add mock
                services.AddSingleton(HackerNewsClientMock.Object);
            });
        }
    }
}
