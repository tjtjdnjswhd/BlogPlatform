using BlogPlatform.EFCore;
using BlogPlatform.EFCore.Extensions;
using BlogPlatform.EFCore.Models;
using BlogPlatform.EFCore.Models.Abstractions;
using BlogPlatform.Shared.Identity.Models;
using BlogPlatform.Shared.Identity.Options;
using BlogPlatform.Shared.Identity.Services;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

using Moq;

using System.Security.Claims;

using Xunit.Abstractions;

using SetUp = BlogPlatform.Api.Tests.Identity.IdentityServiceTestsSetUp;

namespace BlogPlatform.Api.Tests.Identity
{
    public class IdentityServiceTests : IDisposable, IClassFixture<DbContextMySqlMigrateFixture>
    {
        private readonly SetUp _setUp;
        private readonly ITestOutputHelper _outputHelper;

        public IdentityServiceTests(DbContextMySqlMigrateFixture migrateFixture, ITestOutputHelper outputHelper)
        {
            _setUp = new(migrateFixture, outputHelper);
            _outputHelper = outputHelper;
        }

        [Fact]
        public async Task LoginAsync_Basic_Success()
        {
            // Arrange
            IdentityService identityService = CreateIdentityService();
            BasicAccount basicAccount = _setUp.BasicOnlyUser.BasicAccounts.First();
            BasicLoginInfo basicLoginInfo = new(basicAccount.AccountId, SetUp.SuccessPassword);

            // Act
            (ELoginResult loginResult, User? user) = await identityService.LoginAsync(basicLoginInfo);

            // Assert
            Assert.Equal(ELoginResult.Success, loginResult);
            Assert.NotNull(user);
            Assert.Equal(_setUp.BasicOnlyUser.Id, user.Id);
        }

        [Fact]
        public async Task LoginAsync_Basic_NotFound()
        {
            // Arrange
            IdentityService identityService = CreateIdentityService();
            BasicLoginInfo basicLoginInfo = new("NotExistId", SetUp.SuccessPassword);

            // Act
            (ELoginResult loginResult, User? user) = await identityService.LoginAsync(basicLoginInfo);

            // Assert
            Assert.Equal(ELoginResult.NotFound, loginResult);
            Assert.Null(user);
        }

        [Fact]
        public async Task LoginAsync_Basic_WrongPassword()
        {
            // Arrange
            IdentityService identityService = CreateIdentityService();
            BasicAccount basicAccount = _setUp.BasicOnlyUser.BasicAccounts.First();
            BasicLoginInfo basicLoginInfo = new(basicAccount.AccountId, "WrongPassword");

            // Act
            (ELoginResult loginResult, User? user) = await identityService.LoginAsync(basicLoginInfo);

            // Assert
            Assert.Equal(ELoginResult.WrongPassword, loginResult);
            Assert.Null(user);
        }

        [Fact]
        public async Task LoginAsync_OAuth_Success()
        {
            // Arrange
            IdentityService identityService = CreateIdentityService();
            OAuthAccount oAuthAccount = _setUp.OAuthOnlyUser.OAuthAccounts.First();
            OAuthLoginInfo oAuthLoginInfo = new(oAuthAccount.Provider.Name, oAuthAccount.NameIdentifier);

            // Act
            (ELoginResult loginResult, User? user) = await identityService.LoginAsync(oAuthLoginInfo);

            // Assert
            Assert.Equal(ELoginResult.Success, loginResult);
            Assert.NotNull(user);
            Assert.Equal(_setUp.OAuthOnlyUser.Id, user.Id);
        }

