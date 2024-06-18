using BlogPlatform.Api.Models;
using BlogPlatform.EFCore;
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
            HttpClient client = WebApplicationFactory.CreateClient();

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
            HttpClient client = WebApplicationFactory.CreateClient();

            // Act
            HttpResponseMessage response = await client.GetAsync("/api/blog/1");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            BlogRead? blogRead = await response.Content.ReadFromJsonAsync<BlogRead>();
            Assert.NotNull(blogRead);
        }

        protected override void SeedData()
        {
            using var scope = WebApplicationFactory.Services.CreateScope();
            BlogPlatformDbContext dbContext = scope.ServiceProvider.GetRequiredService<BlogPlatformDbContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.Migrate();

            User user = new("blogOwner", "user1@user.com");
            dbContext.Users.Add(user);
            dbContext.SaveChanges();

            BasicAccount basicAccount = new("basicAccountId", "password", user.Id);
            dbContext.BasicAccounts.Add(basicAccount);
            dbContext.SaveChanges();

            EFCore.Models.Blog blog = new("blogName", "blogDescription", user.Id);
            dbContext.Blogs.Add(blog);
            dbContext.SaveChanges();
        }
    }
}
