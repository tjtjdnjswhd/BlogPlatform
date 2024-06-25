using BlogPlatform.Api.Identity.Constants;
using BlogPlatform.Api.Identity.Models;
using BlogPlatform.Api.Identity.Services.Interfaces;
using BlogPlatform.Api.Models;
using BlogPlatform.Api.Services.Interfaces;
using BlogPlatform.EFCore.Models;

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
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Refresh_Ok(bool useCookie)
        {
            // Arrange
            HttpClient client = CreateClient();
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);

            await Helper.SetAuthorizeTokenCache(WebApplicationFactory, authorizeToken);

            // Act
            HttpResponseMessage response;
            if (useCookie)
            {
                client.DefaultRequestHeaders.Add(HeaderNameConstants.AuthorizeTokenSetCookie, "true");
                Helper.SetAuthorizeTokenCookie(client, authorizeToken);
                response = await client.PostAsync("/api/identity/refresh", null);
            }
            else
            {
                response = await client.PostAsJsonAsync("/api/identity/refresh", authorizeToken, CancellationToken.None);
            }

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            if (useCookie)
            {
                response.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? cookieValues);
                Assert.NotNull(cookieValues);
                TestOutputHelper.WriteLine($"cookieValues:{cookieValues}");
            }
            else
            {
                AuthorizeToken? bodyToken = await response.Content.ReadFromJsonAsync<AuthorizeToken>();
                Assert.NotNull(bodyToken);
            }
        }

        [Fact]
        public async Task Refresh_TokenExpired()
        {
            // Arrange
            using var scope = WebApplicationFactory.Services.CreateScope();
            IJwtService jwtService = scope.ServiceProvider.GetRequiredService<IJwtService>();
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory);
            AuthorizeToken authorizeToken = await jwtService.GenerateTokenAsync(user);

            WebApplicationFactory = WebApplicationFactory.WithWebHostBuilder(conf =>
            {
                conf.ConfigureServices(services =>
                {
                    services.Replace(new ServiceDescriptor(typeof(TimeProvider), new FakeTimeProvider(DateTimeOffset.UtcNow.AddDays(2))));
                });
            });

            HttpClient client = CreateClient();

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/refresh", authorizeToken, CancellationToken.None);

            // Assert
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
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
