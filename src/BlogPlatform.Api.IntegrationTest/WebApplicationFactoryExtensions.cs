using Microsoft.AspNetCore.Mvc.Testing;

using Xunit.Abstractions;

namespace BlogPlatform.Api.IntegrationTest
{
    public static class WebApplicationFactoryExtensions
    {
        public static HttpClient CreateLoggingClient(this WebApplicationFactory<Program> applicationFactory, ITestOutputHelper testOutputHelper)
        {
            return applicationFactory.CreateDefaultClient(new HttpClientLogHandler(testOutputHelper));
        }
    }
}
