using BlogPlatform.Api.Identity.Options;
using BlogPlatform.Api.Identity.Services.Interfaces;
using BlogPlatform.Api.Services.Interfaces;
using BlogPlatform.EFCore;
using BlogPlatform.EFCore.Models;

using Meziantou.Extensions.Logging.Xunit;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

using Moq;

using System.Text.Json.Nodes;

using Xunit.Abstractions;

namespace BlogPlatform.Api.IntegrationTest.Identity
{
    public partial class IdentityTests
    {
        public WebApplicationFactory<Program> WebApplicationFactory { get; private set; }

        public ITestOutputHelper TestOutputHelper { get; }

        public IdentityTests(ITestOutputHelper testOutputHelper)
        {
            TestOutputHelper = testOutputHelper;
            XUnitLoggerProvider loggerProvider = new(testOutputHelper, new XUnitLoggerOptions() { IncludeCategory = true, IncludeLogLevel = true });
            WebApplicationFactory = new();
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
                    connectionString += "database=auth_test_blogplatform;";

                    services.RemoveAll<DbContextOptions<BlogPlatformDbContext>>();
                    services.RemoveAll<BlogPlatformDbContext>();
                    services.RemoveAll<DbContextOptions>();

                    services.AddDbContext<BlogPlatformDbContext>(opt =>
                    {
                        opt.UseMySql(connectionString, MySqlServerVersion.LatestSupportedServerVersion);
                        opt.EnableDetailedErrors();
                        opt.EnableSensitiveDataLogging();
                    });

                    services.RemoveAll<IMailSender>();
                    services.AddScoped(_ => new Mock<IMailSender>().Object);

                    services.AddDistributedMemoryCache();
                });
            });

            SeedData();
        }

        private void SeedData()
        {
            using var scope = WebApplicationFactory.Services.CreateScope();
            BlogPlatformDbContext dbContext = scope.ServiceProvider.GetRequiredService<BlogPlatformDbContext>();
            IPasswordHasher<BasicAccount> passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<BasicAccount>>();

            dbContext.Database.EnsureDeleted();
            dbContext.Database.Migrate();
            // Seed data
            List<User> users = [
                new User("user1", "user1@user.com"),
                new User("user2", "user2@user.com"),
                new User("admin1", "admin1@admin.com")
            ];

            dbContext.Users.AddRange(users);
            dbContext.SaveChanges();

            List<Role> roles = [
                new Role("User", 1),
                new Role("Admin", 0)
            ];

            dbContext.Roles.AddRange(roles);
            dbContext.SaveChanges();

            users[0].Roles.Add(roles[0]);
            users[1].Roles.Add(roles[0]);
            users[2].Roles.Add(roles[1]);

            dbContext.SaveChanges();

            List<BasicAccount> basicAccounts = [
                new BasicAccount("user1Id", passwordHasher.HashPassword(null, "user1pw"), users[0].Id),
                new BasicAccount("user2Id", passwordHasher.HashPassword(null, "user2pw"), users[1].Id),
                new BasicAccount("admin1Id", passwordHasher.HashPassword(null, "admin1pw"), users[2].Id)
            ];

            dbContext.BasicAccounts.AddRange(basicAccounts);
            dbContext.SaveChanges();

            List<OAuthProvider> oAuthProviders = [
                new OAuthProvider("Google"),
                new OAuthProvider("Naver")
            ];

            dbContext.OAuthProviders.AddRange(oAuthProviders);
            dbContext.SaveChanges();
        }

        private void SetIEmailServiceMock(Mock<IUserEmailService>? emailServiceMock = null)
        {
            WebApplicationFactory = WebApplicationFactory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IUserEmailService>();
                    if (emailServiceMock is null)
                    {
                        emailServiceMock = new();
                    }
                    services.AddSingleton(emailServiceMock.Object);
                });
            });
        }

        private void SetIEmailVerifyServiceMock(Mock<IEmailVerifyService>? emailVerifyServiceMock = null)
        {
            WebApplicationFactory = WebApplicationFactory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IEmailVerifyService>();
                    if (emailVerifyServiceMock is null)
                    {
                        emailVerifyServiceMock = new();
                    }
                    services.AddSingleton(emailVerifyServiceMock.Object);
                });
            });
        }

        private void PrintResponse(HttpResponseMessage response)
        {
            TestOutputHelper.WriteLine($"Content: {response.Content.ReadAsStringAsync().Result}");
            TestOutputHelper.WriteLine($"Headers: {response.Headers}");
        }

        private HttpClient CreateClient()
        {
            return WebApplicationFactory.CreateDefaultClient(new HttpClientRequestLogHandler(TestOutputHelper));
        }
    }
}
