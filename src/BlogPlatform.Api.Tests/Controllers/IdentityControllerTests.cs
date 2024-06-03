using BlogPlatform.Api.Controllers;
using BlogPlatform.Api.Identity.ActionResults;
using BlogPlatform.Api.Identity.Models;
using BlogPlatform.Api.Identity.Services.Interfaces;
using BlogPlatform.Api.Models;
using BlogPlatform.EFCore.Models;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using Moq;

using System.Security.Claims;

namespace BlogPlatform.Api.Tests.Controllers
{
    public class IdentityControllerTests
    {
        private readonly User _testUser = new("TestName", "TestEmail");

        [Fact]
        public async Task BasicLoginAsync_ReturnsLoginResult()
        {
            // Arrange
            Mock<IIdentityService> identityServiceMock = new();
            identityServiceMock.Setup(i => i.LoginAsync(It.IsAny<BasicLoginInfo>(), It.IsAny<CancellationToken>())).ReturnsAsync(() => (ELoginResult.Success, _testUser));

            IdentityController controller = CreateMockController(identityServiceMock);

            BasicLoginInfo loginInfo = new("TestId", "TestPassword");

            // Act
            IActionResult result = await controller.BasicLoginAsync(loginInfo, CancellationToken.None);

            // Assert
            LoginResult actionResult = Assert.IsType<LoginResult>(result);
            Assert.False(actionResult.SetCookie);
            Assert.Equal(_testUser, actionResult.User);
        }

