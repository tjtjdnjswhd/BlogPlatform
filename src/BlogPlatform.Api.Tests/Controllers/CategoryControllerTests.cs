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
    public class CategoryControllerTests
    {
        private readonly ControllerTestsSetUp _setUp;
        private Mock<DbSet<Category>> _categoryDbSetMock;
        private readonly Mock<DbSet<Blog>> _blogDbSetMock;
        private readonly CategoryController _categoryController;

        public CategoryControllerTests(ITestOutputHelper testOutputHelper)
        {
            _setUp = new();
            List<Blog> blogs = [
                new Blog("Blog1", "Description1", 1) { Id = 1, SoftDeletedAt = EntityBase.DefaultSoftDeletedAt },
                new Blog("Blog2", "Description2", 2) { Id = 2, SoftDeletedAt = EntityBase.DefaultSoftDeletedAt },
            ];

            List<Category> categories = [
                new Category("Category1", 1) { Id = 1, SoftDeletedAt = EntityBase.DefaultSoftDeletedAt },
                new Category("Category1", 2) { Id = 2, SoftDeletedAt = EntityBase.DefaultSoftDeletedAt },
                new Category("Category2", 1) { Id = 3, SoftDeletedAt = EntityBase.DefaultSoftDeletedAt },
                new Category("Category2", 2) { Id = 4, SoftDeletedAt = EntityBase.DefaultSoftDeletedAt },
                new Category("Category3", 1) { Id = 5, SoftDeletedAt = EntityBase.DefaultSoftDeletedAt },
                new Category("Category3", 2) { Id = 6, SoftDeletedAt = EntityBase.DefaultSoftDeletedAt },
            ];

            _blogDbSetMock = _setUp.DbContextMock.SetDbSet(db => db.Blogs, blogs);
            _categoryDbSetMock = _setUp.DbContextMock.SetDbSet(db => db.Categories, categories);

            XUnitLogger<CategoryController> controllerLogger = new(testOutputHelper);
            _categoryController = new(_setUp.DbContextMock.Object, _setUp.SoftDeleteServiceMock.Object, controllerLogger);
        }

        [Fact]
        public async Task Get_NotFound()
        {
            // Act
            IActionResult result = await _categoryController.GetAsync(7, CancellationToken.None);

            // Assert
            Utils.VerifyNotFoundResult(result);
            _categoryDbSetMock.Verify(d => d.FindAsync(new object[] { 7 }, CancellationToken.None), Times.Once);
            _setUp.DbContextMock.Verify(d => d.SaveChangesAsync(CancellationToken.None), Times.Never);
        }

        [Fact]
        public async Task Get_Ok()
        {
            // Act
            IActionResult result = await _categoryController.GetAsync(1, CancellationToken.None);

            // Assert
            Utils.VerifyOkObjectResult<CategoryRead>(result);
            _categoryDbSetMock.Verify(d => d.FindAsync(new object[] { 1 }, CancellationToken.None), Times.Once);
            _setUp.DbContextMock.Verify(d => d.SaveChangesAsync(CancellationToken.None), Times.Never);
        }

        [Fact]
        public async Task Create_BadRequest()
        {
            // Act
            IActionResult result = await _categoryController.CreateAsync("Category4", 3, CancellationToken.None);

            // Assert
            Utils.VerifyBadRequestResult(result);
            _setUp.DbContextMock.Verify(d => d.SaveChangesAsync(CancellationToken.None), Times.Never);
            _categoryDbSetMock.Verify(d => d.FindAsync(new object[] { 3 }, CancellationToken.None), Times.Never);
        }

        [Fact]
        public async Task Create_CreatedAtAction()
        {
            // Act
            IActionResult result = await _categoryController.CreateAsync("Category4", 1, CancellationToken.None);

            // Assert
            Utils.VerifyCreatedResult(result, nameof(CategoryController.GetAsync), "Category");
            _categoryDbSetMock.Verify(d => d.Add(It.IsAny<Category>()), Times.Once);
            _setUp.DbContextMock.Verify(d => d.SaveChangesAsync(CancellationToken.None), Times.Once);
        }

        [Fact]
        public async Task Update_BlogNotExist()
        {
            // Act
            IActionResult result = await _categoryController.UpdateAsync(7, "newCategoryName", 7, CancellationToken.None);

            // Assert
            Utils.VerifyBadRequestResult(result);
            _setUp.DbContextMock.Verify(d => d.SaveChangesAsync(CancellationToken.None), Times.Never);
        }

        [Fact]
        public async Task Update_NotFound()
        {
            // Act
            IActionResult result = await _categoryController.UpdateAsync(7, "newCategoryName", 1, CancellationToken.None);

            // Assert
            Utils.VerifyNotFoundResult(result);
            _setUp.DbContextMock.Verify(d => d.SaveChangesAsync(CancellationToken.None), Times.Never);
        }

        [Fact]
        public async Task Update_NoContent()
        {
            // Act
            IActionResult result = await _categoryController.UpdateAsync(1, "newCategoryName", 1, CancellationToken.None);

            // Assert
            Utils.VerifyNoContentResult(result);
            _setUp.DbContextMock.Verify(d => d.SaveChangesAsync(CancellationToken.None), Times.Once);
        }

        [Fact]
        public async Task Delete_BlogNotExist()
        {
            // Act
            IActionResult result = await _categoryController.DeleteAsync(7, 7, CancellationToken.None);

            // Assert
            Utils.VerifyBadRequestResult(result);
            _setUp.DbContextMock.Verify(d => d.SaveChangesAsync(CancellationToken.None), Times.Never);
            _setUp.SoftDeleteServiceMock.Verify(s => s.SetSoftDeleteAsync(It.IsAny<Category>(), true), Times.Never);
        }

        [Fact]
        public async Task Delete_NotFound()
        {
            // Act
            IActionResult result = await _categoryController.DeleteAsync(7, 1, CancellationToken.None);

            // Assert
            Utils.VerifyNotFoundResult(result);
            _setUp.DbContextMock.Verify(d => d.SaveChangesAsync(CancellationToken.None), Times.Never);
            _setUp.SoftDeleteServiceMock.Verify(s => s.SetSoftDeleteAsync(It.IsAny<Category>(), true), Times.Never);
        }

        [Fact]
        public async Task Delete_BadRequest()
        {
            // Arrange
            StatusGenericHandler<int> errorStatus = new();
            errorStatus.AddError("error");
            _setUp.SoftDeleteServiceMock.Setup(s => s.SetSoftDeleteAsync(It.IsAny<Category>(), It.IsAny<bool>()))
                .ReturnsAsync(errorStatus);

            // Act
            IActionResult result = await _categoryController.DeleteAsync(1, 1, CancellationToken.None);

            // Assert
            Utils.VerifyBadRequestResult(result);
        }

        [Fact]
        public async Task Delete_NoContent()
        {
            // Act
            IActionResult result = await _categoryController.DeleteAsync(1, 1, CancellationToken.None);

            // Assert
            Utils.VerifyNoContentResult(result);
            _setUp.SoftDeleteServiceMock.Verify(s => s.SetSoftDeleteAsync(It.IsAny<Category>(), true), Times.Once);
        }

        [Fact]
        public async Task Restore_BlogNotExist()
        {
            // Act
            IActionResult result = await _categoryController.RestoreAsync(7, 7, CancellationToken.None);

            // Assert
            Utils.VerifyBadRequestResult(result);
            _setUp.DbContextMock.Verify(d => d.SaveChangesAsync(CancellationToken.None), Times.Never);
            _setUp.SoftDeleteServiceMock.Verify(d => d.ResetSoftDeleteAsync(It.IsAny<Category>(), true), Times.Never);
        }

        [Fact]
        public async Task Restore_NotFound()
        {
            // Act
            IActionResult result = await _categoryController.RestoreAsync(7, 1, CancellationToken.None);

            // Assert
            Utils.VerifyNotFoundResult(result);
            _setUp.DbContextMock.Verify(d => d.SaveChangesAsync(CancellationToken.None), Times.Never);
            _setUp.SoftDeleteServiceMock.Verify(d => d.ResetSoftDeleteAsync(It.IsAny<Category>(), true), Times.Never);
        }

        [Fact]
        public async Task Restore_Expires()
        {
            // Arrange
            List<Category> categories = [new Category("category1", 1) { Id = 1, SoftDeleteLevel = 1, SoftDeletedAt = DateTimeOffset.UtcNow.AddDays(-2) }];
            _categoryDbSetMock = _setUp.DbContextMock.SetDbSet(db => db.Categories, categories);

            // Act
            IActionResult result = await _categoryController.RestoreAsync(1, 1, CancellationToken.None);

            // Assert
            Utils.VerifyBadRequestResult(result);
            _setUp.SoftDeleteServiceMock.Verify(s => s.ResetSoftDeleteAsync(It.IsAny<Category>(), true), Times.Never);
        }

        [Fact]
        public async Task Restore_NoContent()
        {
            // Arrange
            List<Category> categories = [new Category("category1", 1) { Id = 1, SoftDeleteLevel = 1, SoftDeletedAt = DateTimeOffset.UtcNow.AddHours(-23) }];
            _categoryDbSetMock = _setUp.DbContextMock.SetDbSet(db => db.Categories, categories);

            // Act
            IActionResult result = await _categoryController.RestoreAsync(1, 1, CancellationToken.None);

            // Assert
            Utils.VerifyNoContentResult(result);
            _setUp.SoftDeleteServiceMock.Verify(s => s.ResetSoftDeleteAsync(It.IsAny<Category>(), true), Times.Once);
        }
    }
}
