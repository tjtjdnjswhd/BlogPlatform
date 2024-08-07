﻿using BlogPlatform.Api.Identity.Constants;
using BlogPlatform.EFCore;
using BlogPlatform.EFCore.Extensions;
using BlogPlatform.EFCore.Models;
using BlogPlatform.Shared.Identity.Models;
using BlogPlatform.Shared.Models.Comment;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using System.Net;
using System.Net.Http.Json;

using Xunit.Abstractions;

namespace BlogPlatform.Api.IntegrationTest.Comment
{
    public class CommentTests : TestBase, ITestDataReset
    {
        public CommentTests(WebApplicationFactoryFixture applicationFactoryFixture, ITestOutputHelper testOutputHelper) : base(applicationFactoryFixture, testOutputHelper, "integration_comment_test")
        {
        }

        [Fact]
        public async Task Get_NotFound()
        {
            // Arrange
            HttpClient httpClient = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);

            // Act
            HttpResponseMessage response = await httpClient.GetAsync("/api/comment/111");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Get_Ok()
        {
            // Arrange
            HttpClient httpClient = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);

            // Act
            HttpResponseMessage response = await httpClient.GetAsync("/api/comment/1");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            CommentRead? commentRead = await response.Content.ReadFromJsonAsync<CommentRead>();
            EFCore.Models.Comment comment = Helper.GetFirstEntity<EFCore.Models.Comment>(WebApplicationFactory, c => c.Id == 1);
            Assert.NotNull(commentRead);
            Assert.Equal(comment.Id, commentRead.Id);
            Assert.Equal(comment.Content, commentRead.Content);
        }

        [Fact]
        public async Task GetByPost_Ok()
        {
            // Arrange
            HttpClient httpClient = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);