        [Theory]
        [InlineData("NotExistProvider", null)]
        [InlineData(null, "NotExistNameIdentifier")]
        [InlineData("NotExistProvider", "NotExistNameIdentifier")]
        public async Task LoginAsync_OAuth_NotFound(string? provider, string? nameIdentifier)
        {
            // Arrange
            OAuthAccount oAuthAccount = _setUp.OAuthOnlyUser.OAuthAccounts.First();
            provider ??= oAuthAccount.Provider.Name;
            nameIdentifier ??= oAuthAccount.NameIdentifier;

            IdentityService identityService = CreateIdentityService();
            OAuthLoginInfo oAuthLoginInfo = new(provider, nameIdentifier);

            // Act
            (ELoginResult loginResult, User? user) = await identityService.LoginAsync(oAuthLoginInfo);

            // Assert
            Assert.Equal(ELoginResult.NotFound, loginResult);
            Assert.Null(user);
        }

        [Fact]
        public async Task SignUpAsync_Basic_Success()
        {
            // Arrange
            IdentityService identityService = CreateIdentityService();
            BasicSignUpInfo basicSignUpInfo = new("NewAccountId", SetUp.SuccessPassword, "newName", "newEmail@email.com");
            int userCount = _setUp.DbContext.Users.Count();

            // Act
            (ESignUpResult signUpResult, User? user) = await identityService.SignUpAsync(basicSignUpInfo);

            // Assert
            Assert.Equal(ESignUpResult.Success, signUpResult);
            Assert.NotNull(user);
            Assert.Equal(userCount + 1, _setUp.DbContext.Users.Count());
            Assert.NotEqual(_setUp.BasicOnlyUser.Id, user!.Id);
            Assert.Equal(basicSignUpInfo.Name, user.Name);
            Assert.Equal(basicSignUpInfo.Email, user.Email);
        }

        [Theory]
        [InlineData(ESignUpResult.UserIdAlreadyExists, null, "newName", "newEmail")]
        [InlineData(ESignUpResult.EmailAlreadyExists, "NewAccountId", "newName", null)]
        [InlineData(ESignUpResult.NameAlreadyExists, "NewAccountId", null, "newEmail")]
        public async Task SignUpAsync_Basic_Fail(ESignUpResult expectedResult, string? accountId, string? name, string? email)
        {
            // Arrange
            User basicOnlyUser = _setUp.BasicOnlyUser;
            BasicAccount basicAccount = _setUp.BasicOnlyUser.BasicAccounts.First();
            accountId ??= basicAccount.AccountId;
            email ??= basicOnlyUser.Email;
            name ??= basicOnlyUser.Name;

            IdentityService identityService = CreateIdentityService();
            BasicSignUpInfo basicSignUpInfo = new(accountId, SetUp.SuccessPassword, name, email);
            int userCount = _setUp.DbContext.Users.Count();

            // Act
            (ESignUpResult signUpResult, User? user) = await identityService.SignUpAsync(basicSignUpInfo);

            // Assert
            Assert.Equal(expectedResult, signUpResult);
            Assert.Null(user);
            Assert.Equal(userCount, _setUp.DbContext.Users.Count());
        }

        [Fact]
        public async Task SignUpAsync_OAuth_Success()
        {
            // Arrange
            IdentityService identityService = CreateIdentityService();

            OAuthSignUpInfo oAuthSignUpInfo = new("oauthEmail", "oauthName", _setUp.OAuthProvider.Name, "newNameIdentifier");
            int userCount = _setUp.DbContext.Users.Count();

            // Act
            (ESignUpResult signUpResult, User? user) = await identityService.SignUpAsync(oAuthSignUpInfo);

            // Assert
            Assert.Equal(ESignUpResult.Success, signUpResult);
            Assert.NotNull(user);
            Assert.Equal(userCount + 1, _setUp.DbContext.Users.Count());
            Assert.NotEqual(_setUp.BasicOnlyUser.Id, user!.Id);
            Assert.Equal(oAuthSignUpInfo.Name, user.Name);
            Assert.Equal(oAuthSignUpInfo.Email, user.Email);
        }

