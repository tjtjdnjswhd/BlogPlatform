using BlogPlatform.EFCore.Models;
using BlogPlatform.Shared.Identity.Models;
using BlogPlatform.Shared.Identity.Services.Interfaces;
using BlogPlatform.Shared.Models;

using Moq;

using System.Net;
using System.Net.Http.Json;

namespace BlogPlatform.Api.IntegrationTest.Identity
{
    public partial class IdentityTests
    {
        [Fact]
        public async Task BasicLogin_NotFound()
        {
            // Arrange
            HttpClient client = CreateClient();

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/login/basic", new BasicLoginInfo("notexist", "password", null));

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task BasicLogin_Unauthorized()
        {
            // Arrange
            HttpClient client = CreateClient();

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/login/basic", new BasicLoginInfo("user1Id", "wrongpassword", null));

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task BasicLogin_Invalid_Id()
        {
            // Arrange
            HttpClient client = CreateClient();

            BasicLoginInfo basicLoginInfo = new("abc", "password", null);

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/login/basic", basicLoginInfo);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task BasicLogin_Invalid_Password()
        {
            // Arrange
            HttpClient client = CreateClient();

            BasicLoginInfo basicLoginInfo = new("user0Id", "abc", null);

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/login/basic", basicLoginInfo);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task BasicLogin_Ok()
        {
            // Arrange
            HttpClient client = CreateClient();

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/login/basic", new BasicLoginInfo("user1Id", "user1pw", null));

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            AuthorizeToken? authorizeToken = await response.Content.ReadFromJsonAsync<AuthorizeToken>();
            Assert.NotNull(authorizeToken);
            response.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? cookieValues);
            Assert.Null(cookieValues);
        }

        [Fact]
        public async Task BasicSignUp_EmailVerifyRequired()
        {
            // Arrange
            HttpClient client = CreateClient();

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/signup/basic", new BasicSignUpInfo("user55", "user55pw", "user55", "user55@user.com", null));

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task BasicSignUp_IdExist()
        {
            // Arrange
            HttpClient client = CreateClient();
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory);
            BasicAccount basicAccount = Helper.GetFirstEntity<BasicAccount>(WebApplicationFactory, b => b.UserId == user.Id, false);
            string email = "user55@user.com";
            await Helper.SetVerifiedEmailAsync(WebApplicationFactory, email);

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/signup/basic", new BasicSignUpInfo(basicAccount.AccountId, "user55pw", "user55", email, null));

            // Assert
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Error? error = await response.Content.ReadFromJsonAsync<Error>();
            Assert.NotNull(error);
            Assert.Equal("중복된 Id입니다", error.Message);
        }

        [Fact]
        public async Task BasicSignUp_NameExist()
        {
            // Arrange
            HttpClient client = CreateClient();
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory);
            string email = "user55@user.com";
            await Helper.SetVerifiedEmailAsync(WebApplicationFactory, email);

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/signup/basic", new BasicSignUpInfo("user55Id", "user55pw", user.Name, email, null));

            // Assert
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Error? error = await response.Content.ReadFromJsonAsync<Error>();
            Assert.NotNull(error);
            Assert.Equal("중복된 이름입니다", error.Message);
        }

        [Fact]
        public async Task BasicSignUp_EmailExist()
        {
            // Arrange
            HttpClient client = CreateClient();
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory);
            await Helper.SetVerifiedEmailAsync(WebApplicationFactory, user.Email);

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/signup/basic", new BasicSignUpInfo("user55Id", "user55pw", "user55", user.Email, null));

            // Assert
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Error? error = await response.Content.ReadFromJsonAsync<Error>();
            Assert.NotNull(error);
            Assert.Equal("중복된 이메일입니다", error.Message);
        }

        [Fact]
        public async Task BasicSignUp_Success()
        {
            // Arrange
            HttpClient client = CreateClient();

            string email = "user55@user.com";
            await Helper.SetVerifiedEmailAsync(WebApplicationFactory, email);

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/signup/basic", new BasicSignUpInfo("user55Id", "user55pw", "user55", email, null));

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            AuthorizeToken? authorizeToken = await response.Content.ReadFromJsonAsync<AuthorizeToken>();
            Assert.NotNull(authorizeToken);
        }

        [Fact]
        public async Task SendVerifyEmail_Ok()
        {
            // Arrange
            SetIEmailServiceMock();
            HttpClient client = CreateClient();

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/signup/basic/email", new EmailModel("user@user.com"));

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task VerifyEmail_BadRequest()
        {
            // Arrange
            HttpClient client = CreateClient();

            // Act
            HttpResponseMessage response = await client.GetAsync("/api/identity/signup/basic/email?code=123456");

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task VerifyEmail_Ok()
        {
            // Arrange
            Mock<IEmailVerifyService> emailVerifyServiceMock = new();
            emailVerifyServiceMock.Setup(s => s.VerifyEmailCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync("success@user.com");
            SetIEmailVerifyServiceMock(emailVerifyServiceMock);
            HttpClient client = CreateClient();

            // Act
            HttpResponseMessage response = await client.GetAsync("/api/identity/signup/basic/email?code=123456");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
