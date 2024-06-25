using BlogPlatform.Api.Identity.Models;
using BlogPlatform.Api.Models;
using BlogPlatform.EFCore;
using BlogPlatform.EFCore.Extensions;
using BlogPlatform.EFCore.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

using Xunit.Abstractions;

namespace BlogPlatform.Api.IntegrationTest.Post
{
    public class PostTests : TestBase
    {
        private const string SeedImgFileName = "seedImg";

        public PostTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper, "integration_post_test")
        {
        }

        [Fact]
        public async Task Get_NotFound()
        {
            // Arrange
            HttpClient httpClient = CreateClient();

            // Act
            HttpResponseMessage response = await httpClient.GetAsync("/api/post/222");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Get_Ok()
        {
            // Arrange
            HttpClient httpClient = CreateClient();

            // Act
            HttpResponseMessage response = await httpClient.GetAsync("/api/post/1");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }


        [Theory]
        [InlineData("<img onmouseover=\"alert(2)\">", "<img>")]
        [InlineData("<img src=\"x\" onerror=\"alert(2)\">", "<img src=\"x\">")]
        [InlineData("<script>alert('xss')</script>", "")]
        [InlineData("<svg/onload=alert(1)>", "")]
        [InlineData("><b onclick=alert(1)>>XSSTEST</b>", "&gt;<b>&gt;XSSTEST</b>")]
        public async Task Get_PreventXSS(string code, string expected)
        {
            // Arrange
            EFCore.Models.Post post = new("title", code, 1);
            Helper.AddEntity(WebApplicationFactory, post);

            HttpClient httpClient = CreateClient();

            // Act
            HttpResponseMessage response = await httpClient.GetAsync($"/api/post/{post.Id}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            PostRead? postRead = await response.Content.ReadFromJsonAsync<PostRead>();
            Assert.NotNull(postRead);
            Assert.Equal(expected, postRead.Content);
        }

        public static readonly TheoryData<string> SearchQueries = new(
            "blogid=1&title=title",
            "blogid=1&title=title&content=content&createdatstart=2024-01-01&createdatend=2025-12-31",
            "blogid=1&title=title&content=content&createdatstart=2024-01-01&createdatend=2025-12-31&tags=tag1&tags=tag2&tagFilter=All",
            "blogid=1&title=title&content=content&createdatstart=2024-01-01&createdatend=2025-12-31&tags=tag1&tags=tag2&tagFilter=Any",
            "blogid=1&title=title&content=content&createdatstart=2024-01-01&createdatend=2025-12-31&orderby=CreatedAt&orderdirection=Descending",
            "categoryid=1&title=title&content=content&createdatestart=2024-01-01&createdatend=2025-12-31&orderby=CreatedAt&orderdirection=Ascending",
            "categoryid=1&title=title&content=content&createdatstart=2024-01-01&createdatend=2025-12-31&tags=tag1&tags=tag2&tagFilter=All&orderby=CreatedAt&orderdirection=Ascending",
            "categoryid=1&title=title&content=content&createdatstart=2024-01-01&createdatend=2025-12-31&tags=tag1&tags=tag2&tagFilter=Any&orderby=CreatedAt&orderdirection=Ascending"
        );

        [Theory]
        [MemberData(nameof(SearchQueries))]
        public async Task Get_Search(string query)
        {
            // Arrange
            HttpClient httpClient = CreateClient();

            // Act
            HttpResponseMessage response = await httpClient.GetAsync($"/api/post?{query}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            PostSearchResult[]? postSearchResults = await response.Content.ReadFromJsonAsync<PostSearchResult[]>();
            Assert.NotNull(postSearchResults);
            Assert.NotEmpty(postSearchResults);
        }

        [Fact]
        public async Task Create_Unauthorized()
        {
            // Arrange
            HttpClient httpClient = CreateClient();

            PostCreate postCreate = new("title", "content", [], 1);

            // Act
            HttpResponseMessage response = await httpClient.PostAsJsonAsync("/api/post", postCreate);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Create_CategoryNotExist_NotFound()
        {
            // Arrange
            HttpClient httpClient = CreateClient();
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(httpClient, authorizeToken);

            PostCreate postCreate = new("title", "content", [], 999);

            // Act
            HttpResponseMessage response = await httpClient.PostAsJsonAsync("/api/post", postCreate);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Create_WithoutImg_CreatedAt()
        {
            // Arrange
            HttpClient httpClient = CreateClient();
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(httpClient, authorizeToken);
            PostCreate postCreate = new("title", "content", [], 1);

            // Act
            HttpResponseMessage response = await httpClient.PostAsJsonAsync("/api/post", postCreate);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task Create_WithImg_CreatedAt()
        {
            // Arrange
            HttpClient httpClient = CreateClient();
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(httpClient, authorizeToken);

            HttpResponseMessage uploadImageResponse = await UploadImageAsync(httpClient, "newImg.jpg", "image/jpg");
            Assert.Equal(HttpStatusCode.Created, uploadImageResponse.StatusCode);
            string imgLocation = GetImageLocation(uploadImageResponse);
            PostCreate postCreate = new("title", $"content<img src=\"{imgLocation}\" />", [], 1);

            // Act
            HttpResponseMessage response = await httpClient.PostAsJsonAsync("/api/post", postCreate);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.True(Helper.IsExist<EFCore.Models.Post>(WebApplicationFactory, p => p.Content.Contains(imgLocation)));
            Assert.True(await IsImageDataExist(imgLocation[(imgLocation.LastIndexOf('/') + 1)..]));
        }

        [Fact]
        public async Task Update_Unauthorized()
        {
            // Arrange
            HttpClient httpClient = CreateClient();

            PostCreate postUpdate = new("title", "content", [], 1);

            // Act
            HttpResponseMessage response = await httpClient.PutAsJsonAsync("/api/post/1", postUpdate);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Update_CategoryNotExist_NotFound()
        {
            // Arrange
            HttpClient httpClient = CreateClient();
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(httpClient, authorizeToken);

            PostCreate postUpdate = new("title", "content", [], 999);

            // Act
            HttpResponseMessage response = await httpClient.PutAsJsonAsync("/api/post/1", postUpdate);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Update_PostNotExist_NotFound()
        {
            // Arrange
            HttpClient httpClient = CreateClient();
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(httpClient, authorizeToken);

            PostCreate postUpdate = new("title", "content", [], 1);

            // Act
            HttpResponseMessage response = await httpClient.PutAsJsonAsync("/api/post/999", postUpdate);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Update_Category_Forbid()
        {
            // Arrange
            HttpClient httpClient = CreateClient();
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Blog.Count == 0);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(httpClient, authorizeToken);

            PostCreate postUpdate = new("title", "content", [], 1);

            // Act
            HttpResponseMessage response = await httpClient.PutAsJsonAsync("/api/post/1", postUpdate);

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Update_WithoutImg_NoContent()
        {
            // Arrange
            HttpClient httpClient = CreateClient();
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(httpClient, authorizeToken);

            PostCreate postUpdate = new("title", "content", [], 1);

            // Act
            HttpResponseMessage response = await httpClient.PutAsJsonAsync("/api/post/1", postUpdate);

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task Update_WithImg_NewImgOnly_NoContent()
        {
            // Arrange
            HttpClient httpClient = CreateClient();
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(httpClient, authorizeToken);

            HttpResponseMessage uploadImageResponse = await UploadImageAsync(httpClient, "newImg.jpg", "image/jpg");
            Assert.Equal(HttpStatusCode.Created, uploadImageResponse.StatusCode);
            string imgLocation = GetImageLocation(uploadImageResponse);
            PostCreate postUpdate = new("title", $"content<img src=\"{imgLocation}\" />", [], 1);

            // Act
            HttpResponseMessage response = await httpClient.PutAsJsonAsync("/api/post/1", postUpdate);

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.True(Helper.IsExist<EFCore.Models.Post>(WebApplicationFactory, p => p.Content.Contains(imgLocation)));
            Assert.True(await IsImageDataExist(imgLocation[(imgLocation.LastIndexOf('/') + 1)..]));
        }

        [Fact]
        public async Task Update_ImgRemoved_NoContent()
        {
            // Arrange
            HttpClient httpClient = CreateClient();
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(httpClient, authorizeToken);

            EFCore.Models.Post imgPost = Helper.GetFirstEntity<EFCore.Models.Post>(WebApplicationFactory, p => p.Content.Contains(SeedImgFileName));
            PostCreate postUpdate = new("title", "content", [], imgPost.CategoryId);

            // Act
            HttpResponseMessage response = await httpClient.PutAsJsonAsync($"/api/post/{imgPost.Id}", postUpdate);

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.False(Helper.IsExist<EFCore.Models.Post>(WebApplicationFactory, p => p.Content.Contains(SeedImgFileName)));
            Assert.False(await IsImageDataExist(SeedImgFileName));
        }

        [Fact]
        public async Task Update_ImgAdded_NoContent()
        {
            // Arrange
            HttpClient httpClient = CreateClient();
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(httpClient, authorizeToken);

            EFCore.Models.Post imgPost = Helper.GetFirstEntity<EFCore.Models.Post>(WebApplicationFactory, p => !p.Content.Contains(SeedImgFileName));
            HttpResponseMessage uploadImageResponse = await UploadImageAsync(httpClient, "newImg.jpg", "image/jpg");
            Assert.Equal(HttpStatusCode.Created, uploadImageResponse.StatusCode);
            string imgLocation = GetImageLocation(uploadImageResponse);

            PostCreate postUpdate = new("title", $"content<img src=\"{imgLocation}\" />", [], imgPost.CategoryId);

            // Act
            HttpResponseMessage response = await httpClient.PutAsJsonAsync($"/api/post/{imgPost.Id}", postUpdate);

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.True(Helper.IsExist<EFCore.Models.Post>(WebApplicationFactory, p => p.Content.Contains(imgLocation)));
            Assert.True(await IsImageDataExist(imgLocation[(imgLocation.LastIndexOf('/') + 1)..]));
        }

        [Fact]
        public async Task Update_ImgReplaced_NoContent()
        {
            // Arrange
            HttpClient httpClient = CreateClient();
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(httpClient, authorizeToken);

            EFCore.Models.Post imgPost = Helper.GetFirstEntity<EFCore.Models.Post>(WebApplicationFactory, p => p.Content.Contains(SeedImgFileName));
            HttpResponseMessage uploadImageResponse = await UploadImageAsync(httpClient, "newImg.jpg", "image/jpg");
            Assert.Equal(HttpStatusCode.Created, uploadImageResponse.StatusCode);
            string imgLocation = GetImageLocation(uploadImageResponse);

            PostCreate postUpdate = new("title", $"content<img src=\"{imgLocation}\" />", [], imgPost.CategoryId);

            // Act
            HttpResponseMessage response = await httpClient.PutAsJsonAsync($"/api/post/{imgPost.Id}", postUpdate);

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.True(Helper.IsExist<EFCore.Models.Post>(WebApplicationFactory, p => p.Content.Contains(imgLocation)));
            Assert.True(await IsImageDataExist(imgLocation[(imgLocation.LastIndexOf('/') + 1)..]));
            Assert.False(Helper.IsExist<EFCore.Models.Post>(WebApplicationFactory, p => p.Content.Contains(SeedImgFileName)));
            Assert.False(await IsImageDataExist(SeedImgFileName));
        }

        [Fact]
        public async Task Delete_Unauthorized()
        {
            // Arrange
            HttpClient httpClient = CreateClient();

            // Act
            HttpResponseMessage response = await httpClient.DeleteAsync("/api/post/1");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Delete_PostNotExist_NotFound()
        {
            // Arrange
            HttpClient httpClient = CreateClient();
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(httpClient, authorizeToken);

            // Act
            HttpResponseMessage response = await httpClient.DeleteAsync("/api/post/999");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Delete_Forbid()
        {
            // Arrange
            HttpClient httpClient = CreateClient();
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Blog.Count == 0);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(httpClient, authorizeToken);

            // Act
            HttpResponseMessage response = await httpClient.DeleteAsync("/api/post/1");

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Delete_NoContent()
        {
            // Arrange
            HttpClient httpClient = CreateClient();
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(httpClient, authorizeToken);

            // Act
            HttpResponseMessage response = await httpClient.DeleteAsync("/api/post/1");

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            EFCore.Models.Post? deletedPost = Helper.GetFirstEntityOrDefault<EFCore.Models.Post>(WebApplicationFactory, p => p.Id == 1, true);
            Assert.NotNull(deletedPost);
            Assert.False(deletedPost.IsSoftDeletedAtDefault());
            Assert.Equal(1, deletedPost.SoftDeleteLevel);

            EFCore.Models.Comment? deletedComment = Helper.GetFirstEntityOrDefault<EFCore.Models.Comment>(WebApplicationFactory, c => c.PostId == deletedPost.Id, true);
            Assert.NotNull(deletedComment);
            Assert.False(deletedComment.IsSoftDeletedAtDefault());
            Assert.Equal(2, deletedComment.SoftDeleteLevel);
        }

        [Fact]
        public async Task Restore_Unauthorized()
        {
            // Arrange
            HttpClient httpClient = CreateClient();

            // Act
            HttpResponseMessage response = await httpClient.PostAsync("/api/post/restore/1", null);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Restore_NotFound()
        {
            // Arrange
            HttpClient httpClient = CreateClient();
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(httpClient, authorizeToken);

            // Act
            HttpResponseMessage response = await httpClient.PostAsync("/api/post/restore/999", null);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Restore_NotDeleted_BadRequest()
        {
            // Arrange
            HttpClient httpClient = CreateClient();
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(httpClient, authorizeToken);

            // Act
            HttpResponseMessage response = await httpClient.PostAsync("/api/post/restore/1", null);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Restore_Forbid()
        {
            // Arrange
            HttpClient httpClient = CreateClient();
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Blog.Count == 0);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(httpClient, authorizeToken);

            EFCore.Models.Post post = Helper.GetFirstEntity<EFCore.Models.Post>(WebApplicationFactory);
            Helper.SoftDelete(WebApplicationFactory, post);

            // Act
            HttpResponseMessage response = await httpClient.PostAsync("/api/post/restore/1", null);

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Restore_Expired_BadRequest()
        {
            // Arrange
            HttpClient httpClient = CreateClient();
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Blog.Count == 1);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(httpClient, authorizeToken);

            EFCore.Models.Post post = Helper.GetFirstEntity<EFCore.Models.Post>(WebApplicationFactory, p => p.Category.Blog.UserId == user.Id);
            Helper.SoftDelete(WebApplicationFactory, post);
            post.SoftDeletedAt = DateTime.UtcNow.AddDays(-2);
            Helper.UpdateEntity(WebApplicationFactory, post);

            // Act
            HttpResponseMessage response = await httpClient.PostAsync("/api/post/restore/1", null);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Restore_NoContent()
        {
            // Arrange
            HttpClient httpClient = CreateClient();
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Blog.Count == 1);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(httpClient, authorizeToken);

            EFCore.Models.Post post = Helper.GetFirstEntity<EFCore.Models.Post>(WebApplicationFactory, p => p.Category.Blog.UserId == user.Id);
            Helper.SoftDelete(WebApplicationFactory, post);

            // Act
            HttpResponseMessage response = await httpClient.PostAsync("/api/post/restore/1", null);

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            EFCore.Models.Post? restoredPost = Helper.GetFirstEntityOrDefault<EFCore.Models.Post>(WebApplicationFactory, p => p.Id == post.Id);
            Assert.NotNull(restoredPost);
            Assert.True(restoredPost.IsSoftDeletedAtDefault());
            Assert.Equal(0, restoredPost.SoftDeleteLevel);

            EFCore.Models.Comment? restoredComment = Helper.GetFirstEntityOrDefault<EFCore.Models.Comment>(WebApplicationFactory, c => c.PostId == post.Id);
            Assert.NotNull(restoredComment);
            Assert.True(restoredComment.IsSoftDeletedAtDefault());
            Assert.Equal(0, restoredComment.SoftDeleteLevel);
        }

        [Fact]
        public async Task CacheImages_Unauthorized()
        {
            // Arrange
            HttpClient httpClient = CreateClient();

            // Act
            HttpResponseMessage response = await httpClient.PostAsync("/api/post/image", null);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Theory]
        [InlineData("invalidtxt.txt", "text/plain")]
        [InlineData("invalidtxt.txt", "image/jpg")]
        [InlineData("InvalidSignatureImg.png", "image/png")]
        [InlineData("InvalidSignatureImg.png", "image/jpg")]
        public async Task CacheImages_InvalidFile_BadRequest(string fileName, string contentType)
        {
            // Arrange
            HttpClient httpClient = CreateClient();
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(httpClient, authorizeToken);

            // Act
            HttpResponseMessage uploadImageResponse = await UploadImageAsync(httpClient, fileName, contentType);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, uploadImageResponse.StatusCode);
        }

        [Fact]
        public async Task CacheImages_TooLarge_BadRequest()
        {
            // Arrange
            HttpClient httpClient = CreateClient();
            User user = Helper.GetFirstEntity<User>(WebApplicationFactory);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(httpClient, authorizeToken);

            // Act
            HttpResponseMessage uploadImageResponse = await UploadImageAsync(httpClient, "toolarge.jpg", "image/jpg");

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, uploadImageResponse.StatusCode);
        }

        [Fact]
        public async Task GetImage_NotFound()
        {
            // Arrange
            HttpClient httpClient = CreateClient();

            // Act
            HttpResponseMessage response = await httpClient.GetAsync("/api/post/image/notexist");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetImage_Ok()
        {
            // Arrange
            HttpClient httpClient = CreateClient();

            // Act
            HttpResponseMessage response = await httpClient.GetAsync("/api/post/image/seedImg");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        protected override void SeedData()
        {
            using var scope = WebApplicationFactory.Services.CreateScope();
            using BlogPlatformDbContext dbContext = Helper.GetNotLoggingDbContext<BlogPlatformDbContext>(scope.ServiceProvider);
            using BlogPlatformImgDbContext imgDbContext = Helper.GetNotLoggingDbContext<BlogPlatformImgDbContext>(scope.ServiceProvider);

            dbContext.Database.EnsureDeleted();
            dbContext.Database.Migrate();
            imgDbContext.Database.Migrate();

            FileStream seedImg = File.OpenRead(Path.Combine(Directory.GetCurrentDirectory(), "Post", "seedImg.jpg"));
            byte[] imgByte = new byte[seedImg.Length];
            seedImg.Read(imgByte, 0, imgByte.Length);
            imgDbContext.Images.Add(new Image(SeedImgFileName, "image/jpg", imgByte));
            imgDbContext.SaveChanges();
            string imgUrl = WebApplicationFactory.ClientOptions.BaseAddress.ToString() + "api/post/image/seedImg";

            User user1 = new("user1", "user1@user.com");
            User user2 = new("user2", "user2@user.com");

            dbContext.Users.AddRange(user1, user2);
            dbContext.SaveChanges();

            Role role = new("User", 1);
            dbContext.Roles.Add(role);
            user1.Roles = [role];
            user2.Roles = [role];
            dbContext.SaveChanges();

            EFCore.Models.Blog blog = new("blog1", "blogDes", user1.Id);

            dbContext.Blogs.AddRange(blog);
            dbContext.SaveChanges();

            EFCore.Models.Category category1 = new("category1", blog.Id);
            EFCore.Models.Category category2 = new("category2", blog.Id);

            dbContext.Categories.AddRange(category1, category2);
            dbContext.SaveChanges();

            EFCore.Models.Post[] posts = [
                new("title1", "content1", category1.Id),
                new("title2", "content2", category1.Id),
                new("title3", "content3", category1.Id),
                new("title4", $"content4<img src=\"{imgUrl}\"/>", category2.Id),
            ];

            posts[0].Tags.AddRange(["tag1", "tag2"]);
            posts[1].Tags.AddRange(["tag1", "tag2"]);
            posts[2].Tags.Add("tag1");
            posts[3].Tags.Add("tag2");
            dbContext.Posts.AddRange(posts);
            dbContext.SaveChanges();

            EFCore.Models.Comment comment = new("content1", posts[0].Id, user1.Id, null);
            dbContext.Comments.Add(comment);
            dbContext.SaveChanges();
        }

        protected override void InitWebApplicationFactory(string dbName)
        {
            base.InitWebApplicationFactory(dbName);
            WebApplicationFactory = WebApplicationFactory.WithWebHostBuilder(cnf =>
            {
                cnf.ConfigureServices(services =>
                {
                    services.RemoveAll<DbContextOptions<BlogPlatformImgDbContext>>();
                    services.RemoveAll<BlogPlatformImgDbContext>();
                    JsonNode connectionStringNode = JsonNode.Parse(File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "testConnectionStrings.json"))) ?? throw new Exception();
                    string connectionString = connectionStringNode["BlogPlatformImgDb"]?.GetValue<string>() ?? throw new Exception();
                    connectionString += $"database={dbName};";

                    services.AddDbContext<BlogPlatformImgDbContext>(options =>
                    {
                        options.UseMySql(connectionString, MySqlServerVersion.LatestSupportedServerVersion);
                        options.EnableDetailedErrors();
                        options.EnableSensitiveDataLogging();
                    });
                });
            });
        }

        private static async Task<HttpResponseMessage> UploadImageAsync(HttpClient httpClient, string fileName, string contentType)
        {
            using FileStream fileStream = File.OpenRead(Path.Combine(Directory.GetCurrentDirectory(), "Post", fileName));
            StreamContent imgStreamContent = new(fileStream);
            imgStreamContent.Headers.ContentType = new(contentType);

            MultipartFormDataContent multipartFormDataContent = new()
            {
                { imgStreamContent, "image", fileName }
            };

            HttpResponseMessage imgCacheResponse = await httpClient.PostAsync("/api/post/image", multipartFormDataContent);
            return imgCacheResponse;
        }

        private static string GetImageLocation(HttpResponseMessage imgCacheResponse)
        {
            return imgCacheResponse.Headers.Location?.ToString() ?? throw new Exception();
        }

        private async Task<bool> IsImageDataExist(string fileName)
        {
            using var scope = WebApplicationFactory.Services.CreateScope();
            BlogPlatformImgDbContext dbContext = scope.ServiceProvider.GetRequiredService<BlogPlatformImgDbContext>();
            return await dbContext.Images.AnyAsync(i => i.Name == fileName);
        }
    }
}
