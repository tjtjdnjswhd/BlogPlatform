using BlogPlatform.Api.Identity.Models;
using BlogPlatform.Api.Models;
using BlogPlatform.EFCore;
using BlogPlatform.EFCore.Extensions;
using BlogPlatform.EFCore.Models;

using Microsoft.Extensions.DependencyInjection;

using System.Net;
using System.Net.Http.Json;

namespace BlogPlatform.Api.IntegrationTest.Identity
{
    public partial class IdentityTests
    {
        [Fact]
        public async Task OAuthLogin_Challenge()
        {
            // Arrange
            HttpClient client = CreateClient();

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/login/oauth", new Models.OAuthProvider("Google"));

            // Assert
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        }

        [Fact]
        public async Task OAuthSignUp_InvalidUserName()
        {
            // Arrange
            HttpClient client = CreateClient();

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/signup/oauth", new OAuthSignUpModel("Google", "ab"));

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task OAuthSignUp_Challenge()
        {
            // Arrange
            HttpClient client = CreateClient();

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/signup/oauth", new OAuthSignUpModel("Google", "userName"));

            // Assert
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        }

        [Fact]
        public async Task AddOAuth_Unauthorize()
        {
            // Arrange
            HttpClient client = CreateClient();

            // Act
            HttpResponseMessage response = await client.GetAsync("/api/identity/oauth?provider=google");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task AddOAuth_Challenge_Header()
        {
            // Arrange
            HttpClient client = CreateClient();
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.GetAsync("/api/identity/oauth?provider=google");

            // Assert
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        }

        [Fact]
        public async Task AddOAuth_Challenge_Cookie()
        {
            // Arrange
            HttpClient client = CreateClient();
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizeTokenCookie(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.GetAsync("/api/identity/oauth?provider=google");

            // Assert
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        }

        [Fact]
        public async Task RemoveOAuth_Unauthorize()
        {
            // Arrange
            HttpClient client = CreateClient();

            // Act
            HttpResponseMessage response = await client.DeleteAsync("/api/identity/oauth/google");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task RemoveOAuth_Conflict()
        {
            // Arrange
            HttpClient client = CreateClient();
            using var scope = WebApplicationFactory.Services.CreateScope();
            BlogPlatformDbContext dbContext = scope.ServiceProvider.GetRequiredService<BlogPlatformDbContext>();
            User oauthUser = new("oauthOnly", "oauthOnly@user.com");
            dbContext.Users.Add(oauthUser);
            dbContext.SaveChanges();

            OAuthAccount oAuthAccount = new("googleNameIdentifier", 1, oauthUser.Id);
            dbContext.OAuthAccounts.Add(oAuthAccount);
            dbContext.SaveChanges();

            oauthUser.Roles.Add(dbContext.Roles.Where(r => r.Name == "user").First());
            dbContext.SaveChanges();

            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, oauthUser);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.DeleteAsync("/api/identity/oauth/google");

            // Assert
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Assert.True(dbContext.OAuthAccounts.Any(o => o.Id == oAuthAccount.Id));
        }

        [Fact]
        public async Task RemoveOAuth_UserNotFound()
        {
            // Arrange
            HttpClient client = CreateClient();
            using var scope = WebApplicationFactory.Services.CreateScope();
            BlogPlatformDbContext dbContext = scope.ServiceProvider.GetRequiredService<BlogPlatformDbContext>();
            User oauthUser = new("oauthOnly", "oauthOnly@user.com");
            dbContext.Users.Add(oauthUser);
            dbContext.SaveChanges();

            OAuthAccount oAuthAccount = new("googleNameIdentifier", 1, oauthUser.Id);
            dbContext.OAuthAccounts.Add(oAuthAccount);
            dbContext.SaveChanges();

            oauthUser.Roles.Add(dbContext.Roles.Where(r => r.Name == "user").First());
            dbContext.SaveChanges();

            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, oauthUser);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            Helper.SoftDelete(WebApplicationFactory, oauthUser);

            // Act
            HttpResponseMessage response = await client.DeleteAsync("/api/identity/oauth/google");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.True(dbContext.OAuthAccounts.IgnoreSoftDeleteFilter().Any(o => o.Id == oAuthAccount.Id));
        }

        [Fact]
        public async Task RemoveOAuth_ProviderNotFound()
        {
            // Arrange
            HttpClient client = CreateClient();
            using var scope = WebApplicationFactory.Services.CreateScope();
            BlogPlatformDbContext dbContext = scope.ServiceProvider.GetRequiredService<BlogPlatformDbContext>();
            User oauthUser = new("newUser", "newUser@user.com");
            dbContext.Users.Add(oauthUser);
            dbContext.SaveChanges();

            OAuthAccount oAuthAccount = new("google", 1, oauthUser.Id);
            dbContext.OAuthAccounts.Add(oAuthAccount);
            dbContext.SaveChanges();

            oauthUser.Roles.Add(dbContext.Roles.Where(r => r.Name == "user").First());
            dbContext.SaveChanges();

            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, oauthUser);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.DeleteAsync("/api/identity/oauth/notexist");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.True(dbContext.OAuthAccounts.Any(o => o.Id == oAuthAccount.Id));
        }

        [Fact]
        public async Task RemoveOAuth_OAuthOnly_Ok()
        {
            // Arrange
            HttpClient client = CreateClient();
            using var scope = WebApplicationFactory.Services.CreateScope();
            BlogPlatformDbContext dbContext = scope.ServiceProvider.GetRequiredService<BlogPlatformDbContext>();
            User oauthUser = new("newUser", "newUser@user.com");
            dbContext.Users.Add(oauthUser);
            dbContext.SaveChanges();

            OAuthAccount googleAccount = new("googleNameIdentifier", 1, oauthUser.Id);
            OAuthAccount naverAccount = new("naverNameIdentifier", 2, oauthUser.Id);

            dbContext.OAuthAccounts.AddRange(googleAccount, naverAccount);
            dbContext.SaveChanges();

            oauthUser.Roles.Add(dbContext.Roles.Where(r => r.Name == "user").First());
            dbContext.SaveChanges();

            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, oauthUser);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.DeleteAsync("/api/identity/oauth/google");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(dbContext.OAuthAccounts.IgnoreSoftDeleteFilter().Any(o => o.Id == googleAccount.Id));
            Assert.False(dbContext.OAuthAccounts.Any(o => o.Id == googleAccount.Id));
            Assert.True(dbContext.OAuthAccounts.Any(o => o.Id == naverAccount.Id));
        }