        [Fact]
        public async Task BasicLoginAsync_ReturnsNotFound()
        {
            // Arrange
            Mock<IIdentityService> identityServiceMock = new();
            identityServiceMock.Setup(i => i.LoginAsync(It.IsAny<BasicLoginInfo>(), It.IsAny<CancellationToken>())).ReturnsAsync(() => (ELoginResult.NotFound, null));

            IdentityController controller = CreateMockController(identityServiceMock);

            BasicLoginInfo loginInfo = new("TestId", "TestPassword");

            // Act
            IActionResult result = await controller.BasicLoginAsync(loginInfo, CancellationToken.None);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task BasicLoginAsync_ReturnsUnAuthorized()
        {
            // Arrange
            Mock<IIdentityService> identityServiceMock = new();
            identityServiceMock.Setup(i => i.LoginAsync(It.IsAny<BasicLoginInfo>(), It.IsAny<CancellationToken>())).ReturnsAsync(() => (ELoginResult.WrongPassword, null));

            IdentityController controller = CreateMockController(identityServiceMock);

            BasicLoginInfo loginInfo = new("TestId", "TestPassword");

            // Act
            IActionResult result = await controller.BasicLoginAsync(loginInfo, CancellationToken.None);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task BasicSignUpAsync_ReturnsLoginResult()
        {
            // Arrange
            Mock<IIdentityService> identityServiceMock = new();
            identityServiceMock.Setup(i => i.SignUpAsync(It.IsAny<BasicSignUpInfo>(), It.IsAny<CancellationToken>())).ReturnsAsync(() => (ESignUpResult.Success, _testUser));

            IdentityController controller = CreateMockController(identityServiceMock);

            BasicSignUpInfo signUpInfo = new("TestId", "TestPassword", "TestEmail", "TestName");

            // Act
            IActionResult result = await controller.BasicSignUpAsync(signUpInfo, CancellationToken.None);

            // Assert
            LoginResult actionResult = Assert.IsType<LoginResult>(result);
            Assert.False(actionResult.SetCookie);
            Assert.Equal(_testUser, actionResult.User);
        }

        [Theory]
        [InlineData(ESignUpResult.UserIdAlreadyExists)]
        [InlineData(ESignUpResult.NameAlreadyExists)]
        [InlineData(ESignUpResult.EmailAlreadyExists)]
        public async Task BasicSignUpAsync_ReturnsConflict(ESignUpResult signUpResult)
        {
            // Arrange
            Mock<IIdentityService> identityServiceMock = new();
            identityServiceMock.Setup(i => i.SignUpAsync(It.IsAny<BasicSignUpInfo>(), It.IsAny<CancellationToken>())).ReturnsAsync(() => (signUpResult, null));

            IdentityController controller = CreateMockController(identityServiceMock);

            BasicSignUpInfo signUpInfo = new("TestId", "TestPassword", "TestEmail", "TestName");

            // Act
            IActionResult result = await controller.BasicSignUpAsync(signUpInfo, CancellationToken.None);

            // Assert
            ConflictObjectResult actionResult = Assert.IsType<ConflictObjectResult>(result);
            Assert.IsType<Error>(actionResult.Value);
        }

        [Fact]
        public async Task BasicSignUpAsync_Throw()
        {
            // Arrange
            Mock<IIdentityService> identityServiceMock = new();
            identityServiceMock.Setup(i => i.SignUpAsync(It.IsAny<BasicSignUpInfo>(), It.IsAny<CancellationToken>())).ReturnsAsync(() => (ESignUpResult.OAuthAlreadyExists, null));

            IdentityController controller = CreateMockController(identityServiceMock);

            BasicSignUpInfo signUpInfo = new("TestId", "TestPassword", "TestEmail", "TestName");

            // Assert
            await Assert.ThrowsAnyAsync<Exception>(() => controller.BasicSignUpAsync(signUpInfo, CancellationToken.None));
        }

        [Fact]
        public async Task BasicSignUpAsync_ReturnsNotFound()
        {
            // Arrange
            Mock<IIdentityService> identityServiceMock = new();
            identityServiceMock.Setup(i => i.SignUpAsync(It.IsAny<BasicSignUpInfo>(), It.IsAny<CancellationToken>())).ReturnsAsync(() => (ESignUpResult.ProviderNotFound, null));

            IdentityController controller = CreateMockController(identityServiceMock);

            BasicSignUpInfo signUpInfo = new("TestId", "TestPassword", "TestEmail", "TestName");

            // Act
            IActionResult result = await controller.BasicSignUpAsync(signUpInfo, CancellationToken.None);

            // Assert
            NotFoundObjectResult actionResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.IsType<Error>(actionResult.Value);
        }

        [Fact]
        public async Task SendVerifyEmailAsync_ReturnsOk()
        {
            // Arrange
            Mock<IVerifyEmailService> verifyEmailServiceMock = new();
            verifyEmailServiceMock.Setup(v => v.SendEmailVerificationAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            IdentityController controller = CreateMockController(verifyEmailServiceMock: verifyEmailServiceMock);

            string email = "test@example.com";

            // Act
            IActionResult result = await controller.SendVerifyEmailAsync(email, CancellationToken.None);

            // Assert
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task VerifyEmailAsync_ReturnsOk()
        {
            // Arrange
            Mock<IVerifyEmailService> verifyEmailServiceMock = new();
            verifyEmailServiceMock.Setup(v => v.VerifyEmailCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult("code")!);

            IdentityController controller = CreateMockController(verifyEmailServiceMock: verifyEmailServiceMock);

            string email = "test@example.com";

            // Act
            IActionResult result = await controller.SendVerifyEmailAsync(email, CancellationToken.None);

            // Assert
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task VerifyEmailAsync_ReturnsBadRequest()
        {
            // Arrange
            Mock<IVerifyEmailService> verifyEmailServiceMock = new();
            verifyEmailServiceMock.Setup(v => v.VerifyEmailCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult<string?>(null)); ;

            IdentityController controller = CreateMockController(verifyEmailServiceMock: verifyEmailServiceMock);

            string email = "test@example.com";

            // Act
            IActionResult result = await controller.VerifyEmailAsync(email, CancellationToken.None);

            // Assert
            BadRequestObjectResult actionResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.IsType<Error>(actionResult.Value);
        }

        [Fact]
        public void OAuthLogin_ReturnsChallenge()
        {
            // Arrange
            IdentityController identityController = CreateMockController();

            // Act
            IActionResult result = identityController.OAuthLogin("provider");

            // Assert
            Assert.IsType<ChallengeResult>(result);
        }

        [Fact]
        public async Task OAuthLoginCallbackAsync_ReturnsLoginResult()
        {
            // Arrange
            Mock<IIdentityService> identityServiceMock = new();
            identityServiceMock.Setup(i => i.LoginAsync(It.IsAny<OAuthLoginInfo>(), It.IsAny<CancellationToken>())).ReturnsAsync(() => (ELoginResult.Success, _testUser));

            IdentityController controller = CreateMockController(identityServiceMock);
            OAuthLoginInfo loginInfo = new("TestProvider", "TestNameIdentifier");

            // Act
            IActionResult result = await controller.OAuthLoginCallbackAsync(loginInfo, false, CancellationToken.None);

            // Assert
            LoginResult actionResult = Assert.IsType<LoginResult>(result);
            Assert.False(actionResult.SetCookie);
            Assert.Equal(_testUser, actionResult.User);
        }

        [Fact]
        public async Task OAuthLoginCallbackAsync_ReturnsNotFound()
        {
            // Arrange
            Mock<IIdentityService> identityServiceMock = new();
            identityServiceMock.Setup(i => i.LoginAsync(It.IsAny<OAuthLoginInfo>(), It.IsAny<CancellationToken>())).ReturnsAsync(() => (ELoginResult.NotFound, null));

            IdentityController controller = CreateMockController(identityServiceMock);
            OAuthLoginInfo loginInfo = new("TestProvider", "TestNameIdentifier");

            // Act
            IActionResult result = await controller.OAuthLoginCallbackAsync(loginInfo, false, CancellationToken.None);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task OAuthLoginCallbackAsync_Throw()
        {
            // Arrange
            Mock<IIdentityService> identityServiceMock = new();
            identityServiceMock.Setup(i => i.LoginAsync(It.IsAny<OAuthLoginInfo>(), It.IsAny<CancellationToken>())).ReturnsAsync(() => (ELoginResult.WrongPassword, null));

            IdentityController controller = CreateMockController(identityServiceMock);
            OAuthLoginInfo loginInfo = new("TestProvider", "TestNameIdentifier");

            // Assert
            await Assert.ThrowsAnyAsync<Exception>(() => controller.OAuthLoginCallbackAsync(loginInfo, false, CancellationToken.None));
        }

        [Fact]
        public void OAuthSignUp_ReturnsChallenge()
        {
            // Arrange
            IdentityController identityController = CreateMockController();

            // Act
            IActionResult result = identityController.OAuthSignUp("provider", "name");

            // Assert
            Assert.IsType<OAuthSignUpChallengeResult>(result);
        }

        [Fact]
        public async Task OAuthSignUpCallbackAsync_ReturnsLoginResult()
        {
            // Arrange
            Mock<IIdentityService> identityServiceMock = new();
            identityServiceMock.Setup(i => i.SignUpAsync(It.IsAny<OAuthSignUpInfo>(), It.IsAny<CancellationToken>())).ReturnsAsync(() => (ESignUpResult.Success, _testUser));

            IdentityController controller = CreateMockController(identityServiceMock);
            OAuthSignUpInfo signUpInfo = new("TestProvider", "TestNameIdentifier", "TestEmail", "TestName");

            // Act
            IActionResult result = await controller.OAuthSignUpCallbackAsync(signUpInfo, false, CancellationToken.None);

            // Assert
            LoginResult actionResult = Assert.IsType<LoginResult>(result);
            Assert.False(actionResult.SetCookie);
            Assert.Equal(_testUser, actionResult.User);
        }

        [Theory]
        [InlineData(ESignUpResult.UserIdAlreadyExists)]
        [InlineData(ESignUpResult.NameAlreadyExists)]
        [InlineData(ESignUpResult.EmailAlreadyExists)]
        [InlineData(ESignUpResult.OAuthAlreadyExists)]
        public async Task OAuthSignUpCallbackAsync_ReturnsConflict(ESignUpResult signUpResult)
        {
            // Arrange
            Mock<IIdentityService> identityServiceMock = new();
            identityServiceMock.Setup(i => i.SignUpAsync(It.IsAny<OAuthSignUpInfo>(), It.IsAny<CancellationToken>())).ReturnsAsync(() => (signUpResult, null));

            IdentityController controller = CreateMockController(identityServiceMock);
            OAuthSignUpInfo signUpInfo = new("TestProvider", "TestNameIdentifier", "TestEmail", "TestName");

            // Act
            IActionResult result = await controller.OAuthSignUpCallbackAsync(signUpInfo, false, CancellationToken.None);

            // Assert
            ConflictObjectResult actionResult = Assert.IsType<ConflictObjectResult>(result);
            Assert.IsType<Error>(actionResult.Value);
        }

        [Fact]
        public void AddOAuth_ReturnsChallenge()
        {
            // Arrange
            IdentityController identityController = CreateMockController();

            // Act
            IActionResult result = identityController.AddOAuth("provider");

            // Assert
            Assert.IsType<ChallengeResult>(result);
        }

        [Fact]
        public async Task AddOAuthCallbackAsync_ReturnsOk()
        {
            // Arrange
            Mock<IIdentityService> identityServiceMock = new();
            identityServiceMock.Setup(i => i.AddOAuthAsync(It.IsAny<HttpContext>(), It.IsAny<OAuthLoginInfo>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(EAddOAuthResult.Success));

            IdentityController controller = CreateMockController(identityServiceMock);
            OAuthLoginInfo loginInfo = new("TestProvider", "TestNameIdentifier");

            // Act
            IActionResult result = await controller.AddOAuthCallbackAsync(loginInfo, CancellationToken.None);

            // Assert
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task AddOAuthCallbackAsync_ReturnsAuthenticatedUserDataNotFoundResult()
        {
            // Arrange
            Mock<IIdentityService> identityServiceMock = new();
            identityServiceMock.Setup(i => i.AddOAuthAsync(It.IsAny<HttpContext>(), It.IsAny<OAuthLoginInfo>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(EAddOAuthResult.UserNotFound));

            IdentityController controller = CreateMockController(identityServiceMock);
            OAuthLoginInfo loginInfo = new("TestProvider", "TestNameIdentifier");

            // Act
            IActionResult result = await controller.AddOAuthCallbackAsync(loginInfo, CancellationToken.None);

            // Assert
            Assert.IsType<AuthenticatedUserDataNotFoundResult>(result);
        }

        [Theory]
        [InlineData(EAddOAuthResult.UserAlreadyHasOAuth)]
        [InlineData(EAddOAuthResult.OAuthAlreadyExists)]
        public async Task AddOAuthCallbackAsync_ReturnsBadRequest(EAddOAuthResult addOAuthResult)
        {
            // Arrange
            Mock<IIdentityService> identityServiceMock = new();
            identityServiceMock.Setup(i => i.AddOAuthAsync(It.IsAny<HttpContext>(), It.IsAny<OAuthLoginInfo>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(addOAuthResult));

            IdentityController controller = CreateMockController(identityServiceMock);
            OAuthLoginInfo loginInfo = new("TestProvider", "TestNameIdentifier");

            // Act
            IActionResult result = await controller.AddOAuthCallbackAsync(loginInfo, CancellationToken.None);

            // Assert
            var actionResult = Assert.IsType<ConflictObjectResult>(result);
            Assert.IsType<Error>(actionResult.Value);
        }

        [Fact]
        public async Task AddOAuthCallbackAsync_ReturnsNotFound()
        {
            // Arrange
            Mock<IIdentityService> identityServiceMock = new();
            identityServiceMock.Setup(i => i.AddOAuthAsync(It.IsAny<HttpContext>(), It.IsAny<OAuthLoginInfo>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(EAddOAuthResult.ProviderNotFound));

            IdentityController controller = CreateMockController(identityServiceMock);
            OAuthLoginInfo loginInfo = new("TestProvider", "TestNameIdentifier");

            // Act
            IActionResult result = await controller.AddOAuthCallbackAsync(loginInfo, CancellationToken.None);

            // Assert
            var actionResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.IsType<Error>(actionResult.Value);
        }

        [Fact]
        public async Task RemoveOAuthCallbackAsync_ReturnsOk()
        {
            // Arrange
            Mock<IIdentityService> identityServiceMock = new();
            identityServiceMock.Setup(i => i.RemoveOAuthAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(ERemoveOAuthResult.Success));

            IdentityController controller = CreateMockController(identityServiceMock);

            // Act
            IActionResult result = await controller.RemoveOAuthAsync("provider", CancellationToken.None);

            // Assert
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task RemoveOAuthCallbackAsync_ReturnsAuthenticatedUserDataNotFoundResult()
        {
            // Arrange
            Mock<IIdentityService> identityServiceMock = new();
            identityServiceMock.Setup(i => i.RemoveOAuthAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(ERemoveOAuthResult.UserNotFound));

            IdentityController controller = CreateMockController(identityServiceMock);

            // Act
            IActionResult result = await controller.RemoveOAuthAsync("provider", CancellationToken.None);

            // Assert
            Assert.IsType<AuthenticatedUserDataNotFoundResult>(result);
        }

        [Fact]
        public async Task RemoveOAuthCallbackAsync_ReturnsNotFound()
        {
            // Arrange
            Mock<IIdentityService> identityServiceMock = new();
            identityServiceMock.Setup(i => i.RemoveOAuthAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(ERemoveOAuthResult.OAuthNotFound));

            IdentityController controller = CreateMockController(identityServiceMock);

            // Act
            IActionResult result = await controller.RemoveOAuthAsync("provider", CancellationToken.None);

            // Assert
            var actionResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.IsType<Error>(actionResult.Value);
        }

        [Fact]
        public void Logout_ReturnsLogOut()
        {
            // Arrange
            IdentityController identityController = CreateMockController();

            // Act
            IActionResult result = identityController.Logout();

            // Assert
            Assert.IsType<LogoutResult>(result);
        }

        [Fact]
        public void Refresh_ReturnsRefresh()
        {
            // Arrange
            IdentityController identityController = CreateMockController();

            // Act
            IActionResult result = identityController.Refresh();

            // Assert
            Assert.IsType<RefreshResult>(result);
        }

        [Fact]
        public async Task ChangePasswordAsync_ReturnsOk()
        {
            // Arrange
            Mock<IIdentityService> identityServiceMock = new();
            identityServiceMock.Setup(i => i.ChangePasswordAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            IdentityController identityController = CreateMockController(identityServiceMock);

            // Act
            IActionResult result = await identityController.ChangePasswordAsync("newPassword", CancellationToken.None);

            // Assert
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task ChangePasswordAsync_ReturnsAuthenticatedUserDataNotFoundResult()
        {
            // Arrange
            Mock<IIdentityService> identityServiceMock = new();
            identityServiceMock.Setup(i => i.ChangePasswordAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(false));
            IdentityController identityController = CreateMockController(identityServiceMock);

            // Act
            IActionResult result = await identityController.ChangePasswordAsync("newPassword", CancellationToken.None);

            // Assert
            Assert.IsType<AuthenticatedUserDataNotFoundResult>(result);
        }

        [Fact]
        public async Task ResetPasswordAsync_ReturnsOk()
        {
            // Arrange
            Mock<IIdentityService> identityServiceMock = new();
            identityServiceMock.Setup(i => i.ResetPasswordAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult("newPassword"));

            IdentityController controller = CreateMockController(identityServiceMock);

            string email = "email@email.com";

            // Act
            IActionResult result = await controller.ResetPasswordAsync(email, CancellationToken.None);

            // Assert
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task ResetPasswordAsync_ReturnsNotFound()
        {
            // Arrange
            Mock<IIdentityService> identityServiceMock = new();
            identityServiceMock.Setup(i => i.ResetPasswordAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult<string?>(null));

            IdentityController controller = CreateMockController(identityServiceMock);

            string email = "email@email.com";

            // Act
            IActionResult result = await controller.ResetPasswordAsync(email, CancellationToken.None);

            // Assert
            var actionResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.IsType<Error>(actionResult.Value);
        }

        [Fact]
        public async Task ChangeNameAsync_ReturnsOk()
        {
            // Arrange
            Mock<IIdentityService> identityServiceMock = new();
            identityServiceMock.Setup(i => i.ChangeNameAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

            IdentityController controller = CreateMockController(identityServiceMock);

            string newName = "NewName";

            // Act
            IActionResult result = await controller.ChangeNameAsync(newName, CancellationToken.None);

            // Assert
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task ChangeNameAsync_ReturnsAuthenticatedUserDataNotFoundResult()
        {
            // Arrange
            Mock<IIdentityService> identityServiceMock = new();
            identityServiceMock.Setup(i => i.ChangeNameAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

            IdentityController controller = CreateMockController(identityServiceMock);

            string newName = "NewName";

            // Act
            IActionResult result = await controller.ChangeNameAsync(newName, CancellationToken.None);

            // Assert
            Assert.IsType<AuthenticatedUserDataNotFoundResult>(result);
        }

        [Fact]
        public async Task WithDrawAsync_ReturnsOk()
        {
            // Arrange
            Mock<IIdentityService> identityServiceMock = new();
            identityServiceMock.Setup(i => i.WithDrawAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

            IdentityController controller = CreateMockController(identityServiceMock);

            // Act
            IActionResult result = await controller.WithDrawAsync(CancellationToken.None);

            // Assert
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task WithDrawAsync_ReturnsAuthenticatedUserDataNotFoundResult()
        {
            // Arrange
            Mock<IIdentityService> identityServiceMock = new();
            identityServiceMock.Setup(i => i.WithDrawAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

            IdentityController controller = CreateMockController(identityServiceMock);

            // Act
            IActionResult result = await controller.WithDrawAsync(CancellationToken.None);

            // Assert
            Assert.IsType<AuthenticatedUserDataNotFoundResult>(result);
        }

        [Fact]
        public async Task CancelWithDrawAsync_ReturnsOk()
        {
            // Arrange
            Mock<IIdentityService> identityServiceMock = new();
            identityServiceMock.Setup(i => i.CancelWithDrawAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>())).ReturnsAsync(ECancelWithDrawResult.Success);

            IdentityController controller = CreateMockController(identityServiceMock);

            // Act
            IActionResult result = await controller.CancelWithDrawAsync(CancellationToken.None);

            // Assert
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task CancelWithDrawAsync_ReturnsAuthenticatedUserDataNotFoundResult()
        {
            // Arrange
            Mock<IIdentityService> identityServiceMock = new();
            identityServiceMock.Setup(i => i.CancelWithDrawAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>())).ReturnsAsync(ECancelWithDrawResult.UserNotFound);

            IdentityController controller = CreateMockController(identityServiceMock);

            // Act
            IActionResult result = await controller.CancelWithDrawAsync(CancellationToken.None);

            // Assert
            Assert.IsType<AuthenticatedUserDataNotFoundResult>(result);
        }

        [Theory]
        [InlineData(ECancelWithDrawResult.Expired)]
        [InlineData(ECancelWithDrawResult.WithDrawNotRequested)]
        public async Task CancelWithDrawAsync_ReturnsBadRequest(ECancelWithDrawResult cancelWithDrawResult)
        {
            // Arrange
            Mock<IIdentityService> identityServiceMock = new();
            identityServiceMock.Setup(i => i.CancelWithDrawAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>())).ReturnsAsync(cancelWithDrawResult);

            IdentityController controller = CreateMockController(identityServiceMock);

            // Act
            IActionResult result = await controller.CancelWithDrawAsync(CancellationToken.None);

            // Assert
            var actionResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.IsType<Error>(actionResult.Value);
        }

        [Fact]
        public async Task ChangeEmailAsync_ReturnsOk()
        {
            // Arrange
            IdentityController controller = CreateMockController();

            string newEmail = "new@example.com";

            // Act
            IActionResult result = await controller.ChangeEmailAsync(newEmail, CancellationToken.None);

            // Assert
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task ConfirmEmailChangeAsync_ReturnsOk()
        {
            // Arrange
            Mock<IIdentityService> identityServiceMock = new();
            identityServiceMock.Setup(i => i.ChangeEmailAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

            Mock<IVerifyEmailService> verifyEmailServiceMock = new();
            verifyEmailServiceMock.Setup(v => v.VerifyEmailCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult("code")!);

            IdentityController controller = CreateMockController(identityServiceMock, verifyEmailServiceMock);

            // Act
            IActionResult result = await controller.ConfirmChangeEmailAsync("code", CancellationToken.None);

            // Assert
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task ConfirmEmailChangeAsync_ReturnsBadRequest()
        {
            // Arrange
            Mock<IVerifyEmailService> verifyEmailServiceMock = new();
            verifyEmailServiceMock.Setup(v => v.VerifyEmailCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult<string?>(null));

            IdentityController controller = CreateMockController(verifyEmailServiceMock: verifyEmailServiceMock);

            // Act
            IActionResult result = await controller.ConfirmChangeEmailAsync("code", CancellationToken.None);

            // Assert
            var actionResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.IsType<Error>(actionResult.Value);
        }

        [Fact]
        public async Task ConfirmEmailChangeAsync_ReturnsAuthenticatedUserDataNotFoundResult()
        {
            // Arrange
            Mock<IIdentityService> identityServiceMock = new();
            identityServiceMock.Setup(i => i.ChangeEmailAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

            Mock<IVerifyEmailService> verifyEmailServiceMock = new();
            verifyEmailServiceMock.Setup(v => v.VerifyEmailCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult("code")!);

            IdentityController controller = CreateMockController(identityServiceMock, verifyEmailServiceMock);

            // Act
            IActionResult result = await controller.ConfirmChangeEmailAsync("code", CancellationToken.None);

            // Assert
            Assert.IsType<AuthenticatedUserDataNotFoundResult>(result);
        }

        private IdentityController CreateMockController(Mock<IIdentityService>? identityServiceMock = null, Mock<IVerifyEmailService>? verifyEmailServiceMock = null, Mock<IPasswordResetMailService>? passwordResetServiceMock = null, Mock<IFindAccountIdMailService>? findAccountIdMailServiceMock = null, Mock<ILogger<IdentityController>>? loggerMock = null, Mock<IUrlHelper>? urlHelper = null)
        {
            identityServiceMock ??= new();
            verifyEmailServiceMock ??= new();
            passwordResetServiceMock ??= new();
            findAccountIdMailServiceMock ??= new();
            loggerMock ??= new();
            urlHelper ??= new();

            IdentityController controller = new(identityServiceMock.Object, verifyEmailServiceMock.Object, passwordResetServiceMock.Object, findAccountIdMailServiceMock.Object, loggerMock.Object);
            controller.Url = urlHelper.Object;
            return controller;
        }
    }
}