            // Act
            HttpResponseMessage response = await httpClient.GetAsync("/api/comment/post/1");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            CommentRead[]? commentReads = await response.Content.ReadFromJsonAsync<CommentRead[]>();
            Assert.NotNull(commentReads);
            Assert.NotEmpty(commentReads);
            EFCore.Models.Comment comment = Helper.GetFirstEntity<EFCore.Models.Comment>(WebApplicationFactory, c => c.PostId == 1);
            Assert.Equal(comment.Id, commentReads[0].Id);
            Assert.Equal(comment.Content, commentReads[0].Content);
        }

        [Fact]
        public async Task Create_Unauthorized()
        {
            // Arrange
            HttpClient httpClient = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);

            // Act
            HttpResponseMessage response = await httpClient.PostAsJsonAsync("/api/comment", new CommentCreate("newContent", 1, null));

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Create_PostNotExist_NotFound()
        {
            // Arrange
            HttpClient httpClient = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(httpClient, authorizeToken);

            // Act
            HttpResponseMessage response = await httpClient.PostAsJsonAsync("/api/comment", new CommentCreate("newContent", 111, null));

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Create_ParentCommentNotExist_NotFound()
        {
            // Arrange
            HttpClient httpClient = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(httpClient, authorizeToken);

            // Act
            HttpResponseMessage response = await httpClient.PostAsJsonAsync("/api/comment", new CommentCreate("newContent", null, 111));

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Create_PostIdAndParentCommentIdSet_BadRequest()
        {
            // Arrange
            HttpClient httpClient = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(httpClient, authorizeToken);

            // Act
            HttpResponseMessage response = await httpClient.PostAsJsonAsync("/api/comment", new CommentCreate("newContent", 1, 1));

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact, ResetDataAfterTest]
        public async Task Create_Root_CreatedAt()
        {
            // Arrange
            HttpClient httpClient = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(httpClient, authorizeToken);

            // Act
            HttpResponseMessage response = await httpClient.PostAsJsonAsync("/api/comment", new CommentCreate("newContent", 1, null));

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            EFCore.Models.Comment comment = Helper.GetFirstEntity<EFCore.Models.Comment>(WebApplicationFactory, c => c.Content == "newContent");
            Assert.NotNull(comment);
            Assert.Equal(user.Id, comment.UserId);
            Assert.Equal(1, comment.PostId);
            Assert.Null(comment.ParentCommentId);
        }

        [Fact, ResetDataAfterTest]
        public async Task Create_Child_CreatedAt()
        {
            // Arrange
            HttpClient httpClient = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory);
            EFCore.Models.Comment parentComment = Helper.GetFirstEntity<EFCore.Models.Comment>(WebApplicationFactory);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(httpClient, authorizeToken);

            // Act
            HttpResponseMessage response = await httpClient.PostAsJsonAsync("/api/comment", new CommentCreate("newContent", null, parentComment.Id));

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            EFCore.Models.Comment comment = Helper.GetFirstEntity<EFCore.Models.Comment>(WebApplicationFactory, c => c.Content == "newContent");
            Assert.NotNull(comment);
            Assert.Equal(user.Id, comment.UserId);
            Assert.Equal(1, comment.PostId);
            Assert.Equal(parentComment.Id, comment.ParentCommentId);
        }

        [Fact]
        public async Task Update_Unauthorized()
        {
            // Arrange
            HttpClient httpClient = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);

            // Act
            HttpResponseMessage response = await httpClient.PutAsJsonAsync("/api/comment/1", new CommentUpdate("newContent"));

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Update_NotFound()
        {
            // Arrange
            HttpClient httpClient = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(httpClient, authorizeToken);

            // Act
            HttpResponseMessage response = await httpClient.PutAsJsonAsync("/api/comment/111", new CommentUpdate("newContent"));

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Update_Forbidden()
        {
            // Arrange
            HttpClient httpClient = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Comments.Count == 0);
            EFCore.Models.Comment comment = Helper.GetFirstEntity<EFCore.Models.Comment>(WebApplicationFactory);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(httpClient, authorizeToken);

            // Act
            HttpResponseMessage response = await httpClient.PutAsJsonAsync($"/api/comment/{comment.Id}", new CommentUpdate("newContent"));

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact, ResetDataAfterTest]
        public async Task Update_NoContent()
        {
            // Arrange
            HttpClient httpClient = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory);
            EFCore.Models.Comment comment = Helper.GetFirstEntity<EFCore.Models.Comment>(WebApplicationFactory);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(httpClient, authorizeToken);

            // Act
            HttpResponseMessage response = await httpClient.PutAsJsonAsync($"/api/comment/{comment.Id}", new CommentUpdate("newContent"));

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            EFCore.Models.Comment updatedComment = Helper.GetFirstEntity<EFCore.Models.Comment>(WebApplicationFactory, c => c.Id == comment.Id);
            Assert.Equal("newContent", updatedComment.Content);
        }

        [Fact]
        public async Task Delete_Unauthorized()
        {
            // Arrange
            HttpClient httpClient = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);

            // Act
            HttpResponseMessage response = await httpClient.DeleteAsync("/api/comment/1");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Delete_NotFound()
        {
            // Arrange
            HttpClient httpClient = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(httpClient, authorizeToken);

            // Act
            HttpResponseMessage response = await httpClient.DeleteAsync("/api/comment/111");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Delete_Forbidden()
        {
            // Arrange
            HttpClient httpClient = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Comments.Count == 0);
            EFCore.Models.Comment comment = Helper.GetFirstEntity<EFCore.Models.Comment>(WebApplicationFactory);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(httpClient, authorizeToken);

            // Act
            HttpResponseMessage response = await httpClient.DeleteAsync($"/api/comment/{comment.Id}");

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact, ResetDataAfterTest]
        public async Task Delete_NoContent()
        {
            // Arrange
            HttpClient httpClient = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory);
            EFCore.Models.Comment comment = Helper.GetFirstEntity<EFCore.Models.Comment>(WebApplicationFactory);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(httpClient, authorizeToken);

            // Act
            HttpResponseMessage response = await httpClient.DeleteAsync($"/api/comment/{comment.Id}");

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            EFCore.Models.Comment? deletedComment = Helper.GetFirstEntity<EFCore.Models.Comment>(WebApplicationFactory, c => c.Id == comment.Id, true);
            Assert.Equal(1, deletedComment.SoftDeleteLevel);
            Assert.False(deletedComment.IsSoftDeletedAtDefault());

            EFCore.Models.Comment? childComment = Helper.GetFirstEntity<EFCore.Models.Comment>(WebApplicationFactory, c => c.ParentCommentId == comment.Id, true);
            Assert.Equal(0, childComment.SoftDeleteLevel);
            Assert.True(childComment.IsSoftDeletedAtDefault());
        }

        protected override void SeedData()
        {
            ResetData();
        }

        public static void ResetData()
        {
            using var scope = FixtureByTestClassName[typeof(CommentTests).Name].ApplicationFactory.Services.CreateScope();
            using BlogPlatformDbContext dbContext = Helper.GetNotLoggingDbContext<BlogPlatformDbContext>(scope.ServiceProvider);

            dbContext.Database.EnsureDeleted();
            dbContext.Database.Migrate();

            User commentOwner = new("user1", "user1@user.com");
            User notHasCommentUser = new("user2", "user2@user.com");
            dbContext.Users.Add(commentOwner);
            dbContext.Users.Add(notHasCommentUser);
            dbContext.SaveChanges();

            Role role = new(PolicyConstants.UserRolePolicy, 1);
            dbContext.Roles.Add(role);
            commentOwner.Roles = [role];
            dbContext.SaveChanges();

            EFCore.Models.Blog blog = new("blogName", "blogdesc", commentOwner.Id);
            dbContext.Blogs.Add(blog);
            dbContext.SaveChanges();

            EFCore.Models.Category category = new("categoryName", blog.Id);
            dbContext.Categories.Add(category);
            dbContext.SaveChanges();

            EFCore.Models.Post post = new("title", "content", category.Id);
            dbContext.Posts.Add(post);
            dbContext.SaveChanges();

            EFCore.Models.Comment comment = new("content", post.Id, commentOwner.Id, null);
            dbContext.Comments.Add(comment);
            dbContext.SaveChanges();

            EFCore.Models.Comment childComment = new("child content", post.Id, commentOwner.Id, comment.Id);
            dbContext.Comments.Add(childComment);
            dbContext.SaveChanges();
        }
    }
}