        [Fact]
        public async Task RemoveOAuth_OneOAuthWithBasic_Ok()
        {
            // Arrange
            HttpClient client = CreateClient();
            using var scope = WebApplicationFactory.Services.CreateScope();
            BlogPlatformDbContext dbContext = scope.ServiceProvider.GetRequiredService<BlogPlatformDbContext>();
            User oauthUser = new("newUser", "newUser@user.com");
            dbContext.Users.Add(oauthUser);
            dbContext.SaveChanges();

            OAuthAccount googleAccount = new("googleNameIdentifier", 1, oauthUser.Id);
            BasicAccount basicAccount = new("newUser", "newUser", oauthUser.Id);

            dbContext.OAuthAccounts.Add(googleAccount);
            dbContext.BasicAccounts.Add(basicAccount);
            dbContext.SaveChanges();

            oauthUser.Roles.Add(dbContext.Roles.Where(r => r.Name == "user").First());
            dbContext.SaveChanges();

            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, oauthUser);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.DeleteAsync("/api/identity/oauth/google");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.False(dbContext.OAuthAccounts.Any(o => o.Id == googleAccount.Id));
            Assert.True(dbContext.OAuthAccounts.IgnoreSoftDeleteFilter().Any(o => o.Id == googleAccount.Id));
            Assert.True(dbContext.BasicAccounts.Any(b => b.Id == basicAccount.Id));
        }
    }
}
