﻿using BlogPlatform.Api.Controllers;
using BlogPlatform.Api.Tests.DbContextMock;
using BlogPlatform.EFCore.Models;
using BlogPlatform.EFCore.Models.Abstractions;
using BlogPlatform.Shared.Models.Category;

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

            blogs[0].Categories = [categories[0], categories[2], categories[4]];
            blogs[1].Categories = [categories[1], categories[3], categories[5]];
            categories[0].Blog = blogs[0];
            categories[1].Blog = blogs[1];
            categories[2].Blog = blogs[0];
            categories[3].Blog = blogs[1];
            categories[4].Blog = blogs[0];
            categories[5].Blog = blogs[1];

            _blogDbSetMock = _setUp.DbContextMock.SetDbSet(db => db.Blogs, blogs);
            _categoryDbSetMock = _setUp.DbContextMock.SetDbSet(db => db.Categories, categories);

            XUnitLogger<CategoryController> controllerLogger = new(testOutputHelper);
            TimeProvider timeProvider = TimeProvider.System;
            _categoryController = new(_setUp.DbContextMock.Object, _setUp.SoftDeleteServiceMock.Object, timeProvider, controllerLogger);
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
            IActionResult result = await _categoryController.CreateAsync(new("Category4"), 3, CancellationToken.None);

            // Assert
            Utils.VerifyBadRequestResult(result);
            _setUp.DbContextMock.Verify(d => d.SaveChangesAsync(CancellationToken.None), Times.Never);
            _categoryDbSetMock.Verify(d => d.FindAsync(new object[] { 3 }, CancellationToken.None), Times.Never);
        }

        [Fact]
        public async Task Create_CreatedAtAction()
        {
            // Act
            IActionResult result = await _categoryController.CreateAsync(new("Category4"), 1, CancellationToken.None);

            // Assert
            Utils.VerifyCreatedResult(result, "Get", "Category");
            _categoryDbSetMock.Verify(d => d.Add(It.IsAny<Category>()), Times.Once);
            _setUp.DbContextMock.Verify(d => d.SaveChangesAsync(CancellationToken.None), Times.Once);
        }

        [Fact]
        public async Task Update_BlogNotExist()
        {
            // Act
            IActionResult result = await _categoryController.UpdateAsync(7, new("newCategoryName"), 7, CancellationToken.None);

            // Assert
            Utils.VerifyNotFoundResult(result);
            _setUp.DbContextMock.Verify(d => d.SaveChangesAsync(CancellationToken.None), Times.Never);
        }

        [Fact]
        public async Task Update_NotFound()
        {
            // Act
            IActionResult result = await _categoryController.UpdateAsync(7, new("newCategoryName"), 1, CancellationToken.None);

            // Assert
            Utils.VerifyNotFoundResult(result);
            _setUp.DbContextMock.Verify(d => d.SaveChangesAsync(CancellationToken.None), Times.Never);
        }

        [Fact]
        public async Task Update_NoContent()
        {
            // Act
            IActionResult result = await _categoryController.UpdateAsync(1, new("newCategoryName"), 1, CancellationToken.None);

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
            Utils.VerifyNotFoundResult(result);
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
        public async Task Delete_InternalServerError()
        {
            // Arrange
            StatusGenericHandler<int> errorStatus = new();
            errorStatus.AddError("error");
            _setUp.SoftDeleteServiceMock.Setup(s => s.SetSoftDeleteAsync(It.IsAny<Category>(), It.IsAny<bool>()))
                .ReturnsAsync(errorStatus);

            // Act
            IActionResult result = await _categoryController.DeleteAsync(1, 1, CancellationToken.None);

            // Assert
            Utils.VerifyInternalServerError(result);
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
    }
}
