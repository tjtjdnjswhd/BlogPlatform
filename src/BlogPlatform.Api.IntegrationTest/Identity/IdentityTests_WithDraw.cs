﻿using BlogPlatform.EFCore.Extensions;
using BlogPlatform.EFCore.Models;
using BlogPlatform.Shared.Identity.Models;

using System.Net;
using System.Net.Http.Json;

namespace BlogPlatform.Api.IntegrationTest.Identity
{
    public partial class IdentityTests
    {
        [Fact]
        public async Task WithDraw_Unauthorize()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);

            // Act
            HttpResponseMessage response = await client.PostAsync("/api/identity/withdraw", null);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task WithDraw_UserNotFound()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);

            User user = Helper.GetFirstEntity<User>(WebApplicationFactory);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(client, authorizeToken);
            Helper.SoftDelete(WebApplicationFactory, user);

            // Act
            HttpResponseMessage response = await client.PostAsync("/api/identity/withdraw", null);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        [ResetDataAfterTest]
        public async Task WithDraw_Ok()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);

            User user = Helper.GetFirstEntity<User>(WebApplicationFactory);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.PostAsync("/api/identity/withdraw", null);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact, ResetDataAfterTest]
        public async Task WithDraw_SignUpSameId_Conflict()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);

            User user = Helper.GetFirstEntity<User>(WebApplicationFactory);
            BasicAccount basicAccount = Helper.GetFirstEntity<BasicAccount>(WebApplicationFactory, b => b.UserId == user.Id, false);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            HttpResponseMessage withDrawResponse = await client.PostAsync("/api/identity/withdraw", null);
            withDrawResponse.EnsureSuccessStatusCode();
            Helper.ResetAuthorizationHeader(client);

            string email = "user55@user.com";
            await Helper.SetSignUpVerifiedEmailAsync(WebApplicationFactory, email);

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/signup/basic", new BasicSignUpInfo(basicAccount.AccountId, "user55pw", "user55", email));

            // Assert
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Helper.ReloadEntity(WebApplicationFactory, user);
            Assert.Equal(1, user.SoftDeleteLevel);
            Assert.False(user.IsSoftDeletedAtDefault());
        }

        [Fact, ResetDataAfterTest]
        public async Task WithDraw_SignUpSameEmail_Conflict()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);

            User user = Helper.GetFirstEntity<User>(WebApplicationFactory);
            Helper.LoadCollection(WebApplicationFactory, user, u => u.BasicAccounts, true);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            HttpResponseMessage withDrawResponse = await client.PostAsync("/api/identity/withdraw", null);
            withDrawResponse.EnsureSuccessStatusCode();
            Helper.ResetAuthorizationHeader(client);

            string email = user.Email;
            await Helper.SetSignUpVerifiedEmailAsync(WebApplicationFactory, email);

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/signup/basic", new BasicSignUpInfo("user55Id", "user55pw", "user55", email));

            // Assert
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Helper.ReloadEntity(WebApplicationFactory, user);
            Assert.Equal(1, user.SoftDeleteLevel);
            Assert.False(user.IsSoftDeletedAtDefault());
        }

        [Fact, ResetDataAfterTest]
        public async Task WithDraw_Expired_SignUpSame_Id_Email_Ok()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);

            User user = Helper.GetFirstEntity<User>(WebApplicationFactory);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            HttpResponseMessage withDrawResponse = await client.PostAsync("/api/identity/withdraw", null);
            withDrawResponse.EnsureSuccessStatusCode();

            user = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Id == user.Id, true);
            user.SoftDeletedAt = DateTimeOffset.UtcNow.AddDays(-2);
            Helper.UpdateEntity(WebApplicationFactory, user);
            BasicAccount basicAccount = Helper.GetFirstEntity<BasicAccount>(WebApplicationFactory, b => b.UserId == user.Id, true);

            Helper.ResetAuthorizationHeader(client);

            string email = user.Email;
            await Helper.SetSignUpVerifiedEmailAsync(WebApplicationFactory, email);

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/signup/basic", new BasicSignUpInfo(basicAccount.AccountId, "user55pw", "user55", email));

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Helper.ReloadEntity(WebApplicationFactory, user);
            Assert.Equal(1, user.SoftDeleteLevel);
            Assert.False(user.IsSoftDeletedAtDefault());

            User newUser = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Email == email);
            Assert.Equal(0, newUser.SoftDeleteLevel);
            Assert.True(newUser.IsSoftDeletedAtDefault());
        }

        [Fact, ResetDataAfterTest]
        public async Task CancelWithDraw_Expired()
        {
            // Arrange
            HttpClient httpClient = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);

            User user = Helper.GetFirstEntity<User>(WebApplicationFactory);
            BasicAccount basicAccount = Helper.GetFirstEntity<BasicAccount>(WebApplicationFactory, b => b.UserId == user.Id);
            Helper.SoftDelete(WebApplicationFactory, user);
            user.SoftDeletedAt = DateTimeOffset.UtcNow.AddDays(-2);
            Helper.UpdateEntity(WebApplicationFactory, user);

            // Act
            HttpResponseMessage response = await httpClient.PostAsJsonAsync("/api/identity/withdraw/cancel", new BasicLoginInfo(basicAccount.AccountId, "user1pw"));

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CancelWithDraw_WithDrawNotRequested()
        {
            // Arrange
            HttpClient httpClient = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);

            User user = Helper.GetFirstEntity<User>(WebApplicationFactory);
            BasicAccount basicAccount = Helper.GetFirstEntity<BasicAccount>(WebApplicationFactory, b => b.UserId == user.Id);

            // Act
            HttpResponseMessage response = await httpClient.PostAsJsonAsync("/api/identity/withdraw/cancel", new BasicLoginInfo(basicAccount.AccountId, "user1pw"));

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact, ResetDataAfterTest]
        public async Task CancelWithDraw_Ok()
        {
            // Arrange
            HttpClient httpClient = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);

            User user = Helper.GetFirstEntity<User>(WebApplicationFactory);
            BasicAccount basicAccount = Helper.GetFirstEntity<BasicAccount>(WebApplicationFactory, b => b.UserId == user.Id);
            Helper.SoftDelete(WebApplicationFactory, user);

            // Act
            HttpResponseMessage response = await httpClient.PostAsJsonAsync("/api/identity/withdraw/cancel", new BasicLoginInfo(basicAccount.AccountId, "user1pw"));

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Helper.ReloadEntity(WebApplicationFactory, user);
            Assert.Equal(0, user.SoftDeleteLevel);
            Assert.True(user.IsSoftDeletedAtDefault());
        }
    }
}
