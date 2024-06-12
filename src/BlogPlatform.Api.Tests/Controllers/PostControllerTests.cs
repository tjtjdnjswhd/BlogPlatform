using BlogPlatform.Api.Controllers;
using BlogPlatform.Api.Models;
using BlogPlatform.Api.Services.Interfaces;
using BlogPlatform.Api.Tests.DbContextMock;
using BlogPlatform.EFCore.Models;
using BlogPlatform.EFCore.Models.Abstractions;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

using Moq;

using StatusGeneric;

using Xunit.Abstractions;

using Utils = BlogPlatform.Api.Tests.Controllers.ControllerTestsUtils;

namespace BlogPlatform.Api.Tests.Controllers
{
    public class PostControllerTests
    {
        private readonly ControllerTestsSetUp _setUp;
        private Mock<DbSet<Post>> _postDbSetMock;
        private readonly PostController _postController;

        public PostControllerTests(ITestOutputHelper testOutputHelper)
        {
            _setUp = new();

            List<Blog> blogs = [
                new Blog("blog0", "blog0", 1) { Id = 1, SoftDeletedAt = EntityBase.DefaultSoftDeletedAt },
                new Blog("blog1", "blog1", 2) { Id = 2, SoftDeletedAt = EntityBase.DefaultSoftDeletedAt },
            ];

            List<Category> categories = [
                new Category("category0", 1) { Id = 1, BlogId = 1, Blog = blogs.First(), SoftDeletedAt = EntityBase.DefaultSoftDeletedAt, },
                new Category("category1", 1) { Id = 2, BlogId = 1, Blog = blogs.First(), SoftDeletedAt = EntityBase.DefaultSoftDeletedAt, },
                new Category("category2", 1) { Id = 3, BlogId = 2, Blog = blogs.Last(), SoftDeletedAt = EntityBase.DefaultSoftDeletedAt, },
            ];

            List<Post> posts = [
                new Post("title0", "content0", 1) { Id = 1, CategoryId = 1, SoftDeletedAt = EntityBase.DefaultSoftDeletedAt, Category = categories.First() },
                new Post("title1", "content1", 1) { Id = 2, CategoryId = 1, SoftDeletedAt = EntityBase.DefaultSoftDeletedAt, Category = categories.First() },
                new Post("title2", "content2", 2) { Id = 3, CategoryId = 2, SoftDeletedAt = EntityBase.DefaultSoftDeletedAt, Category = categories.ElementAt(1) },
                new Post("title3", "content3", 3) { Id = 4, CategoryId = 3, SoftDeletedAt = EntityBase.DefaultSoftDeletedAt, Category = categories.Last() },
            ];

            posts.First().Tags.Add("tag0");
            posts.First().Tags.Add("tag1");

            posts.ElementAt(1).Tags.Add("tag0");
            posts.ElementAt(1).Tags.Add("tag1");
            posts.ElementAt(1).Tags.Add("tag2");

            posts.ElementAt(2).Tags.Add("tag0");
            posts.ElementAt(2).Tags.Add("tag2");

            _setUp.DbContextMock.SetDbSet(d => d.Categories, categories);
            _setUp.DbContextMock.SetDbSet(d => d.Blogs, blogs);
            _postDbSetMock = _setUp.DbContextMock.SetDbSet(d => d.Posts, posts);
            XUnitLogger<PostController> controllerLogger = new(testOutputHelper);

            OptionsWrapper<MemoryDistributedCacheOptions> cacheOption = new(new MemoryDistributedCacheOptions());
            Mock<IPostImageService> imageServiceMock = new();
            imageServiceMock.Setup(i => i.WithTransaction(It.IsAny<IDbContextTransaction>())).Returns(imageServiceMock.Object);
            imageServiceMock.Setup(i => i.CacheImagesToDatabaseAsync(It.IsAny<IEnumerable<string>>(), It.Is<CancellationToken>(c => true))).ReturnsAsync(true);
            MemoryDistributedCache cache = new(cacheOption);

            _postController = new(_setUp.DbContextMock.Object, _setUp.SoftDeleteServiceMock.Object, imageServiceMock.Object, cache, controllerLogger);
            ControllerContext controllerContext = new();

            HttpContext httpContext = new DefaultHttpContext();
            Mock<IServerVariablesFeature> serverVariablesFeatureMock = new();
            serverVariablesFeatureMock.SetupGet(f => f["baseUri"]).Returns("https://localhost");
            httpContext.Features.Set(serverVariablesFeatureMock.Object);
            controllerContext.HttpContext = httpContext;

            _postController.ControllerContext = controllerContext;
        }

