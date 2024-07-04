using BlogPlatform.Api.Controllers;
using BlogPlatform.Api.Identity.Constants;
using BlogPlatform.Api.Tests.DbContextMock;
using BlogPlatform.EFCore.Models;
using BlogPlatform.EFCore.Models.Abstractions;
using BlogPlatform.Shared.Models.Admin;
using BlogPlatform.Shared.Models.User;
using BlogPlatform.Shared.Services.Interfaces;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;

using Moq;

using StatusGeneric;

using Xunit.Abstractions;

using Utils = BlogPlatform.Api.Tests.Controllers.ControllerTestsUtils;

namespace BlogPlatform.Api.Tests.Controllers
{
    public class AdminControllerTests
    {
        private readonly ControllerTestsSetUp _setUp;
        private readonly AdminController _adminController;

        public AdminControllerTests(ITestOutputHelper testOutputHelper)
        {
            _setUp = new();

            List<User> users = [
                new User("user0", "user@user.com") { Id = 1, SoftDeletedAt = EntityBase.DefaultSoftDeletedAt },
                new User("admin0", "admin@admin.com") { Id = 2, SoftDeletedAt = EntityBase.DefaultSoftDeletedAt },
                new User("deleted0", "deleted@deleted.com") { Id = 3, SoftDeleteLevel = 1, SoftDeletedAt = DateTimeOffset.UtcNow }
            ];

            List<Role> roles = [
                new Role(PolicyConstants.AdminRolePolicy, 0) { Id = 1, SoftDeletedAt = EntityBase.DefaultSoftDeletedAt },
                new Role(PolicyConstants.UserRolePolicy, 1) { Id = 2, SoftDeletedAt = EntityBase.DefaultSoftDeletedAt },
            ];

            List<Blog> blogs = [
                new Blog("blog0", "blog0", 1) { Id = 1, SoftDeletedAt = EntityBase.DefaultSoftDeletedAt },
            ];

            List<Category> categories = [
                new Category("category0", 1) { Id = 1, SoftDeletedAt = EntityBase.DefaultSoftDeletedAt },
                new Category("category1", 2) { Id = 2, SoftDeletedAt = EntityBase.DefaultSoftDeletedAt },
            ];

            List<Post> posts = [
                new Post("post0", "post0", 1) {Id = 1, SoftDeletedAt = EntityBase.DefaultSoftDeletedAt },
                new Post("post1", "post1", 2) {Id = 2, SoftDeletedAt = EntityBase.DefaultSoftDeletedAt },
            ];

            List<Comment> comments = [
                new Comment("content0", 1, 1, null) { Id = 1, SoftDeletedAt = EntityBase.DefaultSoftDeletedAt },
                new Comment("content1", 2, 2, 1) { Id = 2, SoftDeletedAt = EntityBase.DefaultSoftDeletedAt },
            ];

            users[0].Roles = [roles[1]];
            users[0].BasicAccounts = [new BasicAccount("userId", "asda", 1)];
            users[0].Blog = [blogs[0]];
            users[0].OAuthAccounts = [];
            users[1].Roles = [roles[0], roles[1]];
            users[2].Roles = [roles[1]];

            _setUp.DbContextMock.SetDbSet(db => db.Users, users);
            _setUp.DbContextMock.SetDbSet(db => db.Roles, roles);
            _setUp.DbContextMock.SetDbSet(db => db.Blogs, blogs);
            _setUp.DbContextMock.SetDbSet(db => db.Categories, categories);
            _setUp.DbContextMock.SetDbSet(db => db.Posts, posts);
            _setUp.DbContextMock.SetDbSet(db => db.Comments, comments);

            XUnitLogger<AdminController> controllerLogger = new(testOutputHelper);

            Mock<IMailSender> mailSenderMock = new();
            TimeProvider timeProvider = TimeProvider.System;
            _adminController = new(_setUp.DbContextMock.Object, _setUp.SoftDeleteServiceMock.Object, mailSenderMock.Object, timeProvider, controllerLogger);

            RouteData routeData = new();
            routeData.Routers.Add(new Mock<IRouter>().Object);
            ActionContext actionContext = new(new DefaultHttpContext(), routeData, new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());
            _adminController.Url = new UrlHelper(actionContext);
        }

        [Theory]
        [InlineData(new int[] { 1, 2 })]
        [InlineData(null)]
        public async Task SendEmails_Ok(int[]? userIds)
        {
            // Act
            IActionResult result = await _adminController.SendEmails(new("subject", "body", userIds?.ToList()), CancellationToken.None);

            // Assert
            Utils.VerifyOkResult(result);
        }

        [Fact]
        public async Task GetUserAsync_NotFound()
        {
            // Act
            IActionResult result = await _adminController.GetUserAsync(new SearchUser(false, null, null, "notExistUser"), CancellationToken.None);

            // Assert
            Utils.VerifyNotFoundResult(result);
        }

