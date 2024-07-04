using BlogPlatform.EFCore.Models;
using BlogPlatform.Shared.Identity.Models;

using System.Net;

namespace BlogPlatform.Api.IntegrationTest.Identity
{
    public partial class IdentityTests
    {
        [Fact]
        public async Task Token_FromCookie_HeaderAuthorize_Unauthorized()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user, true);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.GetAsync("/api/identity");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Token_FromBody_CookieAuthorize_Unauthorized()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user, false);
            Helper.SetAuthorizeTokenCookie(WebApplicationFactory, client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.GetAsync("/api/identity");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
