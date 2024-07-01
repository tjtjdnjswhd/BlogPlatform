using BlogPlatform.EFCore;
using BlogPlatform.EFCore.Extensions;
using BlogPlatform.EFCore.Models;
using BlogPlatform.Shared.Identity.Models;
using BlogPlatform.Shared.Models.Category;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using System.Net;
using System.Net.Http.Json;

using Xunit.Abstractions;

namespace BlogPlatform.Api.IntegrationTest.Category
{
    public class CategoryTests : TestBase
    {
        public CategoryTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper, "integration_category_test")
        {
        }

        [Fact]
        public async Task Get_NotFound()
        {
            // Arrange
            HttpClient client = CreateClient();

            // Act
            HttpResponseMessage response = await client.GetAsync("/api/category/222");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Get_Ok()
        {
            // Arrange
            HttpClient client = CreateClient();

            // Act
            HttpResponseMessage response = await client.GetAsync("/api/category/1");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            CategoryRead? categoryRead = await response.Content.ReadFromJsonAsync<CategoryRead>();
            Assert.NotNull(categoryRead);
        }

        [Fact]
        public async Task Create_Unauthorized()
        {
            // Arrange
            HttpClient client = CreateClient();

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/category", new CategoryNameModel("CategoryName"));

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Create_BlogNotExist_BadRequest()
        {
            // Arrange
            HttpClient client = CreateClient();
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Blog.Count == 0);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/category", new CategoryNameModel("CategoryName"));

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Create_Ok()
        {
            // Arrange
            HttpClient client = CreateClient();
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Blog.Any(b => b.Categories.Count > 0));
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/category", new CategoryNameModel("CategoryName"));

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task Update_Unauthorized()
        {
            // Arrange
            HttpClient client = CreateClient();

            // Act
            HttpResponseMessage response = await client.PutAsJsonAsync("/api/category/1", new CategoryNameModel("CategoryName"));

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Update_CategoryNotExist_NotFound()
        {
            // Arrange
            HttpClient client = CreateClient();
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Blog.Any(b => b.Categories.Count > 0));
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.PutAsJsonAsync("/api/category/222", new CategoryNameModel("CategoryName"));

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Update_Forbid()
        {
            // Arrange
            HttpClient client = CreateClient();
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Blog.Count == 0);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.PutAsJsonAsync("/api/category/1", new CategoryNameModel("CategoryName"));

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Update_NoContent()
        {
            // Arrange
            HttpClient client = CreateClient();
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Blog.Any(b => b.Categories.Count > 0));
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.PutAsJsonAsync("/api/category/1", new CategoryNameModel("newCategoryName"));

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            EFCore.Models.Category category = Helper.GetFirstEntity<EFCore.Models.Category>(WebApplicationFactory, c => c.Blog.UserId == user.Id);
            Assert.Equal("newCategoryName", category.Name);
        }

        [Fact]
        public async Task Delete_Unauthorized()
        {
            // Arrange
            HttpClient client = CreateClient();

            // Act
            HttpResponseMessage response = await client.DeleteAsync("/api/category/1");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Delete_NotFound()
        {
            // Arrange
            HttpClient client = CreateClient();
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Blog.Any(b => b.Categories.Count > 0));
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.DeleteAsync("/api/category/222");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Delete_Forbid()
        {
            // Arrange
            HttpClient client = CreateClient();
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Blog.Count == 0);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.DeleteAsync("/api/category/1");

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Delete_NoContent()
        {
            // Arrange
            HttpClient client = CreateClient();
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Blog.Any(b => b.Categories.Count > 0));
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.DeleteAsync("/api/category/1");

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            EFCore.Models.Category? category = Helper.GetFirstEntity<EFCore.Models.Category>(WebApplicationFactory, c => c.Blog.UserId == user.Id, true);
            Assert.NotNull(category);
            Assert.False(category.IsSoftDeletedAtDefault());
            Assert.NotEqual(0, category.SoftDeleteLevel);
        }

        [Fact]
        public async Task Restore_Unauthorized()
        {
            // Arrange
            HttpClient client = CreateClient();

            // Act
            HttpResponseMessage response = await client.PostAsync("/api/category/restore/1", null);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Restore_NotFound()
        {
            // Arrange
            HttpClient client = CreateClient();
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Blog.Any(b => b.Categories.Count > 0));
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.PostAsync("/api/category/restore/222", null);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Restore_Forbid()
        {
            // Arrange
            HttpClient client = CreateClient();
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Blog.Count == 0);
            EFCore.Models.Category category = Helper.GetFirstEntity<EFCore.Models.Category>(WebApplicationFactory);
            Helper.SoftDelete(WebApplicationFactory, category);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.PostAsync("/api/category/restore/1", null);

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Restore_NotDeleted_BadRequest()
        {
            // Arrange
            HttpClient client = CreateClient();
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Blog.Any(b => b.Categories.Count > 0));
            EFCore.Models.Category category = Helper.GetFirstEntity<EFCore.Models.Category>(WebApplicationFactory);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.PostAsync($"/api/category/restore/{category.Id}", null);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Restore_Expired_BadRequest()
        {
            // Arrange
            HttpClient client = CreateClient();
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Blog.Any(b => b.Categories.Count > 0));
            EFCore.Models.Category category = Helper.GetFirstEntity<EFCore.Models.Category>(WebApplicationFactory);
            Helper.SoftDelete(WebApplicationFactory, category);
            category.SoftDeletedAt = DateTime.UtcNow.AddDays(-2);
            Helper.UpdateEntity(WebApplicationFactory, category);

            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.PostAsync($"/api/category/restore/1", null);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Restore_NoContent()
        {
            // Arrange
            HttpClient client = CreateClient();
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Blog.Any(b => b.Categories.Count > 0));
            EFCore.Models.Category category = Helper.GetFirstEntity<EFCore.Models.Category>(WebApplicationFactory);
            Helper.SoftDelete(WebApplicationFactory, category);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.PostAsync($"/api/category/restore/1", null);

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            EFCore.Models.Category? restoredCategory = Helper.GetFirstEntity<EFCore.Models.Category>(WebApplicationFactory, c => c.Blog.UserId == user.Id);
            Assert.NotNull(restoredCategory);
            Assert.True(restoredCategory.IsSoftDeletedAtDefault());
            Assert.Equal(0, restoredCategory.SoftDeleteLevel);

            EFCore.Models.Post? post = Helper.GetFirstEntity<EFCore.Models.Post>(WebApplicationFactory, p => p.Category.Id == restoredCategory.Id);
            Assert.NotNull(post);
            Assert.True(post.IsSoftDeletedAtDefault());
            Assert.Equal(0, post.SoftDeleteLevel);

            EFCore.Models.Comment? comment = Helper.GetFirstEntity<EFCore.Models.Comment>(WebApplicationFactory, c => c.Post.Id == post.Id);
            Assert.NotNull(comment);
            Assert.True(comment.IsSoftDeletedAtDefault());
            Assert.Equal(0, comment.SoftDeleteLevel);
        }

        protected override void SeedData()
        {
            using var scope = WebApplicationFactory.Services.CreateScope();
            using BlogPlatformDbContext dbContext = Helper.GetNotLoggingDbContext<BlogPlatformDbContext>(scope.ServiceProvider);

            dbContext.Database.EnsureDeleted();
            dbContext.Database.Migrate();

            User blogOwner = new("blogOwner", "user1@user.com");
            User withoutBlog = new("withoutBlog", "user2@user.com");
            dbContext.Users.AddRange(blogOwner, withoutBlog);
            dbContext.SaveChanges();

            Role userRole = new("User", 1);
            blogOwner.Roles = [userRole];
            withoutBlog.Roles = [userRole];
            dbContext.SaveChanges();

            EFCore.Models.Blog blog = new("BlogName", "BlogDescription", blogOwner.Id);
            dbContext.Blogs.Add(blog);
            dbContext.SaveChanges();

            EFCore.Models.Category category1 = new("Category1", blog.Id);
            EFCore.Models.Category category2 = new("Category2", blog.Id);
            dbContext.Categories.AddRange(category1, category2);
            dbContext.SaveChanges();

            EFCore.Models.Post post = new("PostTitle", "PostContent", category1.Id);
            dbContext.Posts.Add(post);
            dbContext.SaveChanges();

            EFCore.Models.Comment comment = new("CommentContent", post.Id, withoutBlog.Id, null);
            dbContext.Comments.Add(comment);
            dbContext.SaveChanges();
        }
    }
}
