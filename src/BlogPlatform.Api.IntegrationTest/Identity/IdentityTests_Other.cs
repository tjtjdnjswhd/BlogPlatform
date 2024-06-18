using BlogPlatform.Api.Identity.Models;
using BlogPlatform.Api.Identity.Services.Interfaces;
using BlogPlatform.Api.Models;
using BlogPlatform.EFCore.Models;

using Microsoft.Extensions.DependencyInjection;

using Moq;

using System.Net;
using System.Net.Http.Json;

namespace BlogPlatform.Api.IntegrationTest.Identity
{
    public partial class IdentityTests
    {
        [Fact]
        public async Task Logout_NoContent()
        {
            // Arrange
            HttpClient client = CreateClient();
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.PostAsync("/api/identity/logout", null);

            // Assert
            PrintResponse(response);
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task Logout_Ok()
        {
            // Arrange
            HttpClient client = CreateClient();
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizeTokenCookie(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.PostAsync("/api/identity/logout", null);

            // Assert
            PrintResponse(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Refresh_Empty()
        {
            // Arrange
            HttpClient client = CreateClient();

            // Act
            HttpResponseMessage response = await client.PostAsync("/api/identity/refresh", null);

            // Assert
            PrintResponse(response);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Refresh_InvalidToken()
        {
            // Arrange
            HttpClient client = CreateClient();

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/refresh", new AuthorizeToken("InvalidToken", "InvalidToken"), CancellationToken.None);

            // Assert
            PrintResponse(response);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Refresh_Ok()
        {
            // Arrange
            HttpClient client = CreateClient();
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            await Helper.SetAuthorizeTokenCache(WebApplicationFactory, authorizeToken);

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/refresh", authorizeToken, CancellationToken.None);

            /*
             ------------------------------
             JwtService.GetBodyToken()에서 json -> AuthorizeToken 역직렬화 오류. AccessToken, RefreshToken 둘 다 null
             ------------------------------
             */
            // Assert
            PrintResponse(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Refresh_TokenExpired()
        {
            // Arrange
            WebApplicationFactory = WebApplicationFactory.WithWebHostBuilder(conf =>
            {
                conf.ConfigureServices(services =>
                {
                    services.AddSingleton<TimeProvider, FakeTimeProvider>(_ => new FakeTimeProvider(DateTimeOffset.UtcNow.AddDays(1)));
                });
            });

            HttpClient client = CreateClient();
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            await Helper.SetAuthorizeTokenCache(WebApplicationFactory, authorizeToken);

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/refresh", authorizeToken, CancellationToken.None);

            // Assert
            PrintResponse(response);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task FindId_NotFound()
        {
            // Arrange
            HttpClient client = CreateClient();

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/id/find", new EmailModel("notExist@user.com"));

            // Assert
            PrintResponse(response);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task FindId_Ok()
        {
            // Arrange
            SetIEmailServiceMock(new Mock<IUserEmailService>());
            HttpClient client = CreateClient();

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/id/find", new EmailModel("user1@user.com"));

            // Assert
            PrintResponse(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