        [Theory]
        [InlineData(ESignUpResult.ProviderNotFound, "Google", "newNameIdentifier", "otherOauthName", "otherOauthEmail@email.com")]
        [InlineData(ESignUpResult.OAuthAlreadyExists, null, null, "otherOauthName", "otherOauthEmail@email.com")]
        [InlineData(ESignUpResult.NameAlreadyExists, null, "newNameIdentifier", null, "otherOauthEmail@email.com")]
        [InlineData(ESignUpResult.EmailAlreadyExists, null, "newNameIdentifier", "otherOauthName", null)]
        public async Task SignUpAsync_OAuth_Fail(ESignUpResult expectedResult, string? provider, string? nameIdentifier, string? name, string? email)
        {
            // Arrange
            User oauthOnlyUser = _setUp.OAuthOnlyUser;
            OAuthAccount oAuthAccount = oauthOnlyUser.OAuthAccounts.First();

            provider ??= oAuthAccount.Provider.Name;
            nameIdentifier ??= oAuthAccount.NameIdentifier;
            name ??= oauthOnlyUser.Name;
            email ??= oauthOnlyUser.Email;

            IdentityService identityService = CreateIdentityService();
            OAuthSignUpInfo oAuthSignUpInfo = new(name, email, provider, nameIdentifier);
            int userCount = _setUp.DbContext.Users.Count();

            // Act
            (ESignUpResult signUpResult, User? user) = await identityService.SignUpAsync(oAuthSignUpInfo);

            // Assert
            Assert.Equal(expectedResult, signUpResult);
            Assert.Null(user);
            Assert.Equal(userCount, _setUp.DbContext.Users.Count());
        }

        [Fact]
        public async Task AddOAuthAsync_Success()
        {
            // Arrange
            User user = _setUp.BasicOnlyUser;

            OAuthLoginInfo oAuthLoginInfo = new(_setUp.OAuthProvider.Name, "basicOnlyNameIdentifier");

            IdentityService identityService = CreateIdentityService();

            int oauthAccountCount = _setUp.DbContext.OAuthAccounts.Count();

            // Act
            EAddOAuthResult addResult = await identityService.AddOAuthAsync(user.Id, oAuthLoginInfo);

            // Assert
            Assert.Equal(EAddOAuthResult.Success, addResult);
            Assert.Equal(oauthAccountCount + 1, _setUp.DbContext.OAuthAccounts.Count());
        }

        [Fact]
        public async Task AddOAuthAsync_UserNotFound()
        {
            // Arrange
            OAuthLoginInfo oAuthLoginInfo = new(_setUp.OAuthProvider.Name, "basicOnlyNameIdentifier");

            int userId = 123456789;

            IdentityService identityService = CreateIdentityService();

            int oauthAccountCount = _setUp.DbContext.OAuthAccounts.Count();

            // Act
            EAddOAuthResult addResult = await identityService.AddOAuthAsync(userId, oAuthLoginInfo);

            // Assert
            Assert.Equal(EAddOAuthResult.UserNotFound, addResult);
            Assert.Equal(oauthAccountCount, _setUp.DbContext.OAuthAccounts.Count());
        }

        [Fact]
        public async Task AddOAuthAsync_ProviderNotFound()
        {
            // Arrange
            User user = _setUp.BasicOnlyUser;

            OAuthLoginInfo oAuthLoginInfo = new("Google", "basicOnlyNameIdentifier");

            IdentityService identityService = CreateIdentityService();

            int oauthAccountCount = _setUp.DbContext.OAuthAccounts.Count();

            // Act
            EAddOAuthResult addResult = await identityService.AddOAuthAsync(user.Id, oAuthLoginInfo);

            // Assert
            Assert.Equal(EAddOAuthResult.ProviderNotFound, addResult);
            Assert.Equal(oauthAccountCount, _setUp.DbContext.OAuthAccounts.Count());
        }

