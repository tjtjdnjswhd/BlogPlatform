using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BlogPlatform.Api.IntegrationTest
{
    public class WebApplicationFactoryFixture
    {
        public WebApplicationFactory<Program> ApplicationFactory { get; set; }

        private bool _inited = false;

        public WebApplicationFactoryFixture()
        {
            ApplicationFactory = new();
        }

        public void Init(Func<WebApplicationFactory<Program>, WebApplicationFactory<Program>> initFunc)
        {
            if (_inited)
            {
                return;
            }

            ApplicationFactory = initFunc(ApplicationFactory);
            _inited = true;
        }

        public void Configure(Action<IWebHostBuilder> configureFunc)
        {
            ApplicationFactory = ApplicationFactory.WithWebHostBuilder(configureFunc);
        }
    }
}
