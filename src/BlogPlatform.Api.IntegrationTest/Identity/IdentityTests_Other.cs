using BlogPlatform.EFCore.Models;
using BlogPlatform.Shared.Identity.Models;
using BlogPlatform.Shared.Models;
using BlogPlatform.Shared.Models.User;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using System.Net;
using System.Net.Http.Json;

namespace BlogPlatform.Api.IntegrationTest.Identity
{
    public partial class IdentityTests
    {
        [Fact]
        public async Task GetUserInfo_Unauthorize()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);

            // Act
            HttpResponseMessage response = await client.GetAsync("/api/identity");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetUserInfo_Ok()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.GetAsync("/api/identity");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            UserRead? read = await response.Content.ReadFromJsonAsync<UserRead>();
            Assert.NotNull(read);
            Assert.Equal(user.Id, read.Id);
            Assert.Equal(user.Name, read.Name);
            Assert.Equal(user.Email, read.Email);
        }

        [Fact]
        public async Task Logout_Ok()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user, true);
            Helper.SetAuthorizeTokenCookie(WebApplicationFactory, client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.PostAsync("/api/identity/logout", null);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Refresh_Empty()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);

            // Act
            HttpResponseMessage response = await client.PostAsync("/api/identity/refresh", null);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Refresh_InvalidToken()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/refresh", new AuthorizeToken("InvalidToken", "InvalidToken"), CancellationToken.None);

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Refresh_Ok()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);

            await Helper.SetAuthorizeTokenCache(WebApplicationFactory, authorizeToken);

            // Act
            HttpResponseMessage response;
            response = await client.PostAsJsonAsync("/api/identity/refresh", authorizeToken, CancellationToken.None);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            AuthorizeToken? bodyToken = await response.Content.ReadFromJsonAsync<AuthorizeToken>();
            Assert.NotNull(bodyToken);
        }

        [Fact]
        public async Task Refresh_TokenExpired()
        {
            // Arrange
            using var scope = WebApplicationFactory.Services.CreateScope();
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);

            WebApplicationFactory<Program> newFactory = WebApplicationFactory.WithWebHostBuilder(conf =>
            {
                conf.ConfigureServices(services =>
                {
                    services.Replace(new ServiceDescriptor(typeof(TimeProvider), new FakeTimeProvider(DateTimeOffset.UtcNow.AddDays(2))));
                });
            });

            HttpClient client = newFactory.CreateLoggingClient(TestOutputHelper);

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/refresh", authorizeToken, CancellationToken.None);

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task FindId_NotFound()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/id/find", new EmailModel("notExist@user.com"));

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task FindId_Ok()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/id/find", new EmailModel("user1@user.com"));

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