        [Fact]
        public async Task AddOAuthAsync_OAuthAlreadyExists()
        {
            // Arrange
            User user = _setUp.BasicOnlyUser;

            OAuthLoginInfo oAuthLoginInfo = new(_setUp.OAuthProvider.Name, _setUp.OAuthOnlyUser.OAuthAccounts.First().NameIdentifier);

            IdentityService identityService = CreateIdentityService();

            int oauthAccountCount = _setUp.DbContext.OAuthAccounts.Count();

            // Act
            EAddOAuthResult addResult = await identityService.AddOAuthAsync(user.Id, oAuthLoginInfo);

            // Assert
            Assert.Equal(EAddOAuthResult.OAuthAlreadyExists, addResult);
            Assert.Equal(oauthAccountCount, _setUp.DbContext.OAuthAccounts.Count());
        }

        [Fact]
        public async Task AddOAuthAsync_UserAlreadyHasOAuth()
        {
            // Arrange
            User user = _setUp.OAuthOnlyUser;

            OAuthLoginInfo oAuthLoginInfo = new(_setUp.OAuthProvider.Name, "otherNameIdentifier");

            IdentityService identityService = CreateIdentityService();

            int oauthAccountCount = _setUp.DbContext.OAuthAccounts.Count();

            // Act
            EAddOAuthResult addResult = await identityService.AddOAuthAsync(user.Id, oAuthLoginInfo);

            // Assert
            Assert.Equal(EAddOAuthResult.UserAlreadyHasOAuth, addResult);
            Assert.Equal(oauthAccountCount, _setUp.DbContext.OAuthAccounts.Count());
        }

        [Fact]
        public async Task RemoveOAuthAsync_Success()
        {
            // Arrange
            User user = _setUp.BasicOAuthUser;

            IdentityService identityService = CreateIdentityService();
            int oauthAccountCount = _setUp.DbContext.OAuthAccounts.Count();

            // Act
            ERemoveOAuthResult removeResult = await identityService.RemoveOAuthAsync(user.Id, _setUp.OAuthProvider.Name);

            // Assert
            Assert.Equal(ERemoveOAuthResult.Success, removeResult);
            Assert.Equal(oauthAccountCount - 1, _setUp.DbContext.OAuthAccounts.Count());
        }

        [Fact]
        public async Task RemoveOAuthAsync_HasSingleAccount()
        {
            // Arrange
            User user = _setUp.OAuthOnlyUser;

            IdentityService identityService = CreateIdentityService();
            int oauthAccountCount = _setUp.DbContext.OAuthAccounts.Count();

            // Act
            ERemoveOAuthResult removeResult = await identityService.RemoveOAuthAsync(user.Id, _setUp.OAuthProvider.Name);

            // Assert
            Assert.Equal(ERemoveOAuthResult.HasSingleAccount, removeResult);
            Assert.Equal(oauthAccountCount, _setUp.DbContext.OAuthAccounts.Count());
        }

        [Fact]
        public async Task RemoveOAuthAsync_UserNotFound()
        {
            // Arrange
            int userId = 123456789;

            IdentityService identityService = CreateIdentityService();
            int oauthAccountCount = _setUp.DbContext.OAuthAccounts.Count();

            // Act
            ERemoveOAuthResult removeResult = await identityService.RemoveOAuthAsync(userId, _setUp.OAuthProvider.Name);

            // Assert
            Assert.Equal(ERemoveOAuthResult.UserNotFound, removeResult);
            Assert.Equal(oauthAccountCount, _setUp.DbContext.OAuthAccounts.Count());
        }

        [Fact]
        public async Task RemoveOAuthAsync_OAuthNotFound()
        {
            // Arrange
            User user = _setUp.BasicOAuthUser;
            IdentityService identityService = CreateIdentityService();
            int oauthAccountCount = _setUp.DbContext.OAuthAccounts.Count();

            // Act
            ERemoveOAuthResult removeResult = await identityService.RemoveOAuthAsync(user.Id, "NotExistProvider");

            // Assert
            Assert.Equal(ERemoveOAuthResult.OAuthNotFound, removeResult);
            Assert.Equal(oauthAccountCount, _setUp.DbContext.OAuthAccounts.Count());
        }

