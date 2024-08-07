﻿using BlogPlatform.Api.Controllers;
using BlogPlatform.Api.Identity.ActionResults;
using BlogPlatform.EFCore;
using BlogPlatform.EFCore.Models;
using BlogPlatform.Shared.Identity.Models;
using BlogPlatform.Shared.Identity.Services.Interfaces;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;

using Moq;

using Xunit.Abstractions;

namespace BlogPlatform.Api.Tests.Controllers
{
    public class IdentityControllerTests
    {
        private readonly User _testUser = new("TestName", "TestEmail");
        private readonly XUnitLogger<IdentityController> _logger;

        public IdentityControllerTests(ITestOutputHelper outputHelper)
        {
            _logger = new(outputHelper);
        }

        [Fact]
        public async Task BasicLoginAsync_ReturnsLoginResult()
        {
            // Arrange
            Mock<IIdentityService> identityServiceMock = new();
            identityServiceMock.Setup(i => i.LoginAsync(It.IsAny<BasicLoginInfo>(), It.IsAny<CancellationToken>())).ReturnsAsync(() => (ELoginResult.Success, _testUser));

            IdentityController controller = CreateMockController(identityServiceMock);

            BasicLoginInfo loginInfo = new("TestId", "TestPassword");

            // Act
            IActionResult result = await controller.BasicLoginAsync(loginInfo, null, CancellationToken.None);

            // Assert
            LoginActionResult actionResult = Assert.IsType<LoginActionResult>(result);
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
            IActionResult result = await controller.BasicLoginAsync(loginInfo, null, CancellationToken.None);

            // Assert
            ControllerTestsUtils.VerifyNotFoundResult(result);
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
            IActionResult result = await controller.BasicLoginAsync(loginInfo, null, CancellationToken.None);

            // Assert
            ControllerTestsUtils.VerifyUnauthorizedResult(result);
        }

        [Fact]
        public async Task BasicSignUpAsync_ReturnsLoginResult()
        {
            // Arrange
            Mock<IIdentityService> identityServiceMock = new();
            identityServiceMock.Setup(i => i.SignUpAsync(It.IsAny<BasicSignUpInfo>(), It.IsAny<CancellationToken>())).ReturnsAsync(() => (ESignUpResult.Success, _testUser));

            IdentityController controller = CreateMockController(identityServiceMock);

            BasicSignUpInfo signUpInfo = new("TestId", "TestPassword", "TestName", "TestEmail");

            // Act
            IActionResult result = await controller.BasicSignUpAsync(signUpInfo, null, CancellationToken.None);

            // Assert
            LoginActionResult actionResult = Assert.IsType<LoginActionResult>(result);
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

            BasicSignUpInfo signUpInfo = new("TestId", "TestPassword", "TestName", "TestEmail");

            // Act
            IActionResult result = await controller.BasicSignUpAsync(signUpInfo, null, CancellationToken.None);

            // Assert
            ControllerTestsUtils.VerifyConflictResult(result);
        }

        [Fact]
        public async Task BasicSignUpAsync_Throw()
        {
            // Arrange
            Mock<IIdentityService> identityServiceMock = new();
            identityServiceMock.Setup(i => i.SignUpAsync(It.IsAny<BasicSignUpInfo>(), It.IsAny<CancellationToken>())).ReturnsAsync(() => (ESignUpResult.OAuthAlreadyExists, null));

            IdentityController controller = CreateMockController(identityServiceMock);

            BasicSignUpInfo signUpInfo = new("TestId", "TestPassword", "TestName", "TestEmail");

            // Assert
            await Assert.ThrowsAnyAsync<Exception>(() => controller.BasicSignUpAsync(signUpInfo, null, CancellationToken.None));
        }

