using BlogPlatform.Api.Identity.Constants;
using BlogPlatform.EFCore;
using BlogPlatform.EFCore.Extensions;
using BlogPlatform.EFCore.Models;
using BlogPlatform.Shared.Identity.Models;
using BlogPlatform.Shared.Models;
using BlogPlatform.Shared.Models.Admin;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using System.Net;
using System.Net.Http.Json;

using Xunit.Abstractions;

namespace BlogPlatform.Api.IntegrationTest.Admin
{
    public class AdminTests : TestBase, ITestDataReset
    {
        public AdminTests(WebApplicationFactoryFixture applicationFactoryFixture, ITestOutputHelper testOutputHelper) : base(applicationFactoryFixture, testOutputHelper, "integration_admin_test")
        {
        }

        [Fact]
        public async Task DeleteUser_Unauthorized()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Roles.Any(r => r.Name == PolicyConstants.UserRolePolicy));

            HttpRequestMessage httpRequestMessage = new(HttpMethod.Delete, "/api/admin/user")
            {
                Content = JsonContent.Create(new EmailModel(user.Email))
            };

            // Act
            HttpResponseMessage response = await client.SendAsync(httpRequestMessage);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task DeleteUser_Forbidden()
        {
            // Arrnage
            HttpClient client = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Roles.Any(r => r.Name == PolicyConstants.UserRolePolicy));
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            HttpRequestMessage httpRequestMessage = new(HttpMethod.Delete, $"/api/admin/user")
            {
                Content = JsonContent.Create(new EmailModel(user.Email))
            };