        [Fact]
        public async Task GetAsync_NotFound()
        {
            // Act
            IActionResult result = await _postController.GetAsync(5, CancellationToken.None);

            // Assert
            Utils.VerifyNotFoundResult(result);
        }

        [Fact]
        public async Task GetAsync_Ok()
        {
            // Act
            IActionResult result = await _postController.GetAsync(1, CancellationToken.None);

            // Assert
            Utils.VerifyOkObjectResult<PostRead>(result);
            _postDbSetMock.Verify(d => d.FindAsync(new object[] { 1 }, CancellationToken.None), Times.Once);
        }

        [Fact]
        public void Get_ByBlog()
        {
            // Act
            IAsyncEnumerable<PostSearchResult> result = _postController.Get(new PostSearch(1, null, null, null, null, null, null, null, null));

            // Assert
            IEnumerable<PostSearchResult> enumerableResult = result.ToBlockingEnumerable();
            Assert.Equal(3, enumerableResult.Count());
        }

        [Fact]
        public void Get_ByCategory()
        {
            // Act
            IAsyncEnumerable<PostSearchResult> result = _postController.Get(new PostSearch(null, 1, null, null, null, null, null, null, null));

            // Assert
            IEnumerable<PostSearchResult> enumerableResult = result.ToBlockingEnumerable();
            Assert.Equal(2, enumerableResult.Count());
        }

        [Fact]
        public void Get_ByTitle()
        {
            // Act
            IAsyncEnumerable<PostSearchResult> result = _postController.Get(new PostSearch(1, null, "title0", null, null, null, null, null, null));

            // Assert
            IEnumerable<PostSearchResult> enumerableResult = result.ToBlockingEnumerable();
            Assert.Single(enumerableResult);
        }

        [Fact]
        public void Get_ByContent()
        {
            // Act
            IAsyncEnumerable<PostSearchResult> result = _postController.Get(new PostSearch(1, null, null, "content0", null, null, null, null, null));

            // Assert
            IEnumerable<PostSearchResult> enumerableResult = result.ToBlockingEnumerable();
            Assert.Single(enumerableResult);
        }

        // Tag Filter는 EF Core function을 사용하므로 테스트 불가
        //[Fact]
        //public void Get_ByTag()
        //{
        //    // Act
        //    IAsyncEnumerable<PostSearchResult> result = _postController.Get(new PostSearch(1, null, null, null, null, null, null, null, ["tag0", "tag1"], TagFilterOption.All));

        //    // Assert
        //    IEnumerable<PostSearchResult> enumerableResult = result.ToBlockingEnumerable();
        //    Assert.Equal(2, enumerableResult.Count());
        //}

        [Fact]
        public async Task Create_NotFound()
        {
            // Act
            IActionResult result = await _postController.CreateAsync("title", "content", "notExistCategory", [], 1, CancellationToken.None);

            // Assert
            Utils.VerifyNotFoundResult(result);
            _postDbSetMock.Verify(d => d.Add(It.IsAny<Post>()), Times.Never);
            _setUp.DbContextMock.Verify(d => d.SaveChangesAsync(CancellationToken.None), Times.Never);
        }

        [Fact]
        public async Task Create_Forbid()
        {
            // Act
            IActionResult result = await _postController.CreateAsync("title", "content", "category0", [], 2, CancellationToken.None);

            // Assert
            Utils.VerifyForbidResult(result);
            _postDbSetMock.Verify(d => d.Add(It.IsAny<Post>()), Times.Never);
            _setUp.DbContextMock.Verify(d => d.SaveChangesAsync(CancellationToken.None), Times.Never);
        }

