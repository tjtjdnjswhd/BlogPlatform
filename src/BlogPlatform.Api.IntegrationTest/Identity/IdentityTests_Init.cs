using BlogPlatform.Api.Identity.Services.Interfaces;
using BlogPlatform.EFCore;
using BlogPlatform.EFCore.Models;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Moq;

using Xunit.Abstractions;

namespace BlogPlatform.Api.IntegrationTest.Identity
{
    public partial class IdentityTests : TestBase
    {
        public IdentityTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper, "integration_identity_test") { }

        protected override void SeedData()
        {
            using var scope = WebApplicationFactory.Services.CreateScope();
            using BlogPlatformDbContext dbContext = GetNotLoggingDbContext<BlogPlatformDbContext>(scope);
            IPasswordHasher<BasicAccount> passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<BasicAccount>>();

            dbContext.Database.EnsureDeleted();
            dbContext.Database.Migrate();

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
                    emailServiceMock ??= new();
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
                    emailVerifyServiceMock ??= new();
                    services.AddSingleton(emailVerifyServiceMock.Object);
                });
            });
        }
    }
}
