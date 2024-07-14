using BlogPlatform.EFCore.Models;
using BlogPlatform.Shared.Identity.Models;
using BlogPlatform.Shared.Identity.Services.Interfaces;
using BlogPlatform.Shared.Models;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
            HttpClient client = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/login/basic", new BasicLoginInfo("notexist", "password"));

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task BasicLogin_Unauthorized()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/login/basic", new BasicLoginInfo("user1Id", "wrongpassword"));

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task BasicLogin_Invalid_Id()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);

            BasicLoginInfo basicLoginInfo = new("bc", "password");

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/login/basic", basicLoginInfo);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task BasicLogin_Invalid_Password()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);

            BasicLoginInfo basicLoginInfo = new("user0Id", "abc");

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/login/basic", basicLoginInfo);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact, ResetDataAfterTest]
        public async Task BasicLogin_Ok()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/login/basic", new BasicLoginInfo("user1Id", "user1pw"));

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
            HttpClient client = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/signup/basic", new BasicSignUpInfo("user55", "user55pw", "user55", "user55@user.com"));

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task BasicSignUp_IdExist()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory);
            BasicAccount basicAccount = Helper.GetFirstEntity<BasicAccount>(WebApplicationFactory, b => b.UserId == user.Id, false);
            string email = "user55@user.com";
            await Helper.SetSignUpVerifiedEmailAsync(WebApplicationFactory, email);

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/signup/basic", new BasicSignUpInfo(basicAccount.AccountId, "user55pw", "user55", email));

            // Assert
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task BasicSignUp_NameExist()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory);
            string email = "user55@user.com";
            await Helper.SetSignUpVerifiedEmailAsync(WebApplicationFactory, email);

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/signup/basic", new BasicSignUpInfo("user55Id", "user55pw", user.Name, email));

            // Assert
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task BasicSignUp_EmailExist()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory);
            await Helper.SetSignUpVerifiedEmailAsync(WebApplicationFactory, user.Email);

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/signup/basic", new BasicSignUpInfo("user55Id", "user55pw", "user55", user.Email));

            // Assert
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact, ResetDataAfterTest]
        public async Task BasicSignUp_Success()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);

            string email = "user55@user.com";
            await Helper.SetSignUpVerifiedEmailAsync(WebApplicationFactory, email);

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/signup/basic", new BasicSignUpInfo("user55Id", "user55pw", "user55", email));

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            AuthorizeToken? authorizeToken = await response.Content.ReadFromJsonAsync<AuthorizeToken>();
            Assert.NotNull(authorizeToken);
        }

        [Fact]
        public async Task SendSignUpVerifyEmail_Ok()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/signup/email/verify", new EmailModel("user@user.com"));

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task VerifyEmail_BadRequest()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);

            // Act
            HttpResponseMessage response = await client.GetAsync("/api/identity/signup/email/confirm?code=123456");

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task VerifyEmail_Ok()
        {
            // Arrange
            WebApplicationFactory<Program> newFactory = WebApplicationFactory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    Mock<IEmailVerifyService> veriftyServiceMock = new();
                    veriftyServiceMock.Setup(v => v.VerifySignUpEmailCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync("");
                    services.RemoveAll<IEmailVerifyService>();
                    services.AddSingleton(veriftyServiceMock.Object);
                });
            });
            HttpClient client = newFactory.CreateLoggingClient(TestOutputHelper);

            // Act
            HttpResponseMessage response = await client.GetAsync("/api/identity/signup/email/verify?code=123456");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
