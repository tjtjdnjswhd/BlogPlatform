using BlogPlatform.EFCore;
using BlogPlatform.EFCore.Models;
using BlogPlatform.Shared.Identity.Models;
using BlogPlatform.Shared.Models;

using Microsoft.Extensions.DependencyInjection;

using System.Net;
using System.Net.Http.Json;

namespace BlogPlatform.Api.IntegrationTest.Identity
{
    public partial class IdentityTests
    {
        [Fact]
        public async Task ChangePassword_BasicAccountNotFound()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);

            User user = Helper.GetFirstEntity<User>(WebApplicationFactory);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(client, authorizeToken);
            Helper.SoftDelete(WebApplicationFactory, user);

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/password/change", new PasswordChangeModel("fadsa", "currentPW", "newPW"));

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task ChangePassword_WrongCurrentPassword()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);

            using var scope = WebApplicationFactory.Services.CreateScope();
            BlogPlatformDbContext dbContext = scope.ServiceProvider.GetRequiredService<BlogPlatformDbContext>();
            BasicAccount basicAccount = dbContext.BasicAccounts.First();

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/password/change", new PasswordChangeModel(basicAccount.AccountId, "wrongPassword", "user11pw"));

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact, ResetDataAfterTest]
        public async Task ChangePassword_Ok()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);

            using var scope = WebApplicationFactory.Services.CreateScope();
            BlogPlatformDbContext dbContext = scope.ServiceProvider.GetRequiredService<BlogPlatformDbContext>();
            BasicAccount basicAccount = dbContext.BasicAccounts.First();

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/password/change", new PasswordChangeModel(basicAccount.AccountId, "user1pw", "newPassword"));

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task ResetPassword_NotFound()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/password/reset", new EmailModel("notExist@notExist"));

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact, ResetDataAfterTest]
        public async Task ResetPassword_Ok()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory);

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/password/reset", new EmailModel(user.Email));

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task ChangeName_UserNotFound()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);

            User user = Helper.GetFirstEntity<User>(WebApplicationFactory);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(client, authorizeToken);
            Helper.SoftDelete(WebApplicationFactory, user);

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/name", new UserNameModel("newName"));

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Changename_InvalidName()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);

            User user = Helper.GetFirstEntity<User>(WebApplicationFactory);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/name", new UserNameModel("ab"));

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact, ResetDataAfterTest]
        public async Task ChangeName_Ok()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);

            User user = Helper.GetFirstEntity<User>(WebApplicationFactory);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/name", new UserNameModel("newName"));

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task ChangeEmail_Unauthorize()
        {
            // Arrange
            HttpClient httpClient = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);

            // Act
            HttpResponseMessage response = await httpClient.PostAsJsonAsync("/api/identity/email/change", new EmailModel("newEmail@email.com"));

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact, ResetDataAfterTest]
        public async Task ChangeEmail_Ok()
        {
            // Arrange
            HttpClient httpClient = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);

            User user = Helper.GetFirstEntity<User>(WebApplicationFactory);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(httpClient, authorizeToken);

            // Act
            HttpResponseMessage response = await httpClient.PostAsJsonAsync("/api/identity/email/change", new EmailModel("newEmail@email.com"));

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task ConfirmChangeEmail_WrongCode()
        {
            // Arrange
            HttpClient httpClient = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);

            User user = Helper.GetFirstEntity<User>(WebApplicationFactory);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(httpClient, authorizeToken);

            // Act
            HttpResponseMessage response = await httpClient.GetAsync("/api/identity/email/change?code=wrongCode");

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ConfirmChangeEmail_UserNotFound()
        {
            // Arrange
            HttpClient httpClient = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);

            User user = Helper.GetFirstEntity<User>(WebApplicationFactory);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(httpClient, authorizeToken);
            Helper.SoftDelete(WebApplicationFactory, user);

            string newEmail = "newEmail@email.com";
            await Helper.SetChangeEmailVerifyCodeAsync(WebApplicationFactory, user.Id, "verifyCode", newEmail);

            // Act
            HttpResponseMessage response = await httpClient.GetAsync("/api/identity/email/change?code=verifyCode");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact, ResetDataAfterTest]
        public async Task ConfirmChangeEmail_Ok()
        {
            // Arrange
            HttpClient httpClient = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);

            User user = Helper.GetFirstEntity<User>(WebApplicationFactory);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(httpClient, authorizeToken);

            string newEmail = "newEmail@email.com";
            await Helper.SetChangeEmailVerifyCodeAsync(WebApplicationFactory, user.Id, "verifyCode", newEmail);

            // Act
            HttpResponseMessage response = await httpClient.GetAsync("/api/identity/email/change?code=verifyCode");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Helper.ReloadEntity(WebApplicationFactory, user);
            Assert.Equal(newEmail, user.Email);
        }
    }
}