        [Fact]
        public async Task SendVerifyEmailAsync_ReturnsOk()
        {
            // Arrange
            IdentityController controller = CreateMockController();

            string email = "test@example.com";

            // Act
            IActionResult result = await controller.SendSignUpVerifyEmailAsync(new(email), CancellationToken.None);

            // Assert
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task VerifyEmailAsync_ReturnsOk()
        {
            // Arrange
            Mock<IEmailVerifyService> verifyService = new();
            verifyService.Setup(v => v.VerifySignUpEmailCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult("code")!);

            IdentityController controller = CreateMockController(emailVerifyService: verifyService);

            string email = "test@example.com";

            // Act
            IActionResult result = await controller.SendSignUpVerifyEmailAsync(new(email), CancellationToken.None);

            // Assert
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task VerifyEmailAsync_ReturnsBadRequest()
        {
            // Arrange
            Mock<IEmailVerifyService> verifyService = new();
            verifyService.Setup(v => v.VerifySignUpEmailCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult<string?>(null)); ;

            IdentityController controller = CreateMockController(emailVerifyService: verifyService);

            string email = "test@example.com";

            // Act
            IActionResult result = await controller.ConfirmSignUpEmailAsync(email, CancellationToken.None);

            // Assert
            ControllerTestsUtils.VerifyBadRequestResult(result);
        }

        [Fact]
        public void OAuthLogin_ReturnsChallenge()
        {
            // Arrange
            IdentityController identityController = CreateMockController();

            // Act
            IActionResult result = identityController.OAuthLogin("provider", null);

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
            IActionResult result = await controller.OAuthLoginCallbackAsync(loginInfo, "returnUrl", CancellationToken.None);

            // Assert
            LoginActionResult actionResult = Assert.IsType<LoginActionResult>(result);
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
            IActionResult result = await controller.OAuthLoginCallbackAsync(loginInfo, null, CancellationToken.None);

            // Assert
            ControllerTestsUtils.VerifyNotFoundResult(result);
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
            await Assert.ThrowsAnyAsync<Exception>(() => controller.OAuthLoginCallbackAsync(loginInfo, null, CancellationToken.None));
        }

        [Fact]
        public void OAuthSignUp_ReturnsChallenge()
        {
            // Arrange
            IdentityController identityController = CreateMockController();

            // Act
            IActionResult result = identityController.OAuthSignUp("provider", "name", null);

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
            OAuthSignUpInfo signUpInfo = new("TestEmail", "TestName", "TestProvider", "TestNameIdentifier");

            // Act
            IActionResult result = await controller.OAuthSignUpCallbackAsync(signUpInfo, null, CancellationToken.None);

            // Assert
            LoginActionResult actionResult = Assert.IsType<LoginActionResult>(result);
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
            OAuthSignUpInfo signUpInfo = new("TestEmail", "TestName", "TestProvider", "TestNameIdentifier");

            // Act
            IActionResult result = await controller.OAuthSignUpCallbackAsync(signUpInfo, null, CancellationToken.None);

            // Assert
            ControllerTestsUtils.VerifyConflictResult(result);
        }

        [Fact]
        public void AddOAuth_ReturnsChallenge()
        {
            // Arrange
            IdentityController identityController = CreateMockController();

            // Act
            IActionResult result = identityController.AddOAuth("provider", null, 1);

            // Assert
            Assert.IsType<ChallengeResult>(result);
        }

        [Fact]
        public async Task AddOAuthCallbackAsync_ReturnsOk()
        {
            // Arrange
            Mock<IIdentityService> identityServiceMock = new();
            identityServiceMock.Setup(i => i.AddOAuthAsync(It.IsAny<int>(), It.IsAny<OAuthLoginInfo>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(EAddOAuthResult.Success));

            IdentityController controller = CreateMockController(identityServiceMock);
            OAuthLoginInfo loginInfo = new("TestProvider", "TestNameIdentifier");

            // Act
            IActionResult result = await controller.AddOAuthCallbackAsync(loginInfo, 1, null, CancellationToken.None);

            // Assert
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task AddOAuthCallbackAsync_ReturnsAuthenticatedUserDataNotFoundResult()
        {
            // Arrange
            Mock<IIdentityService> identityServiceMock = new();
            identityServiceMock.Setup(i => i.AddOAuthAsync(It.IsAny<int>(), It.IsAny<OAuthLoginInfo>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(EAddOAuthResult.UserNotFound));

            IdentityController controller = CreateMockController(identityServiceMock);
            OAuthLoginInfo loginInfo = new("TestProvider", "TestNameIdentifier");

            // Act
            IActionResult result = await controller.AddOAuthCallbackAsync(loginInfo, 1, null, CancellationToken.None);

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
            identityServiceMock.Setup(i => i.AddOAuthAsync(It.IsAny<int>(), It.IsAny<OAuthLoginInfo>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(addOAuthResult));

            IdentityController controller = CreateMockController(identityServiceMock);
            OAuthLoginInfo loginInfo = new("TestProvider", "TestNameIdentifier");

            // Act
            IActionResult result = await controller.AddOAuthCallbackAsync(loginInfo, 1, null, CancellationToken.None);

            // Assert
            ControllerTestsUtils.VerifyConflictResult(result);
        }

        [Fact]
        public async Task AddOAuthCallbackAsync_ReturnsNotFound()
        {
            // Arrange
            Mock<IIdentityService> identityServiceMock = new();
            identityServiceMock.Setup(i => i.AddOAuthAsync(It.IsAny<int>(), It.IsAny<OAuthLoginInfo>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(EAddOAuthResult.ProviderNotFound));

            IdentityController controller = CreateMockController(identityServiceMock);
            OAuthLoginInfo loginInfo = new("TestProvider", "TestNameIdentifier");

            // Act
            IActionResult result = await controller.AddOAuthCallbackAsync(loginInfo, 1, null, CancellationToken.None);

            // Assert
            ControllerTestsUtils.VerifyNotFoundResult(result);
        }

        [Fact]
        public async Task RemoveOAuthAsync_ReturnsOk()
        {
            // Arrange
            Mock<IIdentityService> identityServiceMock = new();
            identityServiceMock.Setup(i => i.RemoveOAuthAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(ERemoveOAuthResult.Success));

            IdentityController controller = CreateMockController(identityServiceMock);

            // Act
            IActionResult result = await controller.RemoveOAuthAsync("provider", 1, CancellationToken.None);

            // Assert
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task RemoveOAuthAsync_ReturnsConflict()
        {
            // Arrange
            Mock<IIdentityService> identityServiceMock = new();
            identityServiceMock.Setup(i => i.RemoveOAuthAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(ERemoveOAuthResult.HasSingleAccount));

            IdentityController controller = CreateMockController(identityServiceMock);

            // Act
            IActionResult result = await controller.RemoveOAuthAsync("provider", 1, CancellationToken.None);

            // Assert
            ControllerTestsUtils.VerifyConflictResult(result);
        }

        [Fact]
        public async Task RemoveOAuthAsync_ReturnsAuthenticatedUserDataNotFoundResult()
        {
            // Arrange
            Mock<IIdentityService> identityServiceMock = new();
            identityServiceMock.Setup(i => i.RemoveOAuthAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(ERemoveOAuthResult.UserNotFound));

            IdentityController controller = CreateMockController(identityServiceMock);

            // Act
            IActionResult result = await controller.RemoveOAuthAsync("provider", 1, CancellationToken.None);

            // Assert
            Assert.IsType<AuthenticatedUserDataNotFoundResult>(result);
        }

        [Fact]
        public async Task RemoveOAuthAsync_ReturnsNotFound()
        {
            // Arrange
            Mock<IIdentityService> identityServiceMock = new();
            identityServiceMock.Setup(i => i.RemoveOAuthAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(ERemoveOAuthResult.OAuthNotFound));

            IdentityController controller = CreateMockController(identityServiceMock);

            // Act
            IActionResult result = await controller.RemoveOAuthAsync("provider", 1, CancellationToken.None);

            // Assert
            ControllerTestsUtils.VerifyNotFoundResult(result);
        }

        [Fact]
        public void Logout_ReturnsLogOut()
        {
            // Arrange
            IdentityController identityController = CreateMockController();

            // Act
            IActionResult result = identityController.Logout(null);

            // Assert
            Assert.IsType<SignOutResult>(result);
        }

        [Fact]
        public void Refresh_ReturnsRefresh()
        {
            // Arrange
            IdentityController identityController = CreateMockController();

            // Act
            IActionResult result = identityController.Refresh(new AuthorizeToken("accessToken", "refreshToken"), null);

            // Assert
            Assert.IsType<RefreshResult>(result);
        }

        [Fact]
        public async Task ChangePasswordAsync_ReturnsOk()
        {
            // Arrange
            Mock<IIdentityService> identityServiceMock = new();
            identityServiceMock.Setup(i => i.ChangePasswordAsync(It.IsAny<PasswordChangeModel>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(EChangePasswordResult.Success));
            IdentityController identityController = CreateMockController(identityServiceMock);

            // Act
            IActionResult result = await identityController.ChangePasswordAsync(new("id", "currentPassword", "newPassword"), CancellationToken.None);

            // Assert
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task ChangePasswordAsync_ReturnsAccountNotFoundResult()
        {
            // Arrange
            Mock<IIdentityService> identityServiceMock = new();
            identityServiceMock.Setup(i => i.ChangePasswordAsync(It.IsAny<PasswordChangeModel>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(EChangePasswordResult.BasicAccountNotFound));
            IdentityController identityController = CreateMockController(identityServiceMock);

            // Act
            IActionResult result = await identityController.ChangePasswordAsync(new("id", "currentPassword", "newPassword"), CancellationToken.None);

            // Assert
            ControllerTestsUtils.VerifyNotFoundResult(result);
        }

        [Fact]
        public async Task ResetPasswordAsync_ReturnsOk()
        {
            // Arrange
            Mock<IIdentityService> identityServiceMock = new();
            identityServiceMock.Setup(i => i.ResetPasswordAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult<string?>("newPassword"));

            IdentityController controller = CreateMockController(identityServiceMock);

            string email = "email@email.com";

            // Act
            IActionResult result = await controller.ResetPasswordAsync(new(email), CancellationToken.None);

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
            IActionResult result = await controller.ResetPasswordAsync(new(email), CancellationToken.None);

            // Assert
            ControllerTestsUtils.VerifyNotFoundResult(result);
        }

        [Fact]
        public async Task ChangeNameAsync_ReturnsOk()
        {
            // Arrange
            Mock<IIdentityService> identityServiceMock = new();
            identityServiceMock.Setup(i => i.ChangeNameAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(EChangeNameResult.Success);

            IdentityController controller = CreateMockController(identityServiceMock);

            string newName = "NewName";

            // Act
            IActionResult result = await controller.ChangeNameAsync(new(newName), 1, CancellationToken.None);

            // Assert
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task ChangeNameAsync_ReturnsAuthenticatedUserDataNotFoundResult()
        {
            // Arrange
            Mock<IIdentityService> identityServiceMock = new();
            identityServiceMock.Setup(i => i.ChangeNameAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(EChangeNameResult.UserNotFound);

            IdentityController controller = CreateMockController(identityServiceMock);

            string newName = "NewName";

            // Act
            IActionResult result = await controller.ChangeNameAsync(new(newName), 1, CancellationToken.None);

            // Assert
            Assert.IsType<AuthenticatedUserDataNotFoundResult>(result);
        }

        [Fact]
        public async Task WithDrawAsync_ReturnsOk()
        {
            // Arrange
            Mock<IIdentityService> identityServiceMock = new();
            identityServiceMock.Setup(i => i.WithDrawAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(EWithDrawResult.Success);

            IdentityController controller = CreateMockController(identityServiceMock);

            // Act
            IActionResult result = await controller.WithDrawAsync(1, CancellationToken.None);

            // Assert
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task WithDrawAsync_ReturnsAuthenticatedUserDataNotFoundResult()
        {
            // Arrange
            Mock<IIdentityService> identityServiceMock = new();
            identityServiceMock.Setup(i => i.WithDrawAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(EWithDrawResult.UserNotFound);

            IdentityController controller = CreateMockController(identityServiceMock);

            // Act
            IActionResult result = await controller.WithDrawAsync(1, CancellationToken.None);

            // Assert
            Assert.IsType<AuthenticatedUserDataNotFoundResult>(result);
        }

        [Fact]
        public async Task CancelWithDrawAsync_ReturnsOk()
        {
            // Arrange
            Mock<IIdentityService> identityServiceMock = new();
            identityServiceMock.Setup(i => i.CancelWithDrawAsync(It.IsAny<BasicLoginInfo>(), It.IsAny<CancellationToken>())).ReturnsAsync(ECancelWithDrawResult.Success);

            IdentityController controller = CreateMockController(identityServiceMock);

            // Act
            IActionResult result = await controller.CancelWithDrawAsync(new BasicLoginInfo("", ""), CancellationToken.None);

            // Assert
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task CancelWithDrawAsync_ReturnsAccountNotFound()
        {
            // Arrange
            Mock<IIdentityService> identityServiceMock = new();
            identityServiceMock.Setup(i => i.CancelWithDrawAsync(It.IsAny<BasicLoginInfo>(), It.IsAny<CancellationToken>())).ReturnsAsync(ECancelWithDrawResult.AccountNotFound);

            IdentityController controller = CreateMockController(identityServiceMock);

            // Act
            IActionResult result = await controller.CancelWithDrawAsync(new("", ""), CancellationToken.None);

            // Assert
            ControllerTestsUtils.VerifyNotFoundResult(result);
        }

        [Theory]
        [InlineData(ECancelWithDrawResult.Expired)]
        [InlineData(ECancelWithDrawResult.WithDrawNotRequested)]
        public async Task CancelWithDrawAsync_ReturnsBadRequest(ECancelWithDrawResult cancelWithDrawResult)
        {
            // Arrange
            Mock<IIdentityService> identityServiceMock = new();
            identityServiceMock.Setup(i => i.CancelWithDrawAsync(It.IsAny<BasicLoginInfo>(), It.IsAny<CancellationToken>())).ReturnsAsync(cancelWithDrawResult);

            IdentityController controller = CreateMockController(identityServiceMock);

            // Act
            IActionResult result = await controller.CancelWithDrawAsync(new("", ""), CancellationToken.None);

            // Assert
            ControllerTestsUtils.VerifyBadRequestResult(result);
        }

        [Fact]
        public async Task ChangeEmailAsync_ReturnsOk()
        {
            // Arrange
            IdentityController controller = CreateMockController();

            string newEmail = "new@example.com";

            // Act
            IActionResult result = await controller.VerifyChangeEmailAsync(new(newEmail), 1, CancellationToken.None);

            // Assert
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task ConfirmEmailChangeAsync_ReturnsOk()
        {
            // Arrange
            Mock<IIdentityService> identityServiceMock = new();
            identityServiceMock.Setup(i => i.ChangeEmailAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(EChangeEmailResult.Success);

            Mock<IEmailVerifyService> verifyService = new();
            verifyService.Setup(v => v.VerifyChangeEmailCodeAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult("code")!);

            IdentityController controller = CreateMockController(identityServiceMock, emailVerifyService: verifyService);

            // Act
            IActionResult result = await controller.ConfirmChangeEmailAsync("code", 1, CancellationToken.None);

            // Assert
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task ConfirmEmailChangeAsync_ReturnsBadRequest()
        {
            // Arrange
            Mock<IEmailVerifyService> verifyService = new();
            verifyService.Setup(v => v.VerifySignUpEmailCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult<string?>(null));

            IdentityController controller = CreateMockController(emailVerifyService: verifyService);

            // Act
            IActionResult result = await controller.ConfirmChangeEmailAsync("code", 1, CancellationToken.None);

            // Assert
            ControllerTestsUtils.VerifyBadRequestResult(result);
        }

        [Fact]
        public async Task ConfirmEmailChangeAsync_ReturnsAuthenticatedUserDataNotFoundResult()
        {
            // Arrange
            Mock<IIdentityService> identityServiceMock = new();
            identityServiceMock.Setup(i => i.ChangeEmailAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(EChangeEmailResult.UserNotFound);

            Mock<IEmailVerifyService> verifyService = new();
            verifyService.Setup(v => v.VerifyChangeEmailCodeAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult("code")!);

            IdentityController controller = CreateMockController(identityServiceMock, emailVerifyService: verifyService);

            // Act
            IActionResult result = await controller.ConfirmChangeEmailAsync("code", 1, CancellationToken.None);

            // Assert
            Assert.IsType<AuthenticatedUserDataNotFoundResult>(result);
        }

        private IdentityController CreateMockController(Mock<IIdentityService>? identityServiceMock = null, Mock<IEmailVerifyService>? emailVerifyService = null)
        {
            identityServiceMock ??= new();
            emailVerifyService ??= new();
            Mock<IUserEmailService> userEmailService = new();
            Mock<IUrlHelper> urlHelper = new();

            urlHelper.Setup(u => u.Action(It.IsAny<UrlActionContext>())).Returns("http://localhost:5000");
            urlHelper.Setup(u => u.ActionContext).Returns(new ActionContext() { HttpContext = new DefaultHttpContext() });

            TimeProvider timeProvider = TimeProvider.System;

            DbContextOptionsBuilder<BlogPlatformDbContext> optionsBuilder = new();
            Mock<BlogPlatformDbContext> dbContextMock = new(optionsBuilder.Options);
            IdentityController controller = new(identityServiceMock.Object, userEmailService.Object, emailVerifyService.Object, timeProvider, _logger)
            {
                Url = urlHelper.Object
            };
            return controller;
        }
    }
}