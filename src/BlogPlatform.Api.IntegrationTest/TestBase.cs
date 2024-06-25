using BlogPlatform.Api.Identity.Options;
using BlogPlatform.Api.Services.Interfaces;
using BlogPlatform.EFCore;

using Meziantou.Extensions.Logging.Xunit;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

using Moq;

using System.Text.Json.Nodes;

using Xunit.Abstractions;


namespace BlogPlatform.Api.IntegrationTest
{
    public abstract class TestBase
    {
        public WebApplicationFactory<Program> WebApplicationFactory { get; set; }

        public ITestOutputHelper TestOutputHelper { get; }

        public ServiceLifetime DbContextLifeTime { get; }

        protected TestBase(ITestOutputHelper testOutputHelper, string dbName, ServiceLifetime dbContextLifeTime = ServiceLifetime.Scoped)
        {
            WebApplicationFactory = new();
            TestOutputHelper = testOutputHelper;
            DbContextLifeTime = dbContextLifeTime;
            InitWebApplicationFactory(dbName);
            SeedData();
        }

        protected virtual void InitWebApplicationFactory(string dbName)
        {
            XUnitLoggerProvider loggerProvider = new(TestOutputHelper, new XUnitLoggerOptions() { IncludeCategory = true, IncludeLogLevel = true });
            WebApplicationFactory = WebApplicationFactory.WithWebHostBuilder(cnf =>
            {
                cnf.ConfigureLogging(lb =>
                {
                    lb.ClearProviders();
                    lb.AddProvider(loggerProvider);
                });

                cnf.ConfigureServices(services =>
                {
                    services.AddOptions<JwtOptions>().Configure(options =>
                    {
                        options.AccessTokenName = Helper.ACCESS_TOKEN_NAME;
                        options.RefreshTokenName = Helper.REFRESH_TOKEN_NAME;
                    }).ValidateDataAnnotations().ValidateOnStart();

                    services.AddOptions<AccountOptions>().Configure(options =>
                    {
                        options.MinIdLength = 5;
                        options.MaxIdLength = 20;
                        options.MinNameLength = 3;
                        options.MaxNameLength = 50;
                        options.MinPasswordLength = 4;
                        options.MaxPasswordLength = int.MaxValue;
                    }).ValidateDataAnnotations().ValidateOnStart();

                    JsonNode connectionStringNode = JsonNode.Parse(File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "testConnectionStrings.json"))) ?? throw new Exception();
                    string connectionString = connectionStringNode["BlogPlatformDb"]?.GetValue<string>() ?? throw new Exception();
                    connectionString += $"database={dbName};";

                    services.RemoveAll<DbContextOptions<BlogPlatformDbContext>>();
                    services.RemoveAll<BlogPlatformDbContext>();
                    services.RemoveAll<DbContextOptions>();

                    services.AddDbContext<BlogPlatformDbContext>(opt =>
                    {
                        opt.UseMySql(connectionString, MySqlServerVersion.LatestSupportedServerVersion);
                        opt.EnableDetailedErrors();
                        opt.EnableSensitiveDataLogging();
                    }, DbContextLifeTime, DbContextLifeTime);

                    services.RemoveAll<IMailSender>();
                    services.AddScoped(_ => new Mock<IMailSender>().Object);

                    services.AddDistributedMemoryCache();
                });
            });
        }

        protected abstract void SeedData();

        protected HttpClient CreateClient()
        {
            return WebApplicationFactory.CreateDefaultClient(new HttpClientLogHandler(TestOutputHelper));
        }
    }
}