        [Fact]
        public async Task Create_Ok()
        {
            // Act
            IActionResult result = await _postController.CreateAsync("title", "content", "category0", [], 1, CancellationToken.None);

            // Assert
            Utils.VerifyCreatedResult(result, "GetAsync", "Post");
            _postDbSetMock.Verify(d => d.Add(It.IsAny<Post>()), Times.Once);
            _setUp.DbContextMock.Verify(d => d.SaveChangesAsync(CancellationToken.None), Times.Once);
        }

        [Fact]
        public async Task Update_NotFound()
        {
            // Act
            IActionResult result = await _postController.UpdateAsync(5, "title", "content", "category0", [], 1, CancellationToken.None);

            // Assert
            Utils.VerifyNotFoundResult(result);
            _postDbSetMock.Verify(d => d.FindAsync(new object[] { 5 }, CancellationToken.None), Times.Once);
            _setUp.DbContextMock.Verify(d => d.SaveChangesAsync(CancellationToken.None), Times.Never);
        }

        [Fact]
        public async Task Update_Forbid()
        {
            // Act
            IActionResult result = await _postController.UpdateAsync(1, "title", "content", "category0", [], 2, CancellationToken.None);

            // Assert
            Utils.VerifyForbidResult(result);
            _postDbSetMock.Verify(d => d.FindAsync(new object[] { 1 }, CancellationToken.None), Times.Never);
            _setUp.DbContextMock.Verify(d => d.SaveChangesAsync(CancellationToken.None), Times.Never);
        }

        [Fact]
        public async Task Update_NoContent()
        {
            // Act
            IActionResult result = await _postController.UpdateAsync(1, "title", "content", "category0", [], 1, CancellationToken.None);

            // Assert
            Utils.VerifyNoContentResult(result);
            _postDbSetMock.Verify(d => d.FindAsync(new object[] { 1 }, CancellationToken.None), Times.Once);
            _setUp.DbContextMock.Verify(d => d.SaveChangesAsync(CancellationToken.None), Times.Once);
        }

        [Fact]
        public async Task Delete_NotFound()
        {
            // Act
            IActionResult result = await _postController.DeleteAsync(5, 1, CancellationToken.None);

            // Assert
            Utils.VerifyNotFoundResult(result);
            _postDbSetMock.Verify(d => d.FindAsync(new object[] { 5 }, CancellationToken.None), Times.Once);
            _setUp.DbContextMock.Verify(d => d.SaveChangesAsync(CancellationToken.None), Times.Never);
        }

        [Fact]
        public async Task Delete_Forbid()
        {
            // Act
            IActionResult result = await _postController.DeleteAsync(1, 2, CancellationToken.None);

            // Assert
            Utils.VerifyForbidResult(result);
            _postDbSetMock.Verify(d => d.FindAsync(new object[] { 1 }, CancellationToken.None), Times.Once);
            _setUp.DbContextMock.Verify(d => d.SaveChangesAsync(CancellationToken.None), Times.Never);
        }

        [Fact]
        public async Task Delete_InternalServerError()
        {
            // Arrange
            StatusGenericHandler<int> errorStatus = new();
            errorStatus.AddError("error");
            _setUp.SoftDeleteServiceMock.Setup(s => s.SetSoftDeleteAsync(It.IsAny<Post>(), It.IsAny<bool>()))
                .ReturnsAsync(errorStatus);

            // Act
            IActionResult result = await _postController.DeleteAsync(1, 1, CancellationToken.None);

            // Assert
            Utils.VerifyInternalServerError(result);
        }

        [Fact]
        public async Task Delete_Ok()
        {
            // Act
            IActionResult result = await _postController.DeleteAsync(1, 1, CancellationToken.None);

            // Assert
            Utils.VerifyNoContentResult(result);
            _postDbSetMock.Verify(d => d.FindAsync(new object[] { 1 }, CancellationToken.None), Times.Once);
            _setUp.SoftDeleteServiceMock.Verify(s => s.SetSoftDeleteAsync(It.IsAny<Post>(), true), Times.Once);
        }
    }
}