        [Fact]
        public async Task ChangePasswordAsync_Success()
        {
            // Arrange
            User user = _setUp.BasicOnlyUser;
            BasicAccount basicAccount = user.BasicAccounts.First();
            basicAccount.IsPasswordChangeRequired = true;
            _setUp.DbContext.SaveChanges();

            string oldPasswordHash = basicAccount.PasswordHash;
            string newPassword = "newPassword";

            IdentityService identityService = CreateIdentityService();

            // Act
            EChangePasswordResult result = await identityService.ChangePasswordAsync(new(basicAccount.AccountId, SetUp.SuccessPassword, newPassword));

            // Assert
            Assert.Equal(EChangePasswordResult.Success, result);
            _setUp.DbContext.Entry(basicAccount).Reload();
            Assert.NotEqual(oldPasswordHash, basicAccount.PasswordHash);
            Assert.False(basicAccount.IsPasswordChangeRequired);
        }

        [Fact]
        public async Task ChangePasswordAsync_WrongPassword()
        {
            // Arrange
            User user = _setUp.BasicOnlyUser;
            BasicAccount basicAccount = user.BasicAccounts.First();
            basicAccount.IsPasswordChangeRequired = true;
            _setUp.DbContext.SaveChanges();

            string newPassword = "newPassword";

            IdentityService identityService = CreateIdentityService();

            // Act
            EChangePasswordResult result = await identityService.ChangePasswordAsync(new(basicAccount.AccountId, "WrongPassword", newPassword));

            // Assert
            Assert.Equal(EChangePasswordResult.WrongPassword, result);
            _setUp.DbContext.Entry(basicAccount).Reload();
            Assert.Equal(SetUp.SuccessPasswordHash, basicAccount.PasswordHash);
            Assert.True(basicAccount.IsPasswordChangeRequired);
        }

        [Fact]
        public async Task ChangePasswordAsync_BasicAccountNotFound()
        {
            // Arrange
            IdentityService identityService = CreateIdentityService();

            // Act
            EChangePasswordResult result = await identityService.ChangePasswordAsync(new("notexist", "currentPW", "newPW"));

            // Assert
            Assert.Equal(EChangePasswordResult.BasicAccountNotFound, result);
        }

        [Fact]
        public async Task ChangeNameAsync_Success()
        {
            // Arrange
            User user = _setUp.BasicOnlyUser;
            string newName = "newName";
            IdentityService identityService = CreateIdentityService();

            // Act
            EChangeNameResult result = await identityService.ChangeNameAsync(user.Id, newName);

            // Assert
            Assert.Equal(EChangeNameResult.Success, result);
            _setUp.DbContext.Entry(user).Reload();
            Assert.Equal(newName, user.Name);
        }

        [Fact]
        public async Task ChangeNameAsync_NameDuplicate()
        {
            // Arrange
            User user = _setUp.BasicOnlyUser;
            string newName = _setUp.BasicOAuthUser.Name;
            IdentityService identityService = CreateIdentityService();

            // Act
            EChangeNameResult result = await identityService.ChangeNameAsync(user.Id, newName);

            // Assert
            Assert.Equal(EChangeNameResult.NameDuplicate, result);
            _setUp.DbContext.Entry(user).Reload();
            Assert.NotEqual(newName, user.Name);
        }

        [Fact]
        public async Task ResetPasswordAsync_Success()
        {
            // Arrange
            User user = _setUp.BasicOnlyUser;
            BasicAccount basicAccount = user.BasicAccounts.First();
            IdentityService identityService = CreateIdentityService();

            // Act
            string? newPassword = await identityService.ResetPasswordAsync(user.Email);

            // Assert
            Assert.NotNull(newPassword);
            _setUp.DbContext.Entry(basicAccount).Reload();
            Assert.NotEqual(SetUp.SuccessPasswordHash, basicAccount.PasswordHash);
            Assert.True(basicAccount.IsPasswordChangeRequired);
        }