            // Act
            HttpResponseMessage response = await client.SendAsync(httpRequestMessage);

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task DeleteUser_NotFound()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);
            User admin = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Roles.Any(r => r.Name == PolicyConstants.AdminRolePolicy));
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, admin);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            HttpRequestMessage httpRequestMessage = new(HttpMethod.Delete, "/api/admin/user")
            {
                Content = JsonContent.Create(new EmailModel("notFoundEmail@user.com"))
            };

            // Act
            HttpResponseMessage response = await client.SendAsync(httpRequestMessage);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact, ResetDataAfterTest]
        public async Task DeleteUser_NoContent()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);
            User admin = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Roles.Any(r => r.Name == PolicyConstants.AdminRolePolicy));
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Roles.Any(r => r.Name == PolicyConstants.UserRolePolicy));
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, admin);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            HttpRequestMessage httpRequestMessage = new(HttpMethod.Delete, "/api/admin/user")
            {
                Content = JsonContent.Create(new EmailModel(user.Email))
            };

            // Act
            HttpResponseMessage response = await client.SendAsync(httpRequestMessage);

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            User deletedUser = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Email == user.Email, true);
            Assert.Equal(1, deletedUser.SoftDeleteLevel);
            Assert.False(deletedUser.IsSoftDeletedAtDefault());

            EFCore.Models.Blog userBlog = Helper.GetFirstEntity<EFCore.Models.Blog>(WebApplicationFactory, b => b.UserId == user.Id, true);
            Assert.Equal(2, userBlog.SoftDeleteLevel);
            Assert.False(userBlog.IsSoftDeletedAtDefault());

            EFCore.Models.Category userCategory = Helper.GetFirstEntity<EFCore.Models.Category>(WebApplicationFactory, c => c.Blog.UserId == user.Id, true);
            Assert.Equal(3, userCategory.SoftDeleteLevel);
            Assert.False(userCategory.IsSoftDeletedAtDefault());

            EFCore.Models.Post userPost = Helper.GetFirstEntity<EFCore.Models.Post>(WebApplicationFactory, p => p.Category.Blog.UserId == user.Id, true);
            Assert.Equal(4, userPost.SoftDeleteLevel);
            Assert.False(userPost.IsSoftDeletedAtDefault());

            EFCore.Models.Comment userComment = Helper.GetFirstEntity<EFCore.Models.Comment>(WebApplicationFactory, c => c.UserId == user.Id, true);
            Assert.Equal(5, userComment.SoftDeleteLevel);
            Assert.False(userComment.IsSoftDeletedAtDefault());
        }

        [Fact]
        public async Task RestoreUser_Unauthorized()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Roles.Any(r => r.Name == PolicyConstants.UserRolePolicy));

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/admin/user/restore", new EmailModel(user.Email));

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task RestoreUser_Forbidden()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Blog.Count == 0 && u.Roles.Any(r => r.Name == PolicyConstants.UserRolePolicy));
            User blogOwnUser = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Blog.Count != 0);

            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/admin/user/restore", new EmailModel(blogOwnUser.Email));

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task RestoreUser_NotFound()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);
            User admin = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Roles.Any(r => r.Name == PolicyConstants.AdminRolePolicy));
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, admin);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/admin/user/restore", new EmailModel("notFoundEmail@user.com"));

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task RestoreUser_NotDeleted_BadRequest()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);
            User admin = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Roles.Any(r => r.Name == PolicyConstants.AdminRolePolicy));
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Roles.Any(r => r.Name == PolicyConstants.UserRolePolicy));
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, admin);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/admin/user/restore", new EmailModel(user.Email));

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact, ResetDataAfterTest]
        public async Task RestoreUser_NoContent()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);
            User admin = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Roles.Any(r => r.Name == PolicyConstants.AdminRolePolicy));
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Blog.Count != 0 && u.Roles.Any(r => r.Name == PolicyConstants.UserRolePolicy));
            Helper.SoftDelete(WebApplicationFactory, user);

            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, admin);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/admin/user/restore", new EmailModel(user.Email));

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            User restoredUser = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Email == user.Email, true);
            Assert.Equal(0, restoredUser.SoftDeleteLevel);
            Assert.True(restoredUser.IsSoftDeletedAtDefault());

            EFCore.Models.Blog userBlog = Helper.GetFirstEntity<EFCore.Models.Blog>(WebApplicationFactory, b => b.UserId == user.Id, true);
            Assert.Equal(0, userBlog.SoftDeleteLevel);
            Assert.True(userBlog.IsSoftDeletedAtDefault());

            EFCore.Models.Category userCategory = Helper.GetFirstEntity<EFCore.Models.Category>(WebApplicationFactory, c => c.Blog.UserId == user.Id, true);
            Assert.Equal(0, userCategory.SoftDeleteLevel);
            Assert.True(userCategory.IsSoftDeletedAtDefault());

            EFCore.Models.Post userPost = Helper.GetFirstEntity<EFCore.Models.Post>(WebApplicationFactory, p => p.Category.Blog.UserId == user.Id, true);
            Assert.Equal(0, userPost.SoftDeleteLevel);
            Assert.True(userPost.IsSoftDeletedAtDefault());

            EFCore.Models.Comment userComment = Helper.GetFirstEntity<EFCore.Models.Comment>(WebApplicationFactory, c => c.UserId == user.Id, true);
            Assert.Equal(0, userComment.SoftDeleteLevel);
            Assert.True(userComment.IsSoftDeletedAtDefault());
        }

        [Fact]
        public async Task BanUser_Unauthorized()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Roles.Any(r => r.Name == PolicyConstants.UserRolePolicy));
            User otherUser = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Id != user.Id);

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/admin/user/ban", new UserBanModel(user.Email, TimeSpan.FromDays(1)));

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task BanUser_Forbidden()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Roles.Any(r => r.Name == PolicyConstants.UserRolePolicy));
            User otherUser = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Id != user.Id);

            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/admin/user/ban", new UserBanModel(otherUser.Email, TimeSpan.FromDays(1)));

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task BanUser_NotFound()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);
            User admin = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Roles.Any(r => r.Name == PolicyConstants.AdminRolePolicy));
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, admin);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/admin/user/ban", new UserBanModel("notExistUser@user.com", TimeSpan.FromDays(1)));

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact, ResetDataAfterTest]
        public async Task BanUser_Ok()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);
            User admin = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Roles.Any(r => r.Name == PolicyConstants.AdminRolePolicy));
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Roles.Any(r => r.Name == PolicyConstants.UserRolePolicy));
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, admin);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/admin/user/ban", new UserBanModel(user.Email, TimeSpan.FromDays(1)));

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            User bannedUser = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Email == user.Email);
            Assert.NotNull(bannedUser.BanExpiresAt);
            Assert.True(bannedUser.BanExpiresAt > DateTime.UtcNow);
        }

        [Fact]
        public async Task UnbanUser_Unauthorized()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Roles.Any(r => r.Name == PolicyConstants.UserRolePolicy));

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/admin/user/unban", new EmailModel(user.Email));

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task UnbanUser_Forbidden()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Roles.Any(r => r.Name == PolicyConstants.UserRolePolicy));
            User otherUser = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Id != user.Id);

            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/admin/user/unban", new EmailModel(otherUser.Email));

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task UnbanUser_NotFound()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);
            User admin = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Roles.Any(r => r.Name == PolicyConstants.AdminRolePolicy));
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, admin);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/admin/user/unban", new EmailModel("notExistUser@user.com"));

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact, ResetDataAfterTest]
        public async Task UnbanUser_Ok()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);
            User admin = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Roles.Any(r => r.Name == PolicyConstants.AdminRolePolicy));
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Roles.Any(r => r.Name == PolicyConstants.UserRolePolicy));
            user.BanExpiresAt = DateTime.UtcNow.Add(TimeSpan.FromDays(1));
            Helper.UpdateEntity(WebApplicationFactory, user);

            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, admin);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/admin/user/unban", new EmailModel(user.Email));

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            User unbannedUser = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Email == user.Email);
            Assert.Null(unbannedUser.BanExpiresAt);
        }

        [Fact]
        public async Task DeletePost_Unauthorized()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);
            EFCore.Models.Post post = Helper.GetFirstEntity<EFCore.Models.Post>(WebApplicationFactory);

            // Act
            HttpResponseMessage response = await client.DeleteAsync($"/api/admin/post/{post.Id}");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task DeletePost_Forbidden()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Roles.Any(r => r.Name == PolicyConstants.UserRolePolicy));
            EFCore.Models.Post post = Helper.GetFirstEntity<EFCore.Models.Post>(WebApplicationFactory);

            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.DeleteAsync($"/api/admin/post/{post.Id}");

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task DeletePost_NotFound()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);
            User admin = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Roles.Any(r => r.Name == PolicyConstants.AdminRolePolicy));
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, admin);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.DeleteAsync("/api/admin/post/111");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact, ResetDataAfterTest]
        public async Task DeletePost_NoContent()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);
            User admin = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Roles.Any(r => r.Name == PolicyConstants.AdminRolePolicy));
            EFCore.Models.Post post = Helper.GetFirstEntity<EFCore.Models.Post>(WebApplicationFactory);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, admin);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.DeleteAsync($"/api/admin/post/{post.Id}");

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            EFCore.Models.Post deletedPost = Helper.GetFirstEntity<EFCore.Models.Post>(WebApplicationFactory, p => p.Id == post.Id, true);
            Assert.Equal(1, deletedPost.SoftDeleteLevel);
            Assert.False(deletedPost.IsSoftDeletedAtDefault());

            EFCore.Models.Comment postComment = Helper.GetFirstEntity<EFCore.Models.Comment>(WebApplicationFactory, c => c.PostId == post.Id, true);
            Assert.Equal(2, postComment.SoftDeleteLevel);
            Assert.False(postComment.IsSoftDeletedAtDefault());
        }

        [Fact]
        public async Task RestorePost_Unauthorized()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);
            EFCore.Models.Post post = Helper.GetFirstEntity<EFCore.Models.Post>(WebApplicationFactory);

            // Act
            HttpResponseMessage response = await client.PostAsync($"/api/admin/post/{post.Id}/restore", null);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task RestorePost_Forbidden()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Roles.Any(r => r.Name == PolicyConstants.UserRolePolicy));
            EFCore.Models.Post post = Helper.GetFirstEntity<EFCore.Models.Post>(WebApplicationFactory);

            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.PostAsync($"/api/admin/post/{post.Id}/restore", null);

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task RestorePost_NotFound()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);
            User admin = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Roles.Any(r => r.Name == PolicyConstants.AdminRolePolicy));
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, admin);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.PostAsync("/api/admin/post/111/restore", null);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task RestorePost_NotDeleted_BadRequest()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);
            User admin = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Roles.Any(r => r.Name == PolicyConstants.AdminRolePolicy));
            EFCore.Models.Post post = Helper.GetFirstEntity<EFCore.Models.Post>(WebApplicationFactory);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, admin);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.PostAsync($"/api/admin/post/{post.Id}/restore", null);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact, ResetDataAfterTest]
        public async Task RestorePost_NoContent()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);
            User admin = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Roles.Any(r => r.Name == PolicyConstants.AdminRolePolicy));
            EFCore.Models.Post post = Helper.GetFirstEntity<EFCore.Models.Post>(WebApplicationFactory);
            Helper.SoftDelete(WebApplicationFactory, post);

            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, admin);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.PostAsync($"/api/admin/post/{post.Id}/restore", null);

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            EFCore.Models.Post restoredPost = Helper.GetFirstEntity<EFCore.Models.Post>(WebApplicationFactory, p => p.Id == post.Id, true);
            Assert.Equal(0, restoredPost.SoftDeleteLevel);
            Assert.True(restoredPost.IsSoftDeletedAtDefault());

            EFCore.Models.Comment postComment = Helper.GetFirstEntity<EFCore.Models.Comment>(WebApplicationFactory, c => c.PostId == post.Id, true);
            Assert.Equal(0, postComment.SoftDeleteLevel);
            Assert.True(postComment.IsSoftDeletedAtDefault());
        }

        [Fact]
        public async Task DeleteComment_Unauthorized()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);
            EFCore.Models.Comment comment = Helper.GetFirstEntity<EFCore.Models.Comment>(WebApplicationFactory);

            // Act
            HttpResponseMessage response = await client.DeleteAsync($"/api/admin/comment/{comment.Id}");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task DeleteComment_Forbidden()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Roles.Any(r => r.Name == PolicyConstants.UserRolePolicy));
            EFCore.Models.Comment comment = Helper.GetFirstEntity<EFCore.Models.Comment>(WebApplicationFactory);

            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.DeleteAsync($"/api/admin/comment/{comment.Id}");

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task DeleteComment_NotFound()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);
            User admin = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Roles.Any(r => r.Name == PolicyConstants.AdminRolePolicy));
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, admin);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.DeleteAsync("/api/admin/comment/111");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact, ResetDataAfterTest]
        public async Task DeleteComment_NoContent()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);
            User admin = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Roles.Any(r => r.Name == PolicyConstants.AdminRolePolicy));
            EFCore.Models.Comment comment = Helper.GetFirstEntity<EFCore.Models.Comment>(WebApplicationFactory);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, admin);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.DeleteAsync($"/api/admin/comment/{comment.Id}");

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            EFCore.Models.Comment deletedComment = Helper.GetFirstEntity<EFCore.Models.Comment>(WebApplicationFactory, c => c.Id == comment.Id, true);
            Assert.Equal(1, deletedComment.SoftDeleteLevel);
            Assert.False(deletedComment.IsSoftDeletedAtDefault());
        }

        [Fact]
        public async Task RestoreComment_Unauthorized()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);
            EFCore.Models.Comment comment = Helper.GetFirstEntity<EFCore.Models.Comment>(WebApplicationFactory);

            // Act
            HttpResponseMessage response = await client.PostAsync($"/api/admin/comment/{comment.Id}/restore", null);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task RestoreComment_Forbidden()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Roles.Any(r => r.Name == PolicyConstants.UserRolePolicy));
            EFCore.Models.Comment comment = Helper.GetFirstEntity<EFCore.Models.Comment>(WebApplicationFactory);

            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.PostAsync($"/api/admin/comment/{comment.Id}/restore", null);

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task RestoreComment_NotFound()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);
            User admin = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Roles.Any(r => r.Name == PolicyConstants.AdminRolePolicy));
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, admin);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.PostAsync("/api/admin/comment/111/restore", null);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task RestoreComment_NotDeleted_BadRequest()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);
            User admin = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Roles.Any(r => r.Name == PolicyConstants.AdminRolePolicy));
            EFCore.Models.Comment comment = Helper.GetFirstEntity<EFCore.Models.Comment>(WebApplicationFactory);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, admin);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.PostAsync($"/api/admin/comment/{comment.Id}/restore", null);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact, ResetDataAfterTest]
        public async Task RestoreComment_NoContent()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);
            User admin = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Roles.Any(r => r.Name == PolicyConstants.AdminRolePolicy));
            EFCore.Models.Comment comment = Helper.GetFirstEntity<EFCore.Models.Comment>(WebApplicationFactory);
            Helper.SoftDelete(WebApplicationFactory, comment);

            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, admin);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.PostAsync($"/api/admin/comment/{comment.Id}/restore", null);

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            EFCore.Models.Comment restoredComment = Helper.GetFirstEntity<EFCore.Models.Comment>(WebApplicationFactory, c => c.Id == comment.Id, true);
            Assert.Equal(0, restoredComment.SoftDeleteLevel);
            Assert.True(restoredComment.IsSoftDeletedAtDefault());
        }

        protected override void SeedData()
        {
            ResetData();
        }

        public static void ResetData()
        {
            using var scope = FixtureByTestClassName[typeof(AdminTests).Name].ApplicationFactory.Services.CreateScope();
            using BlogPlatformDbContext dbContext = Helper.GetNotLoggingDbContext<BlogPlatformDbContext>(scope.ServiceProvider);
            dbContext.Database.EnsureDeleted();
            dbContext.Database.Migrate();

            User admin = new("admin1", "admin@user.com");
            User user1 = new("user1", "user1@user.com");
            User user2 = new("user2", "user2@user.com");
            dbContext.Users.AddRange(admin, user1, user2);
            dbContext.SaveChanges();

            Role adminRole = new(PolicyConstants.AdminRolePolicy, 0);
            Role userRole = new(PolicyConstants.UserRolePolicy, 1);
            dbContext.Roles.AddRange(adminRole, userRole);
            admin.Roles = [adminRole];
            user1.Roles = [userRole];
            user2.Roles = [userRole];
            dbContext.SaveChanges();

            EFCore.Models.Blog blog = new("Blog1", "Blog1", user1.Id);
            dbContext.Blogs.Add(blog);
            dbContext.SaveChanges();

            EFCore.Models.Category category = new("category1", blog.Id);
            dbContext.Categories.Add(category);
            dbContext.SaveChanges();

            EFCore.Models.Post post = new("Post1", "Post1", category.Id);
            dbContext.Posts.Add(post);
            dbContext.SaveChanges();

            EFCore.Models.Comment parentComment = new("Comment1", post.Id, user1.Id, null);
            dbContext.Comments.Add(parentComment);
            dbContext.SaveChanges();

            EFCore.Models.Comment childComment = new("Child1", post.Id, user1.Id, parentComment.Id);
            dbContext.Comments.Add(childComment);
            dbContext.SaveChanges();
        }
    }
}