        [Fact]
        public async Task GetUserAsync_Ok()
        {
            // Act
            IActionResult result = await _adminController.GetUserAsync(new SearchUser(false, null, null, "user0"), CancellationToken.None);

            // Assert
            Utils.VerifyOkObjectResult<UserRead>(result);
        }

        [Fact]
        public async Task DeleteUserAsync_NotFound()
        {
            // Act
            IActionResult result = await _adminController.DeleteUserAsync(new("notExistUser"), CancellationToken.None);

            // Assert
            Utils.VerifyNotFoundResult(result);
            _setUp.SoftDeleteServiceMock.Verify(s => s.SetSoftDeleteAsync(It.IsAny<User>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task DeleteUserAsync_InternalServerError()
        {
            // Arrange
            StatusGenericHandler<int> statusGenericHandler = new();
            statusGenericHandler.AddError("error");
            _setUp.SoftDeleteServiceMock.Setup(s => s.SetSoftDeleteAsync(It.IsAny<User>(), It.IsAny<bool>())).ReturnsAsync(statusGenericHandler);

            // Act
            IActionResult result = await _adminController.DeleteUserAsync(new("user@user.com"), CancellationToken.None);

            // Assert
            Utils.VerifyInternalServerError(result);
            _setUp.SoftDeleteServiceMock.Verify(s => s.SetSoftDeleteAsync(It.IsAny<User>(), It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task DeleteUserAsync_NoContent()
        {
            // Act
            IActionResult result = await _adminController.DeleteUserAsync(new("user@user.com"), CancellationToken.None);

            // Assert
            Utils.VerifyNoContentResult(result);
            _setUp.SoftDeleteServiceMock.Verify(s => s.SetSoftDeleteAsync(It.IsAny<User>(), true), Times.Once);
        }

        [Fact]
        public async Task BanUserAsync_NotFound()
        {
            // Act
            IActionResult result = await _adminController.BanUserAsync(new("notExistUser", TimeSpan.FromDays(1)), CancellationToken.None);

            // Assert
            Utils.VerifyNotFoundResult(result);
            _setUp.DbContextMock.Verify(db => db.SaveChangesAsync(CancellationToken.None), Times.Never);
        }

        [Fact]
        public async Task BanUserAsync_Ok()
        {
            // Act
            IActionResult result = await _adminController.BanUserAsync(new("user@user.com", TimeSpan.FromDays(1)), CancellationToken.None);

            // Assert
            Utils.VerifyOkResult(result);
            _setUp.DbContextMock.Verify(db => db.SaveChangesAsync(CancellationToken.None), Times.Once);
        }

        [Fact]
        public async Task UnbanUserAsync_NotFound()
        {
            // Act
            IActionResult result = await _adminController.UnbanUserAsync(new("notExistUser"), CancellationToken.None);

            // Assert
            Utils.VerifyNotFoundResult(result);
            _setUp.DbContextMock.Verify(db => db.SaveChangesAsync(CancellationToken.None), Times.Never);
        }

        [Fact]
        public async Task UnbanUserAsync_Ok()
        {
            // Act
            IActionResult result = await _adminController.UnbanUserAsync(new("user@user.com"), CancellationToken.None);

            // Assert
            Utils.VerifyOkResult(result);
            _setUp.DbContextMock.Verify(db => db.SaveChangesAsync(CancellationToken.None), Times.Once);
        }

        [Fact]
        public async Task DeletePostAsync_NotFound()
        {
            // Act
            IActionResult result = await _adminController.DeletePostAsync(7, CancellationToken.None);

            // Assert
            Utils.VerifyNotFoundResult(result);
            _setUp.SoftDeleteServiceMock.Verify(s => s.SetSoftDeleteAsync(It.IsAny<Post>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task DeletePostAsync_NoContent()
        {
            // Act
            IActionResult result = await _adminController.DeletePostAsync(1, CancellationToken.None);

            // Assert
            Utils.VerifyNoContentResult(result);
            _setUp.SoftDeleteServiceMock.Verify(s => s.SetSoftDeleteAsync(It.IsAny<Post>(), true), Times.Once);
        }

        [Fact]
        public async Task DeleteCommentAsync_NotFound()
        {
            // Act
            IActionResult result = await _adminController.DeleteCommentAsync(7, CancellationToken.None);

            // Assert
            Utils.VerifyNotFoundResult(result);
            _setUp.SoftDeleteServiceMock.Verify(s => s.SetSoftDeleteAsync(It.IsAny<Comment>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task DeleteCommentAsync_NoContent()
        {
            // Act
            IActionResult result = await _adminController.DeleteCommentAsync(1, CancellationToken.None);

            // Assert
            Utils.VerifyNoContentResult(result);
            _setUp.SoftDeleteServiceMock.Verify(s => s.SetSoftDeleteAsync(It.IsAny<Comment>(), true), Times.Once);
        }
    }
}
