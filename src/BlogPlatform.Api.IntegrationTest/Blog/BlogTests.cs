using BlogPlatform.Api.Identity.Models;
using BlogPlatform.Api.Models;
using BlogPlatform.EFCore;
using BlogPlatform.EFCore.Extensions;
using BlogPlatform.EFCore.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using System.Net;
using System.Net.Http.Json;

using Xunit.Abstractions;

namespace BlogPlatform.Api.IntegrationTest.Blog
{
    public class BlogTests : TestBase
    {
        public BlogTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper, "integration_blog_test")
        {
        }

        [Fact]
        public async Task GetBlog_NotFound()
        {
            // Arrange
            HttpClient client = CreateClient();

            // Act
            HttpResponseMessage response = await client.GetAsync("/api/blog/222");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetBlog_Ok()
        {
            // Arrange
            SeedData();
            HttpClient client = CreateClient();

            // Act
            HttpResponseMessage response = await client.GetAsync("/api/blog/1");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            BlogRead? blogRead = await response.Content.ReadFromJsonAsync<BlogRead>();
            Assert.NotNull(blogRead);
        }

        [Fact]
        public async Task CreateBlog_Unauthorized()
        {
            // Arrange
            HttpClient client = CreateClient();

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/blog", new BlogCreate("blogName", "description"));

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task CreateBlog_BlogExist_Conflict()
        {
            // Arrange
            HttpClient client = CreateClient();
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Blog.Count != 0);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/blog", new BlogCreate("blogName", "description"));

            // Assert
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Helper.LoadCollection(WebApplicationFactory, user, u => u.Blog);
            Assert.Single(user.Blog);
        }

        [Fact]
        public async Task CreateBlog_BlogNameDuplicate_Conflict()
        {
            // Arrange
            HttpClient client = CreateClient();
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Blog.Count == 0);
            EFCore.Models.Blog blog = Helper.GetFirstEntity<EFCore.Models.Blog>(WebApplicationFactory);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/blog", new BlogCreate(blog.Name, "description"));

            // Assert
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Helper.LoadCollection(WebApplicationFactory, user, u => u.Blog);
            Assert.Empty(user.Blog);
        }

