using BlogPlatform.Api.Controllers;
using BlogPlatform.Api.Models;
using BlogPlatform.Api.Tests.DbContextMock;
using BlogPlatform.EFCore.Models;
using BlogPlatform.EFCore.Models.Abstractions;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Moq;

using StatusGeneric;

using Xunit.Abstractions;

using Utils = BlogPlatform.Api.Tests.Controllers.ControllerTestsUtils;

namespace BlogPlatform.Api.Tests.Controllers
{

    public class BlogControllerTests
    {
        private readonly ControllerTestsSetUp _setUp;
        private Mock<DbSet<Blog>> _blogDbSetMock;
        private readonly BlogController _blogController;

        public BlogControllerTests(ITestOutputHelper testOutputHelper)
        {
            _setUp = new();

            List<Blog> blogs =
            [
                new("blog1", "description1", 1) { Id = 1, SoftDeletedAt = EntityBase.DefaultSoftDeletedAt },
                new("blog2", "description2", 2) { Id = 2, SoftDeletedAt = EntityBase.DefaultSoftDeletedAt },
                new("blog3", "description3", 3) { Id = 3, SoftDeletedAt = EntityBase.DefaultSoftDeletedAt}
            ];

            _blogDbSetMock = _setUp.DbContextMock.SetDbSet(db => db.Blogs, blogs);

            XUnitLogger<BlogController> controllerLogger = new(testOutputHelper);

            _blogController = new(_setUp.DbContextMock.Object, _setUp.SoftDeleteServiceMock.Object, controllerLogger);
        }

        [Fact]
        public async Task Get_Ok()
        {
            // Act
            IActionResult result = await _blogController.GetAsync(1, CancellationToken.None);

            // Assert
            _blogDbSetMock.Verify(d => d.FindAsync(new object[] { 1 }, CancellationToken.None), Times.Once);

            Utils.VerifyOkObjectResult<BlogRead>(result);
        }

        [Fact]
        public async Task Get_NotFound()
        {
            // Act
            IActionResult result = await _blogController.GetAsync(5, CancellationToken.None);

            // Assert
            Utils.VerifyNotFoundResult(result);
            _blogDbSetMock.Verify(d => d.FindAsync(new object[] { 5 }, CancellationToken.None), Times.Once);
        }

        [Fact]
        public async Task Create_Conflict()
        {
            // Act
            IActionResult result = await _blogController.CreateAsync("blog1", "bb", 1, CancellationToken.None);

            // Assert
            ConflictObjectResult conflictResult = Assert.IsType<ConflictObjectResult>(result);
            Utils.VerifyConflictResult(conflictResult);
            _blogDbSetMock.Verify(set => set.Add(It.IsAny<Blog>()), Times.Never);
            _setUp.DbContextMock.Verify(db => db.SaveChangesAsync(CancellationToken.None), Times.Never);
        }

        [Fact]
        public async Task Create_CreatedAtAction()
        {
            // Act
            IActionResult result = await _blogController.CreateAsync("blog4", "description4", 4, CancellationToken.None);

            // Assert
            Utils.VerifyCreatedResult(result, nameof(BlogController.GetAsync), "Blog");
            _blogDbSetMock.Verify(set => set.Add(It.IsAny<Blog>()), Times.Once);
            _setUp.DbContextMock.Verify(db => db.SaveChangesAsync(CancellationToken.None), Times.Once);
        }

        [Fact]
        public async Task Update_NotFound()
        {
            // Act
            IActionResult result = await _blogController.UpdateAsync(5, "blog5", "description5", 1, CancellationToken.None);

            // Assert
            Utils.VerifyNotFoundResult(result);
            _blogDbSetMock.Verify(d => d.FindAsync(new object[] { 5 }, CancellationToken.None), Times.Once);
            _setUp.DbContextMock.Verify(db => db.SaveChangesAsync(CancellationToken.None), Times.Never);
        }

        [Fact]
        public async Task Update_Forbidden()
        {
            // Act
            IActionResult result = await _blogController.UpdateAsync(1, "blog1", "description1", 2, CancellationToken.None);

            // Assert
            Utils.VerifyForbidResult(result);
            _blogDbSetMock.Verify(d => d.FindAsync(new object[] { 1 }, CancellationToken.None), Times.Once);
            _setUp.DbContextMock.Verify(db => db.SaveChangesAsync(CancellationToken.None), Times.Never);
        }

        [Fact]
        public async Task Update_Ok()
        {
            // Act
            IActionResult result = await _blogController.UpdateAsync(1, "blog1", "description1", 1, CancellationToken.None);

            // Assert
            Utils.VerifyOkResult(result);
            _blogDbSetMock.Verify(d => d.FindAsync(new object[] { 1 }, CancellationToken.None), Times.Once);
            _setUp.DbContextMock.Verify(db => db.SaveChangesAsync(CancellationToken.None), Times.Once);
        }

        [Fact]
        public async Task Delete_NotFound()
        {
            // Act
            IActionResult result = await _blogController.DeleteAsync(5, 1, CancellationToken.None);

            // Assert
            Utils.VerifyNotFoundResult(result);
            _blogDbSetMock.Verify(d => d.FindAsync(new object[] { 5 }, CancellationToken.None), Times.Once);
        }

        [Fact]
        public async Task Delete_Forbidden()
        {
            // Act
            IActionResult result = await _blogController.DeleteAsync(1, 2, CancellationToken.None);

            // Assert
            Utils.VerifyForbidResult(result);
            _blogDbSetMock.Verify(d => d.FindAsync(new object[] { 1 }, CancellationToken.None), Times.Once);
        }

        [Fact]
        public async Task Delete_BadRequest()
        {
            // Arrange
            StatusGenericHandler<int> errorStatus = new();
            errorStatus.AddError("error");
            _setUp.SoftDeleteServiceMock.Setup(s => s.SetSoftDeleteAsync(It.IsAny<Blog>(), It.IsAny<bool>()))
                .ReturnsAsync(errorStatus);

            // Act
            IActionResult result = await _blogController.DeleteAsync(1, 1, CancellationToken.None);

            // Assert
            Utils.VerifyBadRequestResult(result);
        }

        [Fact]
        public async Task Delete_NoContent()
        {
            // Act
            IActionResult result = await _blogController.DeleteAsync(1, 1, CancellationToken.None);

            // Assert
            Utils.VerifyNoContentResult(result);
            _blogDbSetMock.Verify(d => d.FindAsync(new object[] { 1 }, CancellationToken.None), Times.Once);
            _setUp.SoftDeleteServiceMock.Verify(s => s.SetSoftDeleteAsync(It.IsAny<Blog>(), true), Times.Once);
        }
    }
}
