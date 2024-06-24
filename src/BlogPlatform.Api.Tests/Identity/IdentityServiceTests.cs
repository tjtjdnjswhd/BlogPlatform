using BlogPlatform.Api.Identity.Models;
using BlogPlatform.Api.Services;
using BlogPlatform.Api.Services.Interfaces;
using BlogPlatform.EFCore;
using BlogPlatform.EFCore.Extensions;
using BlogPlatform.EFCore.Models;
using BlogPlatform.EFCore.Models.Abstractions;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

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

            OAuthSignUpInfo oAuthSignUpInfo = new(_setUp.OAuthProvider.Name, "newNameIdentifier", "oauthEmail", "oauthName");
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
            OAuthSignUpInfo oAuthSignUpInfo = new(provider, nameIdentifier, name, email);
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

            IdentityService identityService = CreateIdentityService(CreateJwtServiceMock(user.Id, true));
            Mock<HttpContext> httpContextMock = new();

            int oauthAccountCount = _setUp.DbContext.OAuthAccounts.Count();

            // Act
            EAddOAuthResult addResult = await identityService.AddOAuthAsync(httpContextMock.Object, oAuthLoginInfo);

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

            IdentityService identityService = CreateIdentityService(CreateJwtServiceMock(userId, true));
            Mock<HttpContext> httpContextMock = new();

            int oauthAccountCount = _setUp.DbContext.OAuthAccounts.Count();

            // Act
            EAddOAuthResult addResult = await identityService.AddOAuthAsync(httpContextMock.Object, oAuthLoginInfo);

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

            IdentityService identityService = CreateIdentityService(CreateJwtServiceMock(user.Id, true));
            Mock<HttpContext> httpContextMock = new();

            int oauthAccountCount = _setUp.DbContext.OAuthAccounts.Count();

            // Act
            EAddOAuthResult addResult = await identityService.AddOAuthAsync(httpContextMock.Object, oAuthLoginInfo);

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

            IdentityService identityService = CreateIdentityService(CreateJwtServiceMock(user.Id, true));
            Mock<HttpContext> httpContextMock = new();

            int oauthAccountCount = _setUp.DbContext.OAuthAccounts.Count();

            // Act
            EAddOAuthResult addResult = await identityService.AddOAuthAsync(httpContextMock.Object, oAuthLoginInfo);

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

            IdentityService identityService = CreateIdentityService(CreateJwtServiceMock(user.Id, true));
            Mock<HttpContext> httpContextMock = new();

            int oauthAccountCount = _setUp.DbContext.OAuthAccounts.Count();

            // Act
            EAddOAuthResult addResult = await identityService.AddOAuthAsync(httpContextMock.Object, oAuthLoginInfo);

            // Assert
            Assert.Equal(EAddOAuthResult.UserAlreadyHasOAuth, addResult);
            Assert.Equal(oauthAccountCount, _setUp.DbContext.OAuthAccounts.Count());
        }

        [Fact]
        public async Task RemoveOAuthAsync_Success()
        {
            // Arrange
            User user = _setUp.BasicOAuthUser;

            IdentityService identityService = CreateIdentityService(CreateJwtServiceMock(user.Id, true));
            int oauthAccountCount = _setUp.DbContext.OAuthAccounts.Count();
            ClaimsPrincipal claimsPrincipal = new();

            // Act
            ERemoveOAuthResult removeResult = await identityService.RemoveOAuthAsync(claimsPrincipal, _setUp.OAuthProvider.Name);

            // Assert
            Assert.Equal(ERemoveOAuthResult.Success, removeResult);
            Assert.Equal(oauthAccountCount - 1, _setUp.DbContext.OAuthAccounts.Count());
        }

        [Fact]
        public async Task RemoveOAuthAsync_HasSingleAccount()
        {
            // Arrange
            User user = _setUp.OAuthOnlyUser;

            IdentityService identityService = CreateIdentityService(CreateJwtServiceMock(user.Id, true));
            int oauthAccountCount = _setUp.DbContext.OAuthAccounts.Count();
            ClaimsPrincipal claimsPrincipal = new();

            // Act
            ERemoveOAuthResult removeResult = await identityService.RemoveOAuthAsync(claimsPrincipal, _setUp.OAuthProvider.Name);

            // Assert
            Assert.Equal(ERemoveOAuthResult.HasSingleAccount, removeResult);
            Assert.Equal(oauthAccountCount, _setUp.DbContext.OAuthAccounts.Count());
        }

        [Fact]
        public async Task RemoveOAuthAsync_UserNotFound()
        {
            // Arrange
            int userId = 123456789;

            IdentityService identityService = CreateIdentityService(CreateJwtServiceMock(userId, true));
            ClaimsPrincipal claimsPrincipal = new();

            int oauthAccountCount = _setUp.DbContext.OAuthAccounts.Count();

            // Act
            ERemoveOAuthResult removeResult = await identityService.RemoveOAuthAsync(claimsPrincipal, _setUp.OAuthProvider.Name);

            // Assert
            Assert.Equal(ERemoveOAuthResult.UserNotFound, removeResult);
            Assert.Equal(oauthAccountCount, _setUp.DbContext.OAuthAccounts.Count());
        }

        [Fact]
        public async Task RemoveOAuthAsync_OAuthNotFound()
        {
            // Arrange
            User user = _setUp.BasicOAuthUser;

            IdentityService identityService = CreateIdentityService(CreateJwtServiceMock(user.Id, true));
            ClaimsPrincipal claimsPrincipal = new();

            int oauthAccountCount = _setUp.DbContext.OAuthAccounts.Count();

            // Act
            ERemoveOAuthResult removeResult = await identityService.RemoveOAuthAsync(claimsPrincipal, "NotExistProvider");

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

            IdentityService identityService = CreateIdentityService(CreateJwtServiceMock(user.Id, true));
            ClaimsPrincipal claimsPrincipal = new();

            // Act
            bool result = await identityService.ChangePasswordAsync(claimsPrincipal, newPassword);

            // Assert
            Assert.True(result);
            _setUp.DbContext.Entry(basicAccount).Reload();
            Assert.NotEqual(oldPasswordHash, basicAccount.PasswordHash);
            Assert.False(basicAccount.IsPasswordChangeRequired);
        }

        [Fact]
        public async Task ChangePasswordAsync_Fail()
        {
            // Arrange
            string newPassword = "newPassword";

            int userId = 123456789;

            IdentityService identityService = CreateIdentityService(CreateJwtServiceMock(userId, true));
            ClaimsPrincipal claimsPrincipal = new();

            // Act
            bool result = await identityService.ChangePasswordAsync(claimsPrincipal, newPassword);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ChangeNameAsync_Success()
        {
            // Arrange
            User user = _setUp.BasicOnlyUser;
            string newName = "newName";


            IdentityService identityService = CreateIdentityService(CreateJwtServiceMock(user.Id, true));
            ClaimsPrincipal claimsPrincipal = new();

            // Act
            bool result = await identityService.ChangeNameAsync(claimsPrincipal, newName);

            // Assert
            Assert.True(result);
            _setUp.DbContext.Entry(user).Reload();
            Assert.Equal(newName, user.Name);
        }

        [Fact]
        public async Task ChangeNameAsync_Fail()
        {
            // Arrange
            User user = _setUp.BasicOnlyUser;
            string newName = _setUp.BasicOAuthUser.Name;

            IdentityService identityService = CreateIdentityService(CreateJwtServiceMock(user.Id, true));
            ClaimsPrincipal claimsPrincipal = new();

            // Act
            bool result = await identityService.ChangeNameAsync(claimsPrincipal, newName);

            // Assert
            Assert.False(result);
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

            IdentityService identityService = CreateIdentityService(CreateJwtServiceMock(user.Id, true));
            ClaimsPrincipal claimsPrincipal = new();

            // Act
            bool result = await identityService.ChangeEmailAsync(claimsPrincipal, newEmail);

            // Assert
            Assert.True(result);
            _setUp.DbContext.Entry(user).Reload();
            Assert.Equal(newEmail, user.Email);
        }

        [Fact]
        public async Task ChangeEmailAsync_Fail()
        {
            // Arrange
            User user = _setUp.BasicOnlyUser;
            string newEmail = _setUp.BasicOAuthUser.Email;

            IdentityService identityService = CreateIdentityService(CreateJwtServiceMock(user.Id, true));
            ClaimsPrincipal claimsPrincipal = new();

            // Act
            bool result = await identityService.ChangeEmailAsync(claimsPrincipal, newEmail);

            // Assert
            Assert.NotEqual(newEmail, user.Email);
            _setUp.DbContext.Entry(user).Reload();
            Assert.False(result);
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

            IdentityService identityService = CreateIdentityService(CreateJwtServiceMock(user.Id, true));
            ClaimsPrincipal claimsPrincipal = new();

            // Act
            bool result = await identityService.WithDrawAsync(claimsPrincipal);

            // Assert
            Assert.True(result);
            Assert.True(user.SoftDeleteLevel == 1);
            Assert.True(user.SoftDeletedAt != EntityBase.DefaultSoftDeletedAt);
            Assert.True(user.BasicAccounts.All(b => b.SoftDeleteLevel > 1));
        }

        [Fact]
        public async Task WithDrawAsync_UserNotFound()
        {
            // Arrange
            User user = _setUp.BasicOnlyUser;

            int userId = 123456789;

            IdentityService identityService = CreateIdentityService(CreateJwtServiceMock(userId, true));
            ClaimsPrincipal claimsPrincipal = new();

            // Act
            bool result = await identityService.WithDrawAsync(claimsPrincipal);

            // Assert
            Assert.False(result);
            Assert.True(user.SoftDeleteLevel == 0);
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
            user.BasicAccounts.First().SoftDeleteLevel = 2;
            user.BasicAccounts.First().SoftDeletedAt = DateTimeOffset.UtcNow;
            _setUp.DbContext.SaveChanges();

            IdentityService identityService = CreateIdentityService(CreateJwtServiceMock(user.Id, true));
            ClaimsPrincipal claimsPrincipal = new();

            // Act
            ECancelWithDrawResult result = await identityService.CancelWithDrawAsync(claimsPrincipal);

            // Assert
            Assert.Equal(ECancelWithDrawResult.Success, result);
            Assert.True(user.SoftDeleteLevel == 0);
            Assert.True(user.SoftDeletedAt == EntityBase.DefaultSoftDeletedAt);
            Assert.True(user.BasicAccounts.All(b => b.SoftDeleteLevel == 0));
        }

        [Fact]
        public async Task CancelWithDrawAsync_UserNotFound()
        {
            // Arrange
            User user = _setUp.BasicOnlyUser;
            user.SoftDeleteLevel = 1;
            user.SoftDeletedAt = DateTimeOffset.UtcNow;
            user.BasicAccounts.First().SoftDeleteLevel = 2;
            user.BasicAccounts.First().SoftDeletedAt = DateTimeOffset.UtcNow;
            _setUp.DbContext.SaveChanges();

            int userId = 123456789;

            IdentityService identityService = CreateIdentityService(CreateJwtServiceMock(userId, true));
            ClaimsPrincipal claimsPrincipal = new();

            // Act
            ECancelWithDrawResult result = await identityService.CancelWithDrawAsync(claimsPrincipal);

            // Assert
            Assert.Equal(ECancelWithDrawResult.UserNotFound, result);
            Assert.True(user.SoftDeleteLevel == 1);
            Assert.True(user.SoftDeletedAt != EntityBase.DefaultSoftDeletedAt);
            Assert.True(user.BasicAccounts.All(b => b.SoftDeleteLevel == 2));
        }

        [Fact]
        public async Task CancelWithDrawAsync_Expired()
        {
            // Arrange
            User user = _setUp.BasicOnlyUser;
            user.SoftDeleteLevel = 1;
            user.SoftDeletedAt = DateTimeOffset.UtcNow.AddHours(-25);
            user.BasicAccounts.First().SoftDeleteLevel = 2;
            user.BasicAccounts.First().SoftDeletedAt = DateTimeOffset.UtcNow.AddHours(-25);
            _setUp.DbContext.SaveChanges();

            IdentityService identityService = CreateIdentityService(CreateJwtServiceMock(user.Id, true));
            ClaimsPrincipal claimsPrincipal = new();

            // Act
            ECancelWithDrawResult result = await identityService.CancelWithDrawAsync(claimsPrincipal);

            // Assert
            Assert.Equal(ECancelWithDrawResult.Expired, result);
            Assert.True(user.SoftDeleteLevel == 1);
            Assert.True(user.SoftDeletedAt != EntityBase.DefaultSoftDeletedAt);
            Assert.True(user.BasicAccounts.All(b => b.SoftDeleteLevel == 2));
        }

        [Fact]
        public async Task CancelWithDrawAsync_WithDrawNotRequested()
        {
            // Arrange
            User user = _setUp.BasicOnlyUser;

            IdentityService identityService = CreateIdentityService(CreateJwtServiceMock(user.Id, true));
            ClaimsPrincipal claimsPrincipal = new();

            // Act
            ECancelWithDrawResult result = await identityService.CancelWithDrawAsync(claimsPrincipal);

            // Assert
            Assert.Equal(ECancelWithDrawResult.WithDrawNotRequested, result);
            Assert.True(user.SoftDeleteLevel == 0);
            Assert.True(user.IsSoftDeletedAtDefault());
            Assert.True(user.BasicAccounts.All(b => b.SoftDeleteLevel == 0));
        }

        private IdentityService CreateIdentityService(Mock<IJwtService>? jwtService = null, DateTimeOffset? baseTime = null)
        {
            jwtService ??= new();
            baseTime ??= DateTimeOffset.UtcNow;

            TimeProvider timeProvider = new TestTimeProvider(baseTime.Value);
            Mock<IAuthenticationService> authenticationService = new();
            authenticationService.Setup(a => a.AuthenticateAsync(It.IsAny<HttpContext>(), It.IsAny<string>()))
                                 .ReturnsAsync(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity("authType")), "scheme")));

            XUnitLogger<IdentityService> serviceLogger = new(_outputHelper);
            XUnitLogger<CascadeSoftDeleteService> deleteLogger = new(_outputHelper);

            CascadeSoftDeleteService softDeleteService = new(_setUp.DbContext, timeProvider, deleteLogger);

            return new IdentityService(_setUp.DbContext, jwtService.Object, _setUp.PasswordHasher, softDeleteService, authenticationService.Object, timeProvider, serviceLogger);
        }

        private static Mock<IJwtService> CreateJwtServiceMock(int userId, bool returns)
        {
            Mock<IJwtService> jwtServiceMock = new();
            jwtServiceMock.Setup(j => j.TryGetUserId(It.IsAny<ClaimsPrincipal>(), out userId))
                          .Returns(returns);

            return jwtServiceMock;
        }

        public void Dispose()
        {
            _setUp.Dispose();
        }
    }
}