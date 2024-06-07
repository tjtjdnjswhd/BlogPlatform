using BlogPlatform.EFCore;
using BlogPlatform.EFCore.Models;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using Moq;

using Xunit.Abstractions;

namespace BlogPlatform.Api.Tests.Identity
{
    public class IdentityServiceTestsSetUp : IDisposable
    {
        public static string SuccessPassword => "successPassword";

        public static string SuccessPasswordHash => "successPasswordHash";

        public BlogPlatformDbContext DbContext { get; }

        public User BasicOnlyUser { get; }

        public User OAuthOnlyUser { get; }

        public User BasicOAuthUser { get; }

        public OAuthProvider OAuthProvider { get; }

        public IPasswordHasher<BasicAccount> PasswordHasher { get; }

        public IdentityServiceTestsSetUp(DbContextMySqlMigrateFixture migrateFixture, ITestOutputHelper testOutputHelper)
        {
            DbContextOptions<BlogPlatformDbContext> options = migrateFixture.Init("IdentityServiceTests");

            DbContextOptionsBuilder<BlogPlatformDbContext> optionsBuilder = new(options);
            optionsBuilder.EnableSensitiveDataLogging();
            optionsBuilder.EnableDetailedErrors();
            optionsBuilder.LogTo(testOutputHelper.WriteLine, Microsoft.Extensions.Logging.LogLevel.Information);

            DbContext = new(optionsBuilder.Options);

            BasicOnlyUser = new("BasicUserName", "BasicEmail@email.com");
            DbContext.Users.Add(BasicOnlyUser);
            DbContext.SaveChanges();

            BasicAccount basicAccount = new("accountId", SuccessPasswordHash, BasicOnlyUser.Id);
            DbContext.BasicAccounts.Add(basicAccount);
            DbContext.SaveChanges();

            OAuthOnlyUser = new("OAuthUserName", "OAuthEmail@email.com");
            DbContext.Users.Add(OAuthOnlyUser);
            DbContext.SaveChanges();

            OAuthProvider = new("testProvider");
            DbContext.OAuthProviders.Add(OAuthProvider);
            DbContext.SaveChanges();

            OAuthAccount oAuthAccount = new("OAuthNameIdentifier", OAuthProvider.Id, OAuthOnlyUser.Id);
            DbContext.OAuthAccounts.Add(oAuthAccount);
            DbContext.SaveChanges();

            BasicOAuthUser = new("BasicOAuthUserName", "BasicOAuthEmail@email.com");
            DbContext.Users.Add(BasicOAuthUser);
            DbContext.SaveChanges();

            BasicAccount basicOAuthUserBasic = new("BasicOAuthId", SuccessPassword, BasicOAuthUser.Id);
            OAuthAccount basicOAuthUserOAuth = new("BasicOAuthNameIdentifier", OAuthProvider.Id, BasicOAuthUser.Id);

            DbContext.Add(basicOAuthUserBasic);
            DbContext.Add(basicOAuthUserOAuth);
            DbContext.SaveChanges();

            Mock<IPasswordHasher<BasicAccount>> passwordHasher = new();
            passwordHasher.Setup(p => p.HashPassword(It.IsAny<BasicAccount>(), SuccessPassword))
                          .Returns(SuccessPasswordHash);

            passwordHasher.Setup(p => p.HashPassword(It.IsAny<BasicAccount>(), It.IsNotIn(SuccessPassword)))
                          .Returns("OtherPasswordHash");

            passwordHasher.Setup(p => p.VerifyHashedPassword(It.IsAny<BasicAccount>(), SuccessPasswordHash, SuccessPassword))
                          .Returns(PasswordVerificationResult.Success);

            passwordHasher.Setup(p => p.VerifyHashedPassword(It.IsAny<BasicAccount>(), SuccessPasswordHash, It.IsNotIn(SuccessPassword)))
                         .Returns(PasswordVerificationResult.Failed);

            PasswordHasher = passwordHasher.Object;
        }

        public void Dispose()
        {
            foreach (var tableName in DbContext.Model.GetEntityTypes().Select(e => e.GetTableName()).Where(t => !string.IsNullOrWhiteSpace(t)))
            {
#pragma warning disable EF1002 // Risk of vulnerability to SQL injection.
                DbContext.Database.ExecuteSqlRaw($"SET FOREIGN_KEY_CHECKS = 0; TRUNCATE TABLE {tableName}; SET FOREIGN_KEY_CHECKS = 1;");
#pragma warning restore EF1002 // Risk of vulnerability to SQL injection.
            }
        }
    }
}