        [Fact]
        public async Task ResetPasswordAsync_Fail()
        {
            // Arrange
            IdentityService identityService = CreateIdentityService();

            // Act
            string? newPassword = await identityService.ResetPasswordAsync("notExistEmail");

            // Assert
            Assert.Null(newPassword);
        }

        [Fact]
        public async Task ChangeEmailAsync_Success()
        {
            // Arrange
            User user = _setUp.BasicOnlyUser;
            string newEmail = "newEmail";
            IdentityService identityService = CreateIdentityService();

            // Act
            EChangeEmailResult result = await identityService.ChangeEmailAsync(user.Id, newEmail);

            // Assert
            Assert.Equal(EChangeEmailResult.Success, result);
            _setUp.DbContext.Entry(user).Reload();
            Assert.Equal(newEmail, user.Email);
        }

        [Fact]
        public async Task ChangeEmailAsync_EmailDuplicate()
        {
            // Arrange
            User user = _setUp.BasicOnlyUser;
            string newEmail = _setUp.BasicOAuthUser.Email;
            IdentityService identityService = CreateIdentityService();

            // Act
            EChangeEmailResult result = await identityService.ChangeEmailAsync(user.Id, newEmail);

            // Assert
            Assert.Equal(EChangeEmailResult.EmailDuplicate, result);
            Assert.NotEqual(newEmail, user.Email);
            _setUp.DbContext.Entry(user).Reload();
        }

        [Fact]
        public async Task FindAccountIdAsync_Success()
        {
            // Arrange
            User user = _setUp.BasicOnlyUser;
            IdentityService identityService = CreateIdentityService();

            // Act
            string? accountId = await identityService.FindAccountIdAsync(user.Email);

            // Assert
            Assert.NotNull(accountId);
            Assert.Equal(user.BasicAccounts.First().AccountId, accountId);
        }

        [Fact]
        public async Task FindAccountIdAsync_Fail()
        {
            // Arrange
            IdentityService identityService = CreateIdentityService();

            // Act
            string? accountId = await identityService.FindAccountIdAsync("notExistEmail");

            // Assert
            Assert.Null(accountId);
        }

        [Fact]
        public async Task WithDrawAsync_Success()
        {
            // Arrange
            User user = _setUp.BasicOnlyUser;
            IdentityService identityService = CreateIdentityService();

            // Act
            EWithDrawResult result = await identityService.WithDrawAsync(user.Id);

            // Assert
            Assert.Equal(EWithDrawResult.Success, result);
            Assert.Equal(1, user.SoftDeleteLevel);
            Assert.True(user.SoftDeletedAt != EntityBase.DefaultSoftDeletedAt);
            Assert.True(user.BasicAccounts.All(b => b.SoftDeleteLevel > 1));
        }

        [Fact]
        public async Task WithDrawAsync_UserNotFound()
        {
            // Arrange
            User user = _setUp.BasicOnlyUser;
            int userId = 123456789;
            IdentityService identityService = CreateIdentityService();

            // Act
            EWithDrawResult result = await identityService.WithDrawAsync(userId);

            // Assert
            Assert.Equal(EWithDrawResult.UserNotFound, result);
            Assert.Equal(0, user.SoftDeleteLevel);
            Assert.True(user.IsSoftDeletedAtDefault());
            Assert.True(user.BasicAccounts.All(b => b.SoftDeleteLevel == 0));
        }

        [Fact]
        public async Task CancelWithDrawAsync_Success()
        {
            // Arrange
            User user = _setUp.BasicOnlyUser;
            user.SoftDeleteLevel = 1;
            user.SoftDeletedAt = DateTimeOffset.UtcNow;
            BasicAccount basicAccount = user.BasicAccounts.First();
            basicAccount.SoftDeleteLevel = 2;
            basicAccount.SoftDeletedAt = DateTimeOffset.UtcNow;
            _setUp.DbContext.SaveChanges();

            IdentityService identityService = CreateIdentityService();

            // Act
            ECancelWithDrawResult result = await identityService.CancelWithDrawAsync(new BasicLoginInfo(basicAccount.AccountId, SetUp.SuccessPassword));

            // Assert
            Assert.Equal(ECancelWithDrawResult.Success, result);
            Assert.Equal(0, user.SoftDeleteLevel);
            Assert.True(user.SoftDeletedAt == EntityBase.DefaultSoftDeletedAt);
            Assert.True(user.BasicAccounts.All(b => b.SoftDeleteLevel == 0));
        }