        [Fact]
        public async Task CreateBlog_Ok()
        {
            // Arrange
            HttpClient client = CreateClient();
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Blog.Count == 0);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/blog", new BlogCreate("newblogName", "newdescription"));

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            EFCore.Models.Blog? userBlog = Helper.GetFirstEntityOrDefault<EFCore.Models.Blog>(WebApplicationFactory, b => b.UserId == user.Id, true);
            Assert.NotNull(userBlog);
        }

        [Fact]
        public async Task UpdateBlog_Unauthorized()
        {
            // Arrange
            HttpClient client = CreateClient();

            // Act
            HttpResponseMessage response = await client.PutAsJsonAsync("/api/blog/1", new BlogCreate("newblogName", "newdescription"));

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task UpdateBlog_NotFound()
        {
            // Arrange
            HttpClient client = CreateClient();
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Blog.Count != 0);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.PutAsJsonAsync("/api/blog/222", new BlogCreate("newblogName", "newdescription"));

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task UpdateBlog_Forbid()
        {
            // Arrange
            HttpClient client = CreateClient();
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Blog.Count == 0);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.PutAsJsonAsync("/api/blog/1", new BlogCreate("newblogName", "newdescription"));

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task UpdateBlog_BlogNameDuplicate_Conflict()
        {
            // Arrange
            HttpClient client = CreateClient();
            User blogOwner = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Blog.Count != 0);
            EFCore.Models.Blog blog = Helper.GetFirstEntity<EFCore.Models.Blog>(WebApplicationFactory, b => b.UserId == blogOwner.Id);

            User withoutBlog = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Blog.Count == 0);
            Assert.NotEqual(blogOwner.Id, withoutBlog.Id);
            EFCore.Models.Blog anotherBlog = new("anotherBlog", "anotherBlogDescription", withoutBlog.Id);
            Helper.AddEntity(WebApplicationFactory, anotherBlog);

            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, blogOwner);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.PutAsJsonAsync($"/api/blog/{blog.Id}", new BlogCreate(anotherBlog.Name, "newdescription"));

            // Assert
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Helper.ReloadEntity(WebApplicationFactory, blog);
            Assert.NotEqual(anotherBlog.Name, blog.Name);
        }

        [Fact]
        public async Task UpdateBlog_Ok()
        {
            // Arrange
            HttpClient client = CreateClient();
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Blog.Count != 0);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.PutAsJsonAsync("/api/blog/1", new BlogCreate("newblogName", "newdescription"));

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            EFCore.Models.Blog userBlog = Helper.GetFirstEntity<EFCore.Models.Blog>(WebApplicationFactory, b => b.UserId == user.Id, true);
            Assert.Equal("newblogName", userBlog.Name);
            Assert.Equal("newdescription", userBlog.Description);
        }

        [Fact]
        public async Task DeleteBlog_NotFound()
        {
            // Arrange
            HttpClient client = CreateClient();
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Blog.Count != 0);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.DeleteAsync("/api/blog/222");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DeleteBlog_AlreadyDeleted_NotFound()
        {
            // Arrange
            HttpClient client = CreateClient();
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Blog.Count != 0);
            EFCore.Models.Blog blog = Helper.GetFirstEntity<EFCore.Models.Blog>(WebApplicationFactory, b => b.UserId == user.Id);
            Helper.SoftDelete(WebApplicationFactory, blog);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.DeleteAsync($"/api/blog/{blog.Id}");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DeleteBlog_Forbid()
        {
            // Arrange
            HttpClient client = CreateClient();
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Blog.Count == 0);
            EFCore.Models.Blog blog = Helper.GetFirstEntity<EFCore.Models.Blog>(WebApplicationFactory);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.DeleteAsync($"/api/blog/{blog.Id}");

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task DeleteBlog_NoContent()
        {
            // Arrange
            HttpClient client = CreateClient();
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Blog.Count != 0);
            EFCore.Models.Blog blog = Helper.GetFirstEntity<EFCore.Models.Blog>(WebApplicationFactory, b => b.UserId == user.Id);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.DeleteAsync($"/api/blog/{blog.Id}");

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            EFCore.Models.Blog? userBlog = Helper.GetFirstEntityOrDefault<EFCore.Models.Blog>(WebApplicationFactory, b => b.UserId == user.Id);
            Assert.Null(userBlog);
        }

        [Fact]
        public async Task RestoreBlog_Unauthorized()
        {
            // Arrange
            HttpClient client = CreateClient();

            // Act
            HttpResponseMessage response = await client.PostAsync("/api/blog/restore/1", null);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task RestoreBlog_NotFound()
        {
            // Arrange
            HttpClient client = CreateClient();
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Blog.Count != 0);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.PostAsync("/api/blog/restore/222", null);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task RestoreBlog_NotDeleted()
        {
            // Arrange
            HttpClient client = CreateClient();
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Blog.Count != 0);
            EFCore.Models.Blog blog = Helper.GetFirstEntity<EFCore.Models.Blog>(WebApplicationFactory, b => b.UserId == user.Id);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.PostAsync($"/api/blog/restore/{blog.Id}", null);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task RestoreBlog_Forbid()
        {
            // Arrange
            HttpClient client = CreateClient();
            User blogOwner = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Blog.Count != 0);
            User withoutBlog = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Blog.Count == 0);
            Assert.NotEqual(blogOwner.Id, withoutBlog.Id);
            EFCore.Models.Blog blog = Helper.GetFirstEntity<EFCore.Models.Blog>(WebApplicationFactory, b => b.UserId == blogOwner.Id);
            Helper.SoftDelete(WebApplicationFactory, blog);

            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, withoutBlog);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.PostAsync($"/api/blog/restore/{blog.Id}", null);

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task RestoreBlog_BlogAlreadyExist_Conflict()
        {
            // Arrange
            HttpClient client = CreateClient();
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Blog.Count != 0);
            EFCore.Models.Blog blog = Helper.GetFirstEntity<EFCore.Models.Blog>(WebApplicationFactory, b => b.UserId == user.Id);
            Helper.SoftDelete(WebApplicationFactory, blog);
            EFCore.Models.Blog anotherBlog = new("anotherBlog", "anotherBlogDescription", user.Id);
            Helper.AddEntity(WebApplicationFactory, anotherBlog);

            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.PostAsync($"/api/blog/restore/{blog.Id}", null);

            // Assert
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Helper.ReloadEntity(WebApplicationFactory, blog);
            Assert.False(blog.IsSoftDeletedAtDefault());
            Assert.NotEqual(0, blog.SoftDeleteLevel);

            Helper.ReloadEntity(WebApplicationFactory, anotherBlog);
            Assert.True(anotherBlog.IsSoftDeletedAtDefault());
            Assert.Equal(0, anotherBlog.SoftDeleteLevel);
        }

        [Fact]
        public async Task RestoreBlog_Expired_BadRequest()
        {
            // Arrange
            HttpClient client = CreateClient();
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Blog.Count != 0);
            EFCore.Models.Blog blog = Helper.GetFirstEntity<EFCore.Models.Blog>(WebApplicationFactory, b => b.UserId == user.Id);
            Helper.SoftDelete(WebApplicationFactory, blog);
            blog.SoftDeletedAt = DateTime.UtcNow.AddDays(-1);
            Helper.UpdateEntity(WebApplicationFactory, blog);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.PostAsync($"/api/blog/restore/{blog.Id}", null);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            EFCore.Models.Blog? restoredBlog = Helper.GetFirstEntityOrDefault<EFCore.Models.Blog>(WebApplicationFactory, b => b.UserId == user.Id);
            Assert.Null(restoredBlog);
        }

        [Fact]
        public async Task RestoreBlog_NoContent()
        {
            // Arrange
            HttpClient client = CreateClient();
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Blog.Count != 0);
            EFCore.Models.Blog blog = Helper.GetFirstEntity<EFCore.Models.Blog>(WebApplicationFactory, b => b.UserId == user.Id);
            Helper.SoftDelete(WebApplicationFactory, blog);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.PostAsync($"/api/blog/restore/{blog.Id}", null);

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            EFCore.Models.Blog? restoredBlog = Helper.GetFirstEntityOrDefault<EFCore.Models.Blog>(WebApplicationFactory, b => b.UserId == user.Id);
            Assert.NotNull(restoredBlog);
            Assert.True(restoredBlog.IsSoftDeletedAtDefault());
            Assert.Equal(0, restoredBlog.SoftDeleteLevel);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task RestoreBlog_BlogNameDuplicate_Conflict(bool addBody)
        {
            // Arrange
            HttpClient client = CreateClient();
            User blogOwner = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Blog.Count != 0);
            User withoutBlog = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Blog.Count == 0);
            Assert.NotEqual(blogOwner.Id, withoutBlog.Id);
            EFCore.Models.Blog blog = Helper.GetFirstEntity<EFCore.Models.Blog>(WebApplicationFactory, b => b.UserId == blogOwner.Id);
            Helper.SoftDelete(WebApplicationFactory, blog);

            EFCore.Models.Blog anotherBlog = new(blog.Name, "anotherBlogDescription", withoutBlog.Id);
            Helper.AddEntity(WebApplicationFactory, anotherBlog);

            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, blogOwner);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            // Act
            HttpResponseMessage response;
            if (addBody)
            {
                response = await client.PostAsJsonAsync($"/api/blog/restore/{blog.Id}", new BlogCreate(anotherBlog.Name, "newdescription"));
            }
            else
            {
                response = await client.PostAsync($"/api/blog/restore/{blog.Id}", null);
            }

            // Assert
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Helper.ReloadEntity(WebApplicationFactory, blog);
            Assert.False(blog.IsSoftDeletedAtDefault());
            Assert.NotEqual(0, blog.SoftDeleteLevel);

            Helper.ReloadEntity(WebApplicationFactory, anotherBlog);
            Assert.True(anotherBlog.IsSoftDeletedAtDefault());
            Assert.Equal(0, anotherBlog.SoftDeleteLevel);
        }

        protected override void SeedData()
        {
            using var scope = WebApplicationFactory.Services.CreateScope();
            BlogPlatformDbContext dbContext = scope.ServiceProvider.GetRequiredService<BlogPlatformDbContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.Migrate();

            User blogOwner = new("blogOwner", "user1@user.com");
            User withoutBlog = new("notHaveBlogUser", "user2@user.com");
            dbContext.Users.Add(blogOwner);
            dbContext.Users.Add(withoutBlog);
            dbContext.SaveChanges();

            Role userRole = new("User", 1);
            dbContext.Roles.Add(userRole);
            blogOwner.Roles.Add(userRole);
            withoutBlog.Roles.Add(userRole);
            dbContext.SaveChanges();

            BasicAccount blogOwnerAccount = new("blogOwnerId", "password", blogOwner.Id);
            BasicAccount withoutBlogAccount = new("withoutBlogId", "password", withoutBlog.Id);
            dbContext.BasicAccounts.Add(blogOwnerAccount);
            dbContext.BasicAccounts.Add(withoutBlogAccount);
            dbContext.SaveChanges();

            EFCore.Models.Blog blog = new("blogName", "blogDescription", blogOwner.Id);
            dbContext.Blogs.Add(blog);
            dbContext.SaveChanges();
        }
    }
}
