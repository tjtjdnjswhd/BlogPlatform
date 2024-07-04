using BlogPlatform.EFCore;
using BlogPlatform.Shared.Identity.Services.Interfaces;
using BlogPlatform.Shared.Services.Interfaces;

using Meziantou.Extensions.Logging.Xunit;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

using Moq;

using System.Text.Json.Nodes;

using Xunit.Abstractions;


namespace BlogPlatform.Api.IntegrationTest
{
    public abstract class TestBase : IClassFixture<WebApplicationFactoryFixture>
    {
        public static Dictionary<string, WebApplicationFactoryFixture> FixtureByTestClassName { get; private set; } = [];

        public WebApplicationFactory<Program> WebApplicationFactory => FixtureByTestClassName[GetType().Name].ApplicationFactory;

        public ITestOutputHelper TestOutputHelper { get; }

        public ServiceLifetime DbContextLifeTime { get; }

        protected TestBase(WebApplicationFactoryFixture applicationFactoryFixture, ITestOutputHelper testOutputHelper, string dbName, ServiceLifetime dbContextLifeTime = ServiceLifetime.Scoped)
        {
            string testClassName = GetType().Name;
            FixtureByTestClassName.TryAdd(testClassName, applicationFactoryFixture);
            TestOutputHelper = testOutputHelper;
            DbContextLifeTime = dbContextLifeTime;

            applicationFactoryFixture.Init(factory =>
            {
                var newFactory = factory.WithWebHostBuilder(builder =>
                {
                    InitWebApplicationFactory(builder, dbName);
                });
                SeedData();
                return newFactory;
            });

            XUnitLoggerProvider loggerProvider = new(TestOutputHelper, new XUnitLoggerOptions() { IncludeCategory = true, IncludeLogLevel = true });
            applicationFactoryFixture.Configure(builder =>
            {
                builder.ConfigureLogging(lb =>
                {
                    lb.ClearProviders();
                    lb.AddProvider(loggerProvider);
                });
            });
        }

        protected virtual void InitWebApplicationFactory(IWebHostBuilder builder, string dbName)
        {
            builder.ConfigureServices(services =>
            {
                JsonNode connectionStringNode = JsonNode.Parse(File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "testConnectionStrings.json"))) ?? throw new Exception();
                string connectionString = connectionStringNode["BlogPlatformDb"]?.GetValue<string>() ?? throw new Exception();
                connectionString += $"database={dbName};";

                services.RemoveAll<DbContextOptions>();

                services.RemoveAll<DbContextOptions<BlogPlatformDbContext>>();
                services.RemoveAll<BlogPlatformDbContext>();
                services.AddDbContext<BlogPlatformDbContext>(opt =>
                {
                    opt.UseMySql(connectionString, MySqlServerVersion.LatestSupportedServerVersion);
                    opt.EnableDetailedErrors();
                    opt.EnableSensitiveDataLogging();
                }, DbContextLifeTime, DbContextLifeTime);

                services.RemoveAll<DbContextOptions<BlogPlatformImgDbContext>>();
                services.RemoveAll<BlogPlatformImgDbContext>();
                services.AddDbContext<BlogPlatformImgDbContext>(opt =>
                {
                    opt.UseMySql(connectionString, MySqlServerVersion.LatestSupportedServerVersion);
                    opt.EnableDetailedErrors();
                    opt.EnableSensitiveDataLogging();
                }, DbContextLifeTime, DbContextLifeTime);

                services.RemoveAll<IMailSender>();
                services.AddScoped(_ => new Mock<IMailSender>().Object);

                services.RemoveAll<IDistributedCache>();
                services.AddDistributedMemoryCache();

                services.RemoveAll<IUserEmailService>();
                Mock<IUserEmailService> userEmailServiceMock = new();
                services.AddSingleton(userEmailServiceMock.Object);
            });
        }

        protected abstract void SeedData();
    }
}