        [Fact]
        public async Task CancelWithDrawAsync_AccountNotFound()
        {
            // Arrange
            IdentityService identityService = CreateIdentityService();

            // Act
            ECancelWithDrawResult result = await identityService.CancelWithDrawAsync(new BasicLoginInfo("notexist", SetUp.SuccessPassword));

            // Assert
            Assert.Equal(ECancelWithDrawResult.AccountNotFound, result);
        }

        [Fact]
        public async Task CancelWithDrawAsync_Expired()
        {
            // Arrange
            User user = _setUp.BasicOnlyUser;
            user.SoftDeleteLevel = 1;
            user.SoftDeletedAt = DateTimeOffset.UtcNow.AddHours(-25);
            BasicAccount basicAccount = user.BasicAccounts.First();
            user.BasicAccounts.First().SoftDeleteLevel = 2;
            user.BasicAccounts.First().SoftDeletedAt = DateTimeOffset.UtcNow.AddHours(-25);
            _setUp.DbContext.SaveChanges();

            IdentityService identityService = CreateIdentityService();

            // Act
            ECancelWithDrawResult result = await identityService.CancelWithDrawAsync(new BasicLoginInfo(basicAccount.AccountId, SetUp.SuccessPassword));

            // Assert
            Assert.Equal(ECancelWithDrawResult.Expired, result);
            Assert.Equal(1, user.SoftDeleteLevel);
            Assert.True(user.SoftDeletedAt != EntityBase.DefaultSoftDeletedAt);
            Assert.True(user.BasicAccounts.All(b => b.SoftDeleteLevel == 2));
        }

        [Fact]
        public async Task CancelWithDrawAsync_WithDrawNotRequested()
        {
            // Arrange
            User user = _setUp.BasicOnlyUser;
            BasicAccount basicAccount = user.BasicAccounts.First();
            IdentityService identityService = CreateIdentityService();

            // Act
            ECancelWithDrawResult result = await identityService.CancelWithDrawAsync(new BasicLoginInfo(basicAccount.AccountId, SetUp.SuccessPassword));

            // Assert
            Assert.Equal(ECancelWithDrawResult.WithDrawNotRequested, result);
            Assert.Equal(0, user.SoftDeleteLevel);
            Assert.True(user.IsSoftDeletedAtDefault());
            Assert.True(user.BasicAccounts.All(b => b.SoftDeleteLevel == 0));
        }

        private IdentityService CreateIdentityService(DateTimeOffset? baseTime = null)
        {
            baseTime ??= DateTimeOffset.UtcNow;

            TimeProvider timeProvider = new TestTimeProvider(baseTime.Value);
            Mock<IAuthenticationService> authenticationService = new();
            authenticationService.Setup(a => a.AuthenticateAsync(It.IsAny<HttpContext>(), It.IsAny<string>()))
                                 .ReturnsAsync(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity("authType")), "scheme")));

            XUnitLogger<IdentityService> serviceLogger = new(_outputHelper);
            XUnitLogger<CascadeSoftDeleteService> deleteLogger = new(_outputHelper);

            CascadeSoftDeleteService softDeleteService = new(_setUp.DbContext, timeProvider, deleteLogger);

            return new IdentityService(_setUp.DbContext, _setUp.PasswordHasher, softDeleteService, timeProvider, Options.Create(new IdentityServiceOptions() { UserRoleName = "User" }), serviceLogger);
        }

        public void Dispose()
        {
            _setUp.Dispose();
        }
    }
}