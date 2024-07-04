using BlogPlatform.Api.Identity.Constants;
using BlogPlatform.EFCore;
using BlogPlatform.EFCore.Models;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Xunit.Abstractions;

namespace BlogPlatform.Api.IntegrationTest.Identity
{
    public partial class IdentityTests : TestBase, ITestDataReset
    {
        public IdentityTests(WebApplicationFactoryFixture applicationFactoryFixture, ITestOutputHelper testOutputHelper) : base(applicationFactoryFixture, testOutputHelper, "integration_identity_test") { }

        protected override void SeedData()
        {
            ResetData();
        }

        public static void ResetData()
        {
            using var scope = FixtureByTestClassName[typeof(IdentityTests).Name].ApplicationFactory.Services.CreateScope();
            using BlogPlatformDbContext dbContext = Helper.GetNotLoggingDbContext<BlogPlatformDbContext>(scope.ServiceProvider);
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
                new Role(PolicyConstants.UserRolePolicy, 1),
                new Role(PolicyConstants.AdminRolePolicy, 0)
            ];

            dbContext.Roles.AddRange(roles);
            dbContext.SaveChanges();

            users[0].Roles = [roles[0]];
            users[1].Roles = [roles[0]];
            users[2].Roles = roles;

            dbContext.SaveChanges();

            List<BasicAccount> basicAccounts = [
                new BasicAccount("user1Id", passwordHasher.HashPassword(null, "user1pw"), users[0].Id),
                new BasicAccount("user2Id", passwordHasher.HashPassword(null, "user2pw"), users[1].Id),
                new BasicAccount("admin1Id", passwordHasher.HashPassword(null, "admin1pw"), users[2].Id)
            ];

            dbContext.BasicAccounts.AddRange(basicAccounts);
            dbContext.SaveChanges();
        }
    }
